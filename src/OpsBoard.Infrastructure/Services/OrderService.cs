using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using OpsBoard.Application.Abstractions;
using OpsBoard.Application.Common;
using OpsBoard.Application.Orders;
using OpsBoard.Domain.Audit;
using OpsBoard.Domain.Orders;
using OpsBoard.Domain.Tables;
using OpsBoard.Infrastructure.Persistence;

namespace OpsBoard.Infrastructure.Services;

public sealed class OrderService
{
    private readonly OpsBoardDbContext _dbContext;
    private readonly IAuditService _auditService;
    private readonly ICurrentUser _currentUser;
    private readonly ISystemClock _clock;

    public OrderService(
        OpsBoardDbContext dbContext,
        IAuditService auditService,
        ICurrentUser currentUser,
        ISystemClock clock)
    {
        _dbContext = dbContext;
        _auditService = auditService;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<OrderDetailResponse> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
        {
            throw AppException.Unauthorized();
        }

        return await ExecuteInTransactionIfSupportedAsync(async () =>
        {
            if (request.Items.Count == 0)
            {
                throw AppException.BadRequest("An order must contain at least one order item.");
            }

            if (request.Items.Any(item => item.Quantity <= 0))
            {
                throw AppException.BadRequest("Order item quantity must be positive.");
            }

            var table = await _dbContext.RestaurantTables
                .FirstOrDefaultAsync(candidate => candidate.Id == request.TableId, cancellationToken)
                ?? throw AppException.NotFound("Table was not found.");

            var hasActiveOrder = await _dbContext.Orders.AnyAsync(
                order => order.TableId == request.TableId
                    && order.Status != OrderStatus.Paid
                    && order.Status != OrderStatus.Cancelled,
                cancellationToken);

            if (table.Status != TableStatus.Available || hasActiveOrder)
            {
                throw AppException.Conflict("Table is not available.", ErrorCodes.TableUnavailable);
            }

            var menuItemIds = request.Items.Select(item => item.MenuItemId).ToHashSet();
            var menuItems = await _dbContext.MenuItems
                .Where(item => menuItemIds.Contains(item.Id) && item.IsAvailable)
                .ToDictionaryAsync(item => item.Id, cancellationToken);

            if (menuItems.Count != menuItemIds.Count)
            {
                throw AppException.BadRequest("One or more menu items are unavailable.");
            }

            var drafts = request.Items.Select(item =>
            {
                var menuItem = menuItems[item.MenuItemId];
                return new OrderItemDraft(menuItem.Id, menuItem.Name, menuItem.Price, item.Quantity);
            }).ToArray();

            var order = Order.Create(table.Id, _currentUser.UserId.Value, drafts, _clock.UtcNow);
            table.MarkOccupied(_clock.UtcNow);
            _dbContext.Orders.Add(order);
            await _dbContext.SaveChangesAsync(cancellationToken);

            await _auditService.RecordAsync(
                _currentUser.UserId,
                _currentUser.FullName ?? "Unknown",
                AuditAction.OrderCreated,
                nameof(Order),
                order.Id,
                null,
                new { order.Id, order.Status, order.Total },
                cancellationToken);

            return ToDetail(order, table);
        }, cancellationToken);
    }

    public async Task<IReadOnlyList<OrderSummaryResponse>> GetActiveAsync(CancellationToken cancellationToken)
    {
        var activeOrders = await _dbContext.Orders
            .Where(order => order.Status != OrderStatus.Paid && order.Status != OrderStatus.Cancelled)
            .OrderBy(order => order.CreatedAtUtc)
            .ToArrayAsync(cancellationToken);
        var tableIds = activeOrders.Select(order => order.TableId).ToHashSet();
        var tables = await _dbContext.RestaurantTables
            .Where(table => tableIds.Contains(table.Id))
            .ToDictionaryAsync(table => table.Id, cancellationToken);

        return activeOrders
            .Select(order => ToSummary(order, tables[order.TableId]))
            .ToArray();
    }

    public async Task<OrderDetailResponse> GetByIdAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var order = await LoadOrderAsync(orderId, cancellationToken);
        var table = await LoadTableAsync(order.TableId, cancellationToken);
        return ToDetail(order, table);
    }

    public async Task<OrderDetailResponse> UpdateStatusAsync(
        Guid orderId,
        UpdateOrderStatusRequest request,
        CancellationToken cancellationToken)
    {
        return await ExecuteInTransactionIfSupportedAsync(async () =>
        {
            var order = await LoadOrderAsync(orderId, cancellationToken);
            var table = await LoadTableAsync(order.TableId, cancellationToken);
            var before = new { order.Id, order.Status };

            try
            {
                order.ChangeStatus(request.TargetStatus, _clock.UtcNow);
            }
            catch (InvalidOperationException exception)
            {
                throw AppException.Conflict(exception.Message, ErrorCodes.InvalidOrderTransition);
            }

            if (order.Status == OrderStatus.Paid)
            {
                table.MarkAvailable(_clock.UtcNow);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            await _auditService.RecordAsync(
                _currentUser.UserId,
                _currentUser.FullName ?? "Unknown",
                AuditAction.OrderStatusChanged,
                nameof(Order),
                order.Id,
                before,
                new { order.Id, order.Status },
                cancellationToken);

            return ToDetail(order, table);
        }, cancellationToken);
    }

    public async Task<OrderDetailResponse> CancelAsync(Guid orderId, CancellationToken cancellationToken)
    {
        return await ExecuteInTransactionIfSupportedAsync(async () =>
        {
            var order = await LoadOrderAsync(orderId, cancellationToken);
            var table = await LoadTableAsync(order.TableId, cancellationToken);
            var before = new { order.Id, order.Status };

            try
            {
                order.Cancel(_clock.UtcNow);
            }
            catch (InvalidOperationException exception)
            {
                throw AppException.Conflict(exception.Message, ErrorCodes.InvalidOrderTransition);
            }

            table.MarkAvailable(_clock.UtcNow);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await _auditService.RecordAsync(
                _currentUser.UserId,
                _currentUser.FullName ?? "Unknown",
                AuditAction.OrderCancelled,
                nameof(Order),
                order.Id,
                before,
                new { order.Id, order.Status },
                cancellationToken);

            return ToDetail(order, table);
        }, cancellationToken);
    }

    private async Task<T> ExecuteInTransactionIfSupportedAsync<T>(
        Func<Task<T>> operation,
        CancellationToken cancellationToken)
    {
        var transaction = _dbContext.Database.IsRelational()
            ? await _dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;

        try
        {
            var result = await operation();
            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            return result;
        }
        finally
        {
            if (transaction is not null)
            {
                await transaction.DisposeAsync();
            }
        }
    }

    private async Task<Order> LoadOrderAsync(Guid orderId, CancellationToken cancellationToken)
    {
        return await _dbContext.Orders
            .Include(order => order.Items)
            .FirstOrDefaultAsync(order => order.Id == orderId, cancellationToken)
            ?? throw AppException.NotFound("Order was not found.");
    }

    private async Task<RestaurantTable> LoadTableAsync(Guid tableId, CancellationToken cancellationToken)
    {
        return await _dbContext.RestaurantTables
            .FirstOrDefaultAsync(table => table.Id == tableId, cancellationToken)
            ?? throw AppException.NotFound("Table was not found.");
    }

    private static OrderDetailResponse ToDetail(Order order, RestaurantTable table)
    {
        return new OrderDetailResponse(
            order.Id,
            table.Id,
            table.Name,
            order.Status,
            order.Subtotal,
            order.Total,
            order.CreatedAtUtc,
            order.PaidAtUtc,
            order.Items.Select(item => new OrderItemResponse(
                item.Id,
                item.MenuItemId,
                item.ItemNameSnapshot,
                item.UnitPriceSnapshot,
                item.Quantity,
                item.LineTotal)).ToArray());
    }

    private static OrderSummaryResponse ToSummary(Order order, RestaurantTable table)
    {
        return new OrderSummaryResponse(
            order.Id,
            table.Id,
            table.Name,
            order.Status,
            order.Subtotal,
            order.Total,
            order.CreatedAtUtc,
            order.PaidAtUtc);
    }
}

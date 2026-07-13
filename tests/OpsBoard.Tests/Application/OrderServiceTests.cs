using Microsoft.EntityFrameworkCore;
using OpsBoard.Application.Abstractions;
using OpsBoard.Application.Common;
using OpsBoard.Application.Orders;
using OpsBoard.Domain.Audit;
using OpsBoard.Domain.Menus;
using OpsBoard.Domain.Orders;
using OpsBoard.Domain.Tables;
using OpsBoard.Domain.Users;
using OpsBoard.Infrastructure.Persistence;
using OpsBoard.Infrastructure.Services;

namespace OpsBoard.Tests.Application;

public sealed class OrderServiceTests
{
    private static readonly Guid StaffUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly DateTimeOffset FixedNow = new(2026, 7, 14, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CreateAsync_RejectsEmptyOrder()
    {
        await using var dbContext = CreateDbContext();
        var (_, table) = await SeedMenuAndTableAsync(dbContext);
        var service = CreateService(dbContext);

        var error = await Assert.ThrowsAsync<AppException>(() =>
            service.CreateAsync(new CreateOrderRequest(table.Id, Array.Empty<CreateOrderItemRequest>()), CancellationToken.None));

        Assert.Equal(400, error.StatusCode);
    }

    [Fact]
    public async Task CreateAsync_MarksAvailableTableOccupiedAndWritesAuditLog()
    {
        await using var dbContext = CreateDbContext();
        var (menuItem, table) = await SeedMenuAndTableAsync(dbContext);
        var service = CreateService(dbContext);

        var order = await service.CreateAsync(
            new CreateOrderRequest(table.Id, new[] { new CreateOrderItemRequest(menuItem.Id, 2) }),
            CancellationToken.None);

        Assert.Equal(110000m, order.Total);
        var updatedTable = await dbContext.RestaurantTables.FindAsync(table.Id);
        Assert.Equal(TableStatus.Occupied, updatedTable!.Status);
        Assert.Contains(dbContext.AuditLogs, log => log.Action == AuditAction.OrderCreated);
    }

    [Fact]
    public async Task CreateAsync_RejectsNonAvailableTable()
    {
        await using var dbContext = CreateDbContext();
        var (menuItem, table) = await SeedMenuAndTableAsync(dbContext, TableStatus.Occupied);
        var service = CreateService(dbContext);

        var error = await Assert.ThrowsAsync<AppException>(() =>
            service.CreateAsync(
                new CreateOrderRequest(table.Id, new[] { new CreateOrderItemRequest(menuItem.Id, 1) }),
                CancellationToken.None));

        Assert.Equal(409, error.StatusCode);
    }

    [Fact]
    public async Task UpdateStatusAsync_MarksPaidOrderTableAvailable()
    {
        await using var dbContext = CreateDbContext();
        var (menuItem, table) = await SeedMenuAndTableAsync(dbContext);
        var service = CreateService(dbContext);
        var order = await service.CreateAsync(
            new CreateOrderRequest(table.Id, new[] { new CreateOrderItemRequest(menuItem.Id, 1) }),
            CancellationToken.None);

        await service.UpdateStatusAsync(order.Id, new UpdateOrderStatusRequest(OrderStatus.Preparing), CancellationToken.None);
        await service.UpdateStatusAsync(order.Id, new UpdateOrderStatusRequest(OrderStatus.Ready), CancellationToken.None);
        await service.UpdateStatusAsync(order.Id, new UpdateOrderStatusRequest(OrderStatus.Served), CancellationToken.None);
        await service.UpdateStatusAsync(order.Id, new UpdateOrderStatusRequest(OrderStatus.Paid), CancellationToken.None);

        var updatedTable = await dbContext.RestaurantTables.FindAsync(table.Id);
        Assert.Equal(TableStatus.Available, updatedTable!.Status);
        Assert.Contains(dbContext.AuditLogs, log => log.Action == AuditAction.OrderStatusChanged);
    }

    [Fact]
    public async Task CancelAsync_MarksNewOrderTableAvailable()
    {
        await using var dbContext = CreateDbContext();
        var (menuItem, table) = await SeedMenuAndTableAsync(dbContext);
        var service = CreateService(dbContext);
        var order = await service.CreateAsync(
            new CreateOrderRequest(table.Id, new[] { new CreateOrderItemRequest(menuItem.Id, 1) }),
            CancellationToken.None);

        await service.CancelAsync(order.Id, CancellationToken.None);

        var updatedTable = await dbContext.RestaurantTables.FindAsync(table.Id);
        Assert.Equal(TableStatus.Available, updatedTable!.Status);
        Assert.Contains(dbContext.AuditLogs, log => log.Action == AuditAction.OrderCancelled);
    }

    [Fact]
    public async Task CancelAsync_RejectsReadyOrder()
    {
        await using var dbContext = CreateDbContext();
        var (menuItem, table) = await SeedMenuAndTableAsync(dbContext);
        var service = CreateService(dbContext);
        var order = await service.CreateAsync(
            new CreateOrderRequest(table.Id, new[] { new CreateOrderItemRequest(menuItem.Id, 1) }),
            CancellationToken.None);
        await service.UpdateStatusAsync(order.Id, new UpdateOrderStatusRequest(OrderStatus.Preparing), CancellationToken.None);
        await service.UpdateStatusAsync(order.Id, new UpdateOrderStatusRequest(OrderStatus.Ready), CancellationToken.None);

        var error = await Assert.ThrowsAsync<AppException>(() => service.CancelAsync(order.Id, CancellationToken.None));

        Assert.Equal(409, error.StatusCode);
    }

    private static OpsBoardDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<OpsBoardDbContext>()
            .UseInMemoryDatabase($"order-service-tests-{Guid.NewGuid()}")
            .Options;
        return new OpsBoardDbContext(options);
    }

    private static async Task<(MenuItem MenuItem, RestaurantTable Table)> SeedMenuAndTableAsync(
        OpsBoardDbContext dbContext,
        TableStatus tableStatus = TableStatus.Available)
    {
        var user = new AppUser("Staff", "staff@opsboard.local", "hash", UserRole.Staff);
        typeof(AppUser).GetProperty(nameof(AppUser.Id))!.SetValue(user, StaffUserId);
        var category = new MenuCategory("Main", 1);
        var menuItem = new MenuItem(category.Id, "Pho", "Beef noodle soup", 55000m);
        var table = new RestaurantTable("Table 1", tableStatus);

        dbContext.Users.Add(user);
        dbContext.MenuCategories.Add(category);
        dbContext.MenuItems.Add(menuItem);
        dbContext.RestaurantTables.Add(table);
        await dbContext.SaveChangesAsync();

        return (menuItem, table);
    }

    private static OrderService CreateService(OpsBoardDbContext dbContext)
    {
        var clock = new FixedClock(FixedNow);
        return new OrderService(
            dbContext,
            new AuditService(dbContext, clock),
            new TestCurrentUser(StaffUserId),
            clock);
    }

    private sealed class TestCurrentUser : ICurrentUser
    {
        public TestCurrentUser(Guid userId)
        {
            UserId = userId;
        }

        public Guid? UserId { get; }
        public string? Email => "staff@opsboard.local";
        public string? FullName => "Staff";
        public UserRole? Role => UserRole.Staff;
    }

    private sealed class FixedClock : ISystemClock
    {
        public FixedClock(DateTimeOffset utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTimeOffset UtcNow { get; }
    }
}

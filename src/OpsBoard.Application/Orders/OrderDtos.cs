using OpsBoard.Domain.Orders;

namespace OpsBoard.Application.Orders;

public sealed record OrderSummaryResponse(
    Guid Id,
    Guid TableId,
    string TableName,
    OrderStatus Status,
    decimal Subtotal,
    decimal Total,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? PaidAtUtc);

public sealed record OrderDetailResponse(
    Guid Id,
    Guid TableId,
    string TableName,
    OrderStatus Status,
    decimal Subtotal,
    decimal Total,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? PaidAtUtc,
    IReadOnlyList<OrderItemResponse> Items);

public sealed record OrderItemResponse(
    Guid Id,
    Guid MenuItemId,
    string ItemNameSnapshot,
    decimal UnitPriceSnapshot,
    int Quantity,
    decimal LineTotal);

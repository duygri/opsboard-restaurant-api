namespace OpsBoard.Application.Orders;

public sealed record CreateOrderRequest(Guid TableId, IReadOnlyList<CreateOrderItemRequest> Items);

public sealed record CreateOrderItemRequest(Guid MenuItemId, int Quantity);

namespace OpsBoard.Domain.Orders;

public sealed record OrderItemDraft(Guid MenuItemId, string ItemName, decimal UnitPrice, int Quantity);

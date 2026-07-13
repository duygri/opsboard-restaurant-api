namespace OpsBoard.Domain.Orders;

public sealed class OrderItem
{
    private OrderItem()
    {
        ItemNameSnapshot = string.Empty;
    }

    private OrderItem(Guid menuItemId, string itemNameSnapshot, decimal unitPriceSnapshot, int quantity)
    {
        Id = Guid.NewGuid();
        MenuItemId = menuItemId;
        ItemNameSnapshot = itemNameSnapshot;
        UnitPriceSnapshot = unitPriceSnapshot;
        Quantity = quantity;
        LineTotal = unitPriceSnapshot * quantity;
    }

    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid MenuItemId { get; private set; }
    public string ItemNameSnapshot { get; private set; }
    public decimal UnitPriceSnapshot { get; private set; }
    public int Quantity { get; private set; }
    public decimal LineTotal { get; private set; }

    public static OrderItem CreateSnapshot(OrderItemDraft draft)
    {
        if (draft.Quantity <= 0)
        {
            throw new InvalidOperationException("Order item quantity must be positive.");
        }

        if (string.IsNullOrWhiteSpace(draft.ItemName))
        {
            throw new InvalidOperationException("Order item name is required.");
        }

        if (draft.UnitPrice < 0)
        {
            throw new InvalidOperationException("Order item price cannot be negative.");
        }

        return new OrderItem(draft.MenuItemId, draft.ItemName, draft.UnitPrice, draft.Quantity);
    }
}

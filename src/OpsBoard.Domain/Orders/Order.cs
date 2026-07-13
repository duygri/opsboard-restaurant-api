namespace OpsBoard.Domain.Orders;

public sealed class Order
{
    private readonly List<OrderItem> _items = new();

    private Order()
    {
    }

    private Order(Guid tableId, Guid createdByUserId, DateTimeOffset createdAtUtc)
    {
        Id = Guid.NewGuid();
        TableId = tableId;
        CreatedByUserId = createdByUserId;
        Status = OrderStatus.New;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid TableId { get; private set; }
    public OrderStatus Status { get; private set; }
    public decimal Subtotal { get; private set; }
    public decimal Total { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public DateTimeOffset? PaidAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    public static Order Create(
        Guid tableId,
        Guid createdByUserId,
        IReadOnlyCollection<OrderItemDraft> itemDrafts,
        DateTimeOffset createdAtUtc)
    {
        if (itemDrafts.Count == 0)
        {
            throw new InvalidOperationException("An order must contain at least one order item.");
        }

        var order = new Order(tableId, createdByUserId, createdAtUtc);
        foreach (var draft in itemDrafts)
        {
            order._items.Add(OrderItem.CreateSnapshot(draft));
        }

        order.RecalculateTotals();
        return order;
    }

    public void ChangeStatus(OrderStatus targetStatus, DateTimeOffset changedAtUtc)
    {
        if (!IsValidTransition(Status, targetStatus))
        {
            throw new InvalidOperationException(
                $"Invalid order status transition from {Status} to {targetStatus}.");
        }

        Status = targetStatus;
        UpdatedAtUtc = changedAtUtc;

        if (targetStatus == OrderStatus.Paid)
        {
            PaidAtUtc = changedAtUtc;
        }
    }

    public void Cancel(DateTimeOffset cancelledAtUtc)
    {
        if (Status is not (OrderStatus.New or OrderStatus.Preparing))
        {
            throw new InvalidOperationException("Only New or Preparing orders can be cancelled.");
        }

        Status = OrderStatus.Cancelled;
        UpdatedAtUtc = cancelledAtUtc;
    }

    private void RecalculateTotals()
    {
        Subtotal = _items.Sum(item => item.LineTotal);
        Total = Subtotal;
    }

    private static bool IsValidTransition(OrderStatus current, OrderStatus target)
    {
        return (current, target) switch
        {
            (OrderStatus.New, OrderStatus.Preparing) => true,
            (OrderStatus.Preparing, OrderStatus.Ready) => true,
            (OrderStatus.Ready, OrderStatus.Served) => true,
            (OrderStatus.Served, OrderStatus.Paid) => true,
            (OrderStatus.New, OrderStatus.Cancelled) => true,
            (OrderStatus.Preparing, OrderStatus.Cancelled) => true,
            _ => false
        };
    }
}

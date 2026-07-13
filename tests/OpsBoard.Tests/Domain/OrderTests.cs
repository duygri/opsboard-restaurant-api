using OpsBoard.Domain.Orders;

namespace OpsBoard.Tests.Domain;

public sealed class OrderTests
{
    [Fact]
    public void ChangeStatus_AllowsHappyPathToPaid()
    {
        var order = CreateOrder();

        order.ChangeStatus(OrderStatus.Preparing, Now());
        order.ChangeStatus(OrderStatus.Ready, Now());
        order.ChangeStatus(OrderStatus.Served, Now());
        order.ChangeStatus(OrderStatus.Paid, Now());

        Assert.Equal(OrderStatus.Paid, order.Status);
        Assert.NotNull(order.PaidAtUtc);
    }

    [Fact]
    public void ChangeStatus_RejectsSkippedTransition()
    {
        var order = CreateOrder();

        var error = Assert.Throws<InvalidOperationException>(() =>
            order.ChangeStatus(OrderStatus.Ready, Now()));

        Assert.Contains("Invalid order status transition", error.Message);
    }

    [Fact]
    public void Cancel_AllowsNewOrder()
    {
        var order = CreateOrder();

        order.Cancel(Now());

        Assert.Equal(OrderStatus.Cancelled, order.Status);
    }

    [Fact]
    public void Cancel_RejectsReadyOrder()
    {
        var order = CreateOrder();
        order.ChangeStatus(OrderStatus.Preparing, Now());
        order.ChangeStatus(OrderStatus.Ready, Now());

        var error = Assert.Throws<InvalidOperationException>(() => order.Cancel(Now()));

        Assert.Contains("Only New or Preparing orders can be cancelled", error.Message);
    }

    [Fact]
    public void Create_RejectsEmptyOrder()
    {
        var error = Assert.Throws<InvalidOperationException>(() =>
            Order.Create(Guid.NewGuid(), Guid.NewGuid(), Array.Empty<OrderItemDraft>(), Now()));

        Assert.Contains("at least one", error.Message);
    }

    [Fact]
    public void Create_RejectsNonPositiveQuantity()
    {
        var item = new OrderItemDraft(Guid.NewGuid(), "Pho", 55000m, 0);

        var error = Assert.Throws<InvalidOperationException>(() =>
            Order.Create(Guid.NewGuid(), Guid.NewGuid(), new[] { item }, Now()));

        Assert.Contains("positive", error.Message);
    }

    [Fact]
    public void Create_StoresItemSnapshotsAndCalculatesTotals()
    {
        var item = new OrderItemDraft(Guid.NewGuid(), "Pho", 55000m, 2);

        var order = Order.Create(Guid.NewGuid(), Guid.NewGuid(), new[] { item }, Now());

        Assert.Equal(110000m, order.Subtotal);
        Assert.Equal(order.Subtotal, order.Total);
        var orderItem = Assert.Single(order.Items);
        Assert.Equal("Pho", orderItem.ItemNameSnapshot);
        Assert.Equal(55000m, orderItem.UnitPriceSnapshot);
        Assert.Equal(2, orderItem.Quantity);
        Assert.Equal(110000m, orderItem.LineTotal);
    }

    private static Order CreateOrder()
    {
        var item = new OrderItemDraft(Guid.NewGuid(), "Pho", 55000m, 1);
        return Order.Create(Guid.NewGuid(), Guid.NewGuid(), new[] { item }, Now());
    }

    private static DateTimeOffset Now() => new(2026, 7, 14, 10, 0, 0, TimeSpan.Zero);
}

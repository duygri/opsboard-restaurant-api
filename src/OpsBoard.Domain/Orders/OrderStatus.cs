namespace OpsBoard.Domain.Orders;

public enum OrderStatus
{
    New,
    Preparing,
    Ready,
    Served,
    Paid,
    Cancelled
}

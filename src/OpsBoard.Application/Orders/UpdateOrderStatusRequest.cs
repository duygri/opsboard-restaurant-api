using OpsBoard.Domain.Orders;

namespace OpsBoard.Application.Orders;

public sealed record UpdateOrderStatusRequest(OrderStatus TargetStatus);

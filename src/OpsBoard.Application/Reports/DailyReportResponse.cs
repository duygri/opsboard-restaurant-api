using OpsBoard.Domain.Orders;

namespace OpsBoard.Application.Reports;

public sealed record DailyReportResponse(
    DateOnly Date,
    string TimeZone,
    decimal Revenue,
    int PaidOrderCount,
    IReadOnlyList<OrderStatusCountResponse> StatusCounts);

public sealed record OrderStatusCountResponse(OrderStatus Status, int Count);

using Microsoft.EntityFrameworkCore;
using OpsBoard.Application.Reports;
using OpsBoard.Domain.Orders;
using OpsBoard.Infrastructure.Persistence;

namespace OpsBoard.Infrastructure.Services;

public sealed class ReportingService
{
    public const string ReportTimeZoneId = "Asia/Ho_Chi_Minh";

    private readonly OpsBoardDbContext _dbContext;

    public ReportingService(OpsBoardDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DailyReportResponse> GetDailyAsync(DateOnly date, CancellationToken cancellationToken)
    {
        var timeZone = GetHoChiMinhTimeZone();
        var localStart = date.ToDateTime(TimeOnly.MinValue);
        var localEnd = localStart.AddDays(1);
        var utcStart = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(localStart, timeZone));
        var utcEnd = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(localEnd, timeZone));

        var paidOrders = await _dbContext.Orders
            .Where(order => order.Status == OrderStatus.Paid
                && order.PaidAtUtc >= utcStart
                && order.PaidAtUtc < utcEnd)
            .ToArrayAsync(cancellationToken);

        var ordersCreatedInDay = await _dbContext.Orders
            .Where(order => order.CreatedAtUtc >= utcStart && order.CreatedAtUtc < utcEnd)
            .Select(order => order.Status)
            .ToArrayAsync(cancellationToken);

        var statusCounts = ordersCreatedInDay
            .GroupBy(status => status)
            .Select(group => new OrderStatusCountResponse(group.Key, group.Count()))
            .OrderBy(count => count.Status)
            .ToArray();

        return new DailyReportResponse(
            date,
            ReportTimeZoneId,
            paidOrders.Sum(order => order.Total),
            paidOrders.Length,
            statusCounts);
    }

    private static TimeZoneInfo GetHoChiMinhTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(ReportTimeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        }
        catch (InvalidTimeZoneException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        }
    }
}

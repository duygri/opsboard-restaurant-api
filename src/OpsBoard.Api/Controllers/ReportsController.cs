using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpsBoard.Api.Auth;
using OpsBoard.Infrastructure.Services;

namespace OpsBoard.Api.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize(Policy = AuthorizationPolicies.ReportsView)]
public sealed class ReportsController : ApiControllerBase
{
    private readonly ReportingService _reportingService;

    public ReportsController(ReportingService reportingService)
    {
        _reportingService = reportingService;
    }

    [HttpGet("daily")]
    public async Task<IActionResult> GetDaily([FromQuery] DateOnly? date, CancellationToken cancellationToken)
    {
        var reportDate = date ?? DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)).DateTime);
        var response = await _reportingService.GetDailyAsync(reportDate, cancellationToken);
        return JsonOk(response);
    }
}

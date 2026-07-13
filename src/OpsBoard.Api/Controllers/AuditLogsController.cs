using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpsBoard.Api.Auth;
using OpsBoard.Infrastructure.Services;

namespace OpsBoard.Api.Controllers;

[ApiController]
[Route("api/audit-logs")]
[Authorize(Policy = AuthorizationPolicies.AuditLogsView)]
public sealed class AuditLogsController : ControllerBase
{
    private readonly AuditLogQueryService _auditLogQueryService;

    public AuditLogsController(AuditLogQueryService auditLogQueryService)
    {
        _auditLogQueryService = auditLogQueryService;
    }

    [HttpGet]
    public async Task<IActionResult> GetRecent(CancellationToken cancellationToken)
    {
        var response = await _auditLogQueryService.GetRecentAsync(cancellationToken);
        return new ContentResult
        {
            Content = JsonSerializer.Serialize(response, JsonSerializerOptions.Web),
            ContentType = "application/json",
            StatusCode = StatusCodes.Status200OK
        };
    }
}

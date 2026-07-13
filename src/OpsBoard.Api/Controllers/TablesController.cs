using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpsBoard.Infrastructure.Services;

namespace OpsBoard.Api.Controllers;

[ApiController]
[Route("api/tables")]
[Authorize]
public sealed class TablesController : ApiControllerBase
{
    private readonly TableQueryService _tableQueryService;

    public TablesController(TableQueryService tableQueryService)
    {
        _tableQueryService = tableQueryService;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var response = await _tableQueryService.GetTablesAsync(cancellationToken);
        return JsonOk(response);
    }
}

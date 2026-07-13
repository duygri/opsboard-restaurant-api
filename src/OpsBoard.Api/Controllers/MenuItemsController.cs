using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpsBoard.Infrastructure.Services;

namespace OpsBoard.Api.Controllers;

[ApiController]
[Route("api/menu-items")]
[Authorize]
public sealed class MenuItemsController : ApiControllerBase
{
    private readonly MenuQueryService _menuQueryService;

    public MenuItemsController(MenuQueryService menuQueryService)
    {
        _menuQueryService = menuQueryService;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var response = await _menuQueryService.GetItemsAsync(cancellationToken);
        return JsonOk(response);
    }
}

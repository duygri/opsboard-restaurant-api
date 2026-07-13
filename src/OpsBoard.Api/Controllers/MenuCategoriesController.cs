using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpsBoard.Infrastructure.Services;

namespace OpsBoard.Api.Controllers;

[ApiController]
[Route("api/menu-categories")]
[Authorize]
public sealed class MenuCategoriesController : ApiControllerBase
{
    private readonly MenuQueryService _menuQueryService;

    public MenuCategoriesController(MenuQueryService menuQueryService)
    {
        _menuQueryService = menuQueryService;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var response = await _menuQueryService.GetCategoriesAsync(cancellationToken);
        return JsonOk(response);
    }
}

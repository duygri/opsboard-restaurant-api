using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpsBoard.Application.Auth;
using OpsBoard.Application.Common;
using System.Text.Json;

namespace OpsBoard.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _authService.LoginAsync(request, cancellationToken);
            return JsonContent(response);
        }
        catch (AppException exception) when (exception.StatusCode == StatusCodes.Status401Unauthorized)
        {
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            return new EmptyResult();
        }
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var response = await _authService.GetCurrentUserAsync(cancellationToken);
        return JsonContent(response);
    }

    private static ContentResult JsonContent<T>(T value)
    {
        return new ContentResult
        {
            Content = JsonSerializer.Serialize(value, JsonSerializerOptions.Web),
            ContentType = "application/json",
            StatusCode = StatusCodes.Status200OK
        };
    }
}

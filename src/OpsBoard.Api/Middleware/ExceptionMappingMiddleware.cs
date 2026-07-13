using System.Text.Json;
using OpsBoard.Application.Common;

namespace OpsBoard.Api.Middleware;

public sealed class ExceptionMappingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMappingMiddleware> _logger;

    public ExceptionMappingMiddleware(RequestDelegate next, ILogger<ExceptionMappingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (AppException exception)
        {
            await WriteProblemAsync(context, exception.StatusCode, exception.ErrorCode, exception.Message);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unhandled API exception.");
            await WriteProblemAsync(
                context,
                StatusCodes.Status500InternalServerError,
                "internal_server_error",
                "An unexpected error occurred.");
        }
    }

    private static async Task WriteProblemAsync(
        HttpContext context,
        int statusCode,
        string errorCode,
        string detail)
    {
        if (context.Response.HasStarted)
        {
            throw new InvalidOperationException("Cannot write problem response after the response has started.");
        }

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var body = new
        {
            type = $"https://httpstatuses.com/{statusCode}",
            title = ReasonPhrases.GetTitle(statusCode),
            status = statusCode,
            detail,
            errorCode
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(body, JsonSerializerOptions.Web));
    }

    private static class ReasonPhrases
    {
        public static string GetTitle(int statusCode)
        {
            return statusCode switch
            {
                StatusCodes.Status400BadRequest => "Bad Request",
                StatusCodes.Status401Unauthorized => "Unauthorized",
                StatusCodes.Status403Forbidden => "Forbidden",
                StatusCodes.Status404NotFound => "Not Found",
                StatusCodes.Status409Conflict => "Conflict",
                _ => "Internal Server Error"
            };
        }
    }
}

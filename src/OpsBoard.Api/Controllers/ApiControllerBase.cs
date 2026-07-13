using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using OpsBoard.Application.Common;

namespace OpsBoard.Api.Controllers;

public abstract class ApiControllerBase : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    protected ContentResult JsonOk<T>(T value)
    {
        return Json(value, StatusCodes.Status200OK);
    }

    protected ContentResult JsonCreated<T>(T value, string location)
    {
        Response.Headers.Location = location;
        return Json(value, StatusCodes.Status201Created);
    }

    protected ContentResult JsonError(AppException exception)
    {
        return Json(
            new
            {
                errorCode = exception.ErrorCode,
                message = exception.Message
            },
            exception.StatusCode);
    }

    protected ContentResult Json<T>(T value, int statusCode)
    {
        return new ContentResult
        {
            Content = JsonSerializer.Serialize(value, JsonOptions),
            ContentType = "application/json",
            StatusCode = statusCode
        };
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerOptions.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}

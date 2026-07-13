namespace OpsBoard.Application.Common;

public sealed class AppException : Exception
{
    public AppException(int statusCode, string errorCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
    }

    public int StatusCode { get; }
    public string ErrorCode { get; }

    public static AppException BadRequest(string message, string errorCode = ErrorCodes.ValidationFailed)
        => new(400, errorCode, message);

    public static AppException Unauthorized(string message = "Authentication is required.")
        => new(401, ErrorCodes.Unauthorized, message);

    public static AppException Forbidden(string message = "You do not have permission to perform this action.")
        => new(403, ErrorCodes.Forbidden, message);

    public static AppException NotFound(string message)
        => new(404, ErrorCodes.NotFound, message);

    public static AppException Conflict(string message, string errorCode = ErrorCodes.Conflict)
        => new(409, errorCode, message);
}

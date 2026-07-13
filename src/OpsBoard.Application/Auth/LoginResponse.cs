using OpsBoard.Domain.Users;

namespace OpsBoard.Application.Auth;

public sealed record LoginResponse(
    string AccessToken,
    DateTimeOffset ExpiresAtUtc,
    CurrentUserResponse User);

public sealed record CurrentUserResponse(
    Guid Id,
    string FullName,
    string Email,
    UserRole Role);

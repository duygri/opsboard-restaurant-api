using OpsBoard.Domain.Users;

namespace OpsBoard.Application.Abstractions;

public interface IJwtTokenService
{
    JwtTokenResult CreateToken(AppUser user);
}

public sealed record JwtTokenResult(string AccessToken, DateTimeOffset ExpiresAtUtc);

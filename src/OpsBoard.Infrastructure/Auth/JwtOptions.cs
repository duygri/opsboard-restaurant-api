namespace OpsBoard.Infrastructure.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = "OpsBoard";
    public string Audience { get; init; } = "OpsBoard";
    public string SigningKey { get; init; } = "dev-only-signing-key-change-me-dev-only-signing-key";
    public int ExpirationMinutes { get; init; } = 120;
}

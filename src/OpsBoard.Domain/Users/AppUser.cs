namespace OpsBoard.Domain.Users;

public sealed class AppUser
{
    private AppUser()
    {
        FullName = string.Empty;
        Email = string.Empty;
        PasswordHash = string.Empty;
    }

    public AppUser(string fullName, string email, string passwordHash, UserRole role)
    {
        Id = Guid.NewGuid();
        FullName = fullName;
        Email = email;
        PasswordHash = passwordHash;
        Role = role;
        IsActive = true;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }
    public string FullName { get; private set; }
    public string Email { get; private set; }
    public string PasswordHash { get; private set; }
    public UserRole Role { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
}

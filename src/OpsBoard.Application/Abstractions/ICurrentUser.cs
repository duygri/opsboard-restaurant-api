using OpsBoard.Domain.Users;

namespace OpsBoard.Application.Abstractions;

public interface ICurrentUser
{
    Guid? UserId { get; }
    string? Email { get; }
    string? FullName { get; }
    UserRole? Role { get; }
}

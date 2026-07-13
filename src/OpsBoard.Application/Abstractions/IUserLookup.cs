using OpsBoard.Domain.Users;

namespace OpsBoard.Application.Abstractions;

public interface IUserLookup
{
    Task<AppUser?> FindActiveByEmailAsync(string email, CancellationToken cancellationToken);
    Task<AppUser?> FindActiveByIdAsync(Guid id, CancellationToken cancellationToken);
}

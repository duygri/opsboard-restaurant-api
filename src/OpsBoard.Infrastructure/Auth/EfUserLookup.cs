using Microsoft.EntityFrameworkCore;
using OpsBoard.Application.Abstractions;
using OpsBoard.Domain.Users;
using OpsBoard.Infrastructure.Persistence;

namespace OpsBoard.Infrastructure.Auth;

public sealed class EfUserLookup : IUserLookup
{
    private readonly OpsBoardDbContext _dbContext;

    public EfUserLookup(OpsBoardDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<AppUser?> FindActiveByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        return _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.IsActive && user.Email.ToLower() == normalizedEmail, cancellationToken);
    }

    public Task<AppUser?> FindActiveByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.IsActive && user.Id == id, cancellationToken);
    }
}

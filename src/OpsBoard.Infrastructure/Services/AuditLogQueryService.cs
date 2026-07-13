using Microsoft.EntityFrameworkCore;
using OpsBoard.Application.AuditLogs;
using OpsBoard.Infrastructure.Persistence;

namespace OpsBoard.Infrastructure.Services;

public sealed class AuditLogQueryService
{
    private readonly OpsBoardDbContext _dbContext;

    public AuditLogQueryService(OpsBoardDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<AuditLogResponse>> GetRecentAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.AuditLogs
            .AsNoTracking()
            .OrderByDescending(log => log.CreatedAtUtc)
            .Take(100)
            .Select(log => new AuditLogResponse(
                log.Id,
                log.ActorUserId,
                log.ActorName,
                log.Action,
                log.EntityType,
                log.EntityId,
                log.BeforeJson,
                log.AfterJson,
                log.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }
}

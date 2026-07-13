using System.Text.Json;
using OpsBoard.Application.Abstractions;
using OpsBoard.Domain.Audit;
using OpsBoard.Infrastructure.Persistence;

namespace OpsBoard.Infrastructure.Services;

public sealed class AuditService : IAuditService
{
    private readonly OpsBoardDbContext _dbContext;
    private readonly ISystemClock _clock;

    public AuditService(OpsBoardDbContext dbContext, ISystemClock clock)
    {
        _dbContext = dbContext;
        _clock = clock;
    }

    public async Task RecordAsync(
        Guid? actorUserId,
        string actorName,
        AuditAction action,
        string entityType,
        Guid entityId,
        object? before,
        object? after,
        CancellationToken cancellationToken)
    {
        var auditLog = new AuditLog(
            actorUserId,
            actorName,
            action,
            entityType,
            entityId,
            Serialize(before),
            Serialize(after),
            _clock.UtcNow);

        _dbContext.AuditLogs.Add(auditLog);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string? Serialize(object? value)
    {
        return value is null ? null : JsonSerializer.Serialize(value, JsonSerializerOptions.Web);
    }
}

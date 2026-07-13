using OpsBoard.Domain.Audit;

namespace OpsBoard.Application.Abstractions;

public interface IAuditService
{
    Task RecordAsync(
        Guid? actorUserId,
        string actorName,
        AuditAction action,
        string entityType,
        Guid entityId,
        object? before,
        object? after,
        CancellationToken cancellationToken);
}

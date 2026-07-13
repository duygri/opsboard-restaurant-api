using OpsBoard.Domain.Audit;

namespace OpsBoard.Application.AuditLogs;

public sealed record AuditLogResponse(
    Guid Id,
    Guid? ActorUserId,
    string ActorName,
    AuditAction Action,
    string EntityType,
    Guid EntityId,
    string? BeforeJson,
    string? AfterJson,
    DateTimeOffset CreatedAtUtc);

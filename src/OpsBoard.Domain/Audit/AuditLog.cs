namespace OpsBoard.Domain.Audit;

public sealed class AuditLog
{
    private AuditLog()
    {
        ActorName = string.Empty;
        EntityType = string.Empty;
        BeforeJson = string.Empty;
        AfterJson = string.Empty;
    }

    public AuditLog(
        Guid? actorUserId,
        string actorName,
        AuditAction action,
        string entityType,
        Guid entityId,
        string? beforeJson,
        string? afterJson,
        DateTimeOffset createdAtUtc)
    {
        Id = Guid.NewGuid();
        ActorUserId = actorUserId;
        ActorName = actorName;
        Action = action;
        EntityType = entityType;
        EntityId = entityId;
        BeforeJson = beforeJson;
        AfterJson = afterJson;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid? ActorUserId { get; private set; }
    public string ActorName { get; private set; }
    public AuditAction Action { get; private set; }
    public string EntityType { get; private set; }
    public Guid EntityId { get; private set; }
    public string? BeforeJson { get; private set; }
    public string? AfterJson { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
}

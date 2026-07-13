using OpsBoard.Domain.Tables;

namespace OpsBoard.Application.Tables;

public sealed record TableResponse(Guid Id, string Name, TableStatus Status);

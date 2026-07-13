using OpsBoard.Application.Abstractions;

namespace OpsBoard.Infrastructure.Services;

public sealed class SystemClock : ISystemClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

namespace OpsBoard.Application.Abstractions;

public interface ISystemClock
{
    DateTimeOffset UtcNow { get; }
}

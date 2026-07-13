namespace OpsBoard.Domain.Tables;

public sealed class RestaurantTable
{
    private RestaurantTable()
    {
        Name = string.Empty;
    }

    public RestaurantTable(string name, TableStatus status = TableStatus.Available)
    {
        Id = Guid.NewGuid();
        Name = name;
        Status = status;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public TableStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
}

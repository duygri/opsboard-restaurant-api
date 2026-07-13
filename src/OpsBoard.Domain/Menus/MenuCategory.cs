namespace OpsBoard.Domain.Menus;

public sealed class MenuCategory
{
    private MenuCategory()
    {
        Name = string.Empty;
    }

    public MenuCategory(string name, int displayOrder)
    {
        Id = Guid.NewGuid();
        Name = name;
        DisplayOrder = displayOrder;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public int DisplayOrder { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
}

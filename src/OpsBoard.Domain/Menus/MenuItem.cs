namespace OpsBoard.Domain.Menus;

public sealed class MenuItem
{
    private MenuItem()
    {
        Name = string.Empty;
        Description = string.Empty;
    }

    public MenuItem(Guid categoryId, string name, string description, decimal price)
    {
        Id = Guid.NewGuid();
        CategoryId = categoryId;
        Name = name;
        Description = description;
        Price = price;
        IsAvailable = true;
        IsLowStock = false;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid CategoryId { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public decimal Price { get; private set; }
    public bool IsAvailable { get; private set; }
    public bool IsLowStock { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
}

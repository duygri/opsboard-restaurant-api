namespace OpsBoard.Application.Menus;

public sealed record MenuCategoryResponse(Guid Id, string Name, int DisplayOrder);

public sealed record MenuItemResponse(
    Guid Id,
    Guid CategoryId,
    string Name,
    string Description,
    decimal Price,
    bool IsAvailable,
    bool IsLowStock);

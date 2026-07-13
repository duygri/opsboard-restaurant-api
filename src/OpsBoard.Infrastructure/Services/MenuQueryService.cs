using Microsoft.EntityFrameworkCore;
using OpsBoard.Application.Menus;
using OpsBoard.Infrastructure.Persistence;

namespace OpsBoard.Infrastructure.Services;

public sealed class MenuQueryService
{
    private readonly OpsBoardDbContext _dbContext;

    public MenuQueryService(OpsBoardDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<MenuCategoryResponse>> GetCategoriesAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.MenuCategories
            .OrderBy(category => category.DisplayOrder)
            .ThenBy(category => category.Name)
            .Select(category => new MenuCategoryResponse(category.Id, category.Name, category.DisplayOrder))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MenuItemResponse>> GetItemsAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.MenuItems
            .OrderBy(item => item.CategoryId)
            .ThenBy(item => item.Name)
            .Select(item => new MenuItemResponse(
                item.Id,
                item.CategoryId,
                item.Name,
                item.Description,
                item.Price,
                item.IsAvailable,
                item.IsLowStock))
            .ToArrayAsync(cancellationToken);
    }
}

using Microsoft.EntityFrameworkCore;
using OpsBoard.Application.Abstractions;
using OpsBoard.Domain.Menus;
using OpsBoard.Domain.Tables;
using OpsBoard.Domain.Users;

namespace OpsBoard.Infrastructure.Persistence.Seed;

public sealed class DemoDataSeeder
{
    private readonly OpsBoardDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;

    public DemoDataSeeder(OpsBoardDbContext dbContext, IPasswordHasher passwordHasher)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedUsersAsync(cancellationToken);
        await SeedMenuAsync(cancellationToken);
        await SeedTablesAsync(cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedUsersAsync(CancellationToken cancellationToken)
    {
        if (!await _dbContext.Users.AnyAsync(user => user.Email == "admin@opsboard.local", cancellationToken))
        {
            _dbContext.Users.Add(new AppUser(
                "OpsBoard Admin",
                "admin@opsboard.local",
                _passwordHasher.HashPassword("Admin123!"),
                UserRole.Admin));
        }

        if (!await _dbContext.Users.AnyAsync(user => user.Email == "staff@opsboard.local", cancellationToken))
        {
            _dbContext.Users.Add(new AppUser(
                "OpsBoard Staff",
                "staff@opsboard.local",
                _passwordHasher.HashPassword("Staff123!"),
                UserRole.Staff));
        }
    }

    private async Task SeedMenuAsync(CancellationToken cancellationToken)
    {
        var mains = await GetOrAddCategoryAsync("Main Dishes", 1, cancellationToken);
        var drinks = await GetOrAddCategoryAsync("Drinks", 2, cancellationToken);

        await AddMenuItemIfMissingAsync(mains.Id, "Pho Bo", "Vietnamese beef noodle soup", 55000m, cancellationToken);
        await AddMenuItemIfMissingAsync(mains.Id, "Bun Cha", "Grilled pork with rice noodles", 60000m, cancellationToken);
        await AddMenuItemIfMissingAsync(mains.Id, "Com Tam", "Broken rice with grilled pork", 65000m, cancellationToken);
        await AddMenuItemIfMissingAsync(mains.Id, "Banh Mi", "Vietnamese baguette sandwich", 35000m, cancellationToken);
        await AddMenuItemIfMissingAsync(drinks.Id, "Tra Da", "Iced tea", 5000m, cancellationToken);
        await AddMenuItemIfMissingAsync(drinks.Id, "Ca Phe Sua Da", "Vietnamese iced milk coffee", 30000m, cancellationToken);
    }

    private async Task<MenuCategory> GetOrAddCategoryAsync(string name, int displayOrder, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.MenuCategories
            .FirstOrDefaultAsync(category => category.Name == name, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var category = new MenuCategory(name, displayOrder);
        _dbContext.MenuCategories.Add(category);
        return category;
    }

    private async Task AddMenuItemIfMissingAsync(
        Guid categoryId,
        string name,
        string description,
        decimal price,
        CancellationToken cancellationToken)
    {
        if (await _dbContext.MenuItems.AnyAsync(item => item.Name == name, cancellationToken))
        {
            return;
        }

        _dbContext.MenuItems.Add(new MenuItem(categoryId, name, description, price));
    }

    private async Task SeedTablesAsync(CancellationToken cancellationToken)
    {
        for (var tableNumber = 1; tableNumber <= 6; tableNumber++)
        {
            var tableName = $"Table {tableNumber}";
            if (await _dbContext.RestaurantTables.AnyAsync(table => table.Name == tableName, cancellationToken))
            {
                continue;
            }

            _dbContext.RestaurantTables.Add(new RestaurantTable(tableName));
        }
    }
}

using Microsoft.EntityFrameworkCore;
using OpsBoard.Domain.Audit;
using OpsBoard.Domain.Menus;
using OpsBoard.Domain.Orders;
using OpsBoard.Domain.Tables;
using OpsBoard.Domain.Users;

namespace OpsBoard.Infrastructure.Persistence;

public sealed class OpsBoardDbContext : DbContext
{
    public OpsBoardDbContext(DbContextOptions<OpsBoardDbContext> options)
        : base(options)
    {
    }

    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<MenuCategory> MenuCategories => Set<MenuCategory>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<RestaurantTable> RestaurantTables => Set<RestaurantTable>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OpsBoardDbContext).Assembly);
    }
}

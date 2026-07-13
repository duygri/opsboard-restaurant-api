using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpsBoard.Domain.Menus;
using OpsBoard.Domain.Orders;

namespace OpsBoard.Infrastructure.Persistence.Configurations;

public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("order_items");
        builder.HasKey(item => item.Id);

        builder.Property(item => item.ItemNameSnapshot)
            .HasMaxLength(160)
            .IsRequired();

        builder.Property(item => item.UnitPriceSnapshot)
            .HasPrecision(12, 2)
            .IsRequired();

        builder.Property(item => item.LineTotal)
            .HasPrecision(12, 2)
            .IsRequired();

        builder.HasOne<MenuItem>()
            .WithMany()
            .HasForeignKey(item => item.MenuItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

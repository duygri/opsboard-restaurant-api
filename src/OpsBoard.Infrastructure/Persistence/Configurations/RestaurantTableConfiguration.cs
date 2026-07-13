using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpsBoard.Domain.Tables;

namespace OpsBoard.Infrastructure.Persistence.Configurations;

public sealed class RestaurantTableConfiguration : IEntityTypeConfiguration<RestaurantTable>
{
    public void Configure(EntityTypeBuilder<RestaurantTable> builder)
    {
        builder.ToTable("restaurant_tables");
        builder.HasKey(table => table.Id);

        builder.Property(table => table.Name)
            .HasMaxLength(80)
            .IsRequired();

        builder.HasIndex(table => table.Name)
            .IsUnique();

        builder.Property(table => table.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
    }
}

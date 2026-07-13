using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpsBoard.Domain.Audit;
using OpsBoard.Domain.Users;

namespace OpsBoard.Infrastructure.Persistence.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");
        builder.HasKey(log => log.Id);

        builder.Property(log => log.ActorName)
            .HasMaxLength(160)
            .IsRequired();

        builder.Property(log => log.Action)
            .HasConversion<string>()
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(log => log.EntityType)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(log => log.BeforeJson)
            .HasColumnType("jsonb");

        builder.Property(log => log.AfterJson)
            .HasColumnType("jsonb");

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(log => log.ActorUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

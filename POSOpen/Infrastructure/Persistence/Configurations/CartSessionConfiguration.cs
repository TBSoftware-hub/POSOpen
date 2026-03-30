using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POSOpen.Domain.Entities;
using POSOpen.Infrastructure.Persistence.ValueConverters;

namespace POSOpen.Infrastructure.Persistence.Configurations;

public sealed class CartSessionConfiguration : IEntityTypeConfiguration<CartSession>
{
    public void Configure(EntityTypeBuilder<CartSession> builder)
    {
        builder.ToTable("cart_sessions");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(s => s.FamilyId).HasColumnName("family_id");
        builder.Property(s => s.StaffId).HasColumnName("staff_id").IsRequired();
        builder.Property(s => s.Status).HasColumnName("cart_status").HasConversion<int>().IsRequired();
        builder.Property(s => s.CreatedAtUtc).HasColumnName("created_at_utc").HasConversion(UtcDateTimeConverter.Instance).IsRequired();
        builder.Property(s => s.UpdatedAtUtc).HasColumnName("updated_at_utc").HasConversion(UtcDateTimeConverter.Instance).IsRequired();

        builder.Ignore(s => s.TotalAmountCents);

        builder.HasMany(s => s.LineItems)
            .WithOne()
            .HasForeignKey(i => i.CartSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => new { s.StaffId, s.Status })
            .HasDatabaseName("ix_cart_sessions_staff_status");

        builder.HasIndex(s => s.StaffId)
            .IsUnique()
            .HasDatabaseName("ux_cart_sessions_staff_open")
            .HasFilter("cart_status = 0");
    }
}

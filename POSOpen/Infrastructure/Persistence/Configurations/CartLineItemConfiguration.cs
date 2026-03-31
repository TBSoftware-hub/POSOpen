using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POSOpen.Domain.Entities;
using POSOpen.Infrastructure.Persistence.ValueConverters;

namespace POSOpen.Infrastructure.Persistence.Configurations;

public sealed class CartLineItemConfiguration : IEntityTypeConfiguration<CartLineItem>
{
    public void Configure(EntityTypeBuilder<CartLineItem> builder)
    {
        builder.ToTable("cart_line_items");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(i => i.CartSessionId).HasColumnName("cart_session_id").IsRequired();
        builder.Property(i => i.Description).HasColumnName("description").HasMaxLength(200).IsRequired();
        builder.Property(i => i.FulfillmentContext).HasColumnName("fulfillment_context").HasConversion<int>().IsRequired();
        builder.Property(i => i.ReferenceId).HasColumnName("reference_id");
        builder.Property(i => i.Quantity).HasColumnName("quantity").IsRequired();
        builder.Property(i => i.UnitAmountCents).HasColumnName("unit_amount_cents").IsRequired();
        builder.Property(i => i.CurrencyCode).HasColumnName("currency_code").HasMaxLength(3).IsRequired();
        builder.Property(i => i.CreatedAtUtc).HasColumnName("created_at_utc").HasConversion(UtcDateTimeConverter.Instance).IsRequired();
        builder.Property(i => i.UpdatedAtUtc).HasColumnName("updated_at_utc").HasConversion(UtcDateTimeConverter.Instance).IsRequired();

        builder.Ignore(i => i.LineTotalCents);

        builder.HasIndex(i => i.CartSessionId)
            .HasDatabaseName("ix_cart_line_items_cart_session_id");
    }
}

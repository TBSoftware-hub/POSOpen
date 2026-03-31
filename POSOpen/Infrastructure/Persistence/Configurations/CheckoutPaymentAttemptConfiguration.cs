using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POSOpen.Domain.Entities;
using POSOpen.Infrastructure.Persistence.ValueConverters;

namespace POSOpen.Infrastructure.Persistence.Configurations;

public sealed class CheckoutPaymentAttemptConfiguration : IEntityTypeConfiguration<CheckoutPaymentAttempt>
{
	public void Configure(EntityTypeBuilder<CheckoutPaymentAttempt> builder)
	{
		builder.ToTable("checkout_payment_attempts");
		builder.HasKey(x => x.Id);

		builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
		builder.Property(x => x.CartSessionId).HasColumnName("cart_session_id").IsRequired();
		builder.Property(x => x.AmountCents).HasColumnName("amount_cents").IsRequired();
		builder.Property(x => x.CurrencyCode).HasColumnName("currency_code").HasMaxLength(3).IsRequired();
		builder.Property(x => x.AuthorizationStatus).HasColumnName("authorization_status").HasConversion<int>().IsRequired();
		builder.Property(x => x.ProcessorReference).HasColumnName("processor_reference").HasMaxLength(200);
		builder.Property(x => x.DiagnosticCode).HasColumnName("diagnostic_code").HasMaxLength(100);
		builder.Property(x => x.OccurredAtUtc).HasColumnName("occurred_at_utc").HasConversion(UtcDateTimeConverter.Instance).IsRequired();

		builder.HasOne<CartSession>()
			.WithMany()
			.HasForeignKey(x => x.CartSessionId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasIndex(x => x.CartSessionId)
			.HasDatabaseName("ix_checkout_payment_attempts_cart_session_id");
	}
}
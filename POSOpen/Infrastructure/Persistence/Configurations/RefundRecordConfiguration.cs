using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POSOpen.Domain.Entities;
using POSOpen.Infrastructure.Persistence.ValueConverters;

namespace POSOpen.Infrastructure.Persistence.Configurations;

public sealed class RefundRecordConfiguration : IEntityTypeConfiguration<RefundRecord>
{
	public void Configure(EntityTypeBuilder<RefundRecord> builder)
	{
		builder.ToTable("refund_records");
		builder.HasKey(x => x.Id);

		builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
		builder.Property(x => x.CartSessionId).HasColumnName("cart_session_id").IsRequired();
		builder.Property(x => x.OperationId).HasColumnName("operation_id").IsRequired();
		builder.Property(x => x.Status).HasColumnName("status").HasConversion<int>().IsRequired();
		builder.Property(x => x.Path).HasColumnName("path").HasConversion<int>().IsRequired();
		builder.Property(x => x.AmountCents).HasColumnName("amount_cents").IsRequired();
		builder.Property(x => x.CurrencyCode).HasColumnName("currency_code").HasMaxLength(3).IsRequired();
		builder.Property(x => x.Reason).HasColumnName("reason").HasMaxLength(400).IsRequired();
		builder.Property(x => x.ActorStaffId).HasColumnName("actor_staff_id").IsRequired();
		builder.Property(x => x.ActorRole).HasColumnName("actor_role").HasMaxLength(40).IsRequired();
		builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").HasConversion(UtcDateTimeConverter.Instance).IsRequired();
		builder.Property(x => x.CompletedAtUtc).HasColumnName("completed_at_utc").HasConversion(NullableUtcDateTimeConverter.Instance);

		builder.HasOne<CartSession>()
			.WithMany()
			.HasForeignKey(x => x.CartSessionId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasIndex(x => x.OperationId)
			.IsUnique()
			.HasDatabaseName("ix_refund_records_operation_id");

		builder.HasIndex(x => x.CartSessionId)
			.HasDatabaseName("ix_refund_records_cart_session_id");
	}
}
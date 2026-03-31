using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POSOpen.Domain.Entities;
using POSOpen.Infrastructure.Persistence.ValueConverters;

namespace POSOpen.Infrastructure.Persistence.Configurations;

public sealed class ReceiptMetadataConfiguration : IEntityTypeConfiguration<ReceiptMetadata>
{
	public void Configure(EntityTypeBuilder<ReceiptMetadata> builder)
	{
		builder.ToTable("receipt_metadata");
		builder.HasKey(x => x.Id);

		builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
		builder.Property(x => x.OperationId).HasColumnName("operation_id").IsRequired();
		builder.Property(x => x.TransactionId).HasColumnName("transaction_id").IsRequired();
		builder.Property(x => x.AmountCents).HasColumnName("amount_cents").IsRequired();
		builder.Property(x => x.CurrencyCode).HasColumnName("currency_code").HasMaxLength(3).IsRequired();
		builder.Property(x => x.ItemCount).HasColumnName("item_count").IsRequired();
		builder.Property(x => x.PrintedAtUtc).HasColumnName("printed_at_utc").HasConversion(NullableUtcDateTimeConverter.Instance);
		builder.Property(x => x.PrintStatus).HasColumnName("print_status").HasConversion<int>().IsRequired();
		builder.Property(x => x.DiagnosticCode).HasColumnName("diagnostic_code").HasMaxLength(100);
		builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").HasConversion(UtcDateTimeConverter.Instance).IsRequired();

		builder.HasIndex(x => x.OperationId)
			.IsUnique()
			.HasDatabaseName("ix_receipt_metadata_operation_id");

		builder.HasIndex(x => x.TransactionId)
			.HasDatabaseName("ix_receipt_metadata_transaction_id");
	}
}

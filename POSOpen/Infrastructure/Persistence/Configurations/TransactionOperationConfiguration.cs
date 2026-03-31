using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POSOpen.Domain.Entities;
using POSOpen.Infrastructure.Persistence.ValueConverters;

namespace POSOpen.Infrastructure.Persistence.Configurations;

public sealed class TransactionOperationConfiguration : IEntityTypeConfiguration<TransactionOperation>
{
	public void Configure(EntityTypeBuilder<TransactionOperation> builder)
	{
		builder.ToTable("transaction_operations");
		builder.HasKey(x => x.Id);

		builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
		builder.Property(x => x.OperationId).HasColumnName("operation_id").IsRequired();
		builder.Property(x => x.TransactionId).HasColumnName("transaction_id").HasMaxLength(100).IsRequired();
		builder.Property(x => x.OperationName).HasColumnName("operation_name").HasMaxLength(100).IsRequired();
		builder.Property(x => x.OperationData).HasColumnName("operation_data");
		builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(50).IsRequired();
		builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").HasConversion(UtcDateTimeConverter.Instance).IsRequired();
		builder.Property(x => x.CompletedAtUtc).HasColumnName("completed_at_utc").HasConversion(NullableUtcDateTimeConverter.Instance);

		builder.HasIndex(x => x.OperationId)
			.IsUnique()
			.HasDatabaseName("ix_transaction_operations_operation_id");

		builder.HasIndex(x => x.TransactionId)
			.HasDatabaseName("ix_transaction_operations_transaction_id");
	}
}

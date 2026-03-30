using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POSOpen.Domain.Entities;
using POSOpen.Infrastructure.Persistence.ValueConverters;

namespace POSOpen.Infrastructure.Persistence.Configurations;

public sealed class AdmissionCheckInRecordConfiguration : IEntityTypeConfiguration<AdmissionCheckInRecord>
{
	public void Configure(EntityTypeBuilder<AdmissionCheckInRecord> builder)
	{
		builder.ToTable("admission_check_in_records");
		builder.HasKey(record => record.Id);

		builder.Property(record => record.Id).HasColumnName("id").ValueGeneratedNever();
		builder.Property(record => record.FamilyId).HasColumnName("family_id").IsRequired();
		builder.Property(record => record.OperationId).HasColumnName("operation_id").IsRequired();
		builder.Property(record => record.CompletionStatus).HasColumnName("completion_status").HasMaxLength(32).IsRequired();
		builder.Property(record => record.SettlementStatus).HasColumnName("settlement_status").HasConversion<int>().IsRequired();
		builder.Property(record => record.AmountCents).HasColumnName("amount_cents").IsRequired();
		builder.Property(record => record.CurrencyCode).HasColumnName("currency_code").HasMaxLength(3).IsRequired();
		builder.Property(record => record.CompletedAtUtc).HasColumnName("completed_at_utc").HasConversion(UtcDateTimeConverter.Instance).IsRequired();
		builder.Property(record => record.SettlementDeferredAtUtc).HasColumnName("settlement_deferred_at_utc").HasConversion(NullableUtcDateTimeConverter.Instance);
		builder.Property(record => record.ConfirmationCode).HasColumnName("confirmation_code").HasMaxLength(32).IsRequired();
		builder.Property(record => record.ReceiptReference).HasColumnName("receipt_reference").HasMaxLength(64).IsRequired();

		builder.HasIndex(record => record.OperationId).HasDatabaseName("ix_admission_check_in_records_operation_id").IsUnique();
		builder.HasIndex(record => new { record.FamilyId, record.CompletedAtUtc }).HasDatabaseName("ix_admission_check_in_records_family_completed");
	}
}

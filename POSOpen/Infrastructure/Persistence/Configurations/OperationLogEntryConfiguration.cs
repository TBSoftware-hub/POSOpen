using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POSOpen.Domain.Entities;
using POSOpen.Infrastructure.Persistence.ValueConverters;

namespace POSOpen.Infrastructure.Persistence.Configurations;

public sealed class OperationLogEntryConfiguration : IEntityTypeConfiguration<OperationLogEntry>
{
	public void Configure(EntityTypeBuilder<OperationLogEntry> builder)
	{
		builder.ToTable("OperationLogEntries");
		builder.HasKey(entry => entry.Id);
		builder.Property(entry => entry.Id).ValueGeneratedNever();
		builder.Property(entry => entry.EventId).IsRequired().HasMaxLength(64);
		builder.Property(entry => entry.EventType).IsRequired().HasMaxLength(256);
		builder.Property(entry => entry.AggregateId).IsRequired().HasMaxLength(128);
		builder.Property(entry => entry.PayloadJson).IsRequired();
		builder.Property(entry => entry.Version).IsRequired();
		builder.Property(entry => entry.OccurredUtc).HasConversion(UtcDateTimeConverter.Instance).IsRequired();
		builder.Property(entry => entry.RecordedUtc).HasConversion(UtcDateTimeConverter.Instance).IsRequired();
		builder.HasIndex(entry => entry.EventId).IsUnique();
		builder.HasIndex(entry => new { entry.AggregateId, entry.RecordedUtc });
	}
}
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POSOpen.Domain.Entities;
using POSOpen.Infrastructure.Persistence.ValueConverters;

namespace POSOpen.Infrastructure.Persistence.Configurations;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
	public void Configure(EntityTypeBuilder<OutboxMessage> builder)
	{
		builder.ToTable("OutboxMessages");
		builder.HasKey(message => message.Id);
		builder.Property(message => message.Id).ValueGeneratedNever();
		builder.Property(message => message.MessageId).IsRequired().HasMaxLength(64);
		builder.Property(message => message.EventType).IsRequired().HasMaxLength(256);
		builder.Property(message => message.AggregateId).IsRequired().HasMaxLength(128);
		builder.Property(message => message.ActorStaffId).IsRequired();
		builder.Property(message => message.PayloadJson).IsRequired();
		builder.Property(message => message.OccurredUtc).HasConversion(UtcDateTimeConverter.Instance).IsRequired();
		builder.Property(message => message.EnqueuedUtc).HasConversion(UtcDateTimeConverter.Instance).IsRequired();
		builder.Property(message => message.QueueSequence).HasColumnName("queue_sequence").IsRequired();
		builder.Property(message => message.PublishedUtc).HasConversion(NullableUtcDateTimeConverter.Instance);
		builder.HasIndex(message => message.MessageId).IsUnique();
		builder.HasIndex(message => message.QueueSequence).IsUnique();
		builder.HasIndex(message => new { message.PublishedUtc, message.QueueSequence, message.EnqueuedUtc });
	}
}
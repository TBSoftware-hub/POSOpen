using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POSOpen.Domain.Entities;
using POSOpen.Domain.Enums;
using POSOpen.Infrastructure.Persistence.ValueConverters;

namespace POSOpen.Infrastructure.Persistence.Configurations;

public sealed class PartyBookingConfiguration : IEntityTypeConfiguration<PartyBooking>
{
	public void Configure(EntityTypeBuilder<PartyBooking> builder)
	{
		builder.ToTable("party_bookings");
		builder.HasKey(x => x.Id);

		builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
		builder.Property(x => x.PartyDateUtc).HasColumnName("party_date_utc").HasConversion(UtcDateTimeConverter.Instance).IsRequired();
		builder.Property(x => x.SlotId).HasColumnName("slot_id").HasMaxLength(32).IsRequired();
		builder.Property(x => x.PackageId).HasColumnName("package_id").HasMaxLength(64).IsRequired();
		builder.Property(x => x.Status).HasColumnName("status").HasConversion<int>().IsRequired();
		builder.Property(x => x.OperationId).HasColumnName("operation_id").IsRequired();
		builder.Property(x => x.CorrelationId).HasColumnName("correlation_id").IsRequired();
		builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").HasConversion(UtcDateTimeConverter.Instance).IsRequired();
		builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc").HasConversion(UtcDateTimeConverter.Instance).IsRequired();
		builder.Property(x => x.BookedAtUtc).HasColumnName("booked_at_utc").HasConversion(NullableUtcDateTimeConverter.Instance);

		builder.HasIndex(x => x.OperationId)
			.HasDatabaseName("ix_party_bookings_operation_id");

		builder.HasIndex(x => new { x.PartyDateUtc, x.SlotId, x.Status })
			.HasDatabaseName("ix_party_bookings_party_date_slot_status");

		builder.HasIndex(x => new { x.PartyDateUtc, x.SlotId })
			.IsUnique()
			.HasDatabaseName("ux_party_bookings_active_slot")
			.HasFilter($"status <> {(int)PartyBookingStatus.Cancelled}");
	}
}

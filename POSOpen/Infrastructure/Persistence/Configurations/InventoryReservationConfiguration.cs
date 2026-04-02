using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POSOpen.Domain.Entities;
using POSOpen.Domain.Enums;
using POSOpen.Infrastructure.Persistence.ValueConverters;

namespace POSOpen.Infrastructure.Persistence.Configurations;

public sealed class InventoryReservationConfiguration : IEntityTypeConfiguration<InventoryReservation>
{
	public void Configure(EntityTypeBuilder<InventoryReservation> builder)
	{
		builder.ToTable("inventory_reservations");
		builder.HasKey(x => x.ReservationId);

		builder.Property(x => x.ReservationId).HasColumnName("reservation_id").ValueGeneratedNever();
		builder.Property(x => x.BookingId).HasColumnName("booking_id").IsRequired();
		builder.Property(x => x.OptionId).HasColumnName("option_id").HasMaxLength(64).IsRequired();
		builder.Property(x => x.QuantityReserved).HasColumnName("quantity_reserved").IsRequired();
		builder.Property(x => x.ReservationState).HasColumnName("reservation_state").HasConversion<int>().IsRequired();
		builder.Property(x => x.ReservedAtUtc).HasColumnName("reserved_at_utc").HasConversion(UtcDateTimeConverter.Instance).IsRequired();
		builder.Property(x => x.ReleasedAtUtc).HasColumnName("released_at_utc").HasConversion(NullableUtcDateTimeConverter.Instance);
		builder.Property(x => x.ReservationOperationId).HasColumnName("reservation_operation_id").IsRequired();
		builder.Property(x => x.ReleaseOperationId).HasColumnName("release_operation_id");
		builder.Property(x => x.ReleaseReasonCode).HasColumnName("release_reason_code").HasMaxLength(64);

		builder.HasOne(x => x.Booking)
			.WithMany()
			.HasForeignKey(x => x.BookingId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasIndex(x => x.BookingId)
			.HasDatabaseName("ix_inventory_reservations_booking_id");

		builder.HasIndex(x => new { x.BookingId, x.ReservationState })
			.HasDatabaseName("ix_inventory_reservations_booking_state");

		builder.HasIndex(x => x.ReservationOperationId)
			.HasDatabaseName("ix_inventory_reservations_reservation_operation_id");

		builder.HasIndex(x => x.ReleaseOperationId)
			.HasDatabaseName("ix_inventory_reservations_release_operation_id")
			.HasFilter("release_operation_id IS NOT NULL");

		builder.HasIndex(x => new { x.OptionId, x.ReservationState })
			.HasDatabaseName("ix_inventory_reservations_option_state");
	}
}

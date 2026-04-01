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
		builder.Property(x => x.DepositAmountCents).HasColumnName("deposit_amount_cents");
		builder.Property(x => x.DepositCurrency).HasColumnName("deposit_currency").HasMaxLength(3);
		builder.Property(x => x.DepositCommittedAtUtc).HasColumnName("deposit_committed_at_utc").HasConversion(NullableUtcDateTimeConverter.Instance);
		builder.Property(x => x.DepositCommitmentStatus).HasColumnName("deposit_commitment_status").HasConversion<int>().IsRequired();
		builder.Property(x => x.DepositCommitmentOperationId).HasColumnName("deposit_commitment_operation_id");
		builder.Property(x => x.CompletedAtUtc).HasColumnName("completed_at_utc").HasConversion(NullableUtcDateTimeConverter.Instance);
		builder.Property(x => x.AssignedRoomId).HasColumnName("assigned_room_id").HasMaxLength(64);
		builder.Property(x => x.RoomAssignedAtUtc).HasColumnName("room_assigned_at_utc").HasConversion(NullableUtcDateTimeConverter.Instance);
		builder.Property(x => x.RoomAssignmentOperationId).HasColumnName("room_assignment_operation_id");
		builder.Property(x => x.LastAddOnUpdateOperationId).HasColumnName("last_add_on_update_operation_id");

		builder.HasMany(x => x.AddOnSelections)
			.WithOne(x => x.Booking)
			.HasForeignKey(x => x.BookingId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasIndex(x => x.OperationId)
			.HasDatabaseName("ix_party_bookings_operation_id");

		builder.HasIndex(x => x.DepositCommitmentOperationId)
			.HasDatabaseName("ux_party_bookings_deposit_commitment_operation_id")
			.IsUnique()
			.HasFilter("deposit_commitment_operation_id IS NOT NULL");

		builder.HasIndex(x => new { x.PartyDateUtc, x.SlotId, x.Status })
			.HasDatabaseName("ix_party_bookings_party_date_slot_status");

		builder.HasIndex(x => new { x.Status, x.PartyDateUtc })
			.HasDatabaseName("ix_party_bookings_status_party_date");

		builder.HasIndex(x => new { x.PartyDateUtc, x.SlotId })
			.IsUnique()
			.HasDatabaseName("ux_party_bookings_active_slot")
			.HasFilter($"status <> {(int)PartyBookingStatus.Cancelled}");

		builder.HasIndex(x => new { x.AssignedRoomId, x.PartyDateUtc, x.SlotId })
			.IsUnique()
			.HasDatabaseName("ux_party_bookings_room_date_slot")
			.HasFilter($"assigned_room_id IS NOT NULL AND status <> {(int)PartyBookingStatus.Cancelled}");
	}
}

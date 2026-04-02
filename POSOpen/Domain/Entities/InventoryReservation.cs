using POSOpen.Domain.Enums;

namespace POSOpen.Domain.Entities;

public sealed class InventoryReservation
{
	public Guid ReservationId { get; set; }
	public Guid BookingId { get; set; }
	public string OptionId { get; set; } = string.Empty;
	public int QuantityReserved { get; set; }
	public InventoryReservationState ReservationState { get; set; }
	public DateTime ReservedAtUtc { get; set; }
	public DateTime? ReleasedAtUtc { get; set; }
	public Guid ReservationOperationId { get; set; }
	public Guid? ReleaseOperationId { get; set; }
	public string? ReleaseReasonCode { get; set; }

	public PartyBooking? Booking { get; set; }
}

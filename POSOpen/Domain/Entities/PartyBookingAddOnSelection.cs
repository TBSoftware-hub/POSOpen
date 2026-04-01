using POSOpen.Domain.Enums;

namespace POSOpen.Domain.Entities;

public sealed class PartyBookingAddOnSelection
{
	public Guid Id { get; set; }
	public Guid BookingId { get; set; }
	public PartyAddOnType AddOnType { get; set; }
	public string OptionId { get; set; } = string.Empty;
	public int Quantity { get; set; } = 1;
	public DateTime SelectedAtUtc { get; set; }
	public Guid SelectionOperationId { get; set; }

	public PartyBooking? Booking { get; set; }
}

namespace POSOpen.Application.UseCases.Inventory;

public sealed record InventoryReservationSummaryDto(
	string OptionId,
	int QuantityReserved);

public sealed record InventoryConstraintDto(
	string OptionId,
	int RequiredQuantity,
	int ReservedQuantity,
	int DeficitQuantity);

public sealed record ReserveBookingInventoryResultDto(
	Guid BookingId,
	IReadOnlyList<InventoryReservationSummaryDto> Reservations,
	IReadOnlyList<InventoryConstraintDto> UnresolvedConstraints,
	string NextActionGuidance);

public sealed record ReleaseBookingInventoryResultDto(
	Guid BookingId,
	int ReleasedReservationRowCount,
	IReadOnlyList<InventoryReservationSummaryDto> ActiveReservations);

public sealed record InventoryReleasePersistenceResult(
	int ReleasedReservationRowCount,
	IReadOnlyList<InventoryReservationSummaryDto> ActiveReservations);

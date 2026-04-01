using POSOpen.Application.UseCases.Inventory;
using POSOpen.Domain.Entities;

namespace POSOpen.Application.Abstractions.Repositories;

public interface IInventoryReservationRepository
{
	Task<IReadOnlyList<InventoryReservation>> ListActiveByBookingAsync(Guid bookingId, CancellationToken ct = default);

	Task<IReadOnlyDictionary<string, int>> GetActiveReservedTotalsByOptionAsync(
		IReadOnlyCollection<string> optionIds,
		Guid? excludingBookingId = null,
		CancellationToken ct = default);

	Task<IReadOnlyList<InventoryReservation>> PersistReservationPlanAsync(
		Guid bookingId,
		IReadOnlyDictionary<string, int> reserveQuantitiesByOption,
		Guid operationId,
		Guid correlationId,
		DateTime occurredUtc,
		CancellationToken ct = default);

	Task<InventoryReleasePersistenceResult> ReleaseByTriggerAsync(
		Guid bookingId,
		InventoryReleaseTrigger trigger,
		Guid operationId,
		Guid correlationId,
		DateTime occurredUtc,
		IReadOnlyCollection<string>? removedOptionIds = null,
		IReadOnlyDictionary<string, int>? quantityReductionByOption = null,
		CancellationToken ct = default);
}

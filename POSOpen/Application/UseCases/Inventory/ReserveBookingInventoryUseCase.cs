using Microsoft.Extensions.Logging;
using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.Results;
using POSOpen.Application.UseCases.Party;
using POSOpen.Domain.Enums;
using POSOpen.Shared.Operational;

namespace POSOpen.Application.UseCases.Inventory;

public sealed class ReserveBookingInventoryUseCase
{
	private readonly IPartyBookingRepository _partyBookingRepository;
	private readonly IInventoryReservationRepository _inventoryReservationRepository;
	private readonly ILogger<ReserveBookingInventoryUseCase> _logger;

	public ReserveBookingInventoryUseCase(
		IPartyBookingRepository partyBookingRepository,
		IInventoryReservationRepository inventoryReservationRepository,
		ILogger<ReserveBookingInventoryUseCase> logger)
	{
		_partyBookingRepository = partyBookingRepository;
		_inventoryReservationRepository = inventoryReservationRepository;
		_logger = logger;
	}

	public async Task<AppResult<ReserveBookingInventoryResultDto>> ExecuteAsync(
		ReserveBookingInventoryCommand command,
		CancellationToken ct = default)
	{
		var booking = await _partyBookingRepository.GetByIdWithSelectionsAsync(command.BookingId, ct);
		if (booking is null)
		{
			return AppResult<ReserveBookingInventoryResultDto>.Failure(
				PartyBookingConstants.ErrorBookingNotFound,
				PartyBookingConstants.SafeBookingNotFoundMessage);
		}

		try
		{
			var requiredByOption = booking.AddOnSelections
				.GroupBy(x => x.OptionId, StringComparer.Ordinal)
				.ToDictionary(x => x.Key, x => x.Sum(y => Math.Max(y.Quantity, 0)), StringComparer.Ordinal);

			var optionIds = requiredByOption.Keys.ToArray();
			var totalsByOption = await _inventoryReservationRepository.GetActiveReservedTotalsByOptionAsync(
				optionIds,
				booking.Id,
				ct);

			var reservePlan = new Dictionary<string, int>(StringComparer.Ordinal);
			var constraints = new List<InventoryConstraintDto>();

			foreach (var entry in requiredByOption.OrderBy(x => x.Key, StringComparer.Ordinal))
			{
				var capacity = PartyBookingConstants.InventoryCapacityByOption.TryGetValue(entry.Key, out var configuredCapacity)
					? configuredCapacity
					: PartyBookingConstants.DefaultInventoryCapacity;
				var reservedElsewhere = totalsByOption.TryGetValue(entry.Key, out var alreadyReserved)
					? alreadyReserved
					: 0;
				var available = Math.Max(0, capacity - reservedElsewhere);
				var planned = Math.Min(entry.Value, available);
				reservePlan[entry.Key] = planned;
				if (planned < entry.Value)
				{
					constraints.Add(new InventoryConstraintDto(
						entry.Key,
						entry.Value,
						planned,
						entry.Value - planned));
				}
			}

			var activeReservations = await _inventoryReservationRepository.PersistReservationPlanAsync(
				booking.Id,
				reservePlan,
				command.Context.OperationId,
				command.Context.CorrelationId,
				DateTime.SpecifyKind(command.Context.OccurredUtc, DateTimeKind.Utc),
				ct);

			var result = new ReserveBookingInventoryResultDto(
				booking.Id,
				activeReservations
					.GroupBy(x => x.OptionId, StringComparer.Ordinal)
					.OrderBy(x => x.Key, StringComparer.Ordinal)
					.Select(x => new InventoryReservationSummaryDto(x.Key, x.Sum(y => y.QuantityReserved)))
					.ToArray(),
				constraints,
				constraints.Count == 0
					? PartyBookingConstants.InventoryReservationSatisfiedMessage
					: PartyBookingConstants.InventoryConstraintGuidanceMessage);

			return AppResult<ReserveBookingInventoryResultDto>.Success(
				result,
				constraints.Count == 0
					? PartyBookingConstants.InventoryReservationSavedMessage
					: PartyBookingConstants.InventoryReservationSavedWithConstraintsMessage);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to reserve inventory for booking {BookingId}", command.BookingId);
			return AppResult<ReserveBookingInventoryResultDto>.Failure(
				PartyBookingConstants.ErrorInventoryReservationFailed,
				PartyBookingConstants.SafeInventoryReservationFailedMessage);
		}
	}

	public async Task<IReadOnlyList<InventoryConstraintDto>> EvaluateConstraintsAsync(Guid bookingId, CancellationToken ct = default)
	{
		var booking = await _partyBookingRepository.GetByIdWithSelectionsAsync(bookingId, ct);
		if (booking is null)
		{
			return [];
		}

		var requiredByOption = booking.AddOnSelections
			.GroupBy(x => x.OptionId, StringComparer.Ordinal)
			.ToDictionary(x => x.Key, x => x.Sum(y => Math.Max(y.Quantity, 0)), StringComparer.Ordinal);

		var activeForBooking = await _inventoryReservationRepository.ListActiveByBookingAsync(bookingId, ct);
		var reservedByOption = activeForBooking
			.GroupBy(x => x.OptionId, StringComparer.Ordinal)
			.ToDictionary(x => x.Key, x => x.Sum(y => y.QuantityReserved), StringComparer.Ordinal);

		var constraints = new List<InventoryConstraintDto>();
		foreach (var required in requiredByOption.OrderBy(x => x.Key, StringComparer.Ordinal))
		{
			var reserved = reservedByOption.TryGetValue(required.Key, out var quantity) ? quantity : 0;
			if (reserved < required.Value)
			{
				constraints.Add(new InventoryConstraintDto(required.Key, required.Value, reserved, required.Value - reserved));
			}
		}

		return constraints;
	}
}

public sealed record ReserveBookingInventoryCommand(
	Guid BookingId,
	OperationContext Context);

using Microsoft.Extensions.Logging;
using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.Results;
using POSOpen.Application.UseCases.Party;
using POSOpen.Domain.Enums;
using POSOpen.Shared.Operational;

namespace POSOpen.Application.UseCases.Inventory;

public enum InventoryReleaseTrigger
{
	BookingCancelled = 0,
	BookingItemRemoved = 1,
	BookingItemQuantityReduced = 2,
	BookingDateOrSlotChanged = 3,
	BookingUpdatedNonInventoryFields = 4,
}

public sealed class ReleaseBookingInventoryUseCase
{
	private readonly IPartyBookingRepository _partyBookingRepository;
	private readonly IInventoryReservationRepository _inventoryReservationRepository;
	private readonly ReserveBookingInventoryUseCase _reserveBookingInventoryUseCase;
	private readonly ILogger<ReleaseBookingInventoryUseCase> _logger;

	public ReleaseBookingInventoryUseCase(
		IPartyBookingRepository partyBookingRepository,
		IInventoryReservationRepository inventoryReservationRepository,
		ReserveBookingInventoryUseCase reserveBookingInventoryUseCase,
		ILogger<ReleaseBookingInventoryUseCase> logger)
	{
		_partyBookingRepository = partyBookingRepository;
		_inventoryReservationRepository = inventoryReservationRepository;
		_reserveBookingInventoryUseCase = reserveBookingInventoryUseCase;
		_logger = logger;
	}

	public async Task<AppResult<ReleaseBookingInventoryResultDto>> ExecuteAsync(
		ReleaseBookingInventoryCommand command,
		CancellationToken ct = default)
	{
		var booking = await _partyBookingRepository.GetByIdWithSelectionsAsync(command.BookingId, ct);
		if (booking is null)
		{
			return AppResult<ReleaseBookingInventoryResultDto>.Failure(
				PartyBookingConstants.ErrorBookingNotFound,
				PartyBookingConstants.SafeBookingNotFoundMessage);
		}

		try
		{
			var releasedCount = 0;
			IReadOnlyList<InventoryReservationSummaryDto> active = [];
			var normalizedTriggers = NormalizeTriggers(command.ReleaseTriggers);

			foreach (var trigger in normalizedTriggers)
			{
				var released = await _inventoryReservationRepository.ReleaseByTriggerAsync(
					command.BookingId,
					trigger,
					command.Context.OperationId,
					command.Context.CorrelationId,
					DateTime.SpecifyKind(command.Context.OccurredUtc, DateTimeKind.Utc),
					command.RemovedOptionIds,
					command.QuantityReductionByOption,
					ct);

				releasedCount += released.ReleasedReservationRowCount;
				active = released.ActiveReservations;

				if (trigger == InventoryReleaseTrigger.BookingDateOrSlotChanged)
				{
					var reserveResult = await _reserveBookingInventoryUseCase.ExecuteAsync(
						new ReserveBookingInventoryCommand(command.BookingId, command.Context),
						ct);

					if (reserveResult.IsSuccess && reserveResult.Payload is not null)
					{
						active = reserveResult.Payload.Reservations;
					}
				}
			}

			return AppResult<ReleaseBookingInventoryResultDto>.Success(
				new ReleaseBookingInventoryResultDto(command.BookingId, releasedCount, active),
				PartyBookingConstants.InventoryReleaseAppliedMessage);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to release inventory for booking {BookingId}", command.BookingId);
			return AppResult<ReleaseBookingInventoryResultDto>.Failure(
				PartyBookingConstants.ErrorInventoryReleaseFailed,
				PartyBookingConstants.SafeInventoryReleaseFailedMessage);
		}
	}

	private static IReadOnlyList<InventoryReleaseTrigger> NormalizeTriggers(IReadOnlyList<InventoryReleaseTrigger> triggers)
	{
		if (triggers.Count == 0)
		{
			return [];
		}

		var requested = new HashSet<InventoryReleaseTrigger>(triggers);
		InventoryReleaseTrigger[] policyOrder =
		[
			InventoryReleaseTrigger.BookingItemRemoved,
			InventoryReleaseTrigger.BookingItemQuantityReduced,
			InventoryReleaseTrigger.BookingDateOrSlotChanged,
			InventoryReleaseTrigger.BookingCancelled,
			InventoryReleaseTrigger.BookingUpdatedNonInventoryFields,
		];

		return policyOrder
			.Where(requested.Contains)
			.ToArray();
	}
}

public sealed record ReleaseBookingInventoryCommand(
	Guid BookingId,
	IReadOnlyList<InventoryReleaseTrigger> ReleaseTriggers,
	OperationContext Context,
	IReadOnlyCollection<string>? RemovedOptionIds = null,
	IReadOnlyDictionary<string, int>? QuantityReductionByOption = null);

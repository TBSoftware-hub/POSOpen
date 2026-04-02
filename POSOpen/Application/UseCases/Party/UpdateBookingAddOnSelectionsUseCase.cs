using Microsoft.Extensions.Logging;
using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.Results;
using POSOpen.Application.UseCases.Inventory;
using POSOpen.Domain.Entities;
using POSOpen.Domain.Enums;

namespace POSOpen.Application.UseCases.Party;

public sealed class UpdateBookingAddOnSelectionsUseCase
{
	private readonly IPartyBookingRepository _partyBookingRepository;
	private readonly ReleaseBookingInventoryUseCase _releaseBookingInventoryUseCase;
	private readonly ReserveBookingInventoryUseCase _reserveBookingInventoryUseCase;
	private readonly GetPartyBookingTimelineUseCase _getPartyBookingTimelineUseCase;
	private readonly ILogger<UpdateBookingAddOnSelectionsUseCase> _logger;

	public UpdateBookingAddOnSelectionsUseCase(
		IPartyBookingRepository partyBookingRepository,
		ReleaseBookingInventoryUseCase releaseBookingInventoryUseCase,
		ReserveBookingInventoryUseCase reserveBookingInventoryUseCase,
		GetPartyBookingTimelineUseCase getPartyBookingTimelineUseCase,
		ILogger<UpdateBookingAddOnSelectionsUseCase> logger)
	{
		_partyBookingRepository = partyBookingRepository;
		_releaseBookingInventoryUseCase = releaseBookingInventoryUseCase;
		_reserveBookingInventoryUseCase = reserveBookingInventoryUseCase;
		_getPartyBookingTimelineUseCase = getPartyBookingTimelineUseCase;
		_logger = logger;
	}

	public async Task<AppResult<BookingAddOnUpdateResultDto>> ExecuteAsync(UpdateBookingAddOnSelectionsCommand command, CancellationToken ct = default)
	{
		if (!AreSelectionsValid(command.Selections))
		{
			return AppResult<BookingAddOnUpdateResultDto>.Failure(
				PartyBookingConstants.ErrorAddOnOptionInvalid,
				PartyBookingConstants.SafeAddOnOptionInvalidMessage);
		}

		var booking = await _partyBookingRepository.GetByIdWithSelectionsAsync(command.BookingId, ct);
		if (booking is null)
		{
			return AppResult<BookingAddOnUpdateResultDto>.Failure(
				PartyBookingConstants.ErrorBookingNotFound,
				PartyBookingConstants.SafeBookingNotFoundMessage);
		}

		if (booking.LastAddOnUpdateOperationId == command.OperationContext.OperationId)
		{
			var idempotentPayload = BuildResultDto(booking, command.Selections, []);
			return AppResult<BookingAddOnUpdateResultDto>.Success(
				idempotentPayload,
				PartyBookingConstants.AddOnSelectionsAlreadySavedMessage);
		}

		try
		{
			var previousSelections = booking.AddOnSelections
				.GroupBy(x => x.OptionId, StringComparer.Ordinal)
				.ToDictionary(x => x.Key, x => x.Sum(y => y.Quantity), StringComparer.Ordinal);

			var occurredUtc = DateTime.SpecifyKind(command.OperationContext.OccurredUtc, DateTimeKind.Utc);
			var newSelections = command.Selections.Select(selection => new PartyBookingAddOnSelection
			{
				Id = Guid.NewGuid(),
				BookingId = booking.Id,
				AddOnType = selection.AddOnType,
				OptionId = selection.OptionId,
				Quantity = selection.Quantity,
				SelectedAtUtc = occurredUtc,
				SelectionOperationId = command.OperationContext.OperationId,
			}).ToArray();

			await _partyBookingRepository.ReplaceAddOnSelectionsAsync(
				booking,
				newSelections,
				command.OperationContext.OperationId,
				command.OperationContext.CorrelationId,
				occurredUtc,
				ct);

			if (booking.Status == PartyBookingStatus.Booked)
			{
				var latestSelectionMap = command.Selections
					.GroupBy(x => x.OptionId, StringComparer.Ordinal)
					.ToDictionary(x => x.Key, x => x.Sum(y => y.Quantity), StringComparer.Ordinal);

				var removedOptionIds = previousSelections.Keys
					.Where(x => !latestSelectionMap.ContainsKey(x))
					.OrderBy(x => x, StringComparer.Ordinal)
					.ToArray();

				var reduced = previousSelections
					.Where(x => latestSelectionMap.TryGetValue(x.Key, out var nextQty) && nextQty < x.Value)
					.ToDictionary(x => x.Key, x => x.Value - latestSelectionMap[x.Key], StringComparer.Ordinal);

				var releaseTriggers = new List<InventoryReleaseTrigger>();
				if (removedOptionIds.Length > 0)
				{
					releaseTriggers.Add(InventoryReleaseTrigger.BookingItemRemoved);
				}

				if (reduced.Count > 0)
				{
					releaseTriggers.Add(InventoryReleaseTrigger.BookingItemQuantityReduced);
				}

				if (releaseTriggers.Count > 0)
				{
					await _releaseBookingInventoryUseCase.ExecuteAsync(
						new ReleaseBookingInventoryCommand(
							booking.Id,
							releaseTriggers,
							command.OperationContext,
							removedOptionIds,
							reduced),
						ct);
				}

				await _reserveBookingInventoryUseCase.ExecuteAsync(
					new ReserveBookingInventoryCommand(booking.Id, command.OperationContext),
					ct);
			}

			var milestones = await TryLoadMilestones(command.BookingId, ct);
			var refreshedBooking = await _partyBookingRepository.GetByIdWithSelectionsAsync(command.BookingId, ct) ?? booking;
			var payload = BuildResultDto(refreshedBooking, command.Selections, milestones);

			return AppResult<BookingAddOnUpdateResultDto>.Success(payload, PartyBookingConstants.AddOnSelectionsUpdatedMessage);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to save add-on selections for booking {BookingId}", command.BookingId);
			return AppResult<BookingAddOnUpdateResultDto>.Failure(
				PartyBookingConstants.ErrorAddOnUpdateFailed,
				PartyBookingConstants.SafeAddOnUpdateFailedMessage,
				ex.Message);
		}
	}

	private static bool AreSelectionsValid(IEnumerable<AddOnSelectionItemCommand> selections)
	{
		foreach (var selection in selections)
		{
			if (selection.Quantity <= 0)
			{
				return false;
			}

			var isKnown = selection.AddOnType switch
			{
				PartyAddOnType.Catering => Array.Exists(PartyBookingConstants.KnownCateringOptionIds, id => id == selection.OptionId),
				PartyAddOnType.Decor => Array.Exists(PartyBookingConstants.KnownDecorOptionIds, id => id == selection.OptionId),
				_ => false,
			};

			if (!isKnown)
			{
				return false;
			}
		}

		return true;
	}

	private async Task<IReadOnlyList<PartyBookingTimelineMilestoneDto>> TryLoadMilestones(Guid bookingId, CancellationToken ct)
	{
		var timelineResult = await _getPartyBookingTimelineUseCase.ExecuteAsync(bookingId, ct);
		if (timelineResult.IsSuccess && timelineResult.Payload is not null)
		{
			return timelineResult.Payload.Milestones;
		}

		_logger.LogWarning(
			"Timeline refresh failed after add-on save for booking {BookingId}. ErrorCode={ErrorCode}",
			bookingId,
			timelineResult.ErrorCode);
		return [];
	}

	private static BookingAddOnUpdateResultDto BuildResultDto(
		Domain.Entities.PartyBooking booking,
		IReadOnlyList<AddOnSelectionItemCommand> fallbackSelections,
		IReadOnlyList<PartyBookingTimelineMilestoneDto> milestones)
	{
		var persistedSelections = booking.AddOnSelections.Count > 0
			? booking.AddOnSelections.Select(selection =>
				new AddOnSelectionItemCommand(selection.OptionId, selection.AddOnType, selection.Quantity))
				.ToArray()
			: fallbackSelections;

		var selectedByOption = persistedSelections
			.GroupBy(s => s.OptionId, StringComparer.Ordinal)
			.ToDictionary(g => g.Key, g => g.Last(), StringComparer.Ordinal);

		var catering = BuildOptions(PartyAddOnType.Catering, PartyBookingConstants.KnownCateringOptionIds, selectedByOption);
		var decor = BuildOptions(PartyAddOnType.Decor, PartyBookingConstants.KnownDecorOptionIds, selectedByOption);
		var risks = BookingRiskEvaluator.EvaluateRisks(persistedSelections);
		var total = persistedSelections.Sum(selection => PartyBookingConstants.AddOnOptionPriceCents[selection.OptionId] * selection.Quantity);

		return new BookingAddOnUpdateResultDto(
			booking.Id,
			catering,
			decor,
			total,
			risks,
			milestones);
	}

	private static IReadOnlyList<AddOnOptionItemDto> BuildOptions(
		PartyAddOnType addOnType,
		IEnumerable<string> optionIds,
		IReadOnlyDictionary<string, AddOnSelectionItemCommand> selectedByOption)
	{
		var options = new List<AddOnOptionItemDto>();
		foreach (var optionId in optionIds)
		{
			selectedByOption.TryGetValue(optionId, out var selected);
			PartyBookingConstants.AddOnOptionDisplayNames.TryGetValue(optionId, out var displayName);
			PartyBookingConstants.AddOnOptionPriceCents.TryGetValue(optionId, out var priceCents);
			var (isAtRisk, severity, reason) = BookingRiskEvaluator.GetRiskInfo(optionId);

			options.Add(new AddOnOptionItemDto(
				optionId,
				displayName ?? optionId,
				addOnType,
				selected is not null,
				selected?.Quantity ?? 0,
				priceCents,
				isAtRisk,
				severity,
				reason));
		}

		return options;
	}
}

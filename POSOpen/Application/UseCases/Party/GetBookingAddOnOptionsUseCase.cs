using Microsoft.Extensions.Logging;
using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.Results;
using POSOpen.Domain.Enums;

namespace POSOpen.Application.UseCases.Party;

public sealed class GetBookingAddOnOptionsUseCase
{
	private readonly IPartyBookingRepository _partyBookingRepository;
	private readonly ILogger<GetBookingAddOnOptionsUseCase> _logger;

	public GetBookingAddOnOptionsUseCase(
		IPartyBookingRepository partyBookingRepository,
		ILogger<GetBookingAddOnOptionsUseCase> logger)
	{
		_partyBookingRepository = partyBookingRepository;
		_logger = logger;
	}

	public async Task<AppResult<BookingAddOnOptionsDto>> ExecuteAsync(GetBookingAddOnOptionsQuery query, CancellationToken ct = default)
	{
		try
		{
			var booking = await _partyBookingRepository.GetByIdWithSelectionsAsync(query.BookingId, ct);
			if (booking is null)
			{
				return AppResult<BookingAddOnOptionsDto>.Failure(
					PartyBookingConstants.ErrorBookingNotFound,
					PartyBookingConstants.SafeBookingNotFoundMessage);
			}

			var selectedByOption = booking.AddOnSelections
				.ToDictionary(s => s.OptionId, s => s, StringComparer.Ordinal);

			var cateringOptions = BuildOptions(PartyAddOnType.Catering, PartyBookingConstants.KnownCateringOptionIds, selectedByOption);
			var decorOptions = BuildOptions(PartyAddOnType.Decor, PartyBookingConstants.KnownDecorOptionIds, selectedByOption);
			var total = CalculateTotal(cateringOptions) + CalculateTotal(decorOptions);

			var payload = new BookingAddOnOptionsDto(
				booking.Id,
				cateringOptions,
				decorOptions,
				total);

			return AppResult<BookingAddOnOptionsDto>.Success(payload, PartyBookingConstants.AddOnOptionsLoadedMessage);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to load add-on options for booking {BookingId}", query.BookingId);
			return AppResult<BookingAddOnOptionsDto>.Failure(
				PartyBookingConstants.ErrorAddOnUpdateFailed,
				PartyBookingConstants.SafeAddOnUpdateFailedMessage,
				ex.Message);
		}
	}

	private static IReadOnlyList<AddOnOptionItemDto> BuildOptions(
		PartyAddOnType addOnType,
		IEnumerable<string> optionIds,
		IReadOnlyDictionary<string, Domain.Entities.PartyBookingAddOnSelection> selectedByOption)
	{
		var results = new List<AddOnOptionItemDto>();
		foreach (var optionId in optionIds)
		{
			selectedByOption.TryGetValue(optionId, out var selected);
			PartyBookingConstants.AddOnOptionDisplayNames.TryGetValue(optionId, out var displayName);
			PartyBookingConstants.AddOnOptionPriceCents.TryGetValue(optionId, out var priceCents);
			var (isAtRisk, severity, reason) = BookingRiskEvaluator.GetRiskInfo(optionId);

			results.Add(new AddOnOptionItemDto(
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

		return results;
	}

	private static long CalculateTotal(IEnumerable<AddOnOptionItemDto> options) =>
		options.Where(x => x.IsSelected).Sum(x => x.PriceCents * x.Quantity);
}

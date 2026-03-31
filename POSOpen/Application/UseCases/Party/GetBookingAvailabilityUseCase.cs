using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.Results;
using Microsoft.Extensions.Logging;

namespace POSOpen.Application.UseCases.Party;

public sealed class GetBookingAvailabilityUseCase
{
	private readonly IPartyBookingRepository _partyBookingRepository;
	private readonly ILogger<GetBookingAvailabilityUseCase> _logger;

	public GetBookingAvailabilityUseCase(IPartyBookingRepository partyBookingRepository, ILogger<GetBookingAvailabilityUseCase> logger)
	{
		_partyBookingRepository = partyBookingRepository;
		_logger = logger;
	}

	public async Task<AppResult<BookingAvailabilityDto>> ExecuteAsync(DateTime partyDateUtc, CancellationToken ct = default)
	{
		try
		{
			var targetDateUtc = DateTime.SpecifyKind(partyDateUtc, DateTimeKind.Utc);
			var slots = new List<BookingSlotAvailabilityDto>(PartyBookingConstants.KnownSlotIds.Length);

			foreach (var slotId in PartyBookingConstants.KnownSlotIds)
			{
				var unavailable = await _partyBookingRepository.IsSlotUnavailableAsync(targetDateUtc, slotId, null, ct);
				slots.Add(new BookingSlotAvailabilityDto(
					slotId,
					!unavailable,
					unavailable ? PartyBookingConstants.SafeSlotUnavailableMessage : null));
			}

			return AppResult<BookingAvailabilityDto>.Success(
				new BookingAvailabilityDto(targetDateUtc, slots),
				PartyBookingConstants.AvailabilityLoadedMessage);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to load booking availability for date {PartyDateUtc}", partyDateUtc);
			return AppResult<BookingAvailabilityDto>.Failure(
				PartyBookingConstants.ErrorCommitFailed,
				PartyBookingConstants.SafeCommitFailedMessage);
		}
	}
}

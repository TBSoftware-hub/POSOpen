using Microsoft.Extensions.Logging;
using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.Results;

namespace POSOpen.Application.UseCases.Party;

public sealed class GetRoomOptionsUseCase
{
	private readonly IPartyBookingRepository _partyBookingRepository;
	private readonly ILogger<GetRoomOptionsUseCase> _logger;

	public GetRoomOptionsUseCase(IPartyBookingRepository partyBookingRepository, ILogger<GetRoomOptionsUseCase> logger)
	{
		_partyBookingRepository = partyBookingRepository;
		_logger = logger;
	}

	public async Task<AppResult<RoomOptionsDto>> ExecuteAsync(GetRoomOptionsQuery query, CancellationToken ct = default)
	{
		try
		{
			var targetDateUtc = DateTime.SpecifyKind(query.PartyDateUtc, DateTimeKind.Utc);
			var rooms = new List<RoomOptionItemDto>(PartyBookingConstants.KnownRoomIds.Length);

			foreach (var roomId in PartyBookingConstants.KnownRoomIds)
			{
				var unavailable = await _partyBookingRepository.IsRoomUnavailableAsync(targetDateUtc, roomId, null, ct);
				rooms.Add(new RoomOptionItemDto(
					roomId,
					roomId.Replace("-", " ").ToUpperInvariant(),
					!unavailable,
					unavailable ? PartyBookingConstants.SafeRoomConflictMessage : null));
			}

			return AppResult<RoomOptionsDto>.Success(
				new RoomOptionsDto(targetDateUtc, query.SlotId, rooms),
				PartyBookingConstants.AvailabilityLoadedMessage);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to load room options for date {PartyDateUtc}", query.PartyDateUtc);
			return AppResult<RoomOptionsDto>.Failure(
				PartyBookingConstants.ErrorCommitFailed,
				PartyBookingConstants.SafeRoomAssignmentFailedMessage);
		}
	}
}

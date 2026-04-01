using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.Results;

namespace POSOpen.Application.UseCases.Party;

public sealed class AssignPartyRoomUseCase
{
	private readonly IPartyBookingRepository _partyBookingRepository;
	private readonly GetPartyBookingTimelineUseCase _getPartyBookingTimelineUseCase;
	private readonly ILogger<AssignPartyRoomUseCase> _logger;

	public AssignPartyRoomUseCase(
		IPartyBookingRepository partyBookingRepository,
		GetPartyBookingTimelineUseCase getPartyBookingTimelineUseCase,
		ILogger<AssignPartyRoomUseCase> logger)
	{
		_partyBookingRepository = partyBookingRepository;
		_getPartyBookingTimelineUseCase = getPartyBookingTimelineUseCase;
		_logger = logger;
	}

	public async Task<AppResult<RoomAssignmentResultDto>> ExecuteAsync(AssignPartyRoomCommand command, CancellationToken ct = default)
	{
		if (!Array.Exists(PartyBookingConstants.KnownRoomIds, r => r == command.RoomId))
		{
			return AppResult<RoomAssignmentResultDto>.Failure(
				PartyBookingConstants.ErrorRoomInvalid,
				PartyBookingConstants.SafeRoomAssignmentFailedMessage);
		}

		var booking = await _partyBookingRepository.GetByIdAsync(command.BookingId, ct);
		if (booking is null)
		{
			return AppResult<RoomAssignmentResultDto>.Failure(
				PartyBookingConstants.ErrorBookingNotFound,
				PartyBookingConstants.SafeBookingNotFoundMessage);
		}

		try
		{
			var assignedAtUtc = DateTime.SpecifyKind(command.Context.OccurredUtc, DateTimeKind.Utc);
			var persisted = await _partyBookingRepository.AssignRoomAsync(
				booking,
				command.RoomId,
				command.Context.OperationId,
				command.Context.CorrelationId,
				assignedAtUtc,
				ct);

			IReadOnlyList<PartyBookingTimelineMilestoneDto>? milestones = null;
			var timelineResult = await _getPartyBookingTimelineUseCase.ExecuteAsync(persisted.Id, ct);
			if (timelineResult.IsSuccess && timelineResult.Payload is not null)
			{
				milestones = timelineResult.Payload.Milestones;
			}

			return AppResult<RoomAssignmentResultDto>.Success(
				new RoomAssignmentResultDto(persisted.Id, persisted.AssignedRoomId, milestones),
				PartyBookingConstants.RoomAssignedMessage);
		}
		catch (DbUpdateException ex) when (ex.Message.Contains("ROOM"))
		{
			_logger.LogWarning(ex, "Room conflict for booking {BookingId} on room {RoomId}", command.BookingId, command.RoomId);
			try
			{
				var alternativeRooms = await _partyBookingRepository.ListAlternativeRoomsAsync(
					booking.PartyDateUtc, booking.SlotId, command.RoomId, ct);
				var alternativeSlots = await _partyBookingRepository.ListAlternativeSlotsAsync(
					booking.PartyDateUtc, command.RoomId, booking.SlotId, ct);

				var conflictPayload = new RoomAssignmentResultDto(
					command.BookingId,
					null,
					null,
					alternativeRooms,
					alternativeSlots);

				return new AppResult<RoomAssignmentResultDto>(
					false,
					PartyBookingConstants.ErrorRoomConflict,
					PartyBookingConstants.SafeRoomConflictMessage,
					null,
					conflictPayload);
			}
			catch (Exception innerEx)
			{
				_logger.LogError(innerEx, "Failed to list alternatives after room conflict for booking {BookingId}", command.BookingId);
				return AppResult<RoomAssignmentResultDto>.Failure(
					PartyBookingConstants.ErrorRoomConflict,
					PartyBookingConstants.SafeRoomConflictMessage);
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Room assignment failed for booking {BookingId}", command.BookingId);
			return AppResult<RoomAssignmentResultDto>.Failure(
				PartyBookingConstants.ErrorCommitFailed,
				PartyBookingConstants.SafeRoomAssignmentFailedMessage);
		}
	}
}

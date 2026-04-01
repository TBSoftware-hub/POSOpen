using Microsoft.Extensions.Logging;
using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.Abstractions.Services;
using POSOpen.Application.Results;
using POSOpen.Domain.Entities;
using POSOpen.Domain.Enums;

namespace POSOpen.Application.UseCases.Party;

public sealed class GetPartyBookingTimelineUseCase
{
	private static readonly TimeSpan ActiveWindowDuration = TimeSpan.FromHours(3);

	private readonly IPartyBookingRepository _partyBookingRepository;
	private readonly IUtcClock _clock;
	private readonly ILogger<GetPartyBookingTimelineUseCase> _logger;

	public GetPartyBookingTimelineUseCase(
		IPartyBookingRepository partyBookingRepository,
		IUtcClock clock,
		ILogger<GetPartyBookingTimelineUseCase> logger)
	{
		_partyBookingRepository = partyBookingRepository;
		_clock = clock;
		_logger = logger;
	}

	public async Task<AppResult<PartyBookingTimelineDto>> ExecuteAsync(Guid bookingId, CancellationToken ct = default)
	{
		try
		{
			var booking = await _partyBookingRepository.GetByIdAsync(bookingId, ct);
			if (booking is null)
			{
				return AppResult<PartyBookingTimelineDto>.Failure(
					PartyBookingConstants.ErrorBookingNotFound,
					PartyBookingConstants.SafeBookingNotFoundMessage);
			}

			if (booking.Status != PartyBookingStatus.Booked)
			{
				return AppResult<PartyBookingTimelineDto>.Failure(
					PartyBookingConstants.ErrorTimelineUnavailable,
					PartyBookingConstants.SafeTimelineUnavailableMessage);
			}

			var nowUtc = _clock.UtcNow;
			var milestones = BuildMilestones(booking, nowUtc);
			var timeline = new PartyBookingTimelineDto(
				booking.Id,
				booking.Status,
				booking.DepositCommitmentStatus == PartyDepositCommitmentStatus.Committed,
				nowUtc,
				milestones,
				booking.PartyDateUtc,
				booking.SlotId);

			return AppResult<PartyBookingTimelineDto>.Success(
				timeline,
				PartyBookingConstants.TimelineLoadedMessage);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to generate timeline for booking {BookingId}", bookingId);
			return AppResult<PartyBookingTimelineDto>.Failure(
				PartyBookingConstants.ErrorTimelineUnavailable,
				PartyBookingConstants.SafeTimelineUnavailableMessage);
		}
	}

	private static IReadOnlyList<PartyBookingTimelineMilestoneDto> BuildMilestones(PartyBooking booking, DateTime nowUtc)
	{
		var eventStartUtc = DeriveEventStartUtc(booking);
		var eventEndUtc = eventStartUtc.Add(ActiveWindowDuration);
		var hasCompletedSignal = booking.CompletedAtUtc.HasValue;

		var bookedStatus = booking.BookedAtUtc.HasValue
			? PartyTimelineMilestoneStatus.Completed
			: PartyTimelineMilestoneStatus.Pending;

		var upcomingStatus = hasCompletedSignal
			? PartyTimelineMilestoneStatus.Completed
			: nowUtc < eventStartUtc
				? PartyTimelineMilestoneStatus.Current
				: PartyTimelineMilestoneStatus.Completed;

		var activeStatus = hasCompletedSignal
			? PartyTimelineMilestoneStatus.Completed
			: nowUtc < eventStartUtc
				? PartyTimelineMilestoneStatus.Pending
				: nowUtc < eventEndUtc
					? PartyTimelineMilestoneStatus.Current
					: PartyTimelineMilestoneStatus.Completed;

		var completedStatus = hasCompletedSignal
			? PartyTimelineMilestoneStatus.Completed
			: nowUtc >= eventEndUtc
				? PartyTimelineMilestoneStatus.Current
				: PartyTimelineMilestoneStatus.Pending;

		return
		[
			new PartyBookingTimelineMilestoneDto(
				"booked",
				bookedStatus,
				booking.BookedAtUtc,
				booking.DepositCommitmentStatus == PartyDepositCommitmentStatus.Committed
					? PartyBookingConstants.NextActionPrepareArrivalCode
					: PartyBookingConstants.NextActionCaptureDepositCode,
				booking.DepositCommitmentStatus == PartyDepositCommitmentStatus.Committed
					? PartyBookingConstants.NextActionPrepareArrivalLabel
					: PartyBookingConstants.NextActionCaptureDepositLabel,
				null),
			new PartyBookingTimelineMilestoneDto(
				"upcoming",
				upcomingStatus,
				eventStartUtc,
				PartyBookingConstants.NextActionPrepareArrivalCode,
				PartyBookingConstants.NextActionPrepareArrivalLabel,
				GetUpcomingRailLabel(booking, nowUtc, eventStartUtc)),
			new PartyBookingTimelineMilestoneDto(
				"active",
				activeStatus,
				nowUtc >= eventStartUtc ? eventStartUtc : null,
				completedStatus == PartyTimelineMilestoneStatus.Current
					? PartyBookingConstants.NextActionMarkCompletedCode
					: PartyBookingConstants.NextActionMonitorActiveCode,
				completedStatus == PartyTimelineMilestoneStatus.Current
					? PartyBookingConstants.NextActionMarkCompletedLabel
					: PartyBookingConstants.NextActionMonitorActiveLabel,
				GetActiveRailLabel(booking, nowUtc, eventStartUtc, eventEndUtc)),
			new PartyBookingTimelineMilestoneDto(
				"completed",
				completedStatus,
				booking.CompletedAtUtc,
				hasCompletedSignal
					? PartyBookingConstants.NextActionClosedCode
					: PartyBookingConstants.NextActionMarkCompletedCode,
				hasCompletedSignal
					? PartyBookingConstants.NextActionClosedLabel
					: PartyBookingConstants.NextActionMarkCompletedLabel,
				null),
		];
	}

	private static DateTime DeriveEventStartUtc(PartyBooking booking)
	{
		if (TimeSpan.TryParse(booking.SlotId, out var slotTime))
		{
			return booking.PartyDateUtc.Date.Add(slotTime);
		}

		return booking.PartyDateUtc;
	}

	private static string? GetUpcomingRailLabel(PartyBooking booking, DateTime nowUtc, DateTime eventStartUtc)
	{
		if (booking.Status == PartyBookingStatus.Cancelled)
		{
			return "exception";
		}

		if (nowUtc >= eventStartUtc.AddMinutes(-30) && nowUtc < eventStartUtc)
		{
			return "arrived";
		}

		return null;
	}

	private static string? GetActiveRailLabel(PartyBooking booking, DateTime nowUtc, DateTime eventStartUtc, DateTime eventEndUtc)
	{
		if (booking.Status == PartyBookingStatus.Cancelled)
		{
			return "exception";
		}

		if (nowUtc >= eventStartUtc && nowUtc < eventEndUtc && !booking.CompletedAtUtc.HasValue)
		{
			return "waiver-pending";
		}

		return null;
	}
}

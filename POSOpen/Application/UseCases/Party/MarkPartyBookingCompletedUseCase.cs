using Microsoft.Extensions.Logging;
using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.Abstractions.Services;
using POSOpen.Application.Results;
using POSOpen.Domain.Enums;

namespace POSOpen.Application.UseCases.Party;

public sealed class MarkPartyBookingCompletedUseCase
{
	private readonly IPartyBookingRepository _partyBookingRepository;
	private readonly IUtcClock _clock;
	private readonly ILogger<MarkPartyBookingCompletedUseCase> _logger;

	public MarkPartyBookingCompletedUseCase(
		IPartyBookingRepository partyBookingRepository,
		IUtcClock clock,
		ILogger<MarkPartyBookingCompletedUseCase> logger)
	{
		_partyBookingRepository = partyBookingRepository;
		_clock = clock;
		_logger = logger;
	}

	public async Task<AppResult<MarkPartyBookingCompletedResultDto>> ExecuteAsync(MarkPartyBookingCompletedCommand command, CancellationToken ct = default)
	{
		var booking = await _partyBookingRepository.GetByIdAsync(command.BookingId, ct);
		if (booking is null)
		{
			return AppResult<MarkPartyBookingCompletedResultDto>.Failure(
				PartyBookingConstants.ErrorBookingNotFound,
				PartyBookingConstants.SafeBookingNotFoundMessage);
		}

		if (booking.Status != PartyBookingStatus.Booked)
		{
			return AppResult<MarkPartyBookingCompletedResultDto>.Failure(
				PartyBookingConstants.ErrorStateInvalid,
				PartyBookingConstants.SafeStateInvalidMessage);
		}

		if (booking.CompletedAtUtc.HasValue)
		{
			return AppResult<MarkPartyBookingCompletedResultDto>.Success(
				Map(booking),
				PartyBookingConstants.BookingAlreadyCompletedMessage);
		}

		try
		{
			var completedAtUtc = _clock.UtcNow;
			var persisted = await _partyBookingRepository.MarkCompletedAsync(
				booking,
				command.Context.OperationId,
				command.Context.CorrelationId,
				completedAtUtc,
				ct);

			return AppResult<MarkPartyBookingCompletedResultDto>.Success(
				Map(persisted),
				PartyBookingConstants.BookingMarkedCompletedMessage);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to mark booking {BookingId} completed", command.BookingId);
			return AppResult<MarkPartyBookingCompletedResultDto>.Failure(
				PartyBookingConstants.ErrorCommitFailed,
				PartyBookingConstants.SafeCommitFailedMessage);
		}
	}

	private static MarkPartyBookingCompletedResultDto Map(Domain.Entities.PartyBooking booking) =>
		new(
			booking.Id,
			booking.CompletedAtUtc ?? booking.UpdatedAtUtc,
			booking.OperationId,
			booking.CorrelationId);
}

using Microsoft.Extensions.Logging;
using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.Results;
using POSOpen.Domain.Enums;

namespace POSOpen.Application.UseCases.Party;

public sealed class RecordPartyDepositCommitmentUseCase
{
	private readonly IPartyBookingRepository _partyBookingRepository;
	private readonly ILogger<RecordPartyDepositCommitmentUseCase> _logger;

	public RecordPartyDepositCommitmentUseCase(
		IPartyBookingRepository partyBookingRepository,
		ILogger<RecordPartyDepositCommitmentUseCase> logger)
	{
		_partyBookingRepository = partyBookingRepository;
		_logger = logger;
	}

	public async Task<AppResult<RecordPartyDepositCommitmentResultDto>> ExecuteAsync(RecordPartyDepositCommitmentCommand command, CancellationToken ct = default)
	{
		if (command.DepositAmountCents <= 0)
		{
			return AppResult<RecordPartyDepositCommitmentResultDto>.Failure(
				PartyBookingConstants.ErrorDepositAmountInvalid,
				PartyBookingConstants.SafeDepositAmountInvalidMessage);
		}

		if (string.IsNullOrWhiteSpace(command.DepositCurrency) || command.DepositCurrency.Trim().Length != 3)
		{
			return AppResult<RecordPartyDepositCommitmentResultDto>.Failure(
				PartyBookingConstants.ErrorDepositCurrencyInvalid,
				PartyBookingConstants.SafeDepositCurrencyInvalidMessage);
		}

		var currency = command.DepositCurrency.Trim().ToUpperInvariant();
		if (!currency.All(char.IsLetter))
		{
			return AppResult<RecordPartyDepositCommitmentResultDto>.Failure(
				PartyBookingConstants.ErrorDepositCurrencyInvalid,
				PartyBookingConstants.SafeDepositCurrencyInvalidMessage);
		}

		var booking = await _partyBookingRepository.GetByIdAsync(command.BookingId, ct);
		if (booking is null)
		{
			return AppResult<RecordPartyDepositCommitmentResultDto>.Failure(
				PartyBookingConstants.ErrorBookingNotFound,
				PartyBookingConstants.SafeBookingNotFoundMessage);
		}

		if (booking.Status != PartyBookingStatus.Booked)
		{
			return AppResult<RecordPartyDepositCommitmentResultDto>.Failure(
				PartyBookingConstants.ErrorStateInvalid,
				PartyBookingConstants.SafeStateInvalidMessage);
		}

		if (booking.DepositCommitmentStatus == PartyDepositCommitmentStatus.Committed)
		{
			if (booking.DepositCommitmentOperationId == command.Context.OperationId &&
				booking.DepositAmountCents.HasValue &&
				booking.DepositCommittedAtUtc.HasValue &&
				booking.DepositCurrency is not null)
			{
				return AppResult<RecordPartyDepositCommitmentResultDto>.Success(
					Map(booking),
					PartyBookingConstants.DepositAlreadyCommittedMessage);
			}

			return AppResult<RecordPartyDepositCommitmentResultDto>.Failure(
				PartyBookingConstants.ErrorStateInvalid,
				PartyBookingConstants.DepositAlreadyCommittedMessage);
		}

		try
		{
			var committedAtUtc = DateTime.SpecifyKind(command.Context.OccurredUtc, DateTimeKind.Utc);
			var persisted = await _partyBookingRepository.RecordDepositCommitmentAsync(
				booking,
				command.DepositAmountCents,
				currency,
				command.Context.OperationId,
				command.Context.CorrelationId,
				committedAtUtc,
				ct);

			return AppResult<RecordPartyDepositCommitmentResultDto>.Success(
				Map(persisted),
				PartyBookingConstants.DepositCommittedMessage);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to record deposit commitment for booking {BookingId}", command.BookingId);
			return AppResult<RecordPartyDepositCommitmentResultDto>.Failure(
				PartyBookingConstants.ErrorCommitFailed,
				PartyBookingConstants.SafeCommitFailedMessage);
		}
	}

	private static RecordPartyDepositCommitmentResultDto Map(Domain.Entities.PartyBooking booking) =>
		new(
			booking.Id,
			booking.DepositCommitmentStatus,
			booking.DepositAmountCents ?? 0,
			booking.DepositCurrency ?? string.Empty,
			booking.DepositCommittedAtUtc ?? booking.UpdatedAtUtc,
			booking.OperationId,
			booking.CorrelationId);
}

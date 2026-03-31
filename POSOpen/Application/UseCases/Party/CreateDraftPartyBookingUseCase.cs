using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.Abstractions.Services;
using POSOpen.Application.Results;
using POSOpen.Domain.Entities;
using POSOpen.Domain.Enums;

namespace POSOpen.Application.UseCases.Party;

public sealed class CreateDraftPartyBookingUseCase
{
	private readonly IPartyBookingRepository _partyBookingRepository;
	private readonly IUtcClock _clock;
	private readonly ILogger<CreateDraftPartyBookingUseCase> _logger;

	public CreateDraftPartyBookingUseCase(
		IPartyBookingRepository partyBookingRepository,
		IUtcClock clock,
		ILogger<CreateDraftPartyBookingUseCase> logger)
	{
		_partyBookingRepository = partyBookingRepository;
		_clock = clock;
		_logger = logger;
	}

	public async Task<AppResult<PartyBookingDraftDto>> ExecuteAsync(CreateDraftPartyBookingCommand command, CancellationToken ct = default)
	{
		var normalizedDateUtc = DateTime.SpecifyKind(command.PartyDateUtc, DateTimeKind.Utc);
		if (normalizedDateUtc.Date < _clock.UtcNow.Date)
		{
			return AppResult<PartyBookingDraftDto>.Failure(
				PartyBookingConstants.ErrorDateInvalid,
				PartyBookingConstants.SafeDateInvalidMessage);
		}

		if (string.IsNullOrWhiteSpace(command.SlotId))
		{
			return AppResult<PartyBookingDraftDto>.Failure(
				PartyBookingConstants.ErrorSlotRequired,
				PartyBookingConstants.SafeSlotRequiredMessage);
		}

		var slotId = command.SlotId.Trim();
		if (!PartyBookingConstants.KnownSlotIds.Contains(slotId, StringComparer.Ordinal))
		{
			return AppResult<PartyBookingDraftDto>.Failure(
				PartyBookingConstants.ErrorSlotInvalid,
				PartyBookingConstants.SafeSlotInvalidMessage);
		}

		if (string.IsNullOrWhiteSpace(command.PackageId))
		{
			return AppResult<PartyBookingDraftDto>.Failure(
				PartyBookingConstants.ErrorPackageRequired,
				PartyBookingConstants.SafePackageRequiredMessage);
		}

		var packageId = command.PackageId.Trim();
		var existingByOperation = await _partyBookingRepository.GetByOperationIdAsync(command.Context.OperationId, ct);
		if (existingByOperation is not null)
		{
			return AppResult<PartyBookingDraftDto>.Success(
				Map(existingByOperation),
				PartyBookingConstants.DraftAlreadySavedMessage);
		}

		try
		{
			var unavailable = await _partyBookingRepository.IsSlotUnavailableAsync(normalizedDateUtc, slotId, command.BookingId, ct);
			if (unavailable)
			{
				return AppResult<PartyBookingDraftDto>.Failure(
					PartyBookingConstants.ErrorSlotUnavailable,
					PartyBookingConstants.SafeSlotUnavailableMessage);
			}

			var booking = PartyBooking.CreateDraft(
				command.BookingId ?? Guid.NewGuid(),
				normalizedDateUtc,
				slotId,
				packageId,
				command.Context.OperationId,
				command.Context.CorrelationId,
				_clock.UtcNow);

			var persisted = await _partyBookingRepository.UpsertDraftAsync(booking, ct);
			return AppResult<PartyBookingDraftDto>.Success(Map(persisted), PartyBookingConstants.DraftSavedMessage);
		}
		catch (DbUpdateException ex)
		{
			_logger.LogWarning(ex, "Party booking slot conflict for date {PartyDateUtc} slot {SlotId}", normalizedDateUtc, slotId);
			return AppResult<PartyBookingDraftDto>.Failure(
				PartyBookingConstants.ErrorSlotUnavailable,
				PartyBookingConstants.SafeSlotUnavailableMessage);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to save draft booking for date {PartyDateUtc} slot {SlotId}", normalizedDateUtc, slotId);
			return AppResult<PartyBookingDraftDto>.Failure(
				PartyBookingConstants.ErrorCommitFailed,
				PartyBookingConstants.SafeCommitFailedMessage);
		}
	}

	private static PartyBookingDraftDto Map(PartyBooking booking) =>
		new(
			booking.Id,
			booking.PartyDateUtc,
			booking.SlotId,
			booking.PackageId,
			booking.Status,
			booking.OperationId,
			booking.CorrelationId,
			booking.UpdatedAtUtc);
}

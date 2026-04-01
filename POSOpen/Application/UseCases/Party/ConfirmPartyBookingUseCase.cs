using Microsoft.Extensions.Logging;
using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.Abstractions.Services;
using POSOpen.Application.Results;
using POSOpen.Application.Security;
using POSOpen.Application.UseCases.Inventory;
using POSOpen.Domain.Enums;

namespace POSOpen.Application.UseCases.Party;

public sealed class ConfirmPartyBookingUseCase
{
	private readonly IPartyBookingRepository _partyBookingRepository;
	private readonly IOperationLogRepository _operationLogRepository;
	private readonly IUtcClock _clock;
	private readonly ReserveBookingInventoryUseCase _reserveBookingInventoryUseCase;
	private readonly ILogger<ConfirmPartyBookingUseCase> _logger;

	public ConfirmPartyBookingUseCase(
		IPartyBookingRepository partyBookingRepository,
		IOperationLogRepository operationLogRepository,
		IUtcClock clock,
		ReserveBookingInventoryUseCase reserveBookingInventoryUseCase,
		ILogger<ConfirmPartyBookingUseCase> logger)
	{
		_partyBookingRepository = partyBookingRepository;
		_operationLogRepository = operationLogRepository;
		_clock = clock;
		_reserveBookingInventoryUseCase = reserveBookingInventoryUseCase;
		_logger = logger;
	}

	public async Task<AppResult<ConfirmPartyBookingResultDto>> ExecuteAsync(ConfirmPartyBookingCommand command, CancellationToken ct = default)
	{
		var booking = await _partyBookingRepository.GetByIdAsync(command.BookingId, ct);
		if (booking is null)
		{
			return AppResult<ConfirmPartyBookingResultDto>.Failure(
				PartyBookingConstants.ErrorBookingNotFound,
				PartyBookingConstants.SafeBookingNotFoundMessage);
		}

		if (booking.Status == PartyBookingStatus.Booked && booking.BookedAtUtc.HasValue)
		{
			return AppResult<ConfirmPartyBookingResultDto>.Success(
				Map(booking, booking.BookedAtUtc.Value),
				PartyBookingConstants.BookingAlreadyConfirmedMessage);
		}

		if (booking.Status != PartyBookingStatus.Draft)
		{
			return AppResult<ConfirmPartyBookingResultDto>.Failure(
				PartyBookingConstants.ErrorStateInvalid,
				PartyBookingConstants.SafeStateInvalidMessage);
		}

		if (string.IsNullOrWhiteSpace(booking.SlotId))
		{
			return AppResult<ConfirmPartyBookingResultDto>.Failure(
				PartyBookingConstants.ErrorSlotRequired,
				PartyBookingConstants.SafeSlotRequiredMessage);
		}

		if (string.IsNullOrWhiteSpace(booking.PackageId))
		{
			return AppResult<ConfirmPartyBookingResultDto>.Failure(
				PartyBookingConstants.ErrorPackageRequired,
				PartyBookingConstants.SafePackageRequiredMessage);
		}

		try
		{
			var unavailable = await _partyBookingRepository.IsSlotUnavailableAsync(
				booking.PartyDateUtc,
				booking.SlotId,
				booking.Id,
				ct);

			if (unavailable)
			{
				await _operationLogRepository.AppendAsync(
					SecurityAuditEventTypes.PartyBookingConfirmationDenied,
					booking.Id.ToString(),
					new PartyBookingConfirmationDeniedAuditPayload(
						booking.Id,
						booking.PartyDateUtc,
						booking.SlotId!,
						command.Context.OperationId,
						_clock.UtcNow),
					command.Context,
					cancellationToken: ct);
				return AppResult<ConfirmPartyBookingResultDto>.Failure(
					PartyBookingConstants.ErrorSlotUnavailable,
					PartyBookingConstants.SafeSlotUnavailableMessage);
			}

			var bookedAtUtc = _clock.UtcNow;
			var persisted = await _partyBookingRepository.ConfirmAsync(
				booking,
				command.Context.OperationId,
				command.Context.CorrelationId,
				bookedAtUtc,
				ct);

			var inventoryResult = await _reserveBookingInventoryUseCase.ExecuteAsync(
				new ReserveBookingInventoryCommand(persisted.Id, command.Context),
				ct);

			await _operationLogRepository.AppendAsync(
				SecurityAuditEventTypes.PartyBookingConfirmed,
				persisted.Id.ToString(),
				new PartyBookingConfirmedAuditPayload(
					persisted.Id,
					persisted.PartyDateUtc,
					persisted.SlotId!,
					persisted.PackageId!,
					command.Context.OperationId,
					bookedAtUtc),
				command.Context,
				cancellationToken: ct);

			return AppResult<ConfirmPartyBookingResultDto>.Success(
				Map(persisted, bookedAtUtc),
				inventoryResult.IsSuccess
					? PartyBookingConstants.BookingConfirmedMessage
					: $"{PartyBookingConstants.BookingConfirmedMessage} {PartyBookingConstants.InventoryConstraintGuidanceMessage}");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to confirm booking {BookingId}", command.BookingId);
			return AppResult<ConfirmPartyBookingResultDto>.Failure(
				PartyBookingConstants.ErrorCommitFailed,
				PartyBookingConstants.SafeCommitFailedMessage);
		}
	}

	private static ConfirmPartyBookingResultDto Map(Domain.Entities.PartyBooking booking, DateTime bookedAtUtc) =>
		new(
			booking.Id,
			booking.Status,
			bookedAtUtc,
			booking.OperationId,
			booking.CorrelationId);
}

internal sealed record PartyBookingConfirmedAuditPayload(
	Guid BookingId,
	DateTime PartyDateUtc,
	string SlotId,
	string PackageId,
	Guid OperationId,
	DateTime ConfirmedAtUtc);

internal sealed record PartyBookingConfirmationDeniedAuditPayload(
	Guid BookingId,
	DateTime PartyDateUtc,
	string SlotId,
	Guid OperationId,
	DateTime DeniedAtUtc);

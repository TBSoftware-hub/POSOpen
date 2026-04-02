using Microsoft.Extensions.Logging;
using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.Abstractions.Security;
using POSOpen.Application.Abstractions.Services;
using POSOpen.Application.Results;
using POSOpen.Application.UseCases.Inventory;
using POSOpen.Domain.Enums;

namespace POSOpen.Application.UseCases.Party;

public sealed class MarkPartyBookingCompletedUseCase
{
	private readonly IPartyBookingRepository _partyBookingRepository;
	private readonly ReserveBookingInventoryUseCase _reserveBookingInventoryUseCase;
	private readonly GetAllowedSubstitutesUseCase _getAllowedSubstitutesUseCase;
	private readonly ICurrentSessionService _currentSessionService;
	private readonly IUtcClock _clock;
	private readonly ILogger<MarkPartyBookingCompletedUseCase> _logger;

	public MarkPartyBookingCompletedUseCase(
		IPartyBookingRepository partyBookingRepository,
		ReserveBookingInventoryUseCase reserveBookingInventoryUseCase,
		GetAllowedSubstitutesUseCase getAllowedSubstitutesUseCase,
		ICurrentSessionService currentSessionService,
		IUtcClock clock,
		ILogger<MarkPartyBookingCompletedUseCase> logger)
	{
		_partyBookingRepository = partyBookingRepository;
		_reserveBookingInventoryUseCase = reserveBookingInventoryUseCase;
		_getAllowedSubstitutesUseCase = getAllowedSubstitutesUseCase;
		_currentSessionService = currentSessionService;
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

		var constraints = await _reserveBookingInventoryUseCase.EvaluateConstraintsAsync(command.BookingId, ct);
		if (constraints.Count > 0)
		{
			var role = _currentSessionService.GetCurrent()?.Role ?? StaffRole.Manager;
			var substitutes = await _getAllowedSubstitutesUseCase.ExecuteAsync(
				new GetAllowedSubstitutesQuery(
					command.BookingId,
					role,
					constraints.Select(x => x.OptionId).ToArray()),
				ct);

			var guidance = BuildConstraintGuidance(constraints, substitutes.Payload ?? []);
			return AppResult<MarkPartyBookingCompletedResultDto>.Failure(
				PartyBookingConstants.ErrorInventoryFinalizationBlocked,
				$"{PartyBookingConstants.SafeInventoryFinalizationBlockedMessage} {guidance}");
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

	private static string BuildConstraintGuidance(
		IReadOnlyList<InventoryConstraintDto> constraints,
		IReadOnlyList<AllowedSubstituteOptionDto> substitutes)
	{
		var constraintText = string.Join(
			"; ",
			constraints.Select(x => $"{x.OptionId} short by {x.DeficitQuantity}"));

		if (substitutes.Count == 0)
		{
			return $"Constrained items: {constraintText}.";
		}

		var substituteText = string.Join(
			"; ",
			substitutes
				.GroupBy(x => x.SourceOptionId, StringComparer.Ordinal)
				.OrderBy(x => x.Key, StringComparer.Ordinal)
				.Select(x => $"{x.Key} -> {string.Join(", ", x.Select(y => y.DisplayName))}"));

		return $"Constrained items: {constraintText}. Allowed substitutes: {substituteText}.";
	}
}

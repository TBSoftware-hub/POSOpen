using Microsoft.EntityFrameworkCore;
using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.UseCases.Inventory;
using POSOpen.Domain.Entities;
using POSOpen.Domain.Enums;

namespace POSOpen.Infrastructure.Persistence.Repositories;

public sealed class InventoryReservationRepository : IInventoryReservationRepository
{
	private readonly IDbContextFactory<PosOpenDbContext> _dbContextFactory;

	public InventoryReservationRepository(IDbContextFactory<PosOpenDbContext> dbContextFactory)
	{
		_dbContextFactory = dbContextFactory;
	}

	public async Task<IReadOnlyList<InventoryReservation>> ListActiveByBookingAsync(Guid bookingId, CancellationToken ct = default)
	{
		await using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
		return await dbContext.Set<InventoryReservation>()
			.AsNoTracking()
			.Where(x => x.BookingId == bookingId && x.ReservationState == InventoryReservationState.Reserved)
			.OrderBy(x => x.OptionId)
			.ThenBy(x => x.ReservedAtUtc)
			.ToListAsync(ct);
	}

	public async Task<IReadOnlyDictionary<string, int>> GetActiveReservedTotalsByOptionAsync(
		IReadOnlyCollection<string> optionIds,
		Guid? excludingBookingId = null,
		CancellationToken ct = default)
	{
		if (optionIds.Count == 0)
		{
			return new Dictionary<string, int>(StringComparer.Ordinal);
		}

		await using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
		var query = dbContext.Set<InventoryReservation>()
			.AsNoTracking()
			.Where(x => x.ReservationState == InventoryReservationState.Reserved)
			.Where(x => optionIds.Contains(x.OptionId));

		if (excludingBookingId.HasValue)
		{
			query = query.Where(x => x.BookingId != excludingBookingId.Value);
		}

		var totals = await query
			.GroupBy(x => x.OptionId)
			.Select(x => new { x.Key, Quantity = x.Sum(y => y.QuantityReserved) })
			.ToListAsync(ct);

		return totals.ToDictionary(x => x.Key, x => x.Quantity, StringComparer.Ordinal);
	}

	public async Task<IReadOnlyList<InventoryReservation>> PersistReservationPlanAsync(
		Guid bookingId,
		IReadOnlyDictionary<string, int> reserveQuantitiesByOption,
		Guid operationId,
		Guid correlationId,
		DateTime occurredUtc,
		CancellationToken ct = default)
	{
		await using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
		await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);

		try
		{
			var booking = await dbContext.Set<PartyBooking>()
				.FirstAsync(x => x.Id == bookingId, ct);

			if (booking.LastInventoryReserveOperationId == operationId)
			{
				await transaction.CommitAsync(ct);
				return await dbContext.Set<InventoryReservation>()
					.AsNoTracking()
					.Where(x => x.BookingId == bookingId && x.ReservationState == InventoryReservationState.Reserved)
					.OrderBy(x => x.OptionId)
					.ThenBy(x => x.ReservedAtUtc)
					.ToListAsync(ct);
			}

			var active = await dbContext.Set<InventoryReservation>()
				.Where(x => x.BookingId == bookingId && x.ReservationState == InventoryReservationState.Reserved)
				.ToListAsync(ct);

			var occurredAtUtc = DateTime.SpecifyKind(occurredUtc, DateTimeKind.Utc);
			foreach (var row in active)
			{
				row.ReservationState = InventoryReservationState.Released;
				row.ReleasedAtUtc = occurredAtUtc;
				row.ReleaseOperationId = operationId;
				row.ReleaseReasonCode = "booking-reserve-recalculated";
			}

			foreach (var entry in reserveQuantitiesByOption.Where(x => x.Value > 0))
			{
				dbContext.Set<InventoryReservation>().Add(new InventoryReservation
				{
					ReservationId = Guid.NewGuid(),
					BookingId = bookingId,
					OptionId = entry.Key,
					QuantityReserved = entry.Value,
					ReservationState = InventoryReservationState.Reserved,
					ReservedAtUtc = occurredAtUtc,
					ReservationOperationId = operationId,
				});
			}

			booking.LastInventoryReserveOperationId = operationId;
			booking.OperationId = operationId;
			booking.CorrelationId = correlationId;
			booking.UpdatedAtUtc = occurredAtUtc;

			await dbContext.SaveChangesAsync(ct);
			await transaction.CommitAsync(ct);

			return await dbContext.Set<InventoryReservation>()
				.AsNoTracking()
				.Where(x => x.BookingId == bookingId && x.ReservationState == InventoryReservationState.Reserved)
				.OrderBy(x => x.OptionId)
				.ToListAsync(ct);
		}
		catch
		{
			await transaction.RollbackAsync(ct);
			throw;
		}
	}

	public async Task<InventoryReleasePersistenceResult> ReleaseByTriggerAsync(
		Guid bookingId,
		InventoryReleaseTrigger trigger,
		Guid operationId,
		Guid correlationId,
		DateTime occurredUtc,
		IReadOnlyCollection<string>? removedOptionIds = null,
		IReadOnlyDictionary<string, int>? quantityReductionByOption = null,
		CancellationToken ct = default)
	{
		await using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
		await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);

		try
		{
			var booking = await dbContext.Set<PartyBooking>()
				.FirstAsync(x => x.Id == bookingId, ct);
			if (booking.LastInventoryReleaseOperationId == operationId)
			{
				await transaction.CommitAsync(ct);
				return new InventoryReleasePersistenceResult(
					0,
					await GetActiveSummariesAsync(dbContext, bookingId, ct));
			}

			var active = await dbContext.Set<InventoryReservation>()
				.Where(x => x.BookingId == bookingId && x.ReservationState == InventoryReservationState.Reserved)
				.OrderBy(x => x.OptionId)
				.ThenBy(x => x.ReservedAtUtc)
				.ToListAsync(ct);

			var releasedCount = 0;
			var occurredAtUtc = DateTime.SpecifyKind(occurredUtc, DateTimeKind.Utc);
			var reasonCode = ToReasonCode(trigger);

			if (trigger == InventoryReleaseTrigger.BookingCancelled || trigger == InventoryReleaseTrigger.BookingDateOrSlotChanged)
			{
				releasedCount += ReleaseRows(active, occurredAtUtc, operationId, reasonCode);
			}
			else if (trigger == InventoryReleaseTrigger.BookingItemRemoved)
			{
				var removed = removedOptionIds ?? [];
				releasedCount += ReleaseRows(
					active.Where(x => removed.Contains(x.OptionId, StringComparer.Ordinal)).ToList(),
					occurredAtUtc,
					operationId,
					reasonCode);
			}
			else if (trigger == InventoryReleaseTrigger.BookingItemQuantityReduced)
			{
				var reductions = quantityReductionByOption ?? new Dictionary<string, int>(StringComparer.Ordinal);
				foreach (var reduction in reductions.Where(x => x.Value > 0))
				{
					var remaining = reduction.Value;
					var rows = active.Where(x => string.Equals(x.OptionId, reduction.Key, StringComparison.Ordinal)).ToList();
					foreach (var row in rows)
					{
						if (remaining <= 0)
						{
							break;
						}

						if (row.QuantityReserved <= remaining)
						{
							row.ReservationState = InventoryReservationState.Released;
							row.ReleasedAtUtc = occurredAtUtc;
							row.ReleaseOperationId = operationId;
							row.ReleaseReasonCode = reasonCode;
							releasedCount++;
							remaining -= row.QuantityReserved;
						}
						else
						{
							row.QuantityReserved -= remaining;
							dbContext.Set<InventoryReservation>().Add(new InventoryReservation
							{
								ReservationId = Guid.NewGuid(),
								BookingId = bookingId,
								OptionId = row.OptionId,
								QuantityReserved = remaining,
								ReservationState = InventoryReservationState.Released,
								ReservedAtUtc = row.ReservedAtUtc,
								ReleasedAtUtc = occurredAtUtc,
								ReservationOperationId = row.ReservationOperationId,
								ReleaseOperationId = operationId,
								ReleaseReasonCode = reasonCode,
							});
							releasedCount++;
							remaining = 0;
						}
					}
				}
			}

			if (trigger != InventoryReleaseTrigger.BookingUpdatedNonInventoryFields)
			{
				booking.LastInventoryReleaseOperationId = operationId;
				booking.OperationId = operationId;
				booking.CorrelationId = correlationId;
				booking.UpdatedAtUtc = occurredAtUtc;
			}

			await dbContext.SaveChangesAsync(ct);
			await transaction.CommitAsync(ct);

			var activeSummaries = await GetActiveSummariesAsync(dbContext, bookingId, ct);
			return new InventoryReleasePersistenceResult(releasedCount, activeSummaries);
		}
		catch
		{
			await transaction.RollbackAsync(ct);
			throw;
		}
	}

	private static int ReleaseRows(
		IReadOnlyList<InventoryReservation> rows,
		DateTime occurredAtUtc,
		Guid operationId,
		string reasonCode)
	{
		var released = 0;
		foreach (var row in rows)
		{
			if (row.ReservationState != InventoryReservationState.Reserved)
			{
				continue;
			}

			row.ReservationState = InventoryReservationState.Released;
			row.ReleasedAtUtc = occurredAtUtc;
			row.ReleaseOperationId = operationId;
			row.ReleaseReasonCode = reasonCode;
			released++;
		}

		return released;
	}

	private static async Task<IReadOnlyList<InventoryReservationSummaryDto>> GetActiveSummariesAsync(
		PosOpenDbContext dbContext,
		Guid bookingId,
		CancellationToken ct)
	{
		return await dbContext.Set<InventoryReservation>()
			.AsNoTracking()
			.Where(x => x.BookingId == bookingId && x.ReservationState == InventoryReservationState.Reserved)
			.GroupBy(x => x.OptionId)
			.OrderBy(x => x.Key)
			.Select(x => new InventoryReservationSummaryDto(x.Key, x.Sum(y => y.QuantityReserved)))
			.ToArrayAsync(ct);
	}

	private static string ToReasonCode(InventoryReleaseTrigger trigger) => trigger switch
	{
		InventoryReleaseTrigger.BookingCancelled => "booking-cancelled",
		InventoryReleaseTrigger.BookingItemRemoved => "booking-item-removed",
		InventoryReleaseTrigger.BookingItemQuantityReduced => "booking-item-quantity-reduced",
		InventoryReleaseTrigger.BookingDateOrSlotChanged => "booking-date-or-slot-changed",
		_ => "booking-updated-non-inventory-fields",
	};
}

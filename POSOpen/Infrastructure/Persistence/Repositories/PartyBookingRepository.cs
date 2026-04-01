using Microsoft.EntityFrameworkCore;
using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Domain.Entities;
using POSOpen.Domain.Enums;

namespace POSOpen.Infrastructure.Persistence.Repositories;

public sealed class PartyBookingRepository : IPartyBookingRepository
{
private readonly IDbContextFactory<PosOpenDbContext> _dbContextFactory;

public PartyBookingRepository(IDbContextFactory<PosOpenDbContext> dbContextFactory)
{
_dbContextFactory = dbContextFactory;
}

public async Task<PartyBooking?> GetByIdAsync(Guid bookingId, CancellationToken ct = default)
{
await using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
return await dbContext.Set<PartyBooking>()
.AsNoTracking()
.FirstOrDefaultAsync(x => x.Id == bookingId, ct);
}

public async Task<PartyBooking?> GetByOperationIdAsync(Guid operationId, CancellationToken ct = default)
{
await using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
return await dbContext.Set<PartyBooking>()
.AsNoTracking()
.FirstOrDefaultAsync(x => x.OperationId == operationId, ct);
}

public async Task<IReadOnlyList<PartyBooking>> ListByPartyDateAsync(DateTime partyDateUtc, CancellationToken ct = default)
{
var dateUtc = DateTime.SpecifyKind(partyDateUtc, DateTimeKind.Utc);
var nextDateUtc = dateUtc.Date.AddDays(1);

await using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
return await dbContext.Set<PartyBooking>()
.AsNoTracking()
.Where(x => x.PartyDateUtc >= dateUtc.Date && x.PartyDateUtc < nextDateUtc)
.OrderBy(x => x.SlotId)
.ToListAsync(ct);
}

public async Task<bool> IsSlotUnavailableAsync(DateTime partyDateUtc, string slotId, Guid? excludingBookingId = null, CancellationToken ct = default)
{
var dateUtc = DateTime.SpecifyKind(partyDateUtc, DateTimeKind.Utc).Date;
var nextDateUtc = dateUtc.AddDays(1);

await using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
var query = dbContext.Set<PartyBooking>()
.AsNoTracking()
.Where(x => x.PartyDateUtc >= dateUtc && x.PartyDateUtc < nextDateUtc)
.Where(x => x.SlotId == slotId)
.Where(x => x.Status != PartyBookingStatus.Cancelled);

if (excludingBookingId.HasValue)
{
query = query.Where(x => x.Id != excludingBookingId.Value);
}

return await query.AnyAsync(ct);
}

public async Task<PartyBooking> UpsertDraftAsync(PartyBooking booking, CancellationToken ct = default)
{
await using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);
try
{
var dateUtc = DateTime.SpecifyKind(booking.PartyDateUtc, DateTimeKind.Utc).Date;
var nextDateUtc = dateUtc.AddDays(1);
var slotConflict = await dbContext.Set<PartyBooking>()
.Where(x => x.PartyDateUtc >= dateUtc && x.PartyDateUtc < nextDateUtc
&& x.SlotId == booking.SlotId
&& x.Status != PartyBookingStatus.Cancelled
&& x.Id != booking.Id)
.AnyAsync(ct);

if (slotConflict)
{
throw new Microsoft.EntityFrameworkCore.DbUpdateException(
"UNIQUE constraint failed: slot is already reserved for this date and time.",
new InvalidOperationException("Slot conflict detected within transaction."));
}

var existing = await dbContext.Set<PartyBooking>().FirstOrDefaultAsync(x => x.Id == booking.Id, ct);
if (existing is null)
{
dbContext.Set<PartyBooking>().Add(booking);
}
else
{
existing.PartyDateUtc = booking.PartyDateUtc;
existing.SlotId = booking.SlotId;
existing.PackageId = booking.PackageId;
existing.Status = PartyBookingStatus.Draft;
existing.OperationId = booking.OperationId;
existing.CorrelationId = booking.CorrelationId;
existing.UpdatedAtUtc = booking.UpdatedAtUtc;
}

await dbContext.SaveChangesAsync(ct);
await transaction.CommitAsync(ct);
return existing ?? booking;
}
catch
{
await transaction.RollbackAsync(ct);
throw;
}
}

public async Task<PartyBooking> ConfirmAsync(
PartyBooking booking,
Guid operationId,
Guid correlationId,
DateTime bookedAtUtc,
CancellationToken ct = default)
{
await using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);
try
{
var existing = await dbContext.Set<PartyBooking>().FirstAsync(x => x.Id == booking.Id, ct);
		existing.Status = PartyBookingStatus.Booked;
		existing.OperationId = operationId;
		existing.CorrelationId = correlationId;
		existing.BookedAtUtc = bookedAtUtc;
		existing.UpdatedAtUtc = bookedAtUtc;
		await dbContext.SaveChangesAsync(ct);
		await transaction.CommitAsync(ct);
		return existing;
	}
	catch
	{
		await transaction.RollbackAsync(ct);
		throw;
	}
}

public async Task<PartyBooking> RecordDepositCommitmentAsync(
	PartyBooking booking,
	long depositAmountCents,
	string depositCurrency,
	Guid operationId,
	Guid correlationId,
	DateTime committedAtUtc,
	CancellationToken ct = default)
{
	await using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
	await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);
	try
	{
		var existing = await dbContext.Set<PartyBooking>().FirstAsync(x => x.Id == booking.Id, ct);
		if (existing.DepositCommitmentOperationId == operationId)
		{
			await transaction.CommitAsync(ct);
			return existing;
		}

		existing.RecordDepositCommitment(
			depositAmountCents,
			depositCurrency,
			operationId,
			correlationId,
			DateTime.SpecifyKind(committedAtUtc, DateTimeKind.Utc));

		await dbContext.SaveChangesAsync(ct);
		await transaction.CommitAsync(ct);
		return existing;
	}
	catch
	{
		await transaction.RollbackAsync(ct);
		throw;
	}
}

public async Task<PartyBooking> MarkCompletedAsync(
	PartyBooking booking,
	Guid operationId,
	Guid correlationId,
	DateTime completedAtUtc,
	CancellationToken ct = default)
{
	await using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
	await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);
	try
	{
		var existing = await dbContext.Set<PartyBooking>().FirstAsync(x => x.Id == booking.Id, ct);
		if (existing.CompletedAtUtc.HasValue)
		{
			await transaction.CommitAsync(ct);
			return existing;
		}

		existing.MarkCompleted(
			operationId,
			correlationId,
			DateTime.SpecifyKind(completedAtUtc, DateTimeKind.Utc));

		await dbContext.SaveChangesAsync(ct);
		await transaction.CommitAsync(ct);
		return existing;
	}
	catch
	{
		await transaction.RollbackAsync(ct);
		throw;
	}
}
}

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.Abstractions.Services;
using POSOpen.Domain.Entities;
using POSOpen.Shared.Serialization;

namespace POSOpen.Infrastructure.Persistence.Repositories;

public sealed class AdmissionCheckInRepository : IAdmissionCheckInRepository
{
	private readonly IDbContextFactory<PosOpenDbContext> _dbContextFactory;
	private readonly IUtcClock _clock;

	public AdmissionCheckInRepository(IDbContextFactory<PosOpenDbContext> dbContextFactory, IUtcClock clock)
	{
		_dbContextFactory = dbContextFactory;
		_clock = clock;
	}

	public async Task<AdmissionCheckInRecord> SaveCompletionAsync(
		AdmissionCheckInPersistenceRequest request,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(request);
		ArgumentException.ThrowIfNullOrWhiteSpace(request.OperationLogEventType);

		await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
		await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

		try
		{
			var existing = await dbContext.AdmissionCheckInRecords
				.AsNoTracking()
				.SingleOrDefaultAsync(x => x.OperationId == request.Record.OperationId, cancellationToken);

			if (existing is null)
			{
				dbContext.AdmissionCheckInRecords.Add(request.Record);
			}
			else
			{
				request = request with { Record = existing };
			}

			var operationLogExists = await dbContext.OperationLogEntries.AnyAsync(
				entry => entry.OperationId == request.OperationContext.OperationId && entry.EventType == request.OperationLogEventType,
				cancellationToken);

			if (!operationLogExists)
			{
				dbContext.OperationLogEntries.Add(new OperationLogEntry
				{
					Id = Guid.NewGuid(),
					EventId = Guid.NewGuid().ToString("N"),
					EventType = request.OperationLogEventType,
					AggregateId = request.Record.FamilyId.ToString(),
					OperationId = request.OperationContext.OperationId,
					CorrelationId = request.OperationContext.CorrelationId,
					CausationId = request.OperationContext.CausationId,
					Version = 1,
					PayloadJson = JsonSerializer.Serialize(request.OperationLogPayload, AppJsonSerializerOptions.Default),
					OccurredUtc = request.OperationContext.OccurredUtc,
					RecordedUtc = _clock.UtcNow
				});
			}

			await dbContext.SaveChangesAsync(cancellationToken);
			await transaction.CommitAsync(cancellationToken);
			return request.Record;
		}
		catch
		{
			await transaction.RollbackAsync(cancellationToken);
			throw;
		}
	}

	public async Task<AdmissionCheckInRecord?> GetByOperationIdAsync(
		Guid operationId,
		CancellationToken cancellationToken = default)
	{
		await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
		return await dbContext.AdmissionCheckInRecords
			.AsNoTracking()
			.SingleOrDefaultAsync(record => record.OperationId == operationId, cancellationToken);
	}
}

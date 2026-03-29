using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.Abstractions.Services;
using POSOpen.Domain.Entities;
using POSOpen.Shared.Operational;
using POSOpen.Shared.Serialization;

namespace POSOpen.Infrastructure.Persistence.Repositories;

public sealed class OperationLogRepository : IOperationLogRepository
{
	private readonly IDbContextFactory<PosOpenDbContext> _dbContextFactory;
	private readonly IUtcClock _clock;

	public OperationLogRepository(IDbContextFactory<PosOpenDbContext> dbContextFactory, IUtcClock clock)
	{
		_dbContextFactory = dbContextFactory;
		_clock = clock;
	}

	public async Task<OperationLogEntry> AppendAsync<TPayload>(
		string eventType,
		string aggregateId,
		TPayload payload,
		OperationContext operationContext,
		int version = 1,
		CancellationToken cancellationToken = default)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
		ArgumentException.ThrowIfNullOrWhiteSpace(aggregateId);

		var entry = new OperationLogEntry
		{
			Id = Guid.NewGuid(),
			EventId = Guid.NewGuid().ToString("N"),
			EventType = eventType,
			AggregateId = aggregateId,
			OperationId = operationContext.OperationId,
			CorrelationId = operationContext.CorrelationId,
			CausationId = operationContext.CausationId,
			Version = version,
			PayloadJson = JsonSerializer.Serialize(payload, AppJsonSerializerOptions.Default),
			OccurredUtc = operationContext.OccurredUtc,
			RecordedUtc = _clock.UtcNow
		};

		await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
		dbContext.OperationLogEntries.Add(entry);
		await dbContext.SaveChangesAsync(cancellationToken);
		return entry;
	}

	public async Task<IReadOnlyList<OperationLogEntry>> ListAsync(CancellationToken cancellationToken = default)
	{
		await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
		return await dbContext.OperationLogEntries
			.AsNoTracking()
			.OrderBy(entry => entry.RecordedUtc)
			.ToListAsync(cancellationToken);
	}

	public async Task<IReadOnlyList<OperationLogEntry>> ListByEventTypesAsync(
		IReadOnlyList<string> eventTypes,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(eventTypes);

		await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
		return await dbContext.OperationLogEntries
			.AsNoTracking()
			.Where(entry => eventTypes.Contains(entry.EventType))
			.OrderBy(entry => entry.RecordedUtc)
			.ToListAsync(cancellationToken);
	}
}
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.Abstractions.Services;
using POSOpen.Domain.Entities;
using POSOpen.Shared.Operational;
using POSOpen.Shared.Serialization;

namespace POSOpen.Infrastructure.Persistence.Repositories;

public sealed class OutboxRepository : IOutboxRepository
{
	private readonly IDbContextFactory<PosOpenDbContext> _dbContextFactory;
	private readonly IUtcClock _clock;

	public OutboxRepository(IDbContextFactory<PosOpenDbContext> dbContextFactory, IUtcClock clock)
	{
		_dbContextFactory = dbContextFactory;
		_clock = clock;
	}

	public async Task<OutboxMessage> EnqueueAsync<TPayload>(
		string eventType,
		string aggregateId,
		TPayload payload,
		OperationContext operationContext,
		CancellationToken cancellationToken = default)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
		ArgumentException.ThrowIfNullOrWhiteSpace(aggregateId);

		var message = new OutboxMessage
		{
			Id = Guid.NewGuid(),
			MessageId = Guid.NewGuid().ToString("N"),
			EventType = eventType,
			AggregateId = aggregateId,
			OperationId = operationContext.OperationId,
			CorrelationId = operationContext.CorrelationId,
			CausationId = operationContext.CausationId,
			PayloadJson = JsonSerializer.Serialize(payload, AppJsonSerializerOptions.Default),
			OccurredUtc = operationContext.OccurredUtc,
			EnqueuedUtc = _clock.UtcNow
		};

		await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
		dbContext.OutboxMessages.Add(message);
		await dbContext.SaveChangesAsync(cancellationToken);
		return message;
	}

	public async Task<IReadOnlyList<OutboxMessage>> ListPendingAsync(CancellationToken cancellationToken = default)
	{
		await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
		return await dbContext.OutboxMessages
			.AsNoTracking()
			.Where(message => message.PublishedUtc == null)
			.OrderBy(message => message.EnqueuedUtc)
			.ToListAsync(cancellationToken);
	}
}
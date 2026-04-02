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
		Guid actorStaffId,
		CancellationToken cancellationToken = default)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
		ArgumentException.ThrowIfNullOrWhiteSpace(aggregateId);
		if (actorStaffId == Guid.Empty)
		{
			throw new ArgumentException("Actor staff id is required.", nameof(actorStaffId));
		}

		await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
		await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

		try
		{
			await dbContext.Database.ExecuteSqlRawAsync(
				"INSERT INTO OutboxQueueSequenceAllocations DEFAULT VALUES;",
				cancellationToken);

			var queueSequence = await dbContext.Database
				.SqlQueryRaw<long>("SELECT last_insert_rowid() AS Value")
				.SingleAsync(cancellationToken);

			var message = new OutboxMessage
			{
				Id = Guid.NewGuid(),
				MessageId = Guid.NewGuid().ToString("N"),
				EventType = eventType,
				AggregateId = aggregateId,
				OperationId = operationContext.OperationId,
				CorrelationId = operationContext.CorrelationId,
				CausationId = operationContext.CausationId,
				ActorStaffId = actorStaffId,
				PayloadJson = JsonSerializer.Serialize(payload, AppJsonSerializerOptions.Default),
				OccurredUtc = operationContext.OccurredUtc,
				EnqueuedUtc = _clock.UtcNow,
				QueueSequence = queueSequence
			};

			dbContext.OutboxMessages.Add(message);
			await dbContext.SaveChangesAsync(cancellationToken);
			await transaction.CommitAsync(cancellationToken);
			return message;
		}
		catch
		{
			await transaction.RollbackAsync(cancellationToken);
			throw;
		}
	}

	public async Task<IReadOnlyList<OutboxMessage>> ListPendingAsync(CancellationToken cancellationToken = default)
	{
		await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
		return await dbContext.OutboxMessages
			.AsNoTracking()
			.Where(message => message.PublishedUtc == null)
			.OrderBy(message => message.QueueSequence)
			.ThenBy(message => message.EnqueuedUtc)
			.ToListAsync(cancellationToken);
	}
}
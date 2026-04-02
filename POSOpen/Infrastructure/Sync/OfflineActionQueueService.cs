using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.Abstractions.Services;
using POSOpen.Application.UseCases.Sync;

namespace POSOpen.Infrastructure.Sync;

public sealed class OfflineActionQueueService : IOfflineActionQueueService
{
	private readonly IOutboxRepository _outboxRepository;

	public OfflineActionQueueService(IOutboxRepository outboxRepository)
	{
		_outboxRepository = outboxRepository;
	}

	public async Task<QueueOfflineActionResultDto> QueueAsync(
		QueueOfflineActionCommand command,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(command);
		ArgumentException.ThrowIfNullOrWhiteSpace(command.EventType);
		ArgumentException.ThrowIfNullOrWhiteSpace(command.AggregateId);

		if (command.ActorStaffId == Guid.Empty)
		{
			throw new ArgumentException("Actor staff id is required.", nameof(command));
		}

		ArgumentNullException.ThrowIfNull(command.PayloadSnapshot);

		var message = await _outboxRepository.EnqueueAsync(
			command.EventType,
			command.AggregateId,
			command.PayloadSnapshot,
			command.OperationContext,
			command.ActorStaffId,
			cancellationToken);

		return new QueueOfflineActionResultDto(
			message.MessageId,
			message.OperationId,
			message.CorrelationId,
			message.EnqueuedUtc,
			message.QueueSequence);
	}
}
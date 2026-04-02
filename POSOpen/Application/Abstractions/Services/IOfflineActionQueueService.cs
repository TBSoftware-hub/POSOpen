using POSOpen.Application.UseCases.Sync;

namespace POSOpen.Application.Abstractions.Services;

public interface IOfflineActionQueueService
{
	Task<QueueOfflineActionResultDto> QueueAsync(
		QueueOfflineActionCommand command,
		CancellationToken cancellationToken = default);
}
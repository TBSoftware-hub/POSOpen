using FluentAssertions;
using Moq;
using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.UseCases.Sync;
using POSOpen.Domain.Entities;
using POSOpen.Infrastructure.Sync;
using POSOpen.Shared.Operational;

namespace POSOpen.Tests.Unit.Sync;

public sealed class OfflineActionQueueServiceTests
{
	[Fact]
	public async Task QueueAsync_maps_metadata_and_returns_queue_result()
	{
		var outboxRepository = new Mock<IOutboxRepository>();
		var operationContext = new OperationContext(Guid.NewGuid(), Guid.NewGuid(), null, DateTime.UtcNow);
		var actorStaffId = Guid.NewGuid();
		OutboxMessage? persistedMessage = null;

		outboxRepository
			.Setup(x => x.EnqueueAsync(
				"AdmissionPaymentDeferred",
				"family-42",
				It.IsAny<object>(),
				operationContext,
				actorStaffId,
				It.IsAny<CancellationToken>()))
			.Callback<string, string, object, OperationContext, Guid, CancellationToken>((_, _, payload, _, _, _) =>
			{
				persistedMessage = new OutboxMessage
				{
					Id = Guid.NewGuid(),
					MessageId = "m-42",
					EventType = "AdmissionPaymentDeferred",
					AggregateId = "family-42",
					OperationId = operationContext.OperationId,
					CorrelationId = operationContext.CorrelationId,
					CausationId = operationContext.CausationId,
					ActorStaffId = actorStaffId,
					PayloadJson = "{}",
					OccurredUtc = operationContext.OccurredUtc,
					EnqueuedUtc = DateTime.UtcNow,
					QueueSequence = 33
				};
			})
			.ReturnsAsync(() => persistedMessage!);

		var service = new OfflineActionQueueService(outboxRepository.Object);

		var result = await service.QueueAsync(new QueueOfflineActionCommand(
			"AdmissionPaymentDeferred",
			"family-42",
			actorStaffId,
			new { amountCents = 2500, currencyCode = "USD" },
			operationContext));

		result.MessageId.Should().Be("m-42");
		result.OperationId.Should().Be(operationContext.OperationId);
		result.CorrelationId.Should().Be(operationContext.CorrelationId);
		result.QueueSequence.Should().Be(33);
	}

	[Fact]
	public async Task QueueAsync_with_missing_actor_throws_argument_exception()
	{
		var service = new OfflineActionQueueService(Mock.Of<IOutboxRepository>());
		var operationContext = new OperationContext(Guid.NewGuid(), Guid.NewGuid(), null, DateTime.UtcNow);

		var act = () => service.QueueAsync(new QueueOfflineActionCommand(
			"AdmissionPaymentDeferred",
			"family-42",
			Guid.Empty,
			new { amountCents = 2500 },
			operationContext));

		await act.Should().ThrowAsync<ArgumentException>();
	}
}

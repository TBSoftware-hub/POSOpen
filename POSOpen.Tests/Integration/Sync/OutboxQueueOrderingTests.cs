using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using POSOpen.Application.UseCases.Sync;
using POSOpen.Infrastructure.Persistence;
using POSOpen.Infrastructure.Persistence.Repositories;
using POSOpen.Infrastructure.Services;
using POSOpen.Infrastructure.Sync;

namespace POSOpen.Tests.Integration.Sync;

public sealed class OutboxQueueOrderingTests
{
	[Fact]
	public async Task QueueAsync_serial_captures_preserve_sequence_order()
	{
		var databasePath = TestDatabasePaths.Create();
		await using var dbContextFactory = new TestDbContextFactory(databasePath);
		var initializer = new AppDbContextInitializer(dbContextFactory, NullLogger<AppDbContextInitializer>.Instance);
		await initializer.InitializeAsync();

		var clock = new TestUtcClock(new DateTime(2026, 4, 2, 12, 0, 0, DateTimeKind.Utc));
		var operationContextFactory = new OperationContextFactory(clock);
		var service = new OfflineActionQueueService(new OutboxRepository(dbContextFactory, clock));
		var actorStaffId = Guid.NewGuid();

		var captured = new List<QueueOfflineActionResultDto>();
		for (var i = 0; i < 3; i++)
		{
			captured.Add(await service.QueueAsync(new QueueOfflineActionCommand(
				"AdmissionPaymentDeferred",
				$"family-{i}",
				actorStaffId,
				new { family = i },
				operationContextFactory.CreateRoot())));
		}

		var pending = await new OutboxRepository(dbContextFactory, clock).ListPendingAsync();

		pending.Should().HaveCount(3);
		pending.Select(message => message.QueueSequence).Should().BeInAscendingOrder();
		pending.Select(message => message.AggregateId).Should().ContainInOrder("family-0", "family-1", "family-2");
		captured.Select(result => result.QueueSequence).Should().ContainInOrder(pending.Select(message => message.QueueSequence));
	}

	[Fact]
	public async Task QueueAsync_parallel_captures_generate_unique_strictly_orderable_sequences()
	{
		var databasePath = TestDatabasePaths.Create();
		await using var dbContextFactory = new TestDbContextFactory(databasePath);
		var initializer = new AppDbContextInitializer(dbContextFactory, NullLogger<AppDbContextInitializer>.Instance);
		await initializer.InitializeAsync();

		var clock = new TestUtcClock(new DateTime(2026, 4, 2, 12, 30, 0, DateTimeKind.Utc));
		var operationContextFactory = new OperationContextFactory(clock);
		var service = new OfflineActionQueueService(new OutboxRepository(dbContextFactory, clock));
		var actorStaffId = Guid.NewGuid();

		var enqueueTasks = Enumerable.Range(0, 20)
			.Select(i => service.QueueAsync(new QueueOfflineActionCommand(
				"AdmissionPaymentDeferred",
				$"family-{i}",
				actorStaffId,
				new { family = i },
				operationContextFactory.CreateRoot())))
			.ToArray();

		await Task.WhenAll(enqueueTasks);
		var pending = await new OutboxRepository(dbContextFactory, clock).ListPendingAsync();

		pending.Should().HaveCount(20);
		var sequences = pending.Select(message => message.QueueSequence).ToArray();
		sequences.Should().OnlyHaveUniqueItems();
		sequences.Should().BeInAscendingOrder();
		sequences.Min().Should().BeGreaterThan(0);
	}
}

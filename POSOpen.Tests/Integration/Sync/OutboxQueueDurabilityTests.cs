using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using POSOpen.Application.Abstractions.Services;
using POSOpen.Application.UseCases.Sync;
using POSOpen.Infrastructure.Persistence;
using POSOpen.Infrastructure.Persistence.Repositories;
using POSOpen.Infrastructure.Services;
using POSOpen.Infrastructure.Sync;

namespace POSOpen.Tests.Integration.Sync;

public sealed class OutboxQueueDurabilityTests
{
	[Fact]
	public async Task Queue_records_remain_pending_after_dbcontext_recreation_and_restart_monitoring()
	{
		var databasePath = TestDatabasePaths.Create();
		await using var firstFactory = new TestDbContextFactory(databasePath);
		var initializer = new AppDbContextInitializer(firstFactory, NullLogger<AppDbContextInitializer>.Instance);
		await initializer.InitializeAsync();

		var clock = new TestUtcClock(new DateTime(2026, 4, 2, 13, 0, 0, DateTimeKind.Utc));
		var operationContextFactory = new OperationContextFactory(clock);
		var queueService = new OfflineActionQueueService(new OutboxRepository(firstFactory, clock));
		var actorStaffId = Guid.NewGuid();

		for (var i = 0; i < 2; i++)
		{
			await queueService.QueueAsync(new QueueOfflineActionCommand(
				"AdmissionPaymentDeferred",
				$"family-{i}",
				actorStaffId,
				new { family = i },
				operationContextFactory.CreateRoot()));
		}

		await using var secondFactory = new TestDbContextFactory(databasePath);
		var restartedOutboxRepository = new OutboxRepository(secondFactory, clock);
		var pendingAfterRestart = await restartedOutboxRepository.ListPendingAsync();

		pendingAfterRestart.Should().HaveCount(2);
		pendingAfterRestart.Select(message => message.QueueSequence).Should().BeInAscendingOrder();

		var connectivityService = new Mock<IConnectivityService>();
		connectivityService.SetupGet(x => x.IsConnected).Returns(false);
		var appState = new AppStateService();
		var monitor = new ConnectivityMonitorService(connectivityService.Object, appState, restartedOutboxRepository);

		monitor.Initialize();
		await Task.Delay(20);

		appState.SyncState.Should().Contain("2 actions pending sync");
	}
}

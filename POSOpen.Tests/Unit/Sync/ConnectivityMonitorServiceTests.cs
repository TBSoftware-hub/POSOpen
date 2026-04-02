using FluentAssertions;
using Moq;
using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.Abstractions.Services;
using POSOpen.Domain.Entities;
using POSOpen.Infrastructure.Services;

namespace POSOpen.Tests.Unit.Sync;

public sealed class ConnectivityMonitorServiceTests
{
	[Fact]
	public async Task Initialize_with_no_internet_sets_offline_and_populates_pending_sync_message()
	{
		var connectivity = new Mock<IConnectivityService>();
		connectivity.SetupGet(x => x.IsConnected).Returns(false);

		var appState = new AppStateService();
		var outbox = new Mock<IOutboxRepository>();
		outbox.Setup(x => x.ListPendingAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(
			[
				new OutboxMessage { Id = Guid.NewGuid(), MessageId = "m1", EventType = "evt", AggregateId = "agg", PayloadJson = "{}", OccurredUtc = DateTime.UtcNow, EnqueuedUtc = DateTime.UtcNow },
				new OutboxMessage { Id = Guid.NewGuid(), MessageId = "m2", EventType = "evt", AggregateId = "agg", PayloadJson = "{}", OccurredUtc = DateTime.UtcNow, EnqueuedUtc = DateTime.UtcNow },
			]);

		var monitor = new ConnectivityMonitorService(connectivity.Object, appState, outbox.Object);

		monitor.Initialize();
		await Task.Delay(20);

		appState.IsOffline.Should().BeTrue();
		appState.OfflineSince.Should().NotBeNull();
		appState.SyncState.Should().Contain("2 actions pending sync");
	}

	[Fact]
	public async Task Initialize_with_internet_sets_online_and_no_offline_timestamp()
	{
		var connectivity = new Mock<IConnectivityService>();
		connectivity.SetupGet(x => x.IsConnected).Returns(true);

		var appState = new AppStateService();
		var outbox = new Mock<IOutboxRepository>();
		outbox.Setup(x => x.ListPendingAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync([]);

		var monitor = new ConnectivityMonitorService(connectivity.Object, appState, outbox.Object);

		monitor.Initialize();
		await Task.Delay(20);

		appState.IsOffline.Should().BeFalse();
		appState.OfflineSince.Should().BeNull();
	}

	[Fact]
	public async Task ConnectivityChanged_false_to_true_updates_offline_mode()
	{
		var connectivity = new Mock<IConnectivityService>();
		connectivity.SetupGet(x => x.IsConnected).Returns(false);

		var appState = new AppStateService();
		var outbox = new Mock<IOutboxRepository>();
		outbox.Setup(x => x.ListPendingAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync([]);

		var monitor = new ConnectivityMonitorService(connectivity.Object, appState, outbox.Object);
		monitor.Initialize();
		await Task.Delay(20);

		connectivity.Raise(c => c.ConnectivityChanged += null, connectivity.Object, true);
		await Task.Delay(20);

		appState.IsOffline.Should().BeFalse();
		appState.SyncState.Should().Be("Online - Reconnected");
	}
}

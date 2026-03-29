using System.Text.Json;
using FluentAssertions;
using POSOpen.Infrastructure.Persistence;
using POSOpen.Infrastructure.Persistence.Repositories;
using POSOpen.Shared.Serialization;

namespace POSOpen.Tests.Integration.Persistence;

public sealed class PersistenceRepositoryTests
{
	[Fact]
	public async Task Repositories_persist_append_only_operation_and_outbox_records_in_utc()
	{
		var databasePath = TestDatabasePaths.Create();
		await using var dbContextFactory = new TestDbContextFactory(databasePath);
		var clock = new TestUtcClock(new DateTime(2026, 3, 28, 12, 0, 0, DateTimeKind.Utc));
		var operationContextFactory = new POSOpen.Infrastructure.Services.OperationContextFactory(clock);
		var initializer = new AppDbContextInitializer(dbContextFactory, Microsoft.Extensions.Logging.Abstractions.NullLogger<AppDbContextInitializer>.Instance);
		await initializer.InitializeAsync();

		var operationRepository = new OperationLogRepository(dbContextFactory, clock);
		var outboxRepository = new OutboxRepository(dbContextFactory, clock);
		var operationContext = operationContextFactory.CreateRoot();

		var operationLogEntry = await operationRepository.AppendAsync(
			"TerminalOpened",
			"terminal-001",
			new { cashierId = "cashier-1", isDeferred = false },
			operationContext,
			version: 3);

		var outboxMessage = await outboxRepository.EnqueueAsync(
			"TerminalOpened",
			"terminal-001",
			new { cashierId = "cashier-1", isDeferred = false },
			operationContext);

		operationLogEntry.RecordedUtc.Kind.Should().Be(DateTimeKind.Utc);
		outboxMessage.EnqueuedUtc.Kind.Should().Be(DateTimeKind.Utc);

		var operationRows = await operationRepository.ListAsync();
		var pendingMessages = await outboxRepository.ListPendingAsync();

		operationRows.Should().HaveCount(1);
		pendingMessages.Should().HaveCount(1);
		operationRows[0].Version.Should().Be(3);
		operationRows[0].CorrelationId.Should().Be(operationContext.CorrelationId);
		pendingMessages[0].PublishedUtc.Should().BeNull();
		JsonDocument.Parse(operationRows[0].PayloadJson).RootElement.GetProperty("cashierId").GetString().Should().Be("cashier-1");
		JsonDocument.Parse(outboxMessage.PayloadJson).RootElement.GetProperty("isDeferred").GetBoolean().Should().BeFalse();
		JsonSerializer.Serialize(new { payload = "ok" }, AppJsonSerializerOptions.Default).Should().Contain("payload");
	}
}
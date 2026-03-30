using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.UseCases.Admissions;
using POSOpen.Domain.Entities;
using POSOpen.Domain.Enums;
using POSOpen.Infrastructure.Persistence;
using POSOpen.Infrastructure.Persistence.Repositories;
using POSOpen.Shared.Operational;

namespace POSOpen.Tests.Integration.Admissions;

public sealed class AdmissionCheckInRepositoryTests
{
	[Fact]
	public async Task SaveCompletionAsync_deferred_completion_persists_admission_log_and_outbox_with_same_operation_id()
	{
		await using var fixture = await CreateFixtureAsync();
		var operationContext = new OperationContext(Guid.NewGuid(), Guid.NewGuid(), null, DateTime.UtcNow);
		var record = AdmissionCheckInRecord.Create(
			Guid.NewGuid(),
			Guid.NewGuid(),
			operationContext.OperationId,
			AdmissionSettlementStatus.DeferredQueued,
			2500,
			"USD",
			operationContext.OccurredUtc,
			"CHK-123456",
			"ADM-123456");

		await fixture.Repository.SaveCompletionAsync(
			new AdmissionCheckInPersistenceRequest(
				record,
				CompleteAdmissionCheckInConstants.EventAdmissionPaymentQueued,
				new { operationId = operationContext.OperationId },
				operationContext,
				CompleteAdmissionCheckInConstants.EventAdmissionPaymentQueued,
				new { operationId = operationContext.OperationId }));

		await using var dbContext = await fixture.DbContextFactory.CreateDbContextAsync();
		var persistedAdmission = await dbContext.AdmissionCheckInRecords.SingleAsync();
		var persistedLog = await dbContext.OperationLogEntries.SingleAsync();
		var persistedOutbox = await dbContext.OutboxMessages.SingleAsync();

		persistedAdmission.OperationId.Should().Be(operationContext.OperationId);
		persistedLog.OperationId.Should().Be(operationContext.OperationId);
		persistedOutbox.OperationId.Should().Be(operationContext.OperationId);
		persistedAdmission.SettlementStatus.Should().Be(AdmissionSettlementStatus.DeferredQueued);
	}

	[Fact]
	public async Task SaveCompletionAsync_authorized_completion_does_not_enqueue_outbox_message()
	{
		await using var fixture = await CreateFixtureAsync();
		var operationContext = new OperationContext(Guid.NewGuid(), Guid.NewGuid(), null, DateTime.UtcNow);
		var record = AdmissionCheckInRecord.Create(
			Guid.NewGuid(),
			Guid.NewGuid(),
			operationContext.OperationId,
			AdmissionSettlementStatus.Authorized,
			2500,
			"USD",
			operationContext.OccurredUtc,
			"CHK-654321",
			"ADM-654321");

		await fixture.Repository.SaveCompletionAsync(
			new AdmissionCheckInPersistenceRequest(
				record,
				CompleteAdmissionCheckInConstants.EventAdmissionCompleted,
				new { operationId = operationContext.OperationId },
				operationContext,
				null,
				null));

		await using var dbContext = await fixture.DbContextFactory.CreateDbContextAsync();
		var admissionCount = await dbContext.AdmissionCheckInRecords.CountAsync();
		var operationLogCount = await dbContext.OperationLogEntries.CountAsync();
		var outboxCount = await dbContext.OutboxMessages.CountAsync();

		admissionCount.Should().Be(1);
		operationLogCount.Should().Be(1);
		outboxCount.Should().Be(0);
	}

	private static async Task<TestFixture> CreateFixtureAsync()
	{
		var databasePath = TestDatabasePaths.Create();
		var dbContextFactory = new TestDbContextFactory(databasePath);
		var initializer = new AppDbContextInitializer(dbContextFactory, NullLogger<AppDbContextInitializer>.Instance);
		await initializer.InitializeAsync();
		return new TestFixture(
			dbContextFactory,
			new AdmissionCheckInRepository(dbContextFactory, new TestUtcClock(DateTime.UtcNow)));
	}

	private sealed class TestFixture : IAsyncDisposable
	{
		public TestFixture(TestDbContextFactory dbContextFactory, AdmissionCheckInRepository repository)
		{
			DbContextFactory = dbContextFactory;
			Repository = repository;
		}

		public TestDbContextFactory DbContextFactory { get; }

		public AdmissionCheckInRepository Repository { get; }

		public async ValueTask DisposeAsync()
		{
			await DbContextFactory.DisposeAsync();
		}
	}
}

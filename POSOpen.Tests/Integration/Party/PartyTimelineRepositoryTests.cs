using System.Diagnostics;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using POSOpen.Application.UseCases.Party;
using POSOpen.Domain.Entities;
using POSOpen.Infrastructure.Persistence;
using POSOpen.Infrastructure.Persistence.Repositories;
using POSOpen.Shared.Operational;
using Microsoft.EntityFrameworkCore;
using POSOpen.Domain.Enums;

namespace POSOpen.Tests.Integration.Party;

public sealed class PartyTimelineRepositoryTests
{
	[Fact]
	public async Task TimelineRetrievalAndCompletionUpdate_MeetsNfr4UnderActiveDayProfile()
	{
		await using var fixture = await CreateFixtureAsync();
		var targetDateUtc = new DateTime(2026, 4, 20, 0, 0, 0, DateTimeKind.Utc);

		// Bulk-insert 1000 confirmed bookings directly via EF Core to keep test setup fast
		var bookings = Enumerable.Range(0, 1000).Select(i =>
		{
			var createdAt = fixture.Clock.UtcNow;
			return new PartyBooking
			{
				Id = Guid.NewGuid(),
				PartyDateUtc = targetDateUtc,
				SlotId = $"slot-{i:0000}",
				PackageId = "basic-party",
				Status = PartyBookingStatus.Booked,
				OperationId = Guid.NewGuid(),
				CorrelationId = Guid.NewGuid(),
				CreatedAtUtc = createdAt,
				UpdatedAtUtc = createdAt,
				BookedAtUtc = createdAt,
				DepositCommitmentStatus = PartyDepositCommitmentStatus.None,
			};
		}).ToList();

		var targetBookingId = bookings[500].Id;

		await using (var dbContext = await fixture.DbContextFactory.CreateDbContextAsync())
		{
			dbContext.Set<PartyBooking>().AddRange(bookings);
			await dbContext.SaveChangesAsync();
		}

		var timelineUseCase = new GetPartyBookingTimelineUseCase(
			fixture.Repository,
			fixture.Clock,
			NullLogger<GetPartyBookingTimelineUseCase>.Instance);
		var completionUseCase = new MarkPartyBookingCompletedUseCase(
			fixture.Repository,
			fixture.Clock,
			NullLogger<MarkPartyBookingCompletedUseCase>.Instance);

		var retrievalDurations = new long[20];
		var retrievalTasks = Enumerable.Range(0, 20)
			.Select(async i =>
			{
				var stopwatch = Stopwatch.StartNew();
				var result = await timelineUseCase.ExecuteAsync(targetBookingId);
				stopwatch.Stop();
				result.IsSuccess.Should().BeTrue($"timeline retrieval failed with error: {result.ErrorCode} / {result.UserMessage}");
				retrievalDurations[i] = stopwatch.ElapsedMilliseconds;
			})
			.ToArray();

		await Task.WhenAll(retrievalTasks);
		var sorted = retrievalDurations.OrderBy(x => x).ToArray();
		var p95 = sorted[(int)Math.Ceiling(sorted.Length * 0.95) - 1];
		var median = sorted[sorted.Length / 2];
		median.Should().BeLessThanOrEqualTo(3000, "median timeline retrieval must satisfy NFR4");
		p95.Should().BeLessThanOrEqualTo(3000, "P95 timeline retrieval must satisfy NFR4");

		var completionWatch = Stopwatch.StartNew();
		var completionResult = await completionUseCase.ExecuteAsync(
			new MarkPartyBookingCompletedCommand(targetBookingId, new OperationContext(Guid.NewGuid(), Guid.NewGuid(), null, fixture.Clock.UtcNow)));
		completionWatch.Stop();

		completionResult.IsSuccess.Should().BeTrue();
		completionWatch.ElapsedMilliseconds.Should().BeLessThanOrEqualTo(3000, "completion update must satisfy NFR4");
	}

	private static async Task<TestFixture> CreateFixtureAsync()
	{
		var databasePath = TestDatabasePaths.Create();
		var dbContextFactory = new TestDbContextFactory(databasePath);
		var initializer = new AppDbContextInitializer(dbContextFactory, NullLogger<AppDbContextInitializer>.Instance);
		await initializer.InitializeAsync();

		var clock = new TestUtcClock(new DateTime(2026, 4, 20, 9, 0, 0, DateTimeKind.Utc));
		var operationLogRepository = new OperationLogRepository(dbContextFactory, clock);
		var repository = new PartyBookingRepository(dbContextFactory);
		return new TestFixture(dbContextFactory, repository, clock, operationLogRepository);
	}

	private sealed class TestFixture : IAsyncDisposable
	{
		public TestFixture(TestDbContextFactory dbContextFactory, PartyBookingRepository repository, TestUtcClock clock, OperationLogRepository operationLogRepository)
		{
			DbContextFactory = dbContextFactory;
			Repository = repository;
			Clock = clock;
			OperationLogRepository = operationLogRepository;
		}

		public TestDbContextFactory DbContextFactory { get; }
		public PartyBookingRepository Repository { get; }
		public TestUtcClock Clock { get; }
		public OperationLogRepository OperationLogRepository { get; }

		public async ValueTask DisposeAsync()
		{
			await DbContextFactory.DisposeAsync();
		}
	}
}

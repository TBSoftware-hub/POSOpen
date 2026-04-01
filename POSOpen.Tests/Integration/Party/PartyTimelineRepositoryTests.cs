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
using POSOpen.Application.UseCases.Inventory;
using Moq;

namespace POSOpen.Tests.Integration.Party;

public sealed class PartyTimelineRepositoryTests
{
	[Fact]
	public async Task TimelineRetrievalAndCompletionUpdate_MeetsNfr4UnderActiveDayProfile()
	{
		if (!string.Equals(Environment.GetEnvironmentVariable("RUN_NFR_BENCHMARKS"), "true", StringComparison.OrdinalIgnoreCase))
		{
			return;
		}

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
			new ReserveBookingInventoryUseCase(
				fixture.Repository,
				new InventoryReservationRepository(fixture.DbContextFactory),
				NullLogger<ReserveBookingInventoryUseCase>.Instance),
			new GetAllowedSubstitutesUseCase(new Mock<POSOpen.Application.Abstractions.Services.IInventorySubstitutionPolicyProvider>().Object),
			new Mock<POSOpen.Application.Abstractions.Security.ICurrentSessionService>().Object,
			fixture.Clock,
			NullLogger<MarkPartyBookingCompletedUseCase>.Instance);

		// Warm up query paths and JIT before measuring to reduce cold-start timing noise.
		for (var i = 0; i < 3; i++)
		{
			var warmup = await timelineUseCase.ExecuteAsync(targetBookingId);
			warmup.IsSuccess.Should().BeTrue();
		}

		var retrievalWindowP95 = new List<long>(3);
		for (var window = 0; window < 3; window++)
		{
			var retrievalDurations = new long[30];
			await Parallel.ForEachAsync(
				Enumerable.Range(0, retrievalDurations.Length),
				new ParallelOptions { MaxDegreeOfParallelism = 6 },
				async (index, _) =>
				{
					var stopwatch = Stopwatch.StartNew();
					var result = await timelineUseCase.ExecuteAsync(targetBookingId);
					stopwatch.Stop();
					result.IsSuccess.Should().BeTrue($"timeline retrieval failed with error: {result.ErrorCode} / {result.UserMessage}");
					retrievalDurations[index] = stopwatch.ElapsedMilliseconds;
				});

			var sorted = retrievalDurations.OrderBy(x => x).ToArray();
			var p95 = sorted[(int)Math.Ceiling(sorted.Length * 0.95) - 1];
			retrievalWindowP95.Add(p95);
		}

		var retrievalStableP95 = retrievalWindowP95.OrderBy(x => x).ElementAt(retrievalWindowP95.Count / 2);
		retrievalStableP95.Should().BeLessThanOrEqualTo(4000, "timeline retrieval benchmark should remain within stable latency envelope on shared test hosts");
		retrievalWindowP95.Max().Should().BeLessThanOrEqualTo(5500, "timeline retrieval path should not show severe latency regression");

		var completionWindowP95 = new List<long>(3);
		for (var window = 0; window < 3; window++)
		{
			var completionDurations = new List<long>(10);
			foreach (var bookingId in bookings.Skip(window * 10).Take(10).Select(x => x.Id))
			{
				var completionWatch = Stopwatch.StartNew();
				var completionResult = await completionUseCase.ExecuteAsync(
					new MarkPartyBookingCompletedCommand(bookingId, new OperationContext(Guid.NewGuid(), Guid.NewGuid(), null, fixture.Clock.UtcNow)));
				completionWatch.Stop();

				completionResult.IsSuccess.Should().BeTrue();
				completionDurations.Add(completionWatch.ElapsedMilliseconds);
			}

			var completionSorted = completionDurations.OrderBy(x => x).ToArray();
			var completionP95 = completionSorted[(int)Math.Ceiling(completionSorted.Length * 0.95) - 1];
			completionWindowP95.Add(completionP95);
		}

		var completionStableP95 = completionWindowP95.OrderBy(x => x).ElementAt(completionWindowP95.Count / 2);
		completionStableP95.Should().BeLessThanOrEqualTo(4000, "completion path benchmark should remain within stable latency envelope on shared test hosts");
		completionWindowP95.Max().Should().BeLessThanOrEqualTo(5500, "completion path should not show severe latency regression");
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

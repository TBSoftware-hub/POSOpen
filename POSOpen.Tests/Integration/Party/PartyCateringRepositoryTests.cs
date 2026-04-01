using System.Diagnostics;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using POSOpen.Application.Abstractions.Services;
using POSOpen.Application.UseCases.Party;
using POSOpen.Domain.Entities;
using POSOpen.Domain.Enums;
using POSOpen.Infrastructure.Persistence;
using POSOpen.Infrastructure.Persistence.Repositories;

namespace POSOpen.Tests.Integration.Party;

public sealed class PartyCateringRepositoryTests
{
	private static readonly DateTime TestDateUtc = new(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);

	[Fact]
	public async Task ReplaceAddOnSelectionsAsync_ReplacesRowsAtomically()
	{
		await using var fixture = await CreateFixtureAsync();
		var booking = MakeBooking();

		await using (var db = await fixture.DbContextFactory.CreateDbContextAsync())
		{
			db.Set<PartyBooking>().Add(booking);
			db.Set<PartyBookingAddOnSelection>().Add(new PartyBookingAddOnSelection
			{
				Id = Guid.NewGuid(),
				BookingId = booking.Id,
				AddOnType = PartyAddOnType.Catering,
				OptionId = "pizza-basic",
				Quantity = 1,
				SelectedAtUtc = TestDateUtc,
				SelectionOperationId = Guid.NewGuid(),
			});
			await db.SaveChangesAsync();
		}

		var operationId = Guid.NewGuid();
		await fixture.Repository.ReplaceAddOnSelectionsAsync(
			booking,
			[
				new PartyBookingAddOnSelection
				{
					Id = Guid.NewGuid(),
					BookingId = booking.Id,
					AddOnType = PartyAddOnType.Decor,
					OptionId = "banner-custom",
					Quantity = 1,
					SelectedAtUtc = TestDateUtc,
					SelectionOperationId = operationId,
				},
			],
			operationId,
			Guid.NewGuid(),
			TestDateUtc);

		var loaded = await fixture.Repository.GetByIdWithSelectionsAsync(booking.Id);
		loaded.Should().NotBeNull();
		loaded!.AddOnSelections.Should().ContainSingle();
		loaded.AddOnSelections.Single().OptionId.Should().Be("banner-custom");
		loaded.LastAddOnUpdateOperationId.Should().Be(operationId);
	}

	[Fact]
	public async Task SelectionAndTimelinePath_MeetsNfr4P95()
	{
		if (!string.Equals(Environment.GetEnvironmentVariable("RUN_NFR_BENCHMARKS"), "true", StringComparison.OrdinalIgnoreCase))
		{
			return;
		}

		await using var fixture = await CreateFixtureAsync();
		var booking = MakeBooking();
		await using (var db = await fixture.DbContextFactory.CreateDbContextAsync())
		{
			db.Set<PartyBooking>().Add(booking);
			await db.SaveChangesAsync();
		}

		var timeline = new GetPartyBookingTimelineUseCase(
			fixture.Repository,
			new FixedUtcClock(TestDateUtc),
			NullLogger<GetPartyBookingTimelineUseCase>.Instance);

		// Warm up update + timeline path to reduce first-run variability.
		for (var i = 0; i < 3; i++)
		{
			var warmupOperationId = Guid.NewGuid();
			await fixture.Repository.ReplaceAddOnSelectionsAsync(
				booking,
				[
					new PartyBookingAddOnSelection
					{
						Id = Guid.NewGuid(),
						BookingId = booking.Id,
						AddOnType = PartyAddOnType.Catering,
						OptionId = "pizza-basic",
						Quantity = 1,
						SelectedAtUtc = TestDateUtc,
						SelectionOperationId = warmupOperationId,
					},
				],
				warmupOperationId,
				Guid.NewGuid(),
				TestDateUtc);
			var warmupTimeline = await timeline.ExecuteAsync(booking.Id);
			warmupTimeline.IsSuccess.Should().BeTrue();
		}

		var windowP95 = new List<long>(3);
		for (var window = 0; window < 3; window++)
		{
			var durations = new List<long>();
			for (var i = 0; i < 20; i++)
			{
				var operationId = Guid.NewGuid();
				var sw = Stopwatch.StartNew();

				await fixture.Repository.ReplaceAddOnSelectionsAsync(
					booking,
					[
						new PartyBookingAddOnSelection
						{
							Id = Guid.NewGuid(),
							BookingId = booking.Id,
							AddOnType = PartyAddOnType.Catering,
							OptionId = i % 2 == 0 ? "cake-custom" : "pizza-basic",
							Quantity = 1,
							SelectedAtUtc = TestDateUtc,
							SelectionOperationId = operationId,
						},
					],
					operationId,
					Guid.NewGuid(),
					TestDateUtc);

				var timelineResult = await timeline.ExecuteAsync(booking.Id);
				timelineResult.IsSuccess.Should().BeTrue();

				sw.Stop();
				durations.Add(sw.ElapsedMilliseconds);
			}

			var sorted = durations.OrderBy(x => x).ToArray();
			var p95 = sorted[(int)Math.Ceiling(sorted.Length * 0.95) - 1];
			windowP95.Add(p95);
		}

		var stableP95 = windowP95.OrderBy(x => x).ElementAt(windowP95.Count / 2);
		stableP95.Should().BeLessThanOrEqualTo(5000, "add-on selection+timeline benchmark should remain within stable latency envelope on shared test hosts");
		windowP95.Max().Should().BeLessThanOrEqualTo(7000, "no benchmark window should degrade into severe latency");
	}

	private static PartyBooking MakeBooking() =>
		new()
		{
			Id = Guid.NewGuid(),
			PartyDateUtc = TestDateUtc,
			SlotId = "10:00",
			PackageId = "basic",
			Status = PartyBookingStatus.Booked,
			OperationId = Guid.NewGuid(),
			CorrelationId = Guid.NewGuid(),
			CreatedAtUtc = TestDateUtc,
			UpdatedAtUtc = TestDateUtc,
			BookedAtUtc = TestDateUtc,
			DepositCommitmentStatus = PartyDepositCommitmentStatus.None,
		};

	private static async Task<TestFixture> CreateFixtureAsync()
	{
		var databasePath = TestDatabasePaths.Create();
		var dbContextFactory = new TestDbContextFactory(databasePath);
		var initializer = new AppDbContextInitializer(dbContextFactory, NullLogger<AppDbContextInitializer>.Instance);
		await initializer.InitializeAsync();

		var repository = new PartyBookingRepository(dbContextFactory);
		return new TestFixture(dbContextFactory, repository);
	}

	private sealed class TestFixture(TestDbContextFactory dbContextFactory, PartyBookingRepository repository) : IAsyncDisposable
	{
		public TestDbContextFactory DbContextFactory { get; } = dbContextFactory;
		public PartyBookingRepository Repository { get; } = repository;

		public async ValueTask DisposeAsync()
		{
			await DbContextFactory.DisposeAsync();
		}
	}

	private sealed class FixedUtcClock(DateTime utcNow) : IUtcClock
	{
		public DateTime UtcNow { get; } = utcNow;
	}
}

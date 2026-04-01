using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using POSOpen.Domain.Entities;
using POSOpen.Domain.Enums;
using POSOpen.Infrastructure.Persistence;
using POSOpen.Infrastructure.Persistence.Repositories;
using POSOpen.Application.UseCases.Inventory;

namespace POSOpen.Tests.Integration.Inventory;

public sealed class InventoryReservationRepositoryTests
{
	[Fact]
	public async Task PersistReservationPlanAsync_ReplayedOperation_DoesNotDuplicateRows()
	{
		await using var fixture = await CreateFixtureAsync();
		var bookingId = await InsertBookedBookingAsync(fixture.DbContextFactory);
		var repository = new InventoryReservationRepository(fixture.DbContextFactory);
		var operationId = Guid.NewGuid();
		var correlationId = Guid.NewGuid();
		var occurred = fixture.Clock.UtcNow;

		await repository.PersistReservationPlanAsync(
			bookingId,
			new Dictionary<string, int> { ["pizza-basic"] = 2 },
			operationId,
			correlationId,
			occurred);

		await repository.PersistReservationPlanAsync(
			bookingId,
			new Dictionary<string, int> { ["pizza-basic"] = 2 },
			operationId,
			correlationId,
			occurred.AddMinutes(1));

		await using var dbContext = await fixture.DbContextFactory.CreateDbContextAsync();
		var activeRows = await dbContext.Set<InventoryReservation>()
			.Where(x => x.BookingId == bookingId && x.ReservationState == InventoryReservationState.Reserved)
			.ToListAsync();

		activeRows.Should().ContainSingle();
		activeRows[0].QuantityReserved.Should().Be(2);
	}

	[Fact]
	public async Task ReleaseByTriggerAsync_Cancelled_ReleasesAllActiveRows()
	{
		await using var fixture = await CreateFixtureAsync();
		var bookingId = await InsertBookedBookingAsync(fixture.DbContextFactory);
		var repository = new InventoryReservationRepository(fixture.DbContextFactory);
		var reserveOperationId = Guid.NewGuid();

		await repository.PersistReservationPlanAsync(
			bookingId,
			new Dictionary<string, int> { ["cake-custom"] = 1, ["banner-custom"] = 1 },
			reserveOperationId,
			Guid.NewGuid(),
			fixture.Clock.UtcNow);

		var releaseResult = await repository.ReleaseByTriggerAsync(
			bookingId,
			InventoryReleaseTrigger.BookingCancelled,
			Guid.NewGuid(),
			Guid.NewGuid(),
			fixture.Clock.UtcNow.AddMinutes(1));

		releaseResult.ReleasedReservationRowCount.Should().Be(2);
		releaseResult.ActiveReservations.Should().BeEmpty();
	}

	private static async Task<Guid> InsertBookedBookingAsync(TestDbContextFactory factory)
	{
		var bookingId = Guid.NewGuid();
		await using var dbContext = await factory.CreateDbContextAsync();
		dbContext.Set<PartyBooking>().Add(new PartyBooking
		{
			Id = bookingId,
			PartyDateUtc = new DateTime(2026, 4, 25, 0, 0, 0, DateTimeKind.Utc),
			SlotId = "10:00",
			PackageId = "basic-party",
			Status = PartyBookingStatus.Booked,
			BookedAtUtc = DateTime.UtcNow,
			OperationId = Guid.NewGuid(),
			CorrelationId = Guid.NewGuid(),
			CreatedAtUtc = DateTime.UtcNow,
			UpdatedAtUtc = DateTime.UtcNow,
			DepositCommitmentStatus = PartyDepositCommitmentStatus.None,
		});
		await dbContext.SaveChangesAsync();
		return bookingId;
	}

	private static async Task<TestFixture> CreateFixtureAsync()
	{
		var databasePath = TestDatabasePaths.Create();
		var dbContextFactory = new TestDbContextFactory(databasePath);
		var initializer = new AppDbContextInitializer(dbContextFactory, NullLogger<AppDbContextInitializer>.Instance);
		await initializer.InitializeAsync();
		var clock = new TestUtcClock(new DateTime(2026, 4, 25, 9, 0, 0, DateTimeKind.Utc));
		return new TestFixture(dbContextFactory, clock);
	}

	private sealed class TestFixture : IAsyncDisposable
	{
		public TestFixture(TestDbContextFactory dbContextFactory, TestUtcClock clock)
		{
			DbContextFactory = dbContextFactory;
			Clock = clock;
		}

		public TestDbContextFactory DbContextFactory { get; }
		public TestUtcClock Clock { get; }

		public async ValueTask DisposeAsync()
		{
			await DbContextFactory.DisposeAsync();
		}
	}
}

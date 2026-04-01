using System.Diagnostics;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using POSOpen.Application.Exceptions;
using POSOpen.Application.UseCases.Party;
using POSOpen.Domain.Entities;
using POSOpen.Domain.Enums;
using POSOpen.Infrastructure.Persistence;
using POSOpen.Infrastructure.Persistence.Repositories;
using POSOpen.Shared.Operational;

namespace POSOpen.Tests.Integration.Party;

public sealed class PartyRoomRepositoryTests
{
	private static readonly DateTime TestDateUtc = new(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);

	private static PartyBooking MakeBooking(string slotId = "10:00", PartyBookingStatus status = PartyBookingStatus.Booked, string? roomId = null, DateTime? partyDate = null)
	{
		var date = partyDate ?? TestDateUtc;
		return new PartyBooking
		{
			Id = Guid.NewGuid(),
			PartyDateUtc = date,
			SlotId = slotId,
			PackageId = "basic",
			Status = status,
			OperationId = Guid.NewGuid(),
			CorrelationId = Guid.NewGuid(),
			CreatedAtUtc = date,
			UpdatedAtUtc = date,
			BookedAtUtc = date,
			DepositCommitmentStatus = PartyDepositCommitmentStatus.None,
			AssignedRoomId = roomId,
		};
	}

	[Fact]
	public async Task IsRoomUnavailableAsync_ReturnsTrueWhenRoomIsOccupied()
	{
		await using var fixture = await CreateFixtureAsync();

		var booking = MakeBooking(roomId: "room-a");
		await using (var db = await fixture.DbContextFactory.CreateDbContextAsync())
		{
			db.Set<PartyBooking>().Add(booking);
			await db.SaveChangesAsync();
		}

		var result = await fixture.Repository.IsRoomUnavailableAsync(TestDateUtc, "10:00", "room-a");

		result.Should().BeTrue();
	}

	[Fact]
	public async Task IsRoomUnavailableAsync_ReturnsFalseWhenBookingIsCancelled()
	{
		await using var fixture = await CreateFixtureAsync();

		var booking = MakeBooking(roomId: "room-a", status: PartyBookingStatus.Cancelled);
		await using (var db = await fixture.DbContextFactory.CreateDbContextAsync())
		{
			db.Set<PartyBooking>().Add(booking);
			await db.SaveChangesAsync();
		}

		var result = await fixture.Repository.IsRoomUnavailableAsync(TestDateUtc, "10:00", "room-a");

		result.Should().BeFalse();
	}

	[Fact]
	public async Task IsRoomUnavailableAsync_ReturnsFalseWhenExcludingOwnBooking()
	{
		await using var fixture = await CreateFixtureAsync();

		var booking = MakeBooking(roomId: "room-a");
		await using (var db = await fixture.DbContextFactory.CreateDbContextAsync())
		{
			db.Set<PartyBooking>().Add(booking);
			await db.SaveChangesAsync();
		}

		var result = await fixture.Repository.IsRoomUnavailableAsync(TestDateUtc, "10:00", "room-a", booking.Id);

		result.Should().BeFalse();
	}

	[Fact]
	public async Task AssignRoomAsync_CommitsRoomAssignment()
	{
		await using var fixture = await CreateFixtureAsync();

		var booking = MakeBooking();
		await using (var db = await fixture.DbContextFactory.CreateDbContextAsync())
		{
			db.Set<PartyBooking>().Add(booking);
			await db.SaveChangesAsync();
		}

		var operationId = Guid.NewGuid();
		var result = await fixture.Repository.AssignRoomAsync(booking, "room-a", operationId, Guid.NewGuid(), TestDateUtc);

		result.AssignedRoomId.Should().Be("room-a");
		result.RoomAssignmentOperationId.Should().Be(operationId);
	}

	[Fact]
	public async Task AssignRoomAsync_IsIdempotentOnSameOperationId()
	{
		await using var fixture = await CreateFixtureAsync();

		var booking = MakeBooking();
		await using (var db = await fixture.DbContextFactory.CreateDbContextAsync())
		{
			db.Set<PartyBooking>().Add(booking);
			await db.SaveChangesAsync();
		}

		var operationId = Guid.NewGuid();
		await fixture.Repository.AssignRoomAsync(booking, "room-a", operationId, Guid.NewGuid(), TestDateUtc);

		// Second call with same operationId — must not throw, must return existing
		var result = await fixture.Repository.AssignRoomAsync(booking, "room-a", operationId, Guid.NewGuid(), TestDateUtc);

		result.AssignedRoomId.Should().Be("room-a");
		result.RoomAssignmentOperationId.Should().Be(operationId);
	}

	[Fact]
	public async Task AssignRoomAsync_AllowsSameRoomForDifferentSlots()
	{
		await using var fixture = await CreateFixtureAsync();

		// Two bookings on different slots can both use room-a (room is per-slot, not per-day)
		var booking1 = MakeBooking(slotId: "10:00");
		var booking2 = MakeBooking(slotId: "13:00");
		await using (var db = await fixture.DbContextFactory.CreateDbContextAsync())
		{
			db.Set<PartyBooking>().AddRange(booking1, booking2);
			await db.SaveChangesAsync();
		}

		// First assignment succeeds
		var result1 = await fixture.Repository.AssignRoomAsync(booking1, "room-a", Guid.NewGuid(), Guid.NewGuid(), TestDateUtc);
		result1.AssignedRoomId.Should().Be("room-a");

		// Second booking on a different slot should also get room-a without conflict
		var result2 = await fixture.Repository.AssignRoomAsync(booking2, "room-a", Guid.NewGuid(), Guid.NewGuid(), TestDateUtc);
		result2.AssignedRoomId.Should().Be("room-a");
	}

	[Fact]
	public async Task ListAlternativeRoomsAsync_ExcludesConflictedRoom_ReturnsAvailableRooms()
	{
		await using var fixture = await CreateFixtureAsync();

		// room-a is occupied
		var booking1 = MakeBooking(slotId: "10:00", roomId: "room-a");
		await using (var db = await fixture.DbContextFactory.CreateDbContextAsync())
		{
			db.Set<PartyBooking>().Add(booking1);
			await db.SaveChangesAsync();
		}

		var alternatives = await fixture.Repository.ListAlternativeRoomsAsync(TestDateUtc, "10:00", "room-a");

		alternatives.Should().NotContain("room-a");
		alternatives.Should().Contain("room-b");
		alternatives.Should().Contain("room-c");
	}

	[Fact]
	public async Task RoomConflictQuery_SatisfiesNfr4Under1000BookingActiveDayProfile()
	{
		await using var fixture = await CreateFixtureAsync();

		// Bulk-insert 1000 bookings for the test date, with room assignments scattered
		var rooms = PartyBookingConstants.KnownRoomIds;
		var bookings = Enumerable.Range(0, 1000).Select(i => new PartyBooking
		{
			Id = Guid.NewGuid(),
			PartyDateUtc = TestDateUtc,
			SlotId = $"slot-{i:0000}",
			PackageId = "basic",
			Status = PartyBookingStatus.Booked,
			OperationId = Guid.NewGuid(),
			CorrelationId = Guid.NewGuid(),
			CreatedAtUtc = TestDateUtc,
			UpdatedAtUtc = TestDateUtc,
			BookedAtUtc = TestDateUtc,
			DepositCommitmentStatus = PartyDepositCommitmentStatus.None,
			AssignedRoomId = rooms[i % rooms.Length],
		}).ToList();

		await using (var db = await fixture.DbContextFactory.CreateDbContextAsync())
		{
			db.Set<PartyBooking>().AddRange(bookings);
			await db.SaveChangesAsync();
		}

		var durations = new long[20];
		await Task.WhenAll(Enumerable.Range(0, 20).Select(async i =>
		{
			var sw = Stopwatch.StartNew();
			await fixture.Repository.IsRoomUnavailableAsync(TestDateUtc, "slot-0000", "room-a");
			sw.Stop();
			durations[i] = sw.ElapsedMilliseconds;
		}));

		var sorted = durations.OrderBy(x => x).ToArray();
		var p95 = sorted[(int)Math.Ceiling(sorted.Length * 0.95) - 1];
		p95.Should().BeLessThanOrEqualTo(3000, "P95 room conflict query must satisfy NFR4 ≤3s under 1000-booking active-day profile");
	}

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
}

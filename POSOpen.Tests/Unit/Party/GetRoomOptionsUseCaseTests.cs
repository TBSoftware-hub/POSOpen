using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.UseCases.Party;

namespace POSOpen.Tests.Unit.Party;

public sealed class GetRoomOptionsUseCaseTests
{
	private static GetRoomOptionsUseCase CreateSut(IPartyBookingRepository repo) =>
		new(repo, NullLogger<GetRoomOptionsUseCase>.Instance);

	[Fact]
	public async Task ExecuteAsync_WhenAllRoomsFree_ReturnsAllSelectable()
	{
		var repo = new Mock<IPartyBookingRepository>();
		repo.Setup(x => x.IsRoomUnavailableAsync(It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
			.ReturnsAsync(false);
		var sut = CreateSut(repo.Object);
		var query = new GetRoomOptionsQuery(new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc), "10:00");

		var result = await sut.ExecuteAsync(query);

		result.IsSuccess.Should().BeTrue();
		result.Payload!.Rooms.Should().HaveCount(PartyBookingConstants.KnownRoomIds.Length);
		result.Payload.Rooms.Should().AllSatisfy(r => r.IsSelectable.Should().BeTrue());
		result.Payload.Rooms.Should().AllSatisfy(r => r.Reason.Should().BeNull());
	}

	[Fact]
	public async Task ExecuteAsync_WhenRoomOccupied_ShowsIsSelectableFalse()
	{
		var occupiedRoom = PartyBookingConstants.KnownRoomIds[1]; // "room-b"
		var repo = new Mock<IPartyBookingRepository>();
		repo.Setup(x => x.IsRoomUnavailableAsync(It.IsAny<DateTime>(), It.IsAny<string>(), It.Is<string>(s => s == occupiedRoom), null, It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);
		repo.Setup(x => x.IsRoomUnavailableAsync(It.IsAny<DateTime>(), It.IsAny<string>(), It.Is<string>(s => s != occupiedRoom), null, It.IsAny<CancellationToken>()))
			.ReturnsAsync(false);
		var sut = CreateSut(repo.Object);
		var query = new GetRoomOptionsQuery(new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc), "10:00");

		var result = await sut.ExecuteAsync(query);

		result.IsSuccess.Should().BeTrue();
		var roomB = result.Payload!.Rooms.First(r => r.RoomId == occupiedRoom);
		roomB.IsSelectable.Should().BeFalse();
		roomB.Reason.Should().NotBeNullOrWhiteSpace();
		result.Payload.Rooms.Where(r => r.RoomId != occupiedRoom).Should().AllSatisfy(r => r.IsSelectable.Should().BeTrue());
	}

	[Fact]
	public async Task ExecuteAsync_ReturnsRoomsInDeterministicOrderMatchingKnownRoomIds()
	{
		var repo = new Mock<IPartyBookingRepository>();
		repo.Setup(x => x.IsRoomUnavailableAsync(It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
			.ReturnsAsync(false);
		var sut = CreateSut(repo.Object);
		var query = new GetRoomOptionsQuery(new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc), "10:00");

		var result = await sut.ExecuteAsync(query);

		result.IsSuccess.Should().BeTrue();
		var roomIds = result.Payload!.Rooms.Select(r => r.RoomId).ToArray();
		roomIds.Should().Equal(PartyBookingConstants.KnownRoomIds);
	}
}

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.Exceptions;
using POSOpen.Application.Abstractions.Services;
using POSOpen.Application.UseCases.Party;
using POSOpen.Domain.Entities;
using POSOpen.Domain.Enums;
using POSOpen.Shared.Operational;

namespace POSOpen.Tests.Unit.Party;

public sealed class AssignPartyRoomUseCaseTests
{
	private static readonly DateTime TestDateUtc = new(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
	private static readonly Guid TestBookingId = Guid.NewGuid();

	private static PartyBooking CreateBooking(string slotId = "10:00", string? assignedRoomId = null) =>
		new()
		{
			Id = TestBookingId,
			PartyDateUtc = TestDateUtc,
			SlotId = slotId,
			PackageId = "basic",
			Status = PartyBookingStatus.Booked,
			OperationId = Guid.NewGuid(),
			CorrelationId = Guid.NewGuid(),
			CreatedAtUtc = TestDateUtc,
			UpdatedAtUtc = TestDateUtc,
			BookedAtUtc = TestDateUtc,
			DepositCommitmentStatus = PartyDepositCommitmentStatus.None,
			AssignedRoomId = assignedRoomId,
		};

	private static OperationContext CreateContext() =>
		new(Guid.NewGuid(), Guid.NewGuid(), null, TestDateUtc);

	private static AssignPartyRoomUseCase CreateSut(IPartyBookingRepository repo)
	{
		var clockMock = new Mock<IUtcClock>();
		clockMock.Setup(c => c.UtcNow).Returns(TestDateUtc);
		var timelineUseCase = new GetPartyBookingTimelineUseCase(repo, clockMock.Object, NullLogger<GetPartyBookingTimelineUseCase>.Instance);
		return new AssignPartyRoomUseCase(repo, timelineUseCase, NullLogger<AssignPartyRoomUseCase>.Instance);
	}

	[Fact]
	public async Task ExecuteAsync_SuccessPath_CallsAssignRoomAsyncAndReturnsSuccess()
	{
		var booking = CreateBooking();
		var assigned = CreateBooking(assignedRoomId: "room-a");
		assigned.AssignedRoomId = "room-a";

		var repo = new Mock<IPartyBookingRepository>();
		repo.Setup(x => x.GetByIdAsync(TestBookingId, It.IsAny<CancellationToken>())).ReturnsAsync(booking);
		repo.Setup(x => x.AssignRoomAsync(It.IsAny<PartyBooking>(), "room-a", It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(assigned);

		var sut = CreateSut(repo.Object);
		var command = new AssignPartyRoomCommand(TestBookingId, "room-a", CreateContext());

		var result = await sut.ExecuteAsync(command);

		result.IsSuccess.Should().BeTrue();
		result.Payload!.AssignedRoomId.Should().Be("room-a");
		repo.Verify(x => x.AssignRoomAsync(It.IsAny<PartyBooking>(), "room-a", It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task ExecuteAsync_RoomNotInKnownRoomIds_ReturnsFailure()
	{
		var repo = new Mock<IPartyBookingRepository>();
		var sut = CreateSut(repo.Object);
		var command = new AssignPartyRoomCommand(TestBookingId, "room-z", CreateContext());

		var result = await sut.ExecuteAsync(command);

		result.IsSuccess.Should().BeFalse();
		result.ErrorCode.Should().Be(PartyBookingConstants.ErrorRoomInvalid);
		repo.Verify(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	[Theory]
	[InlineData("room-a")]
	[InlineData("room-b")]
	[InlineData("room-c")]
	public async Task ExecuteAsync_RoomIdInKnownRoomIds_PassesValidation(string roomId)
	{
		var booking = CreateBooking();
		var assigned = CreateBooking(assignedRoomId: roomId);
		assigned.AssignedRoomId = roomId;

		var repo = new Mock<IPartyBookingRepository>();
		repo.Setup(x => x.GetByIdAsync(TestBookingId, It.IsAny<CancellationToken>())).ReturnsAsync(booking);
		repo.Setup(x => x.AssignRoomAsync(It.IsAny<PartyBooking>(), roomId, It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(assigned);

		var sut = CreateSut(repo.Object);
		var command = new AssignPartyRoomCommand(TestBookingId, roomId, CreateContext());

		var result = await sut.ExecuteAsync(command);

		// Validation passes — outcome depends on repository, but we do NOT get a room-invalid error
		result.ErrorCode.Should().NotBe(PartyBookingConstants.ErrorRoomInvalid);
	}

	[Fact]
	public async Task ExecuteAsync_ConflictFromRepository_ReturnsErrorRoomConflictWithAlternatives()
	{
		var booking = CreateBooking();

		var repo = new Mock<IPartyBookingRepository>();
		repo.Setup(x => x.GetByIdAsync(TestBookingId, It.IsAny<CancellationToken>())).ReturnsAsync(booking);
		repo.Setup(x => x.AssignRoomAsync(It.IsAny<PartyBooking>(), "room-a", It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
			.ThrowsAsync(new RoomConflictException("Room 'room-a' is already assigned for slot on this date."));
		repo.Setup(x => x.ListAlternativeRoomsAsync(It.IsAny<DateTime>(), It.IsAny<string>(), "room-a", It.IsAny<CancellationToken>()))
			.ReturnsAsync(new[] { "room-b", "room-c" });
		repo.Setup(x => x.ListAlternativeSlotsAsync(It.IsAny<DateTime>(), "room-a", It.IsAny<string>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(new[] { "13:00" });

		var sut = CreateSut(repo.Object);
		var command = new AssignPartyRoomCommand(TestBookingId, "room-a", CreateContext());

		var result = await sut.ExecuteAsync(command);

		result.IsSuccess.Should().BeFalse();
		result.ErrorCode.Should().Be(PartyBookingConstants.ErrorRoomConflict);
		result.Payload.Should().NotBeNull();
		result.Payload!.AlternativeRooms.Should().Contain("room-b");
		result.Payload.AlternativeRooms.Should().Contain("room-c");
		repo.Verify(x => x.ListAlternativeRoomsAsync(It.IsAny<DateTime>(), It.IsAny<string>(), "room-a", It.IsAny<CancellationToken>()), Times.Once);
		repo.Verify(x => x.ListAlternativeSlotsAsync(It.IsAny<DateTime>(), "room-a", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
	}
}

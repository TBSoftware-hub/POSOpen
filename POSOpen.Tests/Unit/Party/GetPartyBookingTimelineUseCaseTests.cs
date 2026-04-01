using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.Abstractions.Services;
using POSOpen.Application.UseCases.Party;
using POSOpen.Domain.Entities;
using POSOpen.Domain.Enums;

namespace POSOpen.Tests.Unit.Party;

public sealed class GetPartyBookingTimelineUseCaseTests
{
	[Fact]
	public async Task ExecuteAsync_WhenEventIsUpcoming_ReturnsBookedAndUpcomingMilestones()
	{
		var nowUtc = new DateTime(2026, 4, 8, 9, 0, 0, DateTimeKind.Utc);
		var booking = BuildBookedBooking(nowUtc.Date.AddDays(1), "10:00");

		var result = await BuildSut(booking, nowUtc).ExecuteAsync(booking.Id);

		result.IsSuccess.Should().BeTrue();
		result.Payload.Should().NotBeNull();
		result.Payload!.Milestones.Should().ContainSingle(x => x.MilestoneKey == "booked" && x.Status == PartyTimelineMilestoneStatus.Completed);
		result.Payload.Milestones.Should().ContainSingle(x => x.MilestoneKey == "upcoming" && x.Status == PartyTimelineMilestoneStatus.Current);
		result.Payload.Milestones.Should().ContainSingle(x => x.MilestoneKey == "active" && x.Status == PartyTimelineMilestoneStatus.Pending);
	}

	[Fact]
	public async Task ExecuteAsync_WhenEventIsActive_ReturnsActiveCurrentMilestone()
	{
		var nowUtc = new DateTime(2026, 4, 8, 10, 30, 0, DateTimeKind.Utc);
		var booking = BuildBookedBooking(nowUtc.Date, "10:00");

		var result = await BuildSut(booking, nowUtc).ExecuteAsync(booking.Id);

		result.IsSuccess.Should().BeTrue();
		result.Payload!.Milestones.Should().ContainSingle(x => x.MilestoneKey == "active" && x.Status == PartyTimelineMilestoneStatus.Current);
	}

	[Fact]
	public async Task ExecuteAsync_WhenCompletedSignalExists_ReturnsCompletedMilestone()
	{
		var nowUtc = new DateTime(2026, 4, 8, 17, 0, 0, DateTimeKind.Utc);
		var booking = BuildBookedBooking(nowUtc.Date, "13:00");
		booking.CompletedAtUtc = nowUtc.AddMinutes(-30);

		var result = await BuildSut(booking, nowUtc).ExecuteAsync(booking.Id);

		result.IsSuccess.Should().BeTrue();
		result.Payload!.Milestones.Should().ContainSingle(x => x.MilestoneKey == "completed" && x.Status == PartyTimelineMilestoneStatus.Completed);
	}

	private static GetPartyBookingTimelineUseCase BuildSut(PartyBooking booking, DateTime nowUtc)
	{
		var repository = new Mock<IPartyBookingRepository>();
		repository.Setup(x => x.GetByIdAsync(booking.Id, It.IsAny<CancellationToken>())).ReturnsAsync(booking);
		var clock = new Mock<IUtcClock>();
		clock.Setup(x => x.UtcNow).Returns(nowUtc);

		return new GetPartyBookingTimelineUseCase(
			repository.Object,
			clock.Object,
			new Mock<ILogger<GetPartyBookingTimelineUseCase>>().Object);
	}

	private static PartyBooking BuildBookedBooking(DateTime partyDateUtc, string slotId)
	{
		var createdAtUtc = DateTime.SpecifyKind(partyDateUtc.Date.AddDays(-7), DateTimeKind.Utc);
		return new PartyBooking
		{
			Id = Guid.NewGuid(),
			PartyDateUtc = DateTime.SpecifyKind(partyDateUtc, DateTimeKind.Utc),
			SlotId = slotId,
			PackageId = "vip-party",
			Status = PartyBookingStatus.Booked,
			OperationId = Guid.NewGuid(),
			CorrelationId = Guid.NewGuid(),
			CreatedAtUtc = createdAtUtc,
			UpdatedAtUtc = createdAtUtc,
			BookedAtUtc = createdAtUtc.AddDays(1),
		};
	}
}

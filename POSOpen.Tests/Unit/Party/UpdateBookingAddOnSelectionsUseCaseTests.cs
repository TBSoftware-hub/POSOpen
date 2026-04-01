using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.Abstractions.Services;
using POSOpen.Application.UseCases.Inventory;
using POSOpen.Application.UseCases.Party;
using POSOpen.Domain.Entities;
using POSOpen.Domain.Enums;
using POSOpen.Shared.Operational;

namespace POSOpen.Tests.Unit.Party;

public sealed class UpdateBookingAddOnSelectionsUseCaseTests
{
	private static readonly Guid BookingId = Guid.NewGuid();
	private static readonly DateTime TestDateUtc = new(2026, 8, 1, 10, 0, 0, DateTimeKind.Utc);

	private static UpdateBookingAddOnSelectionsUseCase CreateSut(
		IPartyBookingRepository repository,
		IUtcClock? utcClock = null)
	{
		var clock = utcClock ?? Mock.Of<IUtcClock>(x => x.UtcNow == TestDateUtc);
		var inventoryRepository = new Mock<POSOpen.Application.Abstractions.Repositories.IInventoryReservationRepository>();
		inventoryRepository.Setup(x => x.GetActiveReservedTotalsByOptionAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(new Dictionary<string, int>());
		inventoryRepository.Setup(x => x.PersistReservationPlanAsync(It.IsAny<Guid>(), It.IsAny<IReadOnlyDictionary<string, int>>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(Array.Empty<POSOpen.Domain.Entities.InventoryReservation>());
		inventoryRepository.Setup(x => x.ReleaseByTriggerAsync(It.IsAny<Guid>(), It.IsAny<InventoryReleaseTrigger>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<IReadOnlyDictionary<string, int>>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(new InventoryReleasePersistenceResult(0, []));
		inventoryRepository.Setup(x => x.ListActiveByBookingAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<POSOpen.Domain.Entities.InventoryReservation>());

		var reserve = new ReserveBookingInventoryUseCase(repository, inventoryRepository.Object, NullLogger<ReserveBookingInventoryUseCase>.Instance);
		var release = new ReleaseBookingInventoryUseCase(repository, inventoryRepository.Object, reserve, NullLogger<ReleaseBookingInventoryUseCase>.Instance);
		var timeline = new GetPartyBookingTimelineUseCase(repository, clock, NullLogger<GetPartyBookingTimelineUseCase>.Instance);
		return new UpdateBookingAddOnSelectionsUseCase(repository, release, reserve, timeline, NullLogger<UpdateBookingAddOnSelectionsUseCase>.Instance);
	}

	private static PartyBooking CreateBooking(Guid? lastOperationId = null, PartyBookingStatus status = PartyBookingStatus.Booked) =>
		new()
		{
			Id = BookingId,
			PartyDateUtc = TestDateUtc,
			SlotId = "10:00",
			PackageId = "basic",
			Status = status,
			OperationId = Guid.NewGuid(),
			CorrelationId = Guid.NewGuid(),
			CreatedAtUtc = TestDateUtc,
			UpdatedAtUtc = TestDateUtc,
			BookedAtUtc = TestDateUtc,
			DepositCommitmentStatus = PartyDepositCommitmentStatus.None,
			LastAddOnUpdateOperationId = lastOperationId,
		};

	[Fact]
	public async Task ExecuteAsync_InvalidOption_ReturnsValidationFailure()
	{
		var repo = new Mock<IPartyBookingRepository>();
		var sut = CreateSut(repo.Object);
		var command = new UpdateBookingAddOnSelectionsCommand(
			BookingId,
			[new AddOnSelectionItemCommand("unknown", PartyAddOnType.Catering, 1)],
			new OperationContext(Guid.NewGuid(), Guid.NewGuid(), null, TestDateUtc));

		var result = await sut.ExecuteAsync(command);

		result.IsSuccess.Should().BeFalse();
		result.ErrorCode.Should().Be(PartyBookingConstants.ErrorAddOnOptionInvalid);
	}

	[Fact]
	public async Task ExecuteAsync_IdempotentOperation_ShortCircuitsWithoutWrite()
	{
		var operationId = Guid.NewGuid();
		var booking = CreateBooking(operationId);
		booking.AddOnSelections =
		[
			new PartyBookingAddOnSelection
			{
				Id = Guid.NewGuid(),
				BookingId = BookingId,
				AddOnType = PartyAddOnType.Catering,
				OptionId = "pizza-basic",
				Quantity = 1,
				SelectedAtUtc = TestDateUtc,
				SelectionOperationId = operationId,
			},
		];

		var repo = new Mock<IPartyBookingRepository>();
		repo.Setup(x => x.GetByIdWithSelectionsAsync(BookingId, It.IsAny<CancellationToken>())).ReturnsAsync(booking);
		var sut = CreateSut(repo.Object);
		var command = new UpdateBookingAddOnSelectionsCommand(
			BookingId,
			[],
			new OperationContext(operationId, Guid.NewGuid(), null, TestDateUtc));

		var result = await sut.ExecuteAsync(command);

		result.IsSuccess.Should().BeTrue();
		result.UserMessage.Should().Be(PartyBookingConstants.AddOnSelectionsAlreadySavedMessage);
		repo.Verify(x => x.ReplaceAddOnSelectionsAsync(
			It.IsAny<PartyBooking>(),
			It.IsAny<IReadOnlyList<PartyBookingAddOnSelection>>(),
			It.IsAny<Guid>(),
			It.IsAny<Guid>(),
			It.IsAny<DateTime>(),
			It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public async Task ExecuteAsync_TimelineFailure_ReturnsSuccessWithEmptyMilestones()
	{
		var operationId = Guid.NewGuid();
		var booking = CreateBooking(status: PartyBookingStatus.Draft);
		var refreshed = CreateBooking(operationId, PartyBookingStatus.Draft);
		refreshed.AddOnSelections =
		[
			new PartyBookingAddOnSelection
			{
				Id = Guid.NewGuid(),
				BookingId = BookingId,
				AddOnType = PartyAddOnType.Catering,
				OptionId = "cake-custom",
				Quantity = 1,
				SelectedAtUtc = TestDateUtc,
				SelectionOperationId = operationId,
			},
		];

		var repo = new Mock<IPartyBookingRepository>();
		repo.SetupSequence(x => x.GetByIdWithSelectionsAsync(BookingId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(booking)
			.ReturnsAsync(refreshed);
		repo.Setup(x => x.ReplaceAddOnSelectionsAsync(
			It.IsAny<PartyBooking>(),
			It.IsAny<IReadOnlyList<PartyBookingAddOnSelection>>(),
			It.IsAny<Guid>(),
			It.IsAny<Guid>(),
			It.IsAny<DateTime>(),
			It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
		repo.Setup(x => x.GetByIdAsync(BookingId, It.IsAny<CancellationToken>())).ReturnsAsync(refreshed);
		var sut = CreateSut(repo.Object);

		var result = await sut.ExecuteAsync(
			new UpdateBookingAddOnSelectionsCommand(
				BookingId,
				[new AddOnSelectionItemCommand("cake-custom", PartyAddOnType.Catering, 1)],
				new OperationContext(operationId, Guid.NewGuid(), null, TestDateUtc)));

		result.IsSuccess.Should().BeTrue();
		result.Payload.Should().NotBeNull();
		result.Payload!.UpdatedMilestones.Should().BeEmpty();
		result.Payload.RiskIndicators.Should().ContainSingle();
	}
}

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.Abstractions.Services;
using POSOpen.Application.UseCases.Party;
using POSOpen.Application.UseCases.Inventory;
using POSOpen.Domain.Entities;
using POSOpen.Domain.Enums;
using POSOpen.Shared.Operational;

namespace POSOpen.Tests.Unit.Party;

public sealed class ConfirmPartyBookingUseCaseTests
{
	[Fact]
	public async Task ExecuteAsync_WhenBookingNotFound_ReturnsFailure()
	{
		var repository = new Mock<IPartyBookingRepository>();
		repository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync((PartyBooking?)null);

		var sut = BuildSut(repository);
		var command = new ConfirmPartyBookingCommand(Guid.NewGuid(), new OperationContext(Guid.NewGuid(), Guid.NewGuid(), null, DateTime.UtcNow));

		var result = await sut.ExecuteAsync(command);

		result.IsSuccess.Should().BeFalse();
		result.ErrorCode.Should().Be(PartyBookingConstants.ErrorBookingNotFound);
	}

	[Fact]
	public async Task ExecuteAsync_WhenDraftValid_ConfirmsBooking()
	{
		var booking = PartyBooking.CreateDraft(
			Guid.NewGuid(),
			DateTime.UtcNow.Date.AddDays(2),
			"16:00",
			"vip-party",
			Guid.NewGuid(),
			Guid.NewGuid(),
			DateTime.UtcNow);

		var repository = new Mock<IPartyBookingRepository>();
		repository.Setup(x => x.GetByIdAsync(booking.Id, It.IsAny<CancellationToken>())).ReturnsAsync(booking);
		repository.Setup(x => x.GetByIdWithSelectionsAsync(booking.Id, It.IsAny<CancellationToken>())).ReturnsAsync(booking);
		repository.Setup(x => x.IsSlotUnavailableAsync(booking.PartyDateUtc, booking.SlotId, booking.Id, It.IsAny<CancellationToken>()))
			.ReturnsAsync(false);
		repository.Setup(x => x.ConfirmAsync(
			booking,
			It.IsAny<Guid>(),
			It.IsAny<Guid>(),
			It.IsAny<DateTime>(),
			It.IsAny<CancellationToken>()))
			.ReturnsAsync((PartyBooking original, Guid operationId, Guid correlationId, DateTime bookedAtUtc, CancellationToken _) =>
			{
				original.Status = PartyBookingStatus.Booked;
				original.OperationId = operationId;
				original.CorrelationId = correlationId;
				original.BookedAtUtc = bookedAtUtc;
				return original;
			});

		var sut = BuildSut(repository);
		var command = new ConfirmPartyBookingCommand(booking.Id, new OperationContext(Guid.NewGuid(), Guid.NewGuid(), null, DateTime.UtcNow));

		var result = await sut.ExecuteAsync(command);

		result.IsSuccess.Should().BeTrue();
		result.Payload.Should().NotBeNull();
		result.Payload!.Status.Should().Be(PartyBookingStatus.Booked);
		result.Payload.OperationId.Should().Be(command.Context.OperationId);
		result.Payload.CorrelationId.Should().Be(command.Context.CorrelationId);
	}

	private static ConfirmPartyBookingUseCase BuildSut(Mock<IPartyBookingRepository> repository)
	{
		var clock = new Mock<IUtcClock>();
		clock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);
		var inventoryRepo = new Mock<POSOpen.Application.Abstractions.Repositories.IInventoryReservationRepository>();
		inventoryRepo
			.Setup(x => x.GetActiveReservedTotalsByOptionAsync(
				It.IsAny<IReadOnlyCollection<string>>(),
				It.IsAny<Guid?>(),
				It.IsAny<CancellationToken>()))
			.ReturnsAsync(new Dictionary<string, int>());
		inventoryRepo
			.Setup(x => x.PersistReservationPlanAsync(
				It.IsAny<Guid>(),
				It.IsAny<IReadOnlyDictionary<string, int>>(),
				It.IsAny<Guid>(),
				It.IsAny<Guid>(),
				It.IsAny<DateTime>(),
				It.IsAny<CancellationToken>()))
			.ReturnsAsync(Array.Empty<POSOpen.Domain.Entities.InventoryReservation>());

		var operationLogRepository = new Mock<IOperationLogRepository>();
		operationLogRepository
			.Setup(x => x.AppendAsync(
				It.IsAny<string>(),
				It.IsAny<string>(),
				It.IsAny<object>(),
				It.IsAny<OperationContext>(),
				It.IsAny<int>(),
				It.IsAny<CancellationToken>()))
			.ReturnsAsync(new POSOpen.Domain.Entities.OperationLogEntry());

		return new ConfirmPartyBookingUseCase(
			repository.Object,
			operationLogRepository.Object,
			clock.Object,
			new ReserveBookingInventoryUseCase(
				repository.Object,
				inventoryRepo.Object,
				new Mock<ILogger<ReserveBookingInventoryUseCase>>().Object),
			new Mock<ILogger<ConfirmPartyBookingUseCase>>().Object);
	}
}

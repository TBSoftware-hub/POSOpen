using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.UseCases.Inventory;
using POSOpen.Application.UseCases.Party;
using POSOpen.Domain.Entities;
using POSOpen.Domain.Enums;
using POSOpen.Shared.Operational;

namespace POSOpen.Tests.Unit.Inventory;

public sealed class ReserveBookingInventoryUseCaseTests
{
	[Fact]
	public async Task ExecuteAsync_WhenCapacityAvailable_ReservesRequiredQuantities()
	{
		var booking = BuildBooking("pizza-basic", 2);
		var bookingRepo = new Mock<IPartyBookingRepository>();
		bookingRepo.Setup(x => x.GetByIdWithSelectionsAsync(booking.Id, It.IsAny<CancellationToken>())).ReturnsAsync(booking);

		var inventoryRepo = new Mock<IInventoryReservationRepository>();
		inventoryRepo.Setup(x => x.GetActiveReservedTotalsByOptionAsync(It.IsAny<IReadOnlyCollection<string>>(), booking.Id, It.IsAny<CancellationToken>()))
			.ReturnsAsync(new Dictionary<string, int>());
		inventoryRepo.Setup(x => x.PersistReservationPlanAsync(
			booking.Id,
			It.IsAny<IReadOnlyDictionary<string, int>>(),
			It.IsAny<Guid>(),
			It.IsAny<Guid>(),
			It.IsAny<DateTime>(),
			It.IsAny<CancellationToken>()))
			.ReturnsAsync([
				new InventoryReservation { ReservationId = Guid.NewGuid(), BookingId = booking.Id, OptionId = "pizza-basic", QuantityReserved = 2, ReservationState = InventoryReservationState.Reserved }
			]);

		var sut = new ReserveBookingInventoryUseCase(bookingRepo.Object, inventoryRepo.Object, NullLogger<ReserveBookingInventoryUseCase>.Instance);

		var result = await sut.ExecuteAsync(new ReserveBookingInventoryCommand(booking.Id, new OperationContext(Guid.NewGuid(), Guid.NewGuid(), null, DateTime.UtcNow)));

		result.IsSuccess.Should().BeTrue();
		result.Payload.Should().NotBeNull();
		result.Payload!.UnresolvedConstraints.Should().BeEmpty();
		result.Payload.Reservations.Should().ContainSingle(x => x.OptionId == "pizza-basic" && x.QuantityReserved == 2);
	}

	[Fact]
	public async Task ExecuteAsync_WhenCapacityConstrained_ReturnsConstraintGuidance()
	{
		var booking = BuildBooking("cake-custom", 2);
		var bookingRepo = new Mock<IPartyBookingRepository>();
		bookingRepo.Setup(x => x.GetByIdWithSelectionsAsync(booking.Id, It.IsAny<CancellationToken>())).ReturnsAsync(booking);

		var inventoryRepo = new Mock<IInventoryReservationRepository>();
		inventoryRepo.Setup(x => x.GetActiveReservedTotalsByOptionAsync(It.IsAny<IReadOnlyCollection<string>>(), booking.Id, It.IsAny<CancellationToken>()))
			.ReturnsAsync(new Dictionary<string, int> { ["cake-custom"] = 1 });
		inventoryRepo.Setup(x => x.PersistReservationPlanAsync(
			booking.Id,
			It.IsAny<IReadOnlyDictionary<string, int>>(),
			It.IsAny<Guid>(),
			It.IsAny<Guid>(),
			It.IsAny<DateTime>(),
			It.IsAny<CancellationToken>()))
			.ReturnsAsync([
				new InventoryReservation { ReservationId = Guid.NewGuid(), BookingId = booking.Id, OptionId = "cake-custom", QuantityReserved = 1, ReservationState = InventoryReservationState.Reserved }
			]);

		var sut = new ReserveBookingInventoryUseCase(bookingRepo.Object, inventoryRepo.Object, NullLogger<ReserveBookingInventoryUseCase>.Instance);

		var result = await sut.ExecuteAsync(new ReserveBookingInventoryCommand(booking.Id, new OperationContext(Guid.NewGuid(), Guid.NewGuid(), null, DateTime.UtcNow)));

		result.IsSuccess.Should().BeTrue();
		result.Payload.Should().NotBeNull();
		result.Payload!.UnresolvedConstraints.Should().ContainSingle(x => x.OptionId == "cake-custom" && x.DeficitQuantity == 1);
	}

	private static PartyBooking BuildBooking(string optionId, int quantity)
	{
		var now = DateTime.UtcNow;
		return new PartyBooking
		{
			Id = Guid.NewGuid(),
			PartyDateUtc = now.Date.AddDays(1),
			SlotId = "10:00",
			PackageId = "basic-party",
			Status = PartyBookingStatus.Booked,
			BookedAtUtc = now,
			CreatedAtUtc = now,
			UpdatedAtUtc = now,
			OperationId = Guid.NewGuid(),
			CorrelationId = Guid.NewGuid(),
			DepositCommitmentStatus = PartyDepositCommitmentStatus.None,
			AddOnSelections =
			[
				new PartyBookingAddOnSelection
				{
					Id = Guid.NewGuid(),
					OptionId = optionId,
					Quantity = quantity,
					AddOnType = PartyAddOnType.Catering,
				}
			],
		};
	}
}

using FluentAssertions;
using Moq;
using POSOpen.Application.Abstractions.Services;
using POSOpen.Application.Results;
using POSOpen.Application.UseCases.Inventory;
using POSOpen.Application.UseCases.Party;
using POSOpen.Features.Party.ViewModels;
using POSOpen.Shared.Operational;

namespace POSOpen.Tests.Unit.Party;

public sealed class PartyBookingWizardViewModelTests
{
	[Fact]
	public async Task ContinueFromTime_WhenSelectedSlotUnavailable_ShowsError()
	{
		var vm = BuildViewModel(
			availabilityResult: AppResult<BookingAvailabilityDto>.Success(
				new BookingAvailabilityDto(
					DateTime.UtcNow.Date.AddDays(1),
					[
						new BookingSlotAvailabilityDto("10:00", false, PartyBookingConstants.SafeSlotUnavailableMessage),
						new BookingSlotAvailabilityDto("13:00", true, null)
					]),
				"ok"));

		await vm.InitializeCommand.ExecuteAsync(null);
		await vm.ContinueFromDateCommand.ExecuteAsync(null);
		vm.SelectedSlot = vm.AvailableSlots.First(x => x.SlotId == "10:00");

		vm.ContinueFromTimeCommand.Execute(null);

		vm.HasError.Should().BeTrue();
		vm.ErrorMessage.Should().Be(PartyBookingConstants.SafeSlotUnavailableMessage);
	}

	[Fact]
	public async Task ContinueFromPackage_WhenValid_SavesDraftAndMovesToReview()
	{
		var vm = BuildViewModel();

		await vm.InitializeCommand.ExecuteAsync(null);
		await vm.ContinueFromDateCommand.ExecuteAsync(null);
		vm.SelectedSlot = vm.AvailableSlots.First(x => x.IsAvailable);
		vm.ContinueFromTimeCommand.Execute(null);
		vm.SelectedPackageId = "basic-party";

		await vm.ContinueFromPackageCommand.ExecuteAsync(null);

		vm.IsReviewStep.Should().BeTrue();
		vm.StatusMessage.Should().Be(PartyBookingConstants.DraftSavedMessage);
		vm.HasError.Should().BeFalse();
	}

	private static PartyBookingWizardViewModel BuildViewModel(
		AppResult<BookingAvailabilityDto>? availabilityResult = null)
	{
		var availability = availabilityResult ?? AppResult<BookingAvailabilityDto>.Success(
			new BookingAvailabilityDto(
				DateTime.UtcNow.Date.AddDays(1),
				[
					new BookingSlotAvailabilityDto("10:00", true, null),
					new BookingSlotAvailabilityDto("13:00", true, null),
					new BookingSlotAvailabilityDto("16:00", true, null),
				]),
			"ok");

		var repository = new Mock<POSOpen.Application.Abstractions.Repositories.IPartyBookingRepository>();
		repository.Setup(x => x.IsSlotUnavailableAsync(It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync((DateTime _, string slotId, Guid? _, CancellationToken _) =>
			{
				var slot = availability.Payload!.Slots.FirstOrDefault(x => x.SlotId == slotId);
				return slot is not null && slot.IsAvailable == false;
			});
		repository.Setup(x => x.GetByOperationIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync((POSOpen.Domain.Entities.PartyBooking?)null);
		repository.Setup(x => x.UpsertDraftAsync(It.IsAny<POSOpen.Domain.Entities.PartyBooking>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync((POSOpen.Domain.Entities.PartyBooking booking, CancellationToken _) => booking);
		repository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync((POSOpen.Domain.Entities.PartyBooking?)null);

		var clock = new Mock<POSOpen.Application.Abstractions.Services.IUtcClock>();
		clock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);

		var getAvailability = new GetBookingAvailabilityUseCase(
			repository.Object,
			Microsoft.Extensions.Logging.Abstractions.NullLogger<GetBookingAvailabilityUseCase>.Instance);
		var operationLogRepo = new Mock<POSOpen.Application.Abstractions.Repositories.IOperationLogRepository>();
		operationLogRepo
			.Setup(x => x.AppendAsync(
				It.IsAny<string>(),
				It.IsAny<string>(),
				It.IsAny<object>(),
				It.IsAny<POSOpen.Shared.Operational.OperationContext>(),
				It.IsAny<int>(),
				It.IsAny<CancellationToken>()))
			.ReturnsAsync(new POSOpen.Domain.Entities.OperationLogEntry());

		var createDraft = new CreateDraftPartyBookingUseCase(
			repository.Object,
			clock.Object,
			Microsoft.Extensions.Logging.Abstractions.NullLogger<CreateDraftPartyBookingUseCase>.Instance);
		var confirm = new ConfirmPartyBookingUseCase(
			repository.Object,
			operationLogRepo.Object,
			clock.Object,
			new ReserveBookingInventoryUseCase(
				repository.Object,
				new Mock<POSOpen.Application.Abstractions.Repositories.IInventoryReservationRepository>().Object,
				Microsoft.Extensions.Logging.Abstractions.NullLogger<ReserveBookingInventoryUseCase>.Instance),
			Microsoft.Extensions.Logging.Abstractions.NullLogger<ConfirmPartyBookingUseCase>.Instance);
		var operationContextFactory = new Mock<IOperationContextFactory>();
		operationContextFactory.Setup(x => x.CreateRoot(It.IsAny<Guid?>()))
			.Returns(new OperationContext(Guid.NewGuid(), Guid.NewGuid(), null, DateTime.UtcNow));

		return new PartyBookingWizardViewModel(
			getAvailability,
			createDraft,
			confirm,
			operationContextFactory.Object,
			Microsoft.Extensions.Logging.Abstractions.NullLogger<PartyBookingWizardViewModel>.Instance);
	}
}

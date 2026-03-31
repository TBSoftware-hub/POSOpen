using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.Abstractions.Services;
using POSOpen.Application.Results;
using POSOpen.Application.UseCases.Checkout;
using POSOpen.Domain.Entities;
using POSOpen.Domain.Enums;

namespace POSOpen.Tests.Unit.Checkout;

public sealed class CaptureScannerInputUseCaseTests
{
	private static readonly Guid StaffId = Guid.NewGuid();
	private static readonly DateTime FixedNow = new(2026, 4, 1, 10, 0, 0, DateTimeKind.Utc);

	[Fact]
	public async Task ExecuteAsync_WhenScannerUnavailable_ReturnsUnresolvedGuidance()
	{
		var cart = CartSession.Create(Guid.NewGuid(), null, StaffId, FixedNow);
		var cartRepo = new Mock<ICartSessionRepository>();
		cartRepo.Setup(x => x.GetByIdAsync(cart.Id, It.IsAny<CancellationToken>())).ReturnsAsync(cart);

		var scanner = new Mock<IScannerDeviceService>();
		scanner.Setup(x => x.CaptureAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(AppResult<ScannerCaptureDto>.Success(
				new ScannerCaptureDto(ScannerCaptureStatus.Unavailable, null, DeviceDiagnosticCode.ScannerUnavailable),
				CartCheckoutConstants.SafeScannerUnavailableMessage));

		var sut = new CaptureScannerInputUseCase(
			cartRepo.Object,
			new Mock<IFamilyProfileRepository>().Object,
			scanner.Object,
			MockAdmissionPricing().Object,
			new AddCartLineItemUseCase(cartRepo.Object, MockClock().Object),
			new Mock<ILogger<CaptureScannerInputUseCase>>().Object);

		var result = await sut.ExecuteAsync(cart.Id);

		result.IsSuccess.Should().BeTrue();
		result.Payload!.Action.Should().Be(ScannerResolutionAction.Unresolved);
		result.Payload.DiagnosticCode.Should().Be(DeviceDiagnosticCode.ScannerUnavailable);
	}

	[Fact]
	public async Task ExecuteAsync_WhenFamilyAlreadyInCart_SelectsExistingItem()
	{
		var familyId = Guid.NewGuid();
		var cart = CartSession.Create(Guid.NewGuid(), familyId, StaffId, FixedNow);
		var existingItemId = Guid.NewGuid();
		cart.LineItems.Add(CartLineItem.Create(existingItemId, cart.Id, "Admission", FulfillmentContext.Admission, familyId, 1, 2500, "USD", FixedNow));

		var family = FamilyProfile.Create(familyId, "Ava", "Stone", "5551112222", null, StaffId, FixedNow);
		family.ScanToken = "scan-ava";

		var cartRepo = new Mock<ICartSessionRepository>();
		cartRepo.Setup(x => x.GetByIdAsync(cart.Id, It.IsAny<CancellationToken>())).ReturnsAsync(cart);

		var familyRepo = new Mock<IFamilyProfileRepository>();
		familyRepo.Setup(x => x.GetByScanTokenAsync("scan-ava", It.IsAny<CancellationToken>())).ReturnsAsync(family);

		var scanner = new Mock<IScannerDeviceService>();
		scanner.Setup(x => x.CaptureAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(AppResult<ScannerCaptureDto>.Success(
				new ScannerCaptureDto(ScannerCaptureStatus.Captured, "scan-ava", null),
				"Captured"));

		var sut = new CaptureScannerInputUseCase(
			cartRepo.Object,
			familyRepo.Object,
			scanner.Object,
			MockAdmissionPricing().Object,
			new AddCartLineItemUseCase(cartRepo.Object, MockClock().Object),
			new Mock<ILogger<CaptureScannerInputUseCase>>().Object);

		var result = await sut.ExecuteAsync(cart.Id);

		result.IsSuccess.Should().BeTrue();
		result.Payload!.Action.Should().Be(ScannerResolutionAction.SelectedExistingItem);
		result.Payload.SelectedLineItemId.Should().Be(existingItemId);
	}

	[Fact]
	public async Task ExecuteAsync_WhenFamilyScanMatchesProfile_AddsAdmissionToCart()
	{
		var familyId = Guid.NewGuid();
		var cart = CartSession.Create(Guid.NewGuid(), familyId, StaffId, FixedNow);
		var family = FamilyProfile.Create(familyId, "Mia", "Hart", "5551113333", null, StaffId, FixedNow);
		family.ScanToken = "scan-mia";

		var cartRepo = new Mock<ICartSessionRepository>();
		cartRepo.Setup(x => x.GetByIdAsync(cart.Id, It.IsAny<CancellationToken>())).ReturnsAsync(cart);
		cartRepo.Setup(x => x.AddLineItemAsync(cart.Id, It.IsAny<CartLineItem>(), FixedNow, It.IsAny<CancellationToken>()))
			.ReturnsAsync((Guid _, CartLineItem item, DateTime _, CancellationToken _) =>
			{
				cart.LineItems.Add(item);
				return cart;
			});

		var familyRepo = new Mock<IFamilyProfileRepository>();
		familyRepo.Setup(x => x.GetByScanTokenAsync("scan-mia", It.IsAny<CancellationToken>())).ReturnsAsync(family);

		var scanner = new Mock<IScannerDeviceService>();
		scanner.Setup(x => x.CaptureAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(AppResult<ScannerCaptureDto>.Success(
				new ScannerCaptureDto(ScannerCaptureStatus.Captured, "scan-mia", null),
				"Captured"));

		var sut = new CaptureScannerInputUseCase(
			cartRepo.Object,
			familyRepo.Object,
			scanner.Object,
			MockAdmissionPricing().Object,
			new AddCartLineItemUseCase(cartRepo.Object, MockClock().Object),
			new Mock<ILogger<CaptureScannerInputUseCase>>().Object);

		var result = await sut.ExecuteAsync(cart.Id);

		result.IsSuccess.Should().BeTrue();
		result.Payload!.Action.Should().Be(ScannerResolutionAction.AddedToCart);
		result.Payload.Cart.LineItems.Should().ContainSingle(x => x.ReferenceId == familyId);
	}

	private static Mock<IUtcClock> MockClock()
	{
		var clock = new Mock<IUtcClock>();
		clock.Setup(x => x.UtcNow).Returns(FixedNow);
		return clock;
	}

	private static Mock<IAdmissionPricingService> MockAdmissionPricing()
	{
		var pricing = new Mock<IAdmissionPricingService>();
		pricing.Setup(x => x.GetAdmissionTotalAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(new AdmissionTotal(2500, "USD"));
		return pricing;
	}
}
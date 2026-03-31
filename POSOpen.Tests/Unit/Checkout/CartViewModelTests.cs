using FluentAssertions;
using Moq;
using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.Abstractions.Services;
using POSOpen.Application.UseCases.Checkout;
using POSOpen.Domain.Entities;
using POSOpen.Domain.Enums;
using POSOpen.Domain.Policies;
using POSOpen.Features.Checkout.ViewModels;

namespace POSOpen.Tests.Unit.Checkout;

public sealed class CartViewModelTests
{
	private static readonly Guid StaffId = Guid.NewGuid();
	private static readonly DateTime FixedNow = new(2026, 4, 1, 10, 0, 0, DateTimeKind.Utc);

	[Fact]
	public async Task InitializeCommand_when_open_cart_exists_populates_item_groups()
	{
		var cartId = Guid.NewGuid();
		var cart = BuildCartWithItems(cartId);
		var repo = new Mock<ICartSessionRepository>();
		repo.Setup(x => x.GetOpenCartForStaffAsync(StaffId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(cart);

		var vm = CreateViewModel(repo);
		await vm.InitializeCommand.ExecuteAsync(null);

		vm.IsLoading.Should().BeFalse();
		vm.HasError.Should().BeFalse();
		vm.ItemGroups.Should().HaveCount(2, "cart has Admission and RetailItem items");
		vm.GrandTotalLabel.Should().NotBe("$0.00");
	}

	[Fact]
	public async Task InitializeCommand_when_staff_not_authenticated_sets_error_message()
	{
		var appState = new Mock<IAppStateService>();
		appState.Setup(x => x.CurrentStaffId).Returns((Guid?)null);

		var vm = CreateViewModel(new Mock<ICartSessionRepository>(), appState);
		await vm.InitializeCommand.ExecuteAsync(null);

		vm.HasError.Should().BeTrue();
		vm.ErrorMessage.Should().NotBeNullOrWhiteSpace();
		vm.ItemGroups.Should().BeEmpty();
	}

	[Fact]
	public async Task RemoveItemCommand_removes_item_and_refreshes_groups()
	{
		var cartId = Guid.NewGuid();
		var lineItemId = Guid.NewGuid();
		var cart = BuildCartWithItems(cartId, lineItemId);
		var repo = new Mock<ICartSessionRepository>();
		repo.Setup(x => x.GetOpenCartForStaffAsync(StaffId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(cart);
		repo.Setup(x => x.GetByIdAsync(cartId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(cart);
		repo.Setup(x => x.RemoveLineItemAsync(cartId, lineItemId, FixedNow, It.IsAny<CancellationToken>()))
			.ReturnsAsync(() =>
			{
				var toRemove = cart.LineItems.FirstOrDefault(i => i.Id == lineItemId);
				if (toRemove is not null) cart.LineItems.Remove(toRemove);
				return cart;
			});

		var vm = CreateViewModel(repo);
		await vm.InitializeCommand.ExecuteAsync(null);
		await vm.RemoveItemCommand.ExecuteAsync(lineItemId);

		repo.Verify(x => x.RemoveLineItemAsync(cartId, lineItemId, FixedNow, It.IsAny<CancellationToken>()), Times.Once);
		vm.HasError.Should().BeFalse();
	}

	[Fact]
	public async Task IncrementQuantityCommand_calls_update_with_incremented_quantity()
	{
		var cartId = Guid.NewGuid();
		var lineItemId = Guid.NewGuid();
		var cart = BuildCartWithItems(cartId, lineItemId, admissionQty: 2);
		var repo = new Mock<ICartSessionRepository>();
		repo.Setup(x => x.GetOpenCartForStaffAsync(StaffId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(cart);
		repo.Setup(x => x.GetByIdAsync(cartId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(cart);
		repo.Setup(x => x.UpdateLineItemQuantityAsync(cartId, lineItemId, 3, FixedNow, It.IsAny<CancellationToken>()))
			.ReturnsAsync(() =>
			{
				var item = cart.LineItems.First(i => i.Id == lineItemId);
				item.Quantity = 3;
				return cart;
			});

		var vm = CreateViewModel(repo);
		await vm.InitializeCommand.ExecuteAsync(null);
		await vm.IncrementQuantityCommand.ExecuteAsync(lineItemId);

		repo.Verify(x => x.UpdateLineItemQuantityAsync(cartId, lineItemId, 3, FixedNow, It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task DecrementQuantityCommand_when_quantity_is_one_removes_item()
	{
		var cartId = Guid.NewGuid();
		var lineItemId = Guid.NewGuid();
		var cart = BuildCartWithItems(cartId, lineItemId, admissionQty: 1);
		var repo = new Mock<ICartSessionRepository>();
		repo.Setup(x => x.GetOpenCartForStaffAsync(StaffId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(cart);
		repo.Setup(x => x.GetByIdAsync(cartId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(cart);
		repo.Setup(x => x.RemoveLineItemAsync(cartId, lineItemId, FixedNow, It.IsAny<CancellationToken>()))
			.ReturnsAsync(() =>
			{
				var item = cart.LineItems.FirstOrDefault(i => i.Id == lineItemId);
				if (item is not null) cart.LineItems.Remove(item);
				return cart;
			});

		var vm = CreateViewModel(repo);
		await vm.InitializeCommand.ExecuteAsync(null);
		await vm.DecrementQuantityCommand.ExecuteAsync(lineItemId);

		repo.Verify(x => x.RemoveLineItemAsync(cartId, lineItemId, FixedNow, It.IsAny<CancellationToken>()), Times.Once);
		repo.Verify(x => x.UpdateLineItemQuantityAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	// ─── Helpers ─────────────────────────────────────────────────────────

	private CartViewModel CreateViewModel(
		Mock<ICartSessionRepository> repo,
		Mock<IAppStateService>? appState = null)
	{
		var mockAppState = appState ?? BuildAuthenticatedAppState();
		var mockClock = new Mock<IUtcClock>();
		var mockUiService = new Mock<ICheckoutUiService>();
		mockClock.Setup(x => x.UtcNow).Returns(FixedNow);

		var getOrCreate = new GetOrCreateCartSessionUseCase(repo.Object, mockAppState.Object, mockClock.Object);
		var remove = new RemoveCartLineItemUseCase(repo.Object, mockClock.Object);
		var update = new UpdateCartLineItemQuantityUseCase(repo.Object, mockClock.Object);
		var validate = new ValidateCartCompatibilityUseCase(repo.Object, []);

		return new CartViewModel(getOrCreate, remove, update, validate, mockUiService.Object);
	}

	private CartViewModel CreateViewModelWithRules(
		Mock<ICartSessionRepository> repo,
		params ICartCompatibilityRule[] rules)
	{
		var mockAppState = BuildAuthenticatedAppState();
		var mockClock = new Mock<IUtcClock>();
		var mockUiService = new Mock<ICheckoutUiService>();
		mockClock.Setup(x => x.UtcNow).Returns(FixedNow);

		var getOrCreate = new GetOrCreateCartSessionUseCase(repo.Object, mockAppState.Object, mockClock.Object);
		var remove = new RemoveCartLineItemUseCase(repo.Object, mockClock.Object);
		var update = new UpdateCartLineItemQuantityUseCase(repo.Object, mockClock.Object);
		var validate = new ValidateCartCompatibilityUseCase(repo.Object, rules);

		return new CartViewModel(getOrCreate, remove, update, validate, mockUiService.Object);
	}

	[Fact]
	public async Task Initialize_EmptyCart_IsCartValidFalse()
	{
		var cartId = Guid.NewGuid();
		var emptyCart = CartSession.Create(cartId, null, StaffId, FixedNow);
		var repo = new Mock<ICartSessionRepository>();
		repo.Setup(x => x.GetOpenCartForStaffAsync(StaffId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(emptyCart);
		repo.Setup(x => x.GetByIdAsync(cartId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(emptyCart);

		var vm = CreateViewModelWithRules(repo, new CartMustHaveItemsRule());
		await vm.InitializeCommand.ExecuteAsync(null);

		vm.IsCartValid.Should().BeFalse();
		vm.HasValidationIssues.Should().BeTrue();
	}

	[Fact]
	public async Task Initialize_CartWithAdmission_IsCartValidTrue()
	{
		var cartId = Guid.NewGuid();
		var cart = CartSession.Create(cartId, null, StaffId, FixedNow);
		cart.LineItems.Add(CartLineItem.Create(
			Guid.NewGuid(), cartId, "Admission", FulfillmentContext.Admission, null, 1, 1500, "USD", FixedNow));
		var repo = new Mock<ICartSessionRepository>();
		repo.Setup(x => x.GetOpenCartForStaffAsync(StaffId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(cart);
		repo.Setup(x => x.GetByIdAsync(cartId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(cart);

		var vm = CreateViewModelWithRules(repo,
			new CartMustHaveItemsRule(),
			new CateringRequiresPartyDepositRule(),
			new SinglePartyDepositRule());
		await vm.InitializeCommand.ExecuteAsync(null);

		vm.IsCartValid.Should().BeTrue();
		vm.HasValidationIssues.Should().BeFalse();
	}

	[Fact]
	public async Task ApplyFix_RemoveCatering_ClearsIssueAndEnablesCheckout()
	{
		var cartId = Guid.NewGuid();
		var cateringId = Guid.NewGuid();

		var cartBefore = CartSession.Create(cartId, null, StaffId, FixedNow);
		cartBefore.LineItems.Add(CartLineItem.Create(
			Guid.NewGuid(), cartId, "Admission", FulfillmentContext.Admission, null, 1, 1500, "USD", FixedNow));
		cartBefore.LineItems.Add(CartLineItem.Create(
			cateringId, cartId, "Catering", FulfillmentContext.CateringAddon, null, 1, 500, "USD", FixedNow));

		var cartAfter = CartSession.Create(cartId, null, StaffId, FixedNow);
		cartAfter.LineItems.Add(CartLineItem.Create(
			Guid.NewGuid(), cartId, "Admission", FulfillmentContext.Admission, null, 1, 1500, "USD", FixedNow));

		var repo = new Mock<ICartSessionRepository>();
		repo.SetupSequence(x => x.GetOpenCartForStaffAsync(StaffId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(cartBefore)
			.ReturnsAsync(cartAfter);
		repo.SetupSequence(x => x.GetByIdAsync(cartId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(cartBefore)  // init → RunValidationAsync
			.ReturnsAsync(cartBefore)  // ApplyFix → RemoveCartLineItemUseCase
			.ReturnsAsync(cartAfter);  // after fix → RunValidationAsync
		repo.Setup(x => x.RemoveLineItemAsync(cartId, cateringId, FixedNow, It.IsAny<CancellationToken>()))
			.ReturnsAsync(cartAfter);

		var vm = CreateViewModelWithRules(repo, new CateringRequiresPartyDepositRule());
		await vm.InitializeCommand.ExecuteAsync(null);

		vm.IsCartValid.Should().BeFalse("catering without deposit violates the rule");
		vm.HasValidationIssues.Should().BeTrue();

		await vm.ApplyFixCommand.ExecuteAsync(CartValidationFixAction.RemoveCateringItems);

		vm.IsCartValid.Should().BeTrue("catering was removed, no more violations");
		vm.HasValidationIssues.Should().BeFalse();
	}

	private Mock<IAppStateService> BuildAuthenticatedAppState()
	{
		var mock = new Mock<IAppStateService>();
		mock.Setup(x => x.CurrentStaffId).Returns(StaffId);
		return mock;
	}

	private CartSession BuildCartWithItems(
		Guid cartId,
		Guid? admissionItemId = null,
		int admissionQty = 2)
	{
		var cart = CartSession.Create(cartId, null, StaffId, FixedNow);
		cart.LineItems.Add(CartLineItem.Create(
			admissionItemId ?? Guid.NewGuid(), cartId, "General Admission",
			FulfillmentContext.Admission, null, admissionQty, 1500, "USD", FixedNow));
		cart.LineItems.Add(CartLineItem.Create(
			Guid.NewGuid(), cartId, "T-Shirt",
			FulfillmentContext.RetailItem, null, 1, 2500, "USD", FixedNow));
		return cart;
	}
}

using FluentAssertions;
using Moq;
using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.Abstractions.Services;
using POSOpen.Application.UseCases.Checkout;
using POSOpen.Domain.Entities;
using POSOpen.Domain.Enums;

namespace POSOpen.Tests.Unit.Checkout;

public sealed class CartUseCaseTests
{
	private static readonly Guid StaffId = Guid.NewGuid();
	private static readonly DateTime FixedNow = new(2026, 4, 1, 10, 0, 0, DateTimeKind.Utc);

	// ─── GetOrCreateCartSessionUseCase ─────────────────────────────────────

	[Fact]
	public async Task GetOrCreate_when_staff_not_authenticated_returns_failure()
	{
		var appState = new Mock<IAppStateService>();
		appState.Setup(x => x.CurrentStaffId).Returns((Guid?)null);
		var useCase = new GetOrCreateCartSessionUseCase(
			new Mock<ICartSessionRepository>().Object, appState.Object, MockClock());

		var result = await useCase.ExecuteAsync();

		result.IsSuccess.Should().BeFalse();
		result.ErrorCode.Should().Be(CartCheckoutConstants.ErrorStaffNotAuthenticated);
	}

	[Fact]
	public async Task GetOrCreate_when_open_cart_exists_returns_existing_cart()
	{
		var cartId = Guid.NewGuid();
		var existingCart = CartSession.Create(cartId, null, StaffId, FixedNow);
		var repo = new Mock<ICartSessionRepository>();
		repo.Setup(x => x.GetOpenCartForStaffAsync(StaffId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(existingCart);

		var useCase = new GetOrCreateCartSessionUseCase(repo.Object, MockAppState(), MockClock());
		var result = await useCase.ExecuteAsync();

		result.IsSuccess.Should().BeTrue();
		result.Payload!.Id.Should().Be(cartId);
		repo.Verify(x => x.CreateAsync(It.IsAny<CartSession>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public async Task GetOrCreate_when_no_open_cart_creates_and_returns_new_cart()
	{
		var repo = new Mock<ICartSessionRepository>();
		repo.Setup(x => x.GetOpenCartForStaffAsync(StaffId, It.IsAny<CancellationToken>()))
			.ReturnsAsync((CartSession?)null);
		repo.Setup(x => x.CreateAsync(It.IsAny<CartSession>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync((CartSession cart, CancellationToken _) => cart);

		var useCase = new GetOrCreateCartSessionUseCase(repo.Object, MockAppState(), MockClock());
		var result = await useCase.ExecuteAsync();

		result.IsSuccess.Should().BeTrue();
		result.Payload!.StaffId.Should().Be(StaffId);
		result.Payload.Status.Should().Be(CartStatus.Open);
		repo.Verify(x => x.CreateAsync(It.IsAny<CartSession>(), It.IsAny<CancellationToken>()), Times.Once);
	}

	// ─── AddCartLineItemUseCase ───────────────────────────────────────────

	[Fact]
	public async Task AddLineItem_when_quantity_zero_returns_failure()
	{
		var useCase = new AddCartLineItemUseCase(
			new Mock<ICartSessionRepository>().Object, MockClock());

		var result = await useCase.ExecuteAsync(
			new AddCartLineItemCommand(Guid.NewGuid(), "Desc", FulfillmentContext.Admission, null, 0, 1500));

		result.IsSuccess.Should().BeFalse();
		result.ErrorCode.Should().Be(CartCheckoutConstants.ErrorInvalidQuantity);
	}

	[Fact]
	public async Task AddLineItem_when_cart_not_found_returns_failure()
	{
		var repo = new Mock<ICartSessionRepository>();
		repo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync((CartSession?)null);

		var useCase = new AddCartLineItemUseCase(repo.Object, MockClock());
		var result = await useCase.ExecuteAsync(
			new AddCartLineItemCommand(Guid.NewGuid(), "Desc", FulfillmentContext.Admission, null, 2, 1500));

		result.IsSuccess.Should().BeFalse();
		result.ErrorCode.Should().Be(CartCheckoutConstants.ErrorCartNotFound);
	}

	[Fact]
	public async Task AddLineItem_with_valid_cart_adds_item_and_returns_updated_cart()
	{
		var cartId = Guid.NewGuid();
		var cart = CartSession.Create(cartId, null, StaffId, FixedNow);
		var repo = new Mock<ICartSessionRepository>();
		repo.Setup(x => x.GetByIdAsync(cartId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(cart);
		repo.Setup(x => x.AddLineItemAsync(cartId, It.IsAny<CartLineItem>(), FixedNow, It.IsAny<CancellationToken>()))
			.ReturnsAsync((Guid _, CartLineItem item, DateTime _, CancellationToken _) =>
			{
				cart.LineItems.Add(item);
				return cart;
			});

		var useCase = new AddCartLineItemUseCase(repo.Object, MockClock());
		var result = await useCase.ExecuteAsync(
			new AddCartLineItemCommand(cartId, "General Admission", FulfillmentContext.Admission, null, 2, 1500));

		result.IsSuccess.Should().BeTrue();
		result.Payload!.LineItems.Should().HaveCount(1);
		result.Payload.LineItems[0].Description.Should().Be("General Admission");
		result.Payload.LineItems[0].Quantity.Should().Be(2);
		result.Payload.LineItems[0].UnitAmountCents.Should().Be(1500);
		result.Payload.TotalAmountCents.Should().Be(3000);
	}

	// ─── RemoveCartLineItemUseCase ────────────────────────────────────────

	[Fact]
	public async Task RemoveLineItem_when_cart_not_found_returns_failure()
	{
		var repo = new Mock<ICartSessionRepository>();
		repo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync((CartSession?)null);

		var useCase = new RemoveCartLineItemUseCase(repo.Object, MockClock());
		var result = await useCase.ExecuteAsync(
			new RemoveCartLineItemCommand(Guid.NewGuid(), Guid.NewGuid()));

		result.IsSuccess.Should().BeFalse();
		result.ErrorCode.Should().Be(CartCheckoutConstants.ErrorCartNotFound);
	}

	[Fact]
	public async Task RemoveLineItem_when_item_not_found_returns_failure()
	{
		var cartId = Guid.NewGuid();
		var cart = CartSession.Create(cartId, null, StaffId, FixedNow);
		var repo = new Mock<ICartSessionRepository>();
		repo.Setup(x => x.GetByIdAsync(cartId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(cart);

		var useCase = new RemoveCartLineItemUseCase(repo.Object, MockClock());
		var result = await useCase.ExecuteAsync(
			new RemoveCartLineItemCommand(cartId, Guid.NewGuid()));

		result.IsSuccess.Should().BeFalse();
		result.ErrorCode.Should().Be(CartCheckoutConstants.ErrorLineItemNotFound);
	}

	[Fact]
	public async Task RemoveLineItem_with_valid_item_removes_and_returns_updated_cart()
	{
		var cartId = Guid.NewGuid();
		var lineItemId = Guid.NewGuid();
		var cart = CartSession.Create(cartId, null, StaffId, FixedNow);
		cart.LineItems.Add(CartLineItem.Create(lineItemId, cartId, "Ticket", FulfillmentContext.Admission, null, 1, 1500, "USD", FixedNow));

		var repo = new Mock<ICartSessionRepository>();
		repo.Setup(x => x.GetByIdAsync(cartId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(cart);
		repo.Setup(x => x.RemoveLineItemAsync(cartId, lineItemId, FixedNow, It.IsAny<CancellationToken>()))
			.ReturnsAsync(() =>
			{
				cart.LineItems.Clear();
				return cart;
			});

		var useCase = new RemoveCartLineItemUseCase(repo.Object, MockClock());
		var result = await useCase.ExecuteAsync(
			new RemoveCartLineItemCommand(cartId, lineItemId));

		result.IsSuccess.Should().BeTrue();
		result.Payload!.LineItems.Should().BeEmpty();
	}

	// ─── UpdateCartLineItemQuantityUseCase ────────────────────────────────

	[Fact]
	public async Task UpdateQuantity_when_quantity_less_than_one_returns_failure()
	{
		var useCase = new UpdateCartLineItemQuantityUseCase(
			new Mock<ICartSessionRepository>().Object, MockClock());

		var result = await useCase.ExecuteAsync(
			new UpdateCartLineItemQuantityCommand(Guid.NewGuid(), Guid.NewGuid(), 0));

		result.IsSuccess.Should().BeFalse();
		result.ErrorCode.Should().Be(CartCheckoutConstants.ErrorInvalidQuantity);
	}

	[Fact]
	public async Task UpdateQuantity_with_valid_item_updates_and_returns_updated_cart()
	{
		var cartId = Guid.NewGuid();
		var lineItemId = Guid.NewGuid();
		var cart = CartSession.Create(cartId, null, StaffId, FixedNow);
		var item = CartLineItem.Create(lineItemId, cartId, "Ticket", FulfillmentContext.Admission, null, 1, 1500, "USD", FixedNow);
		cart.LineItems.Add(item);

		var repo = new Mock<ICartSessionRepository>();
		repo.Setup(x => x.GetByIdAsync(cartId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(cart);
		repo.Setup(x => x.UpdateLineItemQuantityAsync(cartId, lineItemId, 3, FixedNow, It.IsAny<CancellationToken>()))
			.ReturnsAsync(() =>
			{
				item.Quantity = 3;
				return cart;
			});

		var useCase = new UpdateCartLineItemQuantityUseCase(repo.Object, MockClock());
		var result = await useCase.ExecuteAsync(
			new UpdateCartLineItemQuantityCommand(cartId, lineItemId, 3));

		result.IsSuccess.Should().BeTrue();
		result.Payload!.LineItems[0].Quantity.Should().Be(3);
		result.Payload.TotalAmountCents.Should().Be(4500);
	}

	// ─── Helpers ─────────────────────────────────────────────────────────

	private static IUtcClock MockClock()
	{
		var clock = new Mock<IUtcClock>();
		clock.Setup(x => x.UtcNow).Returns(FixedNow);
		return clock.Object;
	}

	private static IAppStateService MockAppState()
	{
		var appState = new Mock<IAppStateService>();
		appState.Setup(x => x.CurrentStaffId).Returns(StaffId);
		return appState.Object;
	}
}

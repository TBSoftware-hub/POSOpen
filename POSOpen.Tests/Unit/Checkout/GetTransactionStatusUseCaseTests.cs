using FluentAssertions;
using Moq;
using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.Results;
using POSOpen.Application.UseCases.Checkout;
using POSOpen.Domain.Entities;
using POSOpen.Domain.Enums;

namespace POSOpen.Tests.Unit.Checkout;

public sealed class GetTransactionStatusUseCaseTests
{
	private static readonly DateTime FixedNow = new(2026, 4, 1, 10, 0, 0, DateTimeKind.Utc);
	private static readonly Guid StaffId = Guid.NewGuid();

	[Fact]
	public async Task ExecuteAsync_WhenCartNotFound_ReturnsFailure()
	{
		var cartRepo = new Mock<ICartSessionRepository>();
		cartRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync((CartSession?)null);
		var attemptRepo = new Mock<ICheckoutPaymentAttemptRepository>();

		var sut = new GetTransactionStatusUseCase(cartRepo.Object, attemptRepo.Object);

		var result = await sut.ExecuteAsync(Guid.NewGuid());

		result.IsSuccess.Should().BeFalse();
		result.ErrorCode.Should().Be(CartCheckoutConstants.ErrorCartNotFound);
	}

	[Fact]
	public async Task ExecuteAsync_WhenApprovedPaymentExists_ReturnsCompletedOnlineStatus()
	{
		var cartId = Guid.NewGuid();
		var cart = BuildOpenCart(cartId);
		var cartRepo = new Mock<ICartSessionRepository>();
		cartRepo.Setup(x => x.GetByIdAsync(cartId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(cart);

		var attempt = BuildApprovedAttempt(cartId);
		var attemptRepo = new Mock<ICheckoutPaymentAttemptRepository>();
		attemptRepo.Setup(x => x.ListByCartSessionAsync(cartId, It.IsAny<CancellationToken>()))
			.ReturnsAsync([attempt]);

		var sut = new GetTransactionStatusUseCase(cartRepo.Object, attemptRepo.Object);

		var result = await sut.ExecuteAsync(cartId);

		result.IsSuccess.Should().BeTrue();
		result.Payload!.TransactionStatus.Should().Be(TransactionStatus.CompletedOnline);
		result.Payload.CartSessionId.Should().Be(cartId);
	}

	[Fact]
	public async Task ExecuteAsync_WhenOpenCartWithNoApprovedPayment_ReturnsDeferredPaymentStatus()
	{
		var cartId = Guid.NewGuid();
		var cart = BuildOpenCart(cartId);
		var cartRepo = new Mock<ICartSessionRepository>();
		cartRepo.Setup(x => x.GetByIdAsync(cartId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(cart);

		var attemptRepo = new Mock<ICheckoutPaymentAttemptRepository>();
		attemptRepo.Setup(x => x.ListByCartSessionAsync(cartId, It.IsAny<CancellationToken>()))
			.ReturnsAsync([]);

		var sut = new GetTransactionStatusUseCase(cartRepo.Object, attemptRepo.Object);

		var result = await sut.ExecuteAsync(cartId);

		result.IsSuccess.Should().BeTrue();
		result.Payload!.TransactionStatus.Should().Be(TransactionStatus.DeferredPayment);
	}

	[Fact]
	public async Task ExecuteAsync_WhenClosedCartWithNoApprovedPayment_ReturnsErrorStatus()
	{
		var cartId = Guid.NewGuid();
		var cart = BuildOpenCart(cartId);
		cart.Status = CartStatus.Completed;
		var cartRepo = new Mock<ICartSessionRepository>();
		cartRepo.Setup(x => x.GetByIdAsync(cartId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(cart);

		var attemptRepo = new Mock<ICheckoutPaymentAttemptRepository>();
		attemptRepo.Setup(x => x.ListByCartSessionAsync(cartId, It.IsAny<CancellationToken>()))
			.ReturnsAsync([]);

		var sut = new GetTransactionStatusUseCase(cartRepo.Object, attemptRepo.Object);

		var result = await sut.ExecuteAsync(cartId);

		result.IsSuccess.Should().BeTrue();
		result.Payload!.TransactionStatus.Should().Be(TransactionStatus.Error);
	}

	[Fact]
	public async Task ExecuteAsync_WhenApprovedPaymentExists_StatusMessageAndNextStepsArePopulated()
	{
		var cartId = Guid.NewGuid();
		var cart = BuildOpenCart(cartId);
		var cartRepo = new Mock<ICartSessionRepository>();
		cartRepo.Setup(x => x.GetByIdAsync(cartId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(cart);

		var attempt = BuildApprovedAttempt(cartId);
		var attemptRepo = new Mock<ICheckoutPaymentAttemptRepository>();
		attemptRepo.Setup(x => x.ListByCartSessionAsync(cartId, It.IsAny<CancellationToken>()))
			.ReturnsAsync([attempt]);

		var sut = new GetTransactionStatusUseCase(cartRepo.Object, attemptRepo.Object);

		var result = await sut.ExecuteAsync(cartId);

		result.Payload!.StatusMessage.Should().NotBeNullOrWhiteSpace();
		result.Payload.NextStepsMessage.Should().NotBeNullOrWhiteSpace();
	}

	// ─── Helpers ─────────────────────────────────────────────────────────

	private static CartSession BuildOpenCart(Guid cartId)
	{
		var cart = CartSession.Create(cartId, null, StaffId, FixedNow);
		cart.LineItems.Add(CartLineItem.Create(Guid.NewGuid(), cartId, "Admission", FulfillmentContext.Admission, null, 1, 2500, "USD", FixedNow));
		return cart;
	}

	private static CheckoutPaymentAttempt BuildApprovedAttempt(Guid cartId) =>
		CheckoutPaymentAttempt.Create(
			Guid.NewGuid(),
			cartId,
			2500,
			"USD",
			CheckoutPaymentAuthorizationStatus.Approved,
			"tok_test_123",
			null,
			FixedNow);
}

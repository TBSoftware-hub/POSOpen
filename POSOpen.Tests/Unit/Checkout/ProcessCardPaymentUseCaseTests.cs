using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.Abstractions.Services;
using POSOpen.Application.Results;
using POSOpen.Application.UseCases.Checkout;
using POSOpen.Domain.Entities;
using POSOpen.Domain.Enums;
using POSOpen.Domain.Policies;

namespace POSOpen.Tests.Unit.Checkout;

public sealed class ProcessCardPaymentUseCaseTests
{
	private static readonly Guid StaffId = Guid.NewGuid();
	private static readonly DateTime FixedNow = new(2026, 4, 1, 10, 0, 0, DateTimeKind.Utc);

	[Fact]
	public async Task ExecuteAsync_WhenCartMissing_ReturnsFailure()
	{
		var repo = new Mock<ICartSessionRepository>();
		repo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((CartSession?)null);

		var sut = new ProcessCardPaymentUseCase(
			repo.Object,
			BuildValidateUseCase(repo),
			new Mock<ICheckoutPaymentAttemptRepository>().Object,
			new Mock<ICardReaderDeviceService>().Object,
			MockClock().Object,
			new Mock<ILogger<ProcessCardPaymentUseCase>>().Object);

		var result = await sut.ExecuteAsync(Guid.NewGuid());

		result.IsSuccess.Should().BeFalse();
		result.ErrorCode.Should().Be(CartCheckoutConstants.ErrorCartNotFound);
	}

	[Fact]
	public async Task ExecuteAsync_WhenAuthorizationApproved_PersistsAttemptAndReturnsAuthorized()
	{
		var cart = BuildCart();
		var repo = new Mock<ICartSessionRepository>();
		repo.Setup(x => x.GetByIdAsync(cart.Id, It.IsAny<CancellationToken>())).ReturnsAsync(cart);

		var paymentAttemptRepo = new Mock<ICheckoutPaymentAttemptRepository>();
		paymentAttemptRepo.Setup(x => x.AddAsync(It.IsAny<CheckoutPaymentAttempt>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync((CheckoutPaymentAttempt attempt, CancellationToken _) => attempt);

		var cardReader = new Mock<ICardReaderDeviceService>();
		cardReader.Setup(x => x.AuthorizeAsync(It.IsAny<CardAuthorizationRequest>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(AppResult<CardAuthorizationDto>.Success(
				new CardAuthorizationDto(CheckoutPaymentAuthorizationStatus.Approved, "tok_123", null),
				"Card authorized successfully."));

		var sut = new ProcessCardPaymentUseCase(repo.Object, BuildValidateUseCase(repo), paymentAttemptRepo.Object, cardReader.Object, MockClock().Object, new Mock<ILogger<ProcessCardPaymentUseCase>>().Object);

		var result = await sut.ExecuteAsync(cart.Id);

		result.IsSuccess.Should().BeTrue();
		result.Payload!.IsAuthorized.Should().BeTrue();
		result.Payload.Attempt.ProcessorReference.Should().Be("tok_123");
		paymentAttemptRepo.Verify(x => x.AddAsync(It.IsAny<CheckoutPaymentAttempt>(), It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task ExecuteAsync_WhenReaderUnavailable_PersistsAttemptAndReturnsUnauthorized()
	{
		var cart = BuildCart();
		var repo = new Mock<ICartSessionRepository>();
		repo.Setup(x => x.GetByIdAsync(cart.Id, It.IsAny<CancellationToken>())).ReturnsAsync(cart);

		var paymentAttemptRepo = new Mock<ICheckoutPaymentAttemptRepository>();
		paymentAttemptRepo.Setup(x => x.AddAsync(It.IsAny<CheckoutPaymentAttempt>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync((CheckoutPaymentAttempt attempt, CancellationToken _) => attempt);

		var cardReader = new Mock<ICardReaderDeviceService>();
		cardReader.Setup(x => x.AuthorizeAsync(It.IsAny<CardAuthorizationRequest>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(AppResult<CardAuthorizationDto>.Success(
				new CardAuthorizationDto(CheckoutPaymentAuthorizationStatus.Unavailable, null, DeviceDiagnosticCode.CardReaderUnavailable),
				CartCheckoutConstants.SafeCardReaderUnavailableMessage));

		var sut = new ProcessCardPaymentUseCase(repo.Object, BuildValidateUseCase(repo), paymentAttemptRepo.Object, cardReader.Object, MockClock().Object, new Mock<ILogger<ProcessCardPaymentUseCase>>().Object);

		var result = await sut.ExecuteAsync(cart.Id);

		result.IsSuccess.Should().BeTrue();
		result.Payload!.IsAuthorized.Should().BeFalse();
		result.Payload.Attempt.DiagnosticCode.Should().Be(DeviceDiagnosticCode.CardReaderUnavailable);
		result.UserMessage.Should().Be(CartCheckoutConstants.SafeCardReaderUnavailableMessage);
	}

	[Fact]
	public async Task ExecuteAsync_WhenCartIsIncompatible_ReturnsCompatibilityFailureWithoutAuthorizing()
	{
		var cart = CartSession.Create(Guid.NewGuid(), null, StaffId, FixedNow);
		cart.LineItems.Add(CartLineItem.Create(Guid.NewGuid(), cart.Id, "Catering", FulfillmentContext.CateringAddon, null, 1, 12000, "USD", FixedNow));

		var repo = new Mock<ICartSessionRepository>();
		repo.Setup(x => x.GetByIdAsync(cart.Id, It.IsAny<CancellationToken>())).ReturnsAsync(cart);

		var paymentAttemptRepo = new Mock<ICheckoutPaymentAttemptRepository>();
		var cardReader = new Mock<ICardReaderDeviceService>();

		var validate = BuildValidateUseCase(repo, new CateringRequiresPartyDepositRule());
		var sut = new ProcessCardPaymentUseCase(repo.Object, validate, paymentAttemptRepo.Object, cardReader.Object, MockClock().Object, new Mock<ILogger<ProcessCardPaymentUseCase>>().Object);

		var result = await sut.ExecuteAsync(cart.Id);

		result.IsSuccess.Should().BeFalse();
		result.ErrorCode.Should().Be(CartCheckoutConstants.ErrorCateringWithoutDeposit);
		cardReader.Verify(x => x.AuthorizeAsync(It.IsAny<CardAuthorizationRequest>(), It.IsAny<CancellationToken>()), Times.Never);
		paymentAttemptRepo.Verify(x => x.AddAsync(It.IsAny<CheckoutPaymentAttempt>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	private static CartSession BuildCart()
	{
		var cart = CartSession.Create(Guid.NewGuid(), null, StaffId, FixedNow);
		cart.LineItems.Add(CartLineItem.Create(Guid.NewGuid(), cart.Id, "Admission", FulfillmentContext.Admission, null, 1, 2500, "USD", FixedNow));
		return cart;
	}

	private static Mock<IUtcClock> MockClock()
	{
		var clock = new Mock<IUtcClock>();
		clock.Setup(x => x.UtcNow).Returns(FixedNow);
		return clock;
	}

	private static ValidateCartCompatibilityUseCase BuildValidateUseCase(
		Mock<ICartSessionRepository> repo,
		params ICartCompatibilityRule[] rules)
	{
		return new ValidateCartCompatibilityUseCase(repo.Object, rules);
	}
}
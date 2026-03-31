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
using POSOpen.Features.Checkout.ViewModels;

namespace POSOpen.Tests.Unit.Checkout;

public sealed class PaymentCaptureViewModelTests
{
        [Fact]
        public async Task InitializeCommand_LoadsAmountLabel()
        {
                var cartId = Guid.NewGuid();
                var cartRepo = new Mock<ICartSessionRepository>();
                var cart = BuildCart(cartId);
                cartRepo.Setup(x => x.GetByIdAsync(cartId, It.IsAny<CancellationToken>())).ReturnsAsync(cart);
                var summary = new GetCartPaymentSummaryUseCase(cartRepo.Object);
                var payment = BuildPaymentUseCase(
                        cartRepo,
                        new CardAuthorizationDto(CheckoutPaymentAuthorizationStatus.Unavailable, null, DeviceDiagnosticCode.CardReaderUnavailable),
                        CartCheckoutConstants.SafeCardReaderUnavailableMessage);

                var vm = new PaymentCaptureViewModel(summary, payment, new Mock<ICheckoutUiService>().Object)
                {
                        CartId = cartId.ToString()
                };

                await vm.InitializeCommand.ExecuteAsync(null);

                vm.AmountLabel.Should().NotBe("$0.00");
                vm.HasError.Should().BeFalse();
        }

        [Fact]
        public async Task AuthorizeCommand_WhenApproved_UpdatesAuthorizedState()
        {
                var cartId = Guid.NewGuid();
                var cartRepo = new Mock<ICartSessionRepository>();
                var cart = BuildCart(cartId);
                cartRepo.Setup(x => x.GetByIdAsync(cartId, It.IsAny<CancellationToken>())).ReturnsAsync(cart);
                var summary = new GetCartPaymentSummaryUseCase(cartRepo.Object);
                var payment = BuildPaymentUseCase(
                        cartRepo,
                        new CardAuthorizationDto(CheckoutPaymentAuthorizationStatus.Approved, "tok_123", null),
                        "Card authorized successfully.");

                var vm = new PaymentCaptureViewModel(summary, payment, new Mock<ICheckoutUiService>().Object)
                {
                        CartId = cartId.ToString()
                };
                await vm.InitializeCommand.ExecuteAsync(null);

                await vm.AuthorizeCommand.ExecuteAsync(null);

                vm.IsAuthorized.Should().BeTrue();
                vm.ProcessorReference.Should().Be("tok_123");
        }

        [Fact]
        public async Task AuthorizeCommand_WhenUnavailable_ShowsDiagnosticAndUnauthorized()
        {
                var cartId = Guid.NewGuid();
                var cartRepo = new Mock<ICartSessionRepository>();
                var cart = BuildCart(cartId);
                cartRepo.Setup(x => x.GetByIdAsync(cartId, It.IsAny<CancellationToken>())).ReturnsAsync(cart);
                var summary = new GetCartPaymentSummaryUseCase(cartRepo.Object);
                var payment = BuildPaymentUseCase(
                        cartRepo,
                        new CardAuthorizationDto(CheckoutPaymentAuthorizationStatus.Unavailable, null, DeviceDiagnosticCode.CardReaderUnavailable),
                        CartCheckoutConstants.SafeCardReaderUnavailableMessage);

                var vm = new PaymentCaptureViewModel(summary, payment, new Mock<ICheckoutUiService>().Object)
                {
                        CartId = cartId.ToString()
                };
                await vm.InitializeCommand.ExecuteAsync(null);

                await vm.AuthorizeCommand.ExecuteAsync(null);

                vm.IsAuthorized.Should().BeFalse();
                vm.DiagnosticCode.Should().Be(DeviceDiagnosticCode.CardReaderUnavailable);
                vm.StatusMessage.Should().Be(CartCheckoutConstants.SafeCardReaderUnavailableMessage);
        }

        private static CartSession BuildCart(Guid cartId)
        {
                var cart = CartSession.Create(cartId, null, Guid.NewGuid(), DateTime.UtcNow);
                cart.LineItems.Add(CartLineItem.Create(Guid.NewGuid(), cartId, "Admission", FulfillmentContext.Admission, null, 1, 2500, "USD", DateTime.UtcNow));
                return cart;
        }

        private static ProcessCardPaymentUseCase BuildPaymentUseCase(
                Mock<ICartSessionRepository> cartRepo,
                CardAuthorizationDto authorizationDto,
                string userMessage)
        {
                var paymentAttemptRepo = new Mock<ICheckoutPaymentAttemptRepository>();
                paymentAttemptRepo.Setup(x => x.AddAsync(It.IsAny<CheckoutPaymentAttempt>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync((CheckoutPaymentAttempt attempt, CancellationToken _) => attempt);

                var cardReader = new Mock<ICardReaderDeviceService>();
                cardReader.Setup(x => x.AuthorizeAsync(It.IsAny<CardAuthorizationRequest>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(AppResult<CardAuthorizationDto>.Success(authorizationDto, userMessage));

                var clock = new Mock<IUtcClock>();
                clock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);

                var logger = new Mock<ILogger<ProcessCardPaymentUseCase>>();

                var validate = new ValidateCartCompatibilityUseCase(cartRepo.Object, Array.Empty<ICartCompatibilityRule>());
                return new ProcessCardPaymentUseCase(cartRepo.Object, validate, paymentAttemptRepo.Object, cardReader.Object, clock.Object, logger.Object);
        }
}

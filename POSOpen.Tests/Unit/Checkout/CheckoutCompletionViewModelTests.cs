using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.Abstractions.Services;
using POSOpen.Application.Results;
using POSOpen.Application.UseCases.Checkout;
using POSOpen.Domain.Entities;
using POSOpen.Domain.Enums;
using POSOpen.Features.Checkout.ViewModels;

namespace POSOpen.Tests.Unit.Checkout;

public sealed class CheckoutCompletionViewModelTests
{
	private static readonly DateTime FixedNow = new(2026, 4, 1, 10, 0, 0, DateTimeKind.Utc);
	private static readonly Guid StaffId = Guid.NewGuid();

	[Fact]
	public async Task InitializeCommand_WhenCartSessionIdIsInvalid_SetsErrorMessage()
	{
		var vm = BuildViewModel();
		vm.CartSessionIdParam = "not-a-guid";

		await vm.InitializeCommand.ExecuteAsync(null);

		vm.HasError.Should().BeTrue();
		vm.ErrorMessage.Should().NotBeNullOrWhiteSpace();
		vm.IsLoading.Should().BeFalse();
	}

	[Fact]
	public async Task InitializeCommand_WhenGetStatusFails_SetsErrorMessage()
	{
		var cartId = Guid.NewGuid();
		var cartRepo = new Mock<ICartSessionRepository>();
		cartRepo.Setup(x => x.GetByIdAsync(cartId, It.IsAny<CancellationToken>()))
			.ReturnsAsync((CartSession?)null);

		var vm = BuildViewModel(cartRepo: cartRepo);
		vm.CartSessionIdParam = cartId.ToString();

		await vm.InitializeCommand.ExecuteAsync(null);

		vm.HasError.Should().BeTrue();
		vm.ErrorMessage.Should().NotBeNullOrWhiteSpace();
	}

	[Fact]
	public async Task InitializeCommand_WhenApprovedPaymentExists_SetsIsOnlineCompletionTrue()
	{
		var cartId = Guid.NewGuid();
		var cart = BuildCart(cartId);
		var cartRepo = new Mock<ICartSessionRepository>();
		cartRepo.Setup(x => x.GetByIdAsync(cartId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(cart);

		var attempt = CheckoutPaymentAttempt.Create(
			Guid.NewGuid(), cartId, 2500, "USD",
			CheckoutPaymentAuthorizationStatus.Approved, "tok_123", null, FixedNow);
		var attemptRepo = new Mock<ICheckoutPaymentAttemptRepository>();
		attemptRepo.Setup(x => x.ListByCartSessionAsync(cartId, It.IsAny<CancellationToken>()))
			.ReturnsAsync([attempt]);

		var vm = BuildViewModel(cartRepo: cartRepo, attemptRepo: attemptRepo);
		vm.CartSessionIdParam = cartId.ToString();

		await vm.InitializeCommand.ExecuteAsync(null);

		vm.HasError.Should().BeFalse();
		vm.IsOnlineCompletion.Should().BeTrue();
		vm.StatusMessage.Should().NotBeNullOrWhiteSpace();
	}

	[Fact]
	public async Task InitializeCommand_WhenPrinterDeferred_SetsIsPrintDeferredTrue()
	{
		var cartId = Guid.NewGuid();
		var cart = BuildCart(cartId);
		var cartRepo = new Mock<ICartSessionRepository>();
		cartRepo.Setup(x => x.GetByIdAsync(cartId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(cart);

		var attempt = CheckoutPaymentAttempt.Create(
			Guid.NewGuid(), cartId, 2500, "USD",
			CheckoutPaymentAuthorizationStatus.Approved, "tok_123", null, FixedNow);
		var attemptRepo = new Mock<ICheckoutPaymentAttemptRepository>();
		attemptRepo.Setup(x => x.ListByCartSessionAsync(cartId, It.IsAny<CancellationToken>()))
			.ReturnsAsync([attempt]);

		var printerService = new Mock<IPrinterDeviceService>();
		printerService.Setup(x => x.PrintReceiptAsync(It.IsAny<ReceiptData>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(AppResult<PrinterResultDto>.Success(
				new PrinterResultDto(PrintStatus.Deferred, DeviceDiagnosticCode.PrinterUnavailable, "Deferred."),
				"Deferred."));

		var vm = BuildViewModel(cartRepo: cartRepo, attemptRepo: attemptRepo, printerService: printerService);
		vm.CartSessionIdParam = cartId.ToString();

		await vm.InitializeCommand.ExecuteAsync(null);

		vm.HasError.Should().BeFalse();
		vm.IsPrintDeferred.Should().BeTrue();
		vm.ReceiptStatusMessage.Should().NotBeNullOrWhiteSpace();
	}

	[Fact]
	public async Task InitializeCommand_WhenPrintSucceeds_SetsOperationIdReference()
	{
		var cartId = Guid.NewGuid();
		var cart = BuildCart(cartId);
		var cartRepo = new Mock<ICartSessionRepository>();
		cartRepo.Setup(x => x.GetByIdAsync(cartId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(cart);

		var attempt = CheckoutPaymentAttempt.Create(
			Guid.NewGuid(), cartId, 2500, "USD",
			CheckoutPaymentAuthorizationStatus.Approved, "tok_123", null, FixedNow);
		var attemptRepo = new Mock<ICheckoutPaymentAttemptRepository>();
		attemptRepo.Setup(x => x.ListByCartSessionAsync(cartId, It.IsAny<CancellationToken>()))
			.ReturnsAsync([attempt]);

		var vm = BuildViewModel(cartRepo: cartRepo, attemptRepo: attemptRepo);
		vm.CartSessionIdParam = cartId.ToString();

		await vm.InitializeCommand.ExecuteAsync(null);

		vm.HasError.Should().BeFalse();
		vm.HasOperationReference.Should().BeTrue();
		vm.OperationIdReference.Should().NotBeNullOrWhiteSpace();
	}

	[Fact]
	public async Task InitializeCommand_SetsIsLoadingFalseAfterCompletion()
	{
		var cartId = Guid.NewGuid();
		var cart = BuildCart(cartId);
		var cartRepo = new Mock<ICartSessionRepository>();
		cartRepo.Setup(x => x.GetByIdAsync(cartId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(cart);

		var vm = BuildViewModel(cartRepo: cartRepo);
		vm.CartSessionIdParam = cartId.ToString();

		await vm.InitializeCommand.ExecuteAsync(null);

		vm.IsLoading.Should().BeFalse();
	}

	[Fact]
	public async Task NewTransactionCommand_CallsStartNewTransactionAsync()
	{
		var uiService = new Mock<ICheckoutUiService>();
		uiService.Setup(x => x.StartNewTransactionAsync()).Returns(Task.CompletedTask);

		var vm = BuildViewModel(uiService: uiService);

		await vm.NewTransactionCommand.ExecuteAsync(null);

		uiService.Verify(x => x.StartNewTransactionAsync(), Times.Once);
	}

	// ─── Helpers ─────────────────────────────────────────────────────────

	private static CartSession BuildCart(Guid cartId)
	{
		var cart = CartSession.Create(cartId, null, StaffId, FixedNow);
		cart.LineItems.Add(CartLineItem.Create(Guid.NewGuid(), cartId, "Admission", FulfillmentContext.Admission, null, 1, 2500, "USD", FixedNow));
		return cart;
	}

	private static CheckoutCompletionViewModel BuildViewModel(
		Mock<ICartSessionRepository>? cartRepo = null,
		Mock<ICheckoutPaymentAttemptRepository>? attemptRepo = null,
		Mock<IPrinterDeviceService>? printerService = null,
		Mock<ICheckoutUiService>? uiService = null)
	{
		var clock = new Mock<IUtcClock>();
		clock.Setup(x => x.UtcNow).Returns(FixedNow);

		var repo = cartRepo ?? new Mock<ICartSessionRepository>();
		var attempts = attemptRepo ?? new Mock<ICheckoutPaymentAttemptRepository>();
		if (attemptRepo is null)
		{
			attempts.Setup(x => x.ListByCartSessionAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync([]);
		}

		var getStatus = new GetTransactionStatusUseCase(repo.Object, attempts.Object);

		var opIdService = new Mock<IOperationIdService>();
		opIdService.Setup(x => x.GenerateOperationId()).Returns(Guid.NewGuid());
		opIdService.Setup(x => x.SaveOperationAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
			.Returns(Task.CompletedTask);

		var printer = printerService ?? new Mock<IPrinterDeviceService>();
		if (printerService is null)
		{
			printer.Setup(x => x.PrintReceiptAsync(It.IsAny<ReceiptData>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(AppResult<PrinterResultDto>.Success(
					new PrinterResultDto(PrintStatus.Success, null, "Printed."), "Printed."));
		}

		var receiptMetaRepo = new Mock<IReceiptMetadataRepository>();
		receiptMetaRepo.Setup(x => x.AddAsync(It.IsAny<ReceiptMetadata>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync((ReceiptMetadata m, CancellationToken _) => m);

		var printReceipt = new PrintReceiptUseCase(
			repo.Object,
			opIdService.Object,
			printer.Object,
			receiptMetaRepo.Object,
			clock.Object,
			new Mock<ILogger<PrintReceiptUseCase>>().Object);

		var ui = uiService ?? new Mock<ICheckoutUiService>();

		return new CheckoutCompletionViewModel(
			printReceipt,
			getStatus,
			ui.Object,
			new Mock<ILogger<CheckoutCompletionViewModel>>().Object);
	}
}

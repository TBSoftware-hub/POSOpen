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

public sealed class PrintReceiptUseCaseTests
{
	private static readonly DateTime FixedNow = new(2026, 4, 1, 10, 0, 0, DateTimeKind.Utc);
	private static readonly Guid StaffId = Guid.NewGuid();

	[Fact]
	public async Task ExecuteAsync_WhenCartNotFound_ReturnsFailure()
	{
		var cartRepo = new Mock<ICartSessionRepository>();
		cartRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync((CartSession?)null);

		var sut = BuildUseCase(cartRepo);

		var result = await sut.ExecuteAsync(Guid.NewGuid());

		result.IsSuccess.Should().BeFalse();
		result.ErrorCode.Should().Be(CartCheckoutConstants.ErrorCartNotFound);
	}

	[Fact]
	public async Task ExecuteAsync_WhenPrinterReturnsSuccess_ReturnsSuccessWithPrintedStatus()
	{
		var cartId = Guid.NewGuid();
		var cart = BuildCart(cartId);
		var cartRepo = new Mock<ICartSessionRepository>();
		cartRepo.Setup(x => x.GetByIdAsync(cartId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(cart);

		var printerService = new Mock<IPrinterDeviceService>();
		printerService.Setup(x => x.PrintReceiptAsync(It.IsAny<ReceiptData>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(AppResult<PrinterResultDto>.Success(
				new PrinterResultDto(PrintStatus.Success, null, "Receipt printed successfully."),
				"Receipt printed successfully."));

		var receiptMetaRepo = new Mock<IReceiptMetadataRepository>();
		receiptMetaRepo.Setup(x => x.AddAsync(It.IsAny<ReceiptMetadata>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync((ReceiptMetadata m, CancellationToken _) => m);

		var sut = BuildUseCase(cartRepo, printerService: printerService, receiptMetaRepo: receiptMetaRepo);

		var result = await sut.ExecuteAsync(cartId);

		result.IsSuccess.Should().BeTrue();
		result.Payload!.PrintStatus.Should().Be(PrintStatus.Success);
		result.Payload.TransactionCompleted.Should().BeTrue();
		receiptMetaRepo.Verify(x => x.AddAsync(It.IsAny<ReceiptMetadata>(), It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task ExecuteAsync_WhenPrinterDeferred_ReturnsSuccessWithDeferredStatus()
	{
		var cartId = Guid.NewGuid();
		var cart = BuildCart(cartId);
		var cartRepo = new Mock<ICartSessionRepository>();
		cartRepo.Setup(x => x.GetByIdAsync(cartId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(cart);

		var printerService = new Mock<IPrinterDeviceService>();
		printerService.Setup(x => x.PrintReceiptAsync(It.IsAny<ReceiptData>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(AppResult<PrinterResultDto>.Success(
				new PrinterResultDto(PrintStatus.Deferred, DeviceDiagnosticCode.PrinterUnavailable, "Printer unavailable."),
				"Printer unavailable."));

		var receiptMetaRepo = new Mock<IReceiptMetadataRepository>();
		receiptMetaRepo.Setup(x => x.AddAsync(It.IsAny<ReceiptMetadata>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync((ReceiptMetadata m, CancellationToken _) => m);

		var sut = BuildUseCase(cartRepo, printerService: printerService, receiptMetaRepo: receiptMetaRepo);

		var result = await sut.ExecuteAsync(cartId);

		result.IsSuccess.Should().BeTrue("printer failure does not block transaction completion");
		result.Payload!.PrintStatus.Should().Be(PrintStatus.Deferred);
		result.Payload.TransactionCompleted.Should().BeTrue();
	}

	[Fact]
	public async Task ExecuteAsync_WhenPrinterFails_StillPersistsReceiptMetadataAndReturnsSuccess()
	{
		var cartId = Guid.NewGuid();
		var cart = BuildCart(cartId);
		var cartRepo = new Mock<ICartSessionRepository>();
		cartRepo.Setup(x => x.GetByIdAsync(cartId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(cart);

		var printerService = new Mock<IPrinterDeviceService>();
		printerService.Setup(x => x.PrintReceiptAsync(It.IsAny<ReceiptData>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(AppResult<PrinterResultDto>.Failure("PRINTER_ERROR", "Failed to print."));

		var receiptMetaRepo = new Mock<IReceiptMetadataRepository>();
		receiptMetaRepo.Setup(x => x.AddAsync(It.IsAny<ReceiptMetadata>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync((ReceiptMetadata m, CancellationToken _) => m);

		var sut = BuildUseCase(cartRepo, printerService: printerService, receiptMetaRepo: receiptMetaRepo);

		var result = await sut.ExecuteAsync(cartId);

		result.IsSuccess.Should().BeTrue("printer failure does not block transaction completion");
		result.Payload!.PrintStatus.Should().Be(PrintStatus.Failed);
		result.Payload.TransactionCompleted.Should().BeTrue();
		receiptMetaRepo.Verify(x => x.AddAsync(It.IsAny<ReceiptMetadata>(), It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task ExecuteAsync_AlwaysGeneratesAndSavesOperationId()
	{
		var cartId = Guid.NewGuid();
		var cart = BuildCart(cartId);
		var cartRepo = new Mock<ICartSessionRepository>();
		cartRepo.Setup(x => x.GetByIdAsync(cartId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(cart);

		var opIdService = new Mock<IOperationIdService>();
		var capturedOpId = Guid.NewGuid();
		opIdService.Setup(x => x.GenerateOperationId()).Returns(capturedOpId);
		opIdService.Setup(x => x.SaveOperationAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
			.Returns(Task.CompletedTask);

		var sut = BuildUseCase(cartRepo, opIdService: opIdService);

		var result = await sut.ExecuteAsync(cartId);

		result.IsSuccess.Should().BeTrue();
		result.Payload!.OperationId.Should().Be(capturedOpId);
		opIdService.Verify(x => x.SaveOperationAsync(capturedOpId, "PrintReceipt", It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
	}

	// ─── Helpers ─────────────────────────────────────────────────────────

	private static CartSession BuildCart(Guid? cartId = null)
	{
		var id = cartId ?? Guid.NewGuid();
		var cart = CartSession.Create(id, null, StaffId, FixedNow);
		cart.LineItems.Add(CartLineItem.Create(Guid.NewGuid(), id, "Admission", FulfillmentContext.Admission, null, 1, 2500, "USD", FixedNow));
		return cart;
	}

	private static PrintReceiptUseCase BuildUseCase(
		Mock<ICartSessionRepository> cartRepo,
		Mock<IOperationIdService>? opIdService = null,
		Mock<IPrinterDeviceService>? printerService = null,
		Mock<IReceiptMetadataRepository>? receiptMetaRepo = null)
	{
		var clock = new Mock<IUtcClock>();
		clock.Setup(x => x.UtcNow).Returns(FixedNow);

		var opId = opIdService ?? new Mock<IOperationIdService>();
		if (opIdService is null)
		{
			opId.Setup(x => x.GenerateOperationId()).Returns(Guid.NewGuid());
			opId.Setup(x => x.SaveOperationAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
		}

		var printer = printerService ?? new Mock<IPrinterDeviceService>();
		if (printerService is null)
		{
			printer.Setup(x => x.PrintReceiptAsync(It.IsAny<ReceiptData>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(AppResult<PrinterResultDto>.Success(
					new PrinterResultDto(PrintStatus.Success, null, "Printed."), "Printed."));
		}

		var metaRepo = receiptMetaRepo ?? new Mock<IReceiptMetadataRepository>();
		if (receiptMetaRepo is null)
		{
			metaRepo.Setup(x => x.AddAsync(It.IsAny<ReceiptMetadata>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync((ReceiptMetadata m, CancellationToken _) => m);
		}

		return new PrintReceiptUseCase(
			cartRepo.Object,
			opId.Object,
			printer.Object,
			metaRepo.Object,
			clock.Object,
			new Mock<ILogger<PrintReceiptUseCase>>().Object);
	}
}

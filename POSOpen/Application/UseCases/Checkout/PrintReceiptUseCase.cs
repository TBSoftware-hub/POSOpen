using Microsoft.Extensions.Logging;
using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.Abstractions.Services;
using POSOpen.Application.Results;
using POSOpen.Domain.Entities;
using POSOpen.Domain.Enums;

namespace POSOpen.Application.UseCases.Checkout;

public sealed class PrintReceiptUseCase
{
	private readonly ICartSessionRepository _cartSessionRepository;
	private readonly IOperationIdService _operationIdService;
	private readonly IPrinterDeviceService _printerDeviceService;
	private readonly IReceiptMetadataRepository _receiptMetadataRepository;
	private readonly IUtcClock _clock;
	private readonly ILogger<PrintReceiptUseCase> _logger;

	public PrintReceiptUseCase(
		ICartSessionRepository cartSessionRepository,
		IOperationIdService operationIdService,
		IPrinterDeviceService printerDeviceService,
		IReceiptMetadataRepository receiptMetadataRepository,
		IUtcClock clock,
		ILogger<PrintReceiptUseCase> logger)
	{
		_cartSessionRepository = cartSessionRepository;
		_operationIdService = operationIdService;
		_printerDeviceService = printerDeviceService;
		_receiptMetadataRepository = receiptMetadataRepository;
		_clock = clock;
		_logger = logger;
	}

	public async Task<AppResult<PrintReceiptResultDto>> ExecuteAsync(Guid cartSessionId, CancellationToken ct = default)
	{
		var cart = await _cartSessionRepository.GetByIdAsync(cartSessionId, ct);
		if (cart is null)
		{
			return AppResult<PrintReceiptResultDto>.Failure(
				CartCheckoutConstants.ErrorCartNotFound,
				CartCheckoutConstants.SafeCartNotFoundMessage);
		}

		var operationId = _operationIdService.GenerateOperationId();
		var currencyCode = cart.LineItems.FirstOrDefault()?.CurrencyCode ?? "USD";

		await _operationIdService.SaveOperationAsync(
			operationId,
			"PrintReceipt",
			$"{{\"cartSessionId\":\"{cartSessionId}\"}}",
			ct);

		var receiptData = new ReceiptData(
			TransactionId: cart.Id,
			OperationId: operationId,
			AmountCents: cart.TotalAmountCents,
			CurrencyCode: currencyCode,
			ItemCount: cart.LineItems.Count,
			TransactionAtUtc: _clock.UtcNow);

		var printResult = await _printerDeviceService.PrintReceiptAsync(receiptData, ct);

		// Printer failure does NOT block transaction completion — always persist and return success.
		var printStatus = printResult.IsSuccess && printResult.Payload is not null
			? printResult.Payload.PrintStatus
			: PrintStatus.Failed;

		var diagnosticCode = printResult.Payload?.DiagnosticCode;
		DateTime? printedAtUtc = printStatus == PrintStatus.Success ? _clock.UtcNow : null;

		if (printStatus != PrintStatus.Success)
		{
			_logger.LogWarning(
				"Receipt print did not succeed for cart {CartSessionId}. OperationId: {OperationId}, PrintStatus: {PrintStatus}, DiagnosticCode: {DiagnosticCode}",
				cartSessionId,
				operationId,
				printStatus,
				diagnosticCode);
		}

		var metadata = ReceiptMetadata.Create(
			Guid.NewGuid(),
			operationId,
			cart.Id,
			cart.TotalAmountCents,
			currencyCode,
			cart.LineItems.Count,
			printedAtUtc,
			printStatus,
			diagnosticCode,
			_clock.UtcNow);

		await _receiptMetadataRepository.AddAsync(metadata, ct);

		var userMessage = printStatus switch
		{
			PrintStatus.Success => "Receipt printed successfully.",
			PrintStatus.Deferred => "Receipt printing is unavailable right now. A copy will be printed when the printer comes online.",
			PrintStatus.Failed => "Receipt could not be printed. Please provide a manual receipt if needed.",
			_ => "Receipt status unknown.",
		};

		var result = new PrintReceiptResultDto(
			OperationId: operationId,
			PrintStatus: printStatus,
			TransactionCompleted: true,
			DiagnosticCode: diagnosticCode,
			UserMessage: userMessage);

		return AppResult<PrintReceiptResultDto>.Success(result, userMessage);
	}
}

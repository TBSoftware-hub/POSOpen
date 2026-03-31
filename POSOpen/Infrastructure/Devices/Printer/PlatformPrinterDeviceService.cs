using POSOpen.Application.Abstractions.Services;
using POSOpen.Application.Results;
using POSOpen.Application.UseCases.Checkout;
using POSOpen.Domain.Enums;

namespace POSOpen.Infrastructure.Devices.Printer;

/// <summary>
/// V1 stub — printer hardware integration not yet available.
/// All receipts are deferred until a real printer adapter is implemented.
/// </summary>
public sealed class PlatformPrinterDeviceService : IPrinterDeviceService
{
	public Task<AppResult<PrinterResultDto>> PrintReceiptAsync(ReceiptData receiptData, CancellationToken ct = default)
	{
		return Task.FromResult(AppResult<PrinterResultDto>.Success(
			new PrinterResultDto(
				PrintStatus: PrintStatus.Deferred,
				DiagnosticCode: DeviceDiagnosticCode.PrinterUnavailable,
				UserMessage: "Receipt printing is unavailable right now. A copy will be printed when the printer comes online."),
			"Receipt deferred — printer not yet connected."));
	}
}

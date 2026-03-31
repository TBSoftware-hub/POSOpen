using POSOpen.Application.Results;
using POSOpen.Application.UseCases.Checkout;

namespace POSOpen.Application.Abstractions.Services;

public interface IPrinterDeviceService
{
	Task<AppResult<PrinterResultDto>> PrintReceiptAsync(ReceiptData receiptData, CancellationToken ct = default);
}

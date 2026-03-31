using POSOpen.Application.UseCases.Checkout;
using POSOpen.Application.Results;

namespace POSOpen.Application.Abstractions.Services;

public interface IScannerDeviceService
{
	Task<AppResult<ScannerCaptureDto>> CaptureAsync(CancellationToken ct = default);
}
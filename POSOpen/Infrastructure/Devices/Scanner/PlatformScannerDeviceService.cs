using POSOpen.Application.Abstractions.Services;
using POSOpen.Application.Results;
using POSOpen.Application.UseCases.Checkout;
using POSOpen.Domain.Enums;

namespace POSOpen.Infrastructure.Devices.Scanner;

public sealed class PlatformScannerDeviceService : IScannerDeviceService
{
	public Task<AppResult<ScannerCaptureDto>> CaptureAsync(CancellationToken ct = default)
	{
		return Task.FromResult(AppResult<ScannerCaptureDto>.Success(
			new ScannerCaptureDto(ScannerCaptureStatus.Unavailable, null, DeviceDiagnosticCode.ScannerUnavailable),
			CartCheckoutConstants.SafeScannerUnavailableMessage));
	}
}
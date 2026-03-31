using POSOpen.Domain.Enums;

namespace POSOpen.Application.UseCases.Checkout;

/// <summary>Result of a print receipt operation returned from IPrinterDeviceService.</summary>
public sealed record PrinterResultDto(
	PrintStatus PrintStatus,
	string? DiagnosticCode,
	string UserMessage);

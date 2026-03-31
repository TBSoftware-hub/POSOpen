using POSOpen.Domain.Enums;

namespace POSOpen.Application.UseCases.Checkout;

public sealed record ScannerCaptureDto(
	ScannerCaptureStatus Status,
	string? Token,
	string? DiagnosticCode);

public sealed record ScannerCaptureResultDto(
	ScannerResolutionAction Action,
	CartSessionDto Cart,
	Guid? SelectedLineItemId,
	string UserMessage,
	string? DiagnosticCode);

public sealed record CartPaymentSummaryDto(
	Guid CartSessionId,
	long AmountCents,
	string CurrencyCode);

public sealed record CardAuthorizationRequest(
	Guid CartSessionId,
	long AmountCents,
	string CurrencyCode);

public sealed record CardAuthorizationDto(
	CheckoutPaymentAuthorizationStatus Status,
	string? ProcessorReference,
	string? DiagnosticCode);

public sealed record CheckoutPaymentResultDto(
	CheckoutPaymentAttemptDto Attempt,
	bool IsAuthorized);
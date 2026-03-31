using POSOpen.Domain.Enums;

namespace POSOpen.Application.UseCases.Checkout;

public sealed record CartValidationIssueDto(
	string Code,
	ValidationSeverity Severity,
	string Message,
	string? FixLabel,
	CartValidationFixAction FixAction);

public sealed record CartValidationResultDto(
	bool IsValid,
	IReadOnlyList<CartValidationIssueDto> Issues);

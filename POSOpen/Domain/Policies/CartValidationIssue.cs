using POSOpen.Domain.Enums;

namespace POSOpen.Domain.Policies;

/// <summary>
/// Immutable value object representing a single compatibility issue found in a cart.
/// Produced by <see cref="ICartCompatibilityRule"/> implementations.
/// </summary>
public sealed record CartValidationIssue(
	string Code,
	ValidationSeverity Severity,
	string Message,
	string? FixLabel,
	CartValidationFixAction FixAction);

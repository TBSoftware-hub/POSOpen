using POSOpen.Domain.Enums;

namespace POSOpen.Features.Checkout.ViewModels;

/// <summary>Plain-object ViewModel for a single cart validation issue.</summary>
public sealed class ValidationIssueViewModel
{
	public required string Message { get; init; }
	public string? FixLabel { get; init; }
	public CartValidationFixAction FixAction { get; init; }
	public bool HasFix => FixAction != CartValidationFixAction.None;
}

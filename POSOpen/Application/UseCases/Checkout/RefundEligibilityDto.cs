using POSOpen.Domain.Enums;

namespace POSOpen.Application.UseCases.Checkout;

public sealed record RefundEligibilityDto(
	Guid CartSessionId,
	bool IsEligible,
	long EligibleAmountCents,
	string CurrencyCode,
	IReadOnlyList<RefundPath> AllowedPaths,
	string? IneligibilityReasonCode,
	string UserMessage);
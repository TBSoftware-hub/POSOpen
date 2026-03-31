using POSOpen.Domain.Enums;
using POSOpen.Shared.Operational;

namespace POSOpen.Application.UseCases.Checkout;

public sealed record SubmitRefundCommand(
	Guid CartSessionId,
	long AmountCents,
	string? Reason,
	RefundPath RequestedPath,
	OperationContext Context);
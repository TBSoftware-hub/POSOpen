using POSOpen.Shared.Operational;

namespace POSOpen.Application.UseCases.Checkout;

public sealed record DenyRefundApprovalCommand(
	Guid RefundId,
	string Reason,
	OperationContext Context);

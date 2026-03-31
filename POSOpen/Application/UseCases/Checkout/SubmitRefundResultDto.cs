using POSOpen.Domain.Enums;

namespace POSOpen.Application.UseCases.Checkout;

public sealed record SubmitRefundResultDto(
	Guid OperationId,
	Guid CartSessionId,
	RefundStatus Status,
	RefundPath Path,
	Guid ActorStaffId,
	string ActorRole,
	long RefundAmountCents,
	string CurrencyCode,
	string UserMessage);
using POSOpen.Domain.Enums;

namespace POSOpen.Application.UseCases.Admissions;

public sealed record AdmissionCompletionResultDto(
	Guid FamilyId,
	Guid OperationId,
	AdmissionSettlementStatus SettlementStatus,
	string SettlementStatusLabel,
	string ConfirmationCode,
	string ReceiptReference,
	string GuidanceMessage,
	long AmountCents,
	string CurrencyCode,
	DateTime CompletedAtUtc);

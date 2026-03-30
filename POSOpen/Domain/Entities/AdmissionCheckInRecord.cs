using POSOpen.Domain.Enums;

namespace POSOpen.Domain.Entities;

public sealed class AdmissionCheckInRecord
{
	private AdmissionCheckInRecord()
	{
	}

	public static AdmissionCheckInRecord Create(
		Guid id,
		Guid familyId,
		Guid operationId,
		AdmissionSettlementStatus settlementStatus,
		long amountCents,
		string currencyCode,
		DateTime occurredUtc,
		string confirmationCode,
		string receiptReference)
	{
		return new AdmissionCheckInRecord
		{
			Id = id,
			FamilyId = familyId,
			OperationId = operationId,
			CompletionStatus = "Completed",
			SettlementStatus = settlementStatus,
			AmountCents = amountCents,
			CurrencyCode = currencyCode,
			CompletedAtUtc = occurredUtc,
			SettlementDeferredAtUtc = settlementStatus == AdmissionSettlementStatus.DeferredQueued ? occurredUtc : null,
			ConfirmationCode = confirmationCode,
			ReceiptReference = receiptReference
		};
	}

	public Guid Id { get; init; }

	public Guid FamilyId { get; init; }

	public Guid OperationId { get; init; }

	public string CompletionStatus { get; init; } = "Completed";

	public AdmissionSettlementStatus SettlementStatus { get; init; }

	public long AmountCents { get; init; }

	public string CurrencyCode { get; init; } = "USD";

	public DateTime CompletedAtUtc { get; init; }

	public DateTime? SettlementDeferredAtUtc { get; init; }

	public string ConfirmationCode { get; init; } = string.Empty;

	public string ReceiptReference { get; init; } = string.Empty;
}

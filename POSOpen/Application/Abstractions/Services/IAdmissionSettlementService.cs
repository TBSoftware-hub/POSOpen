using POSOpen.Shared.Operational;

namespace POSOpen.Application.Abstractions.Services;

public interface IAdmissionSettlementService
{
	Task<AdmissionSettlementDecision> AttemptAuthorizationAsync(
		Guid familyId,
		long amountCents,
		string currencyCode,
		OperationContext operationContext,
		CancellationToken cancellationToken = default);
}

public enum AdmissionSettlementDecisionType
{
	Authorized = 1,
	DeferredEligible = 2,
	NonEligibleFailure = 3
}

public sealed record AdmissionSettlementDecision(
	AdmissionSettlementDecisionType DecisionType,
	string? ProcessorReference,
	string? FailureCode,
	string? FailureMessage);

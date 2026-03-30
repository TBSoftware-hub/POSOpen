using POSOpen.Application.Abstractions.Services;
using POSOpen.Shared.Operational;

namespace POSOpen.Infrastructure.Services;

public sealed class DefaultAdmissionSettlementService : IAdmissionSettlementService
{
	public Task<AdmissionSettlementDecision> AttemptAuthorizationAsync(
		Guid familyId,
		long amountCents,
		string currencyCode,
		OperationContext operationContext,
		CancellationToken cancellationToken = default)
	{
		var forceDeferred = string.Equals(
			Environment.GetEnvironmentVariable("POSOPEN_FORCE_DEFERRED_SETTLEMENT"),
			"1",
			StringComparison.Ordinal);

		if (forceDeferred)
		{
			return Task.FromResult(new AdmissionSettlementDecision(
				AdmissionSettlementDecisionType.DeferredEligible,
				null,
				"NETWORK_UNAVAILABLE",
				"Payment authorization is unavailable. Payment will be queued."));
		}

		var processorReference = $"AUTH-{operationContext.OperationId:N}"[..16].ToUpperInvariant();
		return Task.FromResult(new AdmissionSettlementDecision(
			AdmissionSettlementDecisionType.Authorized,
			processorReference,
			null,
			null));
	}
}

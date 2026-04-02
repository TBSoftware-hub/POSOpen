using POSOpen.Application;
using POSOpen.Application.Abstractions.Services;

namespace POSOpen.Infrastructure.Services;

public sealed class WorkflowCapabilityService : IWorkflowCapabilityService
{
	private static readonly HashSet<string> OfflineSupportedWorkflowKeys =
	[
		WorkflowKeys.Admissions,
		WorkflowKeys.Checkout,
		WorkflowKeys.PartyBookingUpdate,
	];

	private static readonly IReadOnlyDictionary<string, string> FallbackGuidance = new Dictionary<string, string>(StringComparer.Ordinal)
	{
		[WorkflowKeys.PaymentSettlement] = "Payment authorization is unavailable offline. Payment will be deferred and settled when connectivity is restored.",
		[WorkflowKeys.CloudSync] = "Sync is paused. Operations will sync automatically when internet is available.",
	};

	public bool IsOfflineSupported(string workflowKey) =>
		!string.IsNullOrWhiteSpace(workflowKey) && OfflineSupportedWorkflowKeys.Contains(workflowKey);

	public string GetOfflineFallbackGuidance(string workflowKey)
	{
		if (string.IsNullOrWhiteSpace(workflowKey))
		{
			return "This action is unavailable in offline mode.";
		}

		return FallbackGuidance.TryGetValue(workflowKey, out var guidance)
			? guidance
			: "This action is unavailable in offline mode.";
	}
}

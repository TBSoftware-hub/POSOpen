namespace POSOpen.Application.Abstractions.Services;

public interface IWorkflowCapabilityService
{
	bool IsOfflineSupported(string workflowKey);

	string GetOfflineFallbackGuidance(string workflowKey);
}

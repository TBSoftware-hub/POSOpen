using FluentAssertions;
using POSOpen.Application;
using POSOpen.Infrastructure.Services;

namespace POSOpen.Tests.Unit.Sync;

public sealed class WorkflowCapabilityServiceTests
{
	[Theory]
	[InlineData(WorkflowKeys.Admissions)]
	[InlineData(WorkflowKeys.Checkout)]
	[InlineData(WorkflowKeys.PartyBookingUpdate)]
	public void IsOfflineSupported_returns_true_for_supported_workflows(string workflowKey)
	{
		var service = new WorkflowCapabilityService();

		service.IsOfflineSupported(workflowKey).Should().BeTrue();
	}

	[Theory]
	[InlineData(WorkflowKeys.PaymentSettlement)]
	[InlineData(WorkflowKeys.CloudSync)]
	public void IsOfflineSupported_returns_false_for_network_only_workflows(string workflowKey)
	{
		var service = new WorkflowCapabilityService();

		service.IsOfflineSupported(workflowKey).Should().BeFalse();
	}

	[Theory]
	[InlineData(WorkflowKeys.PaymentSettlement)]
	[InlineData(WorkflowKeys.CloudSync)]
	public void GetOfflineFallbackGuidance_returns_non_empty_message(string workflowKey)
	{
		var service = new WorkflowCapabilityService();

		service.GetOfflineFallbackGuidance(workflowKey).Should().NotBeNullOrWhiteSpace();
	}
}

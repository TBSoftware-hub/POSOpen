using POSOpen.Domain.Enums;

namespace POSOpen.Application.UseCases.Admissions;

public enum FastPathEligibilityState
{
	Allowed = 1,
	Blocked = 2,
	RefreshRequired = 3
}

public sealed record FastPathCheckInEvaluationResultDto(
	Guid FamilyId,
	string FamilyDisplayName,
	WaiverStatus WaiverStatus,
	string WaiverStatusLabel,
	FastPathEligibilityState State,
	bool IsEligible,
	bool ShowRecoveryAction,
	bool ShowRefreshAction,
	string GuidanceMessage);

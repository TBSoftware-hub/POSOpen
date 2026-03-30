namespace POSOpen.Application.UseCases.Admissions;

public static class EvaluateFastPathCheckInConstants
{
	public const string ErrorAuthForbidden = "AUTH_FORBIDDEN";
	public const string ErrorFamilyNotFound = "FAMILY_NOT_FOUND";
	public const string ErrorFastPathUnavailable = "FAST_PATH_UNAVAILABLE";

	public const string SafeAuthForbiddenMessage = "You do not have access to this action.";
	public const string SafeFamilyNotFoundMessage = "Family profile is no longer available. Return to lookup and try again.";
	public const string SafeFastPathUnavailableMessage = "Fast-path check-in is temporarily unavailable. Please try again.";

	public const string AllowedMessage = "Waiver verified. Fast-path check-in is ready.";
	public const string RefreshRequiredMessage = "Waiver is pending. Refresh status after waiver completion.";
	public const string PendingStillBlockedMessage = "Waiver is still pending. Complete waiver recovery, then refresh.";
	public const string BlockedMessage = "Fast-path is blocked until waiver requirements are completed.";
}

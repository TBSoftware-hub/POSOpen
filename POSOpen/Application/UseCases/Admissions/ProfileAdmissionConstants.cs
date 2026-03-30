namespace POSOpen.Application.UseCases.Admissions;

public static class ProfileAdmissionConstants
{
	public const string ErrorAuthForbidden = "AUTH_FORBIDDEN";
	public const string ErrorProfileRequiredFieldsMissing = "PROFILE_REQUIRED_FIELDS_MISSING";
	public const string ErrorProfileNotFound = "PROFILE_NOT_FOUND";
	public const string ErrorProfileSaveFailed = "PROFILE_SAVE_FAILED";
	public const string ErrorAdmissionRouteUnavailable = "ADMISSION_ROUTE_UNAVAILABLE";

	public const string SafeAuthForbiddenMessage = "You do not have access to this action.";
	public const string SafeRequiredFieldsMissingMessage = "Complete required profile fields to continue admission.";
	public const string SafeProfileNotFoundMessage = "Profile could not be found. Return to lookup and try again.";
	public const string SafeProfileSaveFailedMessage = "Profile changes could not be saved. Please try again.";
	public const string SafeAdmissionRouteUnavailableMessage = "Unable to continue admission right now. Please try again.";
}

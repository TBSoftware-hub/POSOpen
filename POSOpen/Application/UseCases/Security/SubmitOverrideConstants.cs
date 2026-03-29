namespace POSOpen.Application.UseCases.Security;

public static class SubmitOverrideConstants
{
	public const string ErrorAuthForbidden = "AUTH_FORBIDDEN";
	public const string ErrorReasonRequired = "OVERRIDE_REASON_REQUIRED";
	public const string ErrorContextInvalid = "OVERRIDE_CONTEXT_INVALID";
	public const string ErrorCommitFailed = "OVERRIDE_COMMIT_FAILED";

	public const string SafeAuthForbiddenMessage = "You do not have permission to perform this action.";
	public const string SafeReasonRequiredMessage = "A reason is required to proceed with this override action.";
	public const string SafeContextInvalidMessage = "The action context is missing required information.";
	public const string SafeCommitFailedMessage = "The override action could not be completed. Please try again.";

	public const string OverrideSucceededMessage = "Override action completed successfully.";
}

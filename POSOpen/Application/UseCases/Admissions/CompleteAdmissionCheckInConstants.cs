namespace POSOpen.Application.UseCases.Admissions;

public static class CompleteAdmissionCheckInConstants
{
	public const string ErrorAuthForbidden = "AUTH_FORBIDDEN";
	public const string ErrorFastPathBlocked = "ADMISSION_FAST_PATH_BLOCKED";
	public const string ErrorCompletionFailed = "ADMISSION_COMPLETION_FAILED";
	public const string ErrorQueuePersistenceFailed = "ADMISSION_QUEUE_PERSISTENCE_FAILED";
	public const string ErrorAmountRequired = "ADMISSION_AMOUNT_REQUIRED";

	public const string EventAdmissionCompleted = "AdmissionCompleted";
	public const string EventAdmissionPaymentQueued = "AdmissionPaymentQueued";

	public const string SafeAuthForbiddenMessage = "You do not have access to this action.";
	public const string SafeFastPathBlockedMessage = "Admission cannot be completed until waiver and profile checks pass.";
	public const string SafeCompletionFailedMessage = "Admission completion could not be saved. Please try again.";
	public const string SafeQueuePersistenceFailedMessage = "Admission completed, but payment queue persistence failed. Please retry.";
	public const string SafeAmountRequiredMessage = "Admission total is required before check-in can be completed.";

	public const string SuccessAuthorizedMessage = "Paid and completed.";
	public const string SuccessDeferredMessage = "Checked in with payment queued.";
	public const string AuthorizedGuidance = "Guest check-in is complete and payment is settled.";
	public const string DeferredGuidance = "Guest check-in is complete. Payment follow-up is queued for sync.";

	public const string SettlementLabelAuthorized = "Paid and completed";
	public const string SettlementLabelDeferred = "Payment queued";
}

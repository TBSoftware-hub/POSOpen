namespace POSOpen.Application.UseCases.Checkout;

public static class RefundWorkflowConstants
{
	public const string ErrorAuthForbidden = "AUTH_FORBIDDEN";
	public const string ErrorTargetNotFound = "REFUND_TARGET_NOT_FOUND";
	public const string ErrorNotEligible = "REFUND_NOT_ELIGIBLE";
	public const string ErrorAlreadyCompleted = "REFUND_ALREADY_COMPLETED";
	public const string ErrorAmountInvalid = "REFUND_AMOUNT_INVALID";
	public const string ErrorReasonRequired = "REFUND_REASON_REQUIRED";
	public const string ErrorPathForbidden = "REFUND_PATH_FORBIDDEN";
	public const string ErrorApprovalStateInvalid = "REFUND_APPROVAL_STATE_INVALID";
	public const string ErrorCommitFailed = "REFUND_COMMIT_FAILED";

	public const string SafeAuthForbiddenMessage = "You do not have permission to process refunds.";
	public const string SafeTargetNotFoundMessage = "The selected transaction could not be found.";
	public const string SafeNotEligibleMessage = "This transaction is not eligible for refund.";
	public const string SafeAlreadyCompletedMessage = "This transaction has already been fully refunded.";
	public const string SafeAmountInvalidMessage = "Enter a refund amount that is greater than zero and within the refundable balance.";
	public const string SafeReasonRequiredMessage = "A reason is required for approval-based refund requests.";
	public const string SafePathForbiddenMessage = "Your role cannot finalize this refund path.";
	public const string SafeApprovalStateInvalidMessage = "The selected refund request is no longer pending approval.";
	public const string SafeCommitFailedMessage = "Refund processing failed. Please try again.";

	public const string RefundCompletedMessage = "Refund completed successfully.";
	public const string RefundApprovalRequestedMessage = "Refund request submitted for approval.";
	public const string RefundAlreadyProcessedMessage = "This refund operation was already processed.";
	public const string DefaultReasonPlaceholder = "No additional reason provided.";
	public const string RefundInitiationRecordedMessage = "Refund initiation recorded.";
	public const string EligibleRefundAvailableMessage = "Refund is available for this transaction.";
	public const string RefundApprovalDeniedMessage = "Refund request was denied.";
}
namespace POSOpen.Application.UseCases.Security;

public static class ListSecurityAuditTrailConstants
{
	public const string ErrorAuthForbidden = "AUTH_FORBIDDEN";
	public const string ErrorAuditTrailUnavailable = "AUDIT_TRAIL_UNAVAILABLE";

	public const string SafeAuthForbiddenMessage =
		"You do not have permission to view the security audit trail.";

	public const string SafeAuditTrailUnavailableMessage =
		"The security audit trail is temporarily unavailable. Please try again.";
}

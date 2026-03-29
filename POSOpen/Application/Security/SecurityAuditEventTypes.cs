namespace POSOpen.Application.Security;

/// <summary>
/// Canonical event type names for security-critical immutable audit events.
/// These names are append-only and must not be renamed once data exists.
/// </summary>
public static class SecurityAuditEventTypes
{
	public const string StaffAccountCreated = "StaffAccountCreated";
	public const string StaffAccountUpdated = "StaffAccountUpdated";
	public const string StaffAccountDeactivated = "StaffAccountDeactivated";
	public const string StaffRoleAssigned = "StaffRoleAssigned";
	public const string OverrideActionCommitted = "OverrideActionCommitted";
	public const string SecurityAuditAccessDenied = "SecurityAuditAccessDenied";

	/// <summary>All event types that constitute the security-critical audit scope.</summary>
	public static readonly IReadOnlyList<string> SecurityCriticalScope =
	[
		StaffAccountCreated,
		StaffAccountUpdated,
		StaffAccountDeactivated,
		StaffRoleAssigned,
		OverrideActionCommitted,
	];
}

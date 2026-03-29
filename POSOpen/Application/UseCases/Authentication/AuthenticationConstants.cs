namespace POSOpen.Application.UseCases.Authentication;

public static class AuthenticationConstants
{
	public const string SafeSignInFailureMessage = "Sign-in failed. Check credentials or contact a manager.";
	public const string SafeSignInUnavailableMessage = "Sign-in is temporarily unavailable. Please try again.";
	public const string SafeRoleHomeUnavailableMessage = "Sign-in succeeded but workspace could not be loaded. Contact a manager.";

	public const string ErrorInvalidCredentials = "AUTH_INVALID_CREDENTIALS";
	public const string ErrorAccountInactive = "AUTH_ACCOUNT_INACTIVE";
	public const string ErrorAccountLocked = "AUTH_ACCOUNT_LOCKED";
	public const string ErrorSignInUnavailable = "AUTH_SIGNIN_UNAVAILABLE";

	public const int LockoutThreshold = 5;
	public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);
}

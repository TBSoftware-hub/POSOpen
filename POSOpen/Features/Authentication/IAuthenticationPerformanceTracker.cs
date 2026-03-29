namespace POSOpen.Features.Authentication;

public interface IAuthenticationPerformanceTracker
{
	void MarkSignInStarted(DateTime startedUtc);

	SignInPerformanceMeasurement? MarkRoleHomeInteractive(string route, DateTime interactiveUtc);
}

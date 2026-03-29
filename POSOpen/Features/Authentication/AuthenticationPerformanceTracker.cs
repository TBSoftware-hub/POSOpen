namespace POSOpen.Features.Authentication;

public sealed class AuthenticationPerformanceTracker : IAuthenticationPerformanceTracker
{
	private readonly object _lock = new();
	private DateTime? _signInStartedUtc;

	public void MarkSignInStarted(DateTime startedUtc)
	{
		lock (_lock)
		{
			_signInStartedUtc = startedUtc;
		}
	}

	public SignInPerformanceMeasurement? MarkRoleHomeInteractive(string route, DateTime interactiveUtc)
	{
		lock (_lock)
		{
			if (!_signInStartedUtc.HasValue)
			{
				return null;
			}

			var startedUtc = _signInStartedUtc.Value;
			_signInStartedUtc = null;
			var duration = interactiveUtc - startedUtc;
			return new SignInPerformanceMeasurement(
				startedUtc,
				interactiveUtc,
				duration,
				route,
				duration <= TimeSpan.FromSeconds(3));
		}
	}
}

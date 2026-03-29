namespace POSOpen.Features.Authentication;

public sealed record SignInPerformanceMeasurement(
	DateTime StartedUtc,
	DateTime InteractiveUtc,
	TimeSpan Duration,
	string Route,
	bool WithinTarget);

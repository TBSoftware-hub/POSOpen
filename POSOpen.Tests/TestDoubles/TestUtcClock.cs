using POSOpen.Application.Abstractions.Services;

namespace POSOpen.Tests;

public sealed class TestUtcClock : IUtcClock
{
	public TestUtcClock(DateTime utcNow)
	{
		UtcNow = utcNow;
	}

	public DateTime UtcNow { get; }
}
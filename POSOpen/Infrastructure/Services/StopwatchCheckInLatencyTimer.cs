using System.Diagnostics;
using POSOpen.Application.Abstractions.Services;

namespace POSOpen.Infrastructure.Services;

public sealed class StopwatchCheckInLatencyTimer : ICheckInLatencyTimer
{
	public long GetTimestamp()
	{
		return Stopwatch.GetTimestamp();
	}

	public double GetElapsedMilliseconds(long startTimestamp, long endTimestamp)
	{
		return Stopwatch.GetElapsedTime(startTimestamp, endTimestamp).TotalMilliseconds;
	}
}

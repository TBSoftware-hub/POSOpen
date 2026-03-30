namespace POSOpen.Application.Abstractions.Services;

public interface ICheckInLatencyTimer
{
	long GetTimestamp();

	double GetElapsedMilliseconds(long startTimestamp, long endTimestamp);
}

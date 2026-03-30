namespace POSOpen.Application.Abstractions.Services;

public interface ICheckInLatencyMonitor
{
	void Record(string interaction, Guid? familyId, double elapsedMilliseconds, bool thresholdExceeded);
}

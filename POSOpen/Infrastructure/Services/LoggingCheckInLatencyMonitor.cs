using Microsoft.Extensions.Logging;
using POSOpen.Application.Abstractions.Services;

namespace POSOpen.Infrastructure.Services;

public sealed class LoggingCheckInLatencyMonitor : ICheckInLatencyMonitor
{
	private readonly ILogger<LoggingCheckInLatencyMonitor> _logger;

	public LoggingCheckInLatencyMonitor(ILogger<LoggingCheckInLatencyMonitor> logger)
	{
		_logger = logger;
	}

	public void Record(string interaction, Guid? familyId, double elapsedMilliseconds, bool thresholdExceeded)
	{
		if (thresholdExceeded)
		{
			_logger.LogWarning(
				"Check-in interaction {Interaction} exceeded threshold at {ElapsedMilliseconds:0.##}ms for family {FamilyId}.",
				interaction,
				elapsedMilliseconds,
				familyId);
			return;
		}

		_logger.LogDebug(
			"Check-in interaction {Interaction} completed at {ElapsedMilliseconds:0.##}ms for family {FamilyId}.",
			interaction,
			elapsedMilliseconds,
			familyId);
	}
}

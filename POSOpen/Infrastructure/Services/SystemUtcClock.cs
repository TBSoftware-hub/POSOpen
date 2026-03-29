using POSOpen.Application.Abstractions.Services;

namespace POSOpen.Infrastructure.Services;

public sealed class SystemUtcClock : IUtcClock
{
	public DateTime UtcNow => DateTime.UtcNow;
}
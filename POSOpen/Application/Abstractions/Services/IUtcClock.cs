namespace POSOpen.Application.Abstractions.Services;

public interface IUtcClock
{
	DateTime UtcNow { get; }
}
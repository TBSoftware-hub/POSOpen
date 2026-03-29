using POSOpen.Application.Abstractions.Services;
using POSOpen.Shared.Operational;

namespace POSOpen.Infrastructure.Services;

public sealed class OperationContextFactory : IOperationContextFactory
{
	private readonly IUtcClock _clock;

	public OperationContextFactory(IUtcClock clock)
	{
		_clock = clock;
	}

	public OperationContext CreateRoot(Guid? correlationId = null)
	{
		var rootCorrelationId = correlationId ?? Guid.NewGuid();
		return new OperationContext(Guid.NewGuid(), rootCorrelationId, null, _clock.UtcNow);
	}

	public OperationContext CreateChild(Guid correlationId, Guid? causationId = null)
	{
		return new OperationContext(Guid.NewGuid(), correlationId, causationId, _clock.UtcNow);
	}
}
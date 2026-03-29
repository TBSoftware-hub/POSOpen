using POSOpen.Shared.Operational;

namespace POSOpen.Application.Abstractions.Services;

public interface IOperationContextFactory
{
	OperationContext CreateRoot(Guid? correlationId = null);

	OperationContext CreateChild(Guid correlationId, Guid? causationId = null);
}
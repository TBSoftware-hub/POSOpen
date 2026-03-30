using POSOpen.Domain.Entities;

namespace POSOpen.Application.Abstractions.Repositories;

public interface IAdmissionCheckInRepository
{
	Task<AdmissionCheckInRecord> SaveCompletionAsync(
		AdmissionCheckInPersistenceRequest request,
		CancellationToken cancellationToken = default);

	Task<AdmissionCheckInRecord?> GetByOperationIdAsync(
		Guid operationId,
		CancellationToken cancellationToken = default);
}

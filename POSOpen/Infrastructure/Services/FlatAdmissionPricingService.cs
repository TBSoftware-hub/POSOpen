using POSOpen.Application.Abstractions.Services;

namespace POSOpen.Infrastructure.Services;

public sealed class FlatAdmissionPricingService : IAdmissionPricingService
{
	private const long StoryScopedAdmissionTotalCents = 2500;

	public Task<AdmissionTotal> GetAdmissionTotalAsync(Guid familyId, CancellationToken cancellationToken = default)
	{
		return Task.FromResult(new AdmissionTotal(StoryScopedAdmissionTotalCents, "USD"));
	}
}

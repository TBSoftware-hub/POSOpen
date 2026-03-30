namespace POSOpen.Application.Abstractions.Services;

public interface IAdmissionPricingService
{
	Task<AdmissionTotal> GetAdmissionTotalAsync(Guid familyId, CancellationToken cancellationToken = default);
}

public sealed record AdmissionTotal(long AmountCents, string CurrencyCode);

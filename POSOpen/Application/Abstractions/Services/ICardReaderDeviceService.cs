using POSOpen.Application.Results;
using POSOpen.Application.UseCases.Checkout;

namespace POSOpen.Application.Abstractions.Services;

public interface ICardReaderDeviceService
{
	Task<AppResult<CardAuthorizationDto>> AuthorizeAsync(CardAuthorizationRequest request, CancellationToken ct = default);
}
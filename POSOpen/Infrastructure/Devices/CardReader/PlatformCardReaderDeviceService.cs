using POSOpen.Application.Abstractions.Services;
using POSOpen.Application.Results;
using POSOpen.Application.UseCases.Checkout;
using POSOpen.Domain.Enums;

namespace POSOpen.Infrastructure.Devices.CardReader;

public sealed class PlatformCardReaderDeviceService : ICardReaderDeviceService
{
	public Task<AppResult<CardAuthorizationDto>> AuthorizeAsync(CardAuthorizationRequest request, CancellationToken ct = default)
	{
		return Task.FromResult(AppResult<CardAuthorizationDto>.Success(
			new CardAuthorizationDto(
				CheckoutPaymentAuthorizationStatus.Unavailable,
				null,
				DeviceDiagnosticCode.CardReaderUnavailable),
			CartCheckoutConstants.SafeCardReaderUnavailableMessage));
	}
}
using POSOpen.Application.Abstractions.Services;
using POSOpen.Features.Checkout;

namespace POSOpen.Infrastructure.Services;

public sealed class CheckoutUiService : ICheckoutUiService
{
	public Task NavigateToAddLineItemAsync(Guid cartId)
	{
		return global::Microsoft.Maui.Controls.Shell.Current.GoToAsync(
			$"{CheckoutRoutes.AddLineItem}?cartId={cartId}");
	}
}
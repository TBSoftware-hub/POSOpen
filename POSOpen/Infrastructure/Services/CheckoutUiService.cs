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

	public Task NavigateToPaymentCaptureAsync(Guid cartId)
	{
		return global::Microsoft.Maui.Controls.Shell.Current.GoToAsync(
			$"{CheckoutRoutes.PaymentCapture}?cartId={cartId}");
	}

	public Task ClosePaymentCaptureAsync()
	{
		return global::Microsoft.Maui.Controls.Shell.Current.GoToAsync("..");
	}

	public Task NavigateToCheckoutCompletionAsync(Guid cartSessionId)
	{
		return global::Microsoft.Maui.Controls.Shell.Current.GoToAsync(
			$"{CheckoutRoutes.CheckoutCompletion}?cartSessionId={cartSessionId}");
	}

	public Task NavigateToRefundWorkflowAsync(Guid cartSessionId)
	{
		return global::Microsoft.Maui.Controls.Shell.Current.GoToAsync(
			$"{CheckoutRoutes.RefundWorkflow}?cartSessionId={cartSessionId}");
	}

	public Task StartNewTransactionAsync()
	{
		return global::Microsoft.Maui.Controls.Shell.Current.GoToAsync(
			$"///{CheckoutRoutes.Cart}");
	}
}
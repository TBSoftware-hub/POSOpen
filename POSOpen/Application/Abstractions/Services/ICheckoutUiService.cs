namespace POSOpen.Application.Abstractions.Services;

public interface ICheckoutUiService
{
	Task NavigateToAddLineItemAsync(Guid cartId);
	Task NavigateToPaymentCaptureAsync(Guid cartId);
	Task ClosePaymentCaptureAsync();
	Task NavigateToCheckoutCompletionAsync(Guid cartSessionId);
	Task NavigateToRefundWorkflowAsync(Guid cartSessionId);
	Task StartNewTransactionAsync();
}
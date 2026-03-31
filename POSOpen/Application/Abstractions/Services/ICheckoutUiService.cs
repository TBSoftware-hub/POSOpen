namespace POSOpen.Application.Abstractions.Services;

public interface ICheckoutUiService
{
	Task NavigateToAddLineItemAsync(Guid cartId);
}
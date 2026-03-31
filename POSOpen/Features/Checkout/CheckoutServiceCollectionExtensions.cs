using Microsoft.Extensions.DependencyInjection;
using POSOpen.Application.UseCases.Checkout;
using POSOpen.Domain.Policies;
using POSOpen.Features.Checkout.ViewModels;
using POSOpen.Features.Checkout.Views;

namespace POSOpen.Features.Checkout;

public static class CheckoutServiceCollectionExtensions
{
	public static IServiceCollection AddCheckoutFeature(this IServiceCollection services)
	{
		services.AddTransient<GetOrCreateCartSessionUseCase>();
		services.AddTransient<AddCartLineItemUseCase>();
		services.AddTransient<RemoveCartLineItemUseCase>();
		services.AddTransient<UpdateCartLineItemQuantityUseCase>();
		// Compatibility rules — each concrete type registers as ICartCompatibilityRule
		services.AddTransient<ICartCompatibilityRule, CartMustHaveItemsRule>();
		services.AddTransient<ICartCompatibilityRule, CateringRequiresPartyDepositRule>();
		services.AddTransient<ICartCompatibilityRule, SinglePartyDepositRule>();
		services.AddTransient<ValidateCartCompatibilityUseCase>();
		services.AddTransient<CartViewModel>();
		services.AddTransient<AddLineItemViewModel>();
		services.AddTransient<CartPage>();
		services.AddTransient<AddLineItemPage>();

		Routing.RegisterRoute(CheckoutRoutes.Cart, typeof(CartPage));
		Routing.RegisterRoute(CheckoutRoutes.AddLineItem, typeof(AddLineItemPage));
		return services;
	}
}

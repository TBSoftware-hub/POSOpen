using Microsoft.Extensions.DependencyInjection;
using POSOpen.Application.UseCases.Checkout;
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
		services.AddTransient<CartViewModel>();
		services.AddTransient<AddLineItemViewModel>();
		services.AddTransient<CartPage>();
		services.AddTransient<AddLineItemPage>();

		Routing.RegisterRoute(CheckoutRoutes.Cart, typeof(CartPage));
		Routing.RegisterRoute(CheckoutRoutes.AddLineItem, typeof(AddLineItemPage));
		return services;
	}
}

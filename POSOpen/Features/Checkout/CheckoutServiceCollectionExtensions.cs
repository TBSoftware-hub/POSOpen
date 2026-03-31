using Microsoft.Extensions.DependencyInjection;
using POSOpen.Application.Abstractions.Services;
using POSOpen.Application.UseCases.Checkout;
using POSOpen.Domain.Policies;
using POSOpen.Features.Checkout.ViewModels;
using POSOpen.Features.Checkout.Views;
using POSOpen.Infrastructure.Devices.CardReader;
using POSOpen.Infrastructure.Devices.Scanner;
using POSOpen.Infrastructure.Devices.Printer;
using POSOpen.Infrastructure.Services;

namespace POSOpen.Features.Checkout;

public static class CheckoutServiceCollectionExtensions
{
	public static IServiceCollection AddCheckoutFeature(this IServiceCollection services)
	{
		services.AddTransient<GetOrCreateCartSessionUseCase>();
		services.AddTransient<AddCartLineItemUseCase>();
		services.AddTransient<RemoveCartLineItemUseCase>();
		services.AddTransient<UpdateCartLineItemQuantityUseCase>();
		services.AddTransient<GetCartPaymentSummaryUseCase>();
		services.AddTransient<CaptureScannerInputUseCase>();
		services.AddTransient<ProcessCardPaymentUseCase>();
		services.AddTransient<PrintReceiptUseCase>();
		services.AddTransient<GetTransactionStatusUseCase>();
		// Compatibility rules — each concrete type registers as ICartCompatibilityRule
		services.AddTransient<ICartCompatibilityRule, CartMustHaveItemsRule>();
		services.AddTransient<ICartCompatibilityRule, CateringRequiresPartyDepositRule>();
		services.AddTransient<ICartCompatibilityRule, SinglePartyDepositRule>();
		services.AddTransient<IScannerDeviceService, PlatformScannerDeviceService>();
		services.AddTransient<ICardReaderDeviceService, PlatformCardReaderDeviceService>();
		services.AddTransient<IPrinterDeviceService, PlatformPrinterDeviceService>();
		services.AddTransient<IOperationIdService, OperationIdService>();
		services.AddTransient<ValidateCartCompatibilityUseCase>();
		services.AddTransient<CartViewModel>();
		services.AddTransient<AddLineItemViewModel>();
		services.AddTransient<PaymentCaptureViewModel>();
		services.AddTransient<CheckoutCompletionViewModel>();
		services.AddTransient<CartPage>();
		services.AddTransient<AddLineItemPage>();
		services.AddTransient<PaymentCapturePage>();
		services.AddTransient<CheckoutCompletionPage>();

		Routing.RegisterRoute(CheckoutRoutes.Cart, typeof(CartPage));
		Routing.RegisterRoute(CheckoutRoutes.AddLineItem, typeof(AddLineItemPage));
		Routing.RegisterRoute(CheckoutRoutes.PaymentCapture, typeof(PaymentCapturePage));
		Routing.RegisterRoute(CheckoutRoutes.CheckoutCompletion, typeof(CheckoutCompletionPage));
		return services;
	}
}

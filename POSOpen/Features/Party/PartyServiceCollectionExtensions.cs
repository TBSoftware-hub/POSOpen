using Microsoft.Extensions.DependencyInjection;
using POSOpen.Application.UseCases.Party;
using POSOpen.Features.Party.ViewModels;
using POSOpen.Features.Party.Views;

namespace POSOpen.Features.Party;

public static class PartyServiceCollectionExtensions
{
	public static IServiceCollection AddPartyFeature(this IServiceCollection services)
	{
		services.AddTransient<GetBookingAvailabilityUseCase>();
		services.AddTransient<CreateDraftPartyBookingUseCase>();
		services.AddTransient<ConfirmPartyBookingUseCase>();
		services.AddTransient<PartyBookingWizardViewModel>();
		services.AddTransient<PartyBookingWizardPage>();

		Routing.RegisterRoute(PartyRoutes.PartyBookingWizard, typeof(PartyBookingWizardPage));
		return services;
	}
}

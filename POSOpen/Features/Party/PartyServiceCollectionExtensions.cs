using Microsoft.Extensions.DependencyInjection;
using POSOpen.Application.UseCases.Inventory;
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
		services.AddTransient<RecordPartyDepositCommitmentUseCase>();
		services.AddTransient<GetPartyBookingTimelineUseCase>();
		services.AddTransient<MarkPartyBookingCompletedUseCase>();
		services.AddTransient<GetRoomOptionsUseCase>();
		services.AddTransient<AssignPartyRoomUseCase>();
		services.AddTransient<GetBookingAddOnOptionsUseCase>();
		services.AddTransient<UpdateBookingAddOnSelectionsUseCase>();
		services.AddTransient<ReserveBookingInventoryUseCase>();
		services.AddTransient<ReleaseBookingInventoryUseCase>();
		services.AddTransient<GetAllowedSubstitutesUseCase>();
		services.AddTransient<PartyBookingWizardViewModel>();
		services.AddTransient<PartyBookingDetailViewModel>();
		services.AddTransient<PartyBookingWizardPage>();
		services.AddTransient<PartyBookingDetailPage>();

		Routing.RegisterRoute(PartyRoutes.PartyBookingWizard, typeof(PartyBookingWizardPage));
		Routing.RegisterRoute(PartyRoutes.PartyBookingDetail, typeof(PartyBookingDetailPage));
		return services;
	}
}

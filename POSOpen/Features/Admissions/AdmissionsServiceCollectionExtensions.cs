using Microsoft.Extensions.DependencyInjection;
using POSOpen.Application.UseCases.Admissions;
using POSOpen.Features.Admissions.ViewModels;
using POSOpen.Features.Admissions.Views;

namespace POSOpen.Features.Admissions;

public static class AdmissionsServiceCollectionExtensions
{
	public static IServiceCollection AddAdmissionsFeature(this IServiceCollection services)
	{
		services.AddTransient<SearchFamiliesUseCase>();
		services.AddTransient<FamilyLookupViewModel>();
		services.AddTransient<FamilyLookupPage>();
		services.AddTransient<FamilyProfilePage>();

		Routing.RegisterRoute(AdmissionsRoutes.FamilyLookup, typeof(FamilyLookupPage));
		Routing.RegisterRoute(AdmissionsRoutes.FamilyProfile, typeof(FamilyProfilePage));
		return services;
	}
}

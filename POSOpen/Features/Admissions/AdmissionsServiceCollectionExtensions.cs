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
		services.AddTransient<EvaluateFastPathCheckInUseCase>();
		services.AddTransient<ProfileAdmissionUseCase>();
		services.AddTransient<CompleteAdmissionCheckInUseCase>();
		services.AddTransient<FamilyLookupViewModel>();
		services.AddTransient<FastPathCheckInViewModel>();
		services.AddTransient<NewProfileAdmissionViewModel>();
		services.AddTransient<FamilyLookupPage>();
		services.AddTransient<FamilyProfilePage>();
		services.AddTransient<FastPathCheckInPage>();
		services.AddTransient<NewProfileAdmissionPage>();

		Routing.RegisterRoute(AdmissionsRoutes.FamilyLookup, typeof(FamilyLookupPage));
		Routing.RegisterRoute(AdmissionsRoutes.FamilyProfile, typeof(FamilyProfilePage));
		Routing.RegisterRoute(AdmissionsRoutes.FastPathCheckIn, typeof(FastPathCheckInPage));
		Routing.RegisterRoute(AdmissionsRoutes.NewProfile, typeof(NewProfileAdmissionPage));
		return services;
	}
}

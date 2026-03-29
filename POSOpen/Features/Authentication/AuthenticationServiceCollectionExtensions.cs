using Microsoft.Extensions.DependencyInjection;
using POSOpen.Application.UseCases.Authentication;
using POSOpen.Features.Authentication.ViewModels;
using POSOpen.Features.Authentication.Views;

namespace POSOpen.Features.Authentication;

public static class AuthenticationServiceCollectionExtensions
{
	public static IServiceCollection AddAuthenticationFeature(this IServiceCollection services)
	{
		services.AddTransient<AuthenticateStaffUseCase>();
		services.AddSingleton<IAuthenticationPerformanceTracker, AuthenticationPerformanceTracker>();
		services.AddTransient<SignInViewModel>();
		services.AddTransient<SignInPage>();

		Routing.RegisterRoute(AuthenticationRoutes.SignIn, typeof(SignInPage));
		return services;
	}
}

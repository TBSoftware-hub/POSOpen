using Microsoft.Extensions.DependencyInjection;
using POSOpen.Application.UseCases.Security;
using POSOpen.Features.Security.ViewModels;
using POSOpen.Features.Security.Views;

namespace POSOpen.Features.Security;

public static class SecurityServiceCollectionExtensions
{
	public static IServiceCollection AddSecurityFeature(this IServiceCollection services)
	{
		services.AddTransient<SubmitOverrideUseCase>();
		services.AddTransient<OverrideApprovalViewModel>();
		services.AddTransient<OverrideApprovalPage>();

		services.AddTransient<ListSecurityAuditTrailUseCase>();
		services.AddTransient<SecurityAuditViewModel>();
		services.AddTransient<SecurityAuditPage>();

		Routing.RegisterRoute(SecurityRoutes.OverrideApproval, typeof(OverrideApprovalPage));
		Routing.RegisterRoute(SecurityRoutes.SecurityAuditTrail, typeof(SecurityAuditPage));
		return services;
	}
}

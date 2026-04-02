using Microsoft.Extensions.DependencyInjection;
using POSOpen.Application.UseCases.Inventory;
using POSOpen.Features.Inventory.ViewModels;
using POSOpen.Features.Inventory.Views;

namespace POSOpen.Features.Inventory;

public static class InventoryServiceCollectionExtensions
{
	public static IServiceCollection AddInventoryFeature(this IServiceCollection services)
	{
		services.AddTransient<GetInventorySubstitutionPoliciesUseCase>();
		services.AddTransient<CreateInventorySubstitutionPolicyUseCase>();
		services.AddTransient<UpdateInventorySubstitutionPolicyUseCase>();
		services.AddTransient<DeleteInventorySubstitutionPolicyUseCase>();
		services.AddTransient<InventorySubstitutionPoliciesViewModel>();
		services.AddTransient<InventorySubstitutionPoliciesPage>();

		Routing.RegisterRoute(InventoryRoutes.SubstitutionPolicies, typeof(InventorySubstitutionPoliciesPage));
		return services;
	}
}

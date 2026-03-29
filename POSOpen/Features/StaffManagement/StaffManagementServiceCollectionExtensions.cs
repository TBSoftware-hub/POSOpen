using Microsoft.Extensions.DependencyInjection;
using POSOpen.Application.UseCases.StaffManagement;
using POSOpen.Features.StaffManagement.ViewModels;
using POSOpen.Features.StaffManagement.Views;

namespace POSOpen.Features.StaffManagement;

public static class StaffManagementServiceCollectionExtensions
{
	public static IServiceCollection AddStaffManagement(this IServiceCollection services)
	{
		services.AddTransient<CreateStaffAccountUseCase>();
		services.AddTransient<UpdateStaffAccountUseCase>();
		services.AddTransient<DeactivateStaffAccountUseCase>();
		services.AddTransient<ListActiveStaffAccountsUseCase>();
		services.AddTransient<GetStaffAccountByIdUseCase>();

		services.AddTransient<StaffListViewModel>();
		services.AddTransient<CreateStaffAccountViewModel>();
		services.AddTransient<EditStaffAccountViewModel>();

		services.AddTransient<StaffListPage>();
		services.AddTransient<CreateStaffAccountPage>();
		services.AddTransient<EditStaffAccountPage>();

		Routing.RegisterRoute(StaffManagementRoutes.CreateStaff, typeof(CreateStaffAccountPage));
		Routing.RegisterRoute(StaffManagementRoutes.EditStaff, typeof(EditStaffAccountPage));

		return services;
	}
}

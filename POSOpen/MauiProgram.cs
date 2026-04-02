using Microsoft.Extensions.Logging;
using POSOpen.Application.Abstractions.Services;
using POSOpen.Features.Admissions;
using POSOpen.Features.Checkout;
using POSOpen.Features.Inventory;
using POSOpen.Features.Party;
using POSOpen.Features.Authentication;
using POSOpen.Features.Security;
using POSOpen.Features.StaffManagement;
using POSOpen.Features.Shell;
using POSOpen.Features.Shell.ViewModels;
using POSOpen.Features.Shell.Views;
using POSOpen.Infrastructure.Persistence;
using POSOpen.Infrastructure.Services;

namespace POSOpen;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		builder.Services.AddPosOpenPersistence();
		builder.Services.AddAuthenticationFeature();
		builder.Services.AddAdmissionsFeature();
		builder.Services.AddCheckoutFeature();
		builder.Services.AddInventoryFeature();
		builder.Services.AddPartyFeature();
		builder.Services.AddSecurityFeature();
		builder.Services.AddStaffManagement();
		builder.Services.AddSingleton<IAppStateService, AppStateService>();
		builder.Services.AddSingleton<ICheckInLatencyTimer, StopwatchCheckInLatencyTimer>();
		builder.Services.AddSingleton<ICheckInLatencyMonitor, LoggingCheckInLatencyMonitor>();
		builder.Services.AddTransient<IAdmissionSettlementService, DefaultAdmissionSettlementService>();
		builder.Services.AddTransient<IAdmissionPricingService, FlatAdmissionPricingService>();
		builder.Services.AddTransient<ICheckoutUiService, CheckoutUiService>();
		builder.Services.AddTransient<IFastPathCheckInUiService, FastPathCheckInUiService>();
		builder.Services.AddTransient<IProfileAdmissionUiService, ProfileAdmissionUiService>();
		builder.Services.AddSingleton<AppShell>();
		builder.Services.AddSingleton<HomePage>();
		builder.Services.AddSingleton<ManagerOperationsPage>();
		builder.Services.AddSingleton<HomeViewModel>();
		builder.Services.AddSingleton<ManagerOperationsViewModel>();

		Routing.RegisterRoute(ShellRoutes.ManagerOperations, typeof(ManagerOperationsPage));

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}

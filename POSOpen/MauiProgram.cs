using Microsoft.Extensions.Logging;
using POSOpen.Application.Abstractions.Services;
using POSOpen.Features.StaffManagement;
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
		builder.Services.AddStaffManagement();
		builder.Services.AddSingleton<IAppStateService, AppStateService>();
		builder.Services.AddSingleton<AppShell>();
		builder.Services.AddSingleton<HomePage>();
		builder.Services.AddSingleton<HomeViewModel>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}

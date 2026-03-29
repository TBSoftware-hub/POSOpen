namespace POSOpen;

public partial class App : global::Microsoft.Maui.Controls.Application
{
	private readonly Application.Abstractions.Persistence.IAppDbContextInitializer _dbContextInitializer;
	private readonly AppShell _appShell;

	public App(
		Application.Abstractions.Persistence.IAppDbContextInitializer dbContextInitializer,
		AppShell appShell)
	{
		InitializeComponent();
		_dbContextInitializer = dbContextInitializer;
		_dbContextInitializer.InitializeAsync().GetAwaiter().GetResult();
		_appShell = appShell;
	}

	protected override global::Microsoft.Maui.Controls.Window CreateWindow(IActivationState? activationState)
	{
		return new global::Microsoft.Maui.Controls.Window(_appShell);
	}
}
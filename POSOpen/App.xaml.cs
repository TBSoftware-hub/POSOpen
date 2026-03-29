namespace POSOpen;

public partial class App : global::Microsoft.Maui.Controls.Application
{
	private readonly Application.Abstractions.Persistence.IAppDbContextInitializer _dbContextInitializer;
	private readonly AppShell _appShell;
	private Task? _initializationTask;

	public App(
		Application.Abstractions.Persistence.IAppDbContextInitializer dbContextInitializer,
		AppShell appShell)
	{
		InitializeComponent();
		_dbContextInitializer = dbContextInitializer;
		_appShell = appShell;
	}

	protected override global::Microsoft.Maui.Controls.Window CreateWindow(IActivationState? activationState)
	{
		_initializationTask ??= InitializePersistenceAsync();
		return new global::Microsoft.Maui.Controls.Window(_appShell);
	}

	private async Task InitializePersistenceAsync()
	{
		await _dbContextInitializer.InitializeAsync().ConfigureAwait(false);
	}
}
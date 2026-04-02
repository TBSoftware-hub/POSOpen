using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.Abstractions.Services;

namespace POSOpen.Infrastructure.Services;

public sealed class ConnectivityMonitorService
{
	private readonly IConnectivityService _connectivityService;
	private readonly IAppStateService _appStateService;
	private readonly IOutboxRepository _outboxRepository;
	private int _initialized;

	public ConnectivityMonitorService(
		IConnectivityService connectivityService,
		IAppStateService appStateService,
		IOutboxRepository outboxRepository)
	{
		_connectivityService = connectivityService;
		_appStateService = appStateService;
		_outboxRepository = outboxRepository;
	}

	public void Initialize()
	{
		if (Interlocked.Exchange(ref _initialized, 1) == 1)
		{
			return;
		}

		ApplyConnectivityState(_connectivityService.IsConnected);
		_connectivityService.ConnectivityChanged += HandleConnectivityChanged;
		_ = RefreshPendingSyncStateAsync();
	}

	private void HandleConnectivityChanged(object? sender, bool isConnected)
	{
		ApplyConnectivityState(isConnected);
		_ = RefreshPendingSyncStateAsync();
	}

	private void ApplyConnectivityState(bool isConnected)
	{
		_appStateService.SetOfflineMode(!isConnected);
		_appStateService.SetSyncState(isConnected ? "Online - Reconnected" : "Offline Mode Active");
	}

	private async Task RefreshPendingSyncStateAsync()
	{
		try
		{
			var pending = await _outboxRepository.ListPendingAsync();
			if (pending.Count == 0 || _connectivityService.IsConnected)
			{
				return;
			}

			_appStateService.SetSyncState($"Offline - {pending.Count} actions pending sync");
		}
		catch
		{
			// Keep offline monitoring resilient even if queue inspection fails.
		}
	}
}

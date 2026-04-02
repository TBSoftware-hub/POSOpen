using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Networking;
using POSOpen.Application.Abstractions.Services;

namespace POSOpen.Infrastructure.Services;

public sealed class MauiConnectivityService : IConnectivityService
{
	private readonly IConnectivity _connectivity;

	public MauiConnectivityService(IConnectivity connectivity)
	{
		_connectivity = connectivity;
		_connectivity.ConnectivityChanged += HandleConnectivityChanged;
	}

	public bool IsConnected => _connectivity.NetworkAccess == NetworkAccess.Internet;

	public event EventHandler<bool>? ConnectivityChanged;

	private void HandleConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
	{
		var isConnected = e.NetworkAccess == NetworkAccess.Internet;
		if (MainThread.IsMainThread)
		{
			ConnectivityChanged?.Invoke(this, isConnected);
			return;
		}

		MainThread.BeginInvokeOnMainThread(() => ConnectivityChanged?.Invoke(this, isConnected));
	}
}

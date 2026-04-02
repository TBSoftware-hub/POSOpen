namespace POSOpen.Application.Abstractions.Services;

public interface IConnectivityService
{
	bool IsConnected { get; }

	event EventHandler<bool>? ConnectivityChanged;
}

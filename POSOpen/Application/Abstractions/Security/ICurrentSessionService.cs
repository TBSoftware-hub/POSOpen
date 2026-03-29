namespace POSOpen.Application.Abstractions.Security;

using POSOpen.Application.Security;

public interface ICurrentSessionService
{
	CurrentSession? GetCurrent();

	void RefreshPermissionSnapshot();

	long IncrementSessionVersion();
}

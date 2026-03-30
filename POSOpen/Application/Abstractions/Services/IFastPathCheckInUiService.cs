namespace POSOpen.Application.Abstractions.Services;

public interface IFastPathCheckInUiService
{
	Task NavigateToWaiverRecoveryAsync(Guid familyId);

	Task ShowFastPathReadyAsync();
}

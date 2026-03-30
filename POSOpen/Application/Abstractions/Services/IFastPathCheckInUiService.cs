namespace POSOpen.Application.Abstractions.Services;

public interface IFastPathCheckInUiService
{
	Task NavigateToWaiverRecoveryAsync(Guid familyId);

	Task NavigateToProfileCompletionAsync(Guid familyId);

	Task ShowFastPathReadyAsync();
}

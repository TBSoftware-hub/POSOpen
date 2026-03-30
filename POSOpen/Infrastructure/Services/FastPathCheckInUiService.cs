using POSOpen.Application.Abstractions.Services;
using POSOpen.Features.Admissions;

namespace POSOpen.Infrastructure.Services;

public sealed class FastPathCheckInUiService : IFastPathCheckInUiService
{
	public Task NavigateToWaiverRecoveryAsync(Guid familyId)
	{
		return global::Microsoft.Maui.Controls.Shell.Current.GoToAsync(
			$"{AdmissionsRoutes.WaiverRecovery}?familyId={familyId}");
	}

	public Task ShowFastPathReadyAsync()
	{
		return global::Microsoft.Maui.Controls.Shell.Current.DisplayAlertAsync(
			"Check-In",
			"Fast-path check-in is ready. Admission completion will be finalized in a later story.",
			"OK");
	}
}

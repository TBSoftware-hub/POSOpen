using POSOpen.Application.Abstractions.Services;
using POSOpen.Features.Admissions;

namespace POSOpen.Infrastructure.Services;

public sealed class ProfileAdmissionUiService : IProfileAdmissionUiService
{
	public Task NavigateToFastPathCheckInAsync(Guid familyId)
	{
		return global::Microsoft.Maui.Controls.Shell.Current.GoToAsync(
			$"{AdmissionsRoutes.FastPathCheckIn}?familyId={familyId}");
	}
}

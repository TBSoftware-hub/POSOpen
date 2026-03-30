namespace POSOpen.Application.Abstractions.Services;

public interface IProfileAdmissionUiService
{
	Task NavigateToFastPathCheckInAsync(Guid familyId);
}

using POSOpen.Shared.Operational;

namespace POSOpen.Application.UseCases.Authentication;

public sealed record AuthenticateStaffCommand(
	string Email,
	string Password,
	OperationContext Context);

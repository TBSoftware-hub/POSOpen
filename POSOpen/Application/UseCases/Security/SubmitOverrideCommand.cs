using POSOpen.Shared.Operational;

namespace POSOpen.Application.UseCases.Security;

public sealed record SubmitOverrideCommand(
	string ActionKey,
	string TargetReference,
	string Reason,
	OperationContext Context);

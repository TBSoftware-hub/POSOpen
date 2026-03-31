using POSOpen.Domain.Enums;

namespace POSOpen.Application.UseCases.Checkout;

/// <summary>Result returned by GetTransactionStatusUseCase.</summary>
public sealed record TransactionStatusDto(
	Guid CartSessionId,
	TransactionStatus TransactionStatus,
	Guid? LastOperationId,
	string StatusMessage,
	string NextStepsMessage);

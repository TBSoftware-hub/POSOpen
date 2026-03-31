using POSOpen.Domain.Enums;

namespace POSOpen.Application.UseCases.Checkout;

/// <summary>Result returned by PrintReceiptUseCase. Printer failure does not block transaction completion.</summary>
public sealed record PrintReceiptResultDto(
	Guid OperationId,
	PrintStatus PrintStatus,
	bool TransactionCompleted,
	string? DiagnosticCode,
	string UserMessage);

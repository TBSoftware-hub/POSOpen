using POSOpen.Domain.Enums;

namespace POSOpen.Domain.Entities;

public sealed class CheckoutPaymentAttempt
{
	public Guid Id { get; init; }
	public Guid CartSessionId { get; init; }
	public long AmountCents { get; init; }
	public string CurrencyCode { get; init; } = "USD";
	public CheckoutPaymentAuthorizationStatus AuthorizationStatus { get; init; }
	public string? ProcessorReference { get; init; }
	public string? DiagnosticCode { get; init; }
	public DateTime OccurredAtUtc { get; init; }

	public static CheckoutPaymentAttempt Create(
		Guid id,
		Guid cartSessionId,
		long amountCents,
		string currencyCode,
		CheckoutPaymentAuthorizationStatus authorizationStatus,
		string? processorReference,
		string? diagnosticCode,
		DateTime occurredAtUtc) =>
		new()
		{
			Id = id,
			CartSessionId = cartSessionId,
			AmountCents = amountCents,
			CurrencyCode = currencyCode,
			AuthorizationStatus = authorizationStatus,
			ProcessorReference = processorReference,
			DiagnosticCode = diagnosticCode,
			OccurredAtUtc = occurredAtUtc,
		};
}
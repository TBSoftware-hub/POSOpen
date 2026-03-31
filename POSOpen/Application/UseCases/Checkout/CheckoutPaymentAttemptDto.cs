using POSOpen.Domain.Entities;
using POSOpen.Domain.Enums;

namespace POSOpen.Application.UseCases.Checkout;

public sealed record CheckoutPaymentAttemptDto(
	Guid Id,
	Guid CartSessionId,
	long AmountCents,
	string CurrencyCode,
	CheckoutPaymentAuthorizationStatus AuthorizationStatus,
	string? ProcessorReference,
	string? DiagnosticCode,
	DateTime OccurredAtUtc)
{
	public static CheckoutPaymentAttemptDto FromEntity(CheckoutPaymentAttempt attempt) =>
		new(
			attempt.Id,
			attempt.CartSessionId,
			attempt.AmountCents,
			attempt.CurrencyCode,
			attempt.AuthorizationStatus,
			attempt.ProcessorReference,
			attempt.DiagnosticCode,
			attempt.OccurredAtUtc);
}
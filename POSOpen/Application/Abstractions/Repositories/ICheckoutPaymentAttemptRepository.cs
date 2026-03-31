using POSOpen.Domain.Entities;

namespace POSOpen.Application.Abstractions.Repositories;

public interface ICheckoutPaymentAttemptRepository
{
	Task<CheckoutPaymentAttempt> AddAsync(CheckoutPaymentAttempt attempt, CancellationToken ct = default);

	Task<IReadOnlyList<CheckoutPaymentAttempt>> ListByCartSessionAsync(Guid cartSessionId, CancellationToken ct = default);
}
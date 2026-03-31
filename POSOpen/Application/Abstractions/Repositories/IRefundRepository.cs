using POSOpen.Domain.Entities;

namespace POSOpen.Application.Abstractions.Repositories;

public interface IRefundRepository
{
	Task<RefundRecord> AddAsync(RefundRecord record, CancellationToken ct = default);

	/// <summary>
	/// Atomically checks the remaining refundable balance and adds a refund record within a transaction.
	/// Ensures concurrent submissions do not exceed the approved payment total.
	/// </summary>
	/// <param name="record">The refund record to insert</param>
	/// <param name="approvedTotalAmountCents">The total approved amount available for refund calculations (in cents)</param>
	/// <param name="ct">Cancellation token</param>
	/// <returns>The added record, or throws if balance would be exceeded</returns>
	Task<RefundRecord> AddAsyncWithBalanceCheckAsync(RefundRecord record, long approvedTotalAmountCents, CancellationToken ct = default);

	Task<RefundRecord?> GetByIdAsync(Guid id, CancellationToken ct = default);

	Task<RefundRecord?> GetByOperationIdAsync(Guid operationId, CancellationToken ct = default);

	Task<IReadOnlyList<RefundRecord>> ListByCartSessionAsync(Guid cartSessionId, CancellationToken ct = default);

	Task<long> SumCompletedAmountByCartSessionAsync(Guid cartSessionId, CancellationToken ct = default);

	Task<RefundRecord> UpdateAsync(RefundRecord record, CancellationToken ct = default);
}
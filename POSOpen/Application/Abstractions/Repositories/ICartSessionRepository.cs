using POSOpen.Domain.Entities;

namespace POSOpen.Application.Abstractions.Repositories;

public interface ICartSessionRepository
{
    Task<CartSession?> GetByIdAsync(Guid cartSessionId, CancellationToken cancellationToken = default);
    Task<CartSession?> GetOpenCartForStaffAsync(Guid staffId, CancellationToken cancellationToken = default);
    Task<CartSession> CreateAsync(CartSession cart, CancellationToken cancellationToken = default);
    Task<CartSession?> AddLineItemAsync(Guid cartId, CartLineItem lineItem, DateTime updatedAtUtc, CancellationToken cancellationToken = default);
    Task<CartSession?> RemoveLineItemAsync(Guid cartId, Guid lineItemId, DateTime updatedAtUtc, CancellationToken cancellationToken = default);
    Task<CartSession?> UpdateLineItemQuantityAsync(Guid cartId, Guid lineItemId, int newQuantity, DateTime updatedAtUtc, CancellationToken cancellationToken = default);
}

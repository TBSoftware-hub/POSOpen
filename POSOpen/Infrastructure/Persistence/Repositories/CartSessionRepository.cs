using Microsoft.EntityFrameworkCore;
using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Domain.Entities;
using POSOpen.Domain.Enums;

namespace POSOpen.Infrastructure.Persistence.Repositories;

public sealed class CartSessionRepository : ICartSessionRepository
{
    private readonly IDbContextFactory<PosOpenDbContext> _dbContextFactory;

    public CartSessionRepository(IDbContextFactory<PosOpenDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<CartSession?> GetByIdAsync(Guid cartSessionId, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.CartSessions
            .Include(s => s.LineItems)
            .FirstOrDefaultAsync(s => s.Id == cartSessionId, cancellationToken);
    }

    public async Task<CartSession?> GetOpenCartForStaffAsync(Guid staffId, CancellationToken cancellationToken = default)
    {
            await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            return await db.CartSessions
            .Include(s => s.LineItems)
            .FirstOrDefaultAsync(s => s.StaffId == staffId && s.Status == CartStatus.Open, cancellationToken);
    }

        public async Task<CartSession> CreateAsync(CartSession cart, CancellationToken cancellationToken = default)
    {
            await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            db.CartSessions.Add(cart);
            await db.SaveChangesAsync(cancellationToken);
            return cart;
    }

        public async Task<CartSession?> AddLineItemAsync(Guid cartId, CartLineItem lineItem, DateTime updatedAtUtc, CancellationToken cancellationToken = default)
    {
            await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var cart = await db.CartSessions
                .Include(s => s.LineItems)
                .FirstOrDefaultAsync(s => s.Id == cartId, cancellationToken);
            if (cart is null) return null;

            cart.LineItems.Add(lineItem);
            cart.UpdatedAtUtc = updatedAtUtc;
            await db.SaveChangesAsync(cancellationToken);
            return cart;
        }

        public async Task<CartSession?> RemoveLineItemAsync(Guid cartId, Guid lineItemId, DateTime updatedAtUtc, CancellationToken cancellationToken = default)
        {
            await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var cart = await db.CartSessions
                .Include(s => s.LineItems)
                .FirstOrDefaultAsync(s => s.Id == cartId, cancellationToken);
            if (cart is null) return null;

            var item = cart.LineItems.FirstOrDefault(i => i.Id == lineItemId);
            if (item is not null)
            {
                cart.LineItems.Remove(item);
                cart.UpdatedAtUtc = updatedAtUtc;
                await db.SaveChangesAsync(cancellationToken);
            }
            return cart;
        }

        public async Task<CartSession?> UpdateLineItemQuantityAsync(Guid cartId, Guid lineItemId, int newQuantity, DateTime updatedAtUtc, CancellationToken cancellationToken = default)
        {
            await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var cart = await db.CartSessions
                .Include(s => s.LineItems)
                .FirstOrDefaultAsync(s => s.Id == cartId, cancellationToken);
            if (cart is null) return null;

            var item = cart.LineItems.FirstOrDefault(i => i.Id == lineItemId);
            if (item is not null)
            {
                item.Quantity = newQuantity;
                item.UpdatedAtUtc = updatedAtUtc;
                cart.UpdatedAtUtc = updatedAtUtc;
                await db.SaveChangesAsync(cancellationToken);
            }
            return cart;
    }
}

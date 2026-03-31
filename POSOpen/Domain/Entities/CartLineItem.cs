using POSOpen.Domain.Enums;

namespace POSOpen.Domain.Entities;

public sealed class CartLineItem
{
    public Guid Id { get; init; }
    public Guid CartSessionId { get; init; }
    public string Description { get; set; } = string.Empty;
    public FulfillmentContext FulfillmentContext { get; init; }
    public Guid? ReferenceId { get; init; }
    public int Quantity { get; set; }
    public long UnitAmountCents { get; init; }
    public string CurrencyCode { get; init; } = "USD";
    public DateTime CreatedAtUtc { get; init; }
    public DateTime UpdatedAtUtc { get; set; }

    // Computed — not persisted. EF ignores via configuration.
    public long LineTotalCents => (long)Quantity * UnitAmountCents;

    public static CartLineItem Create(
        Guid id,
        Guid cartSessionId,
        string description,
        FulfillmentContext fulfillmentContext,
        Guid? referenceId,
        int quantity,
        long unitAmountCents,
        string currencyCode,
        DateTime clockUtc) =>
        new()
        {
            Id = id,
            CartSessionId = cartSessionId,
            Description = description,
            FulfillmentContext = fulfillmentContext,
            ReferenceId = referenceId,
            Quantity = quantity,
            UnitAmountCents = unitAmountCents,
            CurrencyCode = currencyCode,
            CreatedAtUtc = clockUtc,
            UpdatedAtUtc = clockUtc,
        };
}

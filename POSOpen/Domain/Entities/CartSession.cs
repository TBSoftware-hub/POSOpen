using POSOpen.Domain.Enums;

namespace POSOpen.Domain.Entities;

public sealed class CartSession
{
    public Guid Id { get; init; }
    public Guid? FamilyId { get; set; }
    public Guid StaffId { get; init; }
    public CartStatus Status { get; set; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime UpdatedAtUtc { get; set; }
    public ICollection<CartLineItem> LineItems { get; init; } = new List<CartLineItem>();

    // Computed — not persisted. EF ignores via configuration.
    public long TotalAmountCents => LineItems.Sum(i => i.LineTotalCents);

    public static CartSession Create(
        Guid id,
        Guid? familyId,
        Guid staffId,
        DateTime clockUtc) =>
        new()
        {
            Id = id,
            FamilyId = familyId,
            StaffId = staffId,
            Status = CartStatus.Open,
            CreatedAtUtc = clockUtc,
            UpdatedAtUtc = clockUtc,
        };
}

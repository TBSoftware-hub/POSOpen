using POSOpen.Domain.Entities;

namespace POSOpen.Application.Abstractions.Repositories;

public interface IPartyBookingRepository
{
	Task<PartyBooking?> GetByIdAsync(Guid bookingId, CancellationToken ct = default);

	Task<PartyBooking?> GetByOperationIdAsync(Guid operationId, CancellationToken ct = default);

	Task<IReadOnlyList<PartyBooking>> ListByPartyDateAsync(DateTime partyDateUtc, CancellationToken ct = default);

	Task<bool> IsSlotUnavailableAsync(DateTime partyDateUtc, string slotId, Guid? excludingBookingId = null, CancellationToken ct = default);

	Task<PartyBooking> UpsertDraftAsync(PartyBooking booking, CancellationToken ct = default);

	Task<PartyBooking> ConfirmAsync(PartyBooking booking, Guid operationId, Guid correlationId, DateTime bookedAtUtc, CancellationToken ct = default);
}

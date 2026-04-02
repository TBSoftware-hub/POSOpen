# Story 4.5: Reserve and Release Inventory by Booking Policy

## Metadata

| Field | Value |
|---|---|
| Epic | 4 - Party Booking Lifecycle and Inventory Coordination |
| Story | 4.5 |
| Key | `4-5-reserve-and-release-inventory-by-booking-policy` |
| Status | done |
| Author | Timbe (via BMAD Story Creator) |
| Created | 2026-04-01 |
| Target Sprint | Current |

---

## User Story

**As a** manager/party coordinator,  
**I want** inventory to be reserved at booking commitment and released by policy,  
**So that** party fulfillment remains reliable and stock integrity is preserved.

---

## Acceptance Criteria

### AC-1 - Reserve required stock for inventory-linked bookings

> **Given** a booking is committed with inventory-linked items  
> **When** reservation is executed  
> **Then** required stock is reserved against the booking.

### AC-2 - Release reserved inventory on eligible booking changes/cancellations

> **Given** booking is changed/cancelled under release rules  
> **When** policy conditions are met  
> **Then** reserved inventory is released correctly.

V1 release policy matrix for AC-2 (deterministic):

- Trigger `booking-cancelled` -> release 100% of active reservations for the booking.
- Trigger `booking-item-removed` -> release reservation rows tied to removed option IDs only.
- Trigger `booking-item-quantity-reduced` -> release only delta quantity (`old - new`) for that option.
- Trigger `booking-date-or-slot-changed` -> release all active reservations, then re-run reserve flow for current booking composition in the same command pipeline.
- Trigger `booking-updated-non-inventory-fields` -> no release.
- When multiple triggers apply in one update, apply in order: remove -> quantity-reduced -> date/slot-changed -> reserve recalculation.

### AC-3 - Block booking finalization when inventory constraints remain unresolved

> **Given** inventory constraints are violated  
> **When** finalization is attempted  
> **Then** booking cannot finalize without approved resolution  
> **And** user receives actionable guidance.

### AC-4 - Show policy-allowed substitutes by role when constraints are encountered

> **Given** substitution policies exist  
> **When** constrained inventory is encountered  
> **Then** allowed substitutes are shown based on policy and role permission.

---

## Scope

### In Scope

- Introduce booking inventory reservation records tied to booking lifecycle events.
- Reserve inventory on booking commit and maintain reservation integrity during update/cancel paths.
- Apply release policies for cancellation and inventory-impacting booking changes.
- Enforce finalization guardrails when unresolved inventory constraints exist.
- Expose actionable substitute options filtered by role permissions and active policy rules.
- Keep all writes idempotent and traceable via operation and correlation IDs.
- Add unit and integration tests for reservation, release, policy filtering, and finalization-block scenarios.

### Out of Scope

- Manager CRUD UI for substitution policy definitions (Story 4.6).
- Supplier integration or live external inventory feeds.
- Multi-location inventory pools (Epic 6+ concern).
- Distributed locking across multiple active terminals (future architecture concern).
- Automated pricing optimization for substitute choices.

---

## Context

Stories 4.1-4.4 established booking lifecycle, timeline recalculation, room assignment conflict handling, and add-on risk surfacing. Story 4.5 must convert inventory risk signals into enforceable inventory reservation behavior and release policy execution while preserving the same architecture guardrails:

- Feature-first structure and strict layer boundaries.
- Canonical AppResult envelope and safe/error message patterns.
- Idempotent write handling with operation IDs.
- Transactional persistence with atomic read-check-write behavior.
- Timeline consistency after inventory-impacting updates.

This story is the policy execution layer for party inventory reliability and should not be implemented as a detached subsystem.

---

## Current Repo Reality

### Party/add-on baseline already implemented

- `POSOpen/Application/UseCases/Party/GetBookingAddOnOptionsUseCase.cs`
- `POSOpen/Application/UseCases/Party/UpdateBookingAddOnSelectionsUseCase.cs`
- `POSOpen/Application/UseCases/Party/AddOnSelectionDtos.cs`
- `POSOpen/Application/UseCases/Party/PartyBookingConstants.cs`
- `POSOpen/Infrastructure/Persistence/Repositories/PartyBookingRepository.cs`

### Existing guardrails to preserve

- `PartyBookingConstants` already contains:
  - static catalog IDs (`KnownCateringOptionIds`, `KnownDecorOptionIds`)
  - risk metadata (`KnownAtRiskOptionIds`, `AtRiskOptionMeta`)
  - existing booking/room/add-on error code conventions
- `PartyBookingRepository` already follows transaction patterns used in Stories 4.1-4.4.
- Party UI already has booking detail context and inline risk display from Story 4.4.

### Architectural destination for this scope

Architecture maps FR25-FR29 to `Features/Inventory`. Implementation can span Party and Inventory slices, but inventory policy and reservation logic should be centralized in Inventory-focused application services/use cases instead of being duplicated in multiple Party view models.

---

## Previous Story Intelligence (4.4 + recent commits)

### Must preserve from Story 4.4

- Add-on selection and risk context is now part of booking detail; inventory reservation should consume this context, not recreate it.
- Idempotency checks are implemented at booking-level write operations (for example `LastAddOnUpdateOperationId`) and must be mirrored for reservation/release operations.
- Timeline refresh is triggered after booking-impacting saves; inventory-impacting actions must keep this behavior aligned with NFR4.

### Recent git signals

- Latest completed feature branch and merge activity is Story 4.4, with follow-up review fixes in Story 4.3 before that.
- This indicates active convention continuity in Party use cases, repository transaction shape, and story-driven incremental schema updates.

---

## Architecture Compliance Guardrails

- Preserve strict boundary: Presentation -> Application -> Infrastructure (no Presentation -> Infrastructure direct calls).
- Keep inventory decision logic in Application layer policies/use cases, not in XAML or code-behind.
- Use canonical `AppResult<T>` envelope for all new commands/queries.
- Keep UTC timestamps and deterministic ordering for reservation/release events.
- Keep write operations atomic with explicit transaction lifecycle and rollback on failure.
- Persist operation IDs and correlation IDs for replay safety and audit traceability.
- Ensure booking finalization blocks are policy-driven and explicit, not inferred from ad hoc UI checks.

---

## Domain and Data Model Guidance

### Inventory reservation entity model (minimum)

Add a reservation record linked to booking and option identity:

- `ReservationId` (Guid)
- `BookingId` (Guid)
- `OptionId` (string)
- `QuantityReserved` (int)
- `ReservationState` (Reserved, Released)
- `ReservedAtUtc` (DateTime)
- `ReleasedAtUtc` (DateTime?)
- `ReservationOperationId` (Guid)
- `ReleaseOperationId` (Guid?)
- `ReleaseReasonCode` (string?)

### Policy support model (read-side in 4.5)

Story 4.5 consumes substitution policies; Story 4.6 authors/manages them. Implement read-side support now so 4.6 can extend it:

- Policy contract fields used in 4.5 execution:
  - `SourceOptionId`
  - `AllowedSubstituteOptionId`
  - `AllowedRoles`
  - `IsActive`

If no persisted policy store exists yet, use a scoped V1 policy provider abstraction in Application that can be swapped to repository-backed data in Story 4.6.

V1 policy source contract for AC-4:

- 4.5 MUST ship with a deterministic, in-app seeded policy provider implementing `IInventorySubstitutionPolicyProvider`.
- Seeded provider data is the source of truth in 4.5 environments until 4.6 policy persistence is available.
- If the provider has no active match for a constrained option and role, return an empty substitute list (never fail the request).
- Provider output ordering MUST be stable by `SourceOptionId`, then `AllowedSubstituteOptionId`.
- 4.6 may replace the provider internals with repository-backed rules without changing 4.5 command/query contracts.

### Booking aggregate updates (minimum)

- Add tracking fields for reservation lifecycle idempotency:
  - `LastInventoryReserveOperationId` (Guid?)
  - `LastInventoryReleaseOperationId` (Guid?)
- Keep booking as the lifecycle anchor and avoid detached inventory mutation flows.

---

## Use Case Contracts

### Reserve booking inventory

- Command: `ReserveBookingInventoryCommand(BookingId, OperationContext)`
- Result payload should include:
  - reservation summary by option
  - unresolved constraint list
  - next-action guidance

Expected behavior:
- Resolve required inventory-linked items from booking/add-on state.
- Reserve available stock atomically.
- Return constraint details when full reserve cannot be satisfied.
- Idempotent success when same operation ID is replayed.

### Release booking inventory

- Command: `ReleaseBookingInventoryCommand(BookingId, ReleaseTrigger, OperationContext)`
- `ReleaseTrigger` supports at minimum: booking-cancelled, booking-updated-policy-release.

Expected behavior:
- Apply release rules by trigger and booking state.
- Release applicable reservation rows atomically.
- Preserve immutable record of release reason and operation IDs.
- Idempotent success on replay.

### Validate finalization constraints

- Query/command hook for finalization path should enforce:
  - block finalization when unresolved inventory constraints exist
  - return user-safe guidance and substitute options (if policy allows)

Mandatory enforcement point:

- Enforce this gate in `POSOpen/Application/UseCases/Party/MarkPartyBookingCompletedUseCase.cs` before calling repository completion methods.
- UI checks in `PartyBookingDetailViewModel` are advisory only; application-layer gate is authoritative.
- Add/extend canonical constants in `PartyBookingConstants` for inventory-blocked finalization error code and safe message.

### Get substitutes for constrained items

- Query: `GetAllowedSubstitutesQuery(BookingId, Role, ConstrainedOptionIds)`

Expected behavior:
- Return only active policy matches.
- Role-filter result set.
- Deterministic ordering for predictable UI and test assertions.

---

## File Structure Requirements

### Expected application-layer additions

- `POSOpen/Application/UseCases/Inventory/ReserveBookingInventoryUseCase.cs`
- `POSOpen/Application/UseCases/Inventory/ReleaseBookingInventoryUseCase.cs`
- `POSOpen/Application/UseCases/Inventory/GetAllowedSubstitutesUseCase.cs`
- `POSOpen/Application/UseCases/Inventory/InventoryReservationDtos.cs`
- `POSOpen/Application/UseCases/Inventory/InventoryPolicyDtos.cs`

### Expected abstraction/repository additions

- `POSOpen/Application/Abstractions/Repositories/IInventoryReservationRepository.cs`
- Optional for policy read in 4.5:
  - `POSOpen/Application/Abstractions/Services/IInventorySubstitutionPolicyProvider.cs`

### Expected infrastructure additions

- `POSOpen/Infrastructure/Persistence/Repositories/InventoryReservationRepository.cs`
- `POSOpen/Infrastructure/Persistence/Configurations/InventoryReservationConfiguration.cs`
- Migration for reservation table and booking operation-id columns

### Expected Party integration touchpoints

- `POSOpen/Application/UseCases/Party/UpdateBookingAddOnSelectionsUseCase.cs` (hook/resync behavior if reservation should be reevaluated after add-on changes)
- `POSOpen/Features/Party/ViewModels/PartyBookingDetailViewModel.cs` (display reserve/release state and guidance)
- `POSOpen/Features/Party/Views/PartyBookingDetailPage.xaml` (constraint guidance and substitute action area)
- `POSOpen/Features/Party/PartyServiceCollectionExtensions.cs` (DI registration)

---

## UX and Interaction Requirements

- Keep inline, actionable guidance when finalization is blocked (do not force deep modal detours).
- Preserve status-at-a-glance pattern from UX spec (booking state, risk state, constraint state).
- Show substitute recommendations with clear reason and role-eligibility context.
- Keep data-entry continuity: do not clear user selections when a constraint error occurs.
- Maintain consistency with timeline and next-best-action patterns already present in Party flows.

---

## Testing Requirements

### Unit tests

- Reserve success path with full availability.
- Reserve partial/unavailable path returns constraint guidance.
- Reserve idempotent replay returns success without duplicate rows.
- Release policy path for cancel/update triggers.
- Finalization validation blocks unresolved constraints.
- Substitute query returns role-filtered active policy results.

### Integration tests

- Repository transaction atomicity for reserve/release operations.
- No duplicate reservations for replayed operation IDs.
- Booking state and reservation records remain consistent after rollback scenarios.
- Timeline/booking detail response remains within expected shape after inventory transitions.

### Regression focus

- Story 4.4 add-on/risk logic remains intact.
- Story 4.3 room assignment behavior remains unaffected.
- Existing Party detail view commands continue to execute without null-state regressions.

---

## Implementation Tasks / Subtasks

### Task 1 - Introduce reservation persistence and idempotency fields (AC: 1, 2)

- [ ] Add inventory reservation entity and EF configuration.
- [ ] Add migration for reservation table and booking reservation operation fields.
- [ ] Add indices for booking lookups and operation-id idempotency checks.

### Task 2 - Implement reserve and release use cases (AC: 1, 2)

- [ ] Implement reserve command/use case with atomic reservation writes.
- [ ] Implement release command/use case with policy-trigger handling.
- [ ] Enforce idempotency in reserve and release operations.
- [ ] Implement all V1 release policy matrix triggers exactly as specified in AC-2 notes.

### Task 3 - Enforce booking finalization constraints (AC: 3)

- [ ] Integrate constraint validation into finalization path.
- [ ] Integrate constraint validation in `MarkPartyBookingCompletedUseCase` before repository completion call.
- [ ] Return canonical error code and user-safe guidance when blocked.
- [ ] Ensure no finalization side effects occur when blocked.

### Task 4 - Add substitute policy read and role filtering (AC: 4)

- [ ] Add policy provider abstraction and initial implementation.
- [ ] Add deterministic seeded provider implementation for 4.5 (`IInventorySubstitutionPolicyProvider`) with stable ordering.
- [ ] Return only role-allowed, active substitutes for constrained options.
- [ ] Expose deterministic substitute ordering for UX consistency.

### Task 5 - UI integration in Party booking detail flow (AC: 3, 4)

- [ ] Surface reservation/constraint state in booking detail.
- [ ] Show actionable substitute options inline.
- [ ] Preserve existing 4.4 risk indicators and timeline behaviors.

### Task 6 - Add unit/integration tests and update DI wiring (AC: 1, 2, 3, 4)

- [ ] Add Inventory use case unit tests.
- [ ] Add repository integration tests.
- [ ] Register new Inventory services/use cases in DI.
- [ ] Verify all test suites pass with no regression in Party scenarios.

---

## Definition of Done

- All ACs implemented and validated by unit/integration tests.
- Reservation and release flows are idempotent and transaction-safe.
- Finalization block logic enforces unresolved inventory constraints.
- Substitute suggestions are policy-based and role-filtered.
- No regressions in Stories 4.3 and 4.4 paths.
- Story status advanced according to workflow after implementation and review.

---

## References

- `_bmad-output/planning-artifacts/epics.md` (Epic 4, Story 4.5)
- `_bmad-output/planning-artifacts/architecture.md` (feature mapping, layer boundaries, idempotent replay, outbox/audit patterns)
- `_bmad-output/planning-artifacts/ux-design-specification.md` (status-at-a-glance, inline recovery, role-aware views)
- `_bmad-output/implementation-artifacts/4-4-manage-catering-decor-options-with-risk-indicators.md` (previous story intelligence and established Party patterns)

# Story 4.3 - Manage Room Assignment and Conflict Resolution

## Metadata

| Field | Value |
|---|---|
| Epic | 4 - Party Booking Lifecycle and Inventory Coordination |
| Story | 4.3 |
| Key | `4-3-manage-room-assignment-and-conflict-resolution` |
| Status | in-progress |
| Status | review |
| Author | Timbe (via BMAD Story Creator) |
| Created | 2026-04-01 |
| Target Sprint | Current |

---

## User Story

**As a** party coordinator,  
**I want** to assign rooms and resolve schedule conflicts,  
**So that** party operations are feasible and double-booking is prevented.

---

## Acceptance Criteria

### AC-1 - Select only compatible available rooms for the booking time

> **Given** a booking has a target date/time  
> **When** room assignment is requested  
> **Then** only compatible available rooms are selectable.

### AC-2 - Block room/time conflicts and offer alternatives

> **Given** a room/time conflict exists  
> **When** coordinator attempts assignment  
> **Then** conflict is blocked  
> **And** alternative slots/rooms are suggested.

### AC-3 - Recalculate timeline/task state after assignment changes

> **Given** room assignment changes after booking updates  
> **When** save is confirmed  
> **Then** impacted timeline tasks and booking status are recalculated.

---

## Scope

### In Scope

- Add room-assignment model and use-case flow for existing party bookings.
- Validate room compatibility and availability at assignment time.
- Prevent assignment when room/time conflict exists.
- Suggest alternative valid room/slot options when blocked.
- Recompute booking timeline impact after room assignment changes.
- Provide coordinator-visible conflict and suggestion messaging.
- Add unit and integration coverage for room assignment rules and conflict handling.

### Out of Scope

- Catering/decor risk modeling and editing (Story 4.4).
- Inventory reservation/release execution (Story 4.5).
- Substitution policy maintenance UX (Story 4.6).
- Multi-terminal real-time locking and distributed conflict coordination (architecture defers this to V2).

---

## Context

Story 4.1 established booking creation and confirmation. Story 4.2 added deposit commitment and timeline projection/retrieval. Story 4.3 must extend those same booking and timeline assets by introducing room assignment with conflict prevention and actionable alternatives.

This story should reuse the existing Party feature slice and canonical booking contracts. Do not build a parallel room-booking model disconnected from `PartyBooking`.

---

## Current Repo Reality

- Existing booking aggregate and persistence are already in place:
  - `POSOpen/Domain/Entities/PartyBooking.cs`
  - `POSOpen/Application/Abstractions/Repositories/IPartyBookingRepository.cs`
  - `POSOpen/Infrastructure/Persistence/Repositories/PartyBookingRepository.cs`
- Existing booking availability flow and known slot model exist:
  - `POSOpen/Application/UseCases/Party/GetBookingAvailabilityUseCase.cs`
  - `POSOpen/Application/UseCases/Party/PartyBookingConstants.cs` (`KnownSlotIds`)
- **Room catalog (V1):** Add a `KnownRoomIds` static string array to `PartyBookingConstants`, analogous to `KnownSlotIds`. Do NOT create a `Room` entity, table, or separate room repository in this story. V1 rooms are a static catalog.
- Timeline and booking-detail experience exists from Story 4.2:
  - `POSOpen/Application/UseCases/Party/GetPartyBookingTimelineUseCase.cs`
  - `POSOpen/Features/Party/ViewModels/PartyBookingDetailViewModel.cs`
  - `POSOpen/Features/Party/Views/PartyBookingDetailPage.xaml`

---

## Previous Story Intelligence

From Story 4.2 implementation and review history:

- Keep strict layering: ViewModels call Application use cases only; no direct Infrastructure calls from UI.
- Preserve canonical `AppResult<T>` envelope and canonical safe/error messaging patterns.
- Preserve operation and correlation IDs on write paths.
- Keep explicit ViewModel processing states (`Idle -> Loading -> Success|Error|Deferred`).
- Keep UTC-safe date/time behavior and deterministic rule outcomes.
- Extend existing Party feature and tests, avoid parallel modules.
- Existing timeline performance NFR4 guard tests are present; avoid query regressions that degrade timeline retrieval/update.

---

## Architecture Compliance Guardrails

- Feature-first structure remains authoritative: `Features/Party`, `Application/UseCases/Party`, `Infrastructure/Persistence`.
- Use canonical result envelope fields:
  - `isSuccess`, `errorCode`, `userMessage`, `diagnosticMessage`, `payload`
- Keep write operations traceable with `operationId` and `correlationId`.
- Persist UTC timestamps and ISO-8601 serialization semantics.
- Do not swallow infrastructure exceptions; map to canonical app errors.
- Keep retry/orchestration policy at infrastructure boundary only.
- Do not introduce Presentation-to-Infrastructure coupling.

---

## Room Assignment and Conflict Contract

### Domain/Persistence Additions

For room assignment on `PartyBooking`, capture at minimum:

- `AssignedRoomId` (string identifier) or nullable equivalent when unassigned.
- `AssignedRoomDisplayName` (optional denormalized display value if needed for UI summary).
- `RoomAssignedAtUtc` (UTC timestamp).
- `RoomAssignmentOperationId` (idempotency and replay safety for room writes).

Guardrails:

- Keep room assignment as part of existing booking aggregate or tightly scoped child in Party feature.
- Do not create an isolated room schedule subsystem in this story.
- Keep assignment writes idempotent for repeated operation IDs.
- **Suggested migration class name:** `20260402110000_AddPartyBookingRoomAssignment` (maintains chronological order after existing `20260402100000_AddPartyBookingDepositsAndTimeline`).

### Compatibility Rule Contract (AC-1)

At assignment request time, a room is selectable only when all are true:

- Room is in `PartyBookingConstants.KnownRoomIds` (the V1 static catalog).
- Room is available for booking date + slot (excluding current booking when editing).

**V1 compatibility strategy:** All rooms in `KnownRoomIds` are compatible with all packages. Package/capacity-based filtering is a V2 concern. Do not build a package-to-room mapping table in this story.

Return to UI:

- Candidate rooms with `isSelectable` and `reason` for non-selectable entries when needed.
- Deterministic sorting (by room ID / display order matching `KnownRoomIds` array order) to ensure consistent UX.

### Conflict Resolution Contract (AC-2)

If assignment collides with existing booking occupancy for selected date/slot:

- Block save.
- Return canonical conflict error code: `BOOKING_ROOM_CONFLICT` (add to `PartyBookingConstants`).
- Include alternative recommendations:
  - other compatible rooms for current slot
  - other compatible slots for selected room
  - combined room+slot alternatives, bounded to same operational day

Alternative ordering should be deterministic and include user-safe reason labels.

**CRITICAL — Atomicity:** The room conflict check and the assignment write MUST occur within the same EF Core transaction (`BeginTransactionAsync` / `CommitAsync` / `RollbackAsync`). Follow the exact pattern in `UpsertDraftAsync` and `RecordDepositCommitmentAsync` in `PartyBookingRepository.cs`. A two-step check-then-write outside a transaction is a race condition that permits double-booking.

### Timeline Recalculation Contract (AC-3)

When room assignment changes are committed:

- Recalculate impacted timeline tasks and status projection inputs.
- Preserve Story 4.2 timeline model and milestone semantics.
- Update booking status only if business rules require (for example keep `Booked` while enriching timeline tasks).
- Return refreshed timeline/task summary in response payload, or trigger immediate follow-up retrieval path via existing timeline use case.

---

## UX and Interaction Requirements

- Keep status-at-a-glance before detail; avoid deep modal chains for conflict handling.
- Use inline recovery patterns: show conflict reason and actionable alternatives without leaving booking context.
- Preserve tablet-first density and sticky primary action patterns.
- Keep consistency with Party Timeline Rail behaviors (state visibility and next-action prompts).
- Ensure conflict and recovery messaging is explicit, human-readable, and action-oriented.

---

## Implementation Tasks / Subtasks

### Task 1 - Extend booking model and persistence for room assignment (AC: 1, 2, 3)

- [x] Add room-assignment fields to `PartyBooking` (`AssignedRoomId`, `RoomAssignedAtUtc`, `RoomAssignmentOperationId`; snake_case in DB).
- [x] Add `KnownRoomIds` static string array to `PartyBookingConstants` (V1 catalog — replaces any need for a `Room` entity).
- [x] Update EF configuration + migration `20260402110000_AddPartyBookingRoomAssignment` for new fields.
- [x] Add composite index `idx_party_bookings_room_date_status` on `(assigned_room_id, party_date_utc, status)` to support conflict queries and NFR4 guard tests.
- [x] Keep naming and schema aligned with existing `party_bookings` snake_case conventions.

### Task 2 - Extend repository abstraction and implementation for room operations (AC: 1, 2)

- [x] Extend `IPartyBookingRepository` with: `IsRoomUnavailableAsync(date, roomId, excludingBookingId)`, `AssignRoomAsync(booking, roomId, operationId, correlationId, assignedAtUtc)`, `ListAlternativeRoomsAsync(date, slotId, excludingRoomId)`, `ListAlternativeSlotsAsync(date, roomId, excludingSlotId)`.
- [x] Implement `AssignRoomAsync` with atomic conflict check + write inside a single `BeginTransactionAsync`/`CommitAsync` block — same pattern as `UpsertDraftAsync`. Idempotency: short-circuit if `RoomAssignmentOperationId == operationId`.
- [x] Implement alternative-suggestion methods using `KnownRoomIds` / `KnownSlotIds` catalogs for deterministic ordering.
- [x] Preserve `try/catch/RollbackAsync` pattern on all transactional methods.

### Task 3 - Add room assignment use cases and DTO contracts (AC: 1, 2, 3)

- [x] Add `GetRoomOptionsUseCase` — returns `RoomOptionsDto` listing all rooms with `isSelectable` and `reason`.
- [x] Add `AssignPartyRoomUseCase` + `AssignPartyRoomCommand` — performs conflict-safe assignment; on conflict returns alternatives payload.
- [x] Add DTO types: `RoomOptionsDto`, `RoomOptionItemDto(roomId, displayName, isSelectable, reason)`, `RoomAssignmentResultDto(booking, conflictAlternatives)`.
- [x] Add to `PartyBookingConstants`:
  - `ErrorRoomConflict = "BOOKING_ROOM_CONFLICT"`
  - `SafeRoomConflictMessage = "That room is already booked. Choose an alternative below."`
  - `RoomAssignedMessage = "Room assignment saved."`
  - `SafeRoomAssignmentFailedMessage = "Room assignment failed. Please try again."`
  - `KnownRoomIds` static array (e.g., `["room-a", "room-b", "room-c"]`)
- [x] Return canonical `AppResult<T>` with user-safe and diagnostic separation.
- [x] Propagate `operationId` and `correlationId` through assignment write path.

### Task 4 - Wire Party booking-detail UI for room assignment and conflict resolution (AC: 1, 2, 3)

- [x] Extend `PartyBookingDetailViewModel` with `LoadRoomOptionsCommand`, `AssignRoomCommand`, room/conflict state properties.
- [x] Add UI affordances on `PartyBookingDetailPage` for room selection, conflict messages, and suggestion actions.
- [x] Preserve explicit processing state transitions (`Idle → Loading → Success|Error`) and no hidden side effects.
- [x] Refresh timeline/task presentation after successful assignment update.
- [x] **Do NOT move `[QueryProperty]` attributes from `PartyBookingDetailPage` to the ViewModel.** Shell navigation passes `bookingId` via `IQueryAttributable` on the *page* — changing this breaks the test project.

### Task 5 - Add test coverage and regression guardrails (AC: 1, 2, 3)

- [x] Unit tests for `GetRoomOptionsUseCase`: selectable/non-selectable room filtering, deterministic ordering.
- [x] Unit tests for `AssignPartyRoomUseCase`: conflict error mapping, idempotency short-circuit, alternative suggestion payload.
- [x] Integration tests for `IsRoomUnavailableAsync`: exclude-self scenario (editing own booking must not self-conflict).
- [x] Integration tests for `AssignRoomAsync`: conflict under concurrent inserts, commit/rollback behavior.
- [x] Integration test for assignment + timeline recalculation: room conflict query NFR4 ≤3s P95.
- [x] **Test setup bulk insert:** Used `dbContext.Set<PartyBooking>().AddRange(bookings); await dbContext.SaveChangesAsync()`.
- [x] **MAUI test project constraint:** No `Microsoft.Maui.*` using directives in test files.
- [x] Confirm new room/conflict queries satisfy NFR4 (≤3s P95) under active-day profile.
- [x] Regression: all 256 tests pass (4.1/4.2 flows still pass).

---

## Definition of Done

- [x] Coordinator can assign only compatible available rooms for booking date/slot.
- [x] Room/time conflicts are blocked with actionable alternatives.
- [x] Timeline/task impacts are recalculated and visible after assignment change.
- [x] Story 4.1 and 4.2 behavior remains regression-safe.
- [x] New tests cover compatibility, conflicts, suggestions, and timeline impact behavior.

---

## Test Scenarios

| Scenario | AC | Expected Result |
|:--|:--|:--|
| Assign room to booked party with compatible room and free slot | AC1 | Assignment succeeds; room fields persisted; timeline impact recalculated |
| Attempt to assign room already occupied for same date/slot | AC2 | Save blocked with `BOOKING_ROOM_CONFLICT`; alternatives returned |
| Edit existing booking room assignment while excluding same booking from conflict check | AC1, AC2 | Own booking does not self-conflict; valid reassignment allowed |
| Room assignment update after booking detail changes | AC3 | Timeline task projection recalculates and UI refreshes without navigation reset |
| No compatible rooms for selected slot | AC2 | Clear blocked state with slot-based alternatives and actionable guidance |

---

## References

- [Source: _bmad-output/planning-artifacts/epics.md#Story-4.3-Manage-Room-Assignment-and-Conflict-Resolution]
- [Source: _bmad-output/planning-artifacts/epics.md#Epic-4-Party-Booking-Lifecycle-and-Inventory-Coordination]
- [Source: _bmad-output/planning-artifacts/prd.md#Party-Booking-and-Event-Lifecycle]
- [Source: _bmad-output/planning-artifacts/prd.md#Non-Functional-Requirements]
- [Source: _bmad-output/planning-artifacts/architecture.md#Implementation-Patterns--Consistency-Rules]
- [Source: _bmad-output/planning-artifacts/architecture.md#Project-Structure--Boundaries]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Party-Timeline-Rail]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Step-12-UX-Consistency-Patterns]
- [Source: _bmad-output/implementation-artifacts/4-2-capture-deposits-and-build-party-timeline.md]
- [Source: POSOpen/Domain/Entities/PartyBooking.cs]
- [Source: POSOpen/Application/Abstractions/Repositories/IPartyBookingRepository.cs]
- [Source: POSOpen/Application/UseCases/Party/GetBookingAvailabilityUseCase.cs]
- [Source: POSOpen/Application/UseCases/Party/PartyBookingConstants.cs]
- [Source: POSOpen/Infrastructure/Persistence/Repositories/PartyBookingRepository.cs]

## Dev Agent Record

### Agent Model Used

Claude Sonnet 4.6 (GitHub Copilot)

### Debug Log References


### Completion Notes List

- Implemented all 5 tasks: domain model extension, repository methods (atomic + idempotent), GetRoomOptionsUseCase, AssignPartyRoomUseCase with conflict payload, ViewModel room commands, XAML room selection section, migration + snapshot update.
- `RoomAssignmentResultDto` carries nullable `AssignedRoomId` and optional `AlternativeRooms`/`AlternativeSlots` to enable conflict payload via primary constructor.
- `AssignPartyRoomCommand` follows `RecordPartyDepositCommitmentCommand` pattern with `OperationContext`.
- 256 tests pass; build clean with 0 errors.

### File List

- POSOpen/Domain/Entities/PartyBooking.cs
- POSOpen/Application/UseCases/Party/PartyBookingConstants.cs
- POSOpen/Application/UseCases/Party/RoomAssignmentDtos.cs
- POSOpen/Application/UseCases/Party/GetRoomOptionsQuery.cs
- POSOpen/Application/UseCases/Party/GetRoomOptionsUseCase.cs
- POSOpen/Application/UseCases/Party/AssignPartyRoomCommand.cs
- POSOpen/Application/UseCases/Party/AssignPartyRoomUseCase.cs
- POSOpen/Application/Abstractions/Repositories/IPartyBookingRepository.cs
- POSOpen/Infrastructure/Persistence/Repositories/PartyBookingRepository.cs
- POSOpen/Infrastructure/Persistence/Configurations/PartyBookingConfiguration.cs
- POSOpen/Infrastructure/Persistence/Migrations/20260402110000_AddPartyBookingRoomAssignment.cs
- POSOpen/Infrastructure/Persistence/Migrations/PosOpenDbContextModelSnapshot.cs
- POSOpen/Features/Party/ViewModels/PartyBookingDetailViewModel.cs
- POSOpen/Features/Party/Views/PartyBookingDetailPage.xaml
- POSOpen/Features/Party/PartyServiceCollectionExtensions.cs
- POSOpen.Tests/Unit/Party/GetRoomOptionsUseCaseTests.cs
- POSOpen.Tests/Unit/Party/AssignPartyRoomUseCaseTests.cs
- POSOpen.Tests/Integration/Party/PartyRoomRepositoryTests.cs
- _bmad-output/implementation-artifacts/4-3-manage-room-assignment-and-conflict-resolution.md

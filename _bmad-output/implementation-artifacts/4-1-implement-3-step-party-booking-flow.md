# Story 4.1 - Implement 3-Step Party Booking Flow

## Metadata

| Field | Value |
|---|---|
| Epic | 4 - Party Booking Lifecycle and Inventory Coordination |
| Story | 4.1 |
| Key | `4-1-implement-3-step-party-booking-flow` |
| Status | review |
| Author | Timbe (via BMAD Story Creator) |
| Created | 2026-03-31 |
| Target Sprint | Current |

---

## User Story

**As a** party coordinator,  
**I want** to create party bookings using date, time, and package steps,  
**So that** bookings can be created quickly and consistently.

---

## Acceptance Criteria

### AC-1 - Create draft via date-time-package steps

> **Given** I start a new booking  
> **When** I complete date-time-package steps  
> **Then** a draft booking is created with required booking metadata  
> **And** unavailable slots are prevented from selection.

Required booking metadata for draft creation:

- `bookingId` (GUID string generated at command boundary).
- `partyDateUtc` (ISO-8601 UTC date-time string).
- `slotId` (selected schedule slot identifier).
- `packageId` (selected package identifier).
- `createdUtc` (ISO-8601 UTC timestamp).
- `lastUpdatedUtc` (ISO-8601 UTC timestamp).
- `lifecycleStatus` (must be `Draft` for draft records).
- `operationId` (GUID write-operation identifier).
- `correlationId` (trace identifier for related operations).

Unavailable slot enforcement rules:

- A slot is unavailable when an existing booking has the same `partyDateUtc` and `slotId` with lifecycle status not equal to `Cancelled`.
- Slot conflicts are evaluated in UTC.
- On conflict, progression to the confirmation step is blocked and an actionable validation prompt is displayed.

### AC-2 - Block progression on incomplete required fields

> **Given** required booking fields are incomplete  
> **When** I attempt to continue  
> **Then** progression is blocked  
> **And** actionable validation prompts are shown.

Validation matrix:

- Date step: `partyDateUtc` is required and must not be in the past.
- Time step: `slotId` is required and must be available at validation time.
- Package step: `packageId` is required and must resolve to a selectable package.
- Review step: all required fields must be present before save is enabled.

Validation response contract:

- Use canonical error code + user-safe message pairs from `PartyBookingConstants`.
- Include user-safe messages in UI; keep diagnostic detail in diagnostic field only.

### AC-3 - Persist confirmed booking with lifecycle status

> **Given** I confirm booking details  
> **When** booking is saved  
> **Then** a persistent booking record is created with unique booking ID  
> **And** initial lifecycle status is set.

Initial lifecycle status requirements:

- Draft creation sets `lifecycleStatus = Draft`.
- Successful confirmation sets `lifecycleStatus = Booked`.

Write-path traceability requirements:

- Confirmation writes must persist `operationId` and `correlationId`.
- All write operations must return the standard result envelope (`isSuccess`, `errorCode`, `userMessage`, `diagnosticMessage`, `payload`).

---

## Context

Epic 3 finalized mixed-cart checkout and governance-heavy financial controls. Epic 4 begins party booking lifecycle capabilities. Story 4.1 establishes the first booking slice: a deterministic 3-step flow (date -> time -> package) with local persistence and validation.

This story must optimize for frontline execution and clarity:

1. Staff should complete booking setup with minimal navigation overhead.
2. Availability must be enforced before confirmation.
3. Validation must be actionable and non-destructive.
4. The resulting booking entity must be stable for Story 4.2 deposit/timeline expansion.

---

## Scope

### In Scope

- New booking flow with three explicit steps: date, time slot, package.
- Availability query/check for date+time selection.
- Required-field and step-gating validation with user-safe messages.
- Draft booking state and confirmed booking persistence.
- Unique booking identifier generation at save.
- Initial lifecycle status assignment for new bookings.
- Standard result envelope usage for all booking use-case responses.
- Operation and correlation ID capture on all draft/confirm write paths.
- Unit tests for flow/state validation and persistence behavior.
- Integration tests for availability conflict and write-path integrity.

### Out of Scope

- Deposit capture and timeline generation (Story 4.2).
- Room assignment/conflict resolution workflows (Story 4.3).
- Catering/decor and risk indicators (Story 4.4).
- Inventory reservation/release policies (Stories 4.5-4.6).
- External integrations and notifications.

---

## Architecture Guardrails

- Keep layer boundaries strict: `Features -> Application -> Infrastructure`.
- Apply existing `AppResult<T>` and canonical error code/message patterns.
- Use DI registration via feature/service collection extensions.
- Use UTC timestamps and operation-safe identifiers.
- Preserve ViewModel-driven state transitions; do not put business rules in page code-behind.
- Reuse existing naming and testing conventions from Epic 3.
- Enforce canonical result envelope fields for use-case responses.
- Emit and persist `operationId` and `correlationId` on write paths.
- Use explicit ViewModel processing states (`Idle -> Loading -> Success|Error|Deferred`) in addition to wizard step state.
- Apply validation at three layers: ViewModel (field), Application (business), Infrastructure (constraints).

---

## File Impact Plan

### Create - Domain

1. `POSOpen/Domain/Entities/PartyBooking.cs`
- Booking aggregate root for date/time/package and lifecycle state.

2. `POSOpen/Domain/Enums/PartyBookingStatus.cs`
- Initial lifecycle statuses (e.g., `Draft`, `Booked`).

### Create - Application

3. `POSOpen/Application/Abstractions/Repositories/IPartyBookingRepository.cs`
- CRUD operations for draft/confirmed booking persistence.

4. `POSOpen/Application/UseCases/Party/CreateDraftPartyBookingUseCase.cs`
- Creates/updates draft booking state through step progression.

5. `POSOpen/Application/UseCases/Party/ConfirmPartyBookingUseCase.cs`
- Validates complete state and persists confirmed booking with unique ID.

6. `POSOpen/Application/UseCases/Party/GetBookingAvailabilityUseCase.cs`
- Evaluates unavailable slots for selected date/time.

7. `POSOpen/Application/UseCases/Party/PartyBookingDtos.cs`
- Step state DTOs, availability DTOs, and save-result DTO.

8. `POSOpen/Application/UseCases/Party/PartyBookingConstants.cs`
- Error codes/messages for validation and availability conditions.

### Create - Infrastructure

9. `POSOpen/Infrastructure/Persistence/Configurations/PartyBookingConfiguration.cs`
10. `POSOpen/Infrastructure/Persistence/Repositories/PartyBookingRepository.cs`
11. `POSOpen/Infrastructure/Persistence/Migrations/<timestamp>_AddPartyBookings.cs`
12. `POSOpen/Infrastructure/Persistence/PosOpenDbContext.cs` (add DbSet)

Migration naming convention:

- Use timestamped EF migration name format `yyyyMMddHHmmss_AddPartyBookings`.
- Migration must add table, unique booking ID constraint, and unique index covering date+slot for active bookings.

### Create / Modify - Features (UI)

13. `POSOpen/Features/Party/ViewModels/PartyBookingWizardViewModel.cs`
- Manages step progression and validation.

14. `POSOpen/Features/Party/Views/PartyBookingWizardPage.xaml`
15. `POSOpen/Features/Party/Views/PartyBookingWizardPage.xaml.cs`
16. `POSOpen/Features/Party/PartyRoutes.cs`
17. `POSOpen/Features/Party/PartyServiceCollectionExtensions.cs`
18. `POSOpen/MauiProgram.cs` (register party feature)

### Tests

19. `POSOpen.Tests/Unit/Party/PartyBookingWizardViewModelTests.cs`
20. `POSOpen.Tests/Unit/Party/CreateDraftPartyBookingUseCaseTests.cs`
21. `POSOpen.Tests/Unit/Party/ConfirmPartyBookingUseCaseTests.cs`
22. `POSOpen.Tests/Integration/PartyBookingRepositoryTests.cs`

---

## Definition of Done

- [ ] 3-step flow is implemented with deterministic progression and validation.
- [ ] Unavailable slots cannot be selected.
- [ ] Required fields gate progression with actionable prompts.
- [ ] Confirm action persists unique booking record with initial lifecycle status.
- [ ] Story tests cover success, validation failures, and availability blocking.
- [ ] No regressions introduced to existing checkout/admissions flows.

---

## Tasks / Subtasks

### Task 1 - Domain and Persistence Foundation
- [x] Add `PartyBooking` domain entity and lifecycle enum.
- [x] Add repository abstraction and EF configuration.
- [x] Add migration and DbContext registration.
- [x] Add unique constraints/indexes for booking identity and slot conflict prevention.
- [x] Persist operation/correlation IDs for write records.

### Task 2 - Booking Flow Application Logic
- [x] Implement availability use case for date/time step.
- [x] Implement draft-step progression use case.
- [x] Implement confirmation use case with final validation and persistence.
- [x] Return canonical result envelope + canonical error codes for all outcomes.
- [x] Enforce lifecycle transitions `Draft -> Booked` only through use-case flow.

### Task 3 - Party Booking UI Flow
- [x] Add wizard ViewModel with step state machine (`Date`, `Time`, `Package`, `Review`).
- [x] Add page and route integration.
- [x] Surface validation prompts and unavailable-slot UX.
- [x] Implement processing state model (`Idle`, `Loading`, `Success`, `Error`, `Deferred`) without hidden side effects.

### Task 4 - Test Coverage
- [x] Add unit tests for step validation and availability outcomes.
- [x] Add unit tests for confirmation behavior and lifecycle status assignment.
- [x] Add integration test coverage for booking persistence.
- [x] Add integration test for concurrent slot contention (single winner, deterministic loser error).
- [x] Add integration test asserting operation/correlation ID persistence on write paths.
- [x] Add contract tests for result envelope shape and canonical error code mapping.

---

## Previous Story Intelligence

From Epic 3 closure:

- Strong use-case boundary discipline reduced regressions and should be maintained.
- High-risk logic should be covered with integration tests earlier in the cycle.
- Story artifact and sprint-status synchronization should happen at each status transition.

---

## Dev Agent Record

### Agent Model Used

GPT-5.3-Codex

### Implementation Plan

1. Finalize domain model and persistence constraints for booking ID + slot conflict rules.
2. Implement availability and draft/confirm use cases using canonical result envelope and IDs.
3. Implement wizard + processing state behavior in ViewModel/UI.
4. Add unit, integration, and contract tests for validation, concurrency, and envelope consistency.

### Completion Notes List

- Implemented party booking domain model, EF configuration, repository, and migration scaffolding with active-slot uniqueness constraints.
- Added booking use cases for availability, draft persistence, and confirmation with canonical AppResult envelope semantics.
- Added Party feature route, DI registration, and MAUI wizard page/viewmodel with explicit processing and step states.
- Added unit and integration tests for draft validation, confirmation behavior, slot contention handling, and traceability field persistence.
- Updated app startup and persistence DI to register Party feature and repository dependencies.
- Ran full regression suite successfully (231 passed, 0 failed).

### Debug Log

- 2026-03-31: `dotnet test POSOpen.Tests/POSOpen.Tests.csproj --filter "FullyQualifiedName~Party"` (pass: 15/15).
- 2026-03-31: `dotnet test POSOpen.Tests/POSOpen.Tests.csproj` (pass: 231/231).

---

## File List

- POSOpen/Domain/Enums/PartyBookingStatus.cs
- POSOpen/Domain/Entities/PartyBooking.cs
- POSOpen/Application/Abstractions/Repositories/IPartyBookingRepository.cs
- POSOpen/Application/UseCases/Party/PartyBookingConstants.cs
- POSOpen/Application/UseCases/Party/PartyBookingDtos.cs
- POSOpen/Application/UseCases/Party/CreateDraftPartyBookingCommand.cs
- POSOpen/Application/UseCases/Party/ConfirmPartyBookingCommand.cs
- POSOpen/Application/UseCases/Party/GetBookingAvailabilityUseCase.cs
- POSOpen/Application/UseCases/Party/CreateDraftPartyBookingUseCase.cs
- POSOpen/Application/UseCases/Party/ConfirmPartyBookingUseCase.cs
- POSOpen/Infrastructure/Persistence/Configurations/PartyBookingConfiguration.cs
- POSOpen/Infrastructure/Persistence/Repositories/PartyBookingRepository.cs
- POSOpen/Infrastructure/Persistence/Migrations/20260402090000_AddPartyBookings.cs
- POSOpen/Infrastructure/Persistence/PosOpenDbContext.cs
- POSOpen/Infrastructure/Persistence/PersistenceServiceCollectionExtensions.cs
- POSOpen/Features/Party/PartyRoutes.cs
- POSOpen/Features/Party/PartyServiceCollectionExtensions.cs
- POSOpen/Features/Party/ViewModels/PartyBookingWizardViewModel.cs
- POSOpen/Features/Party/Views/PartyBookingWizardPage.xaml
- POSOpen/Features/Party/Views/PartyBookingWizardPage.xaml.cs
- POSOpen/MauiProgram.cs
- POSOpen.Tests/POSOpen.Tests.csproj
- POSOpen.Tests/Unit/Party/CreateDraftPartyBookingUseCaseTests.cs
- POSOpen.Tests/Unit/Party/ConfirmPartyBookingUseCaseTests.cs
- POSOpen.Tests/Unit/Party/PartyBookingWizardViewModelTests.cs
- POSOpen.Tests/Integration/Party/PartyBookingRepositoryTests.cs
- POSOpen.Tests/TestDoubles/TestDbContextFactory.cs

---

## Change Log

| Date | Change |
|---|---|
| 2026-03-31 | Story created and set to ready-for-dev from Epic 4 backlog. |
| 2026-03-31 | DS started: moved story and sprint status to in-progress. |
| 2026-03-31 | Story review fixes applied: clarified metadata contract, validation matrix, lifecycle rules, IDs, and test requirements. |
| 2026-03-31 | Implemented Story 4.1 code, UI wizard flow, persistence, and tests; moved story to review after full regression pass. |

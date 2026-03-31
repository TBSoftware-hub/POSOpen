# Story 4.1 - Implement 3-Step Party Booking Flow

## Metadata

| Field | Value |
|---|---|
| Epic | 4 - Party Booking Lifecycle and Inventory Coordination |
| Story | 4.1 |
| Key | `4-1-implement-3-step-party-booking-flow` |
| Status | ready-for-dev |
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

### AC-2 - Block progression on incomplete required fields

> **Given** required booking fields are incomplete  
> **When** I attempt to continue  
> **Then** progression is blocked  
> **And** actionable validation prompts are shown.

### AC-3 - Persist confirmed booking with lifecycle status

> **Given** I confirm booking details  
> **When** booking is saved  
> **Then** a persistent booking record is created with unique booking ID  
> **And** initial lifecycle status is set.

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
- Unit tests for flow/state validation and persistence behavior.

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
- [ ] Add `PartyBooking` domain entity and lifecycle enum.
- [ ] Add repository abstraction and EF configuration.
- [ ] Add migration and DbContext registration.

### Task 2 - Booking Flow Application Logic
- [ ] Implement availability use case for date/time step.
- [ ] Implement draft-step progression use case.
- [ ] Implement confirmation use case with final validation and persistence.

### Task 3 - Party Booking UI Flow
- [ ] Add wizard ViewModel with step state machine (`Date`, `Time`, `Package`, `Review`).
- [ ] Add page and route integration.
- [ ] Surface validation prompts and unavailable-slot UX.

### Task 4 - Test Coverage
- [ ] Add unit tests for step validation and availability outcomes.
- [ ] Add unit tests for confirmation behavior and lifecycle status assignment.
- [ ] Add integration test coverage for booking persistence.

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

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.

---

## Change Log

| Date | Change |
|---|---|
| 2026-03-31 | Story created and set to ready-for-dev from Epic 4 backlog. |

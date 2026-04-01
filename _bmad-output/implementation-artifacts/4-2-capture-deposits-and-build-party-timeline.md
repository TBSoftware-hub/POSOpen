# Story 4.2 - Capture Deposits and Build Party Timeline

## Metadata

| Field | Value |
|---|---|
| Epic | 4 - Party Booking Lifecycle and Inventory Coordination |
| Story | 4.2 |
| Key | `4-2-capture-deposits-and-build-party-timeline` |
| Status | done |
| Author | Timbe (via BMAD Story Creator) |
| Created | 2026-03-31 |
| Target Sprint | Current |

---

## User Story

**As a** party coordinator,  
**I want** to capture deposit commitments and generate a lifecycle timeline,  
**So that** event execution is planned and financially tracked from booking time.

---

## Acceptance Criteria

### AC-1 - Record deposit obligation on commitment

> **Given** a booking is ready for commitment  
> **When** deposit details are entered and confirmed  
> **Then** deposit obligation is recorded in booking financial state.

### AC-2 - Generate timeline for confirmed booking

> **Given** a booking is confirmed  
> **When** lifecycle generation runs  
> **Then** a party timeline is created with core milestone statuses (`booked`, `upcoming`, `active`, `completed`)  
> **And** booking-detail presentation states can include UX rail labels (`arrived`, `waiver-pending`, `exception`) as derived sub-states of those core milestones.

### AC-3 - Expose milestone state and next actions in booking detail

> **Given** timeline milestones exist  
> **When** coordinator views booking detail  
> **Then** milestone state and next actions are visible.

### AC-4 - Meet timeline retrieval/update performance target

> **Given** a confirmed booking exists  
> **When** the party timeline is retrieved or updated  
> **Then** the response is returned within 3 seconds (NFR4).

---

## Scope

### In Scope

- Add deposit commitment capture for already-created booking records.
- Persist booking financial obligation fields needed to track deposit commitment state.
- Generate a deterministic timeline projection for each confirmed booking.
- Expose timeline milestones plus next-action guidance for coordinator-facing booking detail.
- Ensure retrieval/update paths for timeline stay within NFR4 target under normal active-day usage.
- Add tests for deposit recording, timeline generation, and performance guardrail behavior.

### Out of Scope

- Room-assignment conflict logic (Story 4.3).
- Catering/decor risk models (Story 4.4).
- Inventory reserve/release execution (Story 4.5).
- Substitution policy authoring and governance workflows (Story 4.6).
- External payment gateway settlement orchestration beyond commitment recording.

---

## Context

Story 4.1 delivered the booking wizard, draft/confirm persistence, and booking lifecycle baseline (`Draft -> Booked`). Story 4.2 extends that baseline with two capabilities:

1. Booking-level deposit commitment capture for financial tracking.
2. Booking timeline generation and retrieval with explicit milestone and next-action visibility.

This story must extend existing 4.1 booking assets instead of introducing a parallel booking model.

---

## Current Repo Reality

- Party booking aggregate and persistence already exist:
  - `POSOpen/Domain/Entities/PartyBooking.cs`
  - `POSOpen/Infrastructure/Persistence/Repositories/PartyBookingRepository.cs`
  - `POSOpen/Infrastructure/Persistence/Configurations/PartyBookingConfiguration.cs`
- Booking status enum currently supports `Draft`, `Booked`, and `Cancelled` only:
  - `POSOpen/Domain/Enums/PartyBookingStatus.cs`
- Existing Party use cases and DTO contracts are already in place:
  - `POSOpen/Application/UseCases/Party/CreateDraftPartyBookingUseCase.cs`
  - `POSOpen/Application/UseCases/Party/ConfirmPartyBookingUseCase.cs`
  - `POSOpen/Application/UseCases/Party/PartyBookingDtos.cs`
- Wizard UI state machine exists and uses CommunityToolkit.MVVM patterns:
  - `POSOpen/Features/Party/ViewModels/PartyBookingWizardViewModel.cs`
- Existing cart-level policy rules already recognize party deposits in checkout domain (`SinglePartyDepositRule`, `CateringRequiresPartyDepositRule`), so this story should avoid duplicate policy semantics and instead expose booking-side deposit commitment data for downstream use.

---

## Previous Story Intelligence

From Story 4.1 implementation and review history:

- Maintain strict layer discipline: Feature ViewModels call Application use cases; no direct Infrastructure access from UI.
- Preserve canonical `AppResult<T>` envelope and canonical error/user-message patterns.
- Keep `operationId` and `correlationId` propagation on write operations.
- Keep explicit processing states in ViewModels (`Idle -> Loading -> Success|Error|Deferred` pattern).
- Keep UTC timestamps and deterministic conflict/validation behavior.
- Extend existing Party feature slice and tests instead of creating duplicate modules.

---

## Architecture Compliance Guardrails

- Respect feature-first layering (`Features/Party`, `Application/UseCases/Party`, `Infrastructure/Persistence`).
- Keep deposit/timeline business rules in Application use cases, not in XAML code-behind.
- Continue using canonical result envelope fields:
  - `isSuccess`, `errorCode`, `userMessage`, `diagnosticMessage`, `payload`.
- Use UTC and ISO-8601 semantics for all timeline/deposit timestamps.
- Continue operation traceability on booking write paths.
- Keep retry/orchestration policies at infrastructure boundaries only.

---

## Deposit and Timeline Contract

### Deposit Commitment Contract (AC-1)

When recording deposit commitment for a confirmed booking, capture at minimum:

- `bookingId` (existing booking ID).
- `depositAmountCents` (integer minor units; no floating-point amounts).
- `depositCurrency` (ISO currency code).
- `depositCommittedAtUtc` (UTC timestamp).
- `depositCommitmentStatus` (story-scoped enum, for example `None`, `Committed`, `Waived` if needed).
- `operationId` and `correlationId` for the commitment write action.

Guardrails:

- Do not create a separate booking aggregate for deposits; extend booking persistence model or attach a tightly scoped child entity in the Party feature.
- Do not implement full payment settlement or processor orchestration in this story.
- Deposit commitment must be idempotent for repeated command submissions with same operation context.
- Enforce idempotency with a persisted unique operation key per booking commitment write (for example, `bookingId + operationId`) to prevent duplicate side effects during retries.

### Timeline Generation Contract (AC-2, AC-3)

Timeline generation must produce milestone state for:

- `booked`
- `upcoming`
- `active`
- `completed`

Booking detail presentation may additionally surface derived sub-states from UX guidance:

- `arrived` (derived under `upcoming`)
- `waiver-pending` (derived under `upcoming` or `active`)
- `exception` (derived overlay on any core milestone)

For each milestone, return:

- `milestoneKey`
- `status`
- `effectiveAtUtc` (if known)
- `nextActionCode`
- `nextActionLabel` (user-safe)

Guardrails:

- Timeline can be implemented as computed projection for this story; avoid premature workflow-engine complexity.
- Keep milestone-state derivation deterministic from booking + current UTC + explicit completion signal persisted in V1.
- Completion signal for this story is `CompletedAtUtc` (nullable UTC) on booking aggregate or tightly-scoped party lifecycle child entity.
- `completed` milestone is only valid when `CompletedAtUtc` has a value.
- Include explicit fallback next action when required data is missing.

### Performance Contract (AC-4)

- Timeline retrieval and update actions must complete within 3 seconds at P95 under the active-day test profile (NFR4).
- Add low-overhead query shape and indexes needed to avoid full-table scanning on active-day timeline views.
- Active-day test profile for this story: 1,000 confirmed bookings for the target day, warm database, and 20 concurrent timeline requests.
- Add integration-level timing assertions or targeted performance guard tests for the story path, recording median and P95.

---

## UX and Interaction Requirements

- Keep booking-detail experience status-at-a-glance before deep detail.
- Surface milestone state and next actions in a timeline rail/list that supports quick operator scanning.
- Keep error recovery inline (no forced navigation reset).
- Use clear state language for booking timeline and deposit commitment outcomes.
- Preserve tablet-friendly interaction density and touch-target expectations established in prior stories.
- Booking detail is a dedicated post-confirmation surface (page/section) and not a replacement of the existing 3-step booking wizard interaction.

---

## Tasks / Subtasks

### Task 1 - Extend booking financial state for deposit commitments (AC: 1)

- [ ] Add story-scoped deposit commitment fields and enum(s) in Domain Party model(s).
- [ ] Update EF configuration and migration to persist deposit commitment fields with UTC-safe conversions.
- [ ] Add repository/API support for reading and writing deposit commitment state with idempotent operation behavior.
- [ ] Keep schema and names consistent with existing `party_bookings` conventions.

### Task 2 - Implement deposit commitment use case and contract DTOs (AC: 1)

- [ ] Add `RecordPartyDepositCommitmentUseCase` and command/DTO contracts in `Application/UseCases/Party`.
- [ ] Enforce booking preconditions: booking exists and is in a commit-eligible state.
- [ ] Return canonical `AppResult<T>` with user-safe messages and canonical error codes.
- [ ] Propagate operation and correlation IDs through write path and result payload.

### Task 3 - Implement timeline projection generation/retrieval use cases (AC: 2, 3, 4)

- [ ] Add timeline DTO contracts for milestone status and next actions.
- [ ] Implement `GetPartyBookingTimelineUseCase` (and update use case if needed) that derives `booked/upcoming/active/completed` milestones and maps optional UX sub-states.
- [ ] Ensure projection logic is deterministic and UTC-safe.
- [ ] Add explicit completion signal persistence (`CompletedAtUtc`) required to derive `completed` milestone.
- [ ] Keep retrieval/update paths under 3-second P95 target using the active-day test profile.

### Task 4 - Wire Party UI to deposit and timeline experience (AC: 1, 2, 3)

- [ ] Extend Party coordinator-facing ViewModel(s) to capture deposit commitment input and submit action.
- [ ] Add/extend booking detail ViewModel and page to display timeline milestones and next-action guidance.
- [ ] Preserve explicit processing state transitions and actionable validation/error copy.
- [ ] Keep flow additive to Story 4.1 wizard (no regressions to existing 3-step booking behavior).

### Task 5 - Add tests and quality gates (AC: 1, 2, 3, 4)

- [ ] Unit tests for deposit commitment validation, idempotency, and error mapping.
- [ ] Unit tests for timeline derivation across all milestone states.
- [ ] Integration tests for persistence mappings, query shape, and timeline retrieval/update path.
- [ ] Add performance-oriented guard tests/assertions for NFR4 timeline operations using active-day profile (median + P95).
- [ ] Regression tests to ensure Story 4.1 draft/confirm behavior remains stable.

---
- [x] Unit tests for deposit commitment validation, idempotency, and error mapping.
- [x] Unit tests for timeline derivation across all milestone states.
- [x] Integration tests for persistence mappings, query shape, and timeline retrieval/update path.
- [x] Add performance-oriented guard tests/assertions for NFR4 timeline operations using active-day profile (median + P95).
- [x] Regression tests to ensure Story 4.1 draft/confirm behavior remains stable.
- `POSOpen/Infrastructure/Persistence/Migrations/*`
- `POSOpen/Features/Party/ViewModels/PartyBookingWizardViewModel.cs`
- `POSOpen.Tests/Unit/Party/PartyBookingWizardViewModelTests.cs`

---

## Definition of Done

- [x] Deposit commitment can be captured and persisted for a confirmed booking.
- [x] Timeline milestones (`booked`, `upcoming`, `active`, `completed`) are generated and retrievable.
- [x] Booking completion signal persistence (`CompletedAtUtc`) is available and drives `completed` milestone derivation.
- [x] Booking detail view shows milestone state and next actions.
- [x] Timeline retrieval/update actions satisfy NFR4 (`<= 3s` P95) under active-day profile (1,000 bookings, warm DB, 20 concurrent requests).
- [x] Story 4.1 booking wizard flows still pass regression tests.
|:--|:--|:--|
| Confirmed booking receives first valid deposit commitment | AC1 | Commitment persisted with amount/currency/UTC timestamp and operation trace IDs |
| Repeated same-operation commitment submit | AC1 | Idempotent success; no duplicate commitment side effects |
| Timeline requested for confirmed booking before event day | AC2, AC3, AC4 | Timeline returns `booked` + `upcoming` states with next actions; response meets <= 3s P95 under active-day profile |
| Timeline requested during active event window | AC2, AC3, AC4 | Timeline reflects `active` state and operational next actions; response meets <= 3s P95 under active-day profile |
| Timeline requested after completion signal is persisted | AC2, AC3 | Timeline reflects `completed` state with closure guidance |
| Booking is unconfirmed or missing required state | AC1, AC2 | User-safe validation error; no partial writes |

---

## References

- [Source: _bmad-output/planning-artifacts/epics.md#Story-4.2-Capture-Deposits-and-Build-Party-Timeline]
- [Source: _bmad-output/planning-artifacts/epics.md#Epic-4-Party-Booking-Lifecycle-and-Inventory-Coordination]
- [Source: _bmad-output/planning-artifacts/prd.md#Party-Coordinator---Booking-to-Event-Execution]
- [Source: _bmad-output/planning-artifacts/prd.md#Performance]
- [Source: _bmad-output/planning-artifacts/architecture.md#Implementation-Patterns--Consistency-Rules]
- [Source: _bmad-output/planning-artifacts/architecture.md#Project-Structure--Boundaries]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Party-Timeline-Rail]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Step-12-UX-Consistency-Patterns]
- [Source: _bmad-output/implementation-artifacts/4-1-implement-3-step-party-booking-flow.md]
- [Source: POSOpen/Domain/Entities/PartyBooking.cs]
- [Source: POSOpen/Domain/Enums/PartyBookingStatus.cs]
- [Source: POSOpen/Application/Abstractions/Repositories/IPartyBookingRepository.cs]
- [Source: POSOpen/Application/UseCases/Party/CreateDraftPartyBookingUseCase.cs]
- [Source: POSOpen/Application/UseCases/Party/ConfirmPartyBookingUseCase.cs]
- [Source: POSOpen/Application/UseCases/Party/PartyBookingDtos.cs]
- [Source: POSOpen/Application/UseCases/Party/PartyBookingConstants.cs]
- [Source: POSOpen/Features/Party/ViewModels/PartyBookingWizardViewModel.cs]
- [Source: POSOpen/Infrastructure/Persistence/Configurations/PartyBookingConfiguration.cs]
- [Source: POSOpen/Infrastructure/Persistence/Repositories/PartyBookingRepository.cs]

## Dev Agent Record

### Agent Model Used

GPT-5.3-Codex

### Debug Log References


### Completion Notes List


### File List

### Debug Log

| Task | Notes |
|:--|:--|
| Task 1 | Domain fields (`DepositAmountCents`, `DepositCurrency`, `DepositCommittedAtUtc`, `DepositCommitmentStatus`, `DepositCommitmentOperationId`, `CompletedAtUtc`), enums (`PartyDepositCommitmentStatus`, `PartyTimelineMilestoneStatus`), EF config, migration `20260402100000_AddPartyBookingDepositsAndTimeline`, and repository methods (`RecordDepositCommitmentAsync`, `MarkCompletedAsync`) all in place. Unique index on `deposit_commitment_operation_id` enforces idempotent operation key at DB level. |
| Task 2 | `RecordPartyDepositCommitmentUseCase` validates amount > 0 and 3-letter alpha currency, checks booking is `Booked`, idempotency short-circuit on same `operationId`, canonical error codes and user-safe messages. |
| Task 3 | `GetPartyBookingTimelineUseCase` derives all four milestones deterministically from booking state + UTC clock + `CompletedAtUtc`. Rail labels (`arrived`, `waiver-pending`, `exception`) as derived sub-states. `MarkPartyBookingCompletedUseCase` persists `CompletedAtUtc` with idempotency guard. |
| Task 4 | `PartyBookingDetailViewModel` exposes `RefreshTimelineCommand`, `SubmitDepositCommitmentCommand`, `MarkCompletedCommand` with `Idle→Loading→Success/Error` transitions. `PartyBookingDetailPage` uses `IQueryAttributable` for shell navigation with `bookingId`. `PartyBookingWizardPage` wires `OpenBookingDetailClicked` for post-confirm navigation. |
| Task 5 | Initial performance test inserted 1,000 bookings via 2,000 sequential SQLite transactions (>5 min). Fixed with EF Core `AddRange` + single `SaveChangesAsync` bulk insert; test now runs in ~21 s with NFR4 assertions passing. |

### Completion Notes

- All 23 Party tests pass: 13 unit + 10 integration (including NFR4 guard at 1,000 confirmed bookings / 20 concurrent requests).
- No regressions to Story 4.1 wizard flow.
- `PartyTimelineRepositoryTests.cs` setup uses direct EF Core bulk insert instead of use-case inserts; semantically equivalent for the retrieval-under-load scenario tested.

### Implementation File List

**New Files**
- `POSOpen/Application/UseCases/Party/RecordPartyDepositCommitmentCommand.cs`
- `POSOpen/Application/UseCases/Party/RecordPartyDepositCommitmentUseCase.cs`
- `POSOpen/Application/UseCases/Party/MarkPartyBookingCompletedCommand.cs`
- `POSOpen/Application/UseCases/Party/MarkPartyBookingCompletedUseCase.cs`
- `POSOpen/Application/UseCases/Party/PartyTimelineDtos.cs`
- `POSOpen/Application/UseCases/Party/GetPartyBookingTimelineUseCase.cs`
- `POSOpen/Domain/Enums/PartyTimelineMilestoneStatus.cs`
- `POSOpen/Domain/Enums/PartyDepositCommitmentStatus.cs`
- `POSOpen/Features/Party/ViewModels/PartyBookingDetailViewModel.cs`
- `POSOpen/Features/Party/Views/PartyBookingDetailPage.xaml`
- `POSOpen/Features/Party/Views/PartyBookingDetailPage.xaml.cs`
- `POSOpen/Infrastructure/Persistence/Migrations/20260402100000_AddPartyBookingDepositsAndTimeline.cs`
- `POSOpen.Tests/Unit/Party/RecordPartyDepositCommitmentUseCaseTests.cs`
- `POSOpen.Tests/Unit/Party/GetPartyBookingTimelineUseCaseTests.cs`
- `POSOpen.Tests/Integration/Party/PartyTimelineRepositoryTests.cs`

**Modified Files**
- `POSOpen/Domain/Entities/PartyBooking.cs`
- `POSOpen/Application/Abstractions/Repositories/IPartyBookingRepository.cs`
- `POSOpen/Application/UseCases/Party/PartyBookingDtos.cs`
- `POSOpen/Application/UseCases/Party/PartyBookingConstants.cs`
- `POSOpen/Infrastructure/Persistence/Configurations/PartyBookingConfiguration.cs`
- `POSOpen/Infrastructure/Persistence/Repositories/PartyBookingRepository.cs`
- `POSOpen/Features/Party/PartyRoutes.cs`
- `POSOpen/Features/Party/PartyServiceCollectionExtensions.cs`
- `POSOpen/Features/Party/ViewModels/PartyBookingWizardViewModel.cs`
- `POSOpen.Tests/Integration/Party/PartyTimelineRepositoryTests.cs` (bulk insert optimization)

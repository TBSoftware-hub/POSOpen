# Story 2.4: Admission Completion with Deferred Payment Continuity

Status: review

## Story

As a cashier,
I want admissions to complete even when payment settlement is deferred/offline,
so that frontline operations continue during network disruption.

## Acceptance Criteria

**Given** network and processor are available  
**When** payment is authorized  
**Then** admission is marked completed  
**And** receipt/check-in confirmation is issued.

**Given** settlement cannot complete due to connectivity/processor outage  
**When** cashier confirms admission  
**Then** payment action is queued with unique operation ID  
**And** admission continuation follows deferred-payment policy.

**Given** a deferred payment exists  
**When** cashier views current admission state  
**Then** explicit queued/deferred status is shown  
**And** next-best-action guidance is visible.

## Tasks / Subtasks

- [ ] Introduce an admissions completion result model that can represent both settled and deferred outcomes. (AC: 1, 2, 3)
  - [ ] Add an admissions persistence aggregate such as `AdmissionCheckInRecord` under `POSOpen/Domain/Entities/` with explicit fields for `FamilyId`, `OperationId`, `CompletionStatus`, `SettlementStatus`, `AmountCents`, `CompletedAtUtc`, `SettlementDeferredAtUtc`, `ConfirmationCode`, and `ReceiptReference`.
  - [ ] Add an enum under `POSOpen/Domain/Enums/` for settlement state with only the story-scoped values needed now: `Authorized` and `DeferredQueued`.
  - [ ] Keep this model admissions-only; do not attempt to generalize into mixed-cart checkout or retail/party payment composition in this story. Epic 3 owns that broader transaction model.
  - [ ] Use integer minor units (`AmountCents`) plus ISO currency code rather than floating-point payment amounts.
- [ ] Implement a completion use case that re-checks fast-path eligibility and returns an explicit completion outcome. (AC: 1, 2, 3)
  - [ ] Create `POSOpen/Application/UseCases/Admissions/CompleteAdmissionCheckInUseCase.cs` plus request/response contracts and constants in the same folder.
  - [ ] The use case must call `EvaluateFastPathCheckInUseCase` and `ProfileAdmissionUseCase.InitializeAsync(...)` before committing so waiver and profile completeness are revalidated at execution time, not trusted from stale ViewModel state.
  - [ ] Authorize using the same `ICurrentSessionService` + `IAuthorizationPolicyService` admissions pattern already used by Stories 2.1-2.3.
  - [ ] Return `AppResult<AdmissionCompletionResultDto>` with a story-scoped outcome contract that distinguishes `Authorized` versus `DeferredQueued`, exposes `OperationId`, and provides user-safe next-step guidance.
  - [ ] Include a story-scoped amount input in the command contract instead of inventing mixed-cart logic. If the fast-path UI does not yet have a pricing source, use a minimal admissions-only total provider behind an interface rather than hard-coding view strings into the use case.
- [ ] Persist admission completion, outbox enqueue, and audit log atomically. (AC: 1, 2)
  - [ ] Do not chain the current `IOutboxRepository.EnqueueAsync(...)` and `IOperationLogRepository.AppendAsync(...)` calls as separate commits for this workflow; the architecture requires local state change, audit record, and queued payment event to commit in one transaction.
  - [ ] Introduce a purpose-built admissions persistence abstraction in `POSOpen/Application/Abstractions/Repositories/` or `.../Services/` that allows the infrastructure implementation to write the admissions record, operation-log entry, and optional outbox message through one `PosOpenDbContext` transaction.
  - [ ] Implement the infrastructure writer in `POSOpen/Infrastructure/Persistence/` using the existing `OutboxMessage` and `OperationLogEntry` shapes and `OperationContextFactory` for operation/correlation IDs.
  - [ ] On online authorization, persist an `AdmissionCompleted` operation log event and mark the admission as completed with `SettlementStatus = Authorized`.
  - [ ] On deferred settlement, persist the admission as completed with `SettlementStatus = DeferredQueued`, append an `AdmissionPaymentQueued` operation log event, and enqueue one outbox message containing the operation ID and payment payload snapshot.
- [ ] Extend the fast-path admissions UI to complete admission and surface deferred state transparently. (AC: 1, 2, 3)
  - [ ] Replace the current `FastPathCheckInUiService.ShowFastPathReadyAsync()` placeholder path with a real completion flow from `FastPathCheckInViewModel`.
  - [ ] Add a primary `CompleteCheckInCommand` path that calls `CompleteAdmissionCheckInUseCase` and updates the screen state to `Success` or `DeferredQueued`.
  - [ ] Add a compact admissions-only summary card to the fast-path page that shows the total being settled for this story without attempting the Epic 3 mixed-cart experience.
  - [ ] Show explicit deferred UI when settlement is queued: badge/chip text, operation ID, confirmation text, and next-best-action copy that makes it clear the guest can continue while payment follow-up is pending.
  - [ ] Keep the valid-waiver one-lane interaction intact; queued/deferred status must appear in the same fast-path lane rather than sending the user to a separate recovery screen.
- [ ] Update session-level sync messaging and confirmation behavior. (AC: 1, 2, 3)
  - [ ] Use `IAppStateService.SetSyncState(...)` to reflect queued payment state when a deferred admission is created.
  - [ ] Issue an on-screen confirmation/receipt reference for both success paths. Hardware receipt printing remains Epic 3 scope, so this story should not introduce printer-device integration.
  - [ ] Ensure the confirmation message differentiates between `Paid and completed` versus `Checked in with payment queued`.
  - [ ] Preserve operation ID and confirmation code in the view model so support and later exception-review stories can correlate the local action.
- [ ] Add focused tests for authorization, atomic persistence, and deferred-state UX. (AC: 1, 2, 3)
  - [ ] Unit tests for the completion use case: authorized immediate completion, deferred queue fallback, blocked completion when waiver/profile checks fail, and admissions authorization failure.
  - [ ] Integration tests for the persistence writer proving that admission record, outbox message, and operation log are committed together and include the same operation/correlation identifiers.
  - [ ] ViewModel tests verifying the screen shows deferred status, guidance text, and preserved confirmation data after a queued outcome.
  - [ ] Add at least one regression test that ensures online authorization does not create an outbox entry while deferred completion does.

## Dev Notes

### Story Intent

Story 2.4 is the first implementation that turns the current fast-path waiver gate into a real admission completion flow. The key requirement is continuity, not full checkout breadth: a cashier must be able to finish the guest moment even when payment authorization cannot settle immediately, while preserving enough local state for later replay, audit, and exception review.

### Current Repo Reality

- `FastPathCheckInViewModel` currently stops at a placeholder alert through `IFastPathCheckInUiService.ShowFastPathReadyAsync()` once waiver and profile checks pass.
- `ProfileAdmissionUseCase.InitializeAsync(...)` is already the authoritative profile-completeness check and is called from `FastPathCheckInViewModel`; keep using that instead of duplicating required-field logic.
- `OutboxRepository` and `OperationLogRepository` already exist, but each creates its own `PosOpenDbContext` and commits independently.
- `OperationContextFactory` already provides the required root/child `OperationContext` creation pattern for `OperationId`, `CorrelationId`, `CausationId`, and UTC timestamps.
- `AppStateService` already exposes a single sync-state string; 2.4 should use it for the cashier-visible queued/deferred signal instead of inventing a second app-wide sync indicator.
- There is no existing admissions payment entity, deferred-payment aggregate, or receipt model in the repo today. This story must add the minimum admissions-only record necessary for continuity and auditability without pre-building Epic 3's mixed-cart abstractions.

### Previous Story Intelligence

- Story 2.1 established the admissions feature slice, route constants, family lookup, and `FamilyProfile` as the base aggregate for Epic 2. Do not create a second guest or account model.
- Story 2.2 established that fast-path eligibility is driven by authoritative waiver state and can be invalidated by in-session refresh. 2.4 must re-check eligibility before commit and must not trust stale UI flags.
- Story 2.3 established the profile-completion handoff contract and currently sits in `review`. Use the current `ProfileAdmissionUseCase` behavior as the dependency surface, but assume any 2.3 review fixes may still land before 2.4 implementation starts.
- Story 2.3 validation closed its previous route and completeness ambiguities, so 2.4 can treat `familyId` handoff and missing-field detection as stable.

### Architecture Compliance Guardrails

- Preserve the documented flow: Presentation -> Application -> Infrastructure. The ViewModel must call a use case, not repositories.
- Keep business decisions in the use case layer: online-versus-deferred outcome selection, next-best-action text selection, and eligibility rechecks do not belong in XAML code-behind.
- Follow the architecture requirement that aggregate change, audit log, and outbox message commit in one transaction. This story is the first place where that rule matters for admissions.
- Use past-tense PascalCase event names for audit/outbox records, for example `AdmissionCompleted` and `AdmissionPaymentQueued`.
- Keep timestamps UTC and identifiers GUID-based using the existing `OperationContextFactory` and serializer/persistence conventions.

### Atomic Persistence Requirement

This is the highest-risk implementation point in Story 2.4.

- The existing generic `IOutboxRepository` and `IOperationLogRepository` are safe for standalone writes, but they are not sufficient by themselves for admission completion because they save through separate DbContext instances.
- If the implementation writes the local admission record first and the outbox/audit second, a crash can leave an admission marked complete with no replayable payment action.
- If it writes the outbox first and the admission second, replay could occur without a durable local completion record.
- The correct implementation is one admissions-specific persistence boundary that writes all three artifacts together inside a single EF Core transaction.

### Deferred Payment Policy Contract

- Online success path:
  - authorization succeeds
  - local admission is completed
  - settlement state is `Authorized`
  - user receives a normal completion confirmation
  - no outbox payment message is created
- Deferred path:
  - processor or connectivity failure is classified as deferred-eligible
  - local admission is still completed
  - settlement state is `DeferredQueued`
  - exactly one outbox message is created with the same `OperationId` shown to the user
  - user receives explicit guidance that check-in completed and payment follow-up is pending
- Non-eligible failures:
  - preserve current screen context
  - show a user-safe actionable error
  - do not partially commit local completion state

### UX and Confirmation Contract

- Keep the fast-lane experience one-screen and tablet-friendly per the UX specification.
- Reuse the Confidence Strip semantics already implied by the fast-path page: status must be visible before detail.
- Use the existing semantic status mapping from the UX planning artifact:
  - authorized/success -> green success treatment
  - deferred/queued -> amber warning treatment
  - hard failure -> red error treatment
- The deferred outcome must show all of the following without extra navigation:
  - a visible queued/deferred label
  - a short next-best-action sentence
  - the operation ID or confirmation reference
  - reassurance that prior cashier input is preserved
- This story issues a confirmation reference, not hardware receipt printing. Printing belongs to Epic 3 Story 3.4.

### Suggested Implementation Slice

1. Add the admissions completion domain model and persistence abstraction.
2. Implement the application use case and constants/DTOs.
3. Implement the infrastructure transactional writer using one DbContext transaction.
4. Replace the fast-path completion placeholder with the real use-case invocation and outcome rendering.
5. Add unit, integration, and ViewModel tests around the two outcome branches.

### File Structure Requirements

Expected new files for this story:

```text
POSOpen/Domain/Entities/AdmissionCheckInRecord.cs
POSOpen/Domain/Enums/AdmissionSettlementStatus.cs
POSOpen/Application/Abstractions/Repositories/IAdmissionCheckInRepository.cs
POSOpen/Application/UseCases/Admissions/CompleteAdmissionCheckInCommand.cs
POSOpen/Application/UseCases/Admissions/AdmissionCompletionResultDto.cs
POSOpen/Application/UseCases/Admissions/CompleteAdmissionCheckInConstants.cs
POSOpen/Application/UseCases/Admissions/CompleteAdmissionCheckInUseCase.cs
POSOpen/Infrastructure/Persistence/Repositories/AdmissionCheckInRepository.cs
POSOpen/Infrastructure/Persistence/Configurations/AdmissionCheckInRecordConfiguration.cs
POSOpen.Tests/Unit/Admissions/CompleteAdmissionCheckInUseCaseTests.cs
POSOpen.Tests/Integration/Admissions/AdmissionCheckInRepositoryTests.cs
```

Expected existing files to change:

```text
POSOpen/Features/Admissions/ViewModels/FastPathCheckInViewModel.cs
POSOpen/Features/Admissions/Views/FastPathCheckInPage.xaml
POSOpen/Features/Admissions/Views/FastPathCheckInPage.xaml.cs
POSOpen/Features/Admissions/AdmissionsServiceCollectionExtensions.cs
POSOpen/Infrastructure/Services/FastPathCheckInUiService.cs
POSOpen/Infrastructure/Persistence/PosOpenDbContext.cs
POSOpen/Infrastructure/Persistence/PersistenceServiceCollectionExtensions.cs
POSOpen/Infrastructure/Persistence/Migrations/*
POSOpen.Tests/Unit/Admissions/FastPathCheckInViewModelTests.cs
```

### Error Code Contract

- `AUTH_FORBIDDEN`: current session is missing or does not have admissions permission.
- `ADMISSION_FAST_PATH_BLOCKED`: waiver or profile completeness recheck failed.
- `ADMISSION_PAYMENT_DEFERRED`: settlement could not complete immediately, but admission was queued and completed locally.
- `ADMISSION_COMPLETION_FAILED`: completion could not be committed locally; no partial state should remain.
- `ADMISSION_QUEUE_PERSISTENCE_FAILED`: deferred queue write failed before commit completed.
- `ADMISSION_AMOUNT_REQUIRED`: the completion command did not receive a valid admissions total.

### Testing Requirements

- Verify AC1 with an online-authorized path that returns success, writes an admissions record, appends an operation-log event, and does not enqueue an outbox message.
- Verify AC2 with a deferred path that returns success, writes an admissions record with `DeferredQueued`, enqueues exactly one outbox message, and exposes the same `OperationId` in both persistence and UI result.
- Verify AC2 failure handling does not leave an admission marked complete when the atomic persistence step fails.
- Verify AC3 with fast-path screen reload or return-to-page behavior showing queued/deferred state and actionable guidance.
- Verify the ViewModel still blocks completion when 2.2 waiver gating or 2.3 profile completeness fail during the final recheck.

### Acceptance Test Matrix

| Scenario | AC | Expected Result |
|:--|:--|:--|
| Waiver valid, profile complete, payment authorized | AC1 | Admission completes locally, confirmation reference is shown, no outbox payment message exists |
| Waiver valid, profile complete, processor unavailable but deferred policy allowed | AC2 | Admission completes locally, settlement state is `DeferredQueued`, outbox contains one queued payment with shared `OperationId` |
| Waiver/profile become invalid before final completion | AC1, AC2 | Completion is blocked, no admission record is committed, user sees actionable recovery guidance |
| Deferred admission reopened or revisited | AC3 | Screen shows queued/deferred badge plus next-best-action copy and confirmation metadata |
| Atomic persistence failure while creating deferred completion | AC2 | Result is failure, no partial admission/outbox/audit combination is left behind |

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story-2.4-Admission-Completion-with-Deferred-Payment-Continuity]
- [Source: _bmad-output/planning-artifacts/prd.md#Frontline-Admissions-and-Check-In]
- [Source: _bmad-output/planning-artifacts/architecture.md#Core-Architectural-Decisions]
- [Source: _bmad-output/planning-artifacts/architecture.md#Implementation-Patterns--Consistency-Rules]
- [Source: _bmad-output/planning-artifacts/architecture.md#Project-Structure--Boundaries]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Step-7-Defining-Core-Experience]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Step-10-User-Journey-Flows]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Step-11-Component-Strategy]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Step-12-UX-Consistency-Patterns]
- [Source: _bmad-output/implementation-artifacts/2-1-family-lookup-with-search-and-scan.md]
- [Source: _bmad-output/implementation-artifacts/2-2-waiver-aware-fast-path-check-in.md]
- [Source: _bmad-output/implementation-artifacts/2-3-new-or-incomplete-profile-admission.md]
- [Source: _bmad-output/implementation-artifacts/2-3-new-or-incomplete-profile-admission-validation-report.md]
- [Source: POSOpen/Features/Admissions/ViewModels/FastPathCheckInViewModel.cs]
- [Source: POSOpen/Application/UseCases/Admissions/ProfileAdmissionUseCase.cs]
- [Source: POSOpen/Infrastructure/Persistence/Repositories/OutboxRepository.cs]
- [Source: POSOpen/Infrastructure/Persistence/Repositories/OperationLogRepository.cs]
- [Source: POSOpen/Infrastructure/Services/FastPathCheckInUiService.cs]
- [Source: POSOpen/Application/Abstractions/Services/IAppStateService.cs]
- [Source: POSOpen/Application/Abstractions/Services/IOperationContextFactory.cs]
- [Source: POSOpen/Shared/Operational/OperationContext.cs]

## Dev Agent Record

### Agent Model Used

GPT-5.4

### Debug Log References

- Story creation only; no build or test command was run in this workflow.

### Completion Notes List

- Story context created from Epic 2 planning artifacts, UX constraints, architecture rules, and the current admissions/outbox code path.
- Guidance explicitly anchors the work on `FastPathCheckInViewModel`, `ProfileAdmissionUseCase`, `OutboxRepository`, `OperationLogRepository`, `OperationContextFactory`, and `IAppStateService`.
- Story includes a transaction-safety guardrail so deferred payment implementation does not violate the architecture requirement for atomic local state + audit + outbox persistence.

### File List

- _bmad-output/implementation-artifacts/2-4-admission-completion-with-deferred-payment-continuity.md
# Story 2.5: Check-In Performance and UX Compliance

Status: ready-for-dev

## Story

As a manager/product owner,
I want admissions/check-in to meet speed and UX consistency targets,
so that staff can reliably complete the core guest moment under pressure.

## Acceptance Criteria

**Given** normal connected operation  
**When** standard returning-family check-in is executed  
**Then** operator feedback is returned within 2 seconds (NFR1)  
**And** path supports the fast-lane UX interaction model.

**Given** check-in UI is displayed  
**When** primary/secondary actions are rendered  
**Then** button hierarchy, status messaging, and accessibility patterns follow UX standards.

**Given** a check-in flow is interrupted by recoverable errors  
**When** cashier resolves issue inline  
**Then** flow continues without forced restart  
**And** selected-family context, waiver/status guidance, admission total, and completion/deferred result context remain preserved unless cashier explicitly switches families.

## Tasks / Subtasks

- [ ] Define and enforce a story-scoped check-in performance contract in the admissions flow. (AC: 1)
  - [ ] Add a small timing abstraction for check-in interaction latency measurement that can be unit-tested with deterministic time control.
  - [ ] Define an explicit measurement boundary for AC1: start at command invocation in `FastPathCheckInViewModel` and stop at first user-visible feedback state update (`GuidanceMessage`, `ErrorMessage`, or `ShowCompletionResult`).
  - [ ] Measure elapsed time for the connected returning-family fast-path lane and emit structured diagnostics when elapsed time exceeds 2 seconds.
  - [ ] Ensure diagnostics include operation/correlation context when available and never expose unsafe internal details in user-facing text.
  - [ ] Keep the implementation local-first and non-blocking; performance telemetry must not delay check-in completion.

- [ ] Optimize fast-path check-in responsiveness for the connected path. (AC: 1)
  - [ ] Review `FastPathCheckInViewModel` flow and remove avoidable serial waits in refresh/evaluate path.
  - [ ] Cache or reuse story-safe values within the active check-in context where this does not violate data freshness rules from Story 2.2.
  - [ ] Preserve required revalidation behavior from Stories 2.2 and 2.4; optimization must not weaken waiver/profile correctness.
  - [ ] Keep behavior deterministic across repeated refresh actions during a single session.

- [ ] Align fast-path UI with UX consistency and accessibility rules. (AC: 2)
  - [ ] Apply explicit button hierarchy (primary, secondary, tertiary/destructive) on the fast-path page.
  - [ ] Ensure status messaging follows UX semantic mapping (success/warning/error/info) with icon/text clarity, not color-only signaling.
  - [ ] Verify touch target sizing and keyboard navigation parity for primary actions.
  - [ ] Ensure assistive text/linkage exists for error and guidance regions so state changes are discoverable.

- [ ] Guarantee inline recovery continuity for recoverable errors. (AC: 3)
  - [ ] Keep user input and selected family context intact when recoverable failures occur.
  - [ ] Preserve, at minimum, `FamilyId`, `FamilyDisplayName`, `WaiverStatusLabel`, `GuidanceMessage`, `AdmissionTotalLabel`, `ShowCompletionResult`, `CompletionStatusLabel`, `ConfirmationCode`, and `OperationIdText` across recoverable retry paths.
  - [ ] Ensure recovery actions are offered inline on the same fast-lane screen without forced restart loops.
  - [ ] Avoid clearing completion-relevant context unless a deliberate new-family initialization occurs.
  - [ ] Ensure transient errors can be retried successfully without losing operational state.

- [ ] Add focused tests for latency guardrails and UX/recovery behavior. (AC: 1, 2, 3)
  - [ ] Unit tests for timing/diagnostic emission behavior and threshold handling.
  - [ ] ViewModel tests covering recoverable error retry without state loss.
  - [ ] UI/ViewModel behavior tests covering action hierarchy visibility and status-state consistency.
  - [ ] Add a regression test ensuring optimizations do not break Story 2.4 deferred-payment continuity path.

## Dev Notes

### Story Intent

Story 2.5 is a hardening story for frontline execution quality. The objective is not new business capability, but performance and UX conformance for the admissions fast lane that was implemented across Stories 2.1-2.4.

### Current Repo Reality

- `FastPathCheckInViewModel` already orchestrates evaluate, profile-completeness checks, completion invocation, and deferred state rendering.
- `FastPathCheckInPage.xaml` already contains core waiver status, error, completion-result, and action controls.
- Story 2.4 introduced transactional admission completion persistence and deferred settlement continuity, which must remain behaviorally intact.
- Admissions tests already exist under `POSOpen.Tests/Unit/Admissions/` and should be extended, not replaced.

### Previous Story Intelligence

- Story 2.4 landed end-to-end completion flow and addressed PR feedback for cancellation flow, stale-state reset, and stable total formatting.
- Recent commits indicate active admissions momentum (`ddd4bf3`, `bf056a6`) and should be treated as baseline patterns for naming, dependency wiring, and tests.
- Maintain existing repository and DI patterns introduced by Story 2.4 rather than adding parallel abstractions.

### Architecture Compliance Guardrails

- Preserve layering: Presentation -> Application -> Infrastructure. Keep business rules out of XAML/code-behind.
- Keep ViewModel-first patterns using CommunityToolkit.Mvvm conventions.
- Preserve local-first resilience and explicit state messaging (offline/sync/exception visibility).
- Do not regress atomic persistence and deferred-payment behavior introduced by Story 2.4.
- Keep result-envelope and user-safe error messaging conventions.

### UX Compliance Contract (Story-Scoped)

- One-screen fast-lane interaction remains primary for frontline staff.
- Primary action remains visually dominant and accessible.
- Warnings and errors must include direct next-best-action guidance.
- Inline correction and retry must be preferred over flow restart.
- Status communication must include text semantics and not depend on color alone.

### Performance Contract (Story-Scoped)

- Target path: connected, returning-family fast-path check-in interaction feedback.
- Threshold: feedback returned within 2 seconds (NFR1).
- Measurement boundary: command invocation to first user-visible feedback state update.
- Measurement should be deterministic and testable in unit scope using an injected time abstraction (no wall-clock dependency in unit tests).
- Violations should be surfaced in diagnostics for operational tuning without blocking user progress.

### Suggested Implementation Slice

1. Add timing/diagnostic abstraction and baseline tests.
2. Refactor fast-path evaluation path for responsiveness while preserving correctness checks.
3. Apply UX hierarchy/accessibility updates in fast-path UI and ViewModel state contracts.
4. Add inline recovery continuity refinements.
5. Validate with targeted unit tests and admissions regression coverage.

### File Structure Requirements

Expected files to modify:

```text
POSOpen/Features/Admissions/ViewModels/FastPathCheckInViewModel.cs
POSOpen/Features/Admissions/Views/FastPathCheckInPage.xaml
POSOpen/Features/Admissions/Views/FastPathCheckInPage.xaml.cs
POSOpen/Application/Abstractions/Services/* (single timing/diagnostic interface only, if required)
POSOpen/Infrastructure/Services/* (single timing/diagnostic adapter only, if required)
POSOpen.Tests/Unit/Admissions/FastPathCheckInViewModelTests.cs
POSOpen.Tests/Unit/Admissions/* (timing/performance guardrail tests only)
```

Out-of-scope for Story 2.5 unless a defect fix is required:

```text
POSOpen/Domain/**
POSOpen/Infrastructure/Persistence/**
POSOpen/Application/UseCases/Admissions/CompleteAdmissionCheckInUseCase.cs
```

Expected files to review for regression safety:

```text
POSOpen/Application/UseCases/Admissions/CompleteAdmissionCheckInUseCase.cs
POSOpen/Infrastructure/Persistence/Repositories/AdmissionCheckInRepository.cs
POSOpen.Tests/Unit/Admissions/CompleteAdmissionCheckInUseCaseTests.cs
POSOpen.Tests/Integration/Admissions/AdmissionCheckInRepositoryTests.cs
```

### Testing Requirements

- Verify AC1 with deterministic tests using injected time control:
  - pass case at `<= 2000ms` for connected returning-family path
  - threshold-breach case at `> 2000ms` that emits diagnostics but does not block flow
- Verify AC2 through UI/ViewModel tests that enforce action hierarchy and status semantics.
- Verify AC3 through retry/continuity tests that preserve the defined minimum state contract after recoverable errors.
- Re-run admissions tests from Stories 2.2-2.4 to ensure no behavioral regressions.

### Acceptance Test Matrix

| Scenario | AC | Expected Result |
|:--|:--|:--|
| Connected returning-family fast-path check-in | AC1 | Feedback observable within 2 seconds and check-in flow remains stable |
| Fast-path actions rendered on tablet layout | AC2 | Primary action is visually dominant; secondary/recovery actions follow hierarchy |
| Recoverable evaluate or completion error | AC3 | Inline retry available; family/input context preserved; no forced restart |
| Deferred-payment path after performance/UX refactor | AC1, AC3 | Story 2.4 behavior unchanged, including queued/deferred status clarity |

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story-2.5-Check-In-Performance-and-UX-Compliance]
- [Source: _bmad-output/planning-artifacts/implementation-readiness-report-2026-03-28.md#Non-Functional-Requirements-24-total]
- [Source: _bmad-output/planning-artifacts/architecture.md#Frontend-Architecture]
- [Source: _bmad-output/planning-artifacts/architecture.md#Implementation-Patterns--Consistency-Rules]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Step-10-User-Journey-Flows]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Step-12-UX-Consistency-Patterns]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Step-13-Responsive-Design--Accessibility]
- [Source: _bmad-output/implementation-artifacts/2-4-admission-completion-with-deferred-payment-continuity.md]
- [Source: POSOpen/Features/Admissions/ViewModels/FastPathCheckInViewModel.cs]
- [Source: POSOpen/Features/Admissions/Views/FastPathCheckInPage.xaml]
- [Source: POSOpen/Infrastructure/Persistence/Repositories/AdmissionCheckInRepository.cs]

## Dev Agent Record

### Agent Model Used

GPT-5.3-Codex

### Debug Log References

- Story creation workflow only; no build/test command required for artifact generation.

### Completion Notes List

- Created Story 2.5 implementation artifact with performance, UX consistency, and recovery continuity guardrails.
- Incorporated NFR1 and UX consistency patterns into actionable tasks and file-level implementation guidance.
- Anchored story context to existing admissions flow delivered in Stories 2.1-2.4.

### File List

- _bmad-output/implementation-artifacts/2-5-check-in-performance-and-ux-compliance.md

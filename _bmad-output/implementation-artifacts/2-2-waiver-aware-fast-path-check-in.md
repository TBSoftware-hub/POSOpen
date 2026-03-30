# Story 2.2: Waiver-Aware Fast-Path Check-In

Status: review

## Story

As a cashier,
I want the system to evaluate waiver status and route the check-in path,
so that valid families can be checked in quickly and missing waivers are handled safely.

## Acceptance Criteria

**Given** a selected family has a valid waiver  
**When** check-in starts  
**Then** system enables fast-path admissions flow  
**And** waiver-valid state is clearly shown.

**Given** waiver is missing/expired/invalid  
**When** check-in starts  
**Then** system blocks fast-path completion  
**And** required waiver recovery action is presented.

**Given** waiver status changes during session  
**When** status is re-evaluated  
**Then** check-in eligibility updates immediately  
**And** stale status is not used.

## Tasks / Subtasks

- [x] Implement waiver-evaluation use case and DTOs. (AC: 1, 2, 3)
  - [x] Create `POSOpen/Application/UseCases/Admissions/EvaluateFastPathCheckInUseCase.cs`.
  - [x] Create request/response models in `POSOpen/Application/UseCases/Admissions/` for fast-path eligibility and waiver guidance.
  - [x] Authorize with `ICurrentSessionService.GetCurrent()` plus `IAuthorizationPolicyService.HasPermission(session.Role, ...)` using admissions permissions.
  - [x] Return `AppResult<T>` with canonical, user-safe messages for allowed, blocked, and refresh-required states.
- [x] Add repository read pattern for authoritative waiver state. (AC: 1, 2, 3)
  - [x] Reuse `IFamilyProfileRepository.GetByIdAsync(...)` as the authoritative source for current waiver state.
  - [x] Keep waiver decision logic in the use case (not in repository or view code).
  - [x] Treat `WaiverStatus.Valid` as the only fast-path-eligible state.
- [x] Build fast-path admissions UI flow. (AC: 1, 2)
  - [x] Create `POSOpen/Features/Admissions/ViewModels/FastPathCheckInViewModel.cs`.
  - [x] Create `POSOpen/Features/Admissions/Views/FastPathCheckInPage.xaml` and `.xaml.cs`.
  - [x] Route from family selection into fast-path page using selected `familyId` query parameter.
  - [x] Show explicit waiver status banner (valid/pending/expired/no waiver) using existing semantic colors.
  - [x] Block completion command when not eligible and show inline recovery action options.
- [x] Implement waiver recovery action handoff. (AC: 2)
  - [x] Add a recovery command that navigates to the waiver completion/recovery route placeholder used by admissions flow.
  - [x] Preserve user context (`familyId`) during recovery navigation so flow can resume without re-lookup.
  - [x] Keep messaging actionable and non-technical per UX guidance.
- [x] Implement immediate re-evaluation behavior in-session. (AC: 3)
  - [x] Add `RefreshWaiverStatusCommand` in fast-path view model.
  - [x] Re-check eligibility on page appearing/resume and after recovery flow returns.
  - [x] Ensure stale evaluation results are replaced by the latest repository read before allowing completion.
- [x] Wire routes and DI registration for 2.2 flow. (AC: 1, 2, 3)
  - [x] Extend `POSOpen/Features/Admissions/AdmissionsServiceCollectionExtensions.cs` to register new view model/page and route.
  - [x] Keep route constants in `POSOpen/Features/Admissions/AdmissionsRoutes.cs`.
  - [x] Update `FamilyLookupViewModel.SelectFamilyAsync(...)` to navigate to `AdmissionsRoutes.FastPathCheckIn` with `familyId`.
- [x] Add tests for fast-path waiver routing behavior. (AC: 1, 2, 3)
  - [x] Unit tests for use case eligibility decisions across waiver statuses.
  - [x] Unit tests for unauthorized session handling and error-code contracts.
  - [x] ViewModel tests for blocked completion behavior and refresh/re-evaluation.
  - [x] Integration test validating changed waiver status is reflected on re-evaluation.

## Dev Notes

### Story Intent

This story activates the `admissions/fast-path-check-in` route introduced in 2.1 and makes waiver state a first-class gate for admission completion. The core objective is strict safety with speed: valid waiver families move forward immediately, while all non-valid states are blocked with clear recovery guidance.

### Existing Implementation Context (from 2.1)

- `FamilyLookupViewModel.SelectFamilyAsync(...)` currently logs a warning instead of navigating; this is the handoff point to implement now.
- `AdmissionsRoutes.FastPathCheckIn` constant already exists and should be reused.
- `FamilyProfile` already contains `WaiverStatus` and `WaiverCompletedAtUtc`; do not add duplicate waiver state models.
- `SearchFamiliesUseCase` already enforces admissions authorization and `AppResult<T>` patterns; follow the same approach in new use case(s).

### Technical Requirements and Guardrails

- Keep layering strict: Presentation -> Application -> Infrastructure.
- Do not put waiver eligibility logic in XAML code-behind.
- Keep repository methods persistence-focused; decision rules belong in application use case(s).
- Use UTC and existing clock abstractions if timestamps are touched.
- Prefer extending existing admissions feature structure; do not create a parallel admissions module.

### Waiver Eligibility Contract

- Eligible for fast path: `WaiverStatus.Valid`.
- Blocked states: `WaiverStatus.None`, `WaiverStatus.Pending`, `WaiverStatus.Expired`.
- Blocked states must expose a recovery action and must not allow completion command execution.
- Re-evaluation must read fresh waiver state before allowing completion.

### UX and Accessibility Guardrails

- Preserve one-screen completion feel for valid-waiver flow.
- Surface waiver status prominently at top of fast-path page using semantic status style.
- Keep recovery messaging actionable, brief, and user-safe.
- Ensure keyboard and touch accessibility parity, including visible focus and minimum touch target sizes.
- Preserve entered context and avoid forcing restart after waiver recovery.

### Testing Requirements

- Validate all waiver status branches (valid/pending/expired/none).
- Validate blocked completion path cannot bypass eligibility gate.
- Validate refreshed status transitions from blocked->eligible in-session without stale data.
- Validate role-based authorization failures return safe error results.

### Project Structure Notes

- Follow current admissions file organization under `POSOpen/Features/Admissions/` and `POSOpen/Application/UseCases/Admissions/`.
- Keep route registration in admissions feature extension and shell routing conventions already used in project.
- Reuse existing AppResult and authorization policy patterns from Epic 1 and Story 2.1.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story-2.2-Waiver-Aware-Fast-Path-Check-In]
- [Source: _bmad-output/planning-artifacts/architecture.md#Implementation-Patterns--Consistency-Rules]
- [Source: _bmad-output/planning-artifacts/architecture.md#Project-Structure--Boundaries]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Step-10-User-Journey-Flows]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Step-12-UX-Consistency-Patterns]
- [Source: _bmad-output/implementation-artifacts/2-1-family-lookup-with-search-and-scan.md]
- [Source: POSOpen/Features/Admissions/AdmissionsRoutes.cs]
- [Source: POSOpen/Features/Admissions/ViewModels/FamilyLookupViewModel.cs]
- [Source: POSOpen/Application/UseCases/Admissions/SearchFamiliesUseCase.cs]
- [Source: POSOpen/Domain/Entities/FamilyProfile.cs]

## Dev Agent Record

### Agent Model Used

GPT-5.3-Codex

### Debug Log References

- `dotnet build POSOpen.sln -c Debug -v minimal` (pass)
- `dotnet test POSOpen.Tests/POSOpen.Tests.csproj --framework net10.0-windows10.0.19041.0 -v minimal` (pass, 105/105)

### Completion Notes List

- Implemented fast-path waiver evaluation pipeline via `EvaluateFastPathCheckInUseCase` using authoritative `IFamilyProfileRepository.GetByIdAsync(...)` reads, role authorization guard, and canonical result-state mapping.
- Added fast-path admissions UI (`FastPathCheckInPage` + `FastPathCheckInViewModel`) with waiver status banner, recovery action handoff, refresh command, and blocked completion guard.
- Replaced family selection placeholder with live navigation from lookup to fast-path route carrying `familyId` context.
- Registered new use case/viewmodel/page and route in admissions feature DI setup.
- Added unit and integration coverage for waiver decision branches, authorization failure, unavailable/not-found contracts, and in-session re-evaluation behavior.
- Added ViewModel coverage for blocked completion re-check, refresh exception handling, and recovery-navigation failure behavior.
- All solution tests pass after implementation.

### File List

- POSOpen/Application/UseCases/Admissions/EvaluateFastPathCheckInQuery.cs
- POSOpen/Application/UseCases/Admissions/FastPathCheckInEvaluationResultDto.cs
- POSOpen/Application/UseCases/Admissions/EvaluateFastPathCheckInConstants.cs
- POSOpen/Application/UseCases/Admissions/EvaluateFastPathCheckInUseCase.cs
- POSOpen/Features/Admissions/ViewModels/FastPathCheckInViewModel.cs
- POSOpen/Features/Admissions/Views/FastPathCheckInPage.xaml
- POSOpen/Features/Admissions/Views/FastPathCheckInPage.xaml.cs
- POSOpen/Features/Admissions/ViewModels/FamilyLookupViewModel.cs
- POSOpen/Features/Admissions/AdmissionsRoutes.cs
- POSOpen/Features/Admissions/AdmissionsServiceCollectionExtensions.cs
- POSOpen.Tests/Unit/Admissions/EvaluateFastPathCheckInUseCaseTests.cs
- POSOpen.Tests/Unit/Admissions/FastPathCheckInViewModelTests.cs
- POSOpen.Tests/Integration/Admissions/EvaluateFastPathCheckInUseCaseIntegrationTests.cs
- _bmad-output/implementation-artifacts/2-2-waiver-aware-fast-path-check-in.md

## Assumptions

- A dedicated waiver completion flow/page is not yet implemented; this story uses a recovery handoff placeholder while preserving family context.
- `WaiverStatus` enum values from Story 2.1 are the canonical states for fast-path decisions.
- Admissions permission constants already cover fast-path check-in authorization; if not, extend existing admissions permissions without changing role model semantics.
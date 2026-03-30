# Story 2.3: New or Incomplete Profile Admission

Status: done

## Story

As a cashier,
I want to admit families with missing profile data through a guided minimal flow,
so that admissions continue without losing required information quality.

## Acceptance Criteria

**Given** no existing profile is found  
**When** cashier chooses create profile  
**Then** required minimum fields are collected and validated  
**And** profile is created for immediate admission use.

**Given** a profile is incomplete  
**When** cashier proceeds with admission  
**Then** system prompts for missing mandatory fields only  
**And** previously entered values are preserved.

**Given** required fields fail validation  
**When** user submits  
**Then** submission is blocked  
**And** field-level actionable errors are shown without clearing inputs.

## Tasks / Subtasks

- [x] Implement guided profile completion use case(s) for new and incomplete families. (AC: 1, 2, 3)
  - [x] Create admissions use-case contracts under `POSOpen/Application/UseCases/Admissions/` for: initialize profile draft, validate missing required fields, and submit profile for admission continuity.
  - [x] Enforce admissions authorization using the canonical `ICurrentSessionService` + `IAuthorizationPolicyService` pattern used in prior Epic 2 stories.
  - [x] Return `AppResult<T>` for all outcomes with user-safe messages and canonical error codes (no exception leakage to UI).
  - [x] Ensure use-case logic differentiates new profile creation from incomplete-profile completion while using one consistent validation contract.
- [x] Extend admissions data model/repository contracts for required profile completeness checks. (AC: 1, 2)
  - [x] Add/confirm explicit required-field policy in application layer (not persistence layer) for minimum admission-ready profile data.
  - [x] Add repository methods as needed to load and persist profile draft/completion state without duplicating lookup logic from Story 2.1.
  - [x] Preserve previously entered values when collecting only missing mandatory fields.
  - [x] Keep repository implementations persistence-focused; business validation remains in use-case layer.
- [x] Build UI flow for new/incomplete profile admission continuation. (AC: 1, 2, 3)
  - [x] Create/update admissions ViewModel(s) under `POSOpen/Features/Admissions/ViewModels/` to drive guided minimal data capture and missing-field-only prompts.
  - [x] Create/update admissions page(s) under `POSOpen/Features/Admissions/Views/` with field-level validation messages and non-destructive error handling.
  - [x] Register and wire the `admissions/new-profile` route end-to-end: create page/viewmodel, register route in admissions feature extension, and honor route query input `hint` from lookup handoff.
  - [x] Define and enforce route output contract from new-profile flow: successful submit must return/forward `familyId` for immediate admissions continuation.
  - [x] Preserve user-entered values across validation failures and inline recovery actions.
  - [x] Keep flow aligned to one-lane admissions UX: status-at-a-glance first, then focused data completion.
- [x] Integrate story 2.1 and 2.2 handoff points into 2.3 path. (AC: 1, 2)
  - [x] Ensure no-match branch from family lookup routes directly into create/continue profile flow with retained lookup context.
  - [x] Ensure fast-path waiver gating from Story 2.2 can route to missing-profile completion when waiver is valid but profile completeness is insufficient.
  - [x] Define deterministic post-success handoff: after create/complete succeeds, navigate to `AdmissionsRoutes.FastPathCheckIn` with `familyId` and trigger fresh eligibility evaluation before completion is enabled.
  - [x] Do not regress existing route constants and admissions DI registration patterns.
- [x] Add tests for profile completion and validation behavior. (AC: 1, 2, 3)
  - [x] Unit tests for required-field validation and missing-field-only prompting behavior.
  - [x] Unit tests for authorization and canonical error-code outcomes.
  - [x] ViewModel tests verifying values are preserved after validation errors.
  - [x] Integration tests for new profile creation and incomplete profile completion persistence paths.

## Dev Notes

### Story Intent

Story 2.3 closes the admissions gap between lookup/waiver routing and payment completion by enabling cashiers to continue admission when no profile exists or when the selected profile is incomplete. The implementation must optimize for speed under desk pressure while preserving data quality through focused required-field capture.

### Previous Story Intelligence (2.1 and 2.2)

- Story 2.1 already provides no-match recovery and a create-new-profile entry point from admissions lookup; reuse that handoff and retain query context.
- Story 2.2 already enforces waiver-aware path gating and fresh-state re-evaluation; avoid duplicating waiver eligibility logic in this story.
- Admissions architecture conventions are established: ViewModel-first, route constants in admissions routes, DI through admissions feature extension, and `AppResult<T>` from use-case layer.

### Technical Requirements and Guardrails

- Keep strict layering: Presentation -> Application -> Infrastructure.
- Keep required-field policy in application/use-case validation, not in XAML code-behind.
- Preserve existing admissions feature structure; do not create a parallel admissions module.
- Use canonical error/result envelope patterns from `Application/Results`.
- Maintain UTC and existing clock abstractions if timestamps are touched.

### Minimum Profile Completion Contract

- Canonical minimum required fields for admission-ready profile are:
  - `PrimaryContactFirstName` (required, trimmed, non-empty)
  - `PrimaryContactLastName` (required, trimmed, non-empty)
  - `Phone` (required, trimmed, non-empty; normalized for lookup consistency)
- Optional field for this story: `Email` (nullable; if present, trim and validate format).
- For new profile path, collect and validate the canonical minimum required fields before allowing admission continuation.
- For incomplete profile path, prompt only for missing mandatory fields and keep prior values untouched.
- Submission failure must keep entered values and show actionable field-level errors.
- Validation feedback must be non-destructive and support immediate inline correction.

### Deterministic Admission Handoff Contract

- Input contract to new-profile flow: optional route query `hint` from lookup no-match branch.
- Success contract from new-profile/incomplete-profile submit:
  - Persist profile updates and resolve a concrete `familyId`.
  - Navigate to `AdmissionsRoutes.FastPathCheckIn` with `familyId`.
  - Trigger fresh fast-path eligibility evaluation (Story 2.2 source of truth) before admission completion controls are enabled.
- Failure contract:
  - Stay on current profile page.
  - Preserve all entered values.
  - Present field-level actionable errors and a user-safe summary message.

### Error Code Contract (Story 2.3)

- `AUTH_FORBIDDEN`: session is missing or role lacks admissions permissions.
- `PROFILE_REQUIRED_FIELDS_MISSING`: one or more mandatory profile fields are missing/invalid.
- `PROFILE_NOT_FOUND`: incomplete-profile path references a missing profile.
- `PROFILE_SAVE_FAILED`: persistence failure during create/update.
- `ADMISSION_ROUTE_UNAVAILABLE`: post-success admission handoff route cannot be resolved.

### UX and Accessibility Guardrails

- Preserve one guided admissions lane with explicit next-best action messaging.
- Keep status visibility clear and avoid forced restarts on validation issues.
- Ensure touch targets and focus behavior remain accessible and consistent with admissions UI patterns.
- Use inline recovery patterns; do not clear form state on recoverable errors.

### Testing Requirements

- Validate both paths: new profile creation and incomplete-profile completion.
- Validate missing-required-fields-only prompting behavior.
- Validate field-level validation errors do not clear user-entered inputs.
- Validate authorization failures and canonical error-code contracts.

### Acceptance Test Matrix

| Scenario | AC | Expected Result |
|:--|:--|:--|
| No-match create profile with valid required fields | AC1 | Profile is created, `familyId` is produced, and flow continues to fast-path handoff route |
| Incomplete profile with only one required field missing | AC2 | Only missing field is prompted; existing values remain unchanged |
| Submit with missing required fields | AC3 | Submission blocked; field-level errors shown; entered values preserved |
| Authorization failure during create/complete | AC1, AC2 | Safe error returned with `AUTH_FORBIDDEN`; no data loss |
| Post-success handoff route unavailable | AC1 | Safe route error shown; current page remains with preserved input |

### Project Structure Notes

- Align with current admissions structure under `POSOpen/Features/Admissions/` and `POSOpen/Application/UseCases/Admissions/`.
- Keep persistence contracts and implementations in existing infrastructure persistence folders.
- Keep route registration and DI in admissions service-collection extension and existing shell routing conventions.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story-2.3-New-or-Incomplete-Profile-Admission]
- [Source: _bmad-output/planning-artifacts/prd.md#Frontline-Admissions-and-Check-In]
- [Source: _bmad-output/planning-artifacts/architecture.md#Implementation-Patterns--Consistency-Rules]
- [Source: _bmad-output/planning-artifacts/architecture.md#Project-Structure--Boundaries]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Front-Desk-Rapid-Admission-and-Checkout]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Step-12-UX-Consistency-Patterns]
- [Source: _bmad-output/implementation-artifacts/2-1-family-lookup-with-search-and-scan.md]
- [Source: _bmad-output/implementation-artifacts/2-2-waiver-aware-fast-path-check-in.md]

## Dev Agent Record

### Agent Model Used

GPT-5.3-Codex

### Debug Log References

- `dotnet build POSOpen.sln -c Debug -v minimal` (succeeded)
- `dotnet test POSOpen.Tests/POSOpen.Tests.csproj --framework net10.0-windows10.0.19041.0 -v minimal` (117 passed, 0 failed)

### Completion Notes List

- Implemented `ProfileAdmissionUseCase` with unified required-field validation for new and incomplete profile paths.
- Added admissions UI flow for profile completion with preserved form state, inline validation errors, and deterministic handoff to fast-path check-in.
- Wired `admissions/new-profile` route through admissions feature DI and Shell routing contracts.
- Added and updated admissions unit/integration tests for use-case validation, handoff behavior, and profile persistence paths.
- Verified solution build and test suite pass on current branch.

### File List

- _bmad-output/implementation-artifacts/2-3-new-or-incomplete-profile-admission.md
- POSOpen/Application/UseCases/Admissions/ProfileAdmissionConstants.cs
- POSOpen/Application/UseCases/Admissions/ProfileAdmissionContracts.cs
- POSOpen/Application/UseCases/Admissions/ProfileAdmissionUseCase.cs
- POSOpen/Application/Abstractions/Services/IProfileAdmissionUiService.cs
- POSOpen/Infrastructure/Services/ProfileAdmissionUiService.cs
- POSOpen/Features/Admissions/ViewModels/NewProfileAdmissionViewModel.cs
- POSOpen/Features/Admissions/Views/NewProfileAdmissionPage.xaml
- POSOpen/Features/Admissions/Views/NewProfileAdmissionPage.xaml.cs
- POSOpen/Features/Admissions/AdmissionsServiceCollectionExtensions.cs
- POSOpen/Features/Admissions/ViewModels/FastPathCheckInViewModel.cs
- POSOpen/Features/Admissions/Views/FastPathCheckInPage.xaml
- POSOpen/Application/Abstractions/Services/IFastPathCheckInUiService.cs
- POSOpen/Infrastructure/Services/FastPathCheckInUiService.cs
- POSOpen/MauiProgram.cs
- POSOpen.Tests/POSOpen.Tests.csproj
- POSOpen.Tests/Unit/Admissions/ProfileAdmissionUseCaseTests.cs
- POSOpen.Tests/Unit/Admissions/NewProfileAdmissionViewModelTests.cs
- POSOpen.Tests/Unit/Admissions/FastPathCheckInViewModelTests.cs
- POSOpen.Tests/Integration/Admissions/ProfileAdmissionUseCaseIntegrationTests.cs

## Assumptions

- Waiver evaluation remains the responsibility of Story 2.2 logic and is consumed as an input/state dependency here.
- Booking-reference-specific profile linkage remains deferred to Epic 4 unless explicitly required by this story's implementation details.

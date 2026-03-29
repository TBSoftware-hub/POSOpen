# Story 1.4: Governed Override Workflow

Status: ready-for-dev

## Story

As a Manager/Owner/Admin,
I want to execute override actions with mandatory reason capture,
So that exceptional operations are controlled and traceable.

## Acceptance Criteria

**Given** I have override permission
**When** I initiate an override action
**Then** the system requires a reason before commit
**And** action context is visible to the approver.

**Given** reason is missing
**When** I confirm override
**Then** action is blocked
**And** a validation error is displayed.

**Given** reason is provided and permission is valid
**When** override is confirmed
**Then** the action succeeds
**And** override metadata is recorded immutably.

## Tasks / Subtasks

- [ ] Introduce override use-case contracts and data models. (AC: 1, 2, 3)
  - [ ] Create `POSOpen/Application/UseCases/Security/SubmitOverrideCommand.cs` (target action key, target reference, reason, context metadata).
  - [ ] Create `POSOpen/Application/UseCases/Security/SubmitOverrideResultDto.cs` with operation ID and immutable event reference fields.
  - [ ] Define canonical override error codes and user-safe messages.
- [ ] Implement application-layer override workflow with policy enforcement. (AC: 1, 2, 3)
  - [ ] Create `POSOpen/Application/UseCases/Security/SubmitOverrideUseCase.cs`.
  - [ ] Authorize actor via `ICurrentSessionService` plus `IAuthorizationPolicyService` (do not trust UI role claims).
  - [ ] Enforce required non-empty reason and trim whitespace-only input.
  - [ ] Ensure action context is available in the result used by UI confirmation surfaces.
  - [ ] Return canonical `AppResult` failures for forbidden access and missing reason.
- [ ] Append immutable override events through existing operation-log infrastructure. (AC: 3)
  - [ ] Use `IOperationLogRepository.AppendAsync` to write `OverrideActionCommitted` event payload.
  - [ ] Include required payload fields: `staffId`, `staffRole`, `actionKey`, `targetReference`, `reason`, `operationId`, `occurredUtc`.
  - [ ] Keep UTC timestamp handling through existing operation context/clock abstractions.
- [ ] Add UI flow for governed override with explicit context and reason capture. (AC: 1, 2, 3)
  - [ ] Create `POSOpen/Features/Security/ViewModels/OverrideApprovalViewModel.cs` using CommunityToolkit.MVVM command/state pattern.
  - [ ] Create `POSOpen/Features/Security/Views/OverrideApprovalPage.xaml` and code-behind for binding setup only.
  - [ ] Present action context, required reason input, and blocking validation message when reason is missing.
  - [ ] Preserve entered reason text on transient failures.
- [ ] Integrate route registration and role-aware entry points. (AC: 1, 3)
  - [ ] Add route constants in `POSOpen/Features/Security/SecurityRoutes.cs` and register in startup.
  - [ ] Expose override entry path only for Manager, Owner, and Admin per policy matrix.
  - [ ] Reuse existing safe denial message semantics for unauthorized access attempts.
- [ ] Add tests for authorization, validation, and immutable logging. (AC: 1, 2, 3)
  - [ ] Unit tests for `SubmitOverrideUseCase`: allowed role + reason success, missing reason blocked, unauthorized role blocked.
  - [ ] Unit tests for payload contract and canonical error code outcomes.
  - [ ] Integration test validating operation-log append with immutable override event fields.
  - [ ] Update `POSOpen.Tests/POSOpen.Tests.csproj` source-link entries for newly added non-UI source files.

## Dev Notes

### Story Intent

This story formalizes controlled override execution so operational exceptions can be approved intentionally and later audited with complete context. It builds directly on role enforcement and authenticated-session authority from stories 1.2 and 1.3.

### Current Repo Reality

- Authentication and trusted session context exist (`ICurrentSessionService`, `AppStateCurrentSessionService`).
- Role permission checks are already centralized through `IAuthorizationPolicyService` and `RolePermissions`.
- Immutable operation-log append paths are established in prior stories through `IOperationLogRepository`.
- No dedicated override feature slice currently exists in Application or Features.

### Architecture Compliance

- Keep strict layering: ViewModel -> UseCase -> Repository/Service -> Infrastructure.
- Keep policy checks in application use-cases, not in XAML-only guards.
- Use canonical `AppResult<T>` envelopes and user-safe copy.
- Keep immutable append-only operation logging with UTC timestamps.

### Security and Override Guardrails

- Only Manager, Owner, and Admin can approve override actions.
- Never accept actor role or identity from UI payloads; resolve from trusted session services.
- Reason is mandatory and must not be bypassed by whitespace-only input.
- Do not leak privileged policy internals to non-authorized users in error messages.
- Override event payload must be immutable and complete for incident investigations.

### Canonical Permission and Error Contract

Permission key for this story:

- `security.override.execute`

Role mapping contract:

- Owner: allowed
- Admin: allowed
- Manager: allowed
- Cashier: denied

Story-specific error codes:

- `AUTH_FORBIDDEN`: actor lacks permission for override action.
- `OVERRIDE_REASON_REQUIRED`: reason is null, empty, or whitespace.
- `OVERRIDE_CONTEXT_INVALID`: required action context/target metadata missing.
- `OVERRIDE_COMMIT_FAILED`: override could not be committed due to transient system failure.

### Override Audit Payload Contract

Required immutable event payload fields:

- `staffId`
- `staffRole`
- `actionKey`
- `targetReference`
- `reason`
- `operationId`
- `occurredUtc`

Event naming:

- `OverrideActionCommitted`

### File Structure Requirements

Expected additions for this story:

- `POSOpen/Application/UseCases/Security/SubmitOverrideCommand.cs`
- `POSOpen/Application/UseCases/Security/SubmitOverrideResultDto.cs`
- `POSOpen/Application/UseCases/Security/SubmitOverrideUseCase.cs`
- `POSOpen/Features/Security/SecurityRoutes.cs`
- `POSOpen/Features/Security/ViewModels/OverrideApprovalViewModel.cs`
- `POSOpen/Features/Security/Views/OverrideApprovalPage.xaml`
- `POSOpen/Features/Security/Views/OverrideApprovalPage.xaml.cs`
- `POSOpen/AppShell.xaml`
- `POSOpen/AppShell.xaml.cs`
- `POSOpen/MauiProgram.cs`
- `POSOpen.Tests/Unit/Security/*Override*.cs`
- `POSOpen.Tests/Integration/Security/*Override*.cs`
- `POSOpen.Tests/POSOpen.Tests.csproj`

### Testing Requirements

- Verify authorized actors (Manager/Owner/Admin) can complete override with valid reason.
- Verify missing reason blocks commit and returns `OVERRIDE_REASON_REQUIRED`.
- Verify Cashier is denied with `AUTH_FORBIDDEN` and safe messaging.
- Verify immutable operation-log event append occurs on successful override.
- Verify no operation-log commit occurs when validation or authorization fails.

### Acceptance Test Matrix

- Manager submits override with valid reason -> success and immutable event appended.
- Owner submits override with valid reason -> success and immutable event appended.
- Admin submits override with valid reason -> success and immutable event appended.
- Cashier submits override -> denied with `AUTH_FORBIDDEN` and no commit.
- Authorized role submits override with empty/whitespace reason -> blocked with `OVERRIDE_REASON_REQUIRED`.
- Authorized role submits override with missing target reference/action key -> blocked with `OVERRIDE_CONTEXT_INVALID`.

### Previous Story Intelligence (1.3)

- Keep authority derivation on trusted session context; avoid UI-supplied actor claims.
- Preserve non-revealing, user-safe error wording patterns.
- Reuse operation-log append conventions and UTC conventions already implemented.
- Preserve source-link testing approach in `POSOpen.Tests` rather than adding direct MAUI project references.

### Git Intelligence Summary

Recent commits completed the authentication flow and role-enforcement hardening. This story should extend those existing security primitives instead of introducing alternate authorization or logging pathways.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story-1.4-Governed-Override-Workflow]
- [Source: _bmad-output/planning-artifacts/architecture.md#Authentication--Security]
- [Source: _bmad-output/planning-artifacts/architecture.md#Implementation-Patterns--Consistency-Rules]
- [Source: _bmad-output/planning-artifacts/architecture.md#Project-Structure--Boundaries]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Step-7-Defining-Core-Experience]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Step-10-User-Journey-Flows]
- [Source: _bmad-output/implementation-artifacts/1-3-terminal-authentication-flow.md]
- [Source: POSOpen/Application/Security/RolePermissions.cs]
- [Source: POSOpen/Application/Abstractions/Security/ICurrentSessionService.cs]
- [Source: POSOpen/Application/Abstractions/Repositories/IOperationLogRepository.cs]

## Dev Agent Record

### Agent Model Used

GPT-5.3-Codex

### Debug Log References

- `git log --oneline -5`

### Completion Notes List

- Story context created for governed overrides with explicit policy, validation, and immutable logging contracts.

### File List

- _bmad-output/implementation-artifacts/1-4-governed-override-workflow.md

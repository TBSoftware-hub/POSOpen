# Story 1.5: Immutable Audit Trail for Security-Critical Actions

Status: review

## Story

As an Owner/Admin,
I want immutable audit records for account, role, override, and elevation actions,
So that governance and incident analysis are reliable.

## Acceptance Criteria

**Given** a security-critical action occurs (account change, role change, override, elevation)  
**When** the action completes  
**Then** an immutable audit record is appended  
**And** it includes actor, timestamp UTC, action type, and target reference.

**Given** audit records exist  
**When** queried by authorized users  
**Then** records are returned in chronological order  
**And** records are non-editable and non-destructive.

**Given** unauthorized users attempt audit access  
**When** request is made  
**Then** access is denied  
**And** denial is logged.

## Tasks / Subtasks

- [x] Extend security-critical use cases to emit canonical immutable audit events. (AC: 1)
  - [x] Verify all existing account and role mutations always append operation-log events (`StaffAccountCreated`, `StaffAccountUpdated`, `StaffAccountDeactivated`, `StaffRoleAssigned`) with UTC occurrence time and trusted actor context.
  - [x] Add missing append-only audit events for override and elevation flows introduced in Story 1.4 (or wire stubs if 1.4 implementation is pending in branch).
  - [x] Standardize payload shape for security-critical audit events: `actorStaffId`, `targetReference`, `actionType`, `occurredUtc`, `operationId`, `correlationId`.
  - [x] Ensure audit append is coupled to mutation success and never executed as a mutable post-edit/update path.
- [x] Add an authorized audit query use case and contract. (AC: 2, 3)
  - [x] Create query contract in application layer (for example, `ListSecurityAuditTrailQuery`).
  - [x] Add authorization gate using `IAuthorizationPolicyService` + `ICurrentSessionService` so only Owner/Admin can query full audit trail.
  - [x] Add canonical failure semantics for unauthorized access (`AUTH_FORBIDDEN`) with user-safe messaging.
  - [x] Add denial logging event for rejected audit-trail access attempts.
- [x] Add repository query support for immutable chronological retrieval. (AC: 2)
  - [x] Add read method(s) to audit/operation-log repository abstraction for security event filtering.
  - [x] Ensure retrieval order is deterministic and chronological (UTC-based sort).
  - [x] Preserve append-only behavior: no edit/delete methods and no upsert path for audit records.
- [x] Expose audit-trail read in feature/UI surface for authorized roles. (AC: 2, 3)
  - [x] Add feature slice for audit-trail list/read interaction under role-gated navigation.
  - [x] Ensure unauthorized role attempts show safe denial message and do not reveal protected data.
  - [x] Keep UI logic thin; all access checks and audit logic remain in application services.
- [x] Add test coverage for immutability, access control, and ordering. (AC: 1, 2, 3)
  - [x] Unit tests: security-critical action paths append expected event types and canonical payload fields.
  - [x] Unit tests: unauthorized audit read returns `AUTH_FORBIDDEN` and logs denial attempt.
  - [x] Integration tests: audit entries are returned in chronological order and remain append-only.
  - [x] Integration tests: Owner/Admin can retrieve audit records; Cashier/Manager cannot.
  - [x] Update source-link entries in `POSOpen.Tests/POSOpen.Tests.csproj` for newly added non-UI source files.

## Dev Notes

### Story Intent

Story 1.5 consolidates governance-grade auditing for all security-critical actions in Epic 1. The core requirement is not just logging events, but enforcing an immutable, append-only audit trail with trusted actor context, UTC timestamps, deterministic retrieval order, and strict authorization on access.

### Current Repo Reality

- `IOperationLogRepository` and `OperationLogRepository` already provide append semantics and ordered listing.
- Staff lifecycle and role assignment use cases already append operation log events.
- Authorization primitives are already established via `IAuthorizationPolicyService`, `ICurrentSessionService`, and canonical `AUTH_FORBIDDEN` behavior.
- Story 1.4 implementation artifact is not yet present in `_bmad-output/implementation-artifacts`; this story must align with the governed-override contracts from epics and architecture and should integrate directly when 1.4 code lands.

### Architecture Compliance

- Keep strict layering: Presentation -> Application -> Infrastructure.
- Keep authorization checks in application use cases, not only in UI visibility.
- Preserve immutable append-only audit behavior in persistence and contracts.
- Keep all timestamps UTC and serialized with existing shared serializer conventions.
- Reuse existing operation context (`OperationContext`) and result envelope (`AppResult<TPayload>`) patterns.

### Security and Governance Guardrails

- Never trust UI payloads for actor identity; resolve actor from trusted current-session context.
- Do not expose mutable operations (edit/delete) for audit data.
- Do not expose sensitive diagnostics in user-facing denial messages.
- Denied access to audit data must itself be auditable.
- Keep event type names stable and past-tense to preserve traceability conventions.

### Audit Event Scope for Story 1.5

Security-critical scope for this story must include:

- Staff account create/update/deactivate actions.
- Staff role assignment/change actions.
- Override actions requiring reason capture (from Story 1.4 contracts).
- Elevation attempts/approvals/denials where applicable.

### Canonical Error Codes (Story 1.5)

- `AUTH_FORBIDDEN`: Caller is not authorized to read security audit trail.
- `AUDIT_TRAIL_UNAVAILABLE`: Audit retrieval failed due to transient/system issue.
- `AUDIT_EVENT_INVALID`: Audit event payload/contract validation failed before append.

### File Structure Requirements

Expected additions/updates for this story:

- `POSOpen/Application/Abstractions/Repositories/IOperationLogRepository.cs` (if filtered query contract is expanded)
- `POSOpen/Application/UseCases/Security/*` or `POSOpen/Application/UseCases/StaffManagement/*` (audit query use case)
- `POSOpen/Infrastructure/Persistence/Repositories/OperationLogRepository.cs`
- `POSOpen/Features/*` (audit list surface and role-gated navigation entry)
- `POSOpen/AppShell.xaml` and `POSOpen/AppShell.xaml.cs` (if route visibility is extended)
- `POSOpen.Tests/Unit/Security/*`
- `POSOpen.Tests/Integration/*` (audit ordering/access tests)
- `POSOpen.Tests/POSOpen.Tests.csproj` (source-link updates)

### Testing Requirements

- Validate append behavior for each security-critical event type.
- Validate audit payload fields include actor, action type, target reference, and UTC occurrence time.
- Validate chronological ordering contract under multiple appended events.
- Validate unauthorized read is denied and denial is logged.
- Validate no mutable API path exists for audit records.

### Acceptance Test Matrix

- Owner creates/updates/deactivates staff account -> corresponding immutable audit entry appended.
- Owner/Admin assigns role -> `StaffRoleAssigned` audit entry appended with trusted actor context.
- Manager/Cashier requests audit trail -> denied with safe message and denial event logged.
- Owner/Admin requests audit trail -> records returned in chronological order.
- Any attempt to mutate existing audit records -> unsupported by contract and rejected by design.

### Previous Story Intelligence

From recent Epic 1 implementation work:

- Continue relying on `IOperationLogRepository.AppendAsync(...)` rather than introducing a parallel audit store.
- Preserve canonical authorization messaging (`You do not have access to this action.`) and `AUTH_FORBIDDEN` failure code.
- Continue source-linked test project pattern in `POSOpen.Tests.csproj`; do not add MAUI project reference.
- Keep session authority derived from `ICurrentSessionService` across all protected actions.

### Git Intelligence Summary

Recent completed work in Epic 1 (stories 1.1 through 1.3) establishes the concrete patterns this story should extend: staff lifecycle use cases, role assignment enforcement, trusted-session authorization, and operation-log append behavior. Story 1.5 should extend those patterns instead of creating new parallel abstractions.

### Project Structure Notes

- Story artifacts are tracked in `_bmad-output/implementation-artifacts`.
- No dedicated `project-context.md` file was detected; architecture and implementation artifacts remain the source of truth.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story-1.5-Immutable-Audit-Trail-for-Security-Critical-Actions]
- [Source: _bmad-output/planning-artifacts/architecture.md#Core-Architectural-Decisions]
- [Source: _bmad-output/planning-artifacts/architecture.md#Implementation-Patterns--Consistency-Rules]
- [Source: _bmad-output/planning-artifacts/architecture.md#Requirements-to-Structure-Mapping]
- [Source: _bmad-output/implementation-artifacts/1-3-terminal-authentication-flow.md]
- [Source: POSOpen/Application/Abstractions/Repositories/IOperationLogRepository.cs]
- [Source: POSOpen/Infrastructure/Persistence/Repositories/OperationLogRepository.cs]
- [Source: POSOpen/Infrastructure/Persistence/Configurations/OperationLogEntryConfiguration.cs]
- [Source: POSOpen/Application/UseCases/StaffManagement/AssignStaffRoleUseCase.cs]
- [Source: POSOpen/Application/UseCases/StaffManagement/CreateStaffAccountUseCase.cs]
- [Source: POSOpen/Application/UseCases/StaffManagement/UpdateStaffAccountUseCase.cs]
- [Source: POSOpen/Application/UseCases/StaffManagement/DeactivateStaffAccountUseCase.cs]

## Dev Agent Record

### Agent Model Used

GPT-5.3-Codex

### Debug Log References

- `git log --oneline -n 8`
- `rg "IOperationLogRepository|OperationLogRepository|AUTH_FORBIDDEN|StaffRoleAssigned|StaffAccountCreated|StaffAccountUpdated|StaffAccountDeactivated" POSOpen/**/*.cs`

### Completion Notes List

- Implemented canonical immutable audit event constants and security-critical scope in application layer.
- Added authorized security-audit query use case with session validation, permission gating, denial-event logging, and failure constants.
- Extended operation-log repository contract and EF implementation with event-type filtered chronological retrieval.
- Standardized payload fields for staff account create/update/deactivate operation-log events to include actor and target context.
- Added Security Audit Trail UI surface (view model, page, route registration, and shell role gating for Owner/Admin).
- Added focused unit and integration test suites for ordering, scope filtering, authorization, denial logging, and append-only behavior.
- Executed full test project successfully: 74 passed, 0 failed, 0 skipped.

### File List

- _bmad-output/implementation-artifacts/1-5-immutable-audit-trail-for-security-critical-actions.md
- POSOpen/Application/Security/SecurityAuditEventTypes.cs
- POSOpen/Application/Security/RolePermissions.cs
- POSOpen/Application/Abstractions/Repositories/IOperationLogRepository.cs
- POSOpen/Application/UseCases/StaffManagement/CreateStaffAccountUseCase.cs
- POSOpen/Application/UseCases/StaffManagement/UpdateStaffAccountUseCase.cs
- POSOpen/Application/UseCases/StaffManagement/DeactivateStaffAccountUseCase.cs
- POSOpen/Application/UseCases/Security/SecurityAuditRecordDto.cs
- POSOpen/Application/UseCases/Security/ListSecurityAuditTrailConstants.cs
- POSOpen/Application/UseCases/Security/ListSecurityAuditTrailUseCase.cs
- POSOpen/Infrastructure/Persistence/Repositories/OperationLogRepository.cs
- POSOpen/Features/Security/SecurityRoutes.cs
- POSOpen/Features/Security/SecurityServiceCollectionExtensions.cs
- POSOpen/Features/Security/ViewModels/SecurityAuditViewModel.cs
- POSOpen/Features/Security/Views/SecurityAuditPage.xaml
- POSOpen/Features/Security/Views/SecurityAuditPage.xaml.cs
- POSOpen/AppShell.xaml
- POSOpen/AppShell.xaml.cs
- POSOpen.Tests/Unit/Security/ListSecurityAuditTrailUseCaseTests.cs
- POSOpen.Tests/Integration/Security/ListSecurityAuditTrailIntegrationTests.cs
- POSOpen.Tests/Unit/Security/AuthenticateStaffUseCaseTests.cs
- POSOpen.Tests/Unit/StaffManagement/CreateStaffAccountUseCaseTests.cs
- POSOpen.Tests/Unit/StaffManagement/UpdateStaffAccountUseCaseTests.cs
- POSOpen.Tests/Unit/StaffManagement/DeactivateStaffAccountUseCaseTests.cs
- POSOpen.Tests/Unit/StaffManagement/AssignStaffRoleUseCaseTests.cs

# Story 1.2: Role Assignment and Enforcement

Status: done

## Story

As an Owner/Admin,
I want to assign and update role permissions for staff users,
So that each user can only access allowed operational capabilities.

## Acceptance Criteria

**Given** I am an authenticated Owner/Admin
**When** I assign a role to a staff account
**Then** role mapping is persisted
**And** the assigned permissions are effective at session refresh.

**Given** a Cashier account
**When** the user attempts Manager-only actions
**Then** access is denied
**And** a user-safe authorization message is shown.

**Given** role permissions are updated
**When** the affected user signs in again
**Then** visible navigation/actions match updated role policy
**And** stale permissions are not applied.

## Tasks / Subtasks

- [x] Introduce application-layer role policy abstractions and models. (AC: 1, 2, 3)
  - [x] Create `POSOpen/Application/Abstractions/Security/IAuthorizationPolicyService.cs` with methods to evaluate action access by `StaffRole`.
  - [x] Create `POSOpen/Application/Abstractions/Security/ICurrentSessionService.cs` to expose signed-in staff id, role, and a session version or refresh token.
  - [x] Create `POSOpen/Application/Security/RolePermissions.cs` to define canonical permission keys and role mapping.
- [x] Implement role-assignment use case and audit logging. (AC: 1)
  - [x] Create `POSOpen/Application/UseCases/StaffManagement/AssignStaffRoleCommand.cs`.
  - [x] Create `POSOpen/Application/UseCases/StaffManagement/AssignStaffRoleUseCase.cs`.
  - [x] Resolve actor identity and role from `ICurrentSessionService` only; do not trust UI-supplied role or actor claims in command payload.
  - [x] Validate actor role is Owner or Admin before mutation; return failure code `AUTH_FORBIDDEN` when unauthorized.
  - [x] Persist role updates through `IStaffAccountRepository.UpdateAsync`.
  - [x] Emit immutable operation log event `StaffRoleAssigned` with required payload contract fields.
- [x] Implement authorization policy service in infrastructure. (AC: 2, 3)
  - [x] Create `POSOpen/Infrastructure/Security/AuthorizationPolicyService.cs` implementing role-to-permission checks.
  - [x] Register security services in DI from `MauiProgram` or an infrastructure extension method.
  - [x] Ensure policy checks happen in application use-cases, not only in UI/viewmodel code.
- [x] Add session refresh behavior so role changes are applied at next sign-in/refresh. (AC: 1, 3)
  - [x] Extend app/session state to include current role and session version.
  - [x] Ensure role changes do not alter currently active stale session permissions until re-authentication or explicit session refresh.
  - [x] Add a deterministic role refresh path called at sign-in and explicit session refresh.
- [x] Enforce manager-only action denial for Cashier and show safe message. (AC: 2)
  - [x] Add one explicit manager-only action surface in Shell area if one does not already exist.
  - [x] Guard that action with policy checks.
  - [x] Display a user-safe error message, e.g., "You do not have access to this action." with no diagnostics or role internals.
- [x] Apply role-aware navigation visibility for signed-in user role. (AC: 3)
  - [x] Update shell route or feature entry-point visibility to honor current role policy.
  - [x] Ensure Owner/Admin-only staff management entry points are hidden or disabled for Cashier.
  - [x] Ensure manager-only actions are visible for Manager, Owner, and Admin according to policy.
- [x] Add tests for assignment and enforcement. (AC: 1, 2, 3)
  - [x] Unit tests for `AssignStaffRoleUseCase`: authorized assignment success, unauthorized actor blocked, staff-not-found handling, no-op role update behavior.
  - [x] Unit tests for authorization service policy matrix: Owner/Admin/Manager/Cashier against defined permissions.
  - [x] Unit tests for session refresh behavior to prove stale role is not reused after re-sign-in.
  - [x] Integration tests validating persisted role changes are reflected in subsequent session load and protected action checks.
  - [x] Link any new non-UI source files in `POSOpen.Tests/POSOpen.Tests.csproj` using existing source-link pattern.

### Review Findings

- [x] [Review][Patch] Privileged-by-default app state creates a synthetic authenticated Owner session at startup [POSOpen/Infrastructure/Services/AppStateService.cs:8]
- [x] [Review][Patch] Manager operations route/page lacks authorization enforcement at the actual navigation boundary, so direct route navigation can bypass UI gating [POSOpen/Features/Shell/Views/ManagerOperationsPage.xaml.cs:5]
- [x] [Review][Patch] Shell visibility applies role permissions even when the permission snapshot is stale, which conflicts with the story rule that stale permissions must not be applied [POSOpen/AppShell.xaml.cs:25]
- [x] [Review][Patch] Edit staff save is non-atomic across profile update and role assignment, allowing partial success and preserving `Guid.Empty` as the update actor on the base update path [POSOpen/Features/StaffManagement/ViewModels/EditStaffAccountViewModel.cs:101]

## Dev Notes

### Story Intent

This story establishes the first explicit RBAC enforcement slice for POSOpen. Story 1.1 created and persisted roles on staff accounts; Story 1.2 now formalizes permission policy, role assignment behavior, and runtime enforcement so protected actions are blocked reliably and predictably.

### Current Repo Reality

- `StaffRole` already exists with values: Owner, Admin, Manager, Cashier.
- Staff role is already persisted on `StaffAccount` and can be changed via existing update flow.
- Shell currently exposes a staff route without role-aware gating.
- There is no dedicated authorization policy abstraction yet.
- There is no explicit current-session abstraction carrying signed-in role state yet.

### Scope Clarification for AC2 (Manager-only action)

To make AC2 testable in this sprint slice, implement or designate one explicit manager-only action path in the shell/features layer and enforce policy at application boundary. If no manager action exists yet, add a lightweight placeholder manager action entry point specifically for policy verification.

### Architecture Compliance

- Enforce policy in application services/use-cases, not UI-only.
- Preserve layer boundaries: ViewModel -> UseCase -> Repository/Service.
- Keep immutable operation logging for role assignment events.
- Use canonical `AppResult` failure codes and user-safe messaging.
- Persist UTC timestamps using existing operation context conventions.

### Security and Authorization Guardrails

- Resolve actor authority from trusted current-session context only (`ICurrentSessionService`), never from UI command payload role fields.
- Never expose sensitive authorization diagnostics to frontline users.
- Avoid hardcoding role checks across many viewmodels; centralize in a policy service.
- Keep permission keys stable and explicit so future stories can expand safely.
- Favor deny-by-default behavior for unknown action keys.

### Canonical Permission Matrix

Permission keys for this story:

- `staff.role.assign`
- `manager.operations.view`
- `manager.operations.execute`
- `staff.management.view`

Role mapping contract:

- Owner: `staff.role.assign`, `manager.operations.view`, `manager.operations.execute`, `staff.management.view`
- Admin: `staff.role.assign`, `manager.operations.view`, `manager.operations.execute`, `staff.management.view`
- Manager: `manager.operations.view`, `manager.operations.execute`
- Cashier: none of the above

Policy behavior:

- Unknown permission keys are denied by default.
- UI visibility follows this matrix, but enforcement must be performed in application-layer policy checks.

### Session Refresh Contract

- Permission snapshot is loaded at sign-in from persisted role and policy map.
- Role changes applied by Owner/Admin do not mutate currently active session permissions.
- Updated permissions become effective only on next sign-in or explicit session refresh action.
- Session version increments on successful role assignment and must be compared during refresh to invalidate stale snapshots.

### Story-Specific Error Codes

- `AUTH_FORBIDDEN`: Actor lacks permission to perform requested action.
- `AUTH_POLICY_MISSING`: Required permission mapping is unavailable or invalid.
- `STAFF_NOT_FOUND`: Target staff account does not exist.
- `STAFF_ROLE_NO_CHANGE`: Requested role equals current role.

### Operation Log Payload Contract (StaffRoleAssigned)

Required payload fields:

- `staffAccountId`
- `previousRole`
- `newRole`
- `changedByStaffId`
- `operationId`
- `occurredUtc`

### File Structure Requirements

Expected additions for this story:

- `POSOpen/Application/Abstractions/Security/*`
- `POSOpen/Application/UseCases/StaffManagement/AssignStaffRole*`
- `POSOpen/Infrastructure/Security/AuthorizationPolicyService.cs`
- Shell or feature-level manager action entry point and route guard updates
- `POSOpen/AppShell.xaml`
- `POSOpen/AppShell.xaml.cs`
- `POSOpen/Features/StaffManagement/StaffManagementRoutes.cs`
- `POSOpen/Application/Abstractions/Services/IAppStateService.cs`
- `POSOpen/Infrastructure/Services/AppStateService.cs`
- `POSOpen.Tests/Unit/Security/*`
- `POSOpen.Tests/Unit/StaffManagement/*` additions
- `POSOpen.Tests/Integration/StaffManagement/*` additions

### Testing Requirements

- Cover authorized and unauthorized role assignment paths.
- Cover policy matrix decisions for protected actions.
- Cover session refresh semantics after role change.
- Cover user-safe authorization messaging behavior in viewmodel/UI integration boundaries.

### Acceptance Test Matrix

- Owner assigns role: success; role persisted; `StaffRoleAssigned` audit event appended.
- Admin assigns role: success; role persisted; `StaffRoleAssigned` audit event appended.
- Manager attempts role assignment: denied with `AUTH_FORBIDDEN` and user-safe message.
- Cashier attempts manager-only action: denied with `AUTH_FORBIDDEN` and user-safe message.
- Role updated while target user is signed in: existing session remains on previous permission snapshot.
- Target user re-signs in (or explicit refresh is triggered): updated permission snapshot is applied.

### Previous Story Intelligence (1.1)

- Keep source-linked test project pattern; do not add project reference from tests to MAUI app project.
- Continue using operation-log append-only events for all staff-account mutations.
- Reuse existing `StaffAccount` persistence and DTO mapping patterns.
- Preserve existing naming and folder conventions introduced in story 1.1.

### Git Intelligence Summary

Recent commits show story 1.1 implementation and review hardening completed. Story 1.2 should extend that foundation rather than introducing parallel role-handling paths.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story-1.2-Role-Assignment-and-Enforcement]
- [Source: _bmad-output/planning-artifacts/architecture.md#Authentication--Security]
- [Source: _bmad-output/planning-artifacts/architecture.md#Frontend-Architecture]
- [Source: _bmad-output/planning-artifacts/architecture.md#Project-Structure--Boundaries]
- [Source: _bmad-output/planning-artifacts/architecture.md#Requirements-to-Structure-Mapping]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Navigation-Patterns]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Step-13-Responsive-Design--Accessibility]
- [Source: _bmad-output/implementation-artifacts/1-1-staff-account-management.md]
- [Source: POSOpen/Features/StaffManagement/StaffManagementRoutes.cs]
- [Source: POSOpen/AppShell.xaml]

## Dev Agent Record

### Agent Model Used

GPT-5.3-Codex

### Debug Log References

- `git log --oneline -n 5`
- `dotnet test POSOpen.Tests/POSOpen.Tests.csproj --framework net10.0-windows10.0.19041.0 --list-tests`
- `dotnet test POSOpen.Tests/POSOpen.Tests.csproj --framework net10.0-windows10.0.19041.0 -v minimal`
- `dotnet build POSOpen.sln -c Debug -v:q`

### Completion Notes List

- Implemented dedicated RBAC abstractions and canonical permission matrix (`staff.role.assign`, manager operations, staff management view).
- Added `AssignStaffRoleUseCase` with trusted-session authorization checks and immutable `StaffRoleAssigned` operation logging.
- Introduced session snapshot/version semantics and stale-permission protection via `IAppStateService` + `AppStateCurrentSessionService`.
- Added manager-only protected operation use case and home-screen user-safe denial messaging (`You do not have access to this action.`).
- Applied role-aware shell navigation visibility for Staff and Manager entries.
- Updated edit staff flow to use dedicated role-assignment path when role changes.
- Added unit and integration tests for role assignment, permission matrix, stale snapshot behavior, and protected action enforcement.
- Verified suite passing: 39/39 tests.

### File List

- POSOpen/Application/Abstractions/Security/IAuthorizationPolicyService.cs
- POSOpen/Application/Abstractions/Security/ICurrentSessionService.cs
- POSOpen/Application/Abstractions/Services/IAppStateService.cs
- POSOpen/Application/Security/CurrentSession.cs
- POSOpen/Application/Security/RolePermissions.cs
- POSOpen/Application/UseCases/Shell/ExecuteManagerOperationUseCase.cs
- POSOpen/Application/UseCases/StaffManagement/AssignStaffRoleCommand.cs
- POSOpen/Application/UseCases/StaffManagement/AssignStaffRoleUseCase.cs
- POSOpen/Application/UseCases/StaffManagement/UpdateStaffAccountUseCase.cs
- POSOpen/AppShell.xaml
- POSOpen/AppShell.xaml.cs
- POSOpen/Features/Shell/ShellRoutes.cs
- POSOpen/Features/Shell/ViewModels/HomeViewModel.cs
- POSOpen/Features/Shell/ViewModels/ManagerOperationsViewModel.cs
- POSOpen/Features/Shell/Views/HomePage.xaml
- POSOpen/Features/Shell/Views/ManagerOperationsPage.xaml
- POSOpen/Features/Shell/Views/ManagerOperationsPage.xaml.cs
- POSOpen/Features/StaffManagement/StaffManagementServiceCollectionExtensions.cs
- POSOpen/Features/StaffManagement/ViewModels/EditStaffAccountViewModel.cs
- POSOpen/Infrastructure/Persistence/PersistenceServiceCollectionExtensions.cs
- POSOpen/Infrastructure/Security/AuthorizationPolicyService.cs
- POSOpen/Infrastructure/Services/AppStateCurrentSessionService.cs
- POSOpen/Infrastructure/Services/AppStateService.cs
- POSOpen/MauiProgram.cs
- POSOpen.Tests/Integration/StaffManagement/AssignStaffRoleIntegrationTests.cs
- POSOpen.Tests/POSOpen.Tests.csproj
- POSOpen.Tests/Unit/Security/AppStateCurrentSessionServiceTests.cs
- POSOpen.Tests/Unit/Security/AuthorizationPolicyServiceTests.cs
- POSOpen.Tests/Unit/Security/ExecuteManagerOperationUseCaseTests.cs
- POSOpen.Tests/Unit/StaffManagement/AssignStaffRoleUseCaseTests.cs
- POSOpen.Tests/Unit/StaffManagement/UpdateStaffAccountUseCaseTests.cs
- _bmad-output/implementation-artifacts/1-2-role-assignment-and-enforcement.md

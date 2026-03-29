# Story 1.3: Terminal Authentication Flow

Status: review

## Story

As a staff user,
I want to authenticate on the facility terminal,
So that I can access my role-appropriate workspace securely.

## Acceptance Criteria

**Given** a valid active staff account  
**When** valid credentials are submitted  
**Then** authentication succeeds  
**And** a role-scoped session is established.

**Given** invalid credentials  
**When** sign-in is attempted  
**Then** authentication fails  
**And** non-revealing error messaging is shown.

**Given** an inactive account  
**When** sign-in is attempted  
**Then** access is denied  
**And** no session is created.

**Given** authentication succeeds  
**When** the role-mode home view is loaded  
**Then** the view is fully rendered and interactive within 3 seconds on the target device (NFR2).

## Tasks / Subtasks

- [x] Add application-level authentication contract and models. (AC: 1, 2, 3, 4)
  - [x] Create `POSOpen/Application/UseCases/Authentication/AuthenticateStaffCommand.cs`.
  - [x] Create `POSOpen/Application/UseCases/Authentication/AuthenticationResultDto.cs` (staff id, role, session version, next route).
  - [x] Define canonical auth failure codes and user-safe messages for this story.
- [x] Extend repository capabilities needed for sign-in decisioning. (AC: 1, 2, 3)
  - [x] Add repository method for normalized email lookup in auth path if existing `GetByEmailAsync` semantics are insufficient.
  - [x] Persist failed-attempt increments and lockout metadata updates atomically with account update.
  - [x] Keep email normalization at repository boundary (`Trim().ToLowerInvariant()`).
  - [x] Enforce lockout policy contract: lock account for 15 minutes after 5 consecutive failed attempts.
- [x] Implement `AuthenticateStaffUseCase` with policy-safe flow. (AC: 1, 2, 3)
  - [x] Resolve account by normalized email.
  - [x] Deny when account is missing or password is invalid using non-revealing message.
  - [x] Deny when account status is inactive and ensure no session creation.
  - [x] Verify password using `IPasswordHasher.Verify` and constant-time comparison path already in infrastructure.
  - [x] On success: reset failed-attempt counters, establish session via app-state service, and return role-appropriate workspace route.
  - [x] Append immutable auth operation-log events for success and failure outcomes with UTC timestamps.
- [x] Add terminal sign-in UI flow using CommunityToolkit.MVVM patterns. (AC: 1, 2, 3, 4)
  - [x] Create `POSOpen/Features/Authentication/ViewModels/SignInViewModel.cs` with command-driven state (`Idle -> Loading -> Success|Error`).
  - [x] Create `POSOpen/Features/Authentication/Views/SignInPage.xaml` and code-behind for binding setup only.
  - [x] Add route constants in an authentication routes file and register routes in app startup.
  - [x] Ensure app starts at sign-in when there is no authenticated session.
  - [x] On successful sign-in, navigate to role-appropriate home workspace and clear any stale auth error.
- [x] Enforce non-revealing and UX-consistent feedback semantics. (AC: 2, 3)
  - [x] Invalid credentials, inactive account, and lockout paths must not leak whether the email exists.
  - [x] Use actionable but safe copy (for example: "Sign-in failed. Check credentials or contact a manager.").
  - [x] Preserve entered email on failure; never clear user-entered values except password field if required by policy.
- [x] Meet NFR2 role-home load target with observable timing. (AC: 4)
  - [x] Capture sign-in start timestamp at credential submit command execution.
  - [x] Capture home-ready timestamp when the role home view model completes initial refresh and the first interactive frame is rendered.
  - [x] Verify measured time is <= 3 seconds for standard target device profile in local validation runs.
  - [x] Provide fallback safe message if the role-home load cannot be completed.
- [x] Add tests for authentication and session establishment. (AC: 1, 2, 3, 4)
  - [x] Unit tests for `AuthenticateStaffUseCase`: valid sign-in, invalid password, unknown email, inactive account, lockout path.
  - [x] Unit tests for app-state/session transitions: session created only on successful sign-in; no session on denied cases.
  - [x] Integration tests for repository/auth persistence updates (failed attempts increment/reset, lockout metadata behavior).
  - [x] Update `POSOpen.Tests/POSOpen.Tests.csproj` source-link entries to include new authentication use-case sources.

## Dev Notes

### Story Intent

This story introduces first-class terminal sign-in so staff can establish a trusted role-scoped session before entering operational workflows. It must integrate with the role-enforcement and stale-permission protections delivered in story 1.2, without regressing existing shell gating behavior.

### Current Repo Reality

- `StaffAccount` already includes `Status`, `FailedLoginAttempts`, and `LockedUntilUtc` fields provisioned for auth behavior.
- Password hashing/verification already exists via `IPasswordHasher` and `Pbkdf2PasswordHasher`.
- App state/session primitives already exist (`IAppStateService`, `ICurrentSessionService`, `CurrentSession`) and are used by shell visibility and manager-operation checks.
- No authentication feature, sign-in viewmodel, or sign-in route currently exists.
- Existing shell starts with mission-control routes and currently assumes authenticated session state is managed elsewhere.

### Architecture Compliance

- Keep strict layering: ViewModel -> UseCase -> Repository/Service -> Infrastructure.
- Keep authorization/authentication decisions in application services, not XAML/UI-only checks.
- Use canonical `AppResult<T>` envelope and user-safe messages.
- Keep immutable append-only operation logging for security-sensitive actions.
- Persist all timestamps as UTC through operation context patterns.

### Security and Authentication Guardrails

- Never reveal whether a specific email exists in the system.
- Never create or refresh a session on authentication failure.
- Do not expose password hash/salt in DTOs, logs, or UI bindings.
- Use normalized-email comparisons consistently at repository boundary.
- Do not bypass `IPasswordHasher` by introducing ad hoc credential checks.
- Keep session creation centralized through app-state/session abstractions.

### Session Establishment Contract

- Successful sign-in sets authenticated session with `staffId`, `role`, and current session version.
- Permission snapshot should be aligned to the new session version at sign-in.
- Failed sign-in leaves `IsAuthenticated == false` and does not mutate role-scoped visibility state.

### Lockout Policy Contract

- Consecutive failed attempts threshold: 5.
- Lockout duration: 15 minutes from last failed attempt.
- Lockout scope: per staff account.
- Successful sign-in resets `FailedLoginAttempts` to 0 and clears `LockedUntilUtc`.
- While lockout is active, sign-in returns denial using non-revealing messaging semantics.

### NFR2 Measurement Contract

- Start marker: timestamp captured when the sign-in submit command begins execution.
- End marker: timestamp captured when role-mode home is interactive (initial data loaded and primary actions enabled).
- Pass criteria: `end - start <= 3 seconds` on target facility terminal profile.
- Validation run must record both timestamps and computed duration in test or diagnostics output.

### Error Code Contract (Story 1.3)

- `AUTH_INVALID_CREDENTIALS`: Credentials rejected with non-revealing message.
- `AUTH_ACCOUNT_INACTIVE`: Account inactive; still surfaced to UI as non-revealing denial message.
- `AUTH_ACCOUNT_LOCKED`: Account temporarily locked due to policy.
- `AUTH_SIGNIN_UNAVAILABLE`: Sign-in could not complete due to transient system failure.

### File Structure Requirements

Expected additions for this story:

- `POSOpen/Application/UseCases/Authentication/*`
- `POSOpen/Features/Authentication/ViewModels/*`
- `POSOpen/Features/Authentication/Views/*`
- `POSOpen/Features/Authentication/*Routes*.cs`
- `POSOpen/MauiProgram.cs` updates for route/service registration
- `POSOpen/AppShell.xaml` and/or `POSOpen/AppShell.xaml.cs` updates for startup navigation behavior
- `POSOpen.Tests/Unit/Security/*` or `POSOpen.Tests/Unit/StaffManagement/*` auth-related tests
- `POSOpen.Tests/Integration/StaffManagement/*` or new auth integration test file
- `POSOpen.Tests/POSOpen.Tests.csproj` source-link updates for new application auth files

### Testing Requirements

- Verify AC1 success path creates session and returns role-scoped target route.
- Verify AC2 invalid credentials path returns non-revealing message and does not create session.
- Verify AC3 inactive account path denies access and does not create session.
- Verify AC4 sign-in-to-home path meets <=3 second interactivity target in local validation scenario.
- Verify operation-log events are appended for sign-in attempts with stable event payload shape.
- Verify lockout policy: 5 consecutive failures trigger 15-minute lockout, and successful post-lockout sign-in resets counters.

### Acceptance Test Matrix

- Active staff + valid password -> success, session created, role route resolved.
- Active staff + invalid password -> denied, non-revealing message, no session.
- Unknown email + any password -> denied, same non-revealing message, no session.
- Inactive staff + valid password -> denied, no session.
- 5 consecutive failures on active account -> lockout active for 15 minutes; attempts during lockout denied without account-disclosure messaging.
- Successful sign-in -> role-home view interactive within 3 seconds for target device profile.

### Previous Story Intelligence (1.2)

- Continue trusted-session pattern: actor/session authority comes from session services, not UI payloads.
- Preserve stale-permission protections in shell and use-case boundaries.
- Keep canonical permission keys centralized in `RolePermissions`.
- Reuse user-safe authorization wording style from story 1.2.
- Preserve test-source-link pattern in test project (no direct MAUI app project reference).

### Project Structure Notes

- No `project-context.md` file was detected in the workspace; rely on architecture and story artifacts as source of truth.
- Story and sprint artifacts remain under `_bmad-output` paths.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story-1.3-Terminal-Authentication-Flow]
- [Source: _bmad-output/planning-artifacts/architecture.md#Authentication--Security]
- [Source: _bmad-output/planning-artifacts/architecture.md#Frontend-Architecture]
- [Source: _bmad-output/planning-artifacts/architecture.md#Requirements-to-Structure-Mapping]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Step-13-Responsive-Design--Accessibility]
- [Source: _bmad-output/implementation-artifacts/1-2-role-assignment-and-enforcement.md]
- [Source: POSOpen/Application/Abstractions/Repositories/IStaffAccountRepository.cs]
- [Source: POSOpen/Domain/Entities/StaffAccount.cs]
- [Source: POSOpen/Infrastructure/Services/AppStateService.cs]
- [Source: POSOpen/AppShell.xaml.cs]

## Dev Agent Record

### Agent Model Used

GPT-5.3-Codex

### Debug Log References

- `dotnet test POSOpen.Tests/POSOpen.Tests.csproj --framework net10.0-windows10.0.19041.0 -v minimal`
- `dotnet build POSOpen.sln -c Debug -v minimal`

### Completion Notes List

- Story context created with implementation guardrails for secure terminal authentication.
- Implemented authentication contracts and `AuthenticateStaffUseCase` with non-revealing failure semantics, lockout enforcement (5 attempts, 15 minutes), UTC operation-log auditing, and role-routed session establishment.
- Added authentication feature slice with `SignInViewModel` + `SignInPage`, app startup route registration, and shell startup behavior that routes unauthenticated users to sign-in.
- Added sign-in-to-home timing instrumentation via `IAuthenticationPerformanceTracker` with role-home interactivity markers and safe fallback messaging on home navigation failures.
- Added auth-focused unit and integration tests; validation passed with `45/45` tests.

### File List

- _bmad-output/implementation-artifacts/1-3-terminal-authentication-flow.md
- POSOpen/Application/Abstractions/Repositories/IStaffAccountRepository.cs
- POSOpen/Application/UseCases/Authentication/AuthenticateStaffCommand.cs
- POSOpen/Application/UseCases/Authentication/AuthenticateStaffUseCase.cs
- POSOpen/Application/UseCases/Authentication/AuthenticationConstants.cs
- POSOpen/Application/UseCases/Authentication/AuthenticationResultDto.cs
- POSOpen/AppShell.xaml
- POSOpen/AppShell.xaml.cs
- POSOpen/Features/Authentication/AuthenticationPerformanceTracker.cs
- POSOpen/Features/Authentication/AuthenticationRoutes.cs
- POSOpen/Features/Authentication/AuthenticationServiceCollectionExtensions.cs
- POSOpen/Features/Authentication/IAuthenticationPerformanceTracker.cs
- POSOpen/Features/Authentication/SignInPerformanceMeasurement.cs
- POSOpen/Features/Authentication/ViewModels/SignInViewModel.cs
- POSOpen/Features/Authentication/Views/SignInPage.xaml
- POSOpen/Features/Authentication/Views/SignInPage.xaml.cs
- POSOpen/Features/Shell/Views/HomePage.xaml.cs
- POSOpen/Features/Shell/Views/ManagerOperationsPage.xaml.cs
- POSOpen/Features/StaffManagement/Views/StaffListPage.xaml.cs
- POSOpen/Infrastructure/Persistence/Repositories/StaffAccountRepository.cs
- POSOpen/MauiProgram.cs
- POSOpen.Tests/Integration/StaffManagement/StaffAccountRepositoryTests.cs
- POSOpen.Tests/POSOpen.Tests.csproj
- POSOpen.Tests/Unit/Security/AuthenticateStaffUseCaseTests.cs
- POSOpen.Tests/Unit/StaffManagement/AssignStaffRoleUseCaseTests.cs
- POSOpen.Tests/Unit/StaffManagement/CreateStaffAccountUseCaseTests.cs
- POSOpen.Tests/Unit/StaffManagement/DeactivateStaffAccountUseCaseTests.cs
- POSOpen.Tests/Unit/StaffManagement/UpdateStaffAccountUseCaseTests.cs

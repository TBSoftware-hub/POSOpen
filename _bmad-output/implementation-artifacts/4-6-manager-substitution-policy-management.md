# Story 4.6: Manager Substitution Policy Management

## Metadata

| Field | Value |
|---|---|
| Epic | 4 - Party Booking Lifecycle and Inventory Coordination |
| Story | 4.6 |
| Key | `4-6-manager-substitution-policy-management` |
| Status | done |
| Author | Timbe (via BMAD Story Creator) |
| Created | 2026-04-02 |
| Target Sprint | Current |

---

## User Story

**As a** manager,  
**I want** to create, edit, and delete substitution policy rules for inventory-constrained items,  
**So that** cashiers and party coordinators have accurate, up-to-date substitute options available during transactions and bookings.

---

## Acceptance Criteria

### AC-1 - Managers can view all substitution rules

> **Given** I am an authenticated manager  
> **When** I navigate to substitution policy management  
> **Then** I can see existing rules including source item, allowed substitute item(s), applicable role(s), and active status.

### AC-2 - Managers can create valid substitution rules

> **Given** a constrained item requires a substitution rule  
> **When** I create a rule with source item, allowed substitute, and applicable roles  
> **Then** the rule is persisted  
> **And** it is immediately available to cashiers and coordinators for subsequent flows.

### AC-3 - Managers can edit substitution rules

> **Given** an existing substitution rule  
> **When** I edit allowed substitute, applicable roles, or active status  
> **Then** changes are saved and effective for subsequent transactions and bookings.

### AC-4 - Managers can delete substitution rules

> **Given** an existing substitution rule  
> **When** I delete it  
> **Then** the rule is removed  
> **And** that substitute is no longer offered in checkout or booking flows for that source item.

### AC-5 - Validation prevents invalid item references

> **Given** I submit a rule with source or substitute items that do not exist in inventory catalogs  
> **When** validation runs  
> **Then** a user-safe validation error is shown  
> **And** no rule is persisted.

### AC-6 - Policy changes are immutably audited

> **Given** substitution policy create/update/delete actions occur  
> **When** the action is confirmed  
> **Then** an immutable audit record is written with actor identity, timestamp, and change summary (NFR13).

---

## Scope

### In Scope

- Replace 4.5 seeded in-memory substitution policy internals with repository-backed persistence while preserving 4.5 `GetAllowedSubstitutesUseCase` contract behavior.
- Provide manager-facing policy management read and mutation use cases (list/create/update/delete).
- Enforce manager authorization in application layer for all policy commands and queries (list/create/update/delete).
- Validate source and substitute item IDs against existing inventory option catalogs before persistence.
- Preserve deterministic substitute ordering (`SourceOptionId`, then `AllowedSubstituteOptionId`) for downstream UI/test stability.
- Record immutable audit events for policy create/update/delete.
- Add focused manager UI for policy management aligned with existing role-aware UX patterns.
- Add unit and integration tests for CRUD, authorization, validation, and downstream substitute visibility.

### Out of Scope

- Dynamic inventory level forecasting or supplier integrations.
- Multi-location policy inheritance/override model.
- Bulk CSV import/export for policy administration.
- Advanced policy conflict resolution engine beyond deterministic validation rules.
- Non-manager policy self-service for cashier or party coordinator roles.

---

## Context

Story 4.5 introduced reservation/release behavior and implemented substitution reads through `IInventorySubstitutionPolicyProvider` backed by `SeededInventorySubstitutionPolicyProvider`. Story 4.6 is the ownership and governance layer that turns substitution policy into manager-controlled data instead of hardcoded defaults.

This story must preserve all 4.5 read contracts and consumer integrations so that:

- `GetAllowedSubstitutesUseCase` remains the integration point for party finalization and booking detail guidance.
- Deterministic ordering and role filtering remain stable.
- Existing inventory/finalization enforcement behavior in Story 4.5 remains unchanged except for the provider backing store.

---

## Current Repo Reality

### Existing policy read path (from Story 4.5)

- `POSOpen/Application/Abstractions/Services/IInventorySubstitutionPolicyProvider.cs`
- `POSOpen/Application/UseCases/Inventory/GetAllowedSubstitutesUseCase.cs`
- `POSOpen/Application/UseCases/Inventory/InventoryPolicyDtos.cs`
- `POSOpen/Infrastructure/Services/SeededInventorySubstitutionPolicyProvider.cs`
- `POSOpen/Infrastructure/Persistence/PersistenceServiceCollectionExtensions.cs` (singleton registration)

### Existing Party integrations that consume substitution rules

- `POSOpen/Application/UseCases/Party/MarkPartyBookingCompletedUseCase.cs`
- `POSOpen/Features/Party/ViewModels/PartyBookingDetailViewModel.cs`
- `POSOpen/Features/Party/PartyServiceCollectionExtensions.cs`

### Existing catalog and safe-message baseline

- `POSOpen/Application/UseCases/Party/PartyBookingConstants.cs` contains canonical option IDs, display names, and inventory-related message/error constants.

### Existing immutable audit infrastructure

- `POSOpen/Application/Abstractions/Repositories/IOperationLogRepository.cs`
- `POSOpen/Application/Security/SecurityAuditEventTypes.cs`
- `POSOpen/Domain/Entities/OperationLogEntry.cs`

---

## Previous Story Intelligence (4.5)

- 4.5 explicitly required seeded policy internals to be temporary and replaceable in 4.6 without changing command/query contracts.
- Role-filtered substitute behavior and stable ordering are already asserted in `POSOpen.Tests/Unit/Inventory/GetAllowedSubstitutesUseCaseTests.cs` and must remain true after persistence migration.
- Inventory constraint UX currently surfaces substitutes inline within Party detail and finalization flows; 4.6 must not regress that path.
- Existing guardrails across Epic 4 remain mandatory: `AppResult<T>` envelope, operation context propagation, UTC timestamps, and transaction-safe writes.

---

## Architecture Compliance Guardrails

- Preserve strict layering: Presentation -> Application -> Infrastructure. No direct Presentation -> Infrastructure calls.
- Keep authorization checks in application/service layer, not UI-only.
- Keep `GetAllowedSubstitutesUseCase` API contract stable for 4.5 consumers.
- Keep write operations transaction-safe and idempotent via operation IDs.
- Persist all mutation timestamps in UTC.
- Keep audit records append-only and immutable.
- Keep feature-first organization and avoid scattering policy logic across unrelated features.

---

## Domain and Data Model Guidance

### Persistent policy model (minimum)

Add a persistence-backed policy entity (domain name can vary, but avoid conflicting with existing DTO names):

- `Id` (Guid)
- `SourceOptionId` (string)
- `AllowedSubstituteOptionId` (string)
- `AllowedRoles` (serialized role set or normalized child rows)
- `IsActive` (bool)
- `CreatedAtUtc` (DateTime)
- `UpdatedAtUtc` (DateTime)
- `CreatedByStaffId` (Guid)
- `UpdatedByStaffId` (Guid)
- `LastOperationId` (Guid)

### Validation rules (minimum)

- `SourceOptionId` must exist in known add-on catalogs.
- `AllowedSubstituteOptionId` must exist in known add-on catalogs.
- Source and substitute must not be the same item.
- Duplicate active rule combinations are not allowed for the same source/substitute/role set.
- At least one allowed role is required.

### Contract compatibility rule

Repository-backed provider output must continue to map to `InventorySubstitutionPolicyRule` with deterministic ordering and role filtering semantics matching 4.5.

---

## Use Case Contracts

### List policies

- Query: `GetInventorySubstitutionPoliciesQuery`
- Requires manager authorization in the application layer before returning policy data.
- Result includes policy ID, source/substitute IDs, display names, role set, active status, and updated metadata.

### Create policy

- Command: `CreateInventorySubstitutionPolicyCommand`
- Requires manager authorization and operation context.
- Validates catalog references before save.
- Writes immutable audit event summary.

### Update policy

- Command: `UpdateInventorySubstitutionPolicyCommand`
- Requires manager authorization and operation context.
- Supports updating substitute, roles, and active state.
- Idempotent on replayed operation ID.
- Writes immutable audit event summary.

### Delete policy

- Command: `DeleteInventorySubstitutionPolicyCommand`
- Requires manager authorization and operation context.
- Uses canonical soft delete semantics by setting `IsActive = false`; policy rows are retained for history and audit correlation.
- Ensure deleted rule is no longer returned by `GetAllowedSubstitutesUseCase` reads.
- Writes immutable audit event summary.

---

## File Structure Requirements

### Expected application-layer additions

- `POSOpen/Application/UseCases/Inventory/GetInventorySubstitutionPoliciesUseCase.cs`
- `POSOpen/Application/UseCases/Inventory/CreateInventorySubstitutionPolicyUseCase.cs`
- `POSOpen/Application/UseCases/Inventory/UpdateInventorySubstitutionPolicyUseCase.cs`
- `POSOpen/Application/UseCases/Inventory/DeleteInventorySubstitutionPolicyUseCase.cs`
- `POSOpen/Application/UseCases/Inventory/InventorySubstitutionPolicyManagementDtos.cs`

### Expected abstraction additions

- `POSOpen/Application/Abstractions/Repositories/IInventorySubstitutionPolicyRepository.cs`

### Expected infrastructure additions

- `POSOpen/Infrastructure/Persistence/Repositories/InventorySubstitutionPolicyRepository.cs`
- `POSOpen/Infrastructure/Persistence/Configurations/InventorySubstitutionPolicyConfiguration.cs`
- Migration for substitution policy table(s) and indexes
- Repository-backed implementation for `IInventorySubstitutionPolicyProvider`

### Expected UI additions

- New manager-facing policy management page/viewmodel under a consistent feature slice (prefer `Features/Inventory`).
- Register routes/DI in the relevant feature extension class(es).
- Wire policy CRUD actions to application-layer use cases only.

### Expected integration touchpoints

- `POSOpen/Infrastructure/Persistence/PersistenceServiceCollectionExtensions.cs` (swap seeded provider registration to repository-backed provider)
- `POSOpen/Application/Security/SecurityAuditEventTypes.cs` (add event type constants for policy CRUD)
- Keep `POSOpen/Application/UseCases/Inventory/GetAllowedSubstitutesUseCase.cs` contract stable.

---

## UX and Interaction Requirements

- Manager view must clearly show rule state at a glance (source, substitute, roles, active/inactive).
- Validation failures must be inline and non-destructive; never clear user-entered form state on validation failure.
- Keep interaction parity across tablet and desktop manager layouts.
- Provide explicit success/error feedback using existing semantic status patterns.
- Avoid deep modal chains for routine edits; prefer inline or side-panel edit flows with clear primary actions.

---

## Testing Requirements

### Unit tests

- List query returns deterministic ordering and expected display names.
- List query enforces manager authorization and rejects non-manager roles.
- Create command rejects invalid source/substitute references.
- Create/update/delete enforce manager authorization.
- Update/delete are idempotent under replayed operation IDs.
- `GetAllowedSubstitutesUseCase` still returns role-filtered active substitutes after backing store replacement.

### Integration tests

- CRUD persistence roundtrip for policy rules via repository.
- Transaction behavior: no partial writes when validation or authorization fails.
- Deleted/inactive rules are excluded from downstream substitute read behavior.
- Audit entries are appended with actor, timestamp, and change summary for each mutation type.

### Regression focus

- Story 4.5 inventory reservation/release/finalization behavior remains intact.
- Story 4.4 add-on and risk flows remain intact.
- Existing party detail substitute guidance continues to work with no null-state regressions.

---

## Implementation Tasks / Subtasks

### Task 1 - Add substitution policy persistence model and repository (AC: 1, 2, 3, 4, 5)

- [x] Add persistent policy entity/configuration and migration.
- [x] Add repository abstraction and infrastructure implementation.
- [x] Add indexes/constraints for deterministic reads and duplicate prevention.

### Task 2 - Implement policy management use cases (AC: 1, 2, 3, 4, 5)

- [x] Implement list/query use case for manager policy grid.
- [x] Implement create use case with catalog and role validation.
- [x] Implement update use case with idempotent operation handling.
- [x] Implement delete use case and define deterministic delete semantics.

### Task 3 - Replace seeded provider internals with repository-backed reads (AC: 2, 3, 4)

- [x] Implement repository-backed `IInventorySubstitutionPolicyProvider`.
- [x] Swap DI registration from seeded provider to repository-backed provider.
- [x] Preserve 4.5 read contract and deterministic ordering behavior.

### Task 4 - Add authorization and immutable audit coverage (AC: 1, 6)

- [x] Enforce manager authorization in list and all policy mutations.
- [x] Add canonical audit event constants for policy created/updated/deleted.
- [x] Append immutable audit records including actor, UTC timestamp, and change summary.

### Task 5 - Build manager policy management UI (AC: 1, 2, 3, 4, 5)

- [x] Add manager policy list/edit/create/delete UI.
- [x] Implement inline validation and actionable feedback states.
- [x] Ensure manager-only route accessibility in role-aware navigation.

### Task 6 - Add tests and finalize integration wiring (AC: 1, 2, 3, 4, 5, 6)

- [x] Add/extend unit tests for use cases and provider behavior.
- [x] Add integration tests for repository + audit appends.
- [x] Ensure existing inventory and party test suites remain green.

---

## Definition of Done

- All acceptance criteria implemented and validated.
- Policy rules are manager-managed via persistence-backed CRUD.
- Downstream substitute reads consume the same contract with deterministic, role-filtered behavior.
- Invalid item references are blocked with user-safe validation feedback.
- Policy mutations are immutably audited with actor/timestamp/change summary.
- No regressions in Story 4.5 inventory behaviors and Story 4.4 booking detail flows.

---

## References

- `_bmad-output/planning-artifacts/epics.md` (Epic 4, Story 4.6)
- `_bmad-output/planning-artifacts/architecture.md` (layer boundaries, policy authorization, immutable audit, feature mapping)
- `_bmad-output/planning-artifacts/ux-design-specification.md` (role-aware manager UX, status-at-a-glance, inline validation/recovery)
- `_bmad-output/implementation-artifacts/4-5-reserve-and-release-inventory-by-booking-policy.md` (previous-story guardrails and provider replacement contract)

## Dev Agent Record

### Agent Model Used

GPT-5.3-Codex (GitHub Copilot)

### Debug Log References

- `dotnet ef migrations add AddInventorySubstitutionPolicies --framework net10.0-windows10.0.19041.0 --project POSOpen/POSOpen.csproj --startup-project POSOpen/POSOpen.csproj --output-dir Infrastructure/Persistence/Migrations`
- `runTests` targeted inventory unit/integration files: 12 passed, 0 failed.
- `runTests` full suite: 277 passed, 0 failed.

### Completion Notes List

- Added persistence-backed substitution policy model, EF configuration, and migration (`inventory_substitution_policies`) with deterministic and duplicate-prevention indexes.
- Added repository abstraction and EF repository implementation for list/create/update/find-duplicate/idempotency lookup operations.
- Replaced seeded substitution provider registration with repository-backed provider while preserving `GetAllowedSubstitutesUseCase` contract and deterministic ordering.
- Implemented manager-authorized policy management use cases for list/create/update/delete with catalog validation, self-reference prevention, duplicate-active-rule prevention, and operation-id idempotency for update/delete.
- Added immutable audit events for policy create/update/delete and expanded canonical event constants.
- Added manager-facing inventory substitution policy UI (feature route, service registration, page, and view model) with inline validation and role-aware access checks.
- Added/updated unit and integration tests for use case authorization/validation/idempotency, repository/provider behavior, and audit append coverage.

### File List

- POSOpen/Domain/Entities/InventorySubstitutionPolicy.cs
- POSOpen/Application/Abstractions/Repositories/IInventorySubstitutionPolicyRepository.cs
- POSOpen/Application/UseCases/Inventory/InventorySubstitutionPolicyManagementDtos.cs
- POSOpen/Application/UseCases/Inventory/InventorySubstitutionPolicyConstants.cs
- POSOpen/Application/UseCases/Inventory/InventorySubstitutionPolicyRoleCodec.cs
- POSOpen/Application/UseCases/Inventory/InventorySubstitutionPolicyAuthorization.cs
- POSOpen/Application/UseCases/Inventory/GetInventorySubstitutionPoliciesUseCase.cs
- POSOpen/Application/UseCases/Inventory/CreateInventorySubstitutionPolicyUseCase.cs
- POSOpen/Application/UseCases/Inventory/UpdateInventorySubstitutionPolicyUseCase.cs
- POSOpen/Application/UseCases/Inventory/DeleteInventorySubstitutionPolicyUseCase.cs
- POSOpen/Infrastructure/Persistence/Configurations/InventorySubstitutionPolicyConfiguration.cs
- POSOpen/Infrastructure/Persistence/Repositories/InventorySubstitutionPolicyRepository.cs
- POSOpen/Infrastructure/Services/RepositoryInventorySubstitutionPolicyProvider.cs
- POSOpen/Infrastructure/Persistence/PosOpenDbContext.cs
- POSOpen/Infrastructure/Persistence/PersistenceServiceCollectionExtensions.cs
- POSOpen/Application/Security/SecurityAuditEventTypes.cs
- POSOpen/Features/Inventory/InventoryRoutes.cs
- POSOpen/Features/Inventory/InventoryServiceCollectionExtensions.cs
- POSOpen/Features/Inventory/ViewModels/InventorySubstitutionPoliciesViewModel.cs
- POSOpen/Features/Inventory/Views/InventorySubstitutionPoliciesPage.xaml
- POSOpen/Features/Inventory/Views/InventorySubstitutionPoliciesPage.xaml.cs
- POSOpen/Features/Shell/ViewModels/ManagerOperationsViewModel.cs
- POSOpen/Features/Shell/Views/ManagerOperationsPage.xaml
- POSOpen/MauiProgram.cs
- POSOpen/Infrastructure/Persistence/Migrations/20260402185630_AddInventorySubstitutionPolicies.cs
- POSOpen/Infrastructure/Persistence/Migrations/20260402185630_AddInventorySubstitutionPolicies.Designer.cs
- POSOpen/Infrastructure/Persistence/Migrations/PosOpenDbContextModelSnapshot.cs
- POSOpen.Tests/Unit/Inventory/GetAllowedSubstitutesUseCaseTests.cs
- POSOpen.Tests/Unit/Inventory/InventorySubstitutionPolicyManagementUseCaseTests.cs
- POSOpen.Tests/Integration/Inventory/InventorySubstitutionPolicyManagementIntegrationTests.cs

### Change Log

- 2026-04-02: Implemented Story 4.6 manager substitution policy management end-to-end across persistence, application, security audit, provider replacement, manager UI, and tests.

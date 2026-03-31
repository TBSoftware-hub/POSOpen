# Story 3.5 - Policy-Bound Refund Workflow

## Metadata

| Field | Value |
|---|---|
| Epic | 3 - Mixed-Cart Checkout, Payments, and Device Execution |
| Story | 3.5 |
| Key | `3-5-policy-bound-refund-workflow` |
| Status | done |
| Author | Timbe (via BMAD Story Creator) |
| Created | 2026-03-31 |
| Target Sprint | Current |

---

## User Story

**As a** manager/cashier with permission,  
**I want** to process refunds within policy boundaries,  
**So that** corrections are possible without violating governance controls.

---

## Context

Stories 3.3 and 3.4 established checkout payment attempts, receipt workflows, operation IDs, transaction status, and post-completion checkout UI. Stories 1.4 and 1.5 established the repo's governance patterns for policy-bound actions and immutable audit logging.

This story extends those foundations by introducing a refund workflow that:

1. **Starts from trusted transaction context** using the completed checkout/cart session and its approved payment attempt.
2. **Presents role-appropriate refund paths** based on centralized permissions rather than UI-only logic.
3. **Requires reason capture and override escalation** when a refund path is outside the actor's direct authority.
4. **Records immutable audit evidence** for refund completion and denial/approval paths using the existing operation-log infrastructure.

The goal is to make refunds operationally possible while preserving governance, traceability, and deterministic behavior. This story should not invent a parallel security or audit framework; it should reuse the session, authorization, operation-log, and result-envelope patterns already present in the repo.

---

## Acceptance Criteria

### AC-1 - Present allowed refund paths by role/policy

> **Given** I have refund permission and eligible transaction context  
> **When** refund is initiated  
> **Then** allowed refund paths are presented per role/policy.

### AC-2 - Enforce approval/override reason capture

> **Given** refund requires approval/override  
> **When** approval reason is not provided  
> **Then** refund cannot finalize  
> **And** reason capture is enforced.

### AC-3 - Append immutable audit records on refund completion

> **Given** refund completes  
> **When** transaction state is updated  
> **Then** immutable audit records include actor, reason, operation ID, and UTC timestamp.

---

## Story Scope

### In Scope

- Refund initiation from an eligible completed checkout transaction/cart session.
- Policy-based refund path selection using centralized authorization checks.
- Mandatory reason capture for governed/approval-required refund paths.
- Immutable audit event append for refund completion and governed action traces.
- Deterministic user-safe result handling and diagnostic logging.
- Checkout-focused UI flow for reviewing transaction context and submitting a refund.

### Out of Scope

- Bulk refund processing across multiple transactions.
- Full support/incident investigation tooling for refund failures (Epic 7).
- End-of-day reconciliation reporting for refunds (Epic 6).
- Cloud sync/replay handling beyond operation ID traceability already in place.
- Advanced partial-refund policy matrices by item category, amount threshold, or location, unless they are already derivable from current role-policy rules.

---

## Current Repo Reality

- Checkout currently persists `CartSession`, `CartLineItem`, `CheckoutPaymentAttempt`, `ReceiptMetadata`, and `TransactionOperation`.
- Post-payment flow navigates from `PaymentCaptureViewModel` to `CheckoutCompletionViewModel`.
- Operation correlation exists via `IOperationIdService` and immutable event append exists via `IOperationLogRepository`.
- Policy-bound actions already rely on `ICurrentSessionService` and `IAuthorizationPolicyService`; UI claims are not trusted.
- Refund-specific permission keys, refund entities, and refund use cases do not yet exist.
- The repo already treats cart session ID as the transaction identifier for V1 checkout workflows.

---

## Architecture Guardrails

- **Layering remains strict:** `Features -> Application -> Infrastructure`; no page or view model may decide refund policy independently.
- **Policy enforcement stays server-side/application-side:** use `ICurrentSessionService` + `IAuthorizationPolicyService`, not route visibility alone.
- **Use `AppResult<T>` everywhere:** preserve canonical failure codes and user-safe messaging.
- **Reuse immutable audit infrastructure:** append refund events through `IOperationLogRepository`; do not create a second audit store.
- **Keep UTC + operation IDs:** refund events must carry stable operation/correlation context for later reconciliation.
- **Do not store raw card data:** refund payloads, logs, and persistence must remain PCI-minimized.
- **Prefer additive checkout extension:** integrate into existing checkout feature registration and navigation rather than creating a separate unrelated module.

---

## Policy Contract

Recommended V1 permission model for this story:

- `checkout.refund.initiate`
- `checkout.refund.approve`

Recommended default role mapping:

- Owner: initiate + approve
- Admin: initiate + approve
- Manager: initiate + approve
- Cashier: initiate only

Recommended refund-path behavior:

- If actor has `checkout.refund.initiate` and `checkout.refund.approve`, allow direct refund completion.
- If actor has `checkout.refund.initiate` but not `checkout.refund.approve`, allow governed submission path only and require approval/override reason before finalization.
- If actor lacks initiate permission, deny refund access with canonical forbidden messaging.

This model best matches the story wording "manager/cashier with permission" while staying compatible with the current centralized permission system.

### Eligibility Contract (Required)

`GetRefundEligibilityUseCase` MUST evaluate and return explicit eligibility outcomes using this minimum rule set:

- Transaction/cart session exists; otherwise return `REFUND_TARGET_NOT_FOUND`.
- Transaction has at least one approved payment attempt; otherwise return `REFUND_NOT_ELIGIBLE`.
- Transaction is not already fully refunded; otherwise return `REFUND_ALREADY_COMPLETED`.
- Requested amount is greater than zero and less than or equal to refundable balance; otherwise return `REFUND_AMOUNT_INVALID`.
- Refund path resolution is policy-driven (direct vs approval-required) based on current actor permissions.

The DTO result should expose, at minimum:

- `IsEligible`
- `EligibleAmountCents`
- `CurrencyCode`
- `AllowedPaths` (Direct / ApprovalRequired)
- `IneligibilityReasonCode` and user-safe message when blocked.

---

## Refund Audit Contract

Refund completion must reuse the immutable audit conventions introduced in Stories 1.4 and 1.5.

Canonical event types (required):

- `RefundInitiated`
- `RefundCompleted`
- `RefundDenied`
- `RefundApprovalRequested` (only if approval is modeled as a distinct completion step)

Required immutable payload fields for completion events:

- `actorStaffId`
- `actorRole`
- `targetReference`
- `reason`
- `operationId`
- `occurredUtc`
- `refundAmountCents`
- `currencyCode`

If a governed/approval path is used, the final immutable event must still contain the actor who finalized the refund and the reason that justified it.

---

## File Impact Plan

### Create - Application Checkout Contracts and Use Cases

1. `POSOpen/Application/UseCases/Checkout/GetRefundEligibilityUseCase.cs`
   - Loads completed transaction context and determines which refund paths are allowed for the current actor.
   - Returns transaction summary, refund amount, and allowed actions.

2. `POSOpen/Application/UseCases/Checkout/SubmitRefundCommand.cs`
   - Carries refund target reference, amount, reason, chosen refund path, and operation context.

3. `POSOpen/Application/UseCases/Checkout/SubmitRefundResultDto.cs`
   - Returns operation ID, refund status, actor, target transaction reference, and user-safe status copy.

4. `POSOpen/Application/UseCases/Checkout/RefundEligibilityDto.cs`
   - Shapes UI-facing eligibility data and allowed refund path list.

5. `POSOpen/Application/UseCases/Checkout/SubmitRefundUseCase.cs`
   - Performs policy checks, reason validation, refund persistence/update, and immutable audit append.
   - Must never trust actor identity from UI payloads.

6. `POSOpen/Application/UseCases/Checkout/RefundWorkflowConstants.cs`
   - Canonical error codes and safe messages for refund-denied, refund-reason-required, refund-target-invalid, and refund-failed cases.

### Create - Domain / Persistence Model

7. `POSOpen/Domain/Entities/RefundRecord.cs`
   - Refund persistence model for V1 refund outcomes tied to cart session / transaction reference.

8. `POSOpen/Domain/Enums/RefundStatus.cs`
   - Suggested values: `PendingApproval`, `Completed`, `Denied`, `Failed`.

9. `POSOpen/Domain/Enums/RefundPath.cs`
   - Suggested values: `Direct`, `ApprovalRequired`.

10. `POSOpen/Application/Abstractions/Repositories/IRefundRepository.cs`
    - Add/list/look-up refund records by transaction/cart session reference.

11. `POSOpen/Infrastructure/Persistence/Configurations/RefundRecordConfiguration.cs`
12. `POSOpen/Infrastructure/Persistence/Repositories/RefundRepository.cs`
13. `POSOpen/Infrastructure/Persistence/Migrations/<timestamp>_AddRefundRecords.cs`
14. `POSOpen/Infrastructure/Persistence/PosOpenDbContext.cs`
15. `POSOpen/Infrastructure/Persistence/PersistenceServiceCollectionExtensions.cs`

### Modify - Security / Permissions / Audit Integration

16. `POSOpen/Application/Security/RolePermissions.cs`
    - Add refund permission constants and role mappings.

17. `POSOpen/Application/Security/SecurityAuditEventTypes.cs`
    - Add stable refund event names if not already present.

18. Reuse `IOperationLogRepository` from existing audit infrastructure.
    - No new mutable audit repository should be introduced.

### Create / Modify - Checkout UI Surface

19. `POSOpen/Features/Checkout/ViewModels/RefundWorkflowViewModel.cs`
    - Loads eligibility data, captures reason, exposes allowed actions, and submits refund.

20. `POSOpen/Features/Checkout/Views/RefundWorkflowPage.xaml`
21. `POSOpen/Features/Checkout/Views/RefundWorkflowPage.xaml.cs`
22. `POSOpen/Features/Checkout/CheckoutRoutes.cs`
23. `POSOpen/Application/Abstractions/Services/ICheckoutUiService.cs`
24. `POSOpen/Infrastructure/Services/CheckoutUiService.cs`
25. `POSOpen/Features/Checkout/CheckoutServiceCollectionExtensions.cs`

### Create / Modify - Tests

26. `POSOpen.Tests/Unit/Checkout/GetRefundEligibilityUseCaseTests.cs`
27. `POSOpen.Tests/Unit/Checkout/SubmitRefundUseCaseTests.cs`
28. `POSOpen.Tests/Unit/Checkout/RefundWorkflowViewModelTests.cs`
29. Extend relevant checkout/security/audit integration tests if refund event append needs integration coverage.
30. Update `POSOpen.Tests/POSOpen.Tests.csproj` source-link entries for any new non-UI source files that are not already covered by existing wildcards.

---

## Implementation Notes

### Refund Target Model

Use cart session ID as the V1 refund transaction reference unless a stronger persisted transaction key already exists in checkout state. This is consistent with the current checkout and receipt workflows.

### Refund Persistence Strategy

Persist a refund record rather than relying on audit events alone. Audit records are append-only evidence, but the refund workflow still needs a queryable transaction-state artifact that can be shown in UI and later reconciliation.

### Approval / Override Handling

Do not duplicate governed override logic in a page. If the refund requires approval or a higher-authority path, either:

- Reuse `SubmitOverrideUseCase` semantics and payload structure, or
- Mirror its validation and audit rules exactly in the refund submission use case.

Mandatory rule: missing/whitespace-only reason must block finalization.

### Immutable Logging Strategy

Use `IOperationLogRepository.AppendAsync(...)` with stable event names and payloads. Refund denials due to missing permission or blocked approval path should also be logged when governance requires a trace of the attempted action.

### Idempotency and Duplicate-Prevention Strategy (Required)

`SubmitRefundUseCase` MUST be idempotent for finalization attempts keyed by operation context:

- A retry with the same operation ID must not create duplicate refund records.
- A retry with the same operation ID must not append duplicate `RefundCompleted` events.
- The use case should return the previously completed result when the same logical operation is replayed.

This aligns with financial safety expectations from FR41-FR45 and NFR7.

### UI State Transparency

The refund page should make these states explicit:

- eligible and directly refundable
- eligible but approval/override required
- not eligible / forbidden
- completed
- failed

Do not collapse governed refusal into a generic error label.

---

## Test Strategy

| Layer | File | Coverage |
|---|---|---|
| Application | `GetRefundEligibilityUseCaseTests.cs` | direct path, approval-required path, forbidden path, invalid target |
| Application | `SubmitRefundUseCaseTests.cs` | reason required, permission denied, success, audit append, refund record persistence |
| Presentation | `RefundWorkflowViewModelTests.cs` | eligibility load, reason validation, command enablement, status updates |
| Integration | refund + audit persistence tests | immutable event append and refund persistence consistency |

### Required Test Additions

- Verify Cashier sees only governed/approval path if `checkout.refund.initiate` is granted but `checkout.refund.approve` is not.
- Verify missing reason blocks finalize with canonical error code.
- Verify successful refund appends immutable audit with actor, reason, operation ID, UTC time.
- Verify forbidden or denied refund attempts append `RefundDenied` immutable audit events with actor, target, operation ID, and UTC timestamp.
- Verify no refund record or completion event is written on forbidden or invalid-target paths.
- Verify repeated finalization attempts with the same operation ID are idempotent (no duplicate refund record, no duplicate `RefundCompleted` event).
- Verify refund records can be queried by transaction/cart session reference.

---

## Definition of Done

- [x] Refund permission keys and policy mapping implemented.
- [x] Refund eligibility use case returns allowed paths by role/policy.
- [x] Refund submission use case enforces governed reason capture.
- [x] Refund record persistence implemented with EF configuration and migration.
- [x] Immutable audit events appended for refund completion (and governed denial/request paths if applicable).
- [x] Refund finalization is idempotent by operation ID and prevents duplicate completion side effects.
- [x] Checkout UI exposes refund workflow with explicit allowed/blocked states.
- [x] Canonical user-safe refund error messages and error codes implemented.
- [x] Unit tests cover eligibility, validation, authorization, persistence, and audit behavior.
- [x] Tests verify denied attempts are immutably logged and duplicate retries do not duplicate completion outcomes.
- [x] Integration coverage verifies refund record + immutable audit consistency.
- [x] Story status is moved to `done` after merge.

---

## Previous Story Intelligence

### From Story 3.4

- Checkout already has operation IDs, receipt metadata, checkout completion UI, and transaction status models.
- Use checkout/cart session ID as the operative V1 transaction reference unless a stronger refund aggregate is created.
- Reuse checkout DI and route registration patterns rather than creating a separate startup path.

### From Story 1.4

- Mandatory reason capture belongs in the application use case, not in UI-only validation.
- Trusted actor identity must come from `ICurrentSessionService`, not request payloads.
- Immutable governed actions already use operation-log append with complete payloads.

### From Story 1.5

- Keep refund auditing on `IOperationLogRepository`; do not create a second audit persistence path.
- Use stable event names and canonical audit payload shapes.
- Denied access to sensitive financial/governed actions should be traceable.

---

## Git Intelligence Summary

Recent checkout activity confirms the current implementation direction:

- `e1e349d` introduced Story 3.4 checkout completion, operation correlation, and receipt workflows.
- `c223db1` tightened review feedback around operation correlation correctness and deterministic diagnostics.
- `9681947` merged the full Story 3.4 stack to `main`, making it the baseline for Story 3.5.

Implication for Story 3.5:

- Build refund workflows as an additive extension of the merged checkout flow.
- Reuse existing operation/audit primitives instead of creating parallel implementations.

---

## Dev Agent Handoff

**Latest Status:**
- Stories 3.1 through 3.4 are implemented and merged.
- Checkout now supports cart composition, compatibility validation, card-payment attempts, receipt workflows, and completion UI.
- Story 3.5 is the next checkout slice and is ready for development.

**Key Context for Developer:**
- Refunds are both checkout-domain actions and governance actions. They must fit checkout flows and security/audit rules at the same time.
- Do not weaken the existing trusted-session authorization model to make refund UI easier.
- This story should leave the codebase positioned for later reconciliation/reporting stories in Epic 6 and support flows in Epic 7.

**Related Artifacts:**
- Epic 3 specification: [epics.md](../planning-artifacts/epics.md)
- Previous checkout story: [3-4-print-receipts-and-preserve-offline-continuity.md](./3-4-print-receipts-and-preserve-offline-continuity.md)
- Governed override reference: [1-4-governed-override-workflow.md](./1-4-governed-override-workflow.md)
- Immutable audit reference: [1-5-immutable-audit-trail-for-security-critical-actions.md](./1-5-immutable-audit-trail-for-security-critical-actions.md)
- Architecture: [architecture.md](../planning-artifacts/architecture.md)

---

## Story Completion Note

Story 3.5 should start by defining the permission contract and refund eligibility rules, then add refund persistence and immutable audit append, and finally add the checkout UI flow. This sequence reduces the risk of building UI on top of unclear policy assumptions.

---

## Tasks/Subtasks

### Task 1: Define refund permissions and contracts
- [x] Add `checkout.refund.initiate` and `checkout.refund.approve` to `RolePermissions`.
- [x] Add refund workflow constants with canonical error codes and safe messages.
- [x] Add refund DTOs/commands for eligibility and submission.

### Task 2: Add refund domain and persistence
- [x] Create `RefundRecord` entity and `RefundStatus` / `RefundPath` enums.
- [x] Add `IRefundRepository` abstraction and implementation.
- [x] Add EF configuration, DbSet, and migration for refund records.

### Task 3: Implement application refund workflows
- [x] Implement `GetRefundEligibilityUseCase`.
- [x] Implement `SubmitRefundUseCase` with trusted-session authorization and reason enforcement.
- [x] Append immutable refund audit events through `IOperationLogRepository`.

### Task 4: Build checkout refund UI flow
- [x] Add refund route/navigation hooks in checkout feature services.
- [x] Create `RefundWorkflowViewModel`.
- [x] Create `RefundWorkflowPage.xaml` and code-behind.
- [x] Present allowed refund paths and explicit blocked/approval-required state.

### Task 5: Add tests
- [x] Add eligibility use-case tests.
- [x] Add refund submission use-case tests.
- [x] Add refund workflow view model tests.
- [x] Add integration coverage for refund persistence + immutable audit append.

---

## Dev Agent Record

### Agent Model Used

GPT-5.3-Codex

### Implementation Plan

1. Introduce refund permissions, constants, commands, and DTOs.
2. Add refund persistence model and EF wiring.
3. Implement eligibility and submission use cases with policy enforcement and immutable audit append.
4. Add checkout refund page/view model and navigation integration.
5. Add focused unit and integration tests.

### Completion Notes List

_(populated as tasks complete)_
- Ultimate context engine analysis completed - comprehensive developer guide created.
- Implemented policy-bound refund permissions, eligibility resolution, governed submission, and immutable refund audit events.
- Added refund persistence model and EF migration with operation-id uniqueness for idempotent finalization.
- Added checkout refund UI route/page/view model and completion-page navigation hook.
- Added unit and integration tests for eligibility, reason enforcement, denied audit logging, and idempotency.

### Debug Log

_(populated if issues arise)_
- 2026-03-31: Ran `dotnet test POSOpen.Tests/POSOpen.Tests.csproj` (pass).

---

## File List

_(populated as implementation proceeds)_
- POSOpen/Application/Security/RolePermissions.cs
- POSOpen/Application/Security/SecurityAuditEventTypes.cs
- POSOpen/Application/Abstractions/Services/ICheckoutUiService.cs
- POSOpen/Application/Abstractions/Repositories/IRefundRepository.cs
- POSOpen/Application/UseCases/Checkout/RefundWorkflowConstants.cs
- POSOpen/Application/UseCases/Checkout/RefundEligibilityDto.cs
- POSOpen/Application/UseCases/Checkout/SubmitRefundCommand.cs
- POSOpen/Application/UseCases/Checkout/SubmitRefundResultDto.cs
- POSOpen/Application/UseCases/Checkout/GetRefundEligibilityUseCase.cs
- POSOpen/Application/UseCases/Checkout/SubmitRefundUseCase.cs
- POSOpen/Domain/Entities/RefundRecord.cs
- POSOpen/Domain/Enums/RefundStatus.cs
- POSOpen/Domain/Enums/RefundPath.cs
- POSOpen/Features/Checkout/CheckoutRoutes.cs
- POSOpen/Features/Checkout/CheckoutServiceCollectionExtensions.cs
- POSOpen/Features/Checkout/ViewModels/CheckoutCompletionViewModel.cs
- POSOpen/Features/Checkout/ViewModels/RefundWorkflowViewModel.cs
- POSOpen/Features/Checkout/Views/CheckoutCompletionPage.xaml
- POSOpen/Features/Checkout/Views/RefundWorkflowPage.xaml
- POSOpen/Features/Checkout/Views/RefundWorkflowPage.xaml.cs
- POSOpen/Infrastructure/Services/CheckoutUiService.cs
- POSOpen/Infrastructure/Persistence/PosOpenDbContext.cs
- POSOpen/Infrastructure/Persistence/PersistenceServiceCollectionExtensions.cs
- POSOpen/Infrastructure/Persistence/Configurations/RefundRecordConfiguration.cs
- POSOpen/Infrastructure/Persistence/Repositories/RefundRepository.cs
- POSOpen/Infrastructure/Persistence/Migrations/20260401110000_AddRefundRecords.cs
- POSOpen.Tests/POSOpen.Tests.csproj
- POSOpen.Tests/Unit/Checkout/CheckoutCompletionViewModelTests.cs
- POSOpen.Tests/Unit/Checkout/GetRefundEligibilityUseCaseTests.cs
- POSOpen.Tests/Unit/Checkout/SubmitRefundUseCaseTests.cs
- POSOpen.Tests/Unit/Checkout/RefundWorkflowViewModelTests.cs
- POSOpen.Tests/Integration/Checkout/RefundWorkflowIntegrationTests.cs

---

## Change Log

| Date | Change |
|---|---|
| 2026-03-31 | Story artifact created and set to ready-for-dev. |
| 2026-03-31 | Workflow compliance pass completed with git-intelligence context and handoff refinements. |
| 2026-03-31 | Implemented refund workflow (permissions, use cases, persistence, UI, tests) and moved status to review. |
| 2026-03-31 | Story checklist artifacts synchronized to done after final validation and review closure. |
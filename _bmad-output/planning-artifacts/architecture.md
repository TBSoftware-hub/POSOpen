---
stepsCompleted: [1, 2, 3, 4, 5, 6, 7, 8]
inputDocuments:
  - prd.md
  - prd-finalization-summary.md
  - ux-design-specification.md
  - brainstorming-session-2026-03-28-113758.md
workflowType: architecture
project_name: POSOpen
user_name: Timbe
date: 2026-03-28
lastStep: 8
status: complete
completedAt: 2026-03-28
---

# Architecture Decision Document

_This document builds collaboratively through step-by-step discovery. Sections are appended as we work through each architectural decision together._

## Workflow Initialization

Architecture workflow initialized with required inputs loaded and validated.

---

## Project Context Analysis

### Requirements Overview

**Functional Requirements:**
- 55 functional requirements identified (FR1-FR55), spanning identity/access, admissions, mixed-cart transactions, party lifecycle, inventory reservation, offline sync, hardware integrations, financial controls, reporting/export, and support diagnostics.
- Architectural implication: operations-heavy transactional system with exception handling and lifecycle coordination as first-class concerns.

**Non-Functional Requirements:**
- 24 non-functional requirements identified (NFR1-NFR24), including sub-2s interaction targets, offline continuity, <=5-minute sync SLA, idempotent replay, server-side RBAC, immutable auditability, hardware reliability, and growth readiness.
- Architectural implication: resilience, consistency, auditability, and operability are primary design drivers.

**Scale and Complexity:**
- Primary domain: full-stack transactional operations platform (frontline + back-office).
- Complexity level: high.
- Estimated architectural component families: 12-16.

### Technical Constraints & Dependencies
- Offline-first operation is mandatory for critical workflows.
- Eventual consistency target is explicit (5-minute sync SLA).
- Card-present and deferred-payment flows require strict idempotency and replay safety.
- Hardware integrations are required in MVP (receipt printer, scanner, card reader).
- MVP is single-location/single-tenant operationally, but model must be multi-location ready.
- UX requires role-mode interfaces, next-best-action guidance, and persistent state visibility.
- Compliance baseline requires immutable audit trails and minimized PCI scope via tokenized/hosted payment patterns.

### Cross-Cutting Concerns Identified
1. Authorization and policy enforcement (RBAC, overrides, substitution rules).
2. State consistency and reconciliation (offline queue, replay ordering, conflict resolution, duplicate prevention).
3. Observability and exception handling (sync health, deferred payment failures, hardware faults).
4. UX state transparency (waiver/payment/sync status visible and actionable).
5. Data integrity and traceability (operational actions to financial outcomes and exports).
6. Accessibility and interaction consistency (tablet/desktop behavior parity and WCAG constraints).

### Validation Summary
- POSOpen is a reliability-first, operations-centric architecture problem with strong transactional guarantees, rich role-based UX, and mandatory offline resilience.
- Architecture should prioritize deterministic state management, policy-driven domain services, and operational observability from day one.

*Step 2: Project context analysis saved. Ready for next step.*

---

## Starter Template Evaluation

### Primary Technology Domain
- Desktop/mobile line-of-business application using .NET MAUI.
- Offline-first local data handling with eventual cloud expansion.

### Starter Options Considered
1. .NET MAUI App template (selected)
2. .NET MAUI Blazor template (not selected)

### Selected Starter: .NET MAUI App Template

**Rationale for Selection:**
- Aligns with confirmed stack decision: .NET 10 + MAUI.
- Supports native-feeling role-mode UX and cross-platform deployment.
- Fits single-terminal V1 operations with local-first persistence.
- Preserves clean migration path to SQL Server in V2.

**Initialization Command:**

dotnet new maui -n POSOpen

**Supporting Setup Commands:**

dotnet workload install maui
dotnet add package CommunityToolkit.Mvvm
dotnet add package sqlite-net-pcl
dotnet add package Microsoft.Extensions.DependencyInjection
dotnet add package Microsoft.Extensions.Logging

**Architectural Decisions Provided by Starter:**

**Language & Runtime:**
- C# with .NET 10 MAUI runtime targets.

**UI Approach:**
- Native MAUI app model suitable for role-based operational interfaces.

**Data Strategy (V1 -> V2):**
- V1: SQLite local database as primary store for single-terminal facility operations.
- V2: SQL Server provider (Azure free tier) behind the same repository abstractions.
- Keep schema and identifiers stable now to reduce migration risk.

**Operational Scope Impact:**
- V1 concurrency model can remain lightweight due to mostly single-terminal usage.
- Prioritize local durability, backup/export, and recovery.
- Design interfaces so cloud synchronization can be added in V2 without reworking core domain logic.

**Note:** Project initialization and package installation are implementation prerequisites and should be represented in the earliest implementation stories.

*Step 3: Starter template evaluation saved. Ready for next step.*

---

## Core Architectural Decisions

### Decision Priority Analysis

**Critical Decisions (Block Implementation):**
1. Data abstraction boundary to support SQLite now and SQL Server later.
2. Offline transaction log and deterministic replay strategy.
3. Security model for local encrypted-at-rest data and role enforcement.
4. Local backup/export and recovery strategy for single-terminal operation.

**Important Decisions (Shape Architecture):**
1. Dependency injection and service layering conventions.
2. Observability and diagnostic event model.
3. API/sync contract design for V2 cloud transition.

**Deferred Decisions (Post-MVP):**
1. Multi-terminal conflict resolution policy.
2. Full cloud-hosted authentication and multi-tenant controls.
3. Real-time cross-terminal coordination.

### Data Architecture
- Primary V1 store: SQLite local database.
- ORM strategy: EF Core with provider swap model.
- Provider plan:
  - V1: Microsoft.EntityFrameworkCore.Sqlite.
  - V2: Microsoft.EntityFrameworkCore.SqlServer behind the same repository interfaces.
- Schema strategy: stable GUID-based identifiers and audit columns from day one.
- Migration strategy:
  - Code-first migrations for V1 local schema evolution.
  - V2 SQL Server migrations with one-time data lift/import pipeline.
- Single-terminal optimization:
  - Lightweight concurrency model in V1.
  - Prioritize durability, crash recovery, and deterministic replay.

### Authentication & Security
- V1 identity: local role-based authentication scoped to facility terminal.
- Authorization: policy-based checks in application services (not UI-only).
- Data protection:
  - Encrypt sensitive local fields.
  - Use platform secure storage for secrets.
  - Do not store raw card PAN data locally.
- Audit: immutable append-only audit events for overrides, refunds, and replay outcomes.

### API & Communication Patterns
- V1 mode: local-first with no mandatory cloud dependency for core flows.
- Internal communication: command-handler service layer with domain events for audit and sync queue.
- V2 sync contract:
  - Outbox pattern from local event log.
  - Idempotent server endpoints keyed by operation IDs.
- Error standard: uniform result envelope with user-safe message plus diagnostic code.

### Frontend Architecture
- Pattern: ViewModel-first using CommunityToolkit.Mvvm.
- State management:
  - Per-screen ViewModel state.
  - Shared app-state service for session, terminal mode, and sync status.
- Navigation: Shell with role-gated route access.
- UX state transparency:
  - Persistent sync/queue status.
  - Inline recovery actions for failed operations.
- Offline UX contract:
  - Critical commands return local-commit status and deferred-cloud status separately.

### Infrastructure & Deployment
- V1 deployment: single facility terminal package with local SQLite database.
- V1 operations:
  - Automated local backup snapshots.
  - CSV export for reconciliation/reporting.
- V2 cloud target: Azure SQL free offer for early sync/reporting workloads.
- CI/CD baseline:
  - Build/test pipeline for MAUI app.
  - Migration validation for SQLite and SQL Server providers.

### Decision Impact Analysis

**Implementation Sequence:**
1. Define domain model and repository interfaces.
2. Implement SQLite EF Core infrastructure and migrations.
3. Implement offline operation log and replay worker.
4. Add audit trail and recovery workflows.
5. Add SQL Server provider and sync adapter in V2 branch.

**Cross-Component Dependencies:**
- Sync design depends on stable operation IDs and audit schema.
- Security and audit depend on service-layer command boundaries.
- SQL Server transition depends on provider-agnostic repository contracts.

*Step 4: Core architectural decisions saved. Ready for next step.*

---

## Implementation Patterns & Consistency Rules

### Pattern Categories Defined
Critical conflict points identified: 10 categories where AI agents could diverge (naming, structure, DTO formats, error envelopes, event naming, retry behavior, and state handling).

### Naming Patterns

**Database Naming Conventions:**
- EF entity names: singular PascalCase in code.
- Physical table names: snake_case plural mapping.
- Columns: snake_case in database, Pascal/camel in C# models.
- Primary key: `id` (GUID text in SQLite).
- Foreign key: `{entity}_id`.
- Index naming: `idx_{table}_{column}`.

**API/Sync Contract Naming Conventions:**
- Resource names: plural kebab-case.
- JSON field names: camelCase.
- Correlation header: `X-Request-Id`.

**Code Naming Conventions:**
- Types/files: PascalCase.
- Private fields: `_camelCase`.
- Async methods: suffix `Async`.
- Interfaces: prefix `I`.
- ViewModels: suffix `ViewModel`.
- Services: suffix `Service`.
- Repositories: suffix `Repository`.

### Structure Patterns

**Project Organization:**
- `POSOpen.Domain`: entities, value objects, domain policies.
- `POSOpen.Application`: use cases, interfaces, command handlers.
- `POSOpen.Infrastructure`: EF/SQLite implementations, backup/export, adapters.
- `POSOpen.Presentation`: MAUI views, viewmodels, navigation.
- `POSOpen.Tests`: unit and integration tests.

**Feature Slicing Rule:**
- Organize by feature first (Admissions, Party, Checkout, Inventory, Sync), then by layer.

**Test Placement:**
- Unit and integration tests in `POSOpen.Tests`, grouped by namespace/feature.

### Format Patterns

**Result Envelope:**
- Standard result fields:
  - `isSuccess`
  - `errorCode`
  - `userMessage`
  - `diagnosticMessage`
  - `payload`

**Error Format:**
- Separate user-safe message from diagnostics.
- Use canonical error codes such as `SYNC_QUEUE_CONFLICT`, `PAYMENT_DEFERRED`, `WAIVER_REQUIRED`.

**Date/Time and IDs:**
- Persist UTC timestamps.
- Serialize dates in ISO-8601.
- Use GUID operation IDs at command boundary.
- Include correlation and causation IDs for queued operations.

### Communication Patterns

**Event Naming and Payload:**
- Event names in past-tense PascalCase (`PaymentQueued`, `BookingReserved`, `SyncReplayFailed`).
- Event schema includes:
  - `eventId`
  - `eventType`
  - `occurredUtc`
  - `aggregateId`
  - `operationId`
  - `version`
  - `payload`

**State Update Patterns:**
- Explicit ViewModel state transitions: Idle -> Loading -> Success|Error|Deferred.
- No hidden side-effects in property setters.
- State changes flow through commands/use-cases.

### Process Patterns

**Error Handling:**
- Do not swallow exceptions.
- Map infrastructure exceptions to canonical app error codes.
- Restrict retry policy to infrastructure boundary.

**Loading and Deferred States:**
- Use per-screen and per-command busy flags.
- Deferred operations must surface explicit queued status.

**Validation Layers:**
- ViewModel: synchronous field validation.
- Application layer: business validation.
- Infrastructure: persistence constraints.

**Offline Replay Pattern:**
- Append-only queue writes.
- Ordered, idempotent replay.
- Failed replay remains visible with action guidance.

### Enforcement Guidelines

**All AI Agents MUST:**
1. Respect layer boundaries (Presentation does not call Infrastructure directly).
2. Use standard result/error envelopes.
3. Follow naming and suffix conventions.
4. Emit operation and correlation IDs on write paths.
5. Preserve offline/deferred UX transparency.

**Pattern Enforcement:**
- Use analyzers and style rules.
- Include architecture checklist in PR reviews.
- Add contract tests for DTO and event schema consistency.

### Pattern Examples

**Good Examples:**
- `QueuePaymentCommandHandler` emits `PaymentQueued` with operation ID and returns deferred success.
- `AdmissionViewModel.CheckInAsync` exposes explicit state transitions and queued status when offline.
- Repository interfaces remain in Application, implementations in Infrastructure.

**Anti-Patterns:**
- Views calling SQLite context directly.
- Inconsistent error payload structures.
- Persisting local time instead of UTC.
- UI-layer retry loops.
- Naming convention drift without ADR update.

*Step 5: Implementation patterns and consistency rules saved. Ready for next step.*

---

## Project Structure & Boundaries

### Complete Project Directory Structure

```text
POSOpen/
|- .github/
|  |- workflows/
|  |  |- ci.yml
|  |  |- release.yml
|- docs/
|  |- architecture/
|  |- adr/
|  |- operations/
|- POSOpen/
|  |- POSOpen.csproj
|  |- App.xaml
|  |- AppShell.xaml
|  |- MauiProgram.cs
|  |- Features/
|  |  |- Admissions/
|  |  |  |- Views/
|  |  |  |- ViewModels/
|  |  |  |- Commands/
|  |  |  |- Dtos/
|  |  |- Checkout/
|  |  |- Party/
|  |  |- Inventory/
|  |  |- Sync/
|  |  |- Reporting/
|  |  |- Support/
|  |- Domain/
|  |  |- Entities/
|  |  |- ValueObjects/
|  |  |- Enums/
|  |  |- Policies/
|  |  |- Events/
|  |- Application/
|  |  |- Abstractions/
|  |  |  |- Repositories/
|  |  |  |- Services/
|  |  |  |- Security/
|  |  |- UseCases/
|  |  |- Validation/
|  |  |- Results/
|  |- Infrastructure/
|  |  |- Persistence/
|  |  |  |- Db/
|  |  |  |- Configurations/
|  |  |  |- Migrations/
|  |  |  |- Repositories/
|  |  |- Security/
|  |  |- Devices/
|  |  |  |- Printer/
|  |  |  |- Scanner/
|  |  |  |- CardReader/
|  |  |- Sync/
|  |  |- Export/
|  |  |- Logging/
|  |- Shared/
|  |  |- Constants/
|  |  |- Extensions/
|  |  |- Serialization/
|  |  |- Localization/
|  |- Resources/
|  |- Platforms/
|- POSOpen.Tests/
|  |- POSOpen.Tests.csproj
|  |- Unit/
|  |- Integration/
|  |- Contracts/
|  |- TestData/
|- _bmad-output/
|  |- planning-artifacts/
```

### Architectural Boundaries

**API Boundaries:**
1. No external API required for V1 core flows.
2. Internal application boundary is command/use-case based.
3. V2 introduces sync APIs via dedicated Sync adapter in Infrastructure.

**Component Boundaries:**
1. Views and ViewModels remain in Features.
2. ViewModels call Application use-cases only.
3. Views never access Infrastructure directly.

**Service Boundaries:**
1. Application defines interfaces for repositories/services.
2. Infrastructure implements these interfaces.
3. Domain remains persistence-ignorant.

**Data Boundaries:**
1. SQLite is the V1 system of record.
2. Audit/outbox records are append-only.
3. SQL Server V2 provider is isolated behind repository abstractions.

### Requirements to Structure Mapping

**Feature Mapping:**
1. Identity/roles (FR1-FR5): Features/Support + Application/Security + Infrastructure/Security
2. Admissions/check-in (FR6-FR11): Features/Admissions
3. Mixed cart/checkout (FR12-FR17): Features/Checkout
4. Party lifecycle (FR18-FR24): Features/Party
5. Inventory reservation (FR25-FR29): Features/Inventory
6. Offline/sync (FR30-FR36): Features/Sync + Infrastructure/Sync
7. Devices/hardware (FR37-FR40): Infrastructure/Devices
8. Financial/audit/reconciliation (FR41-FR45): Features/Checkout + Infrastructure/Persistence + Infrastructure/Export
9. Reporting/visibility (FR46-FR50): Features/Reporting
10. Support/diagnostics (FR51-FR55): Features/Support + Infrastructure/Logging

**Cross-Cutting Concerns:**
1. RBAC enforcement: Application/Abstractions/Security and Infrastructure/Security
2. Error/result envelope: Application/Results
3. Event schema: Domain/Events and Infrastructure/Sync
4. UTC/time serialization: Shared/Serialization

### Integration Points

**Internal Communication:**
1. ViewModel -> UseCase -> Repository/Service -> Persistence/Device adapter
2. Domain events emitted from use-cases and persisted to outbox

**External Integrations:**
1. Payment terminal adapter: Infrastructure/Devices/CardReader
2. Printer/scanner adapters: Infrastructure/Devices
3. CSV export interface: Infrastructure/Export
4. V2 cloud sync adapter: Infrastructure/Sync/Cloud

**Data Flow:**
1. UI command executes use-case
2. Use-case validates and writes aggregate changes
3. Changes, audit, and outbox committed in one transaction
4. Sync worker processes outbox when online

### File Organization Patterns

**Configuration:**
1. App-level DI in MauiProgram
2. Feature registration extension methods per module
3. Environment switches centralized in Shared constants/config

**Tests:**
1. Unit tests for Domain/Application logic
2. Integration tests for SQLite persistence and replay behavior
3. Contract tests for DTO/event/result schema consistency

**Build/Deploy:**
1. Single MAUI app package for V1 facility terminal
2. CI validates build, tests, analyzers, and migrations

*Step 6: Project structure and boundaries saved. Ready for next step.*

---

## Architecture Validation Results

### Coherence Validation ✅

**Decision Compatibility:**
- Stack coherence is strong: .NET 10 MAUI + CommunityToolkit.Mvvm + SQLite V1 + SQL Server V2 path.
- Layering decisions align with implementation patterns and project structure.
- Offline-first and deferred-sync patterns are consistent with single-terminal V1 scope.
- No contradictory technology decisions detected.

**Pattern Consistency:**
- Naming, result-envelope, event schema, and replay patterns are mutually consistent.
- Error handling and state-transition rules align with MVVM conventions.
- Security and audit patterns align with policy-based authorization design.

**Structure Alignment:**
- Proposed directory boundaries support feature modules and cross-cutting concerns.
- Data, service, and UI boundaries are explicit enough to prevent agent drift.
- Integration points for devices, export, and V2 cloud sync are well-placed.

### Requirements Coverage Validation ✅

**Functional Requirements Coverage:**
- FR1-FR55 categories are mapped to modules/features.
- Core paths are covered: admissions, checkout, party lifecycle, inventory, sync, and support.
- Device integration and diagnostics are represented in Infrastructure boundaries.

**Non-Functional Requirements Coverage:**
- Performance targets addressed by local-first processing and minimal runtime dependencies.
- Reliability addressed by append-only queue + idempotent replay + audit traceability.
- Security addressed by service-layer authorization and local sensitive-data protection.
- Growth readiness addressed by provider abstraction and stable identifier strategy.

### Implementation Readiness Validation ✅

**Decision Completeness:**
- Critical decisions documented with concrete direction and migration path.
- Core versions verified and compatible for selected stack.
- Enforcement rules specified for agent consistency.

**Structure Completeness:**
- Architecture tree is complete for target shape.
- Existing scaffold at `POSOpen/POSOpen` requires phased refactor to target structure.
- Solution file and test project should be added as first implementation tasks.

**Pattern Completeness:**
- Conflict-prone areas are covered: naming, formats, communication, process patterns.
- Anti-patterns and positive examples are explicit.

### Gap Analysis Results

**Critical Gaps:**
- None.

**Important Gaps:**
1. V1 local authentication hardening specifics (credential storage and lockout policy) should be captured as ADR.
2. Backup/restore operational runbook format should be defined before production rollout.
3. SQLite encryption strategy should be finalized early in implementation.

**Nice-to-Have Gaps:**
1. Optional architecture diagram for onboarding.
2. Additional contract test templates for event and result envelopes.

### Architecture Completeness Checklist

**✅ Requirements Analysis**
- [x] Project context analyzed
- [x] Scale and complexity assessed
- [x] Technical constraints identified
- [x] Cross-cutting concerns mapped

**✅ Architectural Decisions**
- [x] Critical decisions documented with versions
- [x] Technology stack specified
- [x] Integration patterns defined
- [x] Performance considerations addressed

**✅ Implementation Patterns**
- [x] Naming conventions established
- [x] Structure patterns defined
- [x] Communication patterns specified
- [x] Process patterns documented

**✅ Project Structure**
- [x] Complete directory structure defined
- [x] Component boundaries established
- [x] Integration points mapped
- [x] Requirements-to-structure mapping completed

### Architecture Readiness Assessment

**Overall Status:** READY FOR IMPLEMENTATION

**Confidence Level:** High

**Key Strengths:**
- Clear migration path from SQLite V1 to SQL Server V2.
- Strong consistency model for multi-agent implementation.
- Explicit offline reliability and auditability strategy.
- Practical boundary mapping from requirements to structure.

**Areas for Future Enhancement:**
- Multi-terminal conflict resolution (deferred to V2).
- Expanded cloud sync orchestration and server-side policy controls.

### Implementation Handoff

**AI Agent Guidelines:**
- Follow architectural decisions and consistency rules exactly.
- Keep Presentation -> Application -> Infrastructure boundaries strict.
- Use canonical result/error/event formats.
- Preserve operation/correlation IDs on all write paths.
- Maintain offline transparency and replay determinism.

**First Implementation Priority:**
1. Restructure existing MAUI project into defined feature/layer boundaries.
2. Add solution and test project skeleton.
3. Implement SQLite persistence baseline with migrations and operation log.

*Step 7: Architecture validation saved. Ready for completion step.*

---

## Architecture Completion & Handoff

### Completion Summary
Architecture workflow is complete. The document now provides:
- Full project context and requirement-driven architecture analysis
- Core architectural decisions for .NET 10 MAUI + SQLite (V1) + SQL Server (V2)
- Implementation patterns and consistency rules to prevent AI agent drift
- Complete project structure and boundary definitions
- Validation results confirming coherence, coverage, and implementation readiness

### Handoff Guidance
This architecture document is the implementation source of truth.

Implementation should begin with:
1. Restructure the existing MAUI app into the documented feature/layer boundaries.
2. Add solution and test project skeletons aligned to the structure.
3. Implement persistence baseline (SQLite + migrations + operation log/outbox).
4. Implement core use-case pipelines for Admissions, Checkout, and Party flows.
5. Add observability, backup/export, and replay diagnostics.

### Ready for Next Phase
Recommended next workflow options:
1. Create epics and stories mapped to architecture modules.
2. Begin implementation using architecture as enforcement guide.
3. Generate initial ADRs for security hardening, backup policy, and SQLite encryption.

*Step 8: Architecture workflow complete.*

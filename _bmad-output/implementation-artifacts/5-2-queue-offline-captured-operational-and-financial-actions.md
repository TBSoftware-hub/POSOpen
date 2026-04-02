# Story 5.2: Queue Offline-Captured Operational and Financial Actions

Status: done

## Story

As the system,
I want to persist queued actions with operation metadata while offline,
so that actions can be replayed reliably after reconnect.

---

## Acceptance Criteria

### AC-1 - Persist offline write actions with complete operation metadata

> **Given** a write action occurs in offline mode  
> **When** the command is accepted  
> **Then** an outbox/queue record is appended with operation ID, timestamp UTC, actor, and payload snapshot.

### AC-2 - Preserve captured sequence order exactly

> **Given** multiple offline actions are captured  
> **When** the queue is inspected  
> **Then** sequence order is preserved exactly as captured  
> **And** ordering is derived from a database-assigned monotonic queue value or equivalent single-writer transactional guarantee that remains stable under concurrent offline writes.

### AC-3 - Ensure queue durability across app/device restarts

> **Given** queue records are persisted  
> **When** the app/device restarts  
> **Then** queue records remain durable and recoverable.

---

## Tasks / Subtasks

- [ ] Task 1: Introduce explicit offline queue write contract and metadata envelope (AC-1)
  - [ ] Create `POSOpen/Application/UseCases/Sync/QueueOfflineActionCommand.cs` with fields for `EventType`, `AggregateId`, `ActorStaffId`, `PayloadSnapshot`, and `OperationContext`.
  - [ ] Create `POSOpen/Application/UseCases/Sync/QueueOfflineActionResultDto.cs` with `MessageId`, `OperationId`, `CorrelationId`, `EnqueuedUtc`, and `QueueSequence`.
  - [ ] Require actor traceability in persisted queue records by either storing `ActorStaffId` as a first-class outbox column or enforcing it as a non-optional serialized payload field validated before enqueue.
  - [ ] Ensure all queued payload snapshots are serialized with `AppJsonSerializerOptions.Default`.

- [ ] Task 2: Extend outbox schema to support deterministic ordering semantics (AC-2)
  - [ ] Add `QueueSequence` to `POSOpen/Domain/Entities/OutboxMessage.cs` as a monotonic queue ordering field.
  - [ ] Add actor traceability storage to the outbox persistence contract (`ActorStaffId` column or equivalent required persisted field) so replay and incident review do not depend on transient command context.
  - [ ] Update `POSOpen/Infrastructure/Persistence/Configurations/OutboxMessageConfiguration.cs` to map `QueueSequence` and add an index for pending ordered reads.
  - [ ] Add EF Core migration to add `queue_sequence` to `OutboxMessages` and backfill existing records in deterministic order.
  - [ ] Keep current `PublishedUtc` semantics unchanged; this story only appends pending rows.

- [ ] Task 3: Implement offline queueing service in the Application->Infrastructure boundary (AC-1, AC-2)
  - [ ] Create `POSOpen/Application/Abstractions/Services/IOfflineActionQueueService.cs` with `QueueAsync(...)`.
  - [ ] Implement `POSOpen/Infrastructure/Sync/OfflineActionQueueService.cs` to:
    - [ ] Resolve `QueueSequence` using a race-safe persistence mechanism such as a database-assigned monotonic value or an equivalent serialized transactional write rule; do not rely on `max + 1` reads outside the enqueue transaction.
    - [ ] Append outbox message with operation/correlation/causation IDs from `OperationContext`.
    - [ ] Persist actor traceability alongside the payload snapshot so replay, support diagnostics, and audit review can recover the actor without reconstructing command-time state.
  - [ ] Register service in DI (`MauiProgram` or persistence registration extension) without breaking existing service registrations.

- [ ] Task 4: Route offline-accepted operational and financial writes through the queue service (AC-1)
  - [ ] Update `POSOpen/Application/UseCases/Admissions/CompleteAdmissionCheckInUseCase.cs` deferred payment path to queue through `IOfflineActionQueueService` rather than direct ad-hoc outbox composition.
  - [ ] Update `POSOpen/Infrastructure/Persistence/Repositories/AdmissionCheckInRepository.cs` so the deferred admission write path no longer owns bespoke outbox append logic that duplicates the shared queue contract.
  - [ ] Leave checkout, refund, and party flows out of scope for Story 5.2 unless a concrete deferred outbox write path already exists in the current branch at implementation time; do not invent new deferred workflows as part of this story.
  - [ ] Preserve current `AppResult<T>` and user-safe messaging behavior for offline/deferred success responses.

- [ ] Task 5: Preserve visibility and recoverability across restart boundaries (AC-3)
  - [ ] Ensure `IOutboxRepository.ListPendingAsync()` returns pending items ordered by `QueueSequence` then UTC fallback.
  - [ ] Verify `POSOpen/Infrastructure/Services/ConnectivityMonitorService.cs` startup pending read continues to surface pending queue count after restart.
  - [ ] Keep queue writes append-only and non-destructive; no deletion/compaction in this story.

- [ ] Task 6: Add comprehensive tests for metadata, ordering, and durability (AC-1, AC-2, AC-3)
  - [ ] Create `POSOpen.Tests/Unit/Sync/OfflineActionQueueServiceTests.cs` to validate metadata mapping (`OperationId`, `CorrelationId`, `ActorStaffId`, UTC timestamps) and reject enqueue attempts that omit required actor metadata.
  - [ ] Create `POSOpen.Tests/Integration/Sync/OutboxQueueOrderingTests.cs` to validate deterministic ordering by `QueueSequence` across multiple captures.
  - [ ] Add a concurrent enqueue integration test that appends multiple offline actions in parallel and proves the persisted `QueueSequence` values remain unique, gap-tolerant if needed, and strictly orderable by capture commit semantics.
  - [ ] Create `POSOpen.Tests/Integration/Sync/OutboxQueueDurabilityTests.cs` to validate pending queue persistence after DbContext re-creation and simulated restart.
  - [ ] Add regression test to ensure existing Story 5.1 startup pending sync message still reflects persisted pending count.

---

## Dev Notes

### Scope Boundary

Story 5.2 is the queue persistence foundation for Epic 5:
- It defines how offline-captured operational and financial actions are appended with complete metadata.
- It establishes deterministic ordering metadata for later replay worker logic.
- In the current codebase, the concrete in-scope adoption target is the deferred admission payment path; broader checkout/party adoption happens only where an existing deferred outbox write path is already present.
- It does not implement reconnect replay execution (Story 5.3).
- It does not implement duplicate-finalization resolution workflow (Story 5.4).
- It does not implement full queue health dashboard and exception management UI (Story 5.5).

### Architecture Compliance Guardrails

- Respect layer boundaries: Presentation -> Application -> Infrastructure only.
- Keep queue contract in Application abstractions and implementation in Infrastructure.
- Keep outbox append-only; do not mutate historical payload snapshots.
- Use UTC timestamps only for `OccurredUtc` and `EnqueuedUtc`.
- Actor metadata is mandatory on queued records and must survive restart/replay boundaries in persisted form.
- Preserve operation/correlation/causation IDs on every queued write path.
- Queue ordering must be concurrency-safe and derived from the persistence boundary, not from optimistic in-memory sequencing.
- Keep canonical result envelope semantics (`AppResult<T>`) in use-cases.
- Do not add UI-layer retry loops; replay worker logic is deferred to Story 5.3.

### File Structure Requirements

Expected files to add:

- `POSOpen/Application/Abstractions/Services/IOfflineActionQueueService.cs`
- `POSOpen/Application/UseCases/Sync/QueueOfflineActionCommand.cs`
- `POSOpen/Application/UseCases/Sync/QueueOfflineActionResultDto.cs`
- `POSOpen/Infrastructure/Sync/OfflineActionQueueService.cs`
- `POSOpen.Tests/Unit/Sync/OfflineActionQueueServiceTests.cs`
- `POSOpen.Tests/Integration/Sync/OutboxQueueOrderingTests.cs`
- `POSOpen.Tests/Integration/Sync/OutboxQueueDurabilityTests.cs`

Expected files to modify:

- `POSOpen/Domain/Entities/OutboxMessage.cs`
- `POSOpen/Infrastructure/Persistence/Configurations/OutboxMessageConfiguration.cs`
- `POSOpen/Infrastructure/Persistence/Repositories/OutboxRepository.cs`
- `POSOpen/Application/UseCases/Admissions/CompleteAdmissionCheckInUseCase.cs`
- `POSOpen/Infrastructure/Services/ConnectivityMonitorService.cs`
- `POSOpen/MauiProgram.cs` (or the central DI registration extension already used by persistence/services)

### Previous Story Intelligence (5.1)

- Story 5.1 already introduced offline mode detection and visibility through `IAppStateService` and `ConnectivityMonitorService`; reuse this baseline and avoid duplicate connectivity logic.
- `IOutboxRepository.ListPendingAsync()` is already used at startup to reflect pending count; Story 5.2 should strengthen ordering/durability guarantees, not replace this flow.
- Existing offline guidance in admissions/checkout/party viewmodels should remain user-facing and unchanged while queue persistence becomes standardized behind services.

### Git Intelligence Summary

Recent commits indicate Epic 5 has just entered implementation and Story 5.1 merged on main:
- `75b1a2a` feat(sync): implement story 5.1 offline mode baseline
- `e6db924` merge of story 5.1 branch into main

This story should build incrementally on those patterns and avoid broad refactors outside queue persistence.

### Testing Requirements

- Unit tests must validate metadata correctness, mandatory actor persistence, and idempotent append semantics of queueing service behavior.
- Integration tests must verify queue order and durability against real SQLite persistence behavior, including parallel enqueue pressure.
- Existing admission and startup offline-mode tests must remain green.
- Regression coverage must prove no change to successful online completion paths.

### References

- Epic 5 Story 5.2 acceptance criteria [Source: _bmad-output/planning-artifacts/epics.md]
- Offline replay pattern and guardrails [Source: _bmad-output/planning-artifacts/architecture.md]
- UX queue transparency and offline status semantics [Source: _bmad-output/planning-artifacts/ux-design-specification.md]
- Existing outbox entity and repository behavior [Source: POSOpen/Domain/Entities/OutboxMessage.cs]
- Existing outbox entity and repository behavior [Source: POSOpen/Infrastructure/Persistence/Repositories/OutboxRepository.cs]
- Existing deferred queueing path in admissions [Source: POSOpen/Application/UseCases/Admissions/CompleteAdmissionCheckInUseCase.cs]
- Existing startup queue visibility baseline [Source: POSOpen/Infrastructure/Services/ConnectivityMonitorService.cs]

---

## Dev Agent Record

### Agent Model Used

GPT-5.3-Codex (GitHub Copilot)

### Debug Log References

### Completion Notes List

- Story context created with explicit AC mapping, architecture guardrails, and previous story continuity.

### File List

- `_bmad-output/implementation-artifacts/5-2-queue-offline-captured-operational-and-financial-actions.md`

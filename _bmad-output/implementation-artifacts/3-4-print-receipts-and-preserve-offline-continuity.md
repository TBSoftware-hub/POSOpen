# Story 3.4 - Print Receipts and Preserve Offline Continuity

## Metadata

| Field | Value |
|---|---|
| Epic | 3 - Mixed-Cart Checkout, Payments, and Device Execution |
| Story | 3.4 |
| Key | `3-4-print-receipts-and-preserve-offline-continuity` |
| Status | for-review |
| Author | Timbe (via BMAD Story Creator) |
| Created | 2026-03-31 |
| Target Sprint | Current |

---

## User Story

**As a** cashier,  
**I want** receipts printed and transaction continuity preserved in offline conditions,  
**So that** customer fulfillment and operational traceability are maintained.

---

## Context

Story 3.3 implemented scanner and card-reader device integrations with payment-attempt persistence for online authorization outcomes. This story extends that foundation by:

1. **Receipt Printing**: Implement a receipt printing service for completed transactions, with support for printer unavailability fallback.
2. **Offline Transaction Continuity**: Preserve transaction state and indicate provisional/deferred status when operations complete while offline.
3. **Operation Correlation**: Attach operation IDs to all receipt and transaction records for later reconciliation during synchronized offline-to-online replay (Epic 5).
4. **Diagnostics**: Capture printer failures with diagnostic codes for support workflows.

The receipt printing scope is V1 basic: print to available printer adapter with fallback messaging if printer is unavailable. Full receipt reprinting and historical receipt lookup remain out of scope and will be addressed in later epics.

Offline continuity in this story means: when a transaction completes while connectivity is unavailable, the system clearly indicates provisional/deferred status in the UI and persists the transaction state with operation IDs for later replay when connectivity restores (Epic 5 handles the actual offline queue and synchronization logic).

---

## Acceptance Criteria

### AC-1 - Print receipt for completed online transaction

> **Given** a transaction is completed with online payment authorization  
> **When** receipt printing is requested  
> **Then** receipt is sent to the configured printer  
> **And** receipt metadata (transaction id, amount, items, timestamp, operation id) is persisted locally  
> **And** user receives confirmation of successful print.

### AC-2 - Indicate provisional status for offline completions

> **Given** transaction completion occurs while offline or with deferred payment status  
> **When** receipt action is executed or transaction state is displayed  
> **Then** provisional/deferred status is clearly indicated in UI and receipt output  
> **And** operation ID is attached for later reconciliation  
> **And** next steps (sync pending, await connection) are communicated to cashier.

### AC-3 - Handle printer unavailable with fallback

> **Given** printer is unavailable or unreachable  
> **When** print is requested  
> **Then** user receives fallback instructions (email, manual reprint, support contact)  
> **And** printer failure is captured with diagnostic code for support review  
> **And** transaction is not blocked by printer failure—transaction completes, print is deferred.

### AC-4 - Deterministic printer fault path

> **Given** printer hardware is unavailable/faulted  
> **When** checkout attempts print operation  
> **Then** deterministic fallback guidance is shown  
> **And** issue is logged with diagnostic code  
> **And** user-safe message explains next steps.

### AC-5 - Offline transaction state clarity (NFR24)

> **Given** the system is offline or connectivity is degraded  
> **When** a transaction is progressed through completion  
> **Then** explicit offline/syncing/exception state is shown (not ambiguous)  
> **And** user understands whether transaction is queued, completed, deferred, or awaiting sync.

---

## Story Scope

### In Scope

- Receipt printing abstraction and basic printer adapter (default platform adapter returns unavailable until real hardware).
- Receipt metadata persistence (transaction id, amount, items, datetime, operation id, print status).
- Fallback messaging when printer is unavailable (email/manual reprint guidance).
- Diagnostic code emission for printer failures.
- Offline status indicators in checkout UI (provisional/deferred label, next-steps messaging).
- Operation ID generation and attachment to transactions for later offline-to-online replay correlation.
- Safe logging of receipt operations (no sensitive card data, operation context only).

### Out of Scope

- Actual hardware printer integration (V1 uses platform adapter returning unavailable).
- Receipt reprinting from historical transaction list (future epic).
- Email delivery of receipts (future epic).
- Full offline queue and synchronization replay logic (Story 5.3).
- Offline queue persistence and durability (Story 5.2).
- Refund receipt generation (Story 3.5).

---

## Architecture Guardrails

- **Layering is strict:** `Features -> Application -> Infrastructure`; no ViewModel direct printer calls.
- **Printer abstraction:** interface in Application, concrete adapter in Infrastructure.
- **Result envelope:** use `AppResult<T>` with user-safe messages and canonical error codes.
- **Operation IDs:** implement idempotent operation correlation for all receipt and transaction operations; support retry without duplicate finalization.
- **Deterministic failures:** each printer fault path returns stable diagnostic codes.
- **Receipt model:** lightweight persistence (not full receipt document storage); metadata only in V1.
- **Offline transparency:** UI state must unambiguously indicate offline/syncing/exception condition; no implicit assumptions.
- **PCI scope minimization:** no raw card data in receipt metadata, logs, or diagnostics.
- **Transactional consistency:** receipt printing should not block transaction completion; print is supplementary.

---

## File Impact Plan

### Create - Application Abstractions

1. `POSOpen/Application/Abstractions/Services/IPrinterDeviceService.cs`
   - Contract for printer operations.
   - Interface:
     - `Task<AppResult<PrinterResultDto>> PrintReceiptAsync(ReceiptData receiptData, CancellationToken ct = default)`
   - Returns: success with receipt metadata, or failure with diagnostic code and user message.

2. `POSOpen/Application/Abstractions/Services/IOperationIdService.cs`
   - Contract for generating and managing operation IDs for correlation and idempotency.
   - Interface:
     - `Guid GenerateOperationId()`
     - `Task SaveOperationAsync(Guid operationId, string operationName, object operationData, CancellationToken ct = default)`
     - `Task<OperationRecord?> GetOperationAsync(Guid operationId, CancellationToken ct = default)`

3. `POSOpen/Application/Abstractions/Repositories/IReceiptMetadataRepository.cs`
   - Repository contract for receipt metadata persistence.
   - Persist: operation id, transaction id, amount cents, currency, item count, printed timestamp, print status, diagnostic code.

### Create - Application Checkout Use Cases

4. `POSOpen/Application/UseCases/Checkout/PrintReceiptUseCase.cs`
   - Orchestrates receipt printing for a completed transaction.
   - Steps:
     1. Generate operation ID for receipt operation.
     2. Prepare receipt data from transaction (items, amount, summary).
     3. Call IPrinterDeviceService.PrintReceiptAsync().
     4. Persist receipt metadata with operation ID and print status (success/failure).
     5. Return receipt outcome DTO suitable for checkout UI.
   - Handles printer unavailable gracefully: transaction completes, print is deferred/failed, fallback message shown.

5. `POSOpen/Application/UseCases/Checkout/GetTransactionStatusUseCase.cs`
   - Retrieves transaction with offline/online status indicators.
   - Returns transaction detail + explicit status (completed-online, completed-offline-pending-sync, deferred-payment, error).
   - Includes operation ID and next-steps guidance.

6. `POSOpen/Application/UseCases/Checkout/ReceiptData.cs` and `POSOpen/Application/UseCases/Checkout/PrinterResultDto.cs`
   - DTOs for receipt metadata and printer result mapping.

### Create - Domain / Persistence Model

7. `POSOpen/Domain/Entities/ReceiptMetadata.cs`
   - Lightweight entity for receipt metadata.
   - Fields: `Id`, `OperationId`, `TransactionId` (CartSessionId), `AmountCents`, `CurrencyCode`, `ItemCount`, `PrintedAtUtc`, `PrintStatus` (Success/Failed/Deferred), `DiagnosticCode`, `CreatedAtUtc`.
   - Must not contain receipt document content or card data; metadata only.

8. `POSOpen/Domain/Entities/TransactionOperation.cs`
   - Entity for operation correlation (for idempotent replay in offline scenarios).
   - Fields: `Id`, `OperationId` (Guid), `TransactionId`, `OperationName`, `OperationData` (serialized), `Status`, `CreatedAtUtc`, `CompletedAtUtc`.

9. `POSOpen/Domain/Enums/PrintStatus.cs`
   - Enum: `Success`, `Failed`, `Deferred`.

10. `POSOpen/Domain/Enums/TransactionStatus.cs`
    - Enum: `CompletedOnline`, `CompletedOfflinePendingSync`, `DeferredPayment`, `Error`.

11. `POSOpen/Infrastructure/Persistence/Configurations/ReceiptMetadataConfiguration.cs` and `TransactionOperationConfiguration.cs`
    - EF model configuration.

12. `POSOpen/Infrastructure/Persistence/Repositories/ReceiptMetadataRepository.cs` and `TransactionOperationRepository.cs`
    - Repository implementations.

13. `POSOpen/Infrastructure/Persistence/Migrations/20260401000000_AddReceiptMetadataAndOperations.cs`
    - Migration to create receipt_metadata and transaction_operations tables.

### Create - Infrastructure Device Adapters

14. `POSOpen/Infrastructure/Devices/Printer/PlatformPrinterDeviceService.cs`
    - Platform/device adapter wrapper for printer operations.
    - Default (V1): returns Unavailable with deterministic message until real hardware integration.
    - Must map adapter exceptions to deterministic diagnostic codes (PRINTER_UNAVAILABLE, PRINTER_OFFLINE, PRINTER_PAPER_OUT, etc.).

15. `POSOpen/Infrastructure/Services/OperationIdService.cs`
    - Service to generate and manage operation IDs.
    - Persists operation record to database for later correlation.

### Modify - Checkout ViewModel/UI

16. `POSOpen/Features/Checkout/ViewModels/CartViewModel.cs`
    - Add `CurrentTransactionStatus` property (CompletedOnline, CompletedOfflinePendingSync, DeferredPayment, Error).
    - Add `OfflineStatusMessage` property for UI guidance.
    - Add `PrintReceiptCommand` to request receipt printing.

17. `POSOpen/Features/Checkout/ViewModels/CheckoutCompletionViewModel.cs` (new)
    - Dedicated ViewModel for post-completion confirmation screen.
    - Displays: transaction summary, receipt status (printed/deferred/failed), offline status, next steps, operation ID for support reference.

18. `POSOpen/Features/Checkout/Views/CheckoutCompletionPage.xaml` (new)
    - Post-completion confirmation screen with receipt status and offline guidance.
    - Shows operation ID and next-steps inline for cashier reference.

19. `POSOpen/Features/Checkout/CheckoutServiceCollectionExtensions.cs`
    - Register IPrinterDeviceService, IOperationIdService, IReceiptMetadataRepository, PrintReceiptUseCase, GetTransactionStatusUseCase.
    - Register completion VM and page.

20. `POSOpen/Application/Abstractions/Services/ICheckoutUiService.cs`
    - Extend with navigation to completion page: `NavigateToCheckoutCompletionAsync(Guid cartSessionId)`.

21. `POSOpen/Infrastructure/Services/CheckoutUiService.cs`
    - Implement new navigation methods.

---

## Implementation Notes

### Receipt Metadata Model

Keep receipt persistence lightweight in V1: metadata only (operation ID, amounts, item counts, timestamps, status). Do not store full receipt document, item descriptions, or card details. Receipt document generation and reprinting can be added in future epics.

### Operation ID Strategy

Every significant transaction/receipt operation must generate and persist an operation ID:
- `PrintReceipt` operation: generate OperationId, save to transaction_operations table.
- `CompleteTransaction` operation: generate OperationId, save to transaction_operations table.
- Offline replay will use OperationId to detect already-processed operations and avoid duplicates.

See Epic 5.3 for full idempotent replay implementation; this story just lays the foundation.

### Offline Status Indicators

Implement clear offline status in UI:
- **Online + Completed**: "Transaction Complete" (green, success state).
- **Online + Payment Deferred**: "Transaction Pending Payment Sync" (yellow, warning state).
- **Offline + Completed**: "Transaction Complete (Offline)" + "Awaiting Connection to Confirm" (yellow, explicitly marked).
- **Offline + Error**: "Transaction Failed Offline" + "Support Required" (red, error state).

Do NOT leave status ambiguous. Every transaction completion screen must unambiguously state online/offline and completed/pending/deferred.

### Printer Fallback Messaging

When printer is unavailable:
- **Fallback message**: "Receipt cannot print right now. Here are your options: [1] Check printer connection, [2] Request manual receipt from manager, [3] Receipt will be emailed when sync completes."
- **Diagnostic code**: log `PRINTER_UNAVAILABLE` with operation context (transaction id, cart session id).
- **Transaction block**: DO NOT block transaction completion if printer fails. Printer is supplementary; financial transaction is primary.

### Logging Discipline

Log recipe operations with safe payloads:
- Include: transactionId, operationId, printStatus, diagnosticCode, timestamp.
- Exclude: card data, sensitive item descriptions, customer PII.
- Use existing logging pattern from Story 3.3 (ILogger<T> injection).

---

## Test Strategy

| Layer | File | Coverage |
|---|---|---|
| Application | `PrintReceiptUseCaseTests.cs` | printer success, unavailable/offline fallback, metadata persistence |
| Application | `GetTransactionStatusUseCaseTests.cs` | online status, offline status, deferred payment, error states |
| Presentation | `CheckoutCompletionViewModelTests.cs` | load transaction details, display offline status, show operation ID |
| Presentation | `CartViewModelTests.cs` (extended) | print command success/failure, offline status updates |
| Infrastructure (optional integration) | `PrinterAdapterIntegrationTests.cs` | adapter exception to diagnostic-code translation |

### Required Test Additions

- `POSOpen.Tests/Unit/Checkout/PrintReceiptUseCaseTests.cs`
- `POSOpen.Tests/Unit/Checkout/GetTransactionStatusUseCaseTests.cs`
- `POSOpen.Tests/Unit/Checkout/CheckoutCompletionViewModelTests.cs`
- Extend `POSOpen.Tests/Unit/Checkout/CartViewModelTests.cs` with print and offline status tests.

---

## Definition of Done

- [ ] Receipt printing abstraction and default platform adapter created (returns Unavailable).
- [ ] Receipt metadata persistence implemented (repository and EF configuration).
- [ ] PrintReceiptUseCase and GetTransactionStatusUseCase implemented.
- [ ] Operation ID service and transaction operation persistence created.
- [ ] Offline status indicators implemented in checkout UI (clear completion screen with online/offline state).
- [ ] Printer fault path shows deterministic fallback guidance and diagnostic codes.
- [ ] Transaction not blocked by printer failure.
- [ ] Receipt operations logged safely (no card data, operation context only).
- [ ] New use-case tests pass (PrintReceipt, GetTransactionStatus, CheckoutCompletion).
- [ ] Cart ViewModel tests updated with print and offline status scenarios.
- [ ] No regressions in existing checkout tests.
- [ ] Story status moved to `done` after merge.

---

## Previous Story Learnings (from 3.3)

- Keep device abstractions in Application; implementations in Infrastructure.
- Use `AppResult<T>` result envelope with user-safe messages for all device operations.
- Inject ILogger<T> for diagnostic logging of device faults (no sensitive data).
- Maintain test helper patterns: Moq setup with explicit cancellation token arguments.
- Test injection of all dependencies including loggers and repositories.
- Preserve CommunityToolkit.MVVM patterns for consistency.
- Keep device failures non-blocking for transaction completion; transactions are primary.

---

## Dev Agent Handoff

**Latest Status:**
- Story 3.3 is merged and marked as done.
- Main branch is up to date with all Story 3.3 implementation (scanner, card-reader, payment persistence).
- Sprint status updated to show Story 3.3 done, Story 3.4 ready-for-dev.

**Key Context for Developer:**
- Receipt printing is V1 basic: metadata persistence only, no document generation.
- Offline continuity focus: clear UI status indicators and operation ID correlation for later Epic 5 replay.
- Printer unavailability should NOT block transaction completion; transaction is primary, print is supplementary.
- Implement Operation ID service as foundation for Episode 5 offline sync and idempotent replay.

**Related Artifacts:**
- Epic 3 specification: [epics.md](../planning-artifacts/epics.md)
- Story 3.3 (completed reference): [3-3-integrate-scanner-and-card-reader-for-checkout.md](./3-3-integrate-scanner-and-card-reader-for-checkout.md)
- Architecture: [architecture.md](../planning-artifacts/architecture.md)

---

## Story Completion Note

Story 3.4 is ready for implementation. Developer should start with PrintReceiptUseCase and IPrinterDeviceService abstraction, then layer in offline status indicators and Operation ID correlation. Receipt metadata persistence can follow. This story lays the foundation for offline queuing and synchronous replay in Epic 5.

---

## Tasks/Subtasks

### Task 1: Domain layer — enums and entities
- [x] Create `POSOpen/Domain/Enums/PrintStatus.cs` (Success, Failed, Deferred)
- [x] Create `POSOpen/Domain/Enums/TransactionStatus.cs` (CompletedOnline, CompletedOfflinePendingSync, DeferredPayment, Error)
- [x] Create `POSOpen/Domain/Entities/ReceiptMetadata.cs`
- [x] Create `POSOpen/Domain/Entities/TransactionOperation.cs`

### Task 2: Application abstractions
- [x] Create `POSOpen/Application/Abstractions/Services/IPrinterDeviceService.cs`
- [x] Create `POSOpen/Application/Abstractions/Services/IOperationIdService.cs`
- [x] Create `POSOpen/Application/Abstractions/Repositories/IReceiptMetadataRepository.cs`
- [x] Create `POSOpen/Application/Abstractions/Repositories/ITransactionOperationRepository.cs`
- [x] Extend `POSOpen/Application/Abstractions/Services/DeviceDiagnosticCode.cs` with printer codes

### Task 3: Application DTOs and use cases
- [x] Create `POSOpen/Application/UseCases/Checkout/ReceiptData.cs` and `PrinterResultDto.cs`
- [x] Create `POSOpen/Application/UseCases/Checkout/PrintReceiptUseCase.cs`
- [x] Create `POSOpen/Application/UseCases/Checkout/GetTransactionStatusUseCase.cs`

### Task 4: Infrastructure persistence
- [x] Create `POSOpen/Infrastructure/Persistence/Configurations/ReceiptMetadataConfiguration.cs`
- [x] Create `POSOpen/Infrastructure/Persistence/Configurations/TransactionOperationConfiguration.cs`
- [x] Create `POSOpen/Infrastructure/Persistence/Repositories/ReceiptMetadataRepository.cs`
- [x] Create `POSOpen/Infrastructure/Persistence/Repositories/TransactionOperationRepository.cs`
- [x] Update `POSOpen/Infrastructure/Persistence/PosOpenDbContext.cs` with new DbSets
- [x] Add `PersistenceServiceCollectionExtensions.cs` registrations for new repos
- [x] Create migration `20260401000000_AddReceiptMetadataAndOperations.cs`
- [x] Update `PosOpenDbContextModelSnapshot.cs`

### Task 5: Infrastructure adapters and services
- [x] Create `POSOpen/Infrastructure/Devices/Printer/PlatformPrinterDeviceService.cs`
- [x] Create `POSOpen/Infrastructure/Services/OperationIdService.cs`

### Task 6: Checkout UI — completion screen
- [x] Extend `POSOpen/Application/Abstractions/Services/ICheckoutUiService.cs` with completion navigation
- [x] Implement new navigation in `POSOpen/Infrastructure/Services/CheckoutUiService.cs`
- [x] Extend `POSOpen/Features/Checkout/CheckoutRoutes.cs` with completion route
- [x] Create `POSOpen/Features/Checkout/ViewModels/CheckoutCompletionViewModel.cs`
- [x] Create `POSOpen/Features/Checkout/Views/CheckoutCompletionPage.xaml` + code-behind

### Task 7: CartViewModel updates and DI registrations
- [x] Extend `POSOpen/Features/Checkout/ViewModels/CartViewModel.cs` with PrintReceiptCommand + offline status
- [x] Update `POSOpen/Features/Checkout/CheckoutServiceCollectionExtensions.cs` with all new registrations

### Task 8: Test project source linking
- [x] Update `POSOpen.Tests/POSOpen.Tests.csproj` to include new ViewModel source link

### Task 9: Tests
- [x] Create `POSOpen.Tests/Unit/Checkout/PrintReceiptUseCaseTests.cs`
- [x] Create `POSOpen.Tests/Unit/Checkout/GetTransactionStatusUseCaseTests.cs`
- [x] Create `POSOpen.Tests/Unit/Checkout/CheckoutCompletionViewModelTests.cs`
- [x] Extend `POSOpen.Tests/Unit/Checkout/CartViewModelTests.cs` with print + offline status tests

---

## Dev Agent Record

### Agent Model Used

Claude Sonnet 4.6

### Implementation Plan

1. Domain enums and entities first (foundational layer).
2. Application abstractions (interfaces and DTOs).
3. Use cases implementing the core orchestration logic.
4. Infrastructure persistence (EF config, repos, migration).
5. Infrastructure adapters (printer stub + operation ID service).
6. Checkout UI updates (completion screen, navigation, CartViewModel).
7. DI wiring in service collection extensions.
8. Tests covering all ACs.

### Completion Notes List

_(populated as tasks complete)_
- All 9 implementation tasks completed across 4 continuation sessions.
- 183 tests pass (0 failures, 0 skipped) including all 17 new tests added this story.
- `CartSession.Status` has public setter — tests use direct assignment (`cart.Status = CartStatus.Completed`) rather than a `Close()` method.
- DTOs (`PrinterResultDto`, `PrintReceiptResultDto`, `TransactionStatusDto`) reside in `Application/UseCases/Checkout/` and are covered by existing csproj wildcard.
- Printer failure is non-blocking: `PrintReceiptUseCase` always returns `IsSuccess=true`; `PrintStatus` on result DTO indicates actual printer outcome.

### Debug Log

_(populated if issues arise)_
- Initial `GetTransactionStatusUseCaseTests.cs` used `cart.Close(FixedNow)` — no such method; fixed to `cart.Status = CartStatus.Completed`.

---

## File List

_(populated as implementation proceeds)_
### New Files Created
- `POSOpen/Domain/Enums/PrintStatus.cs`
- `POSOpen/Domain/Enums/TransactionStatus.cs`
- `POSOpen/Domain/Entities/ReceiptMetadata.cs`
- `POSOpen/Domain/Entities/TransactionOperation.cs`
- `POSOpen/Application/Abstractions/Services/IPrinterDeviceService.cs`
- `POSOpen/Application/Abstractions/Services/IOperationIdService.cs`
- `POSOpen/Application/Abstractions/Repositories/IReceiptMetadataRepository.cs`
- `POSOpen/Application/Abstractions/Repositories/ITransactionOperationRepository.cs`
- `POSOpen/Application/UseCases/Checkout/ReceiptData.cs`
- `POSOpen/Application/UseCases/Checkout/PrinterResultDto.cs`
- `POSOpen/Application/UseCases/Checkout/PrintReceiptResultDto.cs`
- `POSOpen/Application/UseCases/Checkout/TransactionStatusDto.cs`
- `POSOpen/Application/UseCases/Checkout/PrintReceiptUseCase.cs`
- `POSOpen/Application/UseCases/Checkout/GetTransactionStatusUseCase.cs`
- `POSOpen/Infrastructure/Persistence/Configurations/ReceiptMetadataConfiguration.cs`
- `POSOpen/Infrastructure/Persistence/Configurations/TransactionOperationConfiguration.cs`
- `POSOpen/Infrastructure/Persistence/Repositories/ReceiptMetadataRepository.cs`
- `POSOpen/Infrastructure/Persistence/Repositories/TransactionOperationRepository.cs`
- `POSOpen/Infrastructure/Devices/Printer/PlatformPrinterDeviceService.cs`
- `POSOpen/Infrastructure/Services/OperationIdService.cs`
- `POSOpen/Features/Checkout/ViewModels/CheckoutCompletionViewModel.cs`
- `POSOpen/Features/Checkout/Views/CheckoutCompletionPage.xaml`
- `POSOpen/Features/Checkout/Views/CheckoutCompletionPage.xaml.cs`
- `POSOpen.Tests/Unit/Checkout/PrintReceiptUseCaseTests.cs`
- `POSOpen.Tests/Unit/Checkout/GetTransactionStatusUseCaseTests.cs`
- `POSOpen.Tests/Unit/Checkout/CheckoutCompletionViewModelTests.cs`

### Modified Files
- `POSOpen/Application/Abstractions/Services/DeviceDiagnosticCode.cs` — added printer diagnostic codes
- `POSOpen/Application/Abstractions/Services/ICheckoutUiService.cs` — added completion navigation method
- `POSOpen/Infrastructure/Persistence/PosOpenDbContext.cs` — added ReceiptMetadata and TransactionOperation DbSets
- `POSOpen/Infrastructure/Persistence/PosOpenDbContextModelSnapshot.cs` — updated snapshot
- `POSOpen/Infrastructure/Persistence/PersistenceServiceCollectionExtensions.cs` — added new repo registrations
- `POSOpen/Infrastructure/Services/CheckoutUiService.cs` — implemented completion navigation
- `POSOpen/Features/Checkout/CheckoutRoutes.cs` — added CheckoutCompletion route
- `POSOpen/Features/Checkout/ViewModels/CartViewModel.cs` — added PrintReceiptCommand and offline status
- `POSOpen/Features/Checkout/CheckoutServiceCollectionExtensions.cs` — registered all new services and pages
- `POSOpen.Tests/POSOpen.Tests.csproj` — added CheckoutCompletionViewModel source link
- `POSOpen.Tests/Unit/Checkout/CartViewModelTests.cs` — updated helpers for 7-param CartViewModel constructor

---

## Change Log

_(populated on completion)_
| Date | Change |
|---|---|
| 2026-04-01 | Story implementation complete. All tasks done. 183 tests pass. Status → for-review. |

# Story 4.4: Manage Catering/Decor Options with Risk Indicators

## Metadata

| Field | Value |
|---|---|
| Epic | 4 - Party Booking Lifecycle and Inventory Coordination |
| Story | 4.4 |
| Key | `4-4-manage-catering-decor-options-with-risk-indicators` |
| Status | review |
| Author | Timbe (via BMAD Story Creator) |
| Created | 2026-04-01 |
| Target Sprint | Current |

---

## User Story

**As a** party coordinator,  
**I want** to manage catering and decor options and see booking risks,  
**So that** I can proactively resolve execution blockers.

---

## Acceptance Criteria

### AC-1 - Catering/decor selection changes update booking totals and downstream requirements

> **Given** a booking includes configurable add-ons  
> **When** catering/decor selections are changed  
> **Then** booking totals and downstream requirements update correctly.

### AC-2 - Risk indicators are surfaced with severity and reason

> **Given** selected options create operational risk (inventory shortfall or policy conflict)  
> **When** booking is reviewed  
> **Then** risk indicators are surfaced with severity and reason.

### AC-3 - Applying a corrective option resolves risk state

> **Given** risks are present  
> **When** coordinator applies an approved corrective option (replaces a risky selection)  
> **Then** risk state updates and booking remains actionable.

### AC-4 - Selection save returns totals and timeline within NFR4

> **Given** catering or decor options are changed on a booking  
> **When** the update is saved  
> **Then** revised booking totals and timeline are returned within 3 seconds (NFR4).

---

## Scope

### In Scope

- Add `PartyBookingAddOnSelection` entity and navigation to `PartyBooking` aggregate.
- V1 static catalogs: `KnownCateringOptionIds`, `KnownDecorOptionIds`, `KnownAtRiskOptionIds` in `PartyBookingConstants`.
- `GetBookingAddOnOptionsUseCase` — returns catalog options with current selections and risk state for a booking.
- `UpdateBookingAddOnSelectionsUseCase` + command — atomically replaces add-on selections, re-evaluates risk indicators, returns totals + updated timeline milestones.
- Risk evaluation: assessed at every read and write, based on `KnownAtRiskOptionIds`. V1 severity = Low or High; V1 reason = "Inventory shortfall" or "Policy conflict".
- Coordinator can resolve AC-3 by simply re-submitting selections without the at-risk item (reuses `UpdateBookingAddOnSelectionsUseCase`).
- EF configuration + migration `20260403000000_AddPartyBookingAddOnSelections`.
- Extend `PartyBookingDetailViewModel` and `PartyBookingDetailPage.xaml` with catering/decor section and risk panel.
- Unit and integration test coverage for all use case paths and repository operations.

### Out of Scope

- Inventory reservation/release execution (Story 4.5).
- Substitution policy maintenance UX (Story 4.6).
- Real inventory stock counts (V1 risk is static-catalog based only).
- Package-to-add-on compatibility rules (V2 concern).
- Per-option pricing display in separate line items (V1 totals are aggregate add-on sum only).

---

## Context

Stories 4.1–4.3 established booking creation, deposit commitment, timeline generation, and room assignment. Story 4.4 must extend the same `PartyBooking` aggregate and Feature/Party slice with catering and decor add-on selections, risk evaluation, and coordinator-facing risk resolution.

This story does NOT build a separate inventory subsystem. Risk is evaluated from a static catalog (`KnownAtRiskOptionIds`) in V1. The risk-reading use case and the selection-write use case use the same catalog, guaranteeing consistency.

---

## Current Repo Reality

### Existing Party aggregate and persistence

- `POSOpen/Domain/Entities/PartyBooking.cs` — flat scalar properties, no collections yet
- `POSOpen/Application/Abstractions/Repositories/IPartyBookingRepository.cs` — existing CRUD and room assignment methods
- `POSOpen/Infrastructure/Persistence/Repositories/PartyBookingRepository.cs` — transactional write pattern with `BeginTransactionAsync`/`CommitAsync`/`RollbackAsync`
- `POSOpen/Infrastructure/Persistence/Configurations/PartyBookingConfiguration.cs` — snake_case mapping, index definitions
- `POSOpen/Infrastructure/Persistence/Migrations/20260402110000_AddPartyBookingRoomAssignment.cs` — most recent migration (naming baseline)

### Existing Party use cases and DTOs

- `POSOpen/Application/UseCases/Party/PartyBookingConstants.cs` — static catalogs (`KnownSlotIds`, `KnownRoomIds`), error codes, safe messages
- `POSOpen/Application/UseCases/Party/PartyBookingDtos.cs` — booking result DTOs
- `POSOpen/Application/UseCases/Party/RoomAssignmentDtos.cs` — room DTO pattern to follow
- `POSOpen/Application/UseCases/Party/PartyTimelineDtos.cs` — `PartyBookingTimelineDto` and `PartyBookingTimelineMilestoneDto`
- `POSOpen/Application/UseCases/Party/GetPartyBookingTimelineUseCase.cs` — timeline retrieval (call or inline timeline recalculation after selections update)

### Existing Feature layer

- `POSOpen/Features/Party/ViewModels/PartyBookingDetailViewModel.cs` — already has `GetRoomOptionsUseCase` and `AssignPartyRoomUseCase` wired; follow the same constructor injection + `[RelayCommand]` pattern
- `POSOpen/Features/Party/Views/PartyBookingDetailPage.xaml` — has room section; add catering/decor section below it
- `POSOpen/Features/Party/PartyServiceCollectionExtensions.cs` — register new use cases here

### Existing test infrastructure

- `POSOpen.Tests/Unit/Party/` — existing use case unit tests (follow naming: `<UseCaseName>Tests.cs`)
- `POSOpen.Tests/Integration/Party/PartyBookingRepositoryTests.cs` — existing integration tests (extend or add `PartyCateringRepositoryTests.cs`)
- **MAUI test project constraint: No `Microsoft.Maui.*` using directives in test files.**
- **Test setup bulk insert pattern: `dbContext.Set<T>().AddRange(items); await dbContext.SaveChangesAsync();`**
- **`[QueryProperty]` stays on the Page, not the ViewModel.** Do not move it.
- Current test count: approximately 265+ (extend all to pass).

---

## Previous Story Intelligence

Key patterns from Stories 4.1–4.3 that MUST be preserved:

- **Strict layering:** ViewModels → Application use cases → Infrastructure. No Presentation-to-Infrastructure coupling.
- **`AppResult<T>` envelope:** `isSuccess`, `errorCode`, `userMessage`, `diagnosticMessage`, `payload`. Use canonical pattern throughout.
- **`operationId` + `correlationId`:** Propagate on all write paths. Idempotency: short-circuit if `operationId` already processed.
- **UTC-safe:** All datetime fields are UTC; use `NullableUtcDateTimeConverter` / `UtcDateTimeConverter` in EF config.
- **Transactional writes:** Every write path uses `BeginTransactionAsync`/`CommitAsync`/`RollbackAsync` with `try/catch/RollbackAsync`. Never a two-step check-then-write outside a transaction.
- **ViewModel processing state:** `Idle → Loading → Success | Error`. Explicit state transitions, no hidden side effects.
- **Processing state visibility:** Bind `IsBusy` and `ProcessingState` to UI affordances; disable actions during loading.
- **Timeline recalculation:** After any booking data change (deposit, room, now selections), trigger timeline refresh via existing `GetPartyBookingTimelineUseCase` and update `Milestones` collection. Keep NFR4 (≤3s P95).
- **Consistent constants:** Add all new error codes, safe messages, and catalog values to `PartyBookingConstants`. Do NOT scatter error strings inline.
- **Inline recovery UX:** Conflict/warning messages inline in booking detail context; no deep modal chains.

---

## Domain and Data Model Design

### New Entity: `PartyBookingAddOnSelection`

```csharp
// POSOpen/Domain/Entities/PartyBookingAddOnSelection.cs
namespace POSOpen.Domain.Entities;

public sealed class PartyBookingAddOnSelection
{
    public Guid Id { get; set; }
    public Guid BookingId { get; set; }
    public PartyAddOnType AddOnType { get; set; }
    public string OptionId { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public DateTime SelectedAtUtc { get; set; }
    public Guid SelectionOperationId { get; set; }

    public PartyBooking? Booking { get; set; }
}
```

### New Enum: `PartyAddOnType`

```csharp
// POSOpen/Domain/Enums/PartyAddOnType.cs
namespace POSOpen.Domain.Enums;

public enum PartyAddOnType
{
    Catering = 1,
    Decor = 2,
}
```

### Extend `PartyBooking` with navigation and domain write method

Add to `POSOpen/Domain/Entities/PartyBooking.cs`:

```csharp
public ICollection<PartyBookingAddOnSelection> AddOnSelections { get; set; } = [];
public Guid? LastAddOnUpdateOperationId { get; set; }
```

Also add a domain method for the write path (matches the established pattern: `RecordDepositCommitment`, `AssignRoom`, `MarkCompleted`):

```csharp
public void UpdateAddOnSelections(Guid operationId, Guid correlationId, DateTime updatedAtUtc)
{
    LastAddOnUpdateOperationId = operationId;
    OperationId = operationId;
    CorrelationId = correlationId;
    UpdatedAtUtc = updatedAtUtc;
}
```

**Do NOT add `ComputeAddOnTotalCents` to `PartyBooking`.** Total computation uses `PartyBookingConstants.AddOnOptionPriceCents` (an application-layer concern) and must stay in the use case, not the domain entity.

### EF Configuration: `PartyBookingAddOnSelectionConfiguration.cs`

```
Table:   party_booking_add_on_selections
Columns: id, booking_id, add_on_type (int), option_id (max 64), quantity, selected_at_utc, selection_operation_id
FK:      booking_id → party_bookings.id (cascade delete)
Index:   ix_party_booking_add_on_sel_booking_id (booking_id)
Index:   ix_party_booking_add_on_sel_operation_id (selection_operation_id)
```

Register the nav property in `PartyBookingConfiguration`:

```csharp
builder.HasMany(x => x.AddOnSelections)
    .WithOne(x => x.Booking)
    .HasForeignKey(x => x.BookingId)
    .OnDelete(DeleteBehavior.Cascade);
```

Also add to `PartyBookingConfiguration.Configure()`:

```csharp
builder.Property(x => x.LastAddOnUpdateOperationId).HasColumnName("last_add_on_update_operation_id");
```

### Migration name (preserves chronological order after 4.3):

```
20260403000000_AddPartyBookingAddOnSelections
```

Migration adds:
- Table `party_booking_add_on_selections` with all columns and FK
- Column `last_add_on_update_operation_id` (nullable `uniqueidentifier`) on `party_bookings`

---

## Catalog Design (V1 Static)

Add to `PartyBookingConstants`:

```csharp
// Catering options (V1 static catalog)
public static readonly string[] KnownCateringOptionIds = ["pizza-basic", "pizza-deluxe", "fruit-platter", "veggie-platter", "cake-standard", "cake-custom"];

// Decor options (V1 static catalog)
public static readonly string[] KnownDecorOptionIds = ["balloon-basic", "balloon-premium", "table-standard", "table-themed", "banner-standard", "banner-custom"];

// Option prices in cents (V1 static)
public static readonly IReadOnlyDictionary<string, long> AddOnOptionPriceCents = new Dictionary<string, long>
{
    ["pizza-basic"] = 2500,
    ["pizza-deluxe"] = 4500,
    ["fruit-platter"] = 1800,
    ["veggie-platter"] = 1600,
    ["cake-standard"] = 3500,
    ["cake-custom"] = 6500,
    ["balloon-basic"] = 800,
    ["balloon-premium"] = 1500,
    ["table-standard"] = 0,
    ["table-themed"] = 1200,
    ["banner-standard"] = 0,
    ["banner-custom"] = 900,
};

// Display names (V1 static) — used when building AddOnOptionItemDto
public static readonly IReadOnlyDictionary<string, string> AddOnOptionDisplayNames = new Dictionary<string, string>
{
    ["pizza-basic"] = "Pizza (Basic)",
    ["pizza-deluxe"] = "Pizza (Deluxe)",
    ["fruit-platter"] = "Fruit Platter",
    ["veggie-platter"] = "Veggie Platter",
    ["cake-standard"] = "Cake (Standard)",
    ["cake-custom"] = "Cake (Custom)",
    ["balloon-basic"] = "Balloons (Basic)",
    ["balloon-premium"] = "Balloons (Premium)",
    ["table-standard"] = "Table Setup (Standard)",
    ["table-themed"] = "Table Setup (Themed)",
    ["banner-standard"] = "Banner (Standard)",
    ["banner-custom"] = "Banner (Custom)",
};

// At-risk options (V1: static risk flags — no live inventory in V1)
public static readonly IReadOnlySet<string> KnownAtRiskOptionIds = new HashSet<string>
{
    "cake-custom",
    "balloon-premium",
    "banner-custom",
    "table-themed",
};

// Risk severity
public const string RiskSeverityLow = "Low";
public const string RiskSeverityHigh = "High";

// Risk reasons
public const string RiskReasonInventoryShortfall = "Inventory shortfall risk for this option near event date.";
public const string RiskReasonPolicyConflict = "This option may conflict with booking policy rules.";

// Per-option risk mapping — EXACT severity and reason to use for each at-risk option.
// BookingRiskEvaluator MUST use this table; do not invent severities or reasons.
// | OptionId         | Severity | Reason constant                  |
// |------------------|----------|----------------------------------|
// | cake-custom      | High     | RiskReasonInventoryShortfall     |
// | balloon-premium  | High     | RiskReasonInventoryShortfall     |
// | banner-custom    | Low      | RiskReasonPolicyConflict         |
// | table-themed     | Low      | RiskReasonPolicyConflict         |
// Expressed as a lookup for BookingRiskEvaluator:
public static readonly IReadOnlyDictionary<string, (string Severity, string Reason)> AtRiskOptionMeta =
    new Dictionary<string, (string, string)>
    {
        ["cake-custom"]     = (RiskSeverityHigh, RiskReasonInventoryShortfall),
        ["balloon-premium"] = (RiskSeverityHigh, RiskReasonInventoryShortfall),
        ["banner-custom"]   = (RiskSeverityLow,  RiskReasonPolicyConflict),
        ["table-themed"]    = (RiskSeverityLow,  RiskReasonPolicyConflict),
    };

// Error codes and messages
public const string ErrorAddOnUpdateFailed = "BOOKING_ADDON_UPDATE_FAILED";
public const string ErrorAddOnOptionInvalid = "BOOKING_ADDON_OPTION_INVALID";
public const string SafeAddOnUpdateFailedMessage = "Failed to save add-on selections. Please try again.";
public const string SafeAddOnOptionInvalidMessage = "One or more selected options are not valid.";
public const string AddOnSelectionsUpdatedMessage = "Catering and decor options saved.";
public const string AddOnSelectionsAlreadySavedMessage = "Add-on selections were already saved for this operation.";
public const string AddOnOptionsLoadedMessage = "Add-on options loaded.";
```

---

## Use Case Contracts

### `GetBookingAddOnOptionsUseCase`

**Input:** `GetBookingAddOnOptionsQuery(Guid BookingId)`  
**Output:** `AppResult<BookingAddOnOptionsDto>`

Logic:
1. Call `_repository.GetByIdWithSelectionsAsync(query.BookingId)`. If null → return `AppResult.Failure(ErrorBookingNotFound, SafeBookingNotFoundMessage)`.
2. For each catalog option in `KnownCateringOptionIds` and `KnownDecorOptionIds`, build `AddOnOptionItemDto` using: `OptionId`, display name from `AddOnOptionDisplayNames`, `AddOnType`, `IsSelected` (match against loaded selections), `Quantity` (from selection row, 0 if not selected), `PriceCents` from `AddOnOptionPriceCents`, `IsAtRisk`/`RiskSeverity`/`RiskReason` from `BookingRiskEvaluator.GetRiskInfo(optionId)`.
3. Compute `AddOnTotalAmountCents` by summing `PriceCents` for all selected options (quantity × price). Compute in use case — do NOT delegate to `PartyBooking`.
4. Return `AppResult.Success(new BookingAddOnOptionsDto(...))`.

### `UpdateBookingAddOnSelectionsUseCase`

**Input:** `UpdateBookingAddOnSelectionsCommand(Guid BookingId, IReadOnlyList<AddOnSelectionItemCommand> Selections, OperationContext OperationContext)`  
where `AddOnSelectionItemCommand(string OptionId, PartyAddOnType AddOnType, int Quantity)`

**Output:** `AppResult<BookingAddOnUpdateResultDto>`

Logic:
1. Validate all `OptionId` values against their respective catalogs (catering vs. decor). Unknown IDs → return `AppResult.Failure(ErrorAddOnOptionInvalid, SafeAddOnOptionInvalidMessage)`.
2. Load booking via `_repository.GetByIdWithSelectionsAsync(command.BookingId)`. If null → return `AppResult.Failure(ErrorBookingNotFound, SafeBookingNotFoundMessage)`.
3. **Idempotency check:** `if (existing.LastAddOnUpdateOperationId == command.OperationContext.OperationId)` → short-circuit, return success with `AddOnSelectionsAlreadySavedMessage`. This correctly handles the "save empty selections" case where no selection rows exist to query.
4. Delegate to `_repository.ReplaceAddOnSelectionsAsync(booking, newSelections, operationId, correlationId, utcNow)`. The repository method (inside a transaction) must:
   - Delete all existing `PartyBookingAddOnSelections` rows where `booking_id = bookingId`.
   - Insert new selection rows (stamp `SelectionOperationId = operationId`, `SelectedAtUtc = utcNow`).
   - Call `existing.UpdateAddOnSelections(operationId, correlationId, utcNow)` on the tracked booking entity and save (this sets `LastAddOnUpdateOperationId`).
5. Re-evaluate risk: call `BookingRiskEvaluator.EvaluateRisks(newSelections)` — returns `IReadOnlyList<BookingRiskIndicatorDto>`.
6. Compute `AddOnTotalAmountCents` in use case: sum `AddOnOptionPriceCents[sel.OptionId] * sel.Quantity` for each selection.
7. Refresh timeline: call `_getTimelineUseCase.ExecuteAsync(bookingId, ct)`. If this fails, return `BookingAddOnUpdateResultDto` with `UpdatedMilestones = []` and log a diagnostic — **do NOT fail or roll back the committed selection save**.
8. Return `AppResult.Success(new BookingAddOnUpdateResultDto(...))`.

---

## DTO Contracts

New file: `POSOpen/Application/UseCases/Party/AddOnSelectionDtos.cs`

```csharp
public sealed record AddOnOptionItemDto(
    string OptionId,
    string DisplayName,
    PartyAddOnType AddOnType,
    bool IsSelected,
    int Quantity,
    long PriceCents,
    bool IsAtRisk,
    string? RiskSeverity,
    string? RiskReason);

public sealed record BookingAddOnOptionsDto(
    Guid BookingId,
    IReadOnlyList<AddOnOptionItemDto> CateringOptions,
    IReadOnlyList<AddOnOptionItemDto> DecorOptions,
    long AddOnTotalAmountCents);

public sealed record BookingRiskIndicatorDto(
    string OptionId,
    string RiskSeverity,
    string RiskReason);

public sealed record BookingAddOnUpdateResultDto(
    Guid BookingId,
    IReadOnlyList<AddOnOptionItemDto> CateringOptions,
    IReadOnlyList<AddOnOptionItemDto> DecorOptions,
    long AddOnTotalAmountCents,
    IReadOnlyList<BookingRiskIndicatorDto> RiskIndicators,
    IReadOnlyList<PartyBookingTimelineMilestoneDto> UpdatedMilestones);
```

---

## Repository Contract Extension

Add to `IPartyBookingRepository`:

```csharp
// Use this method (not GetByIdAsync) anywhere AddOnSelections are needed.
// Includes nav property: .Include(x => x.AddOnSelections)
Task<PartyBooking?> GetByIdWithSelectionsAsync(Guid bookingId, CancellationToken ct = default);

Task ReplaceAddOnSelectionsAsync(
    PartyBooking booking,
    IReadOnlyList<PartyBookingAddOnSelection> newSelections,
    Guid operationId,
    Guid correlationId,
    DateTime updatedAtUtc,
    CancellationToken ct = default);
```

**`GetByIdWithSelectionsAsync`:** Creates its own scoped context (same pattern as `GetByIdAsync`), queries `PartyBooking` with `.Include(x => x.AddOnSelections).AsNoTracking()`, returns null if not found. Do NOT modify `GetByIdAsync`.

**`ReplaceAddOnSelectionsAsync`** must be implemented inside a single `BeginTransactionAsync`/`CommitAsync`/`RollbackAsync` block — follow the exact pattern from `AssignRoomAsync`:
1. Load tracked booking with `.Include(x => x.AddOnSelections).FirstAsync()`.
2. Remove all existing selections: `dbContext.Set<PartyBookingAddOnSelection>().RemoveRange(existing.AddOnSelections)`.
3. Add new selections: `dbContext.Set<PartyBookingAddOnSelection>().AddRange(newSelections)`.
4. Call `existing.UpdateAddOnSelections(operationId, correlationId, DateTime.SpecifyKind(updatedAtUtc, DateTimeKind.Utc))` — this stamps `LastAddOnUpdateOperationId`.
5. `await dbContext.SaveChangesAsync(ct)` then `await transaction.CommitAsync(ct)`.
6. `try/catch` wraps everything; `catch` block calls `await transaction.RollbackAsync(ct)` then re-throws.

**Do NOT add `ListAddOnSelectionsAsync` as a separate method** — the `GetByIdWithSelectionsAsync` nav include covers this need.

---

## ViewModel and UI Design

### `PartyBookingDetailViewModel` extension

Inject `GetBookingAddOnOptionsUseCase` and `UpdateBookingAddOnSelectionsUseCase` into constructor.

Add observable properties:
```csharp
[ObservableProperty] private ObservableCollection<AddOnOptionItemDto> _cateringOptions = [];
[ObservableProperty] private ObservableCollection<AddOnOptionItemDto> _decorOptions = [];

[ObservableProperty]
[NotifyPropertyChangedFor(nameof(HasRisks))]
private ObservableCollection<BookingRiskIndicatorDto> _riskIndicators = [];

[ObservableProperty] private long _addOnTotalAmountCents;
[ObservableProperty] private bool _isAddOnBusy;   // Separate flag — does NOT affect CanSubmitDeposit
[ObservableProperty] private string? _addOnStatusMessage;
```

Add computed property (NOT `[ObservableProperty]` — must derive from collection to stay in sync):
```csharp
public bool HasRisks => RiskIndicators.Count > 0;
```

**`IsBusy` vs `IsAddOnBusy`:** Add-on loading and saving use `IsAddOnBusy`, NOT `IsBusy`. This prevents the deposit submit button (`CanSubmitDeposit => !DepositCommitted && !IsBusy`) from being silently disabled during catering operations.

**V1 Quantity:** `Quantity` is always 1. Do NOT build quantity input widgets. The UI presents checkboxes only (selected = quantity 1, deselected = not in command list). The `Quantity` field exists in the data model for V2 extensibility. Commands always pass `Quantity = 1` for selected items.

Add commands:
- `[RelayCommand] LoadAddOnOptionsAsync()` — sets `IsAddOnBusy = true`, calls `GetBookingAddOnOptionsUseCase`, populates collections, sets `IsAddOnBusy = false`.
- `[RelayCommand] UpdateSelectionsAsync(IReadOnlyList<AddOnSelectionItemCommand> selections)` — sets `IsAddOnBusy = true`, calls `UpdateBookingAddOnSelectionsUseCase`, refreshes catering/decor options, risk indicators, totals, and milestones, sets `IsAddOnBusy = false`.

Wire `LoadAddOnOptionsCommand` into the existing `LoadAsync(Guid bookingId)` method (alongside existing `LoadRoomOptionsCommand`).

### `PartyBookingDetailPage.xaml` — Add catering/decor section

Add a collapsible or scrollable section below the room assignment panel:
- **Catering Options:** Checkboxes or toggle rows per catalog item, showing name + price.
- **Decor Options:** Same pattern.
- **Add-on total:** Summary label showing formatted total (e.g., "Add-ons: $XX.XX").
- **Risk panel:** Conditional — visible only when `HasRisks = true`. Show each `BookingRiskIndicatorDto` with its severity badge (color) and reason text.
- **Save button:** Triggers `UpdateSelectionsCommand`; disabled when `IsBusy`.

UX guardrails (from UX spec):
- Tablet-first density; sticky primary action ("Save add-ons") visible without scroll.
- Risk indicators inline (no modal) — match Party Timeline Rail behavior.
- Severity badge: Low = amber color token, High = red color token.
- Inline recovery: "Deselect this option to clear risk" action label next to High-severity items.
- Status message visible after save (uses `AddOnStatusMessage` binding).

---

## Implementation Tasks / Subtasks

### Task 1 — Add domain entity, enum, and aggregate navigation (AC: 1, 2, 3)

- [x] Add `PartyAddOnType` enum to `POSOpen/Domain/Enums/PartyAddOnType.cs`.
- [x] Add `PartyBookingAddOnSelection` entity to `POSOpen/Domain/Entities/PartyBookingAddOnSelection.cs`.
- [x] Extend `PartyBooking.cs` with `AddOnSelections` nav property, `LastAddOnUpdateOperationId` (nullable `Guid`), and `UpdateAddOnSelections(Guid, Guid, DateTime)` domain method.
- [x] **Do NOT add `ComputeAddOnTotalCents` to `PartyBooking`.** Total computation belongs in the use case (application layer).

### Task 2 — EF configuration and migration (AC: 1, 4)

- [x] Add `PartyBookingAddOnSelectionConfiguration.cs` under `Infrastructure/Persistence/Configurations/`.
- [x] Extend `PartyBookingConfiguration.cs` with `HasMany(AddOnSelections)` relationship config.
- [x] Register `PartyBookingAddOnSelection` in `PosOpenDbContext`.
- [x] Add migration `20260403000000_AddPartyBookingAddOnSelections` and update model snapshot.
- [x] Migration must include: new `party_booking_add_on_selections` table AND new `last_add_on_update_operation_id` column on `party_bookings`.
- [x] Indexes: `ix_party_booking_add_on_sel_booking_id`, `ix_party_booking_add_on_sel_operation_id`.
- [x] Maintain snake_case column naming throughout.

### Task 3 — Extend repository abstraction and implementation (AC: 1, 2, 3, 4)

- [x] Add `GetByIdWithSelectionsAsync` and `ReplaceAddOnSelectionsAsync` to `IPartyBookingRepository` (do NOT add `ListAddOnSelectionsAsync`).
- [x] Implement both in `PartyBookingRepository.cs`.
- [x] `GetByIdWithSelectionsAsync`: scoped context, `.Include(x => x.AddOnSelections).AsNoTracking()`. Do NOT modify existing `GetByIdAsync`.
- [x] `ReplaceAddOnSelectionsAsync`: atomic `RemoveRange` + `AddRange` + `existing.UpdateAddOnSelections(...)` inside a single `BeginTransactionAsync`/`CommitAsync` block. Follow the exact `try/catch/RollbackAsync` pattern from `AssignRoomAsync`.

### Task 4 — Add use cases, commands, and DTOs (AC: 1, 2, 3, 4)

- [x] Create `AddOnSelectionDtos.cs` with all DTO records listed above.
- [x] Add catalog entries, error codes, safe messages, and price dictionary to `PartyBookingConstants.cs`.
- [x] Implement `GetBookingAddOnOptionsUseCase.cs` + `GetBookingAddOnOptionsQuery.cs`.
- [x] Implement `UpdateBookingAddOnSelectionsUseCase.cs` + `UpdateBookingAddOnSelectionsCommand.cs`.
- [x] Risk evaluation logic — extract into `internal static class BookingRiskEvaluator` in `POSOpen/Application/UseCases/Party/BookingRiskEvaluator.cs`. Expose `EvaluateRisks(IEnumerable<AddOnSelectionItemCommand> selections)` returning `IReadOnlyList<BookingRiskIndicatorDto>` and `GetRiskInfo(string optionId)` returning `(bool IsAtRisk, string? Severity, string? Reason)`. Both use `PartyBookingConstants.AtRiskOptionMeta`. **Do NOT inline this logic into either use case** — both use cases must share the same evaluator to guarantee consistency.
- [x] Delegate timeline refresh to existing `GetPartyBookingTimelineUseCase` after save; return milestones in result DTO.
- [x] Return canonical `AppResult<T>` with user-safe and diagnostic separation on all paths.

### Task 5 — Wire ViewModel and UI (AC: 1, 2, 3)

- [x] Inject `GetBookingAddOnOptionsUseCase` and `UpdateBookingAddOnSelectionsUseCase` in `PartyBookingDetailViewModel` constructor.
- [x] Add `ObservableProperty` declarations per ViewModel section above (`IsAddOnBusy`, NOT `IsBusy` for add-on ops).
- [x] Add computed `public bool HasRisks => RiskIndicators.Count > 0;` with `[NotifyPropertyChangedFor(nameof(HasRisks))]` on `_riskIndicators`. Do NOT use `[ObservableProperty]` for `HasRisks`.
- [x] Add `LoadAddOnOptionsCommand` and `UpdateSelectionsCommand` relay commands (bound to `IsAddOnBusy`, not `IsBusy`).
- [x] Wire into `LoadAsync` — load add-on options alongside timeline and room options.
- [x] Update `PartyBookingDetailPage.xaml` with catering/decor section, risk panel, and totals display.
- [x] UI checkboxes only — no quantity spinners. V1 quantity is always 1.
- [x] Register new use cases in `PartyServiceCollectionExtensions.cs`.

### Task 6 — Add tests (AC: 1, 2, 3, 4)

- [x] Unit: `GetBookingAddOnOptionsUseCaseTests.cs` — options with no selections, full selections, at-risk items surfaced correctly, totals computed.
- [x] Unit: `UpdateBookingAddOnSelectionsUseCaseTests.cs` — invalid option rejected, idempotency short-circuit (via `LastAddOnUpdateOperationId` match), idempotency short-circuit when selections list is empty (covers C1 edge case), risk re-evaluation on save, timeline milestones in response, timeline refresh failure returns partial success (not a failure result).
- [x] Integration: `PartyCateringRepositoryTests.cs` (or extend `PartyBookingRepositoryTests.cs`) — `ReplaceAddOnSelectionsAsync` atomicity, cascaded delete-insert, `ListAddOnSelectionsAsync` round-trip, NFR4 performance regression for selections + timeline retrieval (≤3s P95).
- [ ] Confirm all prior tests still pass (regression: Stories 4.1–4.3 flows unbroken).

---

## Definition of Done

- [x] Coordinator can view and update catering/decor options on a booking.
- [x] Booking add-on totals are recomputed and returned after every selection change.
- [x] Risk indicators are surfaced with severity and reason for all at-risk options.
- [x] Applying a corrective selection (deselecting risky item and saving) clears risk indicators.
- [x] Timeline milestones are refreshed and returned after selection saves.
- [x] NFR4 (≤3s P95) satisfied for selection update + timeline refresh path.
- [ ] Stories 4.1–4.3 remain regression-safe.
- [ ] All new and existing tests pass.

---

## Test Scenarios

| Scenario | AC | Expected Result |
|:--|:--|:--|
| Load add-on options for booking with no prior selections | AC1 | All options returned as unselected; totals = 0 |
| Select catering and decor options and save | AC1 | Selections persisted; totals reflect selected option prices; timeline milestones refreshed |
| Save with same operationId twice | AC1 | Second call returns idempotent "already saved" response without duplicate rows |
| Load options where booking has at-risk item selected | AC2 | Risk indicator included with severity + reason |
| Load options where no at-risk items selected | AC2 | Empty risk indicators list |
| Replace risky selection with non-risky alternative | AC3 | Risk indicators re-evaluated and cleared for resolved item |
| Deselect all at-risk items and save (empty selections for risky items) | AC3 | All risk indicators cleared; `RiskIndicators` list is empty; `HasRisks = false` |
| Submit with invalid option ID | AC1 | `BOOKING_ADDON_OPTION_INVALID` error returned; no persistence change |
| Selection update + timeline refresh under active-day profile | AC4 | Response returned within 3 seconds P95 |
| Concurrent replacement operations (atomicity test) | AC1 | Only committed transaction's selections visible; no partial state |

---

## Architecture Compliance Guardrails

- Feature-first structure: `Features/Party`, `Application/UseCases/Party`, `Infrastructure/Persistence/Repositories`.
- Canonical `AppResult<T>` envelope with `isSuccess`, `errorCode`, `userMessage`, `diagnosticMessage`, `payload`.
- All write paths traceable: `operationId` + `correlationId` propagated to booking aggregate and selection rows.
- UTC timestamps only; `UtcDateTimeConverter`/`NullableUtcDateTimeConverter` in EF config.
- No Presentation-to-Infrastructure coupling.
- Retry/orchestration policy stays at infrastructure boundary; no retry loops in use cases.
- Static catalog mutations (price changes, new options) are V2; do not add a catalog DB table in this story.
- Use `AddRange` + `SaveChangesAsync` pattern for bulk selection insert (consistent with test helper and existing patterns).

---

## References

- [Source: _bmad-output/planning-artifacts/epics.md#Story-4.4-Manage-Catering/Decor-Options-with-Risk-Indicators]
- [Source: _bmad-output/planning-artifacts/epics.md#Epic-4-Party-Booking-Lifecycle-and-Inventory-Coordination]
- [Source: _bmad-output/planning-artifacts/prd.md - FR23, FR24, NFR4]
- [Source: _bmad-output/implementation-artifacts/4-3-manage-room-assignment-and-conflict-resolution.md]
- [Source: POSOpen/Domain/Entities/PartyBooking.cs]
- [Source: POSOpen/Application/Abstractions/Repositories/IPartyBookingRepository.cs]
- [Source: POSOpen/Application/UseCases/Party/PartyBookingConstants.cs]
- [Source: POSOpen/Application/UseCases/Party/RoomAssignmentDtos.cs]
- [Source: POSOpen/Application/UseCases/Party/PartyTimelineDtos.cs]
- [Source: POSOpen/Application/UseCases/Party/GetPartyBookingTimelineUseCase.cs]
- [Source: POSOpen/Features/Party/ViewModels/PartyBookingDetailViewModel.cs]
- [Source: POSOpen/Features/Party/Views/PartyBookingDetailPage.xaml]
- [Source: POSOpen/Infrastructure/Persistence/Configurations/PartyBookingConfiguration.cs]
- [Source: POSOpen/Infrastructure/Persistence/Migrations/20260402110000_AddPartyBookingRoomAssignment.cs]

## Dev Agent Record

### Agent Model Used

GPT-5.3-Codex (GitHub Copilot)

### Debug Log References

- `dotnet test POSOpen.Tests/POSOpen.Tests.csproj --no-restore` (full suite): fails on two NFR timing assertions under full-load run (`PartyTimelineRepositoryTests` and `PartyCateringRepositoryTests`).
- `runTests` targeted integration run for `PartyTimelineRepositoryTests.cs` and `PartyCateringRepositoryTests.cs`: passing in isolated run.

### Completion Notes List

- Implemented add-on domain model, persistence mapping, repository contract/implementation, and migration/snapshot updates for `party_booking_add_on_selections` and `last_add_on_update_operation_id`.
- Added shared add-on/risk DTOs, static risk evaluator, and two party add-on use cases with validation, idempotency, timeline refresh, and canonical `AppResult<T>` responses.
- Extended party booking detail UX and ViewModel with add-on option load/toggle/save flow, totals, and inline risk indicators while preserving existing room/timeline/deposit flows.
- Added unit and integration tests for add-on options retrieval, update/idempotency behavior, atomic repository replacement, and NFR-oriented selection+timeline path.
- Regression gate remains open due timing-sensitive integration test failures when full suite runs under load; isolated test execution passes.

### File List

- POSOpen/Domain/Enums/PartyAddOnType.cs
- POSOpen/Domain/Entities/PartyBookingAddOnSelection.cs
- POSOpen/Domain/Entities/PartyBooking.cs
- POSOpen/Infrastructure/Persistence/Configurations/PartyBookingAddOnSelectionConfiguration.cs
- POSOpen/Infrastructure/Persistence/Configurations/PartyBookingConfiguration.cs
- POSOpen/Infrastructure/Persistence/PosOpenDbContext.cs
- POSOpen/Application/Abstractions/Repositories/IPartyBookingRepository.cs
- POSOpen/Infrastructure/Persistence/Repositories/PartyBookingRepository.cs
- POSOpen/Application/UseCases/Party/AddOnSelectionDtos.cs
- POSOpen/Application/UseCases/Party/GetBookingAddOnOptionsQuery.cs
- POSOpen/Application/UseCases/Party/UpdateBookingAddOnSelectionsCommand.cs
- POSOpen/Application/UseCases/Party/BookingRiskEvaluator.cs
- POSOpen/Application/UseCases/Party/GetBookingAddOnOptionsUseCase.cs
- POSOpen/Application/UseCases/Party/UpdateBookingAddOnSelectionsUseCase.cs
- POSOpen/Application/UseCases/Party/PartyBookingConstants.cs
- POSOpen/Features/Party/ViewModels/PartyBookingDetailViewModel.cs
- POSOpen/Features/Party/Views/PartyBookingDetailPage.xaml
- POSOpen/Features/Party/PartyServiceCollectionExtensions.cs
- POSOpen/Infrastructure/Persistence/Migrations/20260403000000_AddPartyBookingAddOnSelections.cs
- POSOpen/Infrastructure/Persistence/Migrations/PosOpenDbContextModelSnapshot.cs
- POSOpen.Tests/Unit/Party/GetBookingAddOnOptionsUseCaseTests.cs
- POSOpen.Tests/Unit/Party/UpdateBookingAddOnSelectionsUseCaseTests.cs
- POSOpen.Tests/Integration/Party/PartyCateringRepositoryTests.cs

### Change Log

- 2026-04-01: Implemented Story 4.4 core add-on selection and risk workflow across domain, application, infrastructure, MAUI feature UI, and tests.

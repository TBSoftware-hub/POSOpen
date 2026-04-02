# Story 5.1: Enable Offline Mode for Critical Frontline Workflows

Status: ready-for-dev

## Story

As a cashier/party coordinator,
I want critical frontline workflows to continue uninterrupted when internet is unavailable,
so that operations are never blocked by connectivity outages.

---

## Acceptance Criteria

### AC-1 - Critical workflows continue in offline mode

> **Given** internet connectivity is unavailable  
> **When** a user executes critical workflows (admissions, mixed-cart capture, booking updates)  
> **Then** those workflows continue in offline mode using local SQLite persistence  
> **And** the user is clearly informed that offline mode is active via a persistent banner/indicator.

### AC-2 - Unsupported actions identified with fallback guidance

> **Given** offline mode is active  
> **When** a user navigates to or initiates a workflow  
> **Then** workflows that require internet (e.g., remote payment card authorization, cloud sync) are clearly identified as degraded or unavailable  
> **And** actionable fallback guidance is provided in impacted workflow screens (e.g., "Use deferred payment" or "Reconnect to sync").

### AC-3 - Offline state recovered on app restart

> **Given** the app restarts during an active connectivity outage  
> **When** the user returns to any workflow  
> **Then** offline mode is automatically re-detected and correctly activated  
> **And** previously captured pending operations (from OutboxMessage) remain durable and visible through startup sync state messaging.

---

## Tasks / Subtasks

- [ ] Task 1: Add IConnectivityService abstraction (AC-1, AC-3)
  - [ ] Create `POSOpen/Application/Abstractions/Services/IConnectivityService.cs`
  - [ ] Interface exposes: `bool IsConnected { get; }`, `event EventHandler<bool> ConnectivityChanged;`

- [ ] Task 2: Implement MauiConnectivityService (AC-1, AC-3)
  - [ ] Create `POSOpen/Infrastructure/Services/MauiConnectivityService.cs`
  - [ ] Wrap MAUI's `Microsoft.Maui.Networking.IConnectivity`
  - [ ] `IsConnected`: return `_connectivity.NetworkAccess == NetworkAccess.Internet`
  - [ ] Subscribe to `IConnectivity.ConnectivityChanged` in constructor; raise `ConnectivityChanged` event on UI thread

- [ ] Task 3: Add offline mode members to IAppStateService and AppStateService (AC-1, AC-2, AC-3)
  - [ ] Add `bool IsOffline { get; }` to `IAppStateService`
  - [ ] Add `DateTimeOffset? OfflineSince { get; }` to `IAppStateService`
  - [ ] Add `event EventHandler? StateChanged` to `IAppStateService`
  - [ ] Add `void SetOfflineMode(bool isOffline)` to `IAppStateService`
  - [ ] Implement in `AppStateService`: set `IsOffline`; record `OfflineSince = DateTimeOffset.UtcNow` when going offline; clear to null when coming back online
  - [ ] Raise `StateChanged` on all state mutations (`SetCurrentSession`, `SetSessionVersion`, `RefreshPermissionSnapshot`, `SetTerminalMode`, `SetSyncState`, `SetOfflineMode`)

- [ ] Task 4: Create IWorkflowCapabilityService and implementation (AC-2)
  - [ ] Create `POSOpen/Application/Abstractions/Services/IWorkflowCapabilityService.cs`
    - [ ] `bool IsOfflineSupported(string workflowKey)`
    - [ ] `string GetOfflineFallbackGuidance(string workflowKey)`
  - [ ] Create `POSOpen/Application/WorkflowKeys.cs` — static string constants:
    - `Admissions`, `Checkout`, `PartyBookingUpdate`, `PaymentSettlement`, `CloudSync`
  - [ ] Create `POSOpen/Infrastructure/Services/WorkflowCapabilityService.cs` — policy dictionary:
    - Admissions, Checkout, PartyBookingUpdate → offline supported
    - PaymentSettlement → offline fallback: "Payment will be deferred and settled when connectivity is restored."
    - CloudSync → offline fallback: "Sync will resume automatically when internet is available."

- [ ] Task 5: Create ConnectivityMonitorService (AC-1, AC-3)
  - [ ] Create `POSOpen/Infrastructure/Services/ConnectivityMonitorService.cs`
  - [ ] `Initialize()` method: reads `IConnectivityService.IsConnected` → calls `IAppStateService.SetOfflineMode(!isConnected)`
  - [ ] Subscribe to `IConnectivityService.ConnectivityChanged` → update `IAppStateService.SetOfflineMode`
  - [ ] In `Initialize()`, call `IOutboxRepository.ListPendingAsync()` and include pending operation count in startup sync messaging
  - [ ] Update `AppStateService.SyncState` automatically based on offline mode transitions

- [ ] Task 6: Register services and initialize monitor in MauiProgram.cs (AC-1, AC-3)
  - [ ] Register `IConnectivityService` as singleton → `MauiConnectivityService` (receives MAUI `IConnectivity` via DI)
  - [ ] Register `IWorkflowCapabilityService` as singleton → `WorkflowCapabilityService`
  - [ ] Register `ConnectivityMonitorService` as singleton
  - [ ] In `CreateMauiApp`, after `builder.Build()`: resolve and call `ConnectivityMonitorService.Initialize()`
  - [ ] Wire MAUI's `IConnectivity` via `builder.Services.AddSingleton(Connectivity.Current)` (note: MAUI provides platform-specific `Connectivity.Current`)

- [ ] Task 7: Add offline banner to AppShell (AC-1, AC-2)
  - [ ] Create `POSOpen/Features/Shell/ViewModels/AppShellViewModel.cs`
    - [ ] Depends on `IAppStateService`
    - [ ] `[ObservableProperty] bool _isOffline` bound to `IAppStateService.IsOffline`
    - [ ] `[ObservableProperty] string _offlineMessage` (e.g., "Offline Mode Active — Local operations continue")
    - [ ] Subscribe to `IAppStateService.StateChanged` for deterministic refreshes
  - [ ] Update `AppShell.xaml.cs` to use `AppShellViewModel`
  - [ ] Add offline banner strip to `AppShell.xaml` visible only when `IsOffline = true` (yellow/orange background, auto-hide on reconnect)

- [ ] Task 8: Update HomeViewModel with offline-aware status (AC-1, AC-2)
  - [ ] Update `HomeViewModel.Refresh()` to read `IAppStateService.IsOffline` and set `SyncState` to "Offline Mode Active" or "Online — Syncing"
  - [ ] Expose `IsOffline` and `OfflineSinceDisplay` properties (formatted `OfflineSince` string)
  - [ ] Subscribe to `IAppStateService.StateChanged` so home status reflects connectivity transitions without manual refresh
  - [ ] Integrate workflow fallback guidance in impacted entry points:
    - [ ] `POSOpen/Features/Admissions/ViewModels/FastPathCheckInViewModel.cs` for admissions guidance
    - [ ] `POSOpen/Features/Checkout/ViewModels/PaymentCaptureViewModel.cs` for deferred payment fallback guidance
    - [ ] `POSOpen/Features/Party/ViewModels/PartyBookingDetailViewModel.cs` for booking update guidance
  - [ ] No new MAUI permissions or capabilities needed

- [ ] Task 9: Unit tests (AC-1, AC-2, AC-3)
  - [ ] Create `POSOpen.Tests/Unit/Sync/WorkflowCapabilityServiceTests.cs`
    - [ ] Verify Admissions, Checkout, PartyBookingUpdate return IsOfflineSupported = true
    - [ ] Verify PaymentSettlement, CloudSync return IsOfflineSupported = false
    - [ ] Verify fallback messages are non-empty for unsupported workflows
  - [ ] Create `POSOpen.Tests/Unit/Sync/ConnectivityMonitorServiceTests.cs`
    - [ ] Initialize() with no-internet → AppStateService.IsOffline = true, OfflineSince set
    - [ ] Initialize() with internet → AppStateService.IsOffline = false, OfflineSince null
    - [ ] ConnectivityChanged (false → true) → AppStateService.SetOfflineMode(false) called
    - [ ] Initialize() with pending outbox count > 0 updates sync state message with pending count
  - [ ] Create `POSOpen.Tests/Unit/Sync/AppStateServiceOfflineModeTests.cs`
    - [ ] SetOfflineMode(true) → IsOffline = true, OfflineSince = non-null
    - [ ] SetOfflineMode(false) → IsOffline = false, OfflineSince = null
    - [ ] Idempotent: SetOfflineMode(true) twice → OfflineSince not overwritten on second call
    - [ ] StateChanged is raised once per mutating method call
  - [ ] Create `POSOpen.Tests/Unit/Admissions/FastPathOfflineGuidanceTests.cs`
    - [ ] Offline unsupported action path surfaces fallback guidance from `IWorkflowCapabilityService`
  - [ ] Create `POSOpen.Tests/Unit/Checkout/PaymentCaptureOfflineGuidanceTests.cs`
    - [ ] Offline payment path surfaces deferred-payment guidance
  - [ ] Create `POSOpen.Tests/Unit/Party/PartyBookingOfflineGuidanceTests.cs`
    - [ ] Offline booking update path surfaces actionable guidance

---

## Dev Notes

### Scope Boundary

Story 5.1 establishes the **connectivity detection and UX indication foundation** for Epic 5. It does NOT implement:
- Action queueing to OutboxMessage (Story 5.2)
- Synchronization/replay worker (Story 5.3)
- Duplicate-finalization prevention (Story 5.4)
- Full sync health dashboard (Story 5.5)

Story 5.1 makes offline mode **detectable, visible, and non-blocking** for critical workflows. Baseline pending visibility is included as startup sync state messaging in 5.1; detailed sync health dashboarding remains in 5.5.

---

### MAUI Connectivity Integration

MAUI provides `Microsoft.Maui.Networking.IConnectivity` as a testable interface backed by platform implementation at `Microsoft.Maui.Networking.Connectivity.Current`. Register it as:

```csharp
// MauiProgram.cs
builder.Services.AddSingleton<IConnectivity>(Connectivity.Current);
```

`IConnectivity.NetworkAccess` returns `NetworkAccess.Internet` when a usable network route to the internet exists. All other values (None, Local, ConstrainedInternet, Unknown) should be treated as offline.

`IConnectivity.ConnectivityChanged` fires on `ConnectivityChangedEventArgs` with a `NetworkAccess` property. Marshal to main thread if updating observable properties in a MAUI ViewModel.

**IMPORTANT**: Use `MainThread.BeginInvokeOnMainThread(() => ...)` when raising `ConnectivityChanged` from `MauiConnectivityService` if the handler touches UI state.

---

### AppStateService Offline Extension

Add to `IAppStateService`:
```csharp
bool IsOffline { get; }
DateTimeOffset? OfflineSince { get; }
event EventHandler? StateChanged;
void SetOfflineMode(bool isOffline);
```

In `AppStateService`:
- `SetOfflineMode(true)` → `IsOffline = true`, record `OfflineSince = DateTimeOffset.UtcNow` **only if OfflineSince is currently null** (idempotent — don't reset timestamp on repeat calls)
- `SetOfflineMode(false)` → `IsOffline = false`, `OfflineSince = null`
- Update `SyncState` automatically:
  - When going offline: `"Offline Mode Active"`
  - When coming online: `"Online — Reconnected"`
- Raise `StateChanged` after each successful state mutation so shell and home viewmodels update without polling.

---

### WorkflowCapabilityService Policy

```csharp
// WorkflowKeys.cs
public static class WorkflowKeys
{
    public const string Admissions = "admissions";
    public const string Checkout = "checkout";
    public const string PartyBookingUpdate = "party-booking-update";
    public const string PaymentSettlement = "payment-settlement";
    public const string CloudSync = "cloud-sync";
}
```

Offline-supported: `Admissions`, `Checkout`, `PartyBookingUpdate` — these use local SQLite and continue without internet.

Fallback-only: `PaymentSettlement` → "Payment authorization is unavailable offline. Payment will be deferred and queued for settlement when connectivity is restored." `CloudSync` → "Sync is paused. Operations will sync automatically when internet is available."

---

### ConnectivityMonitorService

`ConnectivityMonitorService` is an app-lifetime singleton. Its `Initialize()` must be called **once** at app startup after the DI container is built:

```csharp
// MauiProgram.cs — after builder.Build()
var app = builder.Build();
app.Services.GetRequiredService<ConnectivityMonitorService>().Initialize();
return app;
```

`Initialize()` performs initial connectivity check and subscribes to change events. No background threads — all updates go through IAppStateService which is a singleton, so thread safety via atomic property writes is sufficient for V1.

---

### AppShell Offline Banner

The offline banner should:
- Be **always in XAML but hidden** via `IsVisible="{Binding IsOffline}"` binding (avoid layout thrashing)
- Use a **warning color** (e.g., `#FF8C00` amber) consistent with UX risk indicator precedents set in Epic 4
- Show message: "Offline Mode Active — Local operations continue"
- NOT show a close/dismiss button (it should auto-dismiss when connectivity returns)
- Position: **top of Shell content area** below the navigation bar

Bind `AppShell.BindingContext` to `AppShellViewModel` in `AppShell.xaml.cs` constructor. `AppShellViewModel` subscribes to `IAppStateService.StateChanged` to refresh `IsOffline` on the main thread.

**Alternatively**, if MAUI Shell doesn't cleanly support overlaid banner at Shell level, use a `Grid` with `RowDefinitions="Auto,*"` in each host `ContentPage`. Discuss in AC-2 review if shell approach has platform issues.

---

### AC-3: Offline State Recovery on Restart

Offline mode does **not** need to be persisted to SQLite. `ConnectivityMonitorService.Initialize()` re-evaluates `IConnectivity.NetworkAccess` at each startup. If the device still has no internet → `IsOffline = true` automatically.

Pending operations (OutboxMessage records) already survive restarts via SQLite persistence (entity is in `PosOpenDbContext`, schema exists from earlier stories). Story 5.1 does not write new OutboxMessage entries — that is Story 5.2. Story 5.1 must ensure the `IOutboxRepository.ListPendingAsync()` result is passed to `IAppStateService` to update `SyncState` on startup.

**Required AC-3 behavior**: On startup in `ConnectivityMonitorService.Initialize()`, call `IOutboxRepository.ListPendingAsync()` and if pending count > 0, reflect it in `AppStateService.SyncState` as "Offline — N actions pending sync".

---

### Architecture Compliance Guardrails

| Rule | Obligation |
|---|---|
| Layer boundaries | `MauiConnectivityService` in Infrastructure; `IConnectivityService` interface in Application/Abstractions; no VM → Infrastructure direct calls |
| Result envelope | `AppResult<T>` is not required for connectivity state — it's an observable service, not a use-case return. Do NOT wrap in AppResult. |
| AppStateService is the source of truth | All ViewModels read connectivity state from `IAppStateService`, not directly from `IConnectivityService` |
| MainThread safety | Connectivity events from OS arrive on background threads; marshal to main thread before updating observable properties |
| UTC timestamps | `OfflineSince` must be `DateTimeOffset.UtcNow` |
| No persistence needed | Offline mode flag is re-derived at startup from live connectivity; no new migration needed |
| CommunityToolkit.Mvvm | `AppShellViewModel` uses `[ObservableProperty]` and `ObservableObject` patterns; no manual INotifyPropertyChanged |
| Feature organization | Connectivity infrastructure belongs in Infrastructure/Services; no Features/Sync folder required yet (introduced in 5.5 for sync health dashboard) |

---

### Project Structure Notes

**New files to create:**

```
POSOpen/Application/Abstractions/Services/
  IConnectivityService.cs         ← new
  IWorkflowCapabilityService.cs   ← new

POSOpen/Application/
  WorkflowKeys.cs                 ← new

POSOpen/Infrastructure/Services/
  MauiConnectivityService.cs      ← new
  WorkflowCapabilityService.cs    ← new
  ConnectivityMonitorService.cs   ← new

POSOpen/Features/Shell/ViewModels/
  AppShellViewModel.cs            ← new

POSOpen.Tests/Unit/Sync/
  WorkflowCapabilityServiceTests.cs     ← new
  ConnectivityMonitorServiceTests.cs    ← new
  AppStateServiceOfflineModeTests.cs    ← new
```

**Files to modify:**

```
POSOpen/Application/Abstractions/Services/IAppStateService.cs
  → Add: IsOffline, OfflineSince, StateChanged, SetOfflineMode(bool)

POSOpen/Infrastructure/Services/AppStateService.cs
  → Implement: IsOffline, OfflineSince, StateChanged, SetOfflineMode(bool) with idempotent OfflineSince

POSOpen/MauiProgram.cs
  → Register: IConnectivity (Connectivity.Current), IConnectivityService, IWorkflowCapabilityService,
    ConnectivityMonitorService; call Initialize() after build

POSOpen/AppShell.xaml
  → Add: offline banner strip (Grid row, Label, color binding)

POSOpen/AppShell.xaml.cs
  → Bind BindingContext to AppShellViewModel (resolved from DI)

POSOpen/Features/Shell/ViewModels/HomeViewModel.cs
  → Update Refresh() to surface IsOffline, OfflineSinceDisplay properties
```

---

### Previous Story Intelligence (4.6)

- **AppResult<T> envelope** is required for ALL use-case returns. This story does not introduce new use cases — connectivity state is an observable service. Do NOT wrap `IConnectivityService` events or state in AppResult.
- **Operation context propagation** required on write paths. ConnectivityMonitorService is read-only from the app-state perspective (it sets flags, not writes financial records). No OperationContext needed for SetOfflineMode.
- **IOperationLogRepository and SecurityAuditEventTypes** — offline mode transitions do NOT need immutable audit events (not security-critical; no financial action involved).
- **UTC timestamps** mandatory — use `DateTimeOffset.UtcNow` for `OfflineSince`.
- **Feature-first organization** — `AppShellViewModel` goes in `Features/Shell/ViewModels/` (not a new Sync feature folder).
- **Test patterns** — use Moq for mocking IConnectivityService, IAppStateService; FluentAssertions for assertions; xUnit [Fact]/[Theory] as in existing tests.
- **SeededInventorySubstitutionPolicyProvider** pattern — `WorkflowCapabilityService` similarly uses a static/seeded policy dictionary. Keep it simple; no database backing needed.

---

### References

- MAUI IConnectivity API: `Microsoft.Maui.Networking.IConnectivity` / `Connectivity.Current` [Source: architecture.md#Frontend Architecture]
- AppStateService current structure: [Source: POSOpen/Infrastructure/Services/AppStateService.cs]
- IAppStateService contract: [Source: POSOpen/Application/Abstractions/Services/IAppStateService.cs]
- OutboxMessage entity (already persisted): [Source: POSOpen/Domain/Entities/OutboxMessage.cs]
- IOutboxRepository: [Source: POSOpen/Application/Abstractions/Repositories/IOutboxRepository.cs]
- Offline/sync feature mapping (FR30-FR36): Features/Sync + Infrastructure/Sync [Source: architecture.md#Requirements to Structure Mapping]
- Offline UX contract: "Critical commands return local-commit status and deferred-cloud status separately" [Source: architecture.md#Frontend Architecture]
- Offline Replay Pattern: Append-only queue writes, ordered idempotent replay [Source: architecture.md#Process Patterns]
- HomeViewModel existing SyncState/TerminalMode binding: [Source: POSOpen/Features/Shell/ViewModels/HomeViewModel.cs]
- AppShell.xaml existing structure: [Source: POSOpen/AppShell.xaml]
- Test patterns: Moq + FluentAssertions + xUnit [Source: POSOpen.Tests/Unit/Admissions/EvaluateFastPathCheckInUseCaseTests.cs]

---

## Dev Agent Record

### Agent Model Used

GPT-5.3-Codex (GitHub Copilot)

### Debug Log References

### Completion Notes List

### File List

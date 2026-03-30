# Story 2.1: Family Lookup with Search and Scan

Status: in-progress

## Story

As a cashier,
I want to find a family using search or QR/barcode scan,
So that I can start check-in quickly with minimal friction.

## Acceptance Criteria

**Given** I am on admissions check-in  
**When** I search by supported identifiers (name, phone, booking ref)  
**Then** matching families are returned with enough context to select the correct record  
**And** results include waiver/payment status indicators.

**Given** a family QR/barcode is scanned  
**When** scan payload matches an existing record  
**Then** the matching family profile opens directly  
**And** fast-lane check-in state is initialized.

**Given** no exact match is found  
**When** lookup completes  
**Then** the UI offers create/continue-new-profile path  
**And** entered query context is retained.

## Tasks / Subtasks

- [x] Introduce FamilyProfile domain entity and WaiverStatus enum. (AC: 1, 2, 3)
  - [x] Create `POSOpen/Domain/Entities/FamilyProfile.cs` with static factory method following StaffAccount pattern.
  - [x] Create `POSOpen/Domain/Enums/WaiverStatus.cs` with: None, Valid, Expired, Pending.
  - [x] Include search-relevant fields: Id, PrimaryContactFirstName, PrimaryContactLastName, Phone, Email, WaiverStatus, WaiverCompletedAtUtc, ScanToken (nullable), CreatedAtUtc, UpdatedAtUtc.
  - [x] ScanToken is a string token (e.g. UUID) used for QR/barcode lookup; generated at profile creation.
- [x] Define IFamilyProfileRepository with search capabilities. (AC: 1, 2, 3)
  - [x] Create `POSOpen/Application/Abstractions/Repositories/IFamilyProfileRepository.cs`.
  - [x] Include: `SearchAsync(string query, CancellationToken)` — normalized text search across name and phone. Booking ref search is deferred until the Booking entity is introduced (Epic 4).
  - [x] Include: `GetByScanTokenAsync(string token, CancellationToken)` — exact scan token lookup.
  - [x] Include: `GetByIdAsync(Guid id, CancellationToken)`.
  - [x] Include: `AddAsync(FamilyProfile profile, CancellationToken)`.
  - [x] Include: `UpdateAsync(FamilyProfile profile, CancellationToken)`.
- [x] Implement SearchFamiliesUseCase and FamilySearchResultDto. (AC: 1, 2, 3)
  - [x] Create `POSOpen/Application/UseCases/Admissions/SearchFamiliesQuery.cs` with `Query` (string) and `Mode` (enum: Text, Scan).
  - [x] Create `POSOpen/Application/UseCases/Admissions/FamilySearchResultDto.cs` with: Id, DisplayName (formatted as `"{LastName}, {FirstName}"` — last name first for quick scanning), Phone, WaiverStatus, WaiverStatusLabel, HasPaymentOnFile (bool, always false for now — reserved for future story).
  - [x] Create `POSOpen/Application/UseCases/Admissions/SearchFamiliesUseCase.cs`.
  - [x] On Scan mode: call `GetByScanTokenAsync`; return single-element result list on match or empty on miss.
  - [x] On Text mode: call `SearchAsync` with normalized (trimmed) query; return ranked result list.
  - [x] Enforce minimum query length of 2 characters in Text mode; return canonical error `LOOKUP_QUERY_TOO_SHORT` if not met.
  - [x] Return `AppResult<IReadOnlyList<FamilySearchResultDto>>` — never throw on not-found; return empty list with user-safe message.
  - [x] Enforce role authorization using `ICurrentSessionService.GetCurrent()` + `IAuthorizationPolicyService.HasPermission(session.Role, ...)` — this is the canonical pattern used by all existing Epic 1 use cases (`AssignStaffRoleUseCase`, `ListSecurityAuditTrailUseCase`, `SubmitOverrideUseCase`). Do NOT use `IAppStateService` for authorization checks in use cases.
- [x] Implement FamilyProfileRepository against EF Core / SQLite. (AC: 1, 2)
  - [x] Create `POSOpen/Infrastructure/Persistence/Repositories/FamilyProfileRepository.cs`.
  - [x] `SearchAsync`: query against `FamilyProfiles` with case-insensitive LIKE across normalized last name, first name, and phone; limit results to 20.
  - [x] `GetByScanTokenAsync`: exact token match (case-insensitive, normalized).
  - [x] Create EF Core configuration `POSOpen/Infrastructure/Persistence/Configurations/FamilyProfileConfiguration.cs` following the existing `StaffAccountConfiguration.cs` pattern in the same folder (snake_case column names, UTC value converters, `HasIndex` for queried columns, `HasColumnName` for all properties).
  - [x] Register `family_profiles` table with snake_case column names, UTC converters, and indexed columns: `phone`, `scan_token`, `last_name`.
  - [x] Add EF migration: `AddFamilyProfileTable`.
  - [x] Register `IFamilyProfileRepository` -> `FamilyProfileRepository` in `PersistenceServiceCollectionExtensions`.
- [x] Build FamilyLookupViewModel using CommunityToolkit.Mvvm. (AC: 1, 2, 3)
  - [x] Create `POSOpen/Features/Admissions/ViewModels/FamilyLookupViewModel.cs`.
  - [x] Expose `SearchQuery` (string, two-way bindable), `SearchResults` (ObservableCollection<FamilySearchResultDto>), `IsLoading` (bool), `HasNoResults` (bool), `ErrorMessage` (string?), `ShowCreateNewProfile` (bool).
  - [x] ViewModel state transitions: Idle -> Loading -> Results | Empty | Error.
  - [x] `SearchCommand`: debounced (300ms) on query change; requires minimum 2 characters; calls `SearchFamiliesUseCase` in Text mode.
  - [x] `ScanCommand`: accepts scanned token string; calls `SearchFamiliesUseCase` in Scan mode; on single match navigates to `AdmissionsRoutes.FamilyProfile` (the profile route defined in this story) passing the family ID; on no match shows "not found" with retained scan context.
  - [x] `SelectFamilyCommand`: accepts `FamilySearchResultDto`; navigates to `AdmissionsRoutes.FastPathCheckIn` (define the route constant now — the target page/ViewModel is Story 2.2's scope). For this story, log a warning and no-op the navigation rather than attempting to open a non-existent page.
  - [x] `CreateNewProfileCommand`: navigates to new-profile creation path (placeholder route for Story 2.3); retain query as pre-fill hint if available.
  - [x] `HasNoResults` becomes true only when query length >= 2 AND search returned empty list.
  - [x] `ShowCreateNewProfile` becomes true when `HasNoResults` is true.
  - [x] Never clear `SearchQuery` after search failure or error.
- [x] Build FamilyLookupPage view. (AC: 1, 2, 3)
  - [x] Create `POSOpen/Features/Admissions/Views/FamilyLookupPage.xaml` and `.xaml.cs` (binding setup only in code-behind).
  - [x] Top section: search entry bound to `SearchQuery` with placeholder "Search by name or phone" (booking ref search is deferred to Epic 4 when the Booking entity exists).
  - [x] Search results: CollectionView bound to `SearchResults` with skeleton/loading state when `IsLoading`.
  - [x] Each result row: DisplayName, Phone, WaiverStatusLabel as badge (green=Valid, amber=Pending, red=Expired/None).
  - [x] Empty state: shown when `HasNoResults`; message: "No matching families found." with "Create New Profile" button bound to `CreateNewProfileCommand`.
  - [x] Error state: shown when `ErrorMessage` is set; non-clearing user-safe error text above results area.
  - [x] Scan button (icon-button in header area) bound to `ScanCommand`; for V1 opens a manual QR entry dialog (no physical scanner device integration in this story — device integration is Epic 3/Story 3.3).
  - [x] Accessibility: ensure SearchEntry has AutomationId and SemanticProperties.Description; result rows have AutomationId linking to family name.
  - [x] Button hierarchy: "Complete Check-In" (primary, placeholder) sticky at bottom; "Create New Profile" (secondary) inline with empty state only.
- [x] Register admissions routes and services. (AC: 1, 2, 3)
  - [x] Create `POSOpen/Features/Admissions/AdmissionsRoutes.cs` with the following route constants: `FamilyLookup` (this story's page), `FamilyProfile` (scan single-match destination), `FastPathCheckIn` (Story 2.2's page — define the constant now so `SelectFamilyCommand` can reference it, but the target page is NOT implemented in this story).
  - [x] Create `POSOpen/Features/Admissions/AdmissionsServiceCollectionExtensions.cs`; register: `FamilyLookupViewModel`, `SearchFamiliesUseCase`.
  - [x] Register MAUI routes in `MauiProgram.cs` (or shell routing initializer) for admissions pages.
  - [x] Ensure admissions entry point is accessible only from authenticated shell navigation (role-gated).
- [x] Add unit tests for SearchFamiliesUseCase. (AC: 1, 2, 3)
  - [x] Create `POSOpen.Tests/Unit/Admissions/SearchFamiliesUseCaseTests.cs`.
  - [x] Test: text search with valid query returns matching DTOs from mocked repository.
  - [x] Test: text search query shorter than 2 characters returns `LOOKUP_QUERY_TOO_SHORT` failure.
  - [x] Test: text search with no matches returns success with empty list (not failure).
  - [x] Test: scan mode with matching token returns single-element list.
  - [x] Test: scan mode with no matching token returns success with empty list.
  - [x] Test: unauthenticated session returns authorization failure (`AUTH_FORBIDDEN`).
  - [x] Add exactly ONE new `<Compile>` entry to `POSOpen.Tests/POSOpen.Tests.csproj` — existing wildcards already cover `Domain/Entities`, `Domain/Enums`, `Application/Abstractions/Repositories`, and `Infrastructure/Persistence/**`. Only the new use-case folder is missing:
    ```xml
    <Compile Include="..\POSOpen\Application\UseCases\Admissions\*.cs"
             Link="Source\Application\UseCases\Admissions\%(Filename)%(Extension)" />
    ```
    Do NOT add entries for files already covered by existing wildcards.
- [x] Add integration tests for FamilyProfileRepository. (AC: 1, 2)
  - [x] Create `POSOpen.Tests/Integration/Admissions/FamilyProfileRepositoryTests.cs`.
  - [x] Test: `SearchAsync` returns families matching partial last name (case-insensitive).
  - [x] Test: `SearchAsync` returns families matching phone substring.
  - [x] Test: `SearchAsync` returns empty list when no records seeded.
  - [x] Test: `GetByScanTokenAsync` returns profile matching exact token.
  - [x] Test: `GetByScanTokenAsync` returns null for unknown token.
  - [x] Test: `AddAsync` then `SearchAsync` finds newly added profile.

## Dev Notes

### Story Intent

This story introduces the **FamilyProfile domain model** — the core aggregate for all Epic 2 (Admissions) and Epic 4 (Party) workflows — and delivers the cashier's primary entry point for guest interactions: finding a family by search or scan. It must establish provider-agnostic repository patterns that subsequent admissions stories build upon, without pre-empting the waiver validation logic (Story 2.2), new profile creation detail (Story 2.3), or device scanner integration (Epic 3 / Story 3.3).

### Current Repo Reality

- `FamilyProfile` entity does **not yet exist** anywhere in the codebase. This is the first story that introduces it.
- No `Admissions` feature folder exists; `POSOpen/Features/Admissions/` must be created.
- No `IFamilyProfileRepository` or `FamilyProfileRepository` exist.
- `AppResult<T>` is already available in `POSOpen/Application/Results/AppResult.cs` — use as-is.
- `ICurrentSessionService` and `IAuthorizationPolicyService` are the canonical authorization dependencies for use cases (consistent with `AssignStaffRoleUseCase`, `ListSecurityAuditTrailUseCase`, `SubmitOverrideUseCase`). Inject both into `SearchFamiliesUseCase` and call `_currentSessionService.GetCurrent()` + `_authorizationPolicyService.HasPermission(session.Role, ...)`. `IAppStateService` is a UI-layer service used only by `AuthenticateStaffUseCase` to *set* the session — do not inject it into use cases for authorization.
- `IOperationContextFactory` is already available for operation IDs — use when creating profiles or performing write operations in future stories, but this story is read-heavy so only add if logging a search event.
- `IUtcClock` is available in `POSOpen/Application/Abstractions/Services/IUtcClock.cs` — use for timestamps.
- `PosOpenDbContext`, EF Core SQLite provider, and migration infrastructure are all in place from Story 1.0.
- `PersistenceServiceCollectionExtensions` is the correct registration point for the new repository.
- The design-time factory exists in `POSOpen/Infrastructure/Persistence/DesignTime/` for EF migration tooling — do not break it when adding the entity.

### Architecture Compliance

- Strict layer rule: `FamilyLookupViewModel` calls `SearchFamiliesUseCase` only — never accesses `IFamilyProfileRepository` directly.
- `SearchFamiliesUseCase` accesses `IFamilyProfileRepository` (defined in Application, implemented in Infrastructure).  
- `FamilyProfileRepository` accesses `PosOpenDbContext` only — no business logic in the repository.
- Use `AppResult<T>` for all use-case return types.
- Follow `PascalCase` types, `_camelCase` private fields, and `snake_case` column names.
- Register all DI in the correct extension methods; do not register in `MauiProgram.cs` directly (use `AdmissionsServiceCollectionExtensions`).

### Scanner Integration Boundary

The physical scanner device hardware integration (FR38) is scoped to Epic 3 / Story 3.3. In this story:
- The scan flow is implemented as a **manual QR token text entry dialog** (a simple `Entry` control in a popup/dialog).
- The `ScanCommand` opens this dialog, accepts input, and passes result to the use case in Scan mode.
- This design allows Story 3.3 to replace the dialog with a real scanner event listener without changing the ViewModel or use-case interface.
- Do **not** introduce `Infrastructure/Devices/Scanner/` or any hardware abstraction in this story.
- **V1 Dialog Implementation:** Use `await Application.Current!.MainPage!.DisplayPromptAsync("Scan QR Code", "Enter QR token:", "OK", "Cancel", maxLength: 64)` directly in `ScanCommand`. This is the MAUI built-in single-field prompt — do NOT build a custom modal `Page` or a CommunityToolkit.Maui `Popup` for this placeholder, as Story 3.3 will replace it entirely.

### FamilyProfile Entity Design

```
FamilyProfile
├── Id: Guid (PK)
├── PrimaryContactFirstName: string (required)
├── PrimaryContactLastName: string (required)
├── Phone: string (required, E.164 or free-text for V1)
├── Email: string? (optional)
├── WaiverStatus: WaiverStatus (enum)
├── WaiverCompletedAtUtc: DateTime? (null if no waiver)
├── ScanToken: string? (QR lookup token, unique, set at creation)
├── CreatedAtUtc: DateTime
├── UpdatedAtUtc: DateTime
└── CreatedByStaffId: Guid? (who created the profile)
```

Static factory: `FamilyProfile.Create(id, firstName, lastName, phone, email, createdByStaffId, clockUtc)` — sets ScanToken as `Guid.NewGuid().ToString("N")`, WaiverStatus as `None`, timestamps from clock.

### WaiverStatus Enum

```csharp
public enum WaiverStatus { None, Pending, Valid, Expired }
```

For Story 2.1, all search results surface WaiverStatus as a badge label. Full waiver flow logic (routing, blocking, re-evaluation) is Story 2.2.

### Repository Search Design

`SearchAsync(string query, CancellationToken)` should:
- Normalize query with `Trim()` before hitting database.
- Use EF Core `EF.Functions.Like` for LIKE pattern matching.
- Match across: full last name, full first name, or phone. **Do NOT add a `BookingRef` column to `FamilyProfile`** — booking ref belongs to a future `Booking` entity in Epic 4; searching by it is out of scope for this story.
- Return at most 20 results, ordered by last name ascending.
- Do not throw on empty result — return empty list.

`GetByScanTokenAsync(string token, CancellationToken)` should:
- Normalize token with `Trim()` before comparison.
- Use case-insensitive exact match on `ScanToken` column.
- Return `null` if not found.

### ViewModel State Transitions

```
Idle        (SearchQuery == "" OR query < 2 chars, ShowCreateNewProfile = false)
  → Loading   (SearchCommand executing)
    → Results   (SearchResults.Count > 0, IsLoading = false)
    → Empty     (SearchResults.Count == 0 AND query >= 2, HasNoResults = true, ShowCreateNewProfile = true)
    → Error     (ErrorMessage != null, IsLoading = false, query retained)
```

### ViewModel Debounce Pattern

`SearchCommand` must be debounced 300ms on `SearchQuery` property change. `[RelayCommand]` has no built-in debounce — use the `OnSearchQueryChanged` partial method with a class-level `CancellationTokenSource`:

```csharp
private CancellationTokenSource? _searchDebounce;

partial void OnSearchQueryChanged(string value)
{
    _searchDebounce?.Cancel();
    _searchDebounce = new CancellationTokenSource();
    _ = ExecuteSearchDebounced(_searchDebounce.Token);
}

private async Task ExecuteSearchDebounced(CancellationToken ct)
{
    try
    {
        await Task.Delay(300, ct);
        await SearchCommand.ExecuteAsync(null);
    }
    catch (OperationCanceledException) { /* debounced — ignore */ }
}
```

Do NOT use `System.Timers.Timer` (cross-thread UI issues) or call `SearchCommand` synchronously from `OnSearchQueryChanged`.

### Error Code Contract (Story 2.1)

- `LOOKUP_QUERY_TOO_SHORT`: Text search query is fewer than 2 characters.
- `LOOKUP_UNAVAILABLE`: Repository call failed due to unexpected infrastructure error; map to user-safe message "Search is temporarily unavailable. Please try again."
- `AUTH_FORBIDDEN`: Session is not authenticated or role is insufficient. This matches the canonical code used by all Epic 1 use cases. Do NOT use `AUTH_REQUIRED`.

### WaiverStatus Badge Color Contract

| WaiverStatus | Label     | Color  |
|:------------ |:--------- |:------ |
| Valid        | Waiver OK | Green (#16A34A) |
| Pending      | Waiver Pending | Amber (#D97706) |
| Expired      | Waiver Expired | Red (#DC2626) |
| None         | No Waiver | Red (#DC2626) |

These colors align with the design system semantic status tokens from the UX specification.

### File Structure Requirements

Expected new files for this story:

```
POSOpen/Domain/Entities/FamilyProfile.cs
POSOpen/Domain/Enums/WaiverStatus.cs
POSOpen/Application/Abstractions/Repositories/IFamilyProfileRepository.cs
POSOpen/Application/UseCases/Admissions/SearchFamiliesQuery.cs
POSOpen/Application/UseCases/Admissions/FamilySearchResultDto.cs
POSOpen/Application/UseCases/Admissions/SearchFamiliesUseCase.cs
POSOpen/Features/Admissions/ViewModels/FamilyLookupViewModel.cs
POSOpen/Features/Admissions/Views/FamilyLookupPage.xaml
POSOpen/Features/Admissions/Views/FamilyLookupPage.xaml.cs
POSOpen/Features/Admissions/AdmissionsRoutes.cs
POSOpen/Features/Admissions/AdmissionsServiceCollectionExtensions.cs
POSOpen/Infrastructure/Persistence/Repositories/FamilyProfileRepository.cs
POSOpen/Infrastructure/Persistence/Configurations/FamilyProfileConfiguration.cs
POSOpen/Infrastructure/Persistence/Migrations/ (new migration)
POSOpen.Tests/Unit/Admissions/SearchFamiliesUseCaseTests.cs
POSOpen.Tests/Integration/Admissions/FamilyProfileRepositoryTests.cs
```

Existing files requiring modification:

```
POSOpen/Infrastructure/Persistence/PersistenceServiceCollectionExtensions.cs   (register IFamilyProfileRepository)
POSOpen/Infrastructure/Persistence/PosOpenDbContext.cs                          (add `public DbSet<FamilyProfile> FamilyProfiles => Set<FamilyProfile>();` using the expression-body property form consistent with all existing DbSets)
POSOpen/MauiProgram.cs                                                          (register AdmissionsServiceCollectionExtensions, admissions routes)
POSOpen.Tests/POSOpen.Tests.csproj                                              (source-link for Admissions use-case)
```

### Testing Requirements

- Verify AC1: text search "Smith" returns FamilyProfile with last name "Smith" plus WaiverStatus in result DTO.
- Verify AC1: text search "555" returns profile with matching phone substring.
- Verify AC1: query of 1 character returns `LOOKUP_QUERY_TOO_SHORT` failure.
- Verify AC2: scan mode with known token returns single result; `ScanCommand` triggers navigation.
- Verify AC2: scan mode with unknown token returns empty list (not error).
- Verify AC3: empty result from text search sets `HasNoResults = true` and `ShowCreateNewProfile = true` in ViewModel.
- Verify AC3: `SearchQuery` is preserved in ViewModel after empty and error states.
- Verify unauthenticated session: use case returns `AUTH_FORBIDDEN`.

### Acceptance Test Matrix

| Scenario | Expected |
|:-------- |:-------- |
| Text search "Jones" matches last name | Result list with DisplayName "Jones, ..." and WaiverStatus badge |
| Text search "0412" matches phone | Result list entry with matching phone |
| Text search "x" (1 char) | Failure: LOOKUP_QUERY_TOO_SHORT, no results shown |
| Scan token found | Single result, direct navigation to family profile |
| Scan token not found | Empty state with "Create New Profile" option; scan context retained |
| Text search no match | Empty state with "Create New Profile" option; SearchQuery retained |
| Unauthenticated session | AUTH_FORBIDDEN failure, redirect to sign-in |

### Previous Story Intelligence (Epic 1)

- **Session authority pattern**: use `ICurrentSessionService.GetCurrent()` + `IAuthorizationPolicyService.HasPermission(session.Role, ...)` for role guards in use cases — this is the canonical Epic 1 pattern. `IAppStateService` is a UI-layer service; do not inject it into use cases for authorization.
- **AppResult<T> pattern**: always return `AppResult<T>` from use cases; never throw for business-level rejections.
- **Canonical error codes**: define story-scoped codes in a summary table in Dev Notes (see Error Code Contract above).
- **User-safe messages**: keep error messages actionable and non-technical; no exception messages exposed to UI.
- **CommunityToolkit.Mvvm**: use `[ObservableProperty]` and `[RelayCommand]` attributes; state transitions through `OnPropertyChanged` or explicit setters with state flag pattern.
- **UTC timestamps**: always use `IUtcClock` for `DateTime.UtcNow`; never use `DateTime.Now`.
- **Test source-link pattern**: update `POSOpen.Tests.csproj` to reference new source files (not MAUI app project directly).
- **No MAUI project reference**: test project uses source includes, not project references, to the MAUI app.
- **Sprint status update**: set `epic-2` to `in-progress` and `2-1-family-lookup-with-search-and-scan` to `in-progress` when work begins.

### Project Structure Notes

- No `project-context.md` file was detected in the workspace; rely on architecture and story artifacts as source of truth.
- Target feature folder is `POSOpen/Features/Admissions/` — must be created as it does not yet exist.
- Target use-case folder is `POSOpen/Application/UseCases/Admissions/` — must be created.
- Integration test folder: `POSOpen.Tests/Integration/Admissions/` — must be created (parallel to existing `Integration/StaffManagement/`).
- Unit test folder: `POSOpen.Tests/Unit/Admissions/` — must be created (parallel to existing `Unit/`).

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story-2.1-Family-Lookup-with-Search-and-Scan]
- [Source: _bmad-output/planning-artifacts/architecture.md#Core-Architectural-Decisions]
- [Source: _bmad-output/planning-artifacts/architecture.md#Implementation-Patterns--Consistency-Rules]
- [Source: _bmad-output/planning-artifacts/architecture.md#Project-Structure--Boundaries]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Step-10-User-Journey-Flows]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Step-11-Component-Strategy]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Step-12-UX-Consistency-Patterns]
- [Source: _bmad-output/implementation-artifacts/1-3-terminal-authentication-flow.md]
- [Source: POSOpen/Domain/Entities/StaffAccount.cs]
- [Source: POSOpen/Application/Abstractions/Repositories/IStaffAccountRepository.cs]
- [Source: POSOpen/Application/Results/AppResult.cs]
- [Source: POSOpen/Application/Abstractions/Services/IAppStateService.cs]
- [Source: POSOpen/Infrastructure/Persistence/PosOpenDbContext.cs]
- [Source: POSOpen/Infrastructure/Persistence/PersistenceServiceCollectionExtensions.cs]

## Dev Agent Record

### Agent Model Used

GPT-5.3-Codex

### Debug Log References

- `dotnet build POSOpen.sln -c Debug -v minimal`
- `dotnet test POSOpen.Tests/POSOpen.Tests.csproj --framework net10.0-windows10.0.19041.0 -v minimal`
- `dotnet ef migrations add AddFamilyProfileTable --project POSOpen --startup-project POSOpen --framework net10.0-windows10.0.19041.0`

### Completion Notes List

- Implemented admissions family lookup stack end-to-end: domain model, repository contract, use case, EF repository/configuration, and migration.
- Added Admissions feature wiring: routes, DI registration, MAUI page/viewmodel, and authenticated shell entry point with role-based visibility.
- Added and passed tests for Story 2.1 behavior:
  - Unit: `SearchFamiliesUseCaseTests` (query length, text/scan match/miss, auth forbidden).
  - Integration: `FamilyProfileRepositoryTests` (last-name/phone search, scan token lookup, add+search flow).
- Updated test source linking in `POSOpen.Tests.csproj` for admissions use cases.
- Validation results: `dotnet build` succeeded; `dotnet test` succeeded with 86/86 passing.
- Focused pre-PR review fixes applied: scan success now navigates to a dedicated `FamilyProfilePage` placeholder that consumes `familyId`, and successful empty-result lookups now clear stale prior errors.

### File List

- POSOpen/Domain/Entities/FamilyProfile.cs
- POSOpen/Domain/Enums/WaiverStatus.cs
- POSOpen/Application/Abstractions/Repositories/IFamilyProfileRepository.cs
- POSOpen/Application/UseCases/Admissions/SearchFamiliesQuery.cs
- POSOpen/Application/UseCases/Admissions/FamilySearchResultDto.cs
- POSOpen/Application/UseCases/Admissions/SearchFamiliesConstants.cs
- POSOpen/Application/UseCases/Admissions/SearchFamiliesUseCase.cs
- POSOpen/Application/Security/RolePermissions.cs
- POSOpen/Infrastructure/Persistence/Repositories/FamilyProfileRepository.cs
- POSOpen/Infrastructure/Persistence/Configurations/FamilyProfileConfiguration.cs
- POSOpen/Infrastructure/Persistence/PosOpenDbContext.cs
- POSOpen/Infrastructure/Persistence/PersistenceServiceCollectionExtensions.cs
- POSOpen/Infrastructure/Persistence/Migrations/20260330154850_AddFamilyProfileTable.cs
- POSOpen/Infrastructure/Persistence/Migrations/20260330154850_AddFamilyProfileTable.Designer.cs
- POSOpen/Infrastructure/Persistence/Migrations/PosOpenDbContextModelSnapshot.cs
- POSOpen/Features/Admissions/AdmissionsRoutes.cs
- POSOpen/Features/Admissions/AdmissionsServiceCollectionExtensions.cs
- POSOpen/Features/Admissions/ViewModels/FamilyLookupViewModel.cs
- POSOpen/Features/Admissions/Views/FamilyLookupPage.xaml
- POSOpen/Features/Admissions/Views/FamilyLookupPage.xaml.cs
- POSOpen/Features/Admissions/Views/FamilyProfilePage.xaml
- POSOpen/Features/Admissions/Views/FamilyProfilePage.xaml.cs
- POSOpen/AppShell.xaml
- POSOpen/AppShell.xaml.cs
- POSOpen/MauiProgram.cs
- POSOpen.Tests/Unit/Admissions/SearchFamiliesUseCaseTests.cs
- POSOpen.Tests/Integration/Admissions/FamilyProfileRepositoryTests.cs
- POSOpen.Tests/POSOpen.Tests.csproj

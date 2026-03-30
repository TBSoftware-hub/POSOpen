# Story 3.1: Build Mixed-Cart Composition by Fulfillment Context

Status: ready-for-dev

## Story

As a cashier,
I want to create a cart containing admissions, retail, party deposit, and catering add-ons,
So that I can complete one combined transaction for the customer.

## Acceptance Criteria

**Given** I am building a transaction  
**When** I add items from multiple service categories  
**Then** the cart accepts all supported line-item types  
**And** each item stores its fulfillment context.

**Given** cart line items exist  
**When** I review the cart  
**Then** items are grouped by context (admissions/retail/party/catering)  
**And** totals are shown clearly.

**Given** item quantity or details are edited  
**When** updates are applied  
**Then** subtotals and totals recalculate correctly  
**And** no existing context mapping is lost.

## Tasks / Subtasks

- [ ] Domain model: Define cart entities and fulfillment context enum. (AC: 1)
  - [ ] Add `FulfillmentContext` enum to `POSOpen/Domain/Enums/FulfillmentContext.cs` with values: `Admission = 0`, `RetailItem = 1`, `PartyDeposit = 2`, `CateringAddon = 3`.
  - [ ] Add `CartStatus` enum to `POSOpen/Domain/Enums/CartStatus.cs` with values: `Open = 0`, `Completed = 1`, `Cancelled = 2`.
  - [ ] Add `CartSession` entity to `POSOpen/Domain/Entities/CartSession.cs` with static `Create(...)` factory, `Id`, `FamilyId?`, `StaffId`, `Status`, `CreatedAtUtc`, `UpdatedAtUtc`, `LineItems` (collection), and computed (not EF-mapped) `TotalAmountCents` = sum of `LineTotalCents` across items.
  - [ ] Add `CartLineItem` entity to `POSOpen/Domain/Entities/CartLineItem.cs` with static `Create(...)` factory, `Id`, `CartSessionId`, `Description` (max 200 chars), `FulfillmentContext`, `ReferenceId?`, `Quantity`, `UnitAmountCents`, `CurrencyCode`, `CreatedAtUtc`, `UpdatedAtUtc`, and computed (not EF-mapped) `LineTotalCents` = `Quantity * UnitAmountCents`.

- [ ] Application layer: Repository interface and use cases. (AC: 1, 2, 3)
  - [ ] Add `ICartSessionRepository` to `POSOpen/Application/Abstractions/Repositories/ICartSessionRepository.cs` with methods: `GetByIdAsync(Guid cartSessionId, CancellationToken)`, `GetOpenCartForStaffAsync(Guid staffId, CancellationToken)`, `AddAsync(CartSession, CancellationToken)`, `SaveChangesAsync(CancellationToken)`.
  - [ ] Add DTO types in `POSOpen/Application/UseCases/Checkout/`: `CartSessionDto` (maps from `CartSession` including grouped line items), `CartLineItemDto` (including `LineTotalCents`).
  - [ ] Add `GetOrCreateCartSessionUseCase` in `POSOpen/Application/UseCases/Checkout/GetOrCreateCartSessionUseCase.cs`: finds the open cart for a staff ID or creates a new one, returns `AppResult<CartSessionDto>`.
  - [ ] Add `AddCartLineItemCommand` record and `AddCartLineItemUseCase` in `POSOpen/Application/UseCases/Checkout/`: validates cart is Open and quantity > 0, adds `CartLineItem` to cart, saves, returns `AppResult<CartSessionDto>`.
  - [ ] Add `RemoveCartLineItemCommand` record and `RemoveCartLineItemUseCase`: validates item exists in cart, removes it, saves, returns `AppResult<CartSessionDto>`.
  - [ ] Add `UpdateCartLineItemQuantityCommand` record and `UpdateCartLineItemQuantityUseCase`: validates new quantity >= 1, updates `Quantity` and `UpdatedAtUtc`, saves, returns `AppResult<CartSessionDto>`.
  - [ ] All error codes follow canonical pattern: `CART_NOT_FOUND`, `CART_NOT_OPEN`, `INVALID_QUANTITY`, `LINE_ITEM_NOT_FOUND`.

- [ ] Infrastructure: EF configuration, DbContext, and repository implementation. (AC: 1, 2, 3)
  - [ ] Add `CartSessionConfiguration` in `POSOpen/Infrastructure/Persistence/Configurations/CartSessionConfiguration.cs`: table `cart_sessions`, snake_case column mapping, `CartStatus` stored as `int`, UTC datetime conversion, index on `staff_id + status`.
  - [ ] Add `CartLineItemConfiguration` in `POSOpen/Infrastructure/Persistence/Configurations/CartLineItemConfiguration.cs`: table `cart_line_items`, snake_case column mapping, `FulfillmentContext` stored as `int`, FK `cart_session_id → cart_sessions.id`, `Description` max 200, index on `cart_session_id`. Ignore computed properties `LineTotalCents`.
  - [ ] Add `DbSet<CartSession>` and `DbSet<CartLineItem>` to `PosOpenDbContext`.
  - [ ] Generate EF migration: `dotnet ef migrations add AddCartTables --project POSOpen --startup-project POSOpen --output-dir Infrastructure/Persistence/Migrations` — verify migration is clean and run `dotnet ef database update`.
  - [ ] Add `CartSessionRepository` in `POSOpen/Infrastructure/Persistence/Repositories/CartSessionRepository.cs`: implements `ICartSessionRepository`, uses `IDbContextFactory<PosOpenDbContext>`, eager-loads `LineItems` via `.Include(s => s.LineItems)` in all reads, `GetOpenCartForStaffAsync` filters `Status == CartStatus.Open`.
  - [ ] Ignore `CartSession.TotalAmountCents` and `CartLineItem.LineTotalCents` in EF configuration using `builder.Ignore(x => x.TotalAmountCents)` / `builder.Ignore(x => x.LineTotalCents)`.

- [ ] Presentation: Cart ViewModel, pages, and DI registration. (AC: 2, 3)
  - [ ] Create `CartViewModel` in `POSOpen/Features/Checkout/ViewModels/CartViewModel.cs` using `ObservableObject`. Observable properties: `ObservableCollection<CartLineItemGroupViewModel> ItemGroups`, `string GrandTotalLabel`, `string StatusMessage`, `bool IsLoading`, `bool HasItems`. Commands: `InitializeCommand` (calls `GetOrCreateCartSessionUseCase`), `RemoveItemCommand(Guid lineItemId)`, `UpdateQuantityCommand(Guid lineItemId, int newQuantity)`.
  - [ ] Create `CartLineItemGroupViewModel` (plain class, not ObservableObject) with `string GroupName`, `string GroupIcon`, `FulfillmentContext Context`, `ObservableCollection<CartLineItemViewModel> Items`, `string SubtotalLabel`. Recalculates `SubtotalLabel` from its items.
  - [ ] Create `CartLineItemViewModel` (`ObservableObject`) with `Guid Id`, `string Description`, `FulfillmentContext FulfillmentContext`, `[ObservableProperty] int Quantity`, `string UnitPriceLabel`, `string LineTotalLabel`. `Quantity` changes trigger line total recalculation.
  - [ ] Create `AddLineItemViewModel` in `POSOpen/Features/Checkout/ViewModels/AddLineItemViewModel.cs` with observable properties: `string Description`, `int Quantity = 1`, `string UnitPrice` (string entry, parsed to cents), `FulfillmentContext SelectedContext`, `IReadOnlyList<FulfillmentContext> AvailableContexts`, `string ErrorMessage`. Commands: `ConfirmAddCommand` (validates and returns via navigation + query parameter or MessagingCenter-free callback), `CancelCommand`.
  - [ ] `CartViewModel.RefreshGroupsFromDto(CartSessionDto)` rebuilds `ItemGroups` from DTO, grouping items by `FulfillmentContext` in canonical order (Admission → RetailItem → PartyDeposit → CateringAddon), computing `SubtotalLabel` per group and `GrandTotalLabel` as sum. Group names: `"Admissions"`, `"Retail"`, `"Party Deposit"`, `"Catering Add-ons"`. Group icons (emoji prefixes): 🎟 Admissions, 🛍 Retail, 🎉 Party Deposit, 🍽 Catering.
  - [ ] Create `CartPage.xaml` and code-behind in `POSOpen/Features/Checkout/Views/CartPage.xaml`: `CollectionView` with `IsGrouped="True"` bound to `ItemGroups`, group header shows group name + subtotal, each item row shows description / qty / unit price / line total + "Remove" `ImageButton`, bottom bar shows grand total + `"+ Add Item"` button (navigates to `AddLineItemPage`) + disabled `"Proceed to Payment"` button (Story 3.3 placeholder).
  - [ ] Create `AddLineItemPage.xaml` and code-behind in `POSOpen/Features/Checkout/Views/AddLineItemPage.xaml`: Picker for `FulfillmentContext`, `Entry` for description, `Entry` for quantity (numeric), `Entry` for unit price (decimal), Confirm/Cancel buttons. On confirm, calls `CartViewModel.AddItemAsync(command)` passed via BindingContext or query parameter pattern.
  - [ ] Create `CheckoutRoutes.cs` in `POSOpen/Features/Checkout/` with constants `Cart = "checkout/cart"`, `AddLineItem = "checkout/add-line-item"`.
  - [ ] Create `CheckoutServiceCollectionExtensions.cs` in `POSOpen/Features/Checkout/` registering use cases (Transient), `CartSessionRepository` as `ICartSessionRepository` (Scoped), ViewModel and pages (Transient), and routes.
  - [ ] Register `AddCheckoutFeature()` in `MauiProgram.cs`.
  - [ ] Add a "New Checkout" button on `HomePage.xaml` that navigates to `checkout/cart`.

- [ ] Tests: Unit and integration tests covering all ACs. (AC: 1, 2, 3)
  - [ ] Create `POSOpen.Tests/Unit/Checkout/CartUseCaseTests.cs` with tests: `AddCartLineItem_with_valid_item_adds_item_to_cart_and_returns_success`, `AddCartLineItem_with_quantity_zero_returns_invalid_quantity_error`, `AddCartLineItem_when_cart_not_found_returns_cart_not_found_error`, `RemoveCartLineItem_removes_item_and_returns_updated_cart`, `RemoveCartLineItem_when_item_not_in_cart_returns_not_found_error`, `UpdateCartLineItemQuantity_updates_quantity_and_recalculates_total`, `UpdateCartLineItemQuantity_with_quantity_zero_returns_invalid_quantity_error`.
  - [ ] Create `POSOpen.Tests/Unit/Checkout/CartViewModelTests.cs` with tests: `Initialize_creates_new_cart_and_populates_item_groups`, `RemoveItem_removes_item_from_groups_and_updates_grand_total`, `UpdateQuantity_recalculates_subtotals_and_grand_total_correctly`, `Items_grouped_in_canonical_order_Admission_Retail_PartyDeposit_CateringAddon`, `Grand_total_is_sum_of_all_line_item_totals`.
  - [ ] Create `POSOpen.Tests/Integration/CartSessionRepositoryTests.cs` with tests: `GetOrCreateOpenCart_creates_new_cart_when_none_exists`, `AddLineItem_persists_and_loads_with_correct_context`, `UpdateLineItemQuantity_persists_change_and_updates_timestamps`, `RemoveLineItem_removes_from_persistence`, `GetOpenCartForStaff_returns_open_cart_not_completed`.

## Dev Notes

### Story Intent

Story 3.1 establishes the mixed-cart composition infrastructure — domain model, persistence, use cases, and a functional composition UI — that all subsequent Epic 3 stories build on. This is a **greenfield feature domain** (Checkout); no Checkout folder or entities exist yet. Every new file must be created from scratch following established project conventions.

### Key Architecture Guardrails (MUST follow)

1. **Layer boundary**: ViewModel calls use case; use case calls repository; repository accesses `PosOpenDbContext`. No XAML code-behind logic, no ViewModel → DbContext direct access.
2. **Dependency injection**: All use cases are `Transient`. Repository is `Scoped`. ViewModels and Pages are `Transient`. Register everything in `CheckoutServiceCollectionExtensions.cs`; call `AddCheckoutFeature()` from `MauiProgram.cs`.
3. **Result envelope**: All use case returns are `AppResult<CartSessionDto>` — never throw exceptions out of use case layer for domain failures.  
4. **Naming conventions**: PascalCase C#, snake_case SQLite tables/columns, `_camelCase` private fields, `ICartSessionRepository` prefix, `CartSessionRepository` implementation, `CartViewModel` suffix, `CartPage` suffix.
5. **UTC timestamps**: Always use `DateTime.UtcNow` in entity factories; persist and read as UTC (use `UtcDateTimeConverter` from existing `ValueConverters` folder).
6. **EF patterns**: Use `IDbContextFactory<PosOpenDbContext>` in repositories. Eager-load `LineItems` via `.Include(s => s.LineItems)`. Ignore computed properties via `builder.Ignore(...)` in the EF configuration.
7. **MVVM pattern**: `CartViewModel` inherits `ObservableObject`, uses `[ObservableProperty]` and `[RelayCommand]`. Commands must be async where they call use cases. No direct constructor navigation logic.
8. **CommunityToolkit.Mvvm**: Already installed. Use `ObservableObject`, `[ObservableProperty]`, `[RelayCommand]` as in `FastPathCheckInViewModel`.
9. **Error codes**: Define all error code constants in a `CartCheckoutConstants.cs` file adjacent to the use cases, not inline as magic strings.

### Existing Patterns to Follow Exactly

- **EF configuration**: Match `AdmissionCheckInRecordConfiguration.cs` exactly for table naming, column mapping, `HasConversion<int>()` for enums, `UtcDateTimeConverter.Instance` for UTC datetimes, index naming pattern `ix_{table}_{column}`.
- **Repository pattern**: Match `AdmissionCheckInRepository.cs` — use `IDbContextFactory<PosOpenDbContext>`, factory creates context per operation, use `await using var dbContext = ...`, use `.Include()` for related data.
- **Feature DI extension**: Match `AdmissionsServiceCollectionExtensions.cs` — same `IServiceCollection` extension method, same `Routing.RegisterRoute()` calls.
- **Page code-behind**: Match `FastPathCheckInPage.xaml.cs` — constructor receives ViewModel, sets `BindingContext` to VM, `OnAppearing` invokes `InitializeCommand`.
- **AppResult usage**: Match use cases in `POSOpen/Application/UseCases/Admissions/` — `AppResult<T>.Success(payload, message)` and `AppResult<T>.Failure(errorCode, userMessage)`.
- **ObservableCollection grouping**: For `CollectionView` with `IsGrouped="True"`, the binding source must be a collection whose items themselves have an `IEnumerable` `Items` property. Bind `CollectionView.ItemsSource` to `ItemGroups`. Each `CartLineItemGroupViewModel` IS the group header data. Use `CollectionView.GroupHeaderTemplate` and `CollectionView.ItemTemplate`.
- **ViewModel test pattern**: Match `FastPathCheckInViewModelTests.cs` — constructor injects Moq mocks, no DI container in tests, arrange-act-assert, FluentAssertions.

### Computed Properties & EF Ignore

Both `CartSession.TotalAmountCents` and `CartLineItem.LineTotalCents` are pure computed properties (not stored):

```csharp
// In CartSession.cs:
public long TotalAmountCents => LineItems.Sum(i => i.LineTotalCents);

// In CartLineItem.cs:
public long LineTotalCents => (long)Quantity * UnitAmountCents;
```

In EF configuration, explicitly ignore them:
```csharp
builder.Ignore(s => s.TotalAmountCents);      // in CartSessionConfiguration
builder.Ignore(i => i.LineTotalCents);          // in CartLineItemConfiguration
```

### Currency Formatting

Format monetary amounts for display using:
```csharp
private static string FormatCents(long cents) =>
    (cents / 100m).ToString("C", System.Globalization.CultureInfo.CurrentCulture);
```

Centralise this in a static helper within `CartViewModel` or a shared extension. Do NOT add a new project-wide static class for this — keep it scoped.

### FulfillmentContext Enum Order and Display Names

Canonical order for grouping (use ordinal sort on the enum int value):
- `Admission = 0` → "Admissions" / icon "🎟"
- `RetailItem = 1` → "Retail" / icon "🛍"
- `PartyDeposit = 2` → "Party Deposit" / icon "🎉"
- `CateringAddon = 3` → "Catering Add-ons" / icon "🍽"

In `CartViewModel.RefreshGroupsFromDto`, group by `FulfillmentContext`, then order groups by `(int)context` ascending.

### CartSession Lifecycle for Story 3.1

- One open cart per staff member at a time (enforced by `GetOpenCartForStaffAsync` which returns any existing `CartStatus.Open` cart for the staff ID).
- `GetOrCreateCartSessionUseCase` returns existing open cart if found, or creates a new one.
- Story 3.1 does NOT close/complete carts — that's Epic 3 completion stories (3.2/3.3). `CartStatus.Completed` and `Cancelled` are defined but not exercised in Story 3.1.
- `FamilyId` is optional (nullable) in Story 3.1 — family association happens when the cart is linked to an admission, which is a cross-story concern.

### StaffId for Story 3.1

For Story 3.1, the `CartViewModel` must obtain the current staff ID. Use the existing `IAppStateService` (already registered in DI) which exposes `CurrentStaffId`. Inject `IAppStateService` into `CartViewModel`.

Look at `HomeViewModel.cs` for the pattern of injecting `IAppStateService`.

```csharp
// IAppStateService is in Application/Abstractions/Services/
// Already registered as singleton in MauiProgram.cs
private readonly IAppStateService _appStateService;
```

### Integration Test Pattern

Use the same in-memory SQLite pattern as existing integration tests. Check `POSOpen.Tests/Integration/` for examples. Use `EF Core InMemory` provider or a real SQLite file path with `Microsoft.Data.Sqlite`.

Look at existing test infrastructure setup: any `TestDbContextFactory` or `AdmissionCheckInRepositoryTests` in the integration folder for the setup pattern.

### Migration Command (run from solution root)

```bash
dotnet ef migrations add AddCartTables \
  --project POSOpen \
  --startup-project POSOpen \
  --output-dir Infrastructure/Persistence/Migrations
dotnet ef database update --project POSOpen --startup-project POSOpen
```

Or on Windows PowerShell:
```powershell
dotnet ef migrations add AddCartTables --project POSOpen --startup-project POSOpen --output-dir Infrastructure/Persistence/Migrations
dotnet ef database update --project POSOpen --startup-project POSOpen
```

### UI Design Notes

**CartPage layout (top to bottom):**
```
[Header: "Cart" / Grand Total badge]
[CollectionView — IsGrouped=True]
  Group Header: "🎟 Admissions   Subtotal: $12.00"
  Item row: "Walk-in Adult x2   $6.00 each   $12.00   [Remove]"
  Group Header: "🛍 Retail   Subtotal: $8.50"  
  Item row: "Grip Socks x1   $8.50 each   $8.50   [Remove]"
[Bottom bar: "Grand Total: $20.50"  |  "+ Add Item"  |  "Proceed to Payment" (disabled)]
```

**AddLineItemPage layout:**
```
[Header: "Add Item"]
[Picker: Item Type (Admissions / Retail / Party Deposit / Catering Add-on)]
[Entry: Description]
[Entry: Quantity (numeric)]
[Entry: Unit Price (decimal, e.g. "6.00")]
[Error message label]
[Buttons: "Add"  |  "Cancel"]
```

Button styles must match the established button hierarchy from Story 2.5 (use `PrimaryActionButtonStyle` for primary actions, `SecondaryActionButtonStyle` for secondary/cancel).

### Test Double Pattern

For use case tests, mock `ICartSessionRepository`:
```csharp
var repoMock = new Mock<ICartSessionRepository>();
repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(existingCart);
repoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
        .Returns(Task.CompletedTask);
```

For `CartViewModel` tests, mock both `ICartSessionRepository` (via use cases) and `IAppStateService`. Pass use case instances constructed with the mock repository into the ViewModel constructor.

### What NOT to Do

- Do NOT add `CartSession` items inline in `PosOpenDbContext` without corresponding `IEntityTypeConfiguration` — use `ApplyConfigurationsFromAssembly` (already configured).
- Do NOT call navigation from inside use cases — navigation is ViewModel concern only.
- Do NOT skip the EF migration — tests requiring integration persistence will fail without it.
- Do NOT use `[NotMapped]` attribute on `LineTotalCents` — use EF fluent `builder.Ignore(...)` instead (consistent with existing project style).
- Do NOT use `MessagingCenter` — it is deprecated in .NET MAUI. Use shell query parameters or direct ViewModel method calls.
- Do NOT add a new DB context — only `PosOpenDbContext` exists and all entities go through it.
- Do NOT invent cart-locking or concurrency control — V1 is single-terminal, no concurrent writes.
- Do NOT make `CartLineItemGroupViewModel` an `ObservableObject` unless absolutely needed — it is a display projection rebuilt from DTO on each state update.

### Previous Story Learnings

From Story 2.4/2.5 implementation:
- Use `IDbContextFactory` over injecting `DbContext` directly (avoids lifetime conflicts with Transient use cases).
- Always eager-load related entities in the repository `.Include()` — do not rely on lazy loading (not configured).
- `OnAppearing` in page code-behind should invoke the ViewModel initialize command asynchronously.
- Keep ViewModel state rebuilds total (rebuild entire `ItemGroups` from DTO) rather than partial — simpler, less bug-prone.
- Test all observable property changes with `FluentAssertions` `.Should().Be(...)` pattern.
- EF config: always specify `IsRequired()` for non-null properties, always specify max lengths.
- Keep constants (error codes, messages) in a dedicated `*Constants.cs` file, co-located with the use cases.

### Files to Create (summary)

| File | Purpose |
|------|---------|
| `POSOpen/Domain/Enums/FulfillmentContext.cs` | Fulfillment context enum |
| `POSOpen/Domain/Enums/CartStatus.cs` | Cart lifecycle enum |
| `POSOpen/Domain/Entities/CartSession.cs` | Cart aggregate root |
| `POSOpen/Domain/Entities/CartLineItem.cs` | Cart line item entity |
| `POSOpen/Application/Abstractions/Repositories/ICartSessionRepository.cs` | Repository contract |
| `POSOpen/Application/UseCases/Checkout/CartCheckoutConstants.cs` | Error codes and messages |
| `POSOpen/Application/UseCases/Checkout/CartSessionDto.cs` | Cart DTO (includes line items) |
| `POSOpen/Application/UseCases/Checkout/GetOrCreateCartSessionUseCase.cs` | Load or create open cart |
| `POSOpen/Application/UseCases/Checkout/AddCartLineItemUseCase.cs` | Add item use case |
| `POSOpen/Application/UseCases/Checkout/RemoveCartLineItemUseCase.cs` | Remove item use case |
| `POSOpen/Application/UseCases/Checkout/UpdateCartLineItemQuantityUseCase.cs` | Update qty use case |
| `POSOpen/Infrastructure/Persistence/Configurations/CartSessionConfiguration.cs` | EF table config |
| `POSOpen/Infrastructure/Persistence/Configurations/CartLineItemConfiguration.cs` | EF table config |
| `POSOpen/Infrastructure/Persistence/Repositories/CartSessionRepository.cs` | EF repository impl |
| `POSOpen/Features/Checkout/CheckoutRoutes.cs` | Route constants |
| `POSOpen/Features/Checkout/CheckoutServiceCollectionExtensions.cs` | DI registration |
| `POSOpen/Features/Checkout/ViewModels/CartViewModel.cs` | Cart ViewModel |
| `POSOpen/Features/Checkout/ViewModels/CartLineItemGroupViewModel.cs` | Group VM |
| `POSOpen/Features/Checkout/ViewModels/CartLineItemViewModel.cs` | Item VM |
| `POSOpen/Features/Checkout/ViewModels/AddLineItemViewModel.cs` | Add item VM |
| `POSOpen/Features/Checkout/Views/CartPage.xaml` | Cart page |
| `POSOpen/Features/Checkout/Views/CartPage.xaml.cs` | Cart page code-behind |
| `POSOpen/Features/Checkout/Views/AddLineItemPage.xaml` | Add item page |
| `POSOpen/Features/Checkout/Views/AddLineItemPage.xaml.cs` | Add item page code-behind |
| `POSOpen.Tests/Unit/Checkout/CartUseCaseTests.cs` | Use case unit tests |
| `POSOpen.Tests/Unit/Checkout/CartViewModelTests.cs` | ViewModel unit tests |
| `POSOpen.Tests/Integration/CartSessionRepositoryTests.cs` | Repository integration tests |

### Files to Modify (summary)

| File | Change |
|------|--------|
| `POSOpen/Infrastructure/Persistence/PosOpenDbContext.cs` | Add `DbSet<CartSession>`, `DbSet<CartLineItem>` |
| `POSOpen/MauiProgram.cs` | Add `builder.Services.AddCheckoutFeature()` |
| `POSOpen/Features/Shell/Views/HomePage.xaml` | Add "New Checkout" navigation button |
| `POSOpen/Features/Shell/ViewModels/HomeViewModel.cs` | Add `OpenCheckoutCommand` |
| `_bmad-output/implementation-artifacts/sprint-status.yaml` | Update 3-1 to `in-progress` |

## Dev Agent Record

### Debug Log

_To be updated by dev agent during implementation._

### Completion Notes

_To be updated by dev agent upon completion._

### File List

_To be updated by dev agent during implementation._

### Change Log

_To be updated by dev agent during implementation._

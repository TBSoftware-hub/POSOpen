# Story 3.2 — Enforce Compatibility Rules and Guided Resolution

## Metadata

| Field            | Value                                                    |
|------------------|----------------------------------------------------------|
| Epic             | 3 — Mixed-Cart Composition & Checkout                    |
| Story            | 3.2                                                      |
| Key              | `3-2-enforce-compatibility-rules-and-guided-resolution`  |
| Status           | in-progress                                              |
| Author           | Timbe (via BMAD Story Creator)                           |
| Created          | 2026-03-31                                               |
| Target Sprint    | Current                                                  |

---

## User Story

**As a** front-desk cashier,  
**I want** the system to evaluate compatibility rules before cart completion,  
**So that** I can see exactly what's wrong and quickly fix it without losing my work.

---

## Context

Story 3.1 established the cart composition workflow: staff can build a mixed-cart across
fulfillment contexts. Proceeding to payment was left as a **disabled placeholder**.

This story activates the Proceed to Payment button by gating it on a compatibility-rule
evaluation. The evaluation runs automatically whenever the cart changes. If blocking issues
exist, the cashier sees them inline with suggested one-tap fixes. Once all issues are
resolved, the Proceed to Payment button enables and the session transitions to checkout.

Actual payment processing (card/cash capture) is deferred to Story 3.3.

---

## Acceptance Criteria

### AC-1 — Blocking issues are surfaced with actionable messages

> **Given** a cashier is viewing a cart with compatibility issues  
> **When** automatic validation runs  
> **Then** each blocking issue appears inline above the action buttons  
> **And** each issue includes a message and (where applicable) a one-tap fix button  
> **And** the Proceed to Payment button remains disabled

### AC-2 — Cashier chooses a suggested fix

> **Given** a blocking issue is displayed with a fix button  
> **When** the cashier taps the fix button  
> **Then** the fix is applied to the cart in-place (the cart is not cleared)  
> **And** validation reruns automatically

### AC-3 — Validation reruns and clears on resolution

> **Given** a fix has been applied  
> **When** validation reruns  
> **Then** resolved issues disappear from the list  
> **And** the Proceed to Payment button enables if no blocking issues remain

### AC-4 — Validation completes within 2 seconds (NFR3)

> **Given** a mixed cart is submitted for validation  
> **When** compatibility rules are evaluated  
> **Then** validation result is returned within 2 seconds even for a cart with items in  
> all fulfillment contexts

---

## V1 Compatibility Rules

The following three rules cover the domain scenarios identified for this first release.
Each returns zero or one `CartValidationIssue`. All issues in V1 are severity `Blocking`.

| Code | Trigger Condition | Message | Fix Available? | Fix Action |
|------|-------------------|---------|---------------|------------|
| `CART_EMPTY` | `cart.LineItems.Count == 0` | "The cart is empty. Add at least one item to proceed." | No | `None` |
| `CATERING_WITHOUT_PARTY_DEPOSIT` | CateringAddon items exist but zero PartyDeposit items | "Catering add-ons require a party deposit in the cart." | Yes | `RemoveCateringItems` |
| `MULTIPLE_PARTY_DEPOSITS` | More than one PartyDeposit line-item | "Only one party deposit is allowed per cart." | Yes | `KeepOldestPartyDeposit` |

---

## Architecture Guardrails

- **Layer boundary:** `CartValidationIssue` and `ICartCompatibilityRule` live in
  `Domain/Policies/`. DTOs live in `Application`. ViewModels live in `Features/`.
- **No new repository methods required.** Validation is pure in-memory against the already-
  loaded `CartSession`. The use case loads the cart via the existing repository.
- **DI rule-discovery pattern:** Register each concrete rule as
  `AddTransient<ICartCompatibilityRule, ConcreteRule>()`. The use case injects
  `IEnumerable<ICartCompatibilityRule>` and evaluates all registered rules.
- **No EF migration.** No schema changes in this story.
- **`AppResult<TPayload>` envelope** used for `ValidateCartCompatibilityUseCase` return type.
- **CommunityToolkit.MVVM:** `[ObservableProperty]`, `[RelayCommand]`, `[NotifyPropertyChangedFor]` throughout.
- **Validation is automatic:** Runs after `InitializeAsync` succeeds and after any successful
  cart mutation (remove, increment, decrement). No separate "Validate" button.
- **Proceed to Payment** changes from `IsEnabled="False"` (hardcoded) to
  `IsEnabled="{Binding IsCartValid}"`. A `ProceedToPaymentCommand` stub is added; actual
  payment flow is implemented in Story 3.3.

---

## Files to Create

### `POSOpen/Domain/Enums/ValidationSeverity.cs`

```csharp
namespace POSOpen.Domain.Enums;

/// <summary>Severity of a cart compatibility issue.</summary>
public enum ValidationSeverity
{
    Blocking = 0,
}
```

### `POSOpen/Domain/Enums/CartValidationFixAction.cs`

```csharp
namespace POSOpen.Domain.Enums;

/// <summary>
/// Identifies the automated fix action the ViewModel should apply
/// when a cashier taps a suggested-fix button.
/// </summary>
public enum CartValidationFixAction
{
    /// <summary>No automated fix; informational issue only.</summary>
    None = 0,

    /// <summary>Remove all CateringAddon line items from the cart.</summary>
    RemoveCateringItems = 1,

    /// <summary>
    /// Remove all but the first (oldest) PartyDeposit line item.
    /// Items are already ordered by CreatedAtUtc in the DTO, so Skip(1) is safe.
    /// </summary>
    KeepOldestPartyDeposit = 2,
}
```

### `POSOpen/Domain/Policies/CartValidationIssue.cs`

```csharp
using POSOpen.Domain.Enums;

namespace POSOpen.Domain.Policies;

/// <summary>
/// Immutable value object representing a single compatibility issue found in a cart.
/// Produced by <see cref="ICartCompatibilityRule"/> implementations.
/// </summary>
public sealed record CartValidationIssue(
    string Code,
    ValidationSeverity Severity,
    string Message,
    string? FixLabel,
    CartValidationFixAction FixAction);
```

### `POSOpen/Domain/Policies/ICartCompatibilityRule.cs`

```csharp
using POSOpen.Domain.Entities;

namespace POSOpen.Domain.Policies;

/// <summary>
/// A single cart compatibility rule.  Register each implementation as
/// <c>AddTransient&lt;ICartCompatibilityRule, ConcreteRule&gt;()</c> so that
/// <see cref="POSOpen.Application.UseCases.Checkout.ValidateCartCompatibilityUseCase"/>
/// discovers them via <c>IEnumerable&lt;ICartCompatibilityRule&gt;</c> injection.
/// </summary>
public interface ICartCompatibilityRule
{
    IReadOnlyList<CartValidationIssue> Evaluate(CartSession cart);
}
```

### `POSOpen/Domain/Policies/CartMustHaveItemsRule.cs`

```csharp
using POSOpen.Domain.Entities;
using POSOpen.Domain.Enums;

namespace POSOpen.Domain.Policies;

/// <summary>Blocks checkout when the cart has no line items.</summary>
public sealed class CartMustHaveItemsRule : ICartCompatibilityRule
{
    public IReadOnlyList<CartValidationIssue> Evaluate(CartSession cart)
    {
        if (cart.LineItems.Count == 0)
        {
            return
            [
                new CartValidationIssue(
                    Code: "CART_EMPTY",
                    Severity: ValidationSeverity.Blocking,
                    Message: "The cart is empty. Add at least one item to proceed.",
                    FixLabel: null,
                    FixAction: CartValidationFixAction.None)
            ];
        }

        return [];
    }
}
```

### `POSOpen/Domain/Policies/CateringRequiresPartyDepositRule.cs`

```csharp
using POSOpen.Domain.Entities;
using POSOpen.Domain.Enums;

namespace POSOpen.Domain.Policies;

/// <summary>
/// Blocks checkout when CateringAddon items exist in the cart but no
/// PartyDeposit item is present.
/// </summary>
public sealed class CateringRequiresPartyDepositRule : ICartCompatibilityRule
{
    public IReadOnlyList<CartValidationIssue> Evaluate(CartSession cart)
    {
        bool hasCatering     = cart.LineItems.Any(i => i.Context == FulfillmentContext.CateringAddon);
        bool hasPartyDeposit = cart.LineItems.Any(i => i.Context == FulfillmentContext.PartyDeposit);

        if (hasCatering && !hasPartyDeposit)
        {
            return
            [
                new CartValidationIssue(
                    Code: "CATERING_WITHOUT_PARTY_DEPOSIT",
                    Severity: ValidationSeverity.Blocking,
                    Message: "Catering add-ons require a party deposit in the cart.",
                    FixLabel: "Remove catering items",
                    FixAction: CartValidationFixAction.RemoveCateringItems)
            ];
        }

        return [];
    }
}
```

> **Note:** `CartLineItem.Context` is the navigation property name for `FulfillmentContext`.
> Verify against the actual entity property name in `CartLineItem.cs` before implementing
> — adjust the property access accordingly if it differs.

### `POSOpen/Domain/Policies/SinglePartyDepositRule.cs`

```csharp
using POSOpen.Domain.Entities;
using POSOpen.Domain.Enums;

namespace POSOpen.Domain.Policies;

/// <summary>
/// Blocks checkout when the cart contains more than one PartyDeposit line item.
/// </summary>
public sealed class SinglePartyDepositRule : ICartCompatibilityRule
{
    public IReadOnlyList<CartValidationIssue> Evaluate(CartSession cart)
    {
        int depositCount = cart.LineItems.Count(i => i.Context == FulfillmentContext.PartyDeposit);

        if (depositCount > 1)
        {
            return
            [
                new CartValidationIssue(
                    Code: "MULTIPLE_PARTY_DEPOSITS",
                    Severity: ValidationSeverity.Blocking,
                    Message: "Only one party deposit is allowed per cart.",
                    FixLabel: "Keep first deposit, remove extras",
                    FixAction: CartValidationFixAction.KeepOldestPartyDeposit)
            ];
        }

        return [];
    }
}
```

### `POSOpen/Application/UseCases/Checkout/CartValidationDto.cs`

```csharp
using POSOpen.Domain.Enums;

namespace POSOpen.Application.UseCases.Checkout;

public sealed record CartValidationIssueDto(
    string Code,
    ValidationSeverity Severity,
    string Message,
    string? FixLabel,
    CartValidationFixAction FixAction);

public sealed record CartValidationResultDto(
    bool IsValid,
    IReadOnlyList<CartValidationIssueDto> Issues);
```

### `POSOpen/Application/UseCases/Checkout/ValidateCartCompatibilityUseCase.cs`

```csharp
using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.Results;
using POSOpen.Domain.Policies;

namespace POSOpen.Application.UseCases.Checkout;

/// <summary>
/// Evaluates all registered <see cref="ICartCompatibilityRule"/> instances against a cart
/// session.  Rules are discovered via DI's <c>IEnumerable&lt;ICartCompatibilityRule&gt;</c>
/// registration—add new rules without touching this class.
/// </summary>
public sealed class ValidateCartCompatibilityUseCase(
    ICartSessionRepository repository,
    IEnumerable<ICartCompatibilityRule> rules)
{
    public async Task<AppResult<CartValidationResultDto>> ExecuteAsync(Guid cartSessionId)
    {
        var cart = await repository.GetByIdAsync(cartSessionId);
        if (cart is null)
            return AppResult<CartValidationResultDto>.Failure(
                CartCheckoutConstants.ErrorCartNotFound,
                CartCheckoutConstants.SafeCartNotFoundMessage);

        var issues = rules
            .SelectMany(r => r.Evaluate(cart))
            .Select(i => new CartValidationIssueDto(
                i.Code, i.Severity, i.Message, i.FixLabel, i.FixAction))
            .ToList();

        var resultDto = new CartValidationResultDto(
            IsValid: issues.Count == 0,
            Issues: issues);

        return AppResult<CartValidationResultDto>.Success(resultDto);
    }
}
```

### `POSOpen/Features/Checkout/ViewModels/ValidationIssueViewModel.cs`

```csharp
using POSOpen.Domain.Enums;

namespace POSOpen.Features.Checkout.ViewModels;

/// <summary>Plain-object ViewModel for a single cart validation issue.</summary>
public sealed class ValidationIssueViewModel
{
    public required string Message { get; init; }
    public string? FixLabel { get; init; }
    public CartValidationFixAction FixAction { get; init; }
    public bool HasFix => FixAction != CartValidationFixAction.None;
}
```

### `POSOpen.Tests/Unit/Checkout/CartCompatibilityRuleTests.cs`

```csharp
using FluentAssertions;
using POSOpen.Domain.Entities;
using POSOpen.Domain.Enums;
using POSOpen.Domain.Policies;
using Xunit;

namespace POSOpen.Tests.Unit.Checkout;

public class CartCompatibilityRuleTests
{
    // --- Helpers ---

    private static CartSession EmptyCart() =>
        CartSession.Create(Guid.NewGuid(), null);

    private static CartSession CartWith(params FulfillmentContext[] contexts)
    {
        var cart = EmptyCart();
        foreach (var ctx in contexts)
            cart.AddLineItem(ctx, null, "Item", 1, 1000, "USD");
        return cart;
    }

    // --- CartMustHaveItemsRule ---

    [Fact]
    public void CartMustHaveItemsRule_EmptyCart_ReturnsOneBlockingIssue()
    {
        var rule = new CartMustHaveItemsRule();
        var issues = rule.Evaluate(EmptyCart());
        issues.Should().HaveCount(1);
        issues[0].Code.Should().Be("CART_EMPTY");
        issues[0].Severity.Should().Be(ValidationSeverity.Blocking);
        issues[0].FixAction.Should().Be(CartValidationFixAction.None);
    }

    [Fact]
    public void CartMustHaveItemsRule_CartWithItems_ReturnsEmpty()
    {
        var rule = new CartMustHaveItemsRule();
        var issues = rule.Evaluate(CartWith(FulfillmentContext.Admission));
        issues.Should().BeEmpty();
    }

    // --- CateringRequiresPartyDepositRule ---

    [Fact]
    public void CateringRequiresPartyDepositRule_CateringWithoutDeposit_ReturnsIssue()
    {
        var rule = new CateringRequiresPartyDepositRule();
        var issues = rule.Evaluate(CartWith(FulfillmentContext.CateringAddon));
        issues.Should().HaveCount(1);
        issues[0].Code.Should().Be("CATERING_WITHOUT_PARTY_DEPOSIT");
        issues[0].FixAction.Should().Be(CartValidationFixAction.RemoveCateringItems);
        issues[0].FixLabel.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void CateringRequiresPartyDepositRule_CateringWithDeposit_ReturnsEmpty()
    {
        var rule = new CateringRequiresPartyDepositRule();
        var issues = rule.Evaluate(CartWith(FulfillmentContext.CateringAddon, FulfillmentContext.PartyDeposit));
        issues.Should().BeEmpty();
    }

    [Fact]
    public void CateringRequiresPartyDepositRule_NoCateringAtAll_ReturnsEmpty()
    {
        var rule = new CateringRequiresPartyDepositRule();
        var issues = rule.Evaluate(CartWith(FulfillmentContext.Admission));
        issues.Should().BeEmpty();
    }

    // --- SinglePartyDepositRule ---

    [Fact]
    public void SinglePartyDepositRule_TwoDeposits_ReturnsIssue()
    {
        var rule = new SinglePartyDepositRule();
        var issues = rule.Evaluate(CartWith(
            FulfillmentContext.PartyDeposit,
            FulfillmentContext.PartyDeposit));
        issues.Should().HaveCount(1);
        issues[0].Code.Should().Be("MULTIPLE_PARTY_DEPOSITS");
        issues[0].FixAction.Should().Be(CartValidationFixAction.KeepOldestPartyDeposit);
    }

    [Fact]
    public void SinglePartyDepositRule_OneDeposit_ReturnsEmpty()
    {
        var rule = new SinglePartyDepositRule();
        var issues = rule.Evaluate(CartWith(FulfillmentContext.PartyDeposit));
        issues.Should().BeEmpty();
    }

    [Fact]
    public void SinglePartyDepositRule_NoDeposit_ReturnsEmpty()
    {
        var rule = new SinglePartyDepositRule();
        var issues = rule.Evaluate(EmptyCart());
        issues.Should().BeEmpty();
    }
}
```

> **Prerequisite:** `CartSession.AddLineItem` must accept `FulfillmentContext` and produce a
> loadable entity. Verify the exact method signature on the entity and adjust the `CartWith`
> helper accordingly.

### `POSOpen.Tests/Unit/Checkout/ValidateCartCompatibilityUseCaseTests.cs`

```csharp
using FluentAssertions;
using Moq;
using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.UseCases.Checkout;
using POSOpen.Domain.Entities;
using POSOpen.Domain.Policies;
using Xunit;

namespace POSOpen.Tests.Unit.Checkout;

public class ValidateCartCompatibilityUseCaseTests
{
    private static Mock<ICartSessionRepository> MockRepo(CartSession? cart = null)
    {
        var mock = new Mock<ICartSessionRepository>();
        mock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(cart);
        return mock;
    }

    [Fact]
    public async Task ExecuteAsync_CartNotFound_ReturnsFailure()
    {
        var sut = new ValidateCartCompatibilityUseCase(
            MockRepo(null).Object,
            Enumerable.Empty<ICartCompatibilityRule>());

        var result = await sut.ExecuteAsync(Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(CartCheckoutConstants.ErrorCartNotFound);
    }

    [Fact]
    public async Task ExecuteAsync_EmptyCart_IsValidFalseAndOneIssue()
    {
        var cart = CartSession.Create(Guid.NewGuid(), null);
        var sut = new ValidateCartCompatibilityUseCase(
            MockRepo(cart).Object,
            [new CartMustHaveItemsRule()]);

        var result = await sut.ExecuteAsync(cart.Id);

        result.IsSuccess.Should().BeTrue();
        result.Payload!.IsValid.Should().BeFalse();
        result.Payload.Issues.Should().HaveCount(1);
        result.Payload.Issues[0].Code.Should().Be("CART_EMPTY");
    }

    [Fact]
    public async Task ExecuteAsync_MultipleRulesWithNoIssues_IsValidTrue()
    {
        var cart = CartSession.Create(Guid.NewGuid(), null);
        // Add one admission item — no compatibility issues
        cart.AddLineItem(FulfillmentContext.Admission, null, "Ticket", 1, 1500, "USD");

        ICartCompatibilityRule[] rules =
        [
            new CartMustHaveItemsRule(),
            new CateringRequiresPartyDepositRule(),
            new SinglePartyDepositRule(),
        ];

        var sut = new ValidateCartCompatibilityUseCase(
            MockRepo(cart).Object,
            rules);

        var result = await sut.ExecuteAsync(cart.Id);

        result.IsSuccess.Should().BeTrue();
        result.Payload!.IsValid.Should().BeTrue();
        result.Payload.Issues.Should().BeEmpty();
    }

    // V1 rules are intentionally mutually exclusive at the issue level:
    //   CART_EMPTY cannot coexist with CATERING_WITHOUT_PARTY_DEPOSIT (catering needs items).
    //   CATERING_WITHOUT_PARTY_DEPOSIT cannot coexist with MULTIPLE_PARTY_DEPOSITS
    //   (the latter requires deposits, which silences the former).
    // This test verifies that the aggregation pipeline works end-to-end for a realistic
    // multi-item cart where exactly one rule fires.
    [Fact]
    public async Task ExecuteAsync_TwoDepositsWithCatering_OnlyMultipleDepositsViolationFires()
    {
        var cart = CartSession.Create(Guid.NewGuid(), null);
        // Cart: catering item + two party deposits.
        //   CartMustHaveItemsRule           → silent (has items)
        //   CateringRequiresPartyDeposit    → silent (a deposit is present)
        //   SinglePartyDepositRule          → FIRES (two deposits)
        cart.AddLineItem(FulfillmentContext.CateringAddon, null, "Catering", 1, 800, "USD");
        cart.AddLineItem(FulfillmentContext.PartyDeposit,  null, "Deposit A", 1, 5000, "USD");
        cart.AddLineItem(FulfillmentContext.PartyDeposit,  null, "Deposit B", 1, 5000, "USD");

        ICartCompatibilityRule[] rules =
        [
            new CartMustHaveItemsRule(),
            new CateringRequiresPartyDepositRule(),
            new SinglePartyDepositRule(),
        ];

        var sut = new ValidateCartCompatibilityUseCase(
            MockRepo(cart).Object,
            rules);

        var result = await sut.ExecuteAsync(cart.Id);

        result.IsSuccess.Should().BeTrue();
        result.Payload!.IsValid.Should().BeFalse();
        result.Payload.Issues.Should().HaveCount(1);
        result.Payload.Issues[0].Code.Should().Be("MULTIPLE_PARTY_DEPOSITS");
    }
}
```

---

## Files to Modify

### `POSOpen/Application/UseCases/Checkout/CartCheckoutConstants.cs`

Add three validation error codes and three corresponding safe messages at the end of the
existing constants class. Preserve all existing members.

```csharp
// Additions only — add after existing error constants:

public const string ErrorCartEmpty = "CART_EMPTY";
public const string SafeCartEmpty =
    "The cart is empty. Add at least one item before proceeding to payment.";

public const string ErrorCateringWithoutDeposit = "CATERING_WITHOUT_PARTY_DEPOSIT";
public const string SafeCateringWithoutDeposit =
    "Catering add-ons require a party deposit in the cart.";

public const string ErrorMultipleDeposits = "MULTIPLE_PARTY_DEPOSITS";
public const string SafeMultipleDeposits =
    "Only one party deposit is allowed per cart.";
```

### `POSOpen/Features/Checkout/ViewModels/CartViewModel.cs`

Full diff of changes:

1. **Add using statements:**
   ```csharp
   using POSOpen.Domain.Enums;
   ```

2. **Extend constructor** — add `ValidateCartCompatibilityUseCase validateCartCompatibility`:
   ```csharp
   private readonly ValidateCartCompatibilityUseCase _validateCartCompatibility;

   public CartViewModel(
       GetOrCreateCartSessionUseCase getOrCreateCartSession,
       RemoveCartLineItemUseCase removeCartLineItem,
       UpdateCartLineItemQuantityUseCase updateCartLineItemQuantity,
       ValidateCartCompatibilityUseCase validateCartCompatibility,
       ICheckoutUiService uiService)
   {
       _getOrCreateCartSession = getOrCreateCartSession;
       _removeCartLineItem = removeCartLineItem;
       _updateCartLineItemQuantity = updateCartLineItemQuantity;
       _validateCartCompatibility = validateCartCompatibility;
       _uiService = uiService;
   }
   ```

3. **New observable properties** — add below `GrandTotalLabel`:
   ```csharp
   [ObservableProperty]
   private bool _isCartValid;

   public ObservableCollection<ValidationIssueViewModel> ValidationIssues { get; } = [];

   public bool HasValidationIssues => ValidationIssues.Count > 0;
   ```

4. **Update `InitializeAsync`** — call `RunValidationAsync()` after `RefreshGroupsFromDto`:
   ```csharp
   _cartSessionId = result.Payload!.Id;
   RefreshGroupsFromDto(result.Payload!);
   await RunValidationAsync();
   ```

5. **Update `RemoveItemAsync`, `IncrementQuantityAsync`, `DecrementQuantityAsync`** — add
   `await RunValidationAsync();` after each successful `RefreshGroupsFromDto(result.Payload!)` call.

   **RemoveItemAsync:**
   ```csharp
   if (result.IsSuccess)
   {
       ErrorMessage = null;
       RefreshGroupsFromDto(result.Payload!);
       await RunValidationAsync();
   }
   else
       ErrorMessage = result.UserMessage;
   ```

   Apply the same pattern to `IncrementQuantityAsync` and `DecrementQuantityAsync`.

6. **Add `RunValidationAsync`** — private async method:
   ```csharp
   private async Task RunValidationAsync()
   {
       if (_cartSessionId is not { } cartId)
       {
           IsCartValid = false;
           ValidationIssues.Clear();
           OnPropertyChanged(nameof(HasValidationIssues));
           return;
       }

       var result = await _validateCartCompatibility.ExecuteAsync(cartId);

       if (!result.IsSuccess)
       {
           // Validation infrastructure failure — treat cart as invalid but don't
           // surface internal error codes to cashier.
           IsCartValid = false;
           ValidationIssues.Clear();
           OnPropertyChanged(nameof(HasValidationIssues));
           return;
       }

       ValidationIssues.Clear();
       foreach (var issue in result.Payload!.Issues)
       {
           ValidationIssues.Add(new ValidationIssueViewModel
           {
               Message  = issue.Message,
               FixLabel = issue.FixLabel,
               FixAction = issue.FixAction,
           });
       }

       IsCartValid = result.Payload.IsValid;
       OnPropertyChanged(nameof(HasValidationIssues));
   }
   ```

7. **Add `ApplyFixCommand`**:
   ```csharp
   [RelayCommand]
   private async Task ApplyFixAsync(CartValidationFixAction action)
   {
       if (_cartSessionId is not { } cartId) return;

       switch (action)
       {
           case CartValidationFixAction.RemoveCateringItems:
               var cateringIds = ItemGroups
                   .SelectMany(g => g)
                   .Where(i => i.FulfillmentContext == FulfillmentContext.CateringAddon)
                   .Select(i => i.Id)
                   .ToList();
               foreach (var id in cateringIds)
                   await _removeCartLineItem.ExecuteAsync(new RemoveCartLineItemCommand(cartId, id));
               await RefreshAndValidateAsync();
               break;

           case CartValidationFixAction.KeepOldestPartyDeposit:
               var extraDepositIds = ItemGroups
                   .SelectMany(g => g)
                   .Where(i => i.FulfillmentContext == FulfillmentContext.PartyDeposit)
                   .Skip(1)
                   .Select(i => i.Id)
                   .ToList();
               foreach (var id in extraDepositIds)
                   await _removeCartLineItem.ExecuteAsync(new RemoveCartLineItemCommand(cartId, id));
               await RefreshAndValidateAsync();
               break;

           case CartValidationFixAction.None:
           default:
               break;
       }
   }

   private async Task RefreshAndValidateAsync()
   {
       var refreshResult = await _getOrCreateCartSession.ExecuteAsync();
       if (refreshResult.IsSuccess)
           RefreshGroupsFromDto(refreshResult.Payload!);
       await RunValidationAsync();
   }
   ```

8. **Add `ProceedToPaymentCommand` placeholder** (Story 3.3 fills in actual flow):
   ```csharp
   [RelayCommand]
   private async Task ProceedToPaymentAsync()
   {
       // TODO Story 3.3: navigate to payment capture page
       await Task.CompletedTask;
   }
   ```

### `POSOpen/Features/Checkout/Views/CartPage.xaml`

**Target changes:**

1. Add `xmlns:enums="clr-namespace:POSOpen.Domain.Enums;assembly=POSOpen"` to the root
   `ContentPage` namespace declarations.

2. Add a validation issues section to the page footer. The footer `Grid` currently has
   `RowDefinitions="Auto,Auto,Auto"` for Grand Total / Add Item / Proceed. Expand it to
   accommodate the validation panel:

   ```xml
   <!-- Change RowDefinitions from "Auto,Auto,Auto" to "Auto,Auto,Auto,Auto" -->
   <Grid.RowDefinitions>
       <RowDefinition Height="Auto" />  <!-- Row 0: Grand Total -->
       <RowDefinition Height="Auto" />  <!-- Row 1: Validation Issues (new) -->
       <RowDefinition Height="Auto" />  <!-- Row 2: Add Item button -->
       <RowDefinition Height="Auto" />  <!-- Row 3: Proceed to Payment button -->
   </Grid.RowDefinitions>
   ```

   > Shift the existing `Add Item` button and `Proceed to Payment` button down by one row
   > each (from `Grid.Row="1"` / `Grid.Row="2"` to `Grid.Row="2"` / `Grid.Row="3"`).

3. **Insert the validation issues panel at `Grid.Row="1"`:**

   ```xml
   <StackLayout
       Grid.Row="1"
       IsVisible="{Binding HasValidationIssues}"
       Margin="0,8,0,0"
       Spacing="6"
       BindableLayout.ItemsSource="{Binding ValidationIssues}"
       AutomationId="validation-issues-panel">

       <BindableLayout.ItemTemplate>
           <DataTemplate x:DataType="viewModels:ValidationIssueViewModel">
               <Border
                   BackgroundColor="#FFFBEB"
                   StrokeThickness="1"
                   Stroke="#F59E0B"
                   Padding="12,8"
                   StrokeShape="RoundRectangle 6">
                   <Grid ColumnDefinitions="*,Auto" ColumnSpacing="8">
                       <Label
                           Grid.Column="0"
                           Text="{Binding Message}"
                           TextColor="#92400E"
                           FontSize="13"
                           VerticalOptions="Center" />
                       <Button
                           Grid.Column="1"
                           Text="{Binding FixLabel}"
                           IsVisible="{Binding HasFix}"
                           Command="{Binding Source={x:Reference cartPage}, Path=BindingContext.ApplyFixCommand}"
                           CommandParameter="{Binding FixAction}"
                           FontSize="12"
                           BackgroundColor="#F59E0B"
                           TextColor="White"
                           Padding="10,6"
                           CornerRadius="4"
                           AutomationId="apply-fix-button" />
                   </Grid>
               </Border>
           </DataTemplate>
       </BindableLayout.ItemTemplate>
   </StackLayout>
   ```

   > **`CommandParameter` and typed `RelayCommand`:** `[RelayCommand]` on `ApplyFixAsync(CartValidationFixAction action)` generates `RelayCommand<CartValidationFixAction>` via CommunityToolkit source generators. The `CommandParameter="{Binding FixAction}"` supplies the enum value as `object`; the generated command performs a type-safe cast internally. No explicit value converter is needed.

4. **Update Proceed to Payment button** (now `Grid.Row="3"`):
   - Change `IsEnabled="False"` → `IsEnabled="{Binding IsCartValid}"`
   - Add `Command="{Binding ProceedToPaymentCommand}"`

   ```xml
   <Button
       Grid.Row="3"
       Text="Proceed to Payment"
       IsEnabled="{Binding IsCartValid}"
       Command="{Binding ProceedToPaymentCommand}"
       AutomationId="proceed-to-payment-button"
       Style="{StaticResource PrimaryButton}"
       Margin="0,8,0,0" />
   ```

### `POSOpen/Features/Checkout/CheckoutServiceCollectionExtensions.cs`

Add after the existing use case registrations (before ViewModel registrations):

```csharp
// Compatibility rules — each concrete type registers as ICartCompatibilityRule
services.AddTransient<ICartCompatibilityRule, CartMustHaveItemsRule>();
services.AddTransient<ICartCompatibilityRule, CateringRequiresPartyDepositRule>();
services.AddTransient<ICartCompatibilityRule, SinglePartyDepositRule>();
services.AddTransient<ValidateCartCompatibilityUseCase>();
```

Add the corresponding using directives:

```csharp
using POSOpen.Domain.Policies;
```

---

## Pre-Implementation Checks

Before writing any code, verify the following in the existing entity files:

1. **`CartLineItem.cs`** — Confirm the property name for `FulfillmentContext`. The rules use
   `i.Context` in the examples above; adjust to the actual property name (e.g., `i.FulfillmentContext`).

2. **`CartSession.cs`** — Confirm `CartSession.Create(Guid staffId, Guid? familyId)` matches
   the factory signature used in tests. Confirm `AddLineItem` method signature; adjust
   `CartWith` helper in tests accordingly.

3. **`ICartSessionRepository.GetByIdAsync` eager-loads `LineItems`** — The rule `Evaluate` methods call `.Count`, `.Any()`, and `.Count()` on `cart.LineItems`. Confirm the EF query uses `Include(c => c.LineItems)` so all items are materialised in one round-trip. Lazy loading here would cause N+1 queries per rule.

4. **`LineItems` ordering** — The `KeepOldestPartyDeposit` fix calls `Skip(1)` on the `PartyDeposit` group in `ItemGroups`, which reflects the order items appear in `dto.LineItems`. Confirm the repository query appends `.OrderBy(li => li.CreatedAtUtc)` to the `LineItems` include. If it does not, `Skip(1)` may remove the wrong deposit.

5. **`CartLineItemViewModel`** — `FulfillmentContext` property is already present from 3.1.
   Confirm before referencing in `ApplyFixAsync`.

6. **`ApplyFixAsync` partial failures** — Each item removal in the `foreach` loop is an independent `await`. If a mid-loop call fails, earlier removals have already committed. For V1 this is acceptable (the issues panel will reflect the partial result and the cashier can retry), but failures must **not** be swallowed silently. If `_removeCartLineItem.ExecuteAsync` returns a failure result, set `ErrorMessage` and `break` out of the loop.

---

## Test Strategy

| Layer | File | Coverage |
|-------|------|----------|
| Domain | `CartCompatibilityRuleTests.cs` | Each rule: empty/violations/no-violation × all rules |
| Application | `ValidateCartCompatibilityUseCaseTests.cs` | Cart not found, empty cart invalid, single item valid, two-deposit-with-catering (only MultipleDeposits fires) |
| Presentation | `CartViewModelTests.cs` | Add cases: cart initialises → `IsCartValid` false when empty; fix applied → `IsCartValid` true; `ValidationIssues` count matches issues |

**Recommended ViewModel test additions to `CartViewModelTests.cs`:**

```csharp
// Arrange: empty cart → validation makes IsCartValid false
[Fact]
public async Task Initialize_EmptyCart_IsCartValidFalse()

// Arrange: cart with one admission → validation makes IsCartValid true
[Fact]
public async Task Initialize_CartWithAdmission_IsCartValidTrue()

// Arrange: catering without deposit → fix applied → IsCartValid true
[Fact]
public async Task ApplyFix_RemoveCatering_ClearsIssueAndEnablesCheckout()
```

---

## Previous Story Learnings (from 3.1)

- **`using` directive ordering:** Alphabetical system usings first, then project usings.
- **`AppResult` static factories:** `AppResult<T>.Success(payload)`, `AppResult<T>.Failure(code, user, diag?)`.
- **`[ObservableProperty]` backing field convention:** `_camelCase` private field, property
  becomes `PascalCase` automatically.
- **Test helper pattern:** `MockClock()`, `MockAppState()` statics in use-case test files;
  prefer manual construction over Moq for use cases that are thin.
- **`RefreshGroupsFromDto` after every mutation** — preserve this pattern; Story 3.2 extends
  it by also calling `RunValidationAsync()`.
- **`x:Reference cartPage`** — `CartPage` already has `x:Name="cartPage"` set; safe to use
  for command binding breakout in DataTemplates.

---

## Out of Scope for This Story

- Actual payment capture (card/cash) — Story 3.3
- New fulfillment-context-specific compatibility rules beyond the V1 three defined above
- Server-side re-validation at payment submission
- Async rule evaluation (all V1 rules are synchronous)
- Rule ordering/priority (all V1 rules are independent; aggregate all issues)

---

## Definition of Done

- [x] All new domain enum and policy files compile with no warnings
- [x] `ValidateCartCompatibilityUseCase` resolves from DI in a full app startup
- [x] Proceed to Payment button is disabled when cart has issues and enabled when clean
- [x] Fix buttons remove the correct items and re-enable the button
- [x] All three rule unit tests pass
- [x] All use-case tests pass
- [x] No regressions in existing `CartUseCaseTests` or `CartViewModelTests`
- [x] `ValidationIssues` is empty and `IsCartValid` is true for a cart with one Admission item
- [ ] AC-4 verified manually: validation of a full 4-context cart completes in under 2 seconds
- [ ] Story marked `done` in sprint status after PR merge


---

## File List

### New Files

| Path | Purpose |
|------|---------|
| POSOpen/Domain/Enums/ValidationSeverity.cs | Enum — Blocking = 0 |
| POSOpen/Domain/Enums/CartValidationFixAction.cs | Enum — None, RemoveCateringItems, KeepOldestPartyDeposit |
| POSOpen/Domain/Policies/CartValidationIssue.cs | Sealed record — issue value object |
| POSOpen/Domain/Policies/ICartCompatibilityRule.cs | Interface — rule contract |
| POSOpen/Domain/Policies/CartMustHaveItemsRule.cs | Rule — CART_EMPTY |
| POSOpen/Domain/Policies/CateringRequiresPartyDepositRule.cs | Rule — CATERING_WITHOUT_PARTY_DEPOSIT |
| POSOpen/Domain/Policies/SinglePartyDepositRule.cs | Rule — MULTIPLE_PARTY_DEPOSITS |
| POSOpen/Application/UseCases/Checkout/CartValidationDto.cs | DTOs — CartValidationIssueDto, CartValidationResultDto |
| POSOpen/Application/UseCases/Checkout/ValidateCartCompatibilityUseCase.cs | Use case — runs all rules, returns result DTO |
| POSOpen/Features/Checkout/ViewModels/ValidationIssueViewModel.cs | Plain VM — issue for data-binding in XAML |
| POSOpen.Tests/Unit/Checkout/CartCompatibilityRuleTests.cs | 8 tests — one per rule × pass/fail path |
| POSOpen.Tests/Unit/Checkout/ValidateCartCompatibilityUseCaseTests.cs | 4 tests — not-found, empty, valid, multi-deposit |

### Modified Files

| Path | Change |
|------|--------|
| POSOpen/Application/UseCases/Checkout/CartCheckoutConstants.cs | Added 6 constants: ErrorCartEmpty, SafeCartEmptyMessage, ErrorCateringWithoutDeposit, SafeCateringWithoutDepositMessage, ErrorMultipleDeposits, SafeMultipleDepositsMessage |
| POSOpen/Features/Checkout/ViewModels/CartViewModel.cs | Injected ValidateCartCompatibilityUseCase; added IsCartValid, ValidationIssues, HasValidationIssues; added RunValidationAsync, ApplyFixCommand, RefreshAndValidateAsync, ProceedToPaymentCommand; each cart mutation now calls RunValidationAsync() |
| POSOpen/Features/Checkout/Views/CartPage.xaml | Expanded footer to 4 rows; added validation-issues panel (amber border cards with fix buttons); Proceed to Payment now bound to IsCartValid and ProceedToPaymentCommand |
| POSOpen/Features/Checkout/CheckoutServiceCollectionExtensions.cs | Registered 3 rules as ICartCompatibilityRule transients; registered ValidateCartCompatibilityUseCase transient |
| POSOpen.Tests/POSOpen.Tests.csproj | Linked Domain/Policies/*.cs and ValidationIssueViewModel.cs; added CreateViewModelWithRules helper and 3 new ViewModel tests |

---

## Change Log

| Date | Author | Description |
|------|--------|-------------|
| 2026-04-01 | GitHub Copilot | Implemented Story 3.2 — Domain enums/policies, application use case, ViewModel integration, XAML validation panel, DI registrations, 15 new tests (171 total, 0 failures). Fixed test project csproj to link new source files. |

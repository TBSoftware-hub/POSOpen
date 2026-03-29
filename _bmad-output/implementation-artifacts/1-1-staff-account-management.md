# Story 1.1: Staff Account Management

Status: review

## Story

As an Owner/Admin,
I want to create, update, and deactivate staff accounts,
So that only authorized staff can use POSOpen.

## Acceptance Criteria

**Given** I am an authenticated Owner/Admin
**When** I create a new staff account with required profile fields
**Then** the account is persisted with active status and assigned unique staff ID
**And** validation rules for required fields are enforced.

**Given** a staff account exists
**When** I update profile fields
**Then** changes are saved
**And** audit metadata (updatedBy, updatedAtUtc) is recorded.

**Given** an active staff account
**When** I deactivate the account
**Then** the user cannot authenticate
**And** account status is set to inactive.

## Tasks / Subtasks

- [x] Define `StaffAccount` domain entity and supporting enums. (AC: 1, 2, 3)
  - [x] Create `POSOpen/Domain/Entities/StaffAccount.cs` with all fields per Dev Notes.
  - [x] Create `POSOpen/Domain/Enums/StaffAccountStatus.cs` (`Active = 1`, `Inactive = 2`).
  - [x] Create `POSOpen/Domain/Enums/StaffRole.cs` (`Owner = 1`, `Admin = 2`, `Manager = 3`, `Cashier = 4`).
- [x] Define application abstractions for staff account management. (AC: 1, 2, 3)
  - [x] Create `POSOpen/Application/Abstractions/Repositories/IStaffAccountRepository.cs` with `AddAsync`, `GetByIdAsync`, `GetByEmailAsync`, `ListActiveAsync`, `UpdateAsync` methods.
  - [x] Create `POSOpen/Application/Abstractions/Services/IPasswordHasher.cs` with `Hash` and `Verify` methods per Dev Notes.
  - [x] Create command records: `CreateStaffAccountCommand`, `UpdateStaffAccountCommand`, `DeactivateStaffAccountCommand` in `POSOpen/Application/UseCases/StaffManagement/`.
  - [x] Create `POSOpen/Application/UseCases/StaffManagement/StaffAccountDto.cs` for use case output.
- [x] Implement `CreateStaffAccountUseCase`. (AC: 1)
  - [x] Validate all required fields and return `Failure` result on first violation.
  - [x] Reject duplicate email (call `GetByEmailAsync`; return `Failure` with code `STAFF_EMAIL_CONFLICT`).
  - [x] Hash the initial password using `IPasswordHasher.Hash`.
  - [x] Persist via `IStaffAccountRepository.AddAsync` with `Status = Active` and a new `Guid` Id.
  - [x] Emit `StaffAccountCreated` operation log entry via `IOperationLogRepository`.
  - [x] Return `AppResult<StaffAccountDto>.Success(dto, "Staff account created.")`.
- [x] Implement `UpdateStaffAccountUseCase`. (AC: 2)
  - [x] Validate required fields.
  - [x] Reject email change to an address already used by another account.
  - [x] Set `UpdatedAtUtc` from `OperationContext.OccurredUtc`; set `UpdatedByStaffId` from caller context.
  - [x] Emit `StaffAccountUpdated` operation log entry.
  - [x] Return `AppResult<StaffAccountDto>.Success(dto, "Staff account updated.")`.
- [x] Implement `DeactivateStaffAccountUseCase`. (AC: 3)
  - [x] Reject deactivation if account is already inactive (return `Failure` with code `STAFF_ALREADY_INACTIVE`).
  - [x] Set `Status = Inactive` and persist via `UpdateAsync`.
  - [x] Emit `StaffAccountDeactivated` operation log entry.
  - [x] Return `AppResult<bool>.Success(true, "Staff account deactivated.")`.
- [x] Implement `Pbkdf2PasswordHasher` in infrastructure security. (AC: 1)
  - [x] Create `POSOpen/Infrastructure/Security/Pbkdf2PasswordHasher.cs` using `Rfc2898DeriveBytes.Pbkdf2` with SHA-256, 100,000 iterations, 16-byte random salt.
  - [x] Store hash and salt as separate Base64 strings.
  - [x] Implement `Verify` as a constant-time comparison using `CryptographicOperations.FixedTimeEquals`.
- [x] Implement EF Core persistence for `StaffAccount`. (AC: 1, 2, 3)
  - [x] Create `POSOpen/Infrastructure/Persistence/Configurations/StaffAccountConfiguration.cs` with table, column, index mapping per Dev Notes.
  - [x] Add `DbSet<StaffAccount> StaffAccounts` to `PosOpenDbContext`.
  - [x] Create `POSOpen/Infrastructure/Persistence/Repositories/StaffAccountRepository.cs`.
  - [x] Generate EF migration: `dotnet ef migrations add AddStaffAccounts ...` per Dev Notes.
- [x] Register new services and repositories in DI. (AC: 1, 2, 3)
  - [x] Register `IStaffAccountRepository` / `StaffAccountRepository` as `Transient` in `PersistenceServiceCollectionExtensions`.
  - [x] Register `IPasswordHasher` / `Pbkdf2PasswordHasher` as `Singleton` in `PersistenceServiceCollectionExtensions` or a new `SecurityServiceCollectionExtensions`.
  - [x] Register Views and ViewModels via a new `StaffManagementServiceCollectionExtensions` called from `MauiProgram`.
- [x] Build the `StaffListViewModel` and `StaffListPage`. (AC: 1, 2, 3)
  - [x] `StaffListViewModel`: loads active accounts, exposes `ObservableCollection<StaffAccountDto>`, `LoadCommand`, navigate-to-create and navigate-to-edit commands.
  - [x] `StaffListPage.xaml`: staff list with name, role badge, status chip; empty state with call-to-action; primary "Add Staff" button in sticky action zone.
  - [x] Skeleton loading state and error banner following UX feedback patterns.
- [x] Build the `CreateStaffAccountViewModel` and `CreateStaffAccountPage`. (AC: 1)
  - [x] ViewModel state: `Idle → Loading → Success|Error`. On success, navigate back to staff list.
  - [x] Form fields: First Name, Last Name, Email, Password (masked), Role picker.
  - [x] Validate on blur for Email; validate all on submit.
  - [x] Show inline field-level error messages below each field; summary error banner on submission failure.
  - [x] Confirm and dismiss on success with a toast message.
- [x] Build the `EditStaffAccountViewModel` and `EditStaffAccountPage`. (AC: 2, 3)
  - [x] ViewModel state: `Idle → Loading → Success|Error`. Loads account on navigation receive.
  - [x] Update path: same fields as create except password (password change is a separate flow, deferred to later story).
  - [x] Deactivate action: destructive button at bottom, shows confirmation dialog before calling `DeactivateStaffAccountUseCase`.
  - [x] On deactivate success, navigate back to staff list with success toast.
- [x] Register Shell routes for staff management. (AC: 1, 2, 3)
  - [x] Add Shell route entries for `StaffListPage`, `CreateStaffAccountPage`, `EditStaffAccountPage` in `AppShell.xaml` or via `Routing.RegisterRoute` in `MauiProgram`.
  - [x] Navigation should be only accessible to Owner/Admin roles (enforced in 1.2; stub for now with no blocking guard).
- [x] Add tests. (AC: 1, 2, 3)
  - [x] Add `<Compile>` link entries to `POSOpen.Tests.csproj` for all new Domain/Application/Infrastructure source files per Dev Notes.
  - [x] Write unit tests: `CreateStaffAccountUseCaseTests` (valid create, duplicate email, missing fields).
  - [x] Write unit tests: `DeactivateStaffAccountUseCaseTests` (active → inactive, already-inactive guard).
  - [x] Write unit tests: `Pbkdf2PasswordHasherTests` (round-trip hash+verify, tampered hash fails).
  - [x] Write integration tests: `StaffAccountRepositoryTests` (add, getById, getByEmail, listActive, update, deactivate).
  - [x] Verify `StaffAccountCreated` / `StaffAccountUpdated` / `StaffAccountDeactivated` operation log entries appear in integration tests.

## Dev Notes

### Story Intent

This story introduces the first real business entity in POSOpen: `StaffAccount`. It provisions the full schema (including credential and lockout fields needed by stories 1.2–1.3), implements the Owner/Admin CRUD workflow, and establishes the credential hashing approach as an in-box .NET 10 pattern requiring no additional packages. The developer should produce a stable, extensible staff account foundation without speculation beyond what the acceptance criteria require.

Stories 1.2 (role assignment) and 1.3 (terminal authentication) extend this entity directly — do not defer schema fields or repository methods that block those stories.

### Current Repo Reality

- Story 1.0 is fully implemented. The codebase has the following foundation in place:
  - `AppResult<TPayload>` sealed record in `Application/Results/AppResult.cs`
  - `OperationContext` sealed record in `Shared/Operational/OperationContext.cs`
  - `IOperationLogRepository` + `OperationLogRepository` (append-only log)
  - `IOutboxRepository` + `OutboxRepository`
  - `PosOpenDbContext` with `OperationLogEntries` and `OutboxMessages` DbSets
  - `IEncryptionKeyProvider` + `SecureStorageEncryptionKeyProvider`
  - `IOperationContextFactory` + `OperationContextFactory`
  - `IUtcClock` + `SystemUtcClock`
  - `IAppDbContextInitializer` + `AppDbContextInitializer`
  - SQLCipher-encrypted SQLite via `SQLitePCLRaw.bundle_e_sqlcipher`
  - EF migration `InitializePersistenceBaseline` already applied
  - `TestDbContextFactory` + `TestDatabasePaths` + `TestUtcClock` in `POSOpen.Tests`
  - Source-file linking in `POSOpen.Tests.csproj` instead of project reference (critical pattern — do not change)
- There is no existing `StaffAccount` entity, no `Features/StaffManagement` folder, and no password hashing infrastructure.
- `HomeViewModel.cs` is the only existing ViewModel; use it as the reference implementation pattern.

### Critical Guardrails

1. **Layer boundaries**: ViewModels call use cases. Use cases call repositories and services. Repositories call `PosOpenDbContext`. No MAUI/UI code in Application or below. No `DbContext` calls from ViewModels.
2. **Password handling**: NEVER log, serialize to JSON output DTOs, or expose `PasswordHash` or `PasswordSalt` fields. These fields must never appear in `StaffAccountDto`. Strip them at the repository/use-case boundary.
3. **Credential hashing algorithm**: Use `Rfc2898DeriveBytes.Pbkdf2` (available in `System.Security.Cryptography`, no extra NuGet). Minimum 100,000 HMACSHA256 iterations. 16-byte (128-bit) random salt via `RandomNumberGenerator.GetBytes(16)`. 32-byte (256-bit) output key. This requires no package changes.
4. **Constant-time comparison**: Use `CryptographicOperations.FixedTimeEquals` in `Verify` to prevent timing attacks.
5. **One active owner**: Guard is out of scope for this story but do not make it impossible. The `StaffRole` enum must accommodate `Owner = 1` so story 1.2 can add the single-owner constraint.
6. **No duplicate email**: Enforce at the use-case level (not only at the DB level), return a structured `Failure` with code `STAFF_EMAIL_CONFLICT` so the ViewModel can show a field-specific error.
7. **UTC everywhere**: `CreatedAtUtc` and `UpdatedAtUtc` must use `IUtcClock.UtcNow`, not `DateTime.Now` or `DateTime.UtcNow` directly.
8. **Audit fields on every mutation**: Every create/update/deactivate operation must emit an `OperationLogEntry` using the operation context from `IOperationContextFactory`. Payload must be JSON-serialized with `AppJsonSerializerOptions`.
9. **Schema completeness**: Include `FailedLoginAttempts` (int, default 0) and `LockedUntilUtc` (DateTime?, nullable) on the entity NOW — they are required by story 1.3 and adding them later breaks the migration chain. Do not add any behavior around them in this story; just provision the columns.
10. **Test source linking**: All non-MAUI infrastructure, domain, and application source files used in tests MUST be linked in `POSOpen.Tests.csproj` as `<Compile Include="../POSOpen/..." Link="Source/..."/>`. DO NOT add a project reference to `POSOpen.csproj` (the MAUI asset pipeline breaks tests).
11. **Encrypted SQLite in tests**: Use `TestDbContextFactory` for integration tests. It already handles SQLCipher provider initialization and per-test database isolation.

### Domain Entity — `StaffAccount`

File: `POSOpen/Domain/Entities/StaffAccount.cs`

```csharp
namespace POSOpen.Domain.Entities;

public sealed class StaffAccount
{
    public Guid Id { get; init; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }        // Unique index; lowercase for comparison
    public required string PasswordHash { get; set; } // Base64-encoded PBKDF2 output
    public required string PasswordSalt { get; set; } // Base64-encoded random salt
    public StaffRole Role { get; set; }
    public StaffAccountStatus Status { get; set; }
    public int FailedLoginAttempts { get; set; }      // Provisioned for story 1.3
    public DateTime? LockedUntilUtc { get; set; }     // Provisioned for story 1.3
    public DateTime CreatedAtUtc { get; init; }
    public DateTime UpdatedAtUtc { get; set; }
    public Guid? CreatedByStaffId { get; init; }      // Null for first-owner bootstrap
    public Guid? UpdatedByStaffId { get; set; }
}
```

- Add a private parameterless constructor for EF Core materialization:
  `private StaffAccount() { }` (EF requires it for required init-only properties).
- Or use EF owned-type constructor injection — the private parameterless ctor approach is simpler.

### Domain Enums

File: `POSOpen/Domain/Enums/StaffAccountStatus.cs`

```csharp
namespace POSOpen.Domain.Enums;

public enum StaffAccountStatus { Active = 1, Inactive = 2 }
```

File: `POSOpen/Domain/Enums/StaffRole.cs`

```csharp
namespace POSOpen.Domain.Enums;

public enum StaffRole { Owner = 1, Admin = 2, Manager = 3, Cashier = 4 }
```

Store as `int` in the database (EF default for enums). Do **not** use string store for enums; keeping them as integers avoids migration pain on rename.

### Repository Interface — `IStaffAccountRepository`

File: `POSOpen/Application/Abstractions/Repositories/IStaffAccountRepository.cs`

```csharp
namespace POSOpen.Application.Abstractions.Repositories;

public interface IStaffAccountRepository
{
    Task AddAsync(StaffAccount account, CancellationToken ct = default);
    Task<StaffAccount?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<StaffAccount?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<IReadOnlyList<StaffAccount>> ListActiveAsync(CancellationToken ct = default);
    Task UpdateAsync(StaffAccount account, CancellationToken ct = default);
}
```

`GetByEmailAsync` must normalize the email to lowercase before querying (or the EF configuration must apply `ToLower()` collation). Normalize at repository level so use cases stay clean.

### Password Hasher Interface — `IPasswordHasher`

File: `POSOpen/Application/Abstractions/Services/IPasswordHasher.cs`

```csharp
namespace POSOpen.Application.Abstractions.Services;

public interface IPasswordHasher
{
    /// <summary>Hashes a plaintext password. Returns separate hash and salt as Base64 strings.</summary>
    (string Hash, string Salt) Hash(string plaintext);

    /// <summary>Verifies a plaintext password against stored hash and salt. Constant-time safe.</summary>
    bool Verify(string plaintext, string storedHash, string storedSalt);
}
```

### Password Hasher Implementation — `Pbkdf2PasswordHasher`

File: `POSOpen/Infrastructure/Security/Pbkdf2PasswordHasher.cs`

Key implementation notes:
- `Salt`: `RandomNumberGenerator.GetBytes(16)` → `Convert.ToBase64String(saltBytes)`
- `Hash`: `Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(plaintext), saltBytes, 100_000, HashAlgorithmName.SHA256, 32)` → `Convert.ToBase64String(hashBytes)`
- `Verify`: decode stored salt and hash from Base64, recompute, compare with `CryptographicOperations.FixedTimeEquals(computedBytes, storedBytes)`.
- Required `using` statements: `System.Security.Cryptography`, `System.Text`, `System.Runtime.CompilerServices` (for `Unsafe`; not actually needed — `CryptographicOperations` is in `System.Security.Cryptography`).
- No argument for making this `async`; keep it synchronous — it is CPU-bound and the caller controls threading.

### Use Case Commands and DTO

All in `POSOpen/Application/UseCases/StaffManagement/`:

**`CreateStaffAccountCommand.cs`**:
```csharp
public sealed record CreateStaffAccountCommand(
    string FirstName, string LastName, string Email,
    string PlaintextPassword, StaffRole Role,
    OperationContext Context, Guid? CreatedByStaffId);
```

**`UpdateStaffAccountCommand.cs`**:
```csharp
public sealed record UpdateStaffAccountCommand(
    Guid StaffAccountId,
    string FirstName, string LastName, string Email, StaffRole Role,
    OperationContext Context, Guid UpdatedByStaffId);
```

**`DeactivateStaffAccountCommand.cs`**:
```csharp
public sealed record DeactivateStaffAccountCommand(
    Guid StaffAccountId,
    OperationContext Context, Guid UpdatedByStaffId);
```

**`StaffAccountDto.cs`**:
```csharp
public sealed record StaffAccountDto(
    Guid Id, string FirstName, string LastName, string Email,
    StaffRole Role, StaffAccountStatus Status,
    DateTime CreatedAtUtc, DateTime UpdatedAtUtc,
    Guid? CreatedByStaffId, Guid? UpdatedByStaffId);
```

**Critical**: `StaffAccountDto` must NOT include `PasswordHash`, `PasswordSalt`, `FailedLoginAttempts`, or `LockedUntilUtc`. Map explicitly, not with AutoMapper or record spreading.

### Use Case — `CreateStaffAccountUseCase`

File: `POSOpen/Application/UseCases/StaffManagement/CreateStaffAccountUseCase.cs`

Constructor dependencies: `IStaffAccountRepository`, `IOperationLogRepository`, `IPasswordHasher`, `ILogger<CreateStaffAccountUseCase>`.

Flow:
1. Validate `FirstName`, `LastName`, `Email` are non-empty; `PlaintextPassword` meets minimum length (8 chars minimum; store the rule as a domain constant).
2. Check `GetByEmailAsync(command.Email.ToLowerInvariant())` — if found, return `Failure("STAFF_EMAIL_CONFLICT", "An account with this email address already exists.", null)`.
3. `var (hash, salt) = _passwordHasher.Hash(command.PlaintextPassword);`
4. Build `StaffAccount { Id = Guid.NewGuid(), ..., PasswordHash = hash, PasswordSalt = salt, Status = Active, CreatedAtUtc = command.Context.OccurredUtc, UpdatedAtUtc = command.Context.OccurredUtc, ... }`.
5. `await _repo.AddAsync(account, ct)`.
6. Emit `OperationLogEntry` with `eventType = "StaffAccountCreated"`, `aggregateId = account.Id.ToString()`, `operationId = command.Context.OperationId`, `occurredUtc = command.Context.OccurredUtc`, `version = 1`, `payload = JsonSerializer.Serialize(new { account.Id, account.Email, account.Role }, AppJsonSerializerOptions.Default)`.
7. Return `Success(MapToDto(account), "Staff account created.")`.

### Use Case — `UpdateStaffAccountUseCase`

File: `POSOpen/Application/UseCases/StaffManagement/UpdateStaffAccountUseCase.cs`

Constructor dependencies: `IStaffAccountRepository`, `IOperationLogRepository`, `ILogger<UpdateStaffAccountUseCase>`.

Flow:
1. `var account = await _repo.GetByIdAsync(command.StaffAccountId, ct) ?? return Failure("STAFF_NOT_FOUND", "Staff account not found.", null)`.
2. If `command.Email` ≠ `account.Email`, check `GetByEmailAsync(command.Email.ToLowerInvariant())` — if found by a different Id, return `Failure("STAFF_EMAIL_CONFLICT", ...)`.
3. Update `account.FirstName`, `account.LastName`, `account.Email`, `account.Role`, `account.UpdatedAtUtc`, `account.UpdatedByStaffId`.
4. `await _repo.UpdateAsync(account, ct)`.
5. Emit `StaffAccountUpdated` operation log entry. Payload: `{ account.Id, UpdatedFields: ["FirstName", ...] }` (list changed fields explicitly to aid audit).
6. Return `Success(MapToDto(account), "Staff account updated.")`.

### Use Case — `DeactivateStaffAccountUseCase`

File: `POSOpen/Application/UseCases/StaffManagement/DeactivateStaffAccountUseCase.cs`

Constructor dependencies: `IStaffAccountRepository`, `IOperationLogRepository`, `ILogger<DeactivateStaffAccountUseCase>`.

Flow:
1. `var account = await _repo.GetByIdAsync(command.StaffAccountId, ct) ?? return Failure("STAFF_NOT_FOUND", ...)`.
2. If `account.Status == Inactive`, return `Failure("STAFF_ALREADY_INACTIVE", "This account is already inactive.", null)`.
3. `account.Status = Inactive; account.UpdatedAtUtc = command.Context.OccurredUtc; account.UpdatedByStaffId = command.UpdatedByStaffId;`.
4. `await _repo.UpdateAsync(account, ct)`.
5. Emit `StaffAccountDeactivated` log entry. Payload: `{ account.Id, account.Email, DeactivatedByStaffId: command.UpdatedByStaffId }`.
6. Return `Success(true, "Staff account deactivated.")`.

Note: Story 1.3 will query `Status == Active` when validating credentials — that check is in the authentication use case, not here. This story simply sets the flag correctly.

### EF Configuration — `StaffAccountConfiguration`

File: `POSOpen/Infrastructure/Persistence/Configurations/StaffAccountConfiguration.cs`

```csharp
// Table: staff_accounts
// PK: id (TEXT/GUID)
// Columns: id, first_name, last_name, email, password_hash, password_salt,
//          role, status, failed_login_attempts, locked_until_utc,
//          created_at_utc, updated_at_utc, created_by_staff_id, updated_by_staff_id
// Unique index: ix_staff_accounts_email
// Apply UTC value converter to: created_at_utc, updated_at_utc, locked_until_utc
```

Use `HasConversion<UtcDateTimeConverter>()` on all `DateTime`/`DateTime?` columns (already defined from story 1.0 at `Infrastructure/Persistence/ValueConverters/UtcDateTimeConverter.cs`).

Column constraints:
- `FirstName`, `LastName`: `HasMaxLength(100)`, `IsRequired()`
- `Email`: `HasMaxLength(254)`, `IsRequired()`, then `HasIndex(x => x.Email).IsUnique().HasDatabaseName("ix_staff_accounts_email")`
- `PasswordHash`: `HasMaxLength(512)`, `IsRequired()`
- `PasswordSalt`: `HasMaxLength(128)`, `IsRequired()`
- `Role`, `Status`: stored as `int`, use `HasConversion<int>()` explicitly for clarity
- `FailedLoginAttempts`: `HasDefaultValue(0)`, `IsRequired()`
- `LockedUntilUtc`: nullable, apply UTC converter

### EF Migration

Generate the migration from the solution root:

```shell
dotnet ef migrations add AddStaffAccounts `
  --project .\POSOpen\POSOpen.csproj `
  --startup-project .\POSOpen\POSOpen.csproj `
  --framework net10.0-windows10.0.19041.0 `
  --context POSOpen.Infrastructure.Persistence.PosOpenDbContext `
  --output-dir Infrastructure/Persistence/Migrations
```

Review the generated migration: verify `staff_accounts` table name, all column names are snake_case, and the unique index on `email` is present. Do not hand-edit the migration snapshot.

### `PosOpenDbContext` Changes

Add to `PosOpenDbContext.cs`:
```csharp
public DbSet<StaffAccount> StaffAccounts => Set<StaffAccount>();
```

`ApplyConfigurationsFromAssembly` already handles picking up `StaffAccountConfiguration` automatically — no manual `modelBuilder.Entity<StaffAccount>()` call needed.

### DI Registration

In `PersistenceServiceCollectionExtensions.cs`, add:
```csharp
services.AddTransient<IStaffAccountRepository, StaffAccountRepository>();
```

For the password hasher, register either in persistence extensions or in a new `SecurityServiceCollectionExtensions.cs`:
```csharp
services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
```

`Pbkdf2PasswordHasher` is stateless, so `Singleton` is correct.

In `MauiProgram.cs`, add a call to `StaffManagementServiceCollectionExtensions.AddStaffManagement(services)` after persistence registration. That extension registers ViewModels and Views (pattern matches existing feature registration).

### ViewModel Implementation Pattern

Follow `HomeViewModel.cs` exactly for state machine and property conventions:

```csharp
[ObservableProperty]
private ViewModelState _pageState = ViewModelState.Idle;
```

Use `[RelayCommand]` source generation. Guard commands with `CanExecute` returning `PageState == Idle`.

For the create/edit forms, declare each field as `[ObservableProperty]` and add validation error properties:
```csharp
[ObservableProperty] private string _firstName = string.Empty;
[ObservableProperty] private string _firstNameError = string.Empty;
```

On submit, call `ValidateAll()` locally (ViewModel-level field presence / length), then call the use case. Map `AppResult.ErrorCode` values to ViewModel-level error strings (e.g., `STAFF_EMAIL_CONFLICT` → `EmailError = result.UserMessage`).

### XAML and UX Notes

- **Color tokens** (from UX specification): Primary Teal `#0F766E`, Action Orange `#F97316`, Success `#16A34A`, Warning Amber `#D97706`, Error Red `#DC2626`. These should reference existing `ResourceDictionary` styles from story 1.0 if they were defined; otherwise define them in `Resources/Styles/`.
- **Staff list**: Show each account as a card row with avatar initials (first letter of first + last name), full name, role badge (color-coded), and status chip. Empty state shows "No staff accounts yet — add your first team member" with an "Add Staff Member" button.
- **Form layout**: Single-column form, tablet-optimized width (max 600dp centered). Required field asterisks. Error messages below each field in Error Red. Primary submit button in teal at bottom in sticky action zone.
- **Deactivate action**: Destructive button (`Error Red` border/text, not filled). Confirm via dialog: "Deactivate [Name]'s account? They will no longer be able to sign in." with Cancel (dismiss) and Deactivate (destructive confirm) buttons.
- **Role picker**: `Picker` or segmented control showing Owner, Admin, Manager, Cashier labels.
- **Status communication**: Never communicate status through color alone — pair color with text label and icon (e.g., green dot + "Active", grey dot + "Inactive").
- **Accessibility**: All interactive elements must have `AutomationId` and `SemanticProperties.Description`. Touch targets minimum 44×44px. Tab order must be logical (top-to-bottom form order).

### Testing Requirements

#### New `<Compile>` Link Entries in `POSOpen.Tests.csproj`

All of the following must be added as linked source files. Follow the exact same `Include`/`Link` pattern already in the `.csproj`:

```xml
<Compile Include="../POSOpen/Domain/Entities/StaffAccount.cs" Link="Source/Domain/Entities/StaffAccount.cs" />
<Compile Include="../POSOpen/Domain/Enums/StaffAccountStatus.cs" Link="Source/Domain/Enums/StaffAccountStatus.cs" />
<Compile Include="../POSOpen/Domain/Enums/StaffRole.cs" Link="Source/Domain/Enums/StaffRole.cs" />
<Compile Include="../POSOpen/Application/Abstractions/Repositories/IStaffAccountRepository.cs" Link="Source/Application/Abstractions/Repositories/IStaffAccountRepository.cs" />
<Compile Include="../POSOpen/Application/Abstractions/Services/IPasswordHasher.cs" Link="Source/Application/Abstractions/Services/IPasswordHasher.cs" />
<Compile Include="../POSOpen/Application/UseCases/StaffManagement/CreateStaffAccountCommand.cs" Link="Source/Application/UseCases/StaffManagement/CreateStaffAccountCommand.cs" />
<Compile Include="../POSOpen/Application/UseCases/StaffManagement/UpdateStaffAccountCommand.cs" Link="Source/Application/UseCases/StaffManagement/UpdateStaffAccountCommand.cs" />
<Compile Include="../POSOpen/Application/UseCases/StaffManagement/DeactivateStaffAccountCommand.cs" Link="Source/Application/UseCases/StaffManagement/DeactivateStaffAccountCommand.cs" />
<Compile Include="../POSOpen/Application/UseCases/StaffManagement/StaffAccountDto.cs" Link="Source/Application/UseCases/StaffManagement/StaffAccountDto.cs" />
<Compile Include="../POSOpen/Application/UseCases/StaffManagement/CreateStaffAccountUseCase.cs" Link="Source/Application/UseCases/StaffManagement/CreateStaffAccountUseCase.cs" />
<Compile Include="../POSOpen/Application/UseCases/StaffManagement/UpdateStaffAccountUseCase.cs" Link="Source/Application/UseCases/StaffManagement/UpdateStaffAccountUseCase.cs" />
<Compile Include="../POSOpen/Application/UseCases/StaffManagement/DeactivateStaffAccountUseCase.cs" Link="Source/Application/UseCases/StaffManagement/DeactivateStaffAccountUseCase.cs" />
<Compile Include="../POSOpen/Infrastructure/Persistence/Repositories/StaffAccountRepository.cs" Link="Source/Infrastructure/Persistence/Repositories/StaffAccountRepository.cs" />
<Compile Include="../POSOpen/Infrastructure/Security/Pbkdf2PasswordHasher.cs" Link="Source/Infrastructure/Security/Pbkdf2PasswordHasher.cs" />
```

#### Unit Tests — `CreateStaffAccountUseCaseTests`

File: `POSOpen.Tests/Unit/StaffManagement/CreateStaffAccountUseCaseTests.cs`

Scenarios:
- **Valid create succeeds**: given valid command with unique email, result is success, `StaffAccountDto` payload returned, `PasswordHash`/`PasswordSalt` not in DTO.
- **Duplicate email fails**: given email already in repository, result is failure with `ErrorCode == "STAFF_EMAIL_CONFLICT"`.
- **Missing first name fails**: given blank `FirstName`, result is failure with validation error.
- **Short password fails**: given `PlaintextPassword.Length < 8`, result is failure.
- **Operation log emitted**: given valid create, `IOperationLogRepository.AddAsync` is called once with `eventType == "StaffAccountCreated"`.

Use `NSubstitute` or hand-rolled test doubles for repository and log. The `TestPasswordHasher` test double should accept any password and return a deterministic `(Hash, Salt)` tuple.

#### Unit Tests — `DeactivateStaffAccountUseCaseTests`

File: `POSOpen.Tests/Unit/StaffManagement/DeactivateStaffAccountUseCaseTests.cs`

Scenarios:
- **Active account deactivates**: result is success, `Status == Inactive` set on persisted entity, `UpdatedAtUtc` updated.
- **Already-inactive guard**: result is failure with `ErrorCode == "STAFF_ALREADY_INACTIVE"`.
- **Not found guard**: result is failure with `ErrorCode == "STAFF_NOT_FOUND"`.
- **Operation log emitted**: `IOperationLogRepository.AddAsync` called once with `eventType == "StaffAccountDeactivated"`.

#### Unit Tests — `Pbkdf2PasswordHasherTests`

File: `POSOpen.Tests/Unit/Security/Pbkdf2PasswordHasherTests.cs`

Scenarios:
- **Round-trip**: `Hash("secret")` produces non-empty hash and salt; `Verify("secret", hash, salt)` returns `true`.
- **Wrong password fails**: `Verify("wrong", hash, salt)` returns `false`.
- **Tampered hash fails**: modifying one byte of the hash causes `Verify` to return `false`.
- **Two hashes differ**: `Hash("same")` called twice produces different salts (and thus different hashes).

#### Integration Tests — `StaffAccountRepositoryTests`

File: `POSOpen.Tests/Integration/StaffManagement/StaffAccountRepositoryTests.cs`

Use `TestDbContextFactory` exactly as used in `PersistenceRepositoryTests.cs`. Scenarios:
- **Add and retrieve by Id**: add a `StaffAccount`, retrieve by GUID, assert all non-credential fields match.
- **GetByEmail**: add with email "alice@example.com", retrieve by email (exact case match after normalization), assert found.
- **ListActive**: add one Active and one Inactive account, `ListActiveAsync` returns only the Active one.
- **Update**: add account, mutate `FirstName`, call `UpdateAsync`, retrieve again, assert new `FirstName` persisted.
- **Deactivate via Update**: set `Status = Inactive`, call `UpdateAsync`, verify `ListActiveAsync` no longer includes it.
- **Email uniqueness**: add account with email X, attempt to add a second with same email, expect DB exception (unique constraint violation).
- **UTC persistence**: `CreatedAtUtc` and `UpdatedAtUtc` retrieved are UTC (`DateTimeKind.Utc`).
- **OperationLog emitted during create**: after `CreateStaffAccountUseCase` runs, `IOperationLogRepository` has an entry with `eventType == "StaffAccountCreated"` and `aggregateId == account.Id.ToString()`.

### File Structure Requirements

```
POSOpen/
  Domain/
    Entities/
      StaffAccount.cs                             [NEW]
    Enums/
      StaffAccountStatus.cs                       [NEW]
      StaffRole.cs                                [NEW]
  Application/
    Abstractions/
      Repositories/
        IStaffAccountRepository.cs                [NEW]
      Services/
        IPasswordHasher.cs                        [NEW]
    UseCases/
      StaffManagement/
        CreateStaffAccountCommand.cs              [NEW]
        CreateStaffAccountUseCase.cs              [NEW]
        DeactivateStaffAccountCommand.cs          [NEW]
        DeactivateStaffAccountUseCase.cs          [NEW]
        StaffAccountDto.cs                        [NEW]
        UpdateStaffAccountCommand.cs              [NEW]
        UpdateStaffAccountUseCase.cs              [NEW]
  Features/
    StaffManagement/
      Dtos/
        (none — use Application-layer StaffAccountDto directly)
      ViewModels/
        CreateStaffAccountViewModel.cs            [NEW]
        EditStaffAccountViewModel.cs              [NEW]
        StaffListViewModel.cs                     [NEW]
      Views/
        CreateStaffAccountPage.xaml               [NEW]
        CreateStaffAccountPage.xaml.cs            [NEW]
        EditStaffAccountPage.xaml                 [NEW]
        EditStaffAccountPage.xaml.cs              [NEW]
        StaffListPage.xaml                        [NEW]
        StaffListPage.xaml.cs                     [NEW]
      StaffManagementServiceCollectionExtensions.cs [NEW]
  Infrastructure/
    Persistence/
      Configurations/
        StaffAccountConfiguration.cs              [NEW]
      Migrations/
        {timestamp}_AddStaffAccounts.cs           [NEW — generated]
        {timestamp}_AddStaffAccounts.Designer.cs  [NEW — generated]
        PosOpenDbContextModelSnapshot.cs          [MODIFIED — regenerated]
      Repositories/
        StaffAccountRepository.cs                 [NEW]
      PosOpenDbContext.cs                         [MODIFIED — add DbSet]
      PersistenceServiceCollectionExtensions.cs   [MODIFIED — register repo + hasher]
    Security/
      Pbkdf2PasswordHasher.cs                     [NEW]
  AppShell.xaml                                   [MODIFIED — add routes or register via code]
  MauiProgram.cs                                  [MODIFIED — add StaffManagement feature]

POSOpen.Tests/
  POSOpen.Tests.csproj                            [MODIFIED — add <Compile> links]
  Integration/
    StaffManagement/
      StaffAccountRepositoryTests.cs              [NEW]
  Unit/
    Security/
      Pbkdf2PasswordHasherTests.cs                [NEW]
    StaffManagement/
      CreateStaffAccountUseCaseTests.cs           [NEW]
      DeactivateStaffAccountUseCaseTests.cs       [NEW]
```

### Library / Framework Requirements

- No new NuGet packages needed for hashing (`System.Security.Cryptography.Rfc2898DeriveBytes` is in-box .NET 10).
- No new NuGet packages for test doubles — use hand-rolled doubles or `NSubstitute` if already present.
  - Check `POSOpen.Tests.csproj` for existing test double packages before adding new ones.
- All existing packages from story 1.0 remain unchanged.

### Architecture Compliance

- **Presentation pattern**: `ObservableObject` base, `[ObservableProperty]` + `[RelayCommand]` source generation, explicit `ViewModelState` machine.
- **Navigation**: Shell route registration; route strings as constants in feature extension.
- **Internal flow**: ViewModel → UseCase → Repository/Service → DbContext.
- **Result pattern**: `AppResult<TPayload>` throughout. Use cases return `AppResult<StaffAccountDto>` or `AppResult<bool>`.
- **Event pattern**: `eventType` past-tense PascalCase (`StaffAccountCreated`, `StaffAccountUpdated`, `StaffAccountDeactivated`). Payload in `AppJsonSerializerOptions.Default` JSON.
- **Naming**: interfaces prefix `I`; use case suffix `UseCase`; viewmodel suffix `ViewModel`; repository suffix `Repository`.
- **HTTP/network**: none in this story (all local SQLite).
- **Security**: credentials hashed before persistence; never logged; never exposed via DTO.

### Definition of Done Notes for Dev

- All tasks are checked off.
- `StaffAccount` entity is persisted through migrations; EF migration snapshot is updated.
- `StaffAccountCreated`, `StaffAccountUpdated`, `StaffAccountDeactivated` log entries appear in integration tests.
- `Pbkdf2PasswordHasher` round-trip verified; tampered-hash scenario returns `false`.
- ViewModel state machine: all three ViewModels transition through `Idle → Loading → Success|Error` correctly.
- Deactivate confirmation dialog is behind a destructive UX pattern with explicit confirmation.
- `PasswordHash` and `PasswordSalt` are absent from `StaffAccountDto` and from any JSON log payload.
- All tests pass: `dotnet test .\POSOpen.Tests\POSOpen.Tests.csproj --framework net10.0-windows10.0.19041.0`.
- Build passes: `dotnet build .\POSOpen\POSOpen.csproj --framework net10.0-windows10.0.19041.0`.
- No new analyzer suppressions added without documented justification.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story-1.1-Staff-Account-Management]
- [Source: _bmad-output/planning-artifacts/architecture.md#Security-Strategy]
- [Source: _bmad-output/planning-artifacts/architecture.md#Implementation-Patterns--Consistency-Rules]
- [Source: _bmad-output/planning-artifacts/architecture.md#Project-Structure--Boundaries]
- [Source: _bmad-output/planning-artifacts/architecture.md#Data-Architecture]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Visual-Design-Foundation]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#UX-Consistency-Patterns]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Responsive-Design--Accessibility]
- [Source: _bmad-output/implementation-artifacts/1-0-initialize-maui-starter-and-local-persistence-baseline.md#Dev-Agent-Record]
- [Source: POSOpen/Application/Results/AppResult.cs]
- [Source: POSOpen/Shared/Operational/OperationContext.cs]
- [Source: POSOpen/Infrastructure/Persistence/PosOpenDbContext.cs]
- [Source: POSOpen/Infrastructure/Persistence/PersistenceServiceCollectionExtensions.cs]
- [Source: POSOpen/Infrastructure/Persistence/ValueConverters/UtcDateTimeConverter.cs]
- [Source: POSOpen/Infrastructure/Security/SecureStorageEncryptionKeyProvider.cs]
- [Source: POSOpen/Features/Shell/ViewModels/HomeViewModel.cs]
- [Source: POSOpen.Tests/TestDoubles/TestDbContextFactory.cs]

## Dev Agent Record

### Agent Model Used

GPT-5.3-Codex

### Debug Log References

- `dotnet test .\POSOpen.Tests\POSOpen.Tests.csproj --framework net10.0-windows10.0.19041.0`
- `dotnet build .\POSOpen\POSOpen.csproj --framework net10.0-windows10.0.19041.0`
- `dotnet ef migrations add AddStaffAccounts --project .\POSOpen\POSOpen.csproj --startup-project .\POSOpen\POSOpen.csproj --framework net10.0-windows10.0.19041.0 --context POSOpen.Infrastructure.Persistence.PosOpenDbContext --output-dir Infrastructure/Persistence/Migrations`

### Completion Notes List

- Implemented staff domain entity, enums, repository abstraction, password hasher abstraction, and staff management command/DTO contracts.
- Implemented `CreateStaffAccountUseCase`, `UpdateStaffAccountUseCase`, and `DeactivateStaffAccountUseCase` with validation, duplicate-email checks, status transitions, and operation log emission.
- Implemented `Pbkdf2PasswordHasher` with SHA-256 PBKDF2 (100,000 iterations, 16-byte salt, 32-byte hash) and constant-time verify.
- Added EF Core persistence (`StaffAccountConfiguration`, `StaffAccountRepository`, `DbSet<StaffAccount>`) and generated migration `20260329175600_AddStaffAccounts`.
- Added staff feature UI stack (`StaffList`, `CreateStaffAccount`, `EditStaffAccount`) with MVVM state transitions, validation, error banners, and shell navigation.
- Added DI and route wiring through `StaffManagementServiceCollectionExtensions`, `MauiProgram`, and `AppShell`.
- Added story-required unit/integration tests for create/deactivate use cases, hasher, and repository coverage including operation log verification for create/update/deactivate events.
- Full validation completed: tests passing (19/19), app build passing.

### File List

- POSOpen/Application/Abstractions/Repositories/IStaffAccountRepository.cs
- POSOpen/Application/Abstractions/Services/IPasswordHasher.cs
- POSOpen/Application/UseCases/StaffManagement/CreateStaffAccountCommand.cs
- POSOpen/Application/UseCases/StaffManagement/CreateStaffAccountUseCase.cs
- POSOpen/Application/UseCases/StaffManagement/DeactivateStaffAccountCommand.cs
- POSOpen/Application/UseCases/StaffManagement/DeactivateStaffAccountUseCase.cs
- POSOpen/Application/UseCases/StaffManagement/GetStaffAccountByIdUseCase.cs
- POSOpen/Application/UseCases/StaffManagement/ListActiveStaffAccountsUseCase.cs
- POSOpen/Application/UseCases/StaffManagement/StaffAccountDto.cs
- POSOpen/Application/UseCases/StaffManagement/UpdateStaffAccountCommand.cs
- POSOpen/Application/UseCases/StaffManagement/UpdateStaffAccountUseCase.cs
- POSOpen/Domain/Entities/StaffAccount.cs
- POSOpen/Domain/Enums/StaffAccountStatus.cs
- POSOpen/Domain/Enums/StaffRole.cs
- POSOpen/Features/StaffManagement/StaffManagementRoutes.cs
- POSOpen/Features/StaffManagement/StaffManagementServiceCollectionExtensions.cs
- POSOpen/Features/StaffManagement/ViewModels/CreateStaffAccountViewModel.cs
- POSOpen/Features/StaffManagement/ViewModels/EditStaffAccountViewModel.cs
- POSOpen/Features/StaffManagement/ViewModels/StaffListViewModel.cs
- POSOpen/Features/StaffManagement/ViewModels/ViewModelState.cs
- POSOpen/Features/StaffManagement/Views/CreateStaffAccountPage.xaml
- POSOpen/Features/StaffManagement/Views/CreateStaffAccountPage.xaml.cs
- POSOpen/Features/StaffManagement/Views/EditStaffAccountPage.xaml
- POSOpen/Features/StaffManagement/Views/EditStaffAccountPage.xaml.cs
- POSOpen/Features/StaffManagement/Views/StaffListPage.xaml
- POSOpen/Features/StaffManagement/Views/StaffListPage.xaml.cs
- POSOpen/Infrastructure/Persistence/Configurations/StaffAccountConfiguration.cs
- POSOpen/Infrastructure/Persistence/Migrations/20260329175600_AddStaffAccounts.cs
- POSOpen/Infrastructure/Persistence/Migrations/20260329175600_AddStaffAccounts.Designer.cs
- POSOpen/Infrastructure/Persistence/Migrations/PosOpenDbContextModelSnapshot.cs
- POSOpen/Infrastructure/Persistence/PosOpenDbContext.cs
- POSOpen/Infrastructure/Persistence/PersistenceServiceCollectionExtensions.cs
- POSOpen/Infrastructure/Persistence/Repositories/StaffAccountRepository.cs
- POSOpen/Infrastructure/Security/Pbkdf2PasswordHasher.cs
- POSOpen/Shared/Converters/InverseBoolConverter.cs
- POSOpen/App.xaml
- POSOpen/AppShell.xaml
- POSOpen/MauiProgram.cs
- POSOpen.Tests/Integration/StaffManagement/StaffAccountRepositoryTests.cs
- POSOpen.Tests/POSOpen.Tests.csproj
- POSOpen.Tests/Unit/Security/Pbkdf2PasswordHasherTests.cs
- POSOpen.Tests/Unit/StaffManagement/CreateStaffAccountUseCaseTests.cs
- POSOpen.Tests/Unit/StaffManagement/DeactivateStaffAccountUseCaseTests.cs

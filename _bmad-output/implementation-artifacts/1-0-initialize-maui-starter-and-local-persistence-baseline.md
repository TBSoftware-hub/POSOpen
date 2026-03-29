# Story 1.0: Initialize MAUI Starter and Local Persistence Baseline

Status: done

## Story

As a developer,
I need to scaffold the .NET 10 MAUI solution with CommunityToolkit.Mvvm and SQLite-backed repository abstractions,
so that all subsequent stories build on a stable, provider-agnostic foundation.

## Acceptance Criteria

1. Given the project baseline is being initialized, when the solution is scaffolded, then it uses .NET 10 MAUI app structure with CommunityToolkit.Mvvm-integrated presentation patterns and layer boundaries are established as Presentation -> Application -> Infrastructure.
2. Given V1 persistence is required, when data access foundation is implemented, then SQLite is configured as the primary local store and repository interfaces remain provider-agnostic for later SQL Server introduction.
3. Given operational reliability constraints are required, when baseline infrastructure is completed, then operation IDs, UTC timestamp conventions, and outbox or operation-log scaffolding are present and baseline build, test, and analyzer checks pass in CI.
4. Given the application handles sensitive data, when the SQLite database is created and written to, then the database file is encrypted at rest using SQLCipher or an equivalent encrypted SQLite strategy.
5. Given the application makes any network call, when the connection is established, then all data in transit is protected with current industry-standard TLS.

## Tasks / Subtasks

- [x] Restructure the starter MAUI app into the documented layer and feature boundaries. (AC: 1)
  - [x] Replace code-behind driven page behavior with ViewModel-first presentation using CommunityToolkit.Mvvm source generators.
  - [x] Introduce folders and namespaces for `Features`, `Domain`, `Application`, `Infrastructure`, and `Shared` inside the MAUI app project.
  - [x] Keep Shell as the navigation host and avoid direct Infrastructure calls from views or viewmodels.
- [x] Establish the baseline application contracts and result patterns. (AC: 1, 2, 3)
  - [x] Define provider-agnostic repository interfaces under `Application/Abstractions/Repositories`.
  - [x] Define a standard result envelope under `Application/Results` with `isSuccess`, `errorCode`, `userMessage`, `diagnosticMessage`, and `payload`.
  - [x] Introduce canonical operation, correlation, and causation IDs at command boundaries.
- [x] Implement the V1 persistence baseline using the architecture-selected EF Core provider model. (AC: 2, 3, 4)
  - [x] Add EF Core SQLite packages and create the initial persistence project structure under `Infrastructure/Persistence`.
  - [x] Create the first DbContext, entity configurations, and migration baseline with stable GUID identifiers and UTC audit columns.
  - [x] Add append-only audit and outbox or operation-log entities that can support ordered, idempotent replay later.
  - [x] Select and document an encrypted SQLite strategy compatible with MAUI and the provider abstraction; do not ship an unencrypted local store.
- [x] Wire dependency injection and shared services in `MauiProgram`. (AC: 1, 2, 3)
  - [x] Register repositories, persistence services, app-state services, and feature registration extension methods.
  - [x] Centralize persistence configuration and environment constants instead of scattering setup across pages.
- [x] Add baseline test and CI coverage for the new foundation. (AC: 3, 4, 5)
  - [x] Create `POSOpen.Tests` with unit, integration, and contract test groupings.
  - [x] Add integration coverage for SQLite persistence, migration startup, UTC persistence, and append-only outbox writes.
  - [x] Add contract tests for result-envelope and event-schema consistency.
  - [x] Add or update `.github/workflows/ci.yml` to run build, tests, analyzers, and persistence migration validation.
- [x] Preserve UX and operational foundations while building the baseline. (AC: 1, 3, 5)
  - [x] Keep sync and deferred-state semantics explicit in app-state models even if the full offline UI appears in later stories.
  - [x] Ensure any baseline status indicators follow the UX requirement that queued, retrying, failed, and synced states are distinct using icon plus text, not color alone.

## Dev Notes

### Story Intent

This story is the architectural foundation for the rest of the backlog. The developer should optimize for correct boundaries and migration safety, not for shipping visible business workflows yet. The main output is a trustworthy skeleton that later stories can extend without rework.

### Current Repo Reality

- The current repo is still the default single-project MAUI starter with `App.xaml`, `AppShell.xaml`, `MainPage.xaml`, and minimal code-behind.
- `MauiProgram.cs` currently only configures fonts and debug logging.
- `POSOpen.csproj` already includes `CommunityToolkit.Mvvm`, `Microsoft.Extensions.Logging`, and `sqlite-net-pcl`.
- There is no `POSOpen.Tests` project yet.
- There are no existing implementation story files to inherit patterns from.

### Critical Guardrails

- Follow the architecture-selected stack: .NET 10 MAUI, CommunityToolkit.Mvvm, SQLite V1, SQL Server V2 path behind the same repository abstractions.
- Do not build persistence directly on `sqlite-net-pcl` if it would bypass the documented EF Core provider-swap strategy. The architecture explicitly calls for `Microsoft.EntityFrameworkCore.Sqlite` now and `Microsoft.EntityFrameworkCore.SqlServer` later behind the same repository interfaces.
- Respect layer boundaries strictly: Presentation does not call Infrastructure directly.
- Persist timestamps in UTC only and serialize in ISO-8601.
- All write paths must emit GUID-based operation IDs, plus correlation and causation IDs where applicable.
- Audit and outbox records are append-only.
- Do not store secrets in preferences. Use secure platform storage for secrets.
- Any future payment integration must remain tokenized or hosted to minimize PCI scope; do not create local raw card storage.

### Architecture Compliance

- Presentation pattern: ViewModel-first using CommunityToolkit.Mvvm.
- Navigation pattern: Shell with role-gated route access.
- Internal flow: ViewModel -> UseCase -> Repository or Service -> Persistence or Adapter.
- Result pattern: standard result envelope with separate user-safe and diagnostic messaging.
- Event pattern: past-tense PascalCase event names with `eventId`, `eventType`, `occurredUtc`, `aggregateId`, `operationId`, `version`, and `payload`.
- State pattern: explicit ViewModel state transitions `Idle -> Loading -> Success|Error|Deferred`.
- Validation split: ViewModel for field validation, Application for business rules, Infrastructure for persistence constraints.

### File Structure Requirements

Target structure for this story:

- `POSOpen/Features/*` for feature-facing views, viewmodels, commands, and DTOs.
- `POSOpen/Domain/*` for entities, value objects, enums, policies, and events.
- `POSOpen/Application/*` for abstractions, use cases, validation, and result models.
- `POSOpen/Infrastructure/*` for persistence, security, sync, export, devices, and logging.
- `POSOpen/Shared/*` for constants, extensions, and serialization helpers.
- `POSOpen.Tests/*` for unit, integration, contracts, and test data.

Detected variance to resolve in this story:

- The repo currently has only the default MAUI starter files and no layer-aligned folders.
- The repo currently has no CI workflow file visible in the workspace tree.
- The repo currently has no tests project, despite architecture and epic requirements calling for baseline build, test, analyzer, and migration validation.

### Library / Framework Requirements

- Required: `CommunityToolkit.Mvvm` for ViewModel and command generation.
- Required: `Microsoft.EntityFrameworkCore.Sqlite` for V1 persistence implementation.
- Planned later: `Microsoft.EntityFrameworkCore.SqlServer` behind the same repository interfaces.
- Required: `Microsoft.Extensions.DependencyInjection` and `Microsoft.Extensions.Logging` registrations in `MauiProgram`.
- Required: an encrypted SQLite strategy compatible with MAUI and the persistence design. If package selection has tradeoffs, document the decision as an ADR while still meeting NFR11.

### Testing Requirements

- Add unit tests for result-envelope helpers, ID and UTC helpers, and any initial application services.
- Add integration tests for SQLite database creation, migration application, encrypted store startup, UTC persistence, and append-only outbox writes.
- Add contract tests for result envelope shape and domain event payload schema.
- CI must validate build, test execution, analyzers, and migration startup.

### UX / Accessibility Notes

- Even though this story is infrastructure-heavy, baseline UI scaffolding should align to the chosen `Guided Mission Control` direction.
- Preserve role-aware Shell and ViewModel structure so later stories can add `Next Best Action` and explicit queue status without re-platforming.
- Any early queue or sync indicator must keep queued, retrying, failed, and synced states explicit with icon and text.
- Tablet is the primary frontline target, but interaction logic must remain consistent across tablet and desktop.
- Status communication cannot rely on color alone and should preserve WCAG 2.2 AA expectations.

### Definition of Done Notes for Dev

- The app is no longer a default counter sample in architecture or code organization.
- A developer can add later feature stories without introducing direct view-to-database coupling.
- SQLite V1 is operational behind abstractions and can evolve through migrations.
- The encrypted local data path is selected and implemented, not deferred ambiguously.
- CI catches build, test, analyzer, and migration regressions.

### Project Structure Notes

- The architecture expects a single MAUI app package for V1, but not a single-layer codebase. Keep the existing MAUI project and restructure internally instead of inventing a different host model.
- The current `POSOpen.csproj` targets Android, iOS, MacCatalyst, and Windows. Baseline persistence and encryption choices must remain compatible with that cross-platform target set.
- Because there is no prior story context, this story should leave clear extension points and avoid speculative business logic.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Epic-1-Terminal-Access-Roles-and-Secure-Staff-Operations]
- [Source: _bmad-output/planning-artifacts/epics.md#Story-10-Initialize-MAUI-Starter-and-Local-Persistence-Baseline]
- [Source: _bmad-output/planning-artifacts/architecture.md#Selected-Starter-NET-MAUI-App-Template]
- [Source: _bmad-output/planning-artifacts/architecture.md#Data-Architecture]
- [Source: _bmad-output/planning-artifacts/architecture.md#Frontend-Architecture]
- [Source: _bmad-output/planning-artifacts/architecture.md#Implementation-Patterns--Consistency-Rules]
- [Source: _bmad-output/planning-artifacts/architecture.md#Project-Structure--Boundaries]
- [Source: _bmad-output/planning-artifacts/prd.md#Non-Functional-Requirements]
- [Source: _bmad-output/planning-artifacts/prd.md#Domain-Specific-Requirements]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Chosen-Direction]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Component-Implementation-Strategy]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#UX-Consistency-Patterns]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Responsive-Design--Accessibility]
- [Source: POSOpen/POSOpen.csproj]
- [Source: POSOpen/MauiProgram.cs]

## Dev Agent Record

### Agent Model Used

GPT-5.4

### Debug Log References

- `dotnet test .\POSOpen.Tests\POSOpen.Tests.csproj --framework net10.0-windows10.0.19041.0`
- `dotnet build .\POSOpen\POSOpen.csproj --framework net10.0-windows10.0.19041.0`
- `dotnet ef migrations add InitializePersistenceBaseline --project .\POSOpen\POSOpen.csproj --startup-project .\POSOpen\POSOpen.csproj --framework net10.0-windows10.0.19041.0 --context POSOpen.Infrastructure.Persistence.PosOpenDbContext --output-dir Infrastructure/Persistence/Migrations`
- `dotnet ef database update --project .\POSOpen\POSOpen.csproj --startup-project .\POSOpen\POSOpen.csproj --framework net10.0-windows10.0.19041.0 --context POSOpen.Infrastructure.Persistence.PosOpenDbContext`

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Resolved an architecture-versus-repo mismatch by explicitly steering persistence toward EF Core SQLite rather than repository implementations bound directly to `sqlite-net-pcl`.
- No prior story learnings were available because this is the first implementation story in the epic.
- Replaced the MAUI starter counter flow with a DI-driven Shell home page and ViewModel-first presentation baseline.
- Added encrypted EF Core SQLite persistence with SQLCipher provider binding, UTC-safe converters, provider-agnostic repositories, operation context generation, and startup initialization.
- Generated the initial EF Core migration baseline and verified `database update` succeeds through the design-time factory.
- Added unit, contract, and integration tests around result serialization, operation IDs, encrypted database creation, UTC persistence, and append-only outbox writes.
- Isolated the test project from the MAUI asset pipeline by linking non-UI foundation source files instead of using a direct project reference.
- Added a Windows CI workflow that restores workloads, builds the solution, runs tests, and validates the EF migration startup path.

### File List

- _bmad-output/implementation-artifacts/1-0-initialize-maui-starter-and-local-persistence-baseline.md
- POSOpen/App.xaml.cs
- POSOpen/MauiProgram.cs
- POSOpen/POSOpen.csproj
- POSOpen/Application/Abstractions/Persistence/IAppDbContextInitializer.cs
- POSOpen/Application/Abstractions/Repositories/IOperationLogRepository.cs
- POSOpen/Application/Abstractions/Repositories/IOutboxRepository.cs
- POSOpen/Application/Abstractions/Services/IEncryptionKeyProvider.cs
- POSOpen/Application/Abstractions/Services/IOperationContextFactory.cs
- POSOpen/Application/Abstractions/Services/IUtcClock.cs
- POSOpen/Domain/Entities/OperationLogEntry.cs
- POSOpen/Domain/Entities/OutboxMessage.cs
- POSOpen/Features/Shell/ViewModels/HomeViewModel.cs
- POSOpen/Infrastructure/Persistence/AppDbContextInitializer.cs
- POSOpen/Infrastructure/Persistence/Configurations/OperationLogEntryConfiguration.cs
- POSOpen/Infrastructure/Persistence/Configurations/OutboxMessageConfiguration.cs
- POSOpen/Infrastructure/Persistence/DesignTime/PosOpenDesignTimeDbContextFactory.cs
- POSOpen/Infrastructure/Persistence/Migrations/20260328225942_InitializePersistenceBaseline.cs
- POSOpen/Infrastructure/Persistence/Migrations/20260328225942_InitializePersistenceBaseline.Designer.cs
- POSOpen/Infrastructure/Persistence/Migrations/PosOpenDbContextModelSnapshot.cs
- POSOpen/Infrastructure/Persistence/PersistenceServiceCollectionExtensions.cs
- POSOpen/Infrastructure/Persistence/PosOpenDatabasePathOptions.cs
- POSOpen/Infrastructure/Persistence/PosOpenDbContext.cs
- POSOpen/Infrastructure/Persistence/Repositories/OperationLogRepository.cs
- POSOpen/Infrastructure/Persistence/Repositories/OutboxRepository.cs
- POSOpen/Infrastructure/Persistence/SqliteConnectionStringFactory.cs
- POSOpen/Infrastructure/Persistence/SqliteProviderBootstrapper.cs
- POSOpen/Infrastructure/Persistence/ValueConverters/UtcDateTimeConverter.cs
- POSOpen/Infrastructure/Security/SecureStorageEncryptionKeyProvider.cs
- POSOpen/Infrastructure/Services/OperationContextFactory.cs
- POSOpen/Infrastructure/Services/SystemUtcClock.cs
- POSOpen/Shared/Operational/OperationContext.cs
- POSOpen/Shared/Serialization/AppJsonSerializerOptions.cs
- POSOpen.Tests/POSOpen.Tests.csproj
- POSOpen.Tests/Integration/Persistence/AppDbContextInitializerTests.cs
- POSOpen.Tests/Integration/Persistence/PersistenceRepositoryTests.cs
- POSOpen.Tests/TestDoubles/TestDatabasePaths.cs
- POSOpen.Tests/TestDoubles/TestDbContextFactory.cs
- POSOpen.Tests/TestDoubles/TestUtcClock.cs
- POSOpen.Tests/Unit/Operational/OperationContextFactoryTests.cs
- POSOpen.Tests/Unit/Serialization/AppResultSerializationContractTests.cs
- .github/workflows/ci.yml

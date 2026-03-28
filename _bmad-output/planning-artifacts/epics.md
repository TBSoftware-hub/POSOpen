---
stepsCompleted: [1, 2, 3, 4]
inputDocuments:
  - prd.md
  - architecture.md
  - ux-design-specification.md
  - brainstorming-session-2026-03-28-113758.md
  - prd-finalization-summary.md
---

# POSOpen - Epic Breakdown

## Overview

This document provides the complete epic and story breakdown for POSOpen, decomposing the requirements from the PRD, UX Design, and Architecture requirements into implementable stories.

## Requirements Inventory

### Functional Requirements

FR1: Owner and Admin can create, update, and deactivate staff accounts.
FR2: Owner and Admin can assign and change role permissions for staff users.
FR3: System can enforce role-based access boundaries for cashier, party coordinator, manager, and owner/admin.
FR4: Owner and Admin can perform override actions with required reason capture.
FR5: System can log all override and elevation actions with actor and timestamp.
FR6: Cashier can look up returning families using available identifiers.
FR7: Cashier can process fast-path check-in when waiver status is valid.
FR8: Cashier can process admission for new or incomplete profiles.
FR9: System can indicate waiver status for admission decisions.
FR10: Cashier can complete admission transactions during offline operation.
FR11: Cashier can continue check-in flow when payment settlement is deferred.
FR12: Cashier can create transactions containing admissions, retail items, party deposits, and catering add-ons.
FR13: Cashier can assign transaction line items to relevant internal contexts.
FR14: Cashier can apply allowed inventory substitutions during transaction creation.
FR15: System can validate transaction compatibility rules before completion.
FR16: System can present transaction exceptions that require operator action.
FR17: Cashier can complete mixed-cart transactions while offline.
FR18: Party coordinator can create party bookings using a date-time-package flow.
FR19: Party coordinator can collect and record party deposit commitments.
FR20: System can generate a lifecycle timeline for each confirmed party booking.
FR21: Party coordinator can update party details and view downstream impacts.
FR22: Party coordinator can manage room assignment and booking conflicts.
FR23: Party coordinator can manage catering and decor options for bookings.
FR24: System can surface booking risk indicators before event execution.
FR25: System can reserve party-related inventory at booking commitment time.
FR26: System can release reserved inventory based on defined cancellation and change policies.
FR27: Manager can define and maintain substitution policies for inventory-constrained items.
FR28: Cashier and party coordinator can view inventory availability state relevant to their tasks.
FR29: System can prevent finalization of bookings that violate inventory constraints unless resolved.
FR30: System can allow critical frontline workflows to continue without internet connectivity.
FR31: System can queue offline-captured operational and financial actions for later synchronization.
FR32: System can synchronize queued actions when connectivity is restored.
FR33: System can preserve action ordering and idempotent replay during synchronization.
FR34: System can prevent duplicate finalization of financial actions after reconnect.
FR35: System can expose synchronization status and unresolved sync exceptions to operators.
FR36: System can support tablet-led operational continuity when Windows back-office is unavailable.
FR37: Cashier can print transaction receipts to supported receipt printers.
FR38: Cashier can capture item and reference data via scanner devices.
FR39: Cashier can execute card-present payment interactions with supported card readers.
FR40: System can notify operators when required hardware is unavailable and provide fallback guidance.
FR41: System can maintain immutable records for financial events, overrides, and settlement outcomes.
FR42: Manager and owner/admin can review deferred-payment outcomes and unresolved exceptions.
FR43: Manager can complete end-of-day reconciliation workflows using system records.
FR44: System can provide traceability between offline-captured actions and synchronized settlement outcomes.
FR45: System can support refund workflows within role and policy boundaries.
FR46: Manager and owner/admin can view operational dashboards for frontline throughput, party operations, and exception states.
FR47: Manager and owner/admin can view inventory risk and booking-risk summaries.
FR48: Owner and Admin can monitor synchronization health and backlog status.
FR49: System can export operational and financial data as CSV for external processing.
FR50: System can provide per-location performance and cost visibility for subscription oversight.
FR51: Support and authorized operations users can investigate incidents by transaction, device, and error context.
FR52: Support and authorized operations users can trigger controlled retry workflows for failed deferred actions.
FR53: Support and authorized operations users can execute approved rollback and config correction actions.
FR54: System can expose integration health status for payment, hardware, and export pipelines.
FR55: System can retain diagnostic evidence necessary for post-incident analysis.

### NonFunctional Requirements

NFR1: Frontline admission and checkout actions must return operator feedback within 2 seconds under normal connected conditions.
NFR2: Role-mode home views must load usable task state within 3 seconds after user authentication.
NFR3: Mixed-cart validation must complete within 2 seconds for standard transaction sizes in normal operating conditions.
NFR4: Booking timeline retrieval and update actions must complete within 3 seconds for active-day operational use.
NFR5: Critical frontline workflows must remain available during internet outage.
NFR6: Queued offline actions must synchronize within 5 minutes after connectivity restoration in normal recovery conditions.
NFR7: Synchronization must be idempotent and must not produce duplicate finalized financial outcomes.
NFR8: System must preserve local queue durability across application restart and device reboot events.
NFR9: Tablet-led failover mode must allow continuation of essential operations when Windows back-office is unavailable.
NFR10: All sensitive data in transit must be encrypted using current industry-standard transport protection.
NFR11: Stored operational and financial records must be encrypted at rest.
NFR12: Role-based authorization must be enforced server-side for all protected actions.
NFR13: Override, refund, substitution, and elevation actions must be captured in immutable audit records.
NFR14: Payment handling must minimize PCI scope through tokenized or hosted card-processing patterns where feasible.
NFR15: Hardware integrations must provide deterministic success and failure feedback to operators.
NFR16: CSV export outputs must use stable identifiers and schema consistency for reconciliation workflows.
NFR17: External integration failures must be surfaced with actionable error context for support and operations users.
NFR18: Integration retry behavior must avoid duplicate side effects for financial and inventory-related actions.
NFR19: V1 architecture must support expansion from one active location to multiple locations without fundamental data model redesign.
NFR20: Performance degradation under planned growth scenarios must remain within acceptable operational tolerance.
NFR21: System must support concurrent role-based usage across multiple in-location devices during peak periods without loss of data integrity.
NFR22: Frontline interfaces must remain operable with clear visual hierarchy and readable controls for prolonged shift-based usage.
NFR23: Core operator workflows must be executable without requiring deep navigation chains.
NFR24: User-visible system states (offline, syncing, exception) must be explicit, unambiguous, and consistently represented.

### Additional Requirements

- Starter and platform baseline: .NET 10 MAUI app with CommunityToolkit.Mvvm pattern.
- V1 persistence: SQLite local database as primary store.
- V2 persistence path: SQL Server provider (Azure SQL free offer) behind same repository abstractions.
- Use provider-agnostic repository interfaces to avoid data-layer rewrites between V1 and V2.
- Implement operation log/outbox pattern with idempotent replay and operation correlation IDs.
- Persist and process all timestamps in UTC and ISO-8601 serialization.
- Enforce strict layer boundaries: Presentation -> Application -> Infrastructure (no direct Presentation -> Infrastructure calls).
- Standardized command/use-case result envelope with canonical error codes.
- Immutable audit event model for financial and override actions.
- Role-based authorization implemented in application/service layer (not UI-only).
- Local security hardening ADRs required for credential storage and lockout policy.
- Backup/restore runbook and automated local snapshots required for single-terminal reliability.
- Device abstraction boundaries for printer, scanner, and card reader integrations.
- Contract tests required for event payload and result envelope consistency.
- CI checks should include build/test/analyzers and persistence migration validation.

### UX Design Requirements

UX-DR1: Implement role-mode interfaces (front desk, party coordinator, manager) with context-sensitive actions.
UX-DR2: Implement the defining fast-lane interaction to complete a guest moment in under 20 seconds.
UX-DR3: Implement persistent status visibility for waiver/payment/sync states in critical workflows.
UX-DR4: Implement one-screen completion pattern for core frontline flows where possible.
UX-DR5: Implement design token system for colors, spacing, typography, motion, and semantic states.
UX-DR6: Use the selected visual direction baseline: Guided Mission Control (Direction 6).
UX-DR7: Implement Next Best Action guidance in high-frequency workflows.
UX-DR8: Implement offline queue visibility and recovery UI with explicit queued/retrying/failed/synced states.
UX-DR9: Implement family-friendly tone and messaging patterns for guest-facing interactions.
UX-DR10: Implement party timeline visualization with lifecycle states and inline action prompts.
UX-DR11: Implement component set: Guest Moment Fast Lane Panel, Confidence Strip, Party Timeline Rail.
UX-DR12: Implement component set: Offline Queue Badge + Drawer, Next Best Action Card, Inline Recovery Composer.
UX-DR13: Enforce button hierarchy standards with sticky primary actions on tablet and fixed footer pattern on mobile.
UX-DR14: Enforce uniform feedback patterns (success, warning, error, info) with user-safe and actionable messages.
UX-DR15: Implement form patterns with layered validation, non-destructive error handling, and inline recovery.
UX-DR16: Enforce search-first retrieval with scan fallback for guest/booking lookup.
UX-DR17: Apply responsive strategy: mobile companion views, tablet frontline primary, desktop manager density.
UX-DR18: Enforce breakpoints: mobile 320-767, tablet 768-1023, desktop 1024+ with 1280+ manager enhancement.
UX-DR19: Meet WCAG 2.2 AA baseline; target higher readability for critical flows where practical.
UX-DR20: Ensure accessibility behaviors: keyboard parity, visible focus, ARIA linking, icon+text status communication.
UX-DR21: Implement touch targets minimum 44x44, preferred 48x48 for primary actions.
UX-DR22: Implement low-stimulation UX options (reduced motion, calmer visuals) for neurodiversity support.
UX-DR23: Preserve user-entered data on validation/network errors and avoid forced flow restarts.
UX-DR24: Maintain cross-form-factor interaction parity (same logic, density/layout variants only).

### FR Coverage Map

FR1: Epic 1 - Staff account lifecycle management
FR2: Epic 1 - Role assignment and updates
FR3: Epic 1 - Role boundary enforcement
FR4: Epic 1 - Override with reason capture
FR5: Epic 1 - Override/elevation audit logging
FR6: Epic 2 - Returning family lookup
FR7: Epic 2 - Fast-path check-in with valid waiver
FR8: Epic 2 - New/incomplete profile admission
FR9: Epic 2 - Waiver status visibility for decisions
FR10: Epic 2 - Offline admission continuity
FR11: Epic 2 - Deferred settlement continuity
FR12: Epic 3 - Mixed-cart transaction creation
FR13: Epic 3 - Transaction line-item context mapping
FR14: Epic 3 - Allowed substitution at checkout
FR15: Epic 3 - Compatibility rule validation
FR16: Epic 3 - Checkout exception handling
FR17: Epic 3 - Offline mixed-cart completion
FR18: Epic 4 - Date-time-package booking flow
FR19: Epic 4 - Deposit capture and recording
FR20: Epic 4 - Party lifecycle timeline generation
FR21: Epic 4 - Party update impact visibility
FR22: Epic 4 - Room assignment/conflict management
FR23: Epic 4 - Catering/decor option management
FR24: Epic 4 - Booking risk surfacing
FR25: Epic 4 - Booking-time inventory reservation
FR26: Epic 4 - Inventory release policy execution
FR27: Epic 4 - Substitution policy maintenance
FR28: Epic 4 - Inventory availability visibility
FR29: Epic 4 - Inventory constraint booking prevention
FR30: Epic 5 - Offline operation for critical workflows
FR31: Epic 5 - Queue offline operational/financial actions
FR32: Epic 5 - Queue synchronization after reconnect
FR33: Epic 5 - Ordered, idempotent replay
FR34: Epic 5 - Duplicate finalization prevention
FR35: Epic 5 - Sync status and unresolved exception visibility
FR36: Epic 5 - Tablet-led failover continuity
FR37: Epic 3 - Receipt printing capability
FR38: Epic 3 - Scanner capture capability
FR39: Epic 3 - Card-reader payment interactions
FR40: Epic 3 - Hardware unavailable fallback guidance
FR41: Epic 6 - Immutable financial/audit records
FR42: Epic 6 - Deferred-payment exception review
FR43: Epic 6 - End-of-day reconciliation
FR44: Epic 5 - Traceability from offline capture to settlement
FR45: Epic 3 - Role/policy-bound refund workflows
FR46: Epic 6 - Operational dashboard visibility
FR47: Epic 6 - Inventory/booking risk summaries
FR48: Epic 6 - Sync health/backlog monitoring
FR49: Epic 6 - CSV export for external processing
FR50: Epic 6 - Per-location performance and cost visibility
FR51: Epic 7 - Incident investigation by transaction/device/error
FR52: Epic 7 - Controlled retry workflows
FR53: Epic 7 - Approved rollback/config correction
FR54: Epic 7 - Integration health status exposure
FR55: Epic 7 - Diagnostic evidence retention

## Epic List

### Epic 1: Terminal Access, Roles, and Secure Staff Operations
Enable secure staff access, role-based operation modes, and governed override actions with auditability.
**FRs covered:** FR1, FR2, FR3, FR4, FR5

### Epic 2: Admissions and Fast Guest Check-In
Enable front desk teams to quickly find families, validate waiver state, and complete admissions including deferred-settlement continuity.
**FRs covered:** FR6, FR7, FR8, FR9, FR10, FR11

### Epic 3: Mixed-Cart Checkout, Payments, and Device Execution
Enable mixed-cart checkout across admissions, retail, party, and catering with integrated payment, receipt, scanner, and refund support.
**FRs covered:** FR12, FR13, FR14, FR15, FR16, FR17, FR37, FR38, FR39, FR40, FR45

### Epic 4: Party Booking Lifecycle and Inventory Coordination
Enable full party booking and execution lifecycle with room/catering/decor management and inventory reservation controls.
**FRs covered:** FR18, FR19, FR20, FR21, FR22, FR23, FR24, FR25, FR26, FR27, FR28, FR29

### Epic 5: Offline Continuity, Queueing, and Synchronization Integrity
Enable outage-proof operations with queued transactions, deterministic replay, sync transparency, and failover continuity.
**FRs covered:** FR30, FR31, FR32, FR33, FR34, FR35, FR36, FR44

### Epic 6: Financial Governance, Reconciliation, and Management Visibility
Enable managers and owners to govern financial operations, reconcile daily activity, and monitor business/operational health.
**FRs covered:** FR41, FR42, FR43, FR46, FR47, FR48, FR49, FR50

### Epic 7: Support Diagnostics and Operational Recovery
Enable support and operations users to diagnose incidents, run controlled recovery workflows, and maintain integration reliability.
**FRs covered:** FR51, FR52, FR53, FR54, FR55

## Epic 1: Terminal Access, Roles, and Secure Staff Operations

Enable secure staff access, role-based operation modes, and governed override actions with auditability.

### Story 1.0: Initialize MAUI Starter and Local Persistence Baseline

**[TECH]** As a developer,
I need to scaffold the .NET 10 MAUI solution with CommunityToolkit.Mvvm and SQLite-backed repository abstractions,
So that all subsequent stories build on a stable, provider-agnostic foundation.

**Acceptance Criteria:**

**Given** the project baseline is being initialized
**When** the solution is scaffolded
**Then** it uses .NET 10 MAUI app structure with CommunityToolkit.Mvvm-integrated presentation patterns
**And** layer boundaries are established (Presentation -> Application -> Infrastructure).

**Given** V1 persistence is required
**When** data access foundation is implemented
**Then** SQLite is configured as primary local store
**And** repository interfaces are provider-agnostic for later SQL Server provider introduction.

**Given** operational reliability constraints are required
**When** baseline infrastructure is completed
**Then** operation IDs, UTC timestamp conventions, and outbox/operation-log scaffolding are present
**And** baseline build/test/analyzer checks pass in CI.

**Given** the application handles sensitive data
**When** the SQLite database is created and written to
**Then** the database file is encrypted at rest using SQLCipher or equivalent (NFR11).

**Given** the application makes any network call
**When** the connection is established
**Then** all data in transit is protected using current industry-standard transport encryption (TLS) (NFR10).

### Story 1.1: Staff Account Management

As an Owner/Admin,
I want to create, update, and deactivate staff accounts,
So that only authorized staff can use POSOpen.

**Acceptance Criteria:**

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

### Story 1.2: Role Assignment and Enforcement

As an Owner/Admin,
I want to assign and update role permissions for staff users,
So that each user can only access allowed operational capabilities.

**Acceptance Criteria:**

**Given** I am an authenticated Owner/Admin
**When** I assign a role to a staff account
**Then** role mapping is persisted
**And** the assigned permissions are effective at session refresh.

**Given** a Cashier account
**When** the user attempts Manager-only actions
**Then** access is denied
**And** a user-safe authorization message is shown.

**Given** role permissions are updated
**When** the affected user signs in again
**Then** visible navigation/actions match updated role policy
**And** stale permissions are not applied.

### Story 1.3: Terminal Authentication Flow

As a staff user,
I want to authenticate on the facility terminal,
So that I can access my role-appropriate workspace securely.

**Acceptance Criteria:**

**Given** a valid active staff account
**When** valid credentials are submitted
**Then** authentication succeeds
**And** a role-scoped session is established.

**Given** invalid credentials
**When** sign-in is attempted
**Then** authentication fails
**And** non-revealing error messaging is shown.

**Given** an inactive account
**When** sign-in is attempted
**Then** access is denied
**And** no session is created.

**Given** authentication succeeds
**When** the role-mode home view is loaded
**Then** the view is fully rendered and interactive within 3 seconds on the target device (NFR2).

### Story 1.4: Governed Override Workflow

As a Manager/Owner/Admin,
I want to execute override actions with mandatory reason capture,
So that exceptional operations are controlled and traceable.

**Acceptance Criteria:**

**Given** I have override permission
**When** I initiate an override action
**Then** the system requires a reason before commit
**And** action context is visible to the approver.

**Given** reason is missing
**When** I confirm override
**Then** action is blocked
**And** a validation error is displayed.

**Given** reason is provided and permission is valid
**When** override is confirmed
**Then** the action succeeds
**And** override metadata is recorded immutably.

### Story 1.5: Immutable Audit Trail for Security-Critical Actions

As an Owner/Admin,
I want immutable audit records for account, role, override, and elevation actions,
So that governance and incident analysis are reliable.

**Acceptance Criteria:**

**Given** a security-critical action occurs (account change, role change, override, elevation)
**When** the action completes
**Then** an immutable audit record is appended
**And** it includes actor, timestamp UTC, action type, and target reference.

**Given** audit records exist
**When** queried by authorized users
**Then** records are returned in chronological order
**And** records are non-editable and non-destructive.

**Given** unauthorized users attempt audit access
**When** request is made
**Then** access is denied
**And** denial is logged.

## Epic 2: Admissions and Fast Guest Check-In

Enable front desk teams to quickly find families, validate waiver state, and complete admissions including deferred-settlement continuity.

### Story 2.1: Family Lookup with Search and Scan

As a cashier,
I want to find a family using search or QR/barcode scan,
So that I can start check-in quickly with minimal friction.

**Acceptance Criteria:**

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

### Story 2.2: Waiver-Aware Fast-Path Check-In

As a cashier,
I want the system to evaluate waiver status and route the check-in path,
So that valid families can be checked in quickly and missing waivers are handled safely.

**Acceptance Criteria:**

**Given** a selected family has a valid waiver
**When** check-in starts
**Then** system enables fast-path admissions flow
**And** waiver-valid state is clearly shown.

**Given** waiver is missing/expired/invalid
**When** check-in starts
**Then** system blocks fast-path completion
**And** required waiver recovery action is presented.

**Given** waiver status changes during session
**When** status is re-evaluated
**Then** check-in eligibility updates immediately
**And** stale status is not used.

### Story 2.3: New or Incomplete Profile Admission

As a cashier,
I want to admit families with missing profile data through a guided minimal flow,
So that admissions continue without losing required information quality.

**Acceptance Criteria:**

**Given** no existing profile is found
**When** cashier chooses create profile
**Then** required minimum fields are collected and validated
**And** profile is created for immediate admission use.

**Given** a profile is incomplete
**When** cashier proceeds with admission
**Then** system prompts for missing mandatory fields only
**And** previously entered values are preserved.

**Given** required fields fail validation
**When** user submits
**Then** submission is blocked
**And** field-level actionable errors are shown without clearing inputs.

### Story 2.4: Admission Completion with Deferred Payment Continuity

As a cashier,
I want admissions to complete even when payment settlement is deferred/offline,
So that frontline operations continue during network disruption.

**Acceptance Criteria:**

**Given** network and processor are available
**When** payment is authorized
**Then** admission is marked completed
**And** receipt/check-in confirmation is issued.

**Given** settlement cannot complete due to connectivity/processor outage
**When** cashier confirms admission
**Then** payment action is queued with unique operation ID
**And** admission continuation follows deferred-payment policy.

**Given** a deferred payment exists
**When** cashier views current admission state
**Then** explicit queued/deferred status is shown
**And** next-best-action guidance is visible.

### Story 2.5: Check-In Performance and UX Compliance

As a manager/product owner,
I want admissions/check-in to meet speed and UX consistency targets,
So that staff can reliably complete the core guest moment under pressure.

**Acceptance Criteria:**

**Given** normal connected operation
**When** standard returning-family check-in is executed
**Then** operator feedback is returned within 2 seconds (NFR1)
**And** path supports the fast-lane UX interaction model.

**Given** check-in UI is displayed
**When** primary/secondary actions are rendered
**Then** button hierarchy, status messaging, and accessibility patterns follow UX standards.

**Given** a check-in flow is interrupted by recoverable errors
**When** cashier resolves issue inline
**Then** flow continues without forced restart
**And** prior user input remains preserved.

## Epic 3: Mixed-Cart Checkout, Payments, and Device Execution

Enable mixed-cart checkout across admissions, retail, party, and catering with integrated payment, receipt, scanner, and refund support.

### Story 3.1: Build Mixed-Cart Composition by Fulfillment Context

As a cashier,
I want to create a cart containing admissions, retail, party deposit, and catering add-ons,
So that I can complete one combined transaction for the customer.

**Acceptance Criteria:**

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

### Story 3.2: Enforce Compatibility Rules and Guided Resolution

As a cashier,
I want compatibility checks to run before completion,
So that invalid item combinations are prevented with clear fixes.

**Acceptance Criteria:**

**Given** a mixed cart has conflicting items/policies
**When** validation runs
**Then** blocking issues are surfaced with actionable messages
**And** completion is prevented until resolved.

**Given** a validation issue is shown
**When** I choose a suggested fix
**Then** the cart updates in place
**And** validation reruns automatically.

**Given** the cart becomes valid
**When** validation completes
**Then** checkout can proceed
**And** prior warnings are cleared.

**Given** a mixed cart is submitted for validation
**When** compatibility rules are evaluated
**Then** validation result is returned within 2 seconds (NFR3).

### Story 3.3: Integrate Scanner and Card Reader for Checkout

As a cashier,
I want scanner and card-reader integrations in checkout,
So that checkout is fast and reliable at the terminal.

**Acceptance Criteria:**

**Given** scanner hardware is available
**When** barcode/reference is scanned
**Then** matching item/reference is added or selected in cart.

**Given** card reader is available
**When** payment is initiated
**Then** payment workflow uses the configured device adapter
**And** authorization outcome is captured in transaction state.

**Given** hardware is unavailable/faulted
**When** checkout attempts device operation
**Then** deterministic fallback guidance is shown
**And** issue is logged with diagnostic code.

**Given** a card payment is initiated
**When** card data is captured by the reader
**Then** raw PANs are never stored or logged within the application layer
**And** all payment data flows through the tokenized/hosted card processing integration only (NFR14).

### Story 3.4: Print Receipts and Preserve Offline Continuity

As a cashier,
I want receipts printed and transaction continuity preserved in offline conditions,
So that customer fulfillment and operational traceability are maintained.

**Acceptance Criteria:**

**Given** a transaction is completed online
**When** receipt printing is requested
**Then** receipt prints to supported printer
**And** receipt metadata is saved.

**Given** transaction completion occurs while offline/deferred
**When** receipt action is executed
**Then** provisional/deferred status is clearly indicated
**And** operation IDs are attached for later reconciliation.

**Given** printer is unavailable
**When** print fails
**Then** user receives fallback instructions
**And** failure is captured for support diagnostics.

### Story 3.5: Policy-Bound Refund Workflow

As a manager/cashier with permission,
I want to process refunds within policy boundaries,
So that corrections are possible without violating governance controls.

**Acceptance Criteria:**

**Given** I have refund permission and eligible transaction context
**When** refund is initiated
**Then** allowed refund paths are presented per role/policy.

**Given** refund requires approval/override
**When** approval reason is not provided
**Then** refund cannot finalize
**And** reason capture is enforced.

**Given** refund completes
**When** transaction state is updated
**Then** immutable audit records include actor, reason, operation ID, and UTC timestamp.

## Epic 4: Party Booking Lifecycle and Inventory Coordination

Enable full party booking and execution lifecycle with room/catering/decor management and inventory reservation controls.

### Story 4.1: Implement 3-Step Party Booking Flow

As a party coordinator,
I want to create party bookings using date, time, and package steps,
So that bookings can be created quickly and consistently.

**Acceptance Criteria:**

**Given** I start a new booking
**When** I complete date-time-package steps
**Then** a draft booking is created with required booking metadata
**And** unavailable slots are prevented from selection.

**Given** required booking fields are incomplete
**When** I attempt to continue
**Then** progression is blocked
**And** actionable validation prompts are shown.

**Given** I confirm booking details
**When** booking is saved
**Then** a persistent booking record is created with unique booking ID
**And** initial lifecycle status is set.

### Story 4.2: Capture Deposits and Build Party Timeline

As a party coordinator,
I want to capture deposit commitments and generate a lifecycle timeline,
So that event execution is planned and financially tracked from booking time.

**Acceptance Criteria:**

**Given** a booking is ready for commitment
**When** deposit details are entered and confirmed
**Then** deposit obligation is recorded in booking financial state.

**Given** a booking is confirmed
**When** lifecycle generation runs
**Then** a party timeline is created with milestone statuses (booked, upcoming, active, completed).

**Given** timeline milestones exist
**When** coordinator views booking detail
**Then** milestone state and next actions are visible.

**Given** a confirmed booking exists
**When** the party timeline is retrieved or updated
**Then** the response is returned within 3 seconds (NFR4).

### Story 4.3: Manage Room Assignment and Conflict Resolution

As a party coordinator,
I want to assign rooms and resolve schedule conflicts,
So that party operations are feasible and double-booking is prevented.

**Acceptance Criteria:**

**Given** a booking has a target date/time
**When** room assignment is requested
**Then** only compatible available rooms are selectable.

**Given** a room/time conflict exists
**When** coordinator attempts assignment
**Then** conflict is blocked
**And** alternative slots/rooms are suggested.

**Given** room assignment changes after booking updates
**When** save is confirmed
**Then** impacted timeline tasks and booking status are recalculated.

### Story 4.4: Manage Catering/Decor Options with Risk Indicators

As a party coordinator,
I want to manage catering and decor options and see booking risks,
So that I can proactively resolve execution blockers.

**Acceptance Criteria:**

**Given** a booking includes configurable add-ons
**When** catering/decor selections are changed
**Then** booking totals and downstream requirements update correctly.

**Given** selected options create operational risk (inventory shortfall/policy conflict)
**When** booking is reviewed
**Then** risk indicators are surfaced with severity and reason.

**Given** risks are present
**When** coordinator applies an approved corrective option
**Then** risk state updates and booking remains actionable.

**Given** catering or decor options are changed on a booking
**When** the update is saved
**Then** revised booking totals and timeline are returned within 3 seconds (NFR4).

### Story 4.5: Reserve and Release Inventory by Booking Policy

As a manager/party coordinator,
I want inventory to be reserved at booking commitment and released by policy,
So that party fulfillment remains reliable and stock integrity is preserved.

**Acceptance Criteria:**

**Given** a booking is committed with inventory-linked items
**When** reservation is executed
**Then** required stock is reserved against the booking.

**Given** booking is changed/cancelled under release rules
**When** policy conditions are met
**Then** reserved inventory is released correctly.

**Given** inventory constraints are violated
**When** finalization is attempted
**Then** booking cannot finalize without approved resolution
**And** user receives actionable guidance.

**Given** substitution policies exist
**When** constrained inventory is encountered
**Then** allowed substitutes are shown based on policy and role permission.

### Story 4.6: Manager Substitution Policy Management

As a manager,
I want to create, edit, and delete substitution policy rules for inventory-constrained items,
So that cashiers and party coordinators have accurate, up-to-date substitute options available during transactions and bookings.

**Acceptance Criteria:**

**Given** I am an authenticated manager
**When** I navigate to the substitution policy management area
**Then** I can see all existing substitution policy rules with their source item, allowed substitute(s), and active status.

**Given** a constrained item requires a substitution rule
**When** I create a new policy specifying source item, allowed substitute, and applicable roles
**Then** the rule is persisted and immediately available to cashiers and coordinators for that item.

**Given** an existing substitution policy rule
**When** I edit the allowed substitute, applicable roles, or active status
**Then** changes are saved and become effective for subsequent transactions and bookings.

**Given** an existing substitution policy rule
**When** I delete it
**Then** the rule is removed
**And** the substitute is no longer offered at checkout or booking for the source item.

**Given** I attempt to create a rule referencing items that do not exist in inventory
**When** the form is submitted
**Then** a validation error is shown
**And** the rule is not persisted.

**Given** substitution policy changes are made
**When** any create, edit, or delete action is confirmed
**Then** the action is recorded in the audit log with actor identity, timestamp, and change summary (NFR13).

## Epic 5: Offline Continuity, Queueing, and Synchronization Integrity

Enable outage-proof operations with queued transactions, deterministic replay, sync transparency, and failover continuity.

### Story 5.1: Enable Offline Mode for Critical Frontline Workflows

As a cashier/party coordinator,
I want critical workflows to continue when internet is unavailable,
So that operations are not blocked by connectivity outages.

**Acceptance Criteria:**

**Given** internet connectivity is unavailable
**When** user executes critical workflows (admissions, mixed-cart capture, booking updates)
**Then** actions continue in offline mode
**And** user is clearly informed that offline mode is active.

**Given** offline mode is active
**When** user navigates supported workflows
**Then** unsupported network-only actions are clearly identified
**And** actionable fallback guidance is provided.

**Given** app restarts during outage
**When** user returns to workflow
**Then** offline mode state and pending operations are recovered.

### Story 5.2: Queue Offline-Captured Operational and Financial Actions

As the system,
I want to persist queued actions with operation metadata while offline,
So that actions can be replayed reliably after reconnect.

**Acceptance Criteria:**

**Given** a write action occurs in offline mode
**When** command is accepted
**Then** an outbox/queue record is appended with operation ID, timestamp UTC, actor, and payload snapshot.

**Given** multiple offline actions are captured
**When** queue is inspected
**Then** sequence order is preserved exactly as captured.

**Given** queue records are persisted
**When** app/device restarts
**Then** queue records remain durable and recoverable.

### Story 5.3: Implement Reconnect Synchronization and Deterministic Replay

As the system,
I want queued actions replayed deterministically after connectivity returns,
So that local and remote states converge safely.

**Acceptance Criteria:**

**Given** connectivity is restored
**When** sync worker starts
**Then** queued operations replay in stored order
**And** each operation uses idempotency keys/correlation IDs.

**Given** replay succeeds for an operation
**When** confirmation is received
**Then** operation status updates to synced
**And** local traceability links are retained.

**Given** replay fails for an operation
**When** error is classified as retryable
**Then** operation remains queued with retry state
**And** failure reason is stored with diagnostic code.

### Story 5.4: Prevent Duplicate Finalization and Preserve Traceability

As a manager/support user,
I want duplicate financial finalization prevented with full action traceability,
So that reconciliation remains accurate and auditable.

**Acceptance Criteria:**

**Given** an operation has already finalized remotely
**When** duplicate replay attempt occurs
**Then** duplicate finalization is prevented
**And** operation resolves safely without double-charge/double-commit.

**Given** any offline-captured operation
**When** it transitions through queued -> replayed -> resolved states
**Then** traceability records link local capture to final settlement outcome.

**Given** traceability data exists
**When** manager/support reviews history
**Then** they can retrieve operation lifecycle by operation ID and transaction context.

### Story 5.5: Expose Sync Health, Exceptions, and Tablet-Led Failover

As a cashier/manager,
I want transparent sync health and failover status,
So that I can continue operations confidently and act on unresolved exceptions.

**Acceptance Criteria:**

**Given** sync subsystem is active
**When** user views status indicators
**Then** queued/retrying/failed/synced counts are visible with clear state semantics.

**Given** unresolved sync exceptions exist
**When** manager/support opens exception view
**Then** each exception includes context, diagnostic code, and recommended next action.

**Given** Windows back-office is unavailable and tablet mode is active
**When** frontline operations continue
**Then** essential workflows remain operable
**And** failover mode is explicitly indicated.

## Epic 6: Financial Governance, Reconciliation, and Management Visibility

Enable managers and owners to govern financial operations, reconcile daily activity, and monitor business/operational health.

### Story 6.1: Build Immutable Financial Event Ledger

As a manager/owner,
I want financial and governance actions stored immutably,
So that reconciliation and audits are trustworthy.

**Acceptance Criteria:**

**Given** a financial/governance action occurs (settlement, refund, override)
**When** the action is committed
**Then** an immutable ledger record is appended with operation ID, actor, UTC timestamp, action type, and references.

**Given** a ledger record exists
**When** queried by authorized users
**Then** the record is retrievable
**And** cannot be edited or deleted through normal application paths.

**Given** system restart/recovery occurs
**When** ledger data is reloaded
**Then** historical integrity and ordering are preserved.

### Story 6.2: Provide Deferred-Payment Exception Review

As a manager/owner,
I want visibility into deferred-payment outcomes and unresolved exceptions,
So that I can resolve financial risk promptly.

**Acceptance Criteria:**

**Given** deferred payment operations exist
**When** manager opens exception review
**Then** operations are listed by state (queued/retrying/failed/resolved) with diagnostic details.

**Given** an exception is selected
**When** details are viewed
**Then** operation context, customer linkage, and recommended next action are available.

**Given** an exception transitions state
**When** resolution is recorded
**Then** status history is retained for audit traceability.

### Story 6.3: Implement End-of-Day Reconciliation Workflow

As a manager,
I want an end-of-day reconciliation process driven by system records,
So that daily financial close is accurate and repeatable.

**Acceptance Criteria:**

**Given** business day activity exists
**When** reconciliation workflow starts
**Then** expected totals are computed from immutable transaction records.

**Given** mismatches are detected
**When** reconciliation view is generated
**Then** discrepancies are highlighted with drill-down references.

**Given** reconciliation is completed
**When** manager confirms close
**Then** reconciliation outcome is stored with actor/time and summary metrics.

### Story 6.4: Deliver Manager Operational and Risk Dashboards

As a manager/owner,
I want dashboards for throughput, party operations, sync health, and risk indicators,
So that I can make informed operational decisions.

**Acceptance Criteria:**

**Given** operational data is available
**When** dashboard loads
**Then** frontline throughput, party status, and exception metrics are shown.

**Given** inventory/booking risk conditions exist
**When** dashboard is viewed
**Then** risk summaries are surfaced with severity and linked context.

**Given** sync backlog/health changes
**When** dashboard refreshes
**Then** sync health indicators update consistently and clearly.

### Story 6.5: Implement CSV Export and Per-Location Performance Views

As an owner/admin,
I want stable CSV exports and per-location performance/cost visibility,
So that external processing and subscription oversight are reliable.

**Acceptance Criteria:**

**Given** export is requested
**When** CSV is generated
**Then** schema uses stable identifiers and consistent field ordering.

**Given** exported records are consumed externally
**When** reconciliation run occurs
**Then** records can be matched deterministically to source operation IDs.

**Given** performance/cost data exists
**When** owner/admin opens oversight view
**Then** per-location KPIs and cost metrics are available in a comparable format.

## Epic 7: Support Diagnostics and Operational Recovery

Enable support and operations users to diagnose incidents, run controlled recovery workflows, and maintain integration reliability.

### Story 7.1: Build Incident Investigation Workspace

As a support/operations user,
I want to investigate incidents by transaction, device, and error context,
So that root causes can be identified quickly.

**Acceptance Criteria:**

**Given** incident data exists
**When** user filters by transaction ID, device ID, and diagnostic code
**Then** matching incident records are returned with correlation references.

**Given** an incident record is selected
**When** details are opened
**Then** timeline of related operations (capture/replay/settlement) is visible.

**Given** filters are changed
**When** query is re-executed
**Then** results update predictably
**And** maintain consistent sort/order behavior.

### Story 7.2: Implement Controlled Retry Workflow

As a support/operations user,
I want to trigger controlled retries for failed deferred actions,
So that recoverable failures can be resolved safely.

**Acceptance Criteria:**

**Given** a failed deferred action is marked retryable
**When** support triggers retry
**Then** action is re-queued with retained operation identity and retry metadata.

**Given** retry is in progress
**When** status is updated
**Then** current retry state is visible with latest outcome details.

**Given** non-retryable failure classification
**When** retry is requested
**Then** retry is blocked
**And** user gets actionable guidance for alternative resolution.

### Story 7.3: Implement Approved Rollback and Configuration Correction

As a support/operations user with required permission,
I want to execute controlled rollback/config correction actions,
So that systemic incidents can be mitigated without unsafe changes.

**Acceptance Criteria:**

**Given** rollback/config action requires approval
**When** request lacks approval context
**Then** execution is blocked
**And** required approval/justification is enforced.

**Given** approved correction action
**When** execution is performed
**Then** action outcome is recorded with actor, reason, operation ID, and UTC timestamp.

**Given** rollback/correction affects active workflows
**When** action completes
**Then** impacted operations are flagged for follow-up verification.

### Story 7.4: Expose Integration Health Status

As a support/operations user,
I want integration health visibility for payment, hardware, and export pipelines,
So that reliability issues can be detected and prioritized.

**Acceptance Criteria:**

**Given** integration adapters are active
**When** health view is requested
**Then** current health state is shown per integration (payment, device, export).

**Given** degradation/failure is detected
**When** health state changes
**Then** the issue is surfaced with severity, component, and diagnostic context.

**Given** health recovers
**When** status updates
**Then** transition is recorded with timestamp and correlated incident reference.

### Story 7.5: Retain Diagnostic Evidence for Post-Incident Analysis

As a support/operations user,
I want diagnostic evidence retained and queryable,
So that post-incident review and prevention actions are possible.

**Acceptance Criteria:**

**Given** operational errors and exceptions occur
**When** evidence is captured
**Then** logs/events include operation IDs, correlation IDs, and contextual metadata.

**Given** evidence retention policy is configured
**When** data lifecycle processes run
**Then** required evidence remains available through retention window.

**Given** post-incident analysis is initiated
**When** support queries evidence store
**Then** incident-linked records are retrievable and exportable for review.

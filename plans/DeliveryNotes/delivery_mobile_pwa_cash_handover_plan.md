# Delivery Mobile PWA And Cash Handover Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a lightweight mobile-first PWA for delivery staff to confirm delivery offline, collect proof images and location, record cash collected from the customer, and let the cashier confirm the cash handover before customer payment is recorded.

**Architecture:** Extend the existing `DeliveryNote` workflow for assignment and mobile delivery metadata, then add a small `DeliveryCashHandover` aggregate for internal cash control. Keep customer accounting in the existing debts/payment flow and record `CustomerPayment` only after cashier confirmation.

**Tech Stack:** ASP.NET Core MVC/Razor, MediatR, Clean Architecture/DDD projects, EF Core SQL Server mappings/migrations, Bootstrap-based admin UI, mobile PWA with Service Worker and IndexedDB.

---

## Current Context
- `DeliveryNote` already has `Draft`, `Confirmed`, `Delivering`, `Delivered`, `Cancelled`.
- `DeliveryNote` already stores `AmountToCollect`, delivery proof picture id, receiver name, and delivered time.
- `DeliveryNoteCreatedHandler` currently releases order-level reservation and reserves warehouse stock when the delivery note is created.
- `DeliveryNoteConfirmedHandler` currently dispatches stock and records outbound cost. For the improved warehouse workflow this is too early; stock should leave inventory when warehouse physically hands goods to the driver.
- `DeliveryNoteDeliveredHandler` already creates `CustomerDebt` when `AmountToCollect > 0`.
- Current `_ImageUploader` pre-uploads images through `/Picture/Upload`, so it cannot support offline proof capture.
- `CustomerPayment` is a customer accounting record. It should not be created when the driver is still holding cash.
- `UserRole` already allows one user to have multiple roles. The new flow must assign work to users, not assume one person has only one role.

## Success Criteria
- Warehouse/admin can assign one or more `Confirmed` delivery notes to a delivery user.
- Warehouse cannot mark goods as handed to the driver until the driver PWA has cached the assigned notes or a paper handover manifest has been issued.
- Stock is not dispatched at warehouse preparation time. Stock is dispatched when goods are physically handed to the driver / moved to `Delivering`.
- Delivery user can open a dedicated mobile UI, cache assigned delivery notes, and work without network after the notes are cached. If the driver forgot to cache, the paper manifest is the operational fallback.
- Delivery user can confirm delivered with receiver name, accepted/rejected quantities, proof photos, GPS coordinates, actual cash collected, and note.
- Offline confirmation is queued locally and syncs when network returns.
- Sync is idempotent. Retrying the same offline submission cannot double-deliver, double-upload as a business action, or double-create cash handover.
- If cash is collected, the system creates a pending cash handover record. The cashier must confirm it before `CustomerPayment` is recorded and applied to the delivery debt.
- Existing admin delivery note flow still works.

## Assumptions
- Phase 1 is a Web PWA under the existing `NamEcommerce.Web` project, not a native app.
- Delivery users authenticate with the existing staff login.
- Phase 0 adds minimal role-based staff classification using existing `User`, `Role`, and `UserRole`: `Admin`, `WarehouseManager`, `DeliveryStaff`, `Cashier`. It does not build a full permission matrix.
- The warehouse handover step must produce either a PWA cache acknowledgement or a printed/paper manifest acknowledgement before the goods leave the store.
- Offline support covers confirming delivery and storing proof locally. Creating new delivery notes, editing order contents, and route optimization are out of scope.
- Cash handover applies to cash collected by the driver. Bank transfer reconciliation stays in the existing debt/bank transfer flow.

## Recommended Architecture

Keep `DeliveryNote` as the core fulfillment aggregate and add a small cash handover aggregate:

```text
Admin/Warehouse
  -> assign DeliveryNote to user
  -> verify goods prepared
  -> require PWA cache acknowledgement or printed manifest
  -> mark Delivering and dispatch stock

Driver PWA
  -> cache assigned notes before departure
  -> offline capture proof/location/cash
  -> sync confirmation when online

Server sync
  -> upload proof pictures
  -> MarkDeliveryNoteDelivered
  -> create CustomerDebt through existing DeliveryNoteDelivered event
  -> create pending DeliveryCashHandover if cashCollectedAmount > 0

Cashier
  -> confirm DeliveryCashHandover
  -> record CustomerPayment through existing CustomerDebtManager
```

Do not record `CustomerPayment` at driver delivery time. At that moment the customer has paid the driver, but the store cash box has not received it yet.

## Revised Operational Workflow

### Delivery note status semantics
- `Draft`: created by admin/sales and waiting for warehouse work. Warehouse stock may be reserved, but no outbound stock movement is recorded.
- `Confirmed`: warehouse has checked/prepared goods and the delivery note is ready for driver assignment/handover. No stock is dispatched at this point in the improved workflow.
- `Delivering`: warehouse has physically handed the goods to the driver. This is the point where stock dispatch and outbound costing should happen.
- `Delivered`: customer received the goods. Proof, location, collected cash, customer debt, and cash handover are recorded.
- `Cancelled`: delivery note is cancelled. Cancellation must release reservation or compensate inventory depending on how far the note progressed.

### Driver-forgot-to-open-app fallback
The system cannot magically know delivery details offline if the driver never downloaded them. The process must prevent that situation before departure:

1. Warehouse creates a delivery run/manifest for one driver.
2. Driver opens the PWA while still at the store and taps `Nhan chuyen`.
3. PWA downloads all assigned notes, stores them in IndexedDB, then posts a cache acknowledgement to the server.
4. Warehouse sees `Da cache tren may nguoi giao` and can hand over goods.

If the driver does not open the PWA, warehouse must print or issue a paper handover manifest before goods leave. The manifest contains:
- delivery note code
- customer name and phone
- shipping address
- item summary
- amount to collect
- a QR code or short code for later lookup

With paper fallback, the driver can still deliver without network. When they return or regain network, delivery confirmation can be entered from the paper manifest by the driver, warehouse manager, or admin. This is less ideal than PWA capture because photos/GPS may be missing or late, but it prevents the "driver does not know what to deliver" failure.

### Inventory timing change
Move normal customer delivery stock dispatch from `DeliveryNoteConfirmedHandler` to a new `DeliveryNoteDeliveringHandler` or equivalent manager step triggered by `MarkDeliveringAsync`.

Keep this distinction:
- `DeliveryNoteCreated`: reserve warehouse stock.
- `DeliveryNoteConfirmed`: warehouse preparation confirmed; no dispatch.
- `DeliveryNoteDelivering`: dispatch stock and register outbound cost.
- `DeliveryNoteDelivered`: customer receipt, debt creation, cash handover creation.

Direct-ship and vendor-return flows need explicit regression tests because their stock timing differs from normal customer delivery.

## Minimal Staff Role Plan

Before PWA work, add a small role layer using existing entities:
- `Admin`
- `WarehouseManager`
- `DeliveryStaff`
- `Cashier`

Rules:
- Admin can see and override all delivery workflows.
- Warehouse manager can prepare delivery notes, create delivery runs/manifests, assign drivers, and hand goods to drivers.
- Delivery staff can see only assigned delivery runs/notes in the PWA.
- Cashier can confirm or reject cash handovers.

This is intentionally role-based, not full permission-based. `Permission` and `RolePermission` can remain unused until the project needs finer control.

## Delivery Run And Manifest Plan

Add a small `DeliveryRun` concept to support multiple delivery notes per driver and to make the pre-departure cache/paper check auditable.

`DeliveryRun` responsibilities:
- group multiple delivery notes for one driver
- store assigned driver
- record warehouse handover status
- record whether the driver PWA cached the run
- record whether a paper manifest was issued
- provide a printable manifest

Suggested statuses:
- `Planning = 10`
- `ReadyForHandover = 20`
- `HandedToDriver = 30`
- `Closed = 40`
- `Cancelled = 50`

The run can be simple: one aggregate with child note references. It does not need route optimization in phase 1.

## Data Model Plan

### Add `DeliveryRun`
Create:
- `NamEcommerce/Domain/NamEcommerce.Domain/Entities/DeliveryNotes/DeliveryRun.cs`
- `NamEcommerce/Domain/NamEcommerce.Domain/Entities/DeliveryNotes/DeliveryRunItem.cs`
- `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Enums/DeliveryNotes/DeliveryRunStatus.cs`
- `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Dtos/DeliveryNotes/DeliveryRunDtos.cs`
- `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Services/DeliveryNotes/IDeliveryRunManager.cs`
- `NamEcommerce/Domain/NamEcommerce.Domain.Services/DeliveryNotes/DeliveryRunManager.cs`
- `NamEcommerce/Application/NamEcommerce.Application.Contracts/DeliveryNotes/IDeliveryRunAppService.cs`
- `NamEcommerce/Application/NamEcommerce.Application.Services/DeliveryNotes/DeliveryRunAppService.cs`
- `NamEcommerce/Infrastructure/NamEcommerce.Data.SqlServer/Mappings/DeliveryRunMap.cs`

Fields:
- `Id`
- `Code`
- `AssignedDeliveryUserId`
- `AssignedDeliveryUsername`
- `AssignedDeliveryFullName`
- `Status`
- `PreparedByUserId`
- `PreparedOnUtc`
- `HandedOverByUserId`
- `HandedOverOnUtc`
- `DriverCachedOnUtc`
- `DriverCacheDeviceId`
- `PaperManifestIssued`
- `PaperManifestIssuedOnUtc`
- `Note`
- `CreatedOnUtc`
- `UpdatedOnUtc`
- `Items`

`DeliveryRunItem` fields:
- `Id`
- `DeliveryRunId`
- `DeliveryNoteId`
- `DeliveryNoteCode`
- `OrderCode`
- `CustomerName`
- `ShippingAddress`
- `AmountToCollect`

Rules:
- A `DeliveryNote` can belong to at most one active run.
- `HandedToDriver` requires either `DriverCachedOnUtc` or `PaperManifestIssued = true`.
- Moving a run to `HandedToDriver` marks its notes `Delivering`.
- Closing a run requires all non-cancelled notes to be `Delivered` or manually closed by admin.

### Extend `DeliveryNote`
Modify:
- `NamEcommerce/Domain/NamEcommerce.Domain/Entities/DeliveryNotes/DeliveryNote.cs`
- `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Dtos/DeliveryNotes/DeliveryNoteDtos.cs`
- `NamEcommerce/Application/NamEcommerce.Application.Contracts/Dtos/DeliveryNotes/DeliveryNoteAppDtos.cs`
- `NamEcommerce/Application/NamEcommerce.Application.Services/Extensions/DeliveryNoteExtensions.cs`
- `NamEcommerce/Infrastructure/NamEcommerce.Data.SqlServer/Mappings/DeliveryNoteMap.cs`

Add fields:
- `DeliveryRunId`
- `DeliveryRunCode`
- `AssignedDeliveryUserId`
- `AssignedDeliveryUsername`
- `AssignedDeliveryFullName`
- `AssignedDeliveryOnUtc`
- `DeliveredByUserId`
- `DeliveredByUsername`
- `DeliveryLatitude`
- `DeliveryLongitude`
- `DeliveryLocationAccuracyMeters`
- `DeliveryLocationCapturedOnUtc`
- `DeliveryConfirmationClientId`
- `CashCollectedAmount`
- `CashCollectedOnUtc`
- `DeliveryNoteDriverNote`

Add domain methods:
- `AssignDeliveryUser(Guid userId, string username, string fullName, DateTime assignedOnUtc)`
- `MarkDeliveredFromMobile(...)`

Keep existing `MarkDelivered(...)` for current admin modal, but route mobile sync through a new method so location, cash, and idempotency are explicit.

### Add `DeliveryCashHandover`
Create:
- `NamEcommerce/Domain/NamEcommerce.Domain/Entities/DeliveryNotes/DeliveryCashHandover.cs`
- `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Enums/DeliveryNotes/DeliveryCashHandoverStatus.cs`
- `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Dtos/DeliveryNotes/DeliveryCashHandoverDtos.cs`
- `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Services/DeliveryNotes/IDeliveryCashHandoverManager.cs`
- `NamEcommerce/Domain/NamEcommerce.Domain.Services/DeliveryNotes/DeliveryCashHandoverManager.cs`
- `NamEcommerce/Application/NamEcommerce.Application.Contracts/DeliveryNotes/IDeliveryCashHandoverAppService.cs`
- `NamEcommerce/Application/NamEcommerce.Application.Services/DeliveryNotes/DeliveryCashHandoverAppService.cs`
- `NamEcommerce/Infrastructure/NamEcommerce.Data.SqlServer/Mappings/DeliveryCashHandoverMap.cs`

Fields:
- `Id`
- `DeliveryNoteId`
- `DeliveryNoteCode`
- `OrderId`
- `OrderCode`
- `CustomerId`
- `CustomerName`
- `Amount`
- `CollectedByUserId`
- `CollectedByUsername`
- `CollectedAtUtc`
- `ClientSubmissionId`
- `Status`
- `ConfirmedByUserId`
- `ConfirmedAtUtc`
- `RejectedByUserId`
- `RejectedAtUtc`
- `RejectReason`
- `CustomerPaymentId`
- `Note`
- `CreatedOnUtc`
- `UpdatedOnUtc`

Statuses:
- `PendingCashierConfirmation = 10`
- `Confirmed = 20`
- `Rejected = 30`
- `Cancelled = 40`

Rules:
- A delivery note can have at most one active handover.
- Confirming a handover creates exactly one `CustomerPayment`.
- Rejecting a handover does not modify `CustomerDebt`; it leaves the delivery as delivered and flags the internal cash issue.
- Re-submitting the same `ClientSubmissionId` returns the existing handover.

## API And Web Plan

### Admin/Warehouse Delivery Preparation And Run Handover
Modify:
- `NamEcommerce/Presentation/NamEcommerce.Web/Controllers/DeliveryNoteController.cs`
- `NamEcommerce/Presentation/NamEcommerce.Web/Views/DeliveryNote/List.cshtml`
- `NamEcommerce/Presentation/NamEcommerce.Web/Views/DeliveryNote/Details.cshtml`
- `NamEcommerce/Presentation/NamEcommerce.Web/Services/DeliveryNotes/DeliveryNoteModelFactory.cs`
- `NamEcommerce/Presentation/NamEcommerce.Web.Contracts/Commands/Models/DeliveryNotes/AssignDeliveryUserCommand.cs`
- `NamEcommerce/Presentation/NamEcommerce.Web.Framework/Commands/Handlers/DeliveryNotes/AssignDeliveryUserHandler.cs`
Create:
- `NamEcommerce/Presentation/NamEcommerce.Web/Controllers/DeliveryRunController.cs`
- `NamEcommerce/Presentation/NamEcommerce.Web/Views/DeliveryRun/List.cshtml`
- `NamEcommerce/Presentation/NamEcommerce.Web/Views/DeliveryRun/Details.cshtml`
- `NamEcommerce/Presentation/NamEcommerce.Web/Views/DeliveryRun/PrintManifest.cshtml`
- `NamEcommerce/Presentation/NamEcommerce.Web.Contracts/Commands/Models/DeliveryNotes/CreateDeliveryRunCommand.cs`
- `NamEcommerce/Presentation/NamEcommerce.Web.Contracts/Commands/Models/DeliveryNotes/ConfirmDeliveryRunCachedCommand.cs`
- `NamEcommerce/Presentation/NamEcommerce.Web.Contracts/Commands/Models/DeliveryNotes/IssueDeliveryRunPaperManifestCommand.cs`
- `NamEcommerce/Presentation/NamEcommerce.Web.Contracts/Commands/Models/DeliveryNotes/HandOverDeliveryRunCommand.cs`
- `NamEcommerce/Presentation/NamEcommerce.Web.Framework/Commands/Handlers/DeliveryNotes/CreateDeliveryRunHandler.cs`
- `NamEcommerce/Presentation/NamEcommerce.Web.Framework/Commands/Handlers/DeliveryNotes/ConfirmDeliveryRunCachedHandler.cs`
- `NamEcommerce/Presentation/NamEcommerce.Web.Framework/Commands/Handlers/DeliveryNotes/IssueDeliveryRunPaperManifestHandler.cs`
- `NamEcommerce/Presentation/NamEcommerce.Web.Framework/Commands/Handlers/DeliveryNotes/HandOverDeliveryRunHandler.cs`

Add:
- Create delivery run from one or more prepared delivery notes.
- Assign one driver to the run.
- Show run/assigned driver on delivery note list/details.
- Print manifest from the run.
- Receive PWA cache acknowledgement from the driver.
- Allow handover only when the run has driver cache acknowledgement or paper manifest issued.
- Handover marks the notes `Delivering` and dispatches stock.

### Driver PWA Shell
Create:
- `NamEcommerce/Presentation/NamEcommerce.Web/Controllers/DeliveryMobileController.cs`
- `NamEcommerce/Presentation/NamEcommerce.Web/Views/DeliveryMobile/Index.cshtml`
- `NamEcommerce/Presentation/NamEcommerce.Web/Views/DeliveryMobile/_Layout.cshtml`
- `NamEcommerce/Presentation/NamEcommerce.Web/Models/DeliveryMobile/DeliveryMobileModels.cs`
- `NamEcommerce/Presentation/NamEcommerce.Web/Services/DeliveryMobile/IDeliveryMobileModelFactory.cs`
- `NamEcommerce/Presentation/NamEcommerce.Web/Services/DeliveryMobile/DeliveryMobileModelFactory.cs`

Routes:
- `GET /DeliveryMobile`
- `GET /DeliveryMobile/Assigned`
- `GET /DeliveryMobile/Run/{id}`
- `POST /DeliveryMobile/AcknowledgeRunCached`
- `POST /DeliveryMobile/SyncDelivered`
- `GET /DeliveryMobile/CashHandoverStatus`

`GET /DeliveryMobile/Assigned` returns only notes assigned to the current user and not yet delivered/cancelled:
- note id, code, status
- customer name, phone, shipping address
- order code
- amount to collect
- items and quantities
- existing returned/pending info if needed
- last server updated timestamp

`POST /DeliveryMobile/AcknowledgeRunCached` tells the server that the phone has saved the run payload into IndexedDB. Warehouse uses this acknowledgement before releasing goods.

`POST /DeliveryMobile/SyncDelivered` accepts `multipart/form-data`:
- `payloadJson`
- `proofImages`

Payload shape:

```json
{
  "clientSubmissionId": "device-generated-guid",
  "deliveryNoteId": "guid",
  "confirmedAtUtc": "2026-06-07T03:30:00Z",
  "receiverName": "Nguyen Van A",
  "driverNote": "Khach nhan du hang",
  "cashCollectedAmount": 1200000,
  "latitude": 10.762622,
  "longitude": 106.660172,
  "locationAccuracyMeters": 18,
  "locationCapturedOnUtc": "2026-06-07T03:29:40Z",
  "acceptedItems": [
    {
      "deliveryNoteItemId": "guid",
      "acceptedQuantity": 2,
      "rejectedQuantity": 0,
      "rejectReason": null
    }
  ]
}
```

Server behavior:
- Validate current user is assigned to the delivery note, unless the current user is admin/warehouse and this override is intentionally added later.
- If `DeliveryConfirmationClientId` already equals `clientSubmissionId`, return success.
- If delivery note is already delivered with a different client id, return conflict.
- Store proof images using existing picture manager/service logic.
- Call mobile delivery completion command.
- Create pending `DeliveryCashHandover` when `cashCollectedAmount > 0`.

### Cashier Handover UI
Create:
- `NamEcommerce/Presentation/NamEcommerce.Web/Controllers/DeliveryCashHandoverController.cs`
- `NamEcommerce/Presentation/NamEcommerce.Web/Views/DeliveryCashHandover/List.cshtml`
- `NamEcommerce/Presentation/NamEcommerce.Web/Views/DeliveryCashHandover/Details.cshtml`
- `NamEcommerce/Presentation/NamEcommerce.Web.Contracts/Commands/Models/DeliveryNotes/ConfirmDeliveryCashHandoverCommand.cs`
- `NamEcommerce/Presentation/NamEcommerce.Web.Contracts/Commands/Models/DeliveryNotes/RejectDeliveryCashHandoverCommand.cs`
- `NamEcommerce/Presentation/NamEcommerce.Web.Framework/Commands/Handlers/DeliveryNotes/ConfirmDeliveryCashHandoverHandler.cs`
- `NamEcommerce/Presentation/NamEcommerce.Web.Framework/Commands/Handlers/DeliveryNotes/RejectDeliveryCashHandoverHandler.cs`

Behavior:
- List pending handovers grouped by driver.
- Cashier confirms actual amount received.
- If amount matches, call `CustomerDebtManager.RecordPaymentAsync` with:
  - `CustomerId`
  - `OrderId`
  - `DeliveryNoteId`
  - `Amount`
  - `PaymentMethod = Cash`
  - `PaymentType = DebtPayment`
  - `PaidOnUtc = ConfirmedAtUtc`
  - `RecordedByUserId = cashier user id`
- If amount does not match, reject with reason. Later adjustment can be a separate feature.

## PWA Offline Plan

Create:
- `NamEcommerce/Presentation/NamEcommerce.Web/wwwroot/manifest-delivery-mobile.webmanifest`
- `NamEcommerce/Presentation/NamEcommerce.Web/wwwroot/delivery-mobile-sw.js`
- `NamEcommerce/Presentation/NamEcommerce.Web/wwwroot/modules/delivery-mobile/app.js`
- `NamEcommerce/Presentation/NamEcommerce.Web/wwwroot/modules/delivery-mobile/api.js`
- `NamEcommerce/Presentation/NamEcommerce.Web/wwwroot/modules/delivery-mobile/offline-store.js`
- `NamEcommerce/Presentation/NamEcommerce.Web/wwwroot/modules/delivery-mobile/sync.js`
- `NamEcommerce/Presentation/NamEcommerce.Web/wwwroot/modules/delivery-mobile/camera.js`
- `NamEcommerce/Presentation/NamEcommerce.Web/wwwroot/modules/delivery-mobile/location.js`
- `NamEcommerce/Presentation/NamEcommerce.Web/wwwroot/css/delivery-mobile.css`

IndexedDB stores:
- `deliveryRuns`: cached run payloads
- `assignedNotes`: server delivery note payloads
- `pendingConfirmations`: offline submissions with payload and image blobs
- `syncLog`: submission id, status, last error, attempt count

Service worker:
- Cache the PWA shell, CSS, JS, manifest, and app icons.
- Use network-first for `/DeliveryMobile/Assigned`.
- Use cache-first for static assets.
- Do not try to background-sync silently as the only sync mechanism. Browsers differ, especially mobile Safari. Always show a visible "Dong bo" action and auto-attempt when online.

Driver UI states:
- Online/offline badge.
- Current run cache status: `Chua tai`, `Da tai vao may`, `Dung phieu giay`.
- "Can dong bo" badge with count of queued confirmations.
- Assigned deliveries list.
- Delivery detail.
- Confirm delivery form.
- Sync progress/error screen.

Mobile UX rules:
- Big tap targets.
- No admin sidebar.
- Minimal cards and dense delivery data.
- Use existing design colors and Bootstrap icons.
- Avoid decorative UI; drivers need fast scanning.

## Implementation Tasks

### Task 0: Minimal staff roles
Files:
- Modify `NamEcommerce/Domain/NamEcommerce.Domain.Services/Users/UserManager.cs`
- Modify `NamEcommerce/Application/NamEcommerce.Application.Contracts/Users/IUserAppService.cs`
- Modify `NamEcommerce/Application/NamEcommerce.Application.Services/Users/UserAppService.cs`
- Create or modify web models/views/controllers for user role assignment, following existing user patterns.
- Test `NamEcommerce/Tests/NamEcommerce.Application.Services.Test/Users/UserAppServiceTests.cs`

Steps:
- [ ] Seed or ensure roles: `Admin`, `WarehouseManager`, `DeliveryStaff`, `Cashier`.
- [ ] Add app-service method to get users by role.
- [ ] Add admin-facing way to assign roles to a user.
- [ ] Use role checks only for workflow routing; do not build full `Permission`/`RolePermission` enforcement in this phase.
- [ ] Verify: `dotnet test NamEcommerce/Tests/NamEcommerce.Application.Services.Test --filter "FullyQualifiedName~User"`

### Task 1: Correct delivery stock timing
Files:
- Modify `NamEcommerce/Application/NamEcommerce.Application.Services/Events/DeliveryNotes/DeliveryNoteConfirmedHandler.cs`
- Create `NamEcommerce/Application/NamEcommerce.Application.Services/Events/DeliveryNotes/DeliveryNoteDeliveringHandler.cs`
- Modify `NamEcommerce/Domain/NamEcommerce.Domain.Services/DeliveryNotes/DeliveryNoteManager.cs`
- Test `NamEcommerce/Tests/NamEcommerce.Domain.Services.Test/DeliveryNotes/DeliveryNoteManagerTests.cs`
- Test `NamEcommerce/Tests/NamEcommerce.Application.Services.Test/DeliveryNotes/DeliveryNoteStockTimingTests.cs`

Steps:
- [ ] Change `DeliveryNoteConfirmedHandler` so normal customer delivery confirmation no longer dispatches stock or registers outbound cost.
- [ ] Add a `DeliveryNoteDeliveringHandler` that dispatches stock and registers outbound cost when goods are handed to the driver.
- [ ] Keep direct-ship and vendor-return stock behavior covered by regression tests.
- [ ] Ensure cancellation releases reservation for notes not yet handed to driver.
- [ ] Verify: `dotnet test NamEcommerce/Tests/NamEcommerce.Application.Services.Test --filter "FullyQualifiedName~DeliveryNoteStockTiming"`

### Task 2: Delivery run and manifest domain
Files:
- Create `NamEcommerce/Domain/NamEcommerce.Domain/Entities/DeliveryNotes/DeliveryRun.cs`
- Create `NamEcommerce/Domain/NamEcommerce.Domain/Entities/DeliveryNotes/DeliveryRunItem.cs`
- Create `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Enums/DeliveryNotes/DeliveryRunStatus.cs`
- Create `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Dtos/DeliveryNotes/DeliveryRunDtos.cs`
- Create `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Services/DeliveryNotes/IDeliveryRunManager.cs`
- Create `NamEcommerce/Domain/NamEcommerce.Domain.Services/DeliveryNotes/DeliveryRunManager.cs`
- Create `NamEcommerce/Application/NamEcommerce.Application.Contracts/DeliveryNotes/IDeliveryRunAppService.cs`
- Create `NamEcommerce/Application/NamEcommerce.Application.Services/DeliveryNotes/DeliveryRunAppService.cs`
- Create `NamEcommerce/Application/NamEcommerce.Application.Services/Extensions/DeliveryRunExtensions.cs`
- Create `NamEcommerce/Infrastructure/NamEcommerce.Data.SqlServer/Mappings/DeliveryRunMap.cs`
- Test `NamEcommerce/Tests/NamEcommerce.Domain.Services.Test/DeliveryNotes/DeliveryRunManagerTests.cs`
- Test `NamEcommerce/Tests/NamEcommerce.Application.Services.Test/DeliveryNotes/DeliveryRunAppServiceTests.cs`

Steps:
- [ ] Create run from prepared delivery notes and one delivery user.
- [ ] Reject adding a delivery note already in another active run.
- [ ] Record PWA cache acknowledgement.
- [ ] Record paper manifest issued.
- [ ] Block handover if neither PWA cache acknowledgement nor paper manifest exists.
- [ ] Handover run and mark child notes `Delivering`.
- [ ] Verify:
  - `dotnet test NamEcommerce/Tests/NamEcommerce.Domain.Services.Test --filter "FullyQualifiedName~DeliveryRun"`
  - `dotnet test NamEcommerce/Tests/NamEcommerce.Application.Services.Test --filter "FullyQualifiedName~DeliveryRun"`

### Task 3: Delivery assignment and mobile metadata fields
Files:
- Modify `NamEcommerce/Domain/NamEcommerce.Domain/Entities/DeliveryNotes/DeliveryNote.cs`
- Modify `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Dtos/DeliveryNotes/DeliveryNoteDtos.cs`
- Modify `NamEcommerce/Application/NamEcommerce.Application.Contracts/Dtos/DeliveryNotes/DeliveryNoteAppDtos.cs`
- Modify `NamEcommerce/Application/NamEcommerce.Application.Services/Extensions/DeliveryNoteExtensions.cs`
- Modify `NamEcommerce/Infrastructure/NamEcommerce.Data.SqlServer/Mappings/DeliveryNoteMap.cs`
- Test `NamEcommerce/Tests/NamEcommerce.Domain.Services.Test/DeliveryNotes/DeliveryNoteManagerTests.cs`

Steps:
- [ ] Add assignment and mobile completion fields.
- [ ] Add domain method `AssignDeliveryUser`.
- [ ] Add domain method for mobile delivery completion with client id, location, and cash amount.
- [ ] Map fields through domain/app DTOs.
- [ ] Add EF mapping.
- [ ] Verify: `dotnet test NamEcommerce/Tests/NamEcommerce.Domain.Services.Test --filter "FullyQualifiedName~DeliveryNote"`

### Task 4: Delivery run admin UI
Files:
- Modify `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Services/DeliveryNotes/IDeliveryNoteManager.cs`
- Modify `NamEcommerce/Domain/NamEcommerce.Domain.Services/DeliveryNotes/DeliveryNoteManager.cs`
- Modify `NamEcommerce/Application/NamEcommerce.Application.Contracts/DeliveryNotes/IDeliveryNoteAppService.cs`
- Modify `NamEcommerce/Application/NamEcommerce.Application.Services/DeliveryNotes/DeliveryNoteAppService.cs`
- Create `NamEcommerce/Presentation/NamEcommerce.Web.Contracts/Commands/Models/DeliveryNotes/AssignDeliveryUserCommand.cs`
- Create `NamEcommerce/Presentation/NamEcommerce.Web.Framework/Commands/Handlers/DeliveryNotes/AssignDeliveryUserHandler.cs`
- Modify `NamEcommerce/Presentation/NamEcommerce.Web/Controllers/DeliveryNoteController.cs`
- Modify `NamEcommerce/Presentation/NamEcommerce.Web/Views/DeliveryNote/List.cshtml`
- Modify `NamEcommerce/Presentation/NamEcommerce.Web/Views/DeliveryNote/Details.cshtml`

Steps:
- [ ] Add delivery-run create/cache/manifest/handover commands and handlers.
- [ ] Add run list/detail/print-manifest pages.
- [ ] Add UI to build a run from prepared delivery notes.
- [ ] Add UI to show whether the driver's PWA cached the run.
- [ ] Add paper manifest button as explicit fallback.
- [ ] Add handover button that is disabled until cache acknowledgement or paper manifest exists.
- [ ] Show run/assigned driver on delivery note list and details.
- [ ] Verify: `dotnet build NamEcommerce.sln`

### Task 5: Cash handover domain and application service
Files:
- Create `NamEcommerce/Domain/NamEcommerce.Domain/Entities/DeliveryNotes/DeliveryCashHandover.cs`
- Create `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Enums/DeliveryNotes/DeliveryCashHandoverStatus.cs`
- Create `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Dtos/DeliveryNotes/DeliveryCashHandoverDtos.cs`
- Create `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Services/DeliveryNotes/IDeliveryCashHandoverManager.cs`
- Create `NamEcommerce/Domain/NamEcommerce.Domain.Services/DeliveryNotes/DeliveryCashHandoverManager.cs`
- Create `NamEcommerce/Application/NamEcommerce.Application.Contracts/DeliveryNotes/IDeliveryCashHandoverAppService.cs`
- Create `NamEcommerce/Application/NamEcommerce.Application.Services/DeliveryNotes/DeliveryCashHandoverAppService.cs`
- Create `NamEcommerce/Application/NamEcommerce.Application.Services/Extensions/DeliveryCashHandoverExtensions.cs`
- Create `NamEcommerce/Infrastructure/NamEcommerce.Data.SqlServer/Mappings/DeliveryCashHandoverMap.cs`
- Test `NamEcommerce/Tests/NamEcommerce.Domain.Services.Test/DeliveryNotes/DeliveryCashHandoverManagerTests.cs`
- Test `NamEcommerce/Tests/NamEcommerce.Application.Services.Test/DeliveryNotes/DeliveryCashHandoverAppServiceTests.cs`

Steps:
- [ ] Create pending handover.
- [ ] Confirm handover and record customer payment.
- [ ] Reject handover with reason.
- [ ] Enforce one active handover per delivery note.
- [ ] Verify:
  - `dotnet test NamEcommerce/Tests/NamEcommerce.Domain.Services.Test --filter "FullyQualifiedName~DeliveryCashHandover"`
  - `dotnet test NamEcommerce/Tests/NamEcommerce.Application.Services.Test --filter "FullyQualifiedName~DeliveryCashHandover"`

### Task 6: Mobile delivery sync command
Files:
- Create `NamEcommerce/Application/NamEcommerce.Application.Contracts/Dtos/DeliveryNotes/CompleteDeliveryFromMobileAppDto.cs`
- Modify `NamEcommerce/Application/NamEcommerce.Application.Contracts/DeliveryNotes/IDeliveryNoteAppService.cs`
- Modify `NamEcommerce/Application/NamEcommerce.Application.Services/DeliveryNotes/DeliveryNoteAppService.cs`
- Create `NamEcommerce/Presentation/NamEcommerce.Web.Contracts/Commands/Models/DeliveryNotes/SyncMobileDeliveryCommand.cs`
- Create `NamEcommerce/Presentation/NamEcommerce.Web.Framework/Commands/Handlers/DeliveryNotes/SyncMobileDeliveryHandler.cs`

Steps:
- [ ] Validate assignment to current user.
- [ ] Validate picture count after server-side upload.
- [ ] Validate accepted/rejected quantities against delivery note items.
- [ ] Save mobile delivery metadata.
- [ ] Create pending cash handover if cash collected.
- [ ] Return success for duplicate same `clientSubmissionId`.
- [ ] Verify: `dotnet test NamEcommerce/Tests/NamEcommerce.Application.Services.Test --filter "FullyQualifiedName~DeliveryNote"`

### Task 7: Driver PWA backend and app shell
Files:
- Create `NamEcommerce/Presentation/NamEcommerce.Web/Controllers/DeliveryMobileController.cs`
- Create `NamEcommerce/Presentation/NamEcommerce.Web/Models/DeliveryMobile/DeliveryMobileModels.cs`
- Create `NamEcommerce/Presentation/NamEcommerce.Web/Services/DeliveryMobile/IDeliveryMobileModelFactory.cs`
- Create `NamEcommerce/Presentation/NamEcommerce.Web/Services/DeliveryMobile/DeliveryMobileModelFactory.cs`
- Create `NamEcommerce/Presentation/NamEcommerce.Web/Views/DeliveryMobile/_Layout.cshtml`
- Create `NamEcommerce/Presentation/NamEcommerce.Web/Views/DeliveryMobile/Index.cshtml`
- Modify DI registration if this project registers model factories explicitly.

Steps:
- [ ] Render a lightweight page with no admin sidebar.
- [ ] Add endpoints for assigned notes and sync.
- [ ] Use anti-forgery token for online posts.
- [ ] Verify: run web app and open `/DeliveryMobile`.

### Task 8: PWA client offline store and sync
Files:
- Create `NamEcommerce/Presentation/NamEcommerce.Web/wwwroot/manifest-delivery-mobile.webmanifest`
- Create `NamEcommerce/Presentation/NamEcommerce.Web/wwwroot/delivery-mobile-sw.js`
- Create `NamEcommerce/Presentation/NamEcommerce.Web/wwwroot/modules/delivery-mobile/app.js`
- Create `NamEcommerce/Presentation/NamEcommerce.Web/wwwroot/modules/delivery-mobile/api.js`
- Create `NamEcommerce/Presentation/NamEcommerce.Web/wwwroot/modules/delivery-mobile/offline-store.js`
- Create `NamEcommerce/Presentation/NamEcommerce.Web/wwwroot/modules/delivery-mobile/sync.js`
- Create `NamEcommerce/Presentation/NamEcommerce.Web/wwwroot/modules/delivery-mobile/camera.js`
- Create `NamEcommerce/Presentation/NamEcommerce.Web/wwwroot/modules/delivery-mobile/location.js`
- Create `NamEcommerce/Presentation/NamEcommerce.Web/wwwroot/css/delivery-mobile.css`

Steps:
- [ ] Cache app shell and static assets.
- [ ] Cache delivery run payloads and post run cache acknowledgement to the server.
- [ ] Store assigned delivery notes in IndexedDB.
- [ ] Store proof images as blobs in IndexedDB.
- [ ] Generate `clientSubmissionId` before enqueue.
- [ ] Show queue count and sync errors.
- [ ] Sync uploads all proof images in a single delivery confirmation request.
- [ ] Verify manually:
  - Open `/DeliveryMobile` online and load assigned notes.
  - Switch browser offline.
  - Confirm delivery with image and location.
  - Switch online.
  - Click sync and verify server receives one delivered record.

### Task 9: Cashier UI
Files:
- Create `NamEcommerce/Presentation/NamEcommerce.Web/Controllers/DeliveryCashHandoverController.cs`
- Create `NamEcommerce/Presentation/NamEcommerce.Web/Models/DeliveryCashHandovers/DeliveryCashHandoverModels.cs`
- Create `NamEcommerce/Presentation/NamEcommerce.Web/Services/DeliveryCashHandovers/IDeliveryCashHandoverModelFactory.cs`
- Create `NamEcommerce/Presentation/NamEcommerce.Web/Services/DeliveryCashHandovers/DeliveryCashHandoverModelFactory.cs`
- Create `NamEcommerce/Presentation/NamEcommerce.Web/Views/DeliveryCashHandover/List.cshtml`
- Create `NamEcommerce/Presentation/NamEcommerce.Web/Views/DeliveryCashHandover/Details.cshtml`
- Create `NamEcommerce/Presentation/NamEcommerce.Web.Contracts/Commands/Models/DeliveryNotes/ConfirmDeliveryCashHandoverCommand.cs`
- Create `NamEcommerce/Presentation/NamEcommerce.Web.Contracts/Commands/Models/DeliveryNotes/RejectDeliveryCashHandoverCommand.cs`
- Create `NamEcommerce/Presentation/NamEcommerce.Web.Framework/Commands/Handlers/DeliveryNotes/ConfirmDeliveryCashHandoverHandler.cs`
- Create `NamEcommerce/Presentation/NamEcommerce.Web.Framework/Commands/Handlers/DeliveryNotes/RejectDeliveryCashHandoverHandler.cs`

Steps:
- [ ] List pending handovers.
- [ ] Confirm and create `CustomerPayment`.
- [ ] Reject with reason.
- [ ] Show related delivery note and proof metadata.
- [ ] Verify:
  - Deliver with cash.
  - See pending handover.
  - Confirm handover.
  - Verify customer debt paid amount increases.

### Task 10: Migration and wiring
Files:
- Modify `NamEcommerce/Infrastructure/NamEcommerce.Data.SqlServer/NamEcommerceEfDbContext.cs` only if DbSet registration is explicit.
- Create EF migration in `NamEcommerce/Migrations/NamEcommerce.Data.SqlServerMigrations`.
- Modify dependency registration files if app services/managers are registered explicitly.
- Modify navigation/menu if the current layout has a central menu list.

Steps:
- [ ] Add migration for new fields and `DeliveryCashHandover`.
- [ ] Ensure services are registered.
- [ ] Add menu links for warehouse/admin, PWA, and cashier handovers.
- [ ] Verify: `dotnet build NamEcommerce.sln`

### Task 11: End-to-end verification
Steps:
- [ ] Create/order a delivery note with `AmountToCollect > 0`.
- [ ] Confirm warehouse preparation and verify stock is not dispatched yet.
- [ ] Create delivery run and assign driver.
- [ ] Try to hand over the run before cache acknowledgement or paper manifest; verify it is blocked.
- [ ] Open `/DeliveryMobile` as driver and cache the run.
- [ ] Verify cache acknowledgement appears for warehouse.
- [ ] Hand over the run and verify stock dispatch happens at `Delivering`.
- [ ] Simulate offline.
- [ ] Confirm delivery with proof image, GPS, receiver, and cash.
- [ ] Return online and sync.
- [ ] Confirm delivery note becomes `Delivered`.
- [ ] Confirm customer debt exists.
- [ ] Confirm cash handover is pending.
- [ ] Confirm handover as cashier.
- [ ] Confirm `CustomerPayment` exists and debt remaining amount decreases.
- [ ] Repeat with driver not opening PWA and paper manifest issued; verify handover is allowed and manual/post-return confirmation path works.
- [ ] Run final verification: `dotnet build NamEcommerce.sln`

## Risks And Guardrails
- Do not modify the order purchase/import flow unless a failing test proves it is required.
- Do not make native mobile app in phase 1.
- Do not record `CustomerPayment` before cashier confirmation.
- Do not depend only on browser background sync.
- Do not pre-upload images in offline flow.
- Do not allow warehouse handover without either PWA cache acknowledgement or paper manifest fallback.
- Do not dispatch stock at `Confirmed` in the improved normal customer delivery flow.
- Keep the existing admin `MarkDelivered` modal working.
- Keep role handling user-based and non-exclusive.

## Open Follow-Up After MVP
- Permission enforcement for `Delivery.Assign`, `Delivery.Execute`, and `CashHandover.Confirm`.
- Native app wrapper if PWA is not reliable enough on target devices.
- Route grouping and delivery batches.
- Customer signature capture.
- Saved customer delivery coordinates for future delivery suggestions.

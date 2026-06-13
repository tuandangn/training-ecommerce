# Partial Delivery Acceptance And Cash Handover Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Complete delivery confirmation so admin and delivery staff can record partial goods acceptance and partial cash collected, while customer debt is reduced only when the cashier confirms cash handover.

**Architecture:** Keep `DeliveryNote` as the delivery completion aggregate and keep `DeliveryRun` as the cash handover boundary. Both admin and mobile completion flows must call the same `MarkDeliveredAsync` path with acceptance lines and explicit cash-collected metadata. `CustomerPayment` remains created only from `DeliveryRunManager.ConfirmCashHandoverAsync`.

**Tech Stack:** ASP.NET Core MVC/Razor, MediatR, Clean Architecture/DDD projects, EF Core SQL Server mappings, existing DeliveryRun mobile PWA, existing customer debt/payment managers.

---

## Current Findings

- Admin `DeliveryNote/MarkDelivered` already supports partial item acceptance through `acceptanceItemsJson`, `AcceptedQuantity`, and `RejectedQuantity`.
- Admin modal `_DeliveryNote.ConfirmDelivered.cshtml` has accepted/rejected item UI but does not collect `CashCollectedAmount`.
- Mobile `CompleteMobileDeliveryNoteCommand` only sends receiver, proof, location, note, idempotency key, and `CashCollectedAmount`; it does not send accepted/rejected item lines.
- Mobile UI defaults `cashCollectedAmount` to `AmountToCollect`, which makes the full-collection path too easy and hides partial collection as a first-class workflow.
- `DeliveryNoteManager.ResolveDeliveryAcceptance` already recalculates `AmountToCollect` from actual accepted goods plus surcharge plus agreed charge.
- `DeliveryRunManager.ConfirmCashHandoverAsync` is the correct accounting boundary: it creates `CustomerPayment` only after cashier confirmation.
- `DeliveryRunManager.GetCashCollectedAmount` currently falls back from `DeliveryCashCollectedAmount == null` to `AmountToCollect`. This can record a full payment even when no one explicitly entered collected cash.

## Business Rules To Preserve

- Delivery completion records operational facts:
  - goods customer accepted
  - goods customer rejected
  - reason for rejected goods
  - amount customer should pay after acceptance adjustment
  - cash the shipper/admin says was collected
  - receiver/proof/location/note
- Completing delivery must not reduce customer debt immediately.
- Cash collected by shipper is internal pending cash until cashier confirms handover.
- Cashier handover confirmation is the only place that creates `CustomerPayment`.
- If cashier confirms less than the explicit collected amount, reject the handover for now; do not create partial/adjustment flows in this slice.
- If no collected cash was explicitly recorded on a delivered note, the system must not assume full collection.

## Scope

### In Scope

- Add partial acceptance inputs to mobile delivery completion.
- Add explicit cash collected input to admin `Đã giao` modal.
- Pass cash collected through command/controller/handler to `DeliveryCompletionMetadata`.
- Make cash handover use explicit `DeliveryCashCollectedAmount` only.
- Show pending cash clearly in delivery run/admin screens.
- Keep customer debt reduction only at cashier confirmation.
- Update plan/implementation docs.

### Out Of Scope

- Bank transfer collection from shipper flow.
- Customer signature capture.
- Route optimization.
- Cash shortage/loss settlement accounting.
- Automated tests for this slice, per current project instruction to skip tests temporarily.

## Acceptance Scenarios

1. Shipper delivers all goods and collects all cash.
   - Mobile sends accepted quantities equal delivered quantities.
   - Mobile sends cash collected equal amount to collect.
   - Delivery note becomes `Delivered`.
   - Customer debt is created.
   - Customer debt is not paid yet.
   - Delivery run shows pending cash.
   - Cashier confirms handover.
   - Customer payment is created and debt decreases.

2. Shipper delivers partial goods and collects partial cash.
   - Mobile sends accepted/rejected quantities per line.
   - Rejected items require reason.
   - Delivery note `AmountToCollect` is recalculated from accepted goods.
   - Auto customer return is created for rejected goods.
   - Mobile sends cash collected lower than recalculated amount.
   - Customer debt remains with unpaid remaining balance after cashier confirms collected cash.

3. Admin marks delivery as delivered on behalf of shipper.
   - Admin can enter accepted/rejected quantities.
   - Admin can enter cash customer paid to shipper.
   - Cash does not reduce debt until cashier confirms handover.

4. Admin/mobile completes delivered note with no cash collected.
   - `DeliveryCashCollectedAmount` is saved as `0`.
   - Cash handover is not required for that note.
   - Customer debt remains outstanding.

5. Legacy delivered note has `DeliveryCashCollectedAmount == null`.
   - Delivery run does not assume full collection.
   - UI shows collected cash as missing/unknown or zero pending action.
   - Cashier confirmation cannot accidentally create a full payment from null.

## File Plan

### Contracts And Handlers

- Modify `NamEcommerce/Presentation/NamEcommerce.Web.Contracts/Commands/Models/DeliveryNotes/MarkDeliveryNoteDeliveredCommand.cs`
  - Add `decimal? CashCollectedAmount`.
- Modify `NamEcommerce/Presentation/NamEcommerce.Web.Framework/Commands/Handlers/DeliveryNotes/MarkDeliveryNoteDeliveredHandler.cs`
  - Map `CashCollectedAmount` into `DeliveryCompletionMetadataAppDto`.
  - Set source to a stable admin value such as `Admin`.
- Modify `NamEcommerce/Presentation/NamEcommerce.Web.Contracts/Commands/Models/DeliveryNotes/CompleteMobileDeliveryNoteCommand.cs`
  - Add `IList<CompleteMobileDeliveryNoteItemCommand> Items`.
  - Add item command fields: `DeliveryNoteItemId`, `AcceptedQuantity`, `RejectedQuantity`, `RejectReason`.
- Modify `NamEcommerce/Presentation/NamEcommerce.Web.Framework/Commands/Handlers/DeliveryNotes/CompleteMobileDeliveryNoteHandler.cs`
  - Map mobile item lines into `DeliveryAcceptanceAppDto`.
  - Keep `CashCollectedAmount` in `CompletionMetadata`.
- Modify `NamEcommerce/Application/NamEcommerce.Application.Contracts/Dtos/DeliveryNotes/DeliveryNoteAppDtos.cs`
  - Reuse existing `DeliveryAcceptanceAppDto`; no new application DTO is required unless handler duplication becomes noisy.

### Admin Delivery Note UI

- Modify `NamEcommerce/Presentation/NamEcommerce.Web/Controllers/DeliveryNoteController.cs`
  - Add `decimal? cashCollectedAmount` parameter to `MarkDelivered`.
  - Pass it to `MarkDeliveryNoteDeliveredCommand`.
- Modify `NamEcommerce/Presentation/NamEcommerce.Web/Views/Shared/_DeliveryNote.ConfirmDelivered.cshtml`
  - Add an explicit `Tiền khách đã trả` field.
  - Default value should be the recalculated amount only after JS computes accepted quantities; it must remain user-editable.
  - Show a small note: this is cash held by delivery staff/admin and will only reduce debt after cashier handover.
- Modify `NamEcommerce/Presentation/NamEcommerce.Web/wwwroot/modules/DeliveryNoteController.js`
  - Recalculate expected amount when accepted quantities or agreed charge changes.
  - Keep accepted/rejected payload generation.
  - Include `cashCollectedAmount` in posted form data.
  - Ensure `cashCollectedAmount` can be `0`.

### Mobile Delivery UI And Offline Sync

- Modify `NamEcommerce/Presentation/NamEcommerce.Web.Contracts/Models/DeliveryNotes/DeliveryRunModels.cs`
  - Add `Id` or `DeliveryNoteItemId` to `DeliveryRunProductItemModel`.
  - Add `QuantityDecimalPlaces` if available in the model factory.
  - Add unit price or line subtotal only if needed to calculate expected collected amount client-side.
- Modify `NamEcommerce/Presentation/NamEcommerce.Web/Services/DeliveryNotes/DeliveryRunModelFactory.cs`
  - Populate product item ids and decimal places.
  - Keep `AmountToCollect` and `CashCollectedAmount`.
- Modify `NamEcommerce/Presentation/NamEcommerce.Web/Views/DeliveryMobile/Run.cshtml`
  - Replace read-only goods list with compact accepted quantity controls.
  - Show delivered quantity, accepted quantity, rejected quantity.
  - Require a reject reason when any rejected quantity is greater than 0.
  - Keep receiver, proof, note, location, and cash collected fields.
  - Change cash field label to `Tiền khách đã trả cho bạn`.
  - Do not visually imply that collected cash has already gone to store cash.
- Modify `NamEcommerce/Presentation/NamEcommerce.Web/Controllers/DeliveryMobileController.cs`
  - Accept `acceptanceItemsJson` or repeated form fields from mobile.
  - Parse into `CompleteMobileDeliveryNoteCommand.Items`.
  - Persist offline submissions with the same payload shape.
- Modify `NamEcommerce/Presentation/NamEcommerce.Web/wwwroot/modules/DeliveryMobileCache.js`
  - Save acceptance item fields into IndexedDB pending completion.
  - Sync item lines along with proof image and cash collected amount.
  - Keep idempotency key behavior unchanged.

### Domain And Cash Handover Semantics

- Modify `NamEcommerce/Domain/NamEcommerce.Domain.Services/DeliveryNotes/DeliveryRunManager.cs`
  - Change `GetCashCollectedAmount(DeliveryNote note)` to return explicit collected amount only:
    - `note.DeliveryCashCollectedAmount.GetValueOrDefault()`
  - Do not fallback to `note.AmountToCollect`.
  - If all delivered notes have explicit collected amount `0`, cash handover is not required.
  - If any delivered note has null collected amount and belongs to a run that is not cash-confirmed, return a domain error requiring cash collection review before handover.
  - Keep `RecordCodPaymentsAsync` unchanged in principle: it records payment only during cashier confirmation.
- Modify `NamEcommerce/Domain/NamEcommerce.Domain/Entities/DeliveryNotes/DeliveryNote.cs`
  - Keep `DeliveryCashCollectedAmount` nullable for legacy data.
  - Ensure new delivery completion always sets it to `0` or a positive value.
  - Existing validation `cashCollectedAmount < 0` remains.
- Modify `NamEcommerce/Application/NamEcommerce.Application.Services/DeliveryNotes/DeliveryNoteAppService.cs`
  - Ensure admin and mobile calls both provide completion metadata with cash value.

### Delivery Run And Cashier UI

- Modify `NamEcommerce/Presentation/NamEcommerce.Web/Views/DeliveryRun/Details.cshtml`
  - Show three values:
    - `Phải thu sau điều chỉnh`
    - `Shipper báo đã thu`
    - `Thủ quỹ đã nhận`
  - Use explicit collected amount only; do not display null as full collection.
  - If a delivered note has missing cash collection info, show `Chưa khai báo` and block confirmation.
- Modify `NamEcommerce/Presentation/NamEcommerce.Web/Services/DeliveryNotes/DeliveryRunModelFactory.cs`
  - Calculate pending cash from explicit `DeliveryCashCollectedAmount`.
  - Surface missing-cash-info state for delivered notes.
- Modify `NamEcommerce/Presentation/NamEcommerce.Web.Contracts/Models/DeliveryNotes/DeliveryRunModels.cs`
  - Add model fields if needed:
    - `bool CashCollectionAmountMissing`
    - `decimal ExplicitCashCollectedAmount`
    - `decimal PendingCashHandoverAmount`

### Documentation

- Modify `plans/DeliveryNotes/delivery_mobile_pwa_cash_handover_implement.md`
  - Add a checklist entry for partial acceptance/cash handover hardening.
- Create `plans/DeliveryNotes/partial_acceptance_cash_handover_implement.md` during implementation.

## Implementation Tasks

### Task 1: Wire admin cash collected through MarkDelivered

- [ ] Modify `MarkDeliveryNoteDeliveredCommand` to add `decimal? CashCollectedAmount`.
- [ ] Modify `MarkDeliveryNoteDeliveredHandler` to set:
  - `CompletionMetadata.Source = "Admin"`
  - `CompletionMetadata.CashCollectedAmount = request.CashCollectedAmount ?? 0`
- [ ] Modify `DeliveryNoteController.MarkDelivered` to accept and pass `cashCollectedAmount`.
- [ ] Modify `_DeliveryNote.ConfirmDelivered.cshtml` to add the cash collected input.
- [ ] Modify `DeliveryNoteController.js` to update expected amount and submit cash collected.
- [ ] Verify manually:
  - Open a delivery note details page.
  - Click `Đã giao`.
  - Enter accepted quantities and cash collected lower than expected.
  - Submit.
  - Verify `DeliveryCashCollectedAmount` is saved and debt is not paid.

### Task 2: Add mobile partial acceptance payload

- [ ] Modify `CompleteMobileDeliveryNoteCommand` to include item lines.
- [ ] Modify `CompleteMobileDeliveryNoteHandler` to map item lines into `DeliveryAcceptanceAppDto`.
- [ ] Modify `DeliveryMobileController.CompleteDeliveryNote` to parse mobile acceptance item data.
- [ ] Use the same validation semantics as admin:
  - accepted + rejected must equal delivered quantity
  - rejected quantity requires reason
  - negative quantities are invalid
- [ ] Verify manually:
  - Submit mobile completion with one rejected item.
  - Delivery note amount to collect is based on accepted quantity.
  - Auto customer return is created for rejected quantity.

### Task 3: Update mobile UI for item acceptance and partial cash

- [ ] Modify `DeliveryRunProductItemModel` to include the delivery note item id.
- [ ] Populate the new field in `DeliveryRunModelFactory`.
- [ ] Modify `DeliveryMobile/Run.cshtml`:
  - render accepted quantity inputs per product item
  - render rejected quantity as derived value
  - render reject reason only when needed
  - render `Tiền khách đã trả cho bạn`
- [ ] Modify `DeliveryMobileCache.js`:
  - include acceptance items in online form submission
  - include acceptance items in offline queued payload
  - include acceptance items in sync retry payload
- [ ] Verify manually online and offline:
  - Online submit with partial acceptance.
  - Offline queue with partial acceptance.
  - Reconnect and sync.
  - Server receives the same item lines.

### Task 4: Remove unsafe full-cash fallback

- [ ] Modify `DeliveryRunManager.GetCashCollectedAmount` to use explicit `DeliveryCashCollectedAmount` only.
- [ ] Add a private helper such as `HasMissingCashCollectionInfo(DeliveryNote note)` for delivered run notes with null amount.
- [ ] Update `ConfirmCashHandoverAsync`:
  - if any delivered note has missing cash info, return a domain error such as `Error.DeliveryCashCollectedAmountRequired`
  - expected amount is sum of explicit collected amounts
  - zero expected amount means handover not required
- [ ] Keep `RecordCodPaymentsAsync` creating `CustomerPayment` only after cashier confirmation.
- [ ] Verify manually:
  - Delivered note with `CashCollectedAmount = 0` does not require cash handover.
  - Delivered note with `CashCollectedAmount > 0` requires cashier confirmation.
  - Delivered note with null cash amount cannot accidentally create full payment.

### Task 5: Update delivery run/cashier display

- [ ] Modify `DeliveryRunModels` to expose explicit cash state.
- [ ] Modify `DeliveryRunModelFactory` to calculate:
  - total amount to collect after delivery adjustment
  - explicit cash collected by shipper
  - missing cash info count
  - cashier received amount
- [ ] Modify `DeliveryRun/Details.cshtml` to show:
  - `Phải thu sau điều chỉnh`
  - `Shipper báo đã thu`
  - `Thủ quỹ đã nhận`
  - `Chưa khai báo tiền thu` warning when applicable
- [ ] Disable cash handover confirmation while cash info is missing.
- [ ] Verify manually:
  - Run with partial collected cash shows the partial amount.
  - Cashier confirmation form defaults to explicit collected amount.
  - After confirmation, customer payment exists and debt decreases by only confirmed collected amount.

### Task 6: Regression checks for debt/payment boundary

- [ ] Complete a delivery with cash collected.
- [ ] Before cashier confirmation, verify:
  - `CustomerDebt` exists for delivered amount
  - no `CustomerPayment` was created for the COD cash
  - cash book does not include the COD cash
- [ ] Confirm cash handover.
- [ ] After cashier confirmation, verify:
  - `CustomerPayment` exists
  - customer debt remaining decreases
  - cash book includes the cash inflow
- [ ] Repeat with partial delivery and partial cash.
- [ ] Repeat with zero cash collected.

### Task 7: Build and UI lint

- [ ] Run `dotnet build NamEcommerce/Presentation/NamEcommerce.Web/NamEcommerce.Web.csproj`.
- [ ] Run `powershell -ExecutionPolicy Bypass -File tools/ui-lint.ps1`.
- [ ] If the normal build is blocked by a running web process, run the same Web project build with a temporary `OutDir` and remove the temporary output afterward.
- [ ] Do not add automated tests in this slice unless the user explicitly re-enables test writing.

## Manual Verification Checklist

- [ ] Admin full delivery, full cash collected, no immediate payment.
- [ ] Admin partial delivery, partial cash collected, no immediate payment.
- [ ] Mobile full delivery, full cash collected, no immediate payment.
- [ ] Mobile partial delivery, partial cash collected, no immediate payment.
- [ ] Mobile offline partial delivery syncs accepted/rejected lines and cash amount.
- [ ] Cashier confirmation creates `CustomerPayment`.
- [ ] Cashier confirmation amount cannot differ from explicit shipper collected amount.
- [ ] Delivery run no longer treats null collected cash as full collected cash.
- [ ] Customer debt and cash book only change after cashier confirmation.

## Risks

- Existing delivered notes with null `DeliveryCashCollectedAmount` need clear UI handling. The plan avoids converting null to full cash automatically.
- Partial rejected goods already create customer return records; UI should not create duplicate return requests.
- Cash collected greater than amount to collect currently becomes deposit during handover. Keep this behavior unless the user asks to block over-collection.
- Offline image payloads can be large. Keep existing size validation and show sync errors clearly.


# Partial Delivery Acceptance And Cash Handover Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Complete delivery confirmation so admin and delivery staff can record returned quantities and cash collected by shipper, while customer debt is reduced only when cashier confirms cash handover.

**Architecture:** `DeliveryNote` remains the source of truth for delivery completion, returned goods, recalculated amount to collect, and the cash amount reported by shipper/admin. `DeliveryRun` remains the cash handover boundary; it turns explicit shipper cash into `CustomerPayment` only after cashier confirmation. UI collects returned quantity, while handlers derive accepted quantity before calling the existing acceptance DTOs.

**Tech Stack:** ASP.NET Core MVC/Razor, MediatR, Clean Architecture/DDD projects, EF Core SQL Server mappings, existing DeliveryRun mobile PWA/offline cache, existing customer debt/payment managers, existing Order timeline model.

---

## Decisions

- Delivery confirmation UI must ask for `Số lượng trả về`, not `Số lượng đã nhận`.
- `Số lượng đã nhận` is derived as `số lượng giao - số lượng trả về`.
- Returned quantity defaults to `0`.
- Returned quantity must be between `0` and delivered quantity.
- If any returned quantity is greater than `0`, reject reason is required.
- Delivery completion creates or updates the operational delivery result and raises the existing delivered event.
- Customer debt is still created when delivery is marked delivered.
- No customer payment is created when shipper/admin marks a note as delivered.
- Cash collected by shipper is only an explicit operational amount: `DeliveryCashCollectedAmount`.
- Cash collected by shipper reduces debt only after cashier confirms handover on the delivery run.
- `DeliveryCashCollectedAmount == null` must never mean full collection.
- Cash collected by shipper must be `>= 0` and `<= AmountToCollect` after returned quantities are applied.
- Over-collection and customer deposit are not handled in delivery completion. Use the normal customer payment/deposit flow instead.
- QR/bank transfer is not shipper cash. If the customer pays by QR directly to the shop account, that must go through the existing bank transfer/payment intent flow, not `DeliveryCashCollectedAmount` or delivery-run cash handover.

## Current Findings

- Admin `DeliveryNote/MarkDelivered` already supports partial item acceptance through `acceptanceItemsJson`, `AcceptedQuantity`, and `RejectedQuantity`.
- Admin modal currently asks for accepted quantity, but the requested UX is to ask for returned quantity.
- Admin modal does not collect `CashCollectedAmount`.
- Mobile `CompleteMobileDeliveryNoteCommand` sends receiver/proof/location/note/idempotency/cash only; it does not send returned item lines.
- Mobile UI defaults cash collected to `AmountToCollect`, which hides partial collection.
- `DeliveryNoteManager.ResolveDeliveryAcceptance` already recalculates `AmountToCollect` from accepted goods, surcharge, and agreed customer charge.
- `DeliveryNoteDeliveredHandler` creates `CustomerDebt` when the note is delivered.
- `DeliveryRunManager.ConfirmCashHandoverAsync` is the correct accounting boundary for creating `CustomerPayment`.
- `DeliveryRunManager`, `DeliveryRunModelFactory`, `DeliveryRun/Details`, and mobile UI currently fallback missing cash to `AmountToCollect`.
- Order timeline already exists and can display delivery/debt/payment events; it should be extended with richer delivery cash/return nodes.

## Scope

### In Scope

- Change admin delivery confirmation from accepted quantity input to returned quantity input.
- Add mobile returned quantity inputs per delivery note item.
- Derive accepted quantity server-side or before sending the existing acceptance DTOs.
- Add explicit cash collected input to admin `Đã giao`.
- Keep explicit cash collected input in mobile, but make partial collection first-class.
- Validate cash collected does not exceed recalculated amount to collect.
- Remove every unsafe `null cash => amount to collect` fallback in delivery-run handover and display.
- Add a narrow cash review action for delivered notes with missing cash info before cashier handover.
- Show delivery-run cash state clearly:
  - `Phải thu sau trả hàng`
  - `Shipper báo đã thu`
  - `Thủ quỹ đã nhận`
  - `Chưa khai báo tiền thu`
- Add Order timeline nodes for partial/full delivery, returned goods, shipper-reported cash, cash handover, and payment.
- Update documentation.

### Out Of Scope

- QR/bank transfer collection inside shipper cash handover.
- Customer signature capture.
- Route optimization.
- Cash shortage/loss settlement accounting.
- Allowing cashier to confirm an amount different from shipper-reported cash.
- Treating shipper over-collection as customer deposit.
- Automated tests for this slice, per current instruction to skip tests temporarily.

## Acceptance Scenarios

1. Full delivery, full cash collected by shipper.
   - Returned quantities are all `0`.
   - Accepted quantities are derived as full delivered quantities.
   - `AmountToCollect` stays equal to the delivered amount.
   - `DeliveryCashCollectedAmount` is saved as the cash collected.
   - Customer debt is created.
   - Customer payment is not created yet.
   - Delivery run shows pending shipper cash.
   - Cashier confirms handover.
   - Customer payment is created and debt decreases.

2. Partial delivery, partial cash collected by shipper.
   - User enters returned quantity per line.
   - Accepted quantity is derived per line.
   - Reject reason is required.
   - `AmountToCollect` is recalculated from accepted goods.
   - Auto customer return is created for returned goods.
   - `DeliveryCashCollectedAmount` can be lower than recalculated amount.
   - After cashier handover, payment reduces debt only by the cash actually collected.
   - Remaining debt stays open.

3. Delivered with no cash collected.
   - Returned quantities may be `0` or greater.
   - `DeliveryCashCollectedAmount` is saved as `0`.
   - Customer debt is created.
   - Cash handover is not required for that note.

4. Delivered note has missing cash info from legacy/admin flow.
   - Delivery run and mobile do not assume full collection.
   - UI shows `Chưa khai báo tiền thu`.
   - Cashier confirmation is disabled until admin records an explicit cash amount for that delivered note.

5. Customer pays by QR/bank transfer directly to shop.
   - Delivery confirmation records `DeliveryCashCollectedAmount = 0`.
   - QR payment is recorded through the existing bank transfer/customer payment flow after verification.
   - Delivery-run cash handover is not used for that QR amount.

6. User enters shipper cash greater than amount to collect.
   - Submission is rejected.
   - No delivery completion or handover payment is created from the invalid cash amount.

## File Plan

### Contracts And Handlers

- Modify `NamEcommerce/Presentation/NamEcommerce.Web.Contracts/Commands/Models/DeliveryNotes/MarkDeliveryNoteDeliveredCommand.cs`
  - Add `decimal? CashCollectedAmount`.
  - Keep `MarkDeliveryNoteDeliveredItemCommand.AcceptedQuantity` and `RejectedQuantity` because the application/domain DTOs already use this shape.
- Modify `NamEcommerce/Presentation/NamEcommerce.Web.Framework/Commands/Handlers/DeliveryNotes/MarkDeliveryNoteDeliveredHandler.cs`
  - Map `CashCollectedAmount` into `DeliveryCompletionMetadataAppDto`.
  - Set `CompletionMetadata.Source = "Admin"`.
- Modify `NamEcommerce/Presentation/NamEcommerce.Web.Contracts/Commands/Models/DeliveryNotes/CompleteMobileDeliveryNoteCommand.cs`
  - Add `IList<CompleteMobileDeliveryNoteItemCommand> Items`.
  - Add item command fields: `DeliveryNoteItemId`, `ReturnedQuantity`, `RejectReason`.
- Modify `NamEcommerce/Presentation/NamEcommerce.Web.Framework/Commands/Handlers/DeliveryNotes/CompleteMobileDeliveryNoteHandler.cs`
  - Convert mobile returned quantities to `DeliveryAcceptanceAppDto`:
    - `RejectedQuantity = ReturnedQuantity`
    - `AcceptedQuantity = delivered quantity - ReturnedQuantity`
  - The handler will need delivery note item quantities from `IDeliveryNoteAppService.GetByIdAsync`.
  - Keep `CashCollectedAmount` in `CompletionMetadata`.
- Modify `NamEcommerce/Application/NamEcommerce.Application.Contracts/Dtos/DeliveryNotes/DeliveryNoteAppDtos.cs`
  - Reuse existing acceptance DTOs.
  - No new application DTO is required unless the handler becomes noisy.

### Domain And App Services

- Modify `NamEcommerce/Domain/NamEcommerce.Domain.Services/DeliveryNotes/DeliveryNoteManager.cs`
  - After resolving acceptance and before `deliveryNote.MarkDelivered(...)`, validate `CashCollectedAmount <= acceptance.AmountToCollect`.
  - Keep `CashCollectedAmount = null` valid only for legacy/internal callers; admin/mobile handlers must pass `0` or a positive value.
  - Use a clear domain error such as `Error.CashCollectedAmountCannotExceedAmountToCollect`.
- Modify `NamEcommerce/Domain/NamEcommerce.Domain/Entities/DeliveryNotes/DeliveryNote.cs`
  - Keep `DeliveryCashCollectedAmount` nullable for legacy data.
  - Keep existing negative cash validation.
  - Add a focused internal method to update missing cash collection info after delivery:
    - Only `Delivered` notes can be updated.
    - Amount must be `>= 0`.
    - Amount must be `<= AmountToCollect`.
    - This method only updates `DeliveryCashCollectedAmount` and `UpdatedOnUtc`.
- Modify `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Services/DeliveryNotes/IDeliveryRunManager.cs`
  - Add a method to record missing cash info for a delivered note in a run before cash handover.
- Modify `NamEcommerce/Domain/NamEcommerce.Domain.Services/DeliveryNotes/DeliveryRunManager.cs`
  - Add `UpdateDeliveredNoteCashCollectedAmountAsync(runId, deliveryNoteId, amount)`.
  - Verify run exists, note belongs to run, run cash handover is not confirmed, note is delivered, and amount is valid.
  - Change cash calculations to use explicit `DeliveryCashCollectedAmount` only.
  - If any delivered note in the run has null cash info, block cash handover with `Error.DeliveryCashCollectedAmountRequired`.
  - If explicit collected sum is `0`, return `Error.DeliveryRunCashHandoverNotRequired`.
  - Keep `RecordCodPaymentsAsync` as the only place where COD cash becomes `CustomerPayment`.
  - Remove creation of `PaymentType.Deposit` for over-collected shipper cash by preventing over-collection earlier.
- Modify `NamEcommerce/Application/NamEcommerce.Application.Contracts/DeliveryNotes/IDeliveryRunAppService.cs`
  - Add an app-service method for the narrow cash review action.
- Modify `NamEcommerce/Application/NamEcommerce.Application.Services/DeliveryNotes/DeliveryRunAppService.cs`
  - Map the cash review request to the manager.
  - Update `HandOverAsync` notification logic so it does not use original `AmountToCollect` as actual collected cash.

### Admin Delivery Note UI

- Modify `NamEcommerce/Presentation/NamEcommerce.Web/Views/Shared/_DeliveryNote.ConfirmDelivered.cshtml`
  - Replace accepted quantity input with returned quantity input.
  - Show delivered quantity.
  - Show derived accepted quantity as read-only text.
  - Add explicit `Tiền khách đã trả cho shipper/admin` field.
  - Add a short helper text: the amount is cash currently held by delivery staff/admin and only reduces debt after cashier handover.
- Modify `NamEcommerce/Presentation/NamEcommerce.Web/wwwroot/modules/DeliveryNoteController.js`
  - Build acceptance payload from returned quantity:
    - `rejectedQuantity = returnedQuantity`
    - `acceptedQuantity = deliveredQuantity - returnedQuantity`
  - Clamp returned quantity to `0..deliveredQuantity`.
  - Recalculate derived accepted quantity on input.
  - Recalculate expected amount to collect for display where reliable.
  - Include `cashCollectedAmount` in submitted form data.
  - Allow `cashCollectedAmount = 0`.
  - Reject submit if cash collected exceeds displayed/known amount to collect.
- Modify `NamEcommerce/Presentation/NamEcommerce.Web/Controllers/DeliveryNoteController.cs`
  - Add `decimal? cashCollectedAmount` to `MarkDelivered`.
  - Pass it to `MarkDeliveryNoteDeliveredCommand`.

### Mobile Delivery UI And Offline Sync

- Modify `NamEcommerce/Presentation/NamEcommerce.Web.Contracts/Models/DeliveryNotes/DeliveryRunModels.cs`
  - Add `DeliveryNoteItemId` to `DeliveryRunProductItemModel`.
  - Add `UnitPrice`, `SubTotal`, and `QuantityDecimalPlaces` to support returned quantity entry and expected amount display.
  - Add note-level fields if needed for client calculation: `ShowPrice`, `Surcharge`, and explicit cash state.
- Modify `NamEcommerce/Presentation/NamEcommerce.Web/Services/DeliveryNotes/DeliveryRunModelFactory.cs`
  - Populate product item ids, unit price, subtotal, and quantity decimal places.
  - Stop replacing null `DeliveryCashCollectedAmount` with `AmountToCollect`.
  - Surface `CashCollectionAmountMissing` for delivered notes with null cash info.
- Modify `NamEcommerce/Presentation/NamEcommerce.Web/Views/DeliveryMobile/Run.cshtml`
  - Replace read-only goods list with compact returned quantity controls.
  - Show delivered quantity, returned quantity input, and derived accepted quantity.
  - Require reject reason when any returned quantity is greater than `0`.
  - Keep receiver, proof, note, location, and cash collected fields.
  - Change cash field label to `Tiền khách đã trả cho bạn`.
  - Default cash collected to the currently expected amount for new submissions, but allow lower values and `0`.
  - Do not display missing cash as full collected.
- Modify `NamEcommerce/Presentation/NamEcommerce.Web/Controllers/DeliveryMobileController.cs`
  - Accept `acceptanceItemsJson` or repeated returned-quantity fields.
  - Parse into `CompleteMobileDeliveryNoteCommand.Items`.
- Modify `NamEcommerce/Presentation/NamEcommerce.Web/wwwroot/modules/DeliveryMobileCache.js`
  - Save returned quantity fields and reject reason into IndexedDB pending completion.
  - Sync returned item lines with proof image and cash collected amount.
  - Keep idempotency key behavior unchanged.

### Delivery Run And Cashier UI

- Modify `NamEcommerce/Presentation/NamEcommerce.Web.Contracts/Models/DeliveryNotes/DeliveryRunModels.cs`
  - Add:
    - `bool CashCollectionAmountMissing`
    - `decimal ExplicitCashCollectedAmount`
    - `decimal PendingCashHandoverAmount`
    - `decimal RemainingCustomerDebtAmount` only if already easy to compute without extra domain work.
- Modify `NamEcommerce/Presentation/NamEcommerce.Web/Services/DeliveryNotes/DeliveryRunModelFactory.cs`
  - Calculate explicit shipper cash from `DeliveryCashCollectedAmount.GetValueOrDefault()`.
  - Do not fallback null to `AmountToCollect`.
  - Calculate missing-cash count.
  - Calculate pending handover amount from explicit shipper cash only.
- Modify `NamEcommerce/Presentation/NamEcommerce.Web/Views/DeliveryRun/Details.cshtml`
  - Show:
    - `Phải thu sau trả hàng`
    - `Shipper báo đã thu`
    - `Thủ quỹ đã nhận`
    - `Chưa khai báo tiền thu`
  - Disable cash handover confirmation while any delivered note has missing cash info.
  - Add a compact cash review form per delivered note with missing cash info.
- Modify `NamEcommerce/Presentation/NamEcommerce.Web/Controllers/DeliveryRunController.cs`
  - Add a POST action for updating missing cash info before handover.
- Modify `NamEcommerce/Presentation/NamEcommerce.Web.Contracts/Commands/Models/DeliveryNotes/DeliveryRunCommands.cs`
  - Add a command for the cash review action.
- Modify `NamEcommerce/Presentation/NamEcommerce.Web.Framework/Commands/Handlers/DeliveryNotes/DeliveryRunCommandHandlers.cs`
  - Map the cash review command to the app service.

### Order Timeline And Notifications

- Modify `NamEcommerce/Presentation/NamEcommerce.Web/Models/Orders/OrderDetailsModel.cs`
  - Add `DeliveryCashCollectedAmount` to `DeliveryNoteBasicModel`.
  - Add missing-cash display fields only if needed by the timeline.
- Modify `NamEcommerce/Presentation/NamEcommerce.Web/Services/Orders/OrderModelFactory.cs`
  - Populate `DeliveryCashCollectedAmount` when mapping delivery notes.
  - Enhance delivery timeline:
    - Delivered note with no returns: `Xác nhận đã giao`
    - Delivered note with returns: `Khách trả một phần khi giao`
    - Explicit shipper cash > 0: `Shipper báo đã thu tiền`
    - Explicit shipper cash = 0: `Chưa thu tiền khi giao`
    - Missing cash info: `Chưa khai báo tiền thu`
  - Existing customer payment timeline remains the source for actual debt reduction.
- Modify `NamEcommerce/Application/NamEcommerce.Application.Services/Notifications/DeliverySystemNotificationComposer.cs`
  - Update pending handover wording to use explicit shipper cash or pending review state, not original amount to collect.

### Resources And Documentation

- Modify `NamEcommerce/Presentation/NamEcommerce.Web/Resources/SharedResource.vi-VN.resx`
  - Add localized errors for cash exceeds amount and missing cash info.
- Modify `NamEcommerce/Presentation/NamEcommerce.Web/Resources/SharedResource.resx`
  - Add matching default resource keys.
- Modify `plans/DeliveryNotes/delivery_mobile_pwa_cash_handover_implement.md`
  - Add a checklist entry for returned quantity and explicit cash handover hardening.
- Create or update `plans/DeliveryNotes/partial_acceptance_cash_handover_implement.md` during implementation.

## Implementation Tasks

### Task 1: Admin uses returned quantity and explicit cash

- [ ] Add `CashCollectedAmount` to `MarkDeliveryNoteDeliveredCommand`.
- [ ] Map admin completion metadata in `MarkDeliveryNoteDeliveredHandler`.
- [ ] Add `cashCollectedAmount` to `DeliveryNoteController.MarkDelivered`.
- [ ] Change `_DeliveryNote.ConfirmDelivered.cshtml` from accepted quantity input to returned quantity input.
- [ ] Update `DeliveryNoteController.js` to derive accepted quantity from returned quantity.
- [ ] Update form submission to include `cashCollectedAmount`.
- [ ] Manual check: admin can enter returned quantity `0`, returned quantity partial, and cash collected lower than amount to collect.

### Task 2: Mobile sends returned quantity lines

- [ ] Add `CompleteMobileDeliveryNoteItemCommand` with `DeliveryNoteItemId`, `ReturnedQuantity`, and `RejectReason`.
- [ ] Inject or use `IDeliveryNoteAppService` in `CompleteMobileDeliveryNoteHandler` to derive accepted quantities from current delivery note items.
- [ ] Map returned item lines to `DeliveryAcceptanceAppDto`.
- [ ] Parse returned item lines in `DeliveryMobileController.CompleteDeliveryNote`.
- [ ] Manual check: mobile online submission with one returned line creates the expected acceptance payload.

### Task 3: Mobile UI and offline cache preserve returned quantities

- [ ] Extend `DeliveryRunProductItemModel` with item id, unit price, subtotal, and quantity decimal places.
- [ ] Populate the new fields in `DeliveryRunModelFactory`.
- [ ] Render returned quantity controls in `DeliveryMobile/Run.cshtml`.
- [ ] Show derived accepted quantity on mobile.
- [ ] Require reject reason when returned quantity is greater than `0`.
- [ ] Store and sync returned quantity fields in `DeliveryMobileCache.js`.
- [ ] Manual check: offline partial return syncs the same returned quantities after reconnect.

### Task 4: Domain validates explicit cash and preserves debt boundary

- [ ] In `DeliveryNoteManager.MarkDeliveredAsync`, validate cash collected after acceptance is resolved.
- [ ] Reject cash collected greater than recalculated `AmountToCollect`.
- [ ] Keep customer debt creation on `DeliveryNoteDeliveredHandler`.
- [ ] Keep customer payment creation only in `DeliveryRunManager.RecordCodPaymentsAsync`.
- [ ] Manual check: after delivery completion with cash, debt exists but no `CustomerPayment` exists until cashier handover.

### Task 5: Remove unsafe cash fallbacks and add cash review

- [ ] Change `DeliveryRunManager.GetCashCollectedAmount` to use explicit `DeliveryCashCollectedAmount` only.
- [ ] Block cash handover if any delivered run note has missing cash info.
- [ ] Add narrow cash review method before handover.
- [ ] Add matching app service, command, handler, and controller action.
- [ ] Add cash review form in delivery run details for missing-cash notes.
- [ ] Manual check: legacy delivered note with null cash cannot auto-create full payment; admin can record explicit `0` or a real cash amount before handover.

### Task 6: Update delivery run display and notifications

- [ ] Add explicit cash state fields to delivery run models.
- [ ] Remove `null => AmountToCollect` fallback in `DeliveryRunModelFactory`.
- [ ] Remove `null => AmountToCollect` fallback in `DeliveryRun/Details.cshtml`.
- [ ] Update pending cash totals to use explicit shipper cash only.
- [ ] Update delivery run cash handover notification wording and amount logic.
- [ ] Manual check: delivery run shows amount to collect, shipper-reported cash, cashier-received cash, and missing-cash warnings distinctly.

### Task 7: Add Order timeline nodes

- [ ] Add `DeliveryCashCollectedAmount` to `OrderDetailsModel.DeliveryNoteBasicModel`.
- [ ] Populate it in `OrderModelFactory`.
- [ ] Add timeline events for returned goods and shipper-reported cash.
- [ ] Keep existing payment timeline as the debt-reduction event.
- [ ] Manual check: Order details timeline shows delivery result before payment, then customer payment only after cashier handover.

### Task 8: Final verification

- [ ] Run manual admin full delivery scenario.
- [ ] Run manual admin partial-return scenario.
- [ ] Run manual mobile full delivery scenario.
- [ ] Run manual mobile partial-return scenario.
- [ ] Run manual mobile offline partial-return sync scenario.
- [ ] Run manual cashier handover scenario.
- [ ] Run manual QR/bank transfer scenario through existing payment flow, with delivery cash saved as `0`.
- [ ] Run `dotnet build NamEcommerce/Presentation/NamEcommerce.Web/NamEcommerce.Web.csproj` because this plan changes C# contracts, handlers, managers, and views.
- [ ] Run `powershell -ExecutionPolicy Bypass -File tools/ui-lint.ps1` because this plan changes Razor/JS UI.
- [ ] Do not add automated tests unless the user explicitly re-enables test writing.

## Manual Verification Checklist

- [ ] Admin `Đã giao` asks for returned quantity, not accepted quantity.
- [ ] Mobile `Đã giao` asks for returned quantity, not accepted quantity.
- [ ] Returned quantity `0` means full delivery.
- [ ] Returned quantity partial creates/reuses auto customer return behavior.
- [ ] Cash collected can be lower than amount to collect.
- [ ] Cash collected cannot exceed amount to collect.
- [ ] Completing delivery creates customer debt but no customer payment.
- [ ] Cashier handover creates customer payment and reduces debt.
- [ ] QR/bank transfer payment does not go through shipper cash handover.
- [ ] Missing cash info is shown as missing, not as full collection.
- [ ] Order timeline shows delivery, returned goods, shipper cash, handover/payment in the correct order.

## Risks

- Existing delivered notes with null `DeliveryCashCollectedAmount` need the cash review path before handover.
- Client-side expected amount may differ from server for edge cases such as hidden price; server validation remains authoritative.
- Partial returned goods already create customer return records; the implementation must not create duplicate returns on idempotent mobile retries.
- Offline payload changes must preserve idempotency keys and proof image upload behavior.
- QR/bank transfer must stay out of delivery-run cash handover to avoid CashFlow/BalanceSheet mismatches.

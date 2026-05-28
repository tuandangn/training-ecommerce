# DeliveryAcceptance Design

## Objective

Add a first-class delivery acceptance flow for the moment a customer confirms receipt of a delivery note. This covers the case where the customer receives only part of the delivered goods and immediately returns the rest, with reason, evidence pictures, agreed extra charges, and correct financial/inventory effects.

This is distinct from a later customer return request. Later returns remain handled by CustomerReturn / Customer Portal return requests.

## Confirmed Decisions

- Use a new `DeliveryAcceptance` concept attached to `DeliveryNote`.
- Do not mutate the original delivered quantities on `DeliveryNoteItem`; keep the historical shipped quantity.
- A delivery can be accepted fully, accepted partially, or rejected entirely at the point of delivery.
- If the customer returns goods at delivery time, the system creates an internal `CustomerReturn` in Draft/Inspecting flow for those rejected quantities.
- All CustomerReturn flows choose the receiving warehouse only at the final return confirmation step.
- For normal delivery notes, the return confirmation step should suggest the original dispatch warehouse as the default receiving warehouse.
- For direct-ship delivery notes, do not ask for a receiving warehouse at delivery acceptance time. The receiving warehouse is also selected later when confirming the generated CustomerReturn.
- Do not add a separate damaged/holding warehouse concept in this phase.
- If the delivery note does not show prices to the customer, the acceptance UI must still allow quantity, reason, agreed extra charge, and evidence pictures, but must not require or expose pricing to the customer/delivery actor.
- Customer debt is created immediately after delivery acceptance using internal prices, even when `ShowPrice = false`.

## Domain Model

### DeliveryAcceptance

Represents the actual customer receipt result for one `DeliveryNote`.

Fields:
- `Id`
- `DeliveryNoteId`
- `CustomerId`
- `Status`: `FullAccepted`, `PartialAccepted`, `Rejected`
- `ReceiverName`
- `Note`
- `AgreedCustomerCharge`: extra amount the customer agrees to pay because of the delivery issue, if any
- `AgreedCustomerChargeReason`
- `DeliveryProofPictureId`: optional/required according to existing delivery proof rules
- `CreatedByUserId`
- `CreatedOnUtc`

Rules:
- One active acceptance per delivery note.
- Sum of accepted and rejected quantities per item must equal the original delivery note item quantity.
- Acceptance is allowed for normal delivery notes in Confirmed/Delivering state and direct-ship delivery notes in Confirmed state.
- After acceptance, the delivery note becomes Delivered, but acceptance status shows whether customer received all, part, or none.

### DeliveryAcceptanceItem

Fields:
- `Id`
- `DeliveryAcceptanceId`
- `DeliveryNoteItemId`
- `ProductId`
- `ProductName`
- `DeliveredQuantity`
- `AcceptedQuantity`
- `RejectedQuantity`
- `RejectReason`
- `ReturnUnitPriceSnapshot`: internal unit price used to value the rejected quantity; hidden from customer if `ShowPrice = false`

Rules:
- `AcceptedQuantity >= 0`
- `RejectedQuantity >= 0`
- `AcceptedQuantity + RejectedQuantity == DeliveredQuantity`
- If `RejectedQuantity > 0`, `RejectReason` is required.

### DeliveryAcceptanceItemPicture

Fields:
- `Id`
- `DeliveryAcceptanceItemId`
- `PictureId`
- `CreatedOnUtc`

Rules:
- Evidence pictures are required for items with `RejectedQuantity > 0`.
- Use the same upload constraints as return request evidence: JPG/PNG/WEBP, max 3 pictures per item, max 5MB each.

## Financial Rules

Debt should be created immediately after acceptance, based on actual accepted quantities and internal prices.

Formula:

```text
Collectable goods amount = Sum(AcceptedQuantity * internal delivery item unit price)
Debt amount = Collectable goods amount + DeliveryNote.Surcharge + DeliveryAcceptance.AgreedCustomerCharge
```

Notes:
- If `ShowPrice = true`, UI may display line prices and total to admin/customer where current product behavior allows it.
- If `ShowPrice = false`, UI should display only quantities, reasons, and agreed extra charge text/amount. The backend still computes debt from internal unit prices.
- Rejected quantities at delivery time are not included in the debt amount.
- `AmountToCollect` display should use the acceptance-adjusted amount after acceptance, not full `DeliveryNote.TotalAmount`.
- Existing payments/debts should remain idempotent by `DeliveryNoteId`.

## Inventory And Return Rules

When `RejectedQuantity > 0`, create an internal `CustomerReturn` linked to the delivery acceptance.

CustomerReturn generation:
- `CustomerId`: delivery note customer
- `DeliveryNoteId`: delivery note
- Items: one item per rejected delivery note item
- `RequestedQuantity`: rejected quantity
- `AcceptedQuantity`: initially rejected quantity or 0 depending on the current return status convention; final quantity is confirmed in return confirmation
- `ReturnUnitPrice`: internal delivery item unit price by default
- `AdditionalCost`: not used for the agreed customer charge; that charge belongs to delivery acceptance/debt calculation
- `WarehouseId`: not required at creation time after this design

Warehouse selection:
- CustomerReturn creation no longer asks for receiving warehouse.
- Moving to final confirmation must require a receiving warehouse.
- For returns generated from normal delivery notes, suggest the delivery note warehouse.
- For returns generated from direct-ship delivery notes, no default warehouse is required; admin selects a warehouse at confirmation.

Inventory effect:
- Delivery stock-out remains based on the original delivery note shipped quantity.
- Returned stock-in happens only when the generated CustomerReturn is confirmed.
- This preserves the real sequence: goods left stock, customer rejected some goods, returned goods are received back after inspection.

## Admin UX

### Normal Delivery Confirmation

Replace the current single “Xác nhận Đã giao” modal with an acceptance form:
- Delivery proof picture and receiver name.
- Per-item table with delivered quantity, accepted quantity, rejected quantity.
- Reject reason and evidence upload for rows with rejected quantity.
- Agreed customer charge and reason.
- Summary: accepted goods quantity, rejected goods quantity, calculated/hidden totals according to `ShowPrice`.

Default behavior:
- All accepted quantities prefilled equal delivered quantity.
- Rejected quantity starts at 0.
- Editing accepted quantity updates rejected quantity automatically.

### Direct Ship Confirmation

Use the same acceptance form. Do not ask for warehouse here.

If any rejected quantity exists:
- Generate CustomerReturn after acceptance.
- Admin selects the receiving warehouse later when confirming the generated return.

### CustomerReturn Confirmation

Update CustomerReturn flow:
- Create/manual draft does not require a receiving warehouse.
- Inspecting/confirm flow requires warehouse before generating GoodsReceipt.
- For delivery-acceptance-generated returns from normal deliveries, prefill the original delivery note warehouse.
- For direct ship, require explicit warehouse selection.

## Customer Portal UX

On the delivery note detail page, the “Đã nhận hàng” flow becomes an acceptance form:
- Customer can confirm all received with one action.
- Customer can adjust accepted/rejected quantities.
- If rejected quantity is greater than 0, require reason and evidence picture.
- Customer can enter the agreed extra charge and reason when there is a charge agreement at delivery time. This defaults to 0 when there is no extra charge.
- Portal must not ask for a receiving warehouse.

Recommended customer-facing version:
- Customer enters quantities, rejection reasons, evidence pictures, and agreed extra charge if any.
- Backend creates the customer debt immediately after successful acceptance, using internal prices plus the submitted agreed extra charge.
- If `ShowPrice = false`, the portal still hides product prices and line totals; it may show only accepted/rejected quantities and the agreed extra charge amount.
- Any generated CustomerReturn waits for admin inspection/confirmation before selecting the receiving warehouse.

Because the user decision says debt is created immediately after acceptance, portal acceptance is a final acceptance action, not a pending financial review. If the business later needs admin approval before debt creation, that should be a separate future feature.

## Error Handling

- Reject acceptance if any item has accepted + rejected quantity different from delivered quantity.
- Reject acceptance if a rejected item has no reason.
- Reject acceptance if rejected item has no evidence picture.
- Reject duplicate acceptance for the same delivery note unless the existing acceptance is cancelled by a future explicit feature.
- Reject CustomerReturn confirmation without a receiving warehouse.
- Reject debt creation if acceptance cannot be resolved; do not mark delivery as completed without its financial/inventory side effects.

## Reporting And Display

Delivery note details should show:
- Original delivered/shipped quantity.
- Accepted quantity.
- Rejected quantity.
- Acceptance status.
- Rejection reasons and evidence pictures.
- Generated CustomerReturn link, if any.
- Adjusted debt/amount to collect.

Customer return details should show when a return was generated from delivery acceptance.

## Out Of Scope For This Phase

- New damaged/holding warehouse type.
- Vendor claim/debit automation for direct-ship rejected goods.
- Editing a finalized delivery acceptance.
- Refund/cash return workflow beyond existing customer return debt adjustment.
- Running migrations; the developer/user will run migration commands manually.

## Implementation Notes

- Existing `RETURNS_MODULE_PLAN.md` says CustomerReturn requires `DeliveryNoteId`; this remains acceptable as an internal reference, but CustomerReturn creation must not require the user/customer to choose a delivery note in the UI.
- `CustomerReturn.WarehouseId` is currently required in the entity/mapping. Implementation may require a schema change to make it nullable until confirmation, or a transitional internal placeholder approach. The preferred design is to make it nullable before confirmation because it matches the workflow cleanly.
- `DeliveryNoteDelivered` currently carries full `TotalAmount`. The handler must use acceptance-adjusted amount once acceptance exists.
- Direct-ship acceptance must share the same acceptance model as normal delivery, with only default warehouse suggestion differing later in CustomerReturn confirmation.

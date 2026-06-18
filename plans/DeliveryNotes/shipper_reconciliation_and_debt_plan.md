# Shipper Reconciliation And Delivery Debt Plan

## Goal
Fix delivery completion so hidden-price delivery notes still record the delivered goods value in customer debt, and make shipper completion wait for admin/warehouse reconciliation before the delivery note becomes Delivered.

## Assumptions
- `ShowPrice` controls display/printing only, not accounting.
- `AmountToCollect` is cash expected at delivery time.
- Customer debt/ledger charge should use delivered goods value plus delivery surcharges/approved charges.
- Admin completion may still mark a delivery note Delivered immediately.
- Shipper mobile completion should preserve proof, receiver, cash, and acceptance data, but not raise `DeliveryNoteDelivered` until admin/warehouse confirms.

## Tasks
1. Add a `PendingConfirmation` delivery note status.
   - Use value `35` between `Delivering` and `Delivered`.
   - Update status labels in admin/mobile/list views and factories.
2. Split mobile and admin completion behavior.
   - Mobile command records delivery completion evidence and moves to `PendingConfirmation`.
   - Admin command can complete from `Confirmed`, `Delivering`, or `PendingConfirmation`.
3. Fix debt amount calculation.
   - Use `TotalAmount + Surcharge + AgreedCustomerCharge` for the delivered debt amount.
   - Keep `AmountToCollect` for cash collection checks and cash handover.
4. Verify without adding tests or migrations.
   - Review compile-sensitive call sites.
   - Run build only if needed.

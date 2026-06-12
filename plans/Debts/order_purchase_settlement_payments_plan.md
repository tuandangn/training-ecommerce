# Order and PurchaseOrder Settlement Payments Plan

## Goal

Add payment actions to the Order and PurchaseOrder settlement panels using the account-based debt ledger model. Payments only record money movement and do not allocate to individual debts.

## Assumptions

- Customer and vendor debt use balance-forward ledger semantics from `plans/Debts/plan_account_based_debt_ledger.md`.
- A positive balance means the customer/vendor account still owes money. A negative balance means there is advance/overpaid money.
- Order payments support cash and store-bank QR transfer.
- PurchaseOrder payments support cash and manual bank transfer only. No QR is created for vendor payments.
- This iteration intentionally skips new tests per user request.
- Build is run only when needed to validate compile after implementation.

## Scope

1. Order settlement panel
   - Show customer account balance status: remaining debt, settled, or customer advance.
   - Add a payment modal with Cash and QR options.
   - Cash payment records one `CustomerPayment` with `OrderId`.
   - QR payment creates a `BankTransferPaymentIntent`; a confirmed intent records one `CustomerPayment` and consumes the intent.
   - Timeline shows customer payments related to the order.

2. PurchaseOrder settlement panel
   - Show vendor account balance status: remaining payable, settled, or vendor advance.
   - Add a payment modal with Cash and Bank Transfer options.
   - Vendor payment records one `VendorPayment` with `PurchaseOrderId`.
   - Timeline shows vendor payments related to the purchase order.

3. Existing behavior preserved
   - No FIFO allocation.
   - Existing debt and ledger details pages stay unchanged.
   - Existing FastSale QR flow stays unchanged.

## Not In Scope

- New database migrations.
- New QR flow for paying vendors.
- Changing the global debt ledger cutover plan.
- Writing automated tests in this iteration.

## Success Criteria

- Admin can record cash payment from Order settlement.
- Admin can create and confirm QR payment from Order settlement; only confirmed intents create payments.
- Admin can record cash/manual transfer payment from PurchaseOrder settlement.
- Settlement panels clearly show current account balance state before payment.
- Timeline includes payment nodes for Order and PurchaseOrder.

# Fast Sale Fulfillment And Payment Modes Plan

## Goal
Improve `Order/FastCreate` so counter staff can choose delivery timing and payment timing independently without breaking inventory or customer accounting.

## Assumptions
- Default behavior remains current quick sale: deliver now and pay now.
- "Chua giao hang" creates an order and keeps order-level reservation, but skips available-stock validation and does not create delivery notes or stock outbound movements.
- "Chua giao hang + thanh toan ngay" records a customer deposit linked to the order. When a delivery note is completed later, existing debt logic applies the deposit to that delivery debt.
- "Giao hang ngay + chua thanh toan" creates delivery note and customer debt, but no customer payment.
- Tests are skipped per user instruction; verification is `dotnet build`.

## Tasks
- [x] Add quick sale fulfillment/payment mode enums and DTO validation.
- [x] Allow order creation to skip available-stock validation while keeping order reservation.
- [x] Update fast sale application service to branch records by delivery/payment mode.
- [x] Allow bank transfer payment intents to be consumed by deposit-only sales.
- [x] Add unpaid fast sale command/endpoint and map new mode fields.
- [x] Update `Order/FastCreate` UI and `FastSale.js` behavior.
- [x] Run build verification and record implementation notes.

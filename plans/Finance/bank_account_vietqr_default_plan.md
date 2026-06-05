# Bank Account VietQR Default Plan

## Goal
Use `BankAccount` as the primary source for VietQR receiving-account data, while keeping `Payments:BankTransfer` as fallback and seed source.

## Assumptions
- `Code` stays as the existing internal bank-account code such as `NH-001`.
- `BankCode` is the user-entered VietQR bank code/id and maps to `Payments:BankTransfer:BankId`.
- `Payments:BankTransfer:Enabled` still controls whether bank-transfer QR is available.
- If there are no active bank accounts and appsettings has full `BankId`, `AccountNo`, and `AccountName`, the system creates one active default `BankAccount`.

## Todo
- [x] Add required `BankCode` to `BankAccount`, finance DTOs, commands, manager, app service, EF mapping, and accounting UI.
- [x] Add a receiving-account resolver that returns active default `BankAccount` first.
- [x] In the resolver, auto-create a default `BankAccount` from appsettings only when there are no bank accounts and appsettings is complete.
- [x] Use the resolver in `BankTransferPaymentIntentAppService.CreateAsync`.
- [x] Use the resolver in `FastSaleModelFactory` for the bank account label and bank-transfer availability.
- [x] Add/update EF migration metadata for the new `BankCode` column.
- [x] Verify JavaScript syntax and web build.

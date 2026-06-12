# Order and PurchaseOrder Settlement Payments Implementation

## TodoList

- [x] Add payment result/command models for settlement payments.
- [x] Add handlers for Order cash payment, Order QR payment recording, and PurchaseOrder vendor payment.
- [x] Add controller actions with debt payment permissions.
- [x] Extend Order details model and factory with ledger account balance and order-related payment rows.
- [x] Extend PurchaseOrder details model and factory with vendor ledger balance and purchase-order payment rows.
- [x] Update Order settlement Razor partial with balance card, payment modal, and QR panel.
- [x] Update PurchaseOrder settlement Razor partial with balance card and payment modal.
- [x] Update Order details JavaScript for cash submit, QR create/status/confirm/record.
- [x] Use normal PurchaseOrder form post for vendor payment submit.
- [x] Update timeline builders to include payment nodes.
- [x] Build only if implementation changes require compile validation.

## Implementation Notes

- Use `ICustomerLedgerManager.GetBalanceAsync` and `IVendorLedgerManager.GetBalanceAsync` for current debt/advance state.
- Use `CustomerDebtAppService.RecordPaymentAsync` for Order settlement payments with `OrderId` set and `PaymentType = PaymentType.DebtPayment`.
- Use `IBankTransferPaymentIntentAppService` or the existing intent commands for QR creation/status/manual confirm.
- Add a dedicated action for recording a confirmed Order QR intent so no new order is created.
- Use `IVendorDebtAppService.RecordPaymentAsync` for PurchaseOrder settlement payments with `PurchaseOrderId` set and `PaymentType = PaymentType.VendorDebtPayment`.
- Keep controllers thin: controller sends MediatR command, returns JSON for Order, and redirects with notification for PurchaseOrder form post.
- Keep Razor free of inline style blocks and new `style=""` attributes.
- Reuse Bootstrap and existing `workflow-*` classes where possible.

## Verification

- `dotnet build NamEcommerce/Presentation/NamEcommerce.Web/NamEcommerce.Web.csproj` was blocked by a running `NamEcommerce.Web` process locking files in `bin`/`obj`.
- `dotnet build NamEcommerce/Presentation/NamEcommerce.Web/NamEcommerce.Web.csproj -p:DebugType=none -p:OutDir="D:\Learning\NamTraining\training-ecommerce\.codex-build\web\"` passed with existing repository warnings.
- `powershell -ExecutionPolicy Bypass -File tools/ui-lint.ps1` passed with `styleBlockCount=0/0 inlineStyleCount=0/0`.

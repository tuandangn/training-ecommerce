# Accounting Module — Implementation Index

## Thứ tự implement

```
Sprint 1: Field additions (đơn giản nhất, ít rủi ro)
Sprint 2: AccountingSetup + BankAccount (foundation)
Sprint 3: FixedAsset
Sprint 4: CashBook service + UI
Sprint 5: B02 Income Statement
Sprint 6: B03 Cash Flow + B01 Balance Sheet
Sprint 7: UI polish (print, compare periods)
```

---

## Files spec

| File | Nội dung | Sprint |
|---|---|---|
| [implement_field_additions.md](implement_field_additions.md) | PRE-2 IsOpeningBalance, PRE-3 Discount, PRE-4 VAT, PRE-5 Invoice numbers | 1 |
| [implement_accounting_setup.md](implement_accounting_setup.md) | AccountingSetup entity → manager → appservice → controller → view | 2 |
| [implement_bank_account.md](implement_bank_account.md) | BankAccount entity → link payments → UI | 2 |
| [implement_fixed_asset.md](implement_fixed_asset.md) | FixedAsset entity + depreciation + disposal event | 3 |
| [implement_report_service.md](implement_report_service.md) | ICashBookService + IAccountingReportService + B02/B03/B01 DTOs + Views | 4–7 |

---

## Migration order

```
1.  AddAccountingSetupTable
2.  AddBankAccountTable
3.  AddFixedAssetTable
4.  AddIsOpeningBalanceToDebts
5.  AddBankAccountIdToPayments
6.  AddDiscountAndTaxToDeliveryNote
7.  AddTaxAndVendorInvoiceToGoodsReceipt
8.  AddTaxPaymentMethodBankAccountToExpense
9.  AddInvoiceFieldsToDocuments
10. AddTaxFieldsToPurchaseOrderItems   (optional)
```

---

## TODOs còn để `NotImplementedException` / `TODO` trong spec

| File | Dòng | Nội dung |
|---|---|---|
| implement_report_service.md | CashBookService.GetCashBookAsync | Build lines + running balance |
| implement_report_service.md | B03 InventoryChange | Tính HTK đầu kỳ từ cost ledger snapshot |
| implement_report_service.md | B03 VendorReturn COGS adj | Map VendorReturn items → cost entries |
| implement_report_service.md | B01 RetainedEarnings | Support arbitrary date range cho ComputeCumulativeNetProfit |

Các TODO này ổn để defer sang sau khi core reports chạy được. Bắt đầu với B02 (đơn giản nhất), sau đó B03, B01.

---

## InternalsVisibleTo cần bổ sung

File `NamEcommerce.Domain/Accessibility/AssemblyAccessibility.cs` — kiểm tra đã có chưa:
```csharp
[assembly: InternalsVisibleTo("NamEcommerce.Domain.Services")]
```

---

## DI Registration cần thêm

**NamEcommerce.Application.Services** — `ServiceCollectionExtensions.cs`:
```csharp
services.AddScoped<IAccountingSetupAppService, AccountingSetupAppService>();
services.AddScoped<IBankAccountAppService, BankAccountAppService>();
services.AddScoped<IFixedAssetAppService, FixedAssetAppService>();
services.AddScoped<ICashBookService, CashBookService>();
services.AddScoped<IAccountingReportService, AccountingReportService>();
```

**NamEcommerce.Domain.Services** — `ServiceCollectionExtensions.cs`:
```csharp
services.AddScoped<IAccountingSetupManager, AccountingSetupManager>();
services.AddScoped<IBankAccountManager, BankAccountManager>();
services.AddScoped<IFixedAssetManager, FixedAssetManager>();
```

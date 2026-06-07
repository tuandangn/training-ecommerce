# Kế hoạch Hoàn thiện Hệ thống cho Module Kế toán

**Mục tiêu:** Xuất 3 báo cáo tài chính theo **Thông tư 200/2014/TT-BTC** (VAS) cho cửa hàng bán lẻ vật liệu xây dựng:
- **B01-DN** — Bảng cân đối kế toán
- **B02-DN** — Báo cáo kết quả hoạt động kinh doanh
- **B03-DN** — Báo cáo lưu chuyển tiền tệ (phương pháp gián tiếp)

**Xác nhận từ người dùng:**
- ✅ Có dùng chiết khấu thương mại
- ✅ Có nhiều tài khoản ngân hàng → cần entity `BankAccount`
- ✅ Đăng ký khai thuế GTGT
- ✅ Có TSCĐ (xe tải, kệ, máy móc) → cần entity `FixedAsset` + khấu hao

---

## PHẦN I — Gap Analysis toàn diện

### 1.1 Đã có ✅

| Thành phần kế toán | Entity / Nguồn dữ liệu |
|---|---|
| Doanh thu | `DeliveryNote` (Delivered) + `DeliveryNoteItem.SubTotal` |
| Giá vốn hàng bán | `InventoryCostLedgerEntry` (Dispatch, trong kỳ) |
| Chi phí hoạt động | `Expense` (6 loại: Payroll, Rent, Marketing, Utilities, General, ReturnCost) |
| Ngân sách chi phí | `ExpenseBudget` (Year + Month + ExpenseType + Amount) |
| Phải thu KH | `CustomerDebt` + `CustomerPayment` |
| Phải trả NCC | `VendorDebt` + `VendorPayment` |
| Phiếu giảm trừ KH | `CustomerCreditNote` (từ trả hàng) — entity đã có ✅ |
| Hoàn tiền KH | `CustomerRefund` (tiền mặt/CK) — entity đã có ✅ |
| Hàng tồn kho | `InventoryStock` × `AverageCost` (WAVG/FIFO) |
| Hoàn trả NCC | `VendorReturn` + `VendorCreditNote` |
| Phân biệt thanh toán KH | `CustomerPayment.PaymentMethod` (Cash/BankTransfer/COD) ✅ |
| Phân biệt thanh toán NCC | `VendorPayment.PaymentMethod` ✅ |
| Phân biệt hoàn tiền KH | `CustomerRefund.PaymentMethod` ✅ |
| Số dư đầu kỳ KH | `CustomerDebt` constructor không có DeliveryNoteId ✅ |
| Số dư đầu kỳ NCC | `VendorDebt` constructor không có GoodsReceiptId ✅ |
| Hàng tồn kho đầu kỳ | `GoodsReceipt(OpeningBalance)` ✅ |

---

### 1.2 Thiếu / Cần bổ sung ❌

#### 🔴 Critical — báo cáo SAI hoặc KHÔNG CÂN nếu thiếu

| # | Thiếu | Ảnh hưởng | Entity cần sửa/tạo |
|---|---|---|---|
| C1 | **Kỳ kế toán** (fiscal year start) | B01/B02/B03 không xác định được "kỳ" | `AccountingSetup` *(tạo mới)* |
| C2 | **Tiền đầu kỳ — tách TK111 / TK112 per account** | B03 sai số dư mở/đóng; không biết tiền nằm ở NH nào | `AccountingSetup.OpeningCash` + `BankAccount.OpeningBalance` |
| C3 | **Vốn chủ sở hữu đầu kỳ** | B01 không cân | `AccountingSetup.OpeningEquity` |
| C4 | **Thuế GTGT đầu ra** | B02 doanh thu thuần sai; TK 3331 sai | `DeliveryNoteItem.TaxRate` + `TaxAmount` |
| C5 | **Thuế GTGT đầu vào** | Thuế GTGT phải nộp sai | `GoodsReceiptItem.TaxRate` + `TaxAmount` |
| C6 | **`IsOpeningBalance` flag trên Debt** | B01 không phân biệt số dư đầu kỳ vs. phát sinh | `CustomerDebt`, `VendorDebt` |
| C7 | **`CustomerCreditNote` trong công thức** | B01 Phải thu cao hơn thực tế; B02 doanh thu không giảm | Tính vào công thức báo cáo |
| C8 | **`CustomerRefund` trong B03** | Cash flow thiếu khoản hoàn tiền KH | Tính vào Cash Book |
| C9 | **Chiết khấu thương mại (TK 521)** | B02 không có dòng "Giảm trừ doanh thu" | `DeliveryNote` + `DeliveryNoteItem` |
| C10 | **`BankAccount` entity** (nhiều TK NH) | Không biết tiền ở đâu; B01 TK112 per bank; B03 cash flow sai | `BankAccount` *(tạo mới)* + link payments |
| C11 | **TSCĐ & Khấu hao** | B01 thiếu tài sản dài hạn; B02 thiếu chi phí KH; B03 không adjust | `FixedAsset` + `FixedAssetDepreciationEntry` *(tạo mới)* |

#### 🟡 High — ảnh hưởng độ chính xác

| # | Thiếu | Ảnh hưởng |
|---|---|---|
| H1 | `PaymentMethod` + `BankAccountId` trên `Expense` | B03 không tách chi phí tiền mặt vs. ngân hàng |
| H2 | Thuế GTGT trên chi phí | Thuế đầu vào trên chi phí bị thiếu |
| H3 | `VendorCreditNote` trong công thức | B01 Phải trả NCC không chính xác |
| H4 | Điều chỉnh COGS cho VendorReturn | B02 giá vốn hơi cao |
| H5 | Thuế GTGT trên PurchaseOrder | Tracking VAT từ giai đoạn đặt hàng |

#### 🔵 Medium — compliance & audit

| # | Thiếu |
|---|---|
| M1 | Số hóa đơn GTGT trên `DeliveryNote` |
| M2 | Số hóa đơn NCC trên `GoodsReceipt` |
| M3 | Thuế TNDN (TK 821) — manual provision |

---

### 1.3 Kết luận

**Dữ liệu hiện tại CHƯA đủ.** Phải implement các pre-requisites trước khi build UI kế toán.
Scope mở rộng so với plan cũ vì: nhiều TK ngân hàng + TSCĐ + thuế GTGT đầy đủ.

---

## PHẦN II — Cải thiện hệ thống (Pre-requisites)

---

### PRE-1 — `AccountingSetup` entity *(tạo mới)*

**Module:** Finance / Accounting — Singleton entity

| Field | Type | Ý nghĩa |
|---|---|---|
| `FiscalYearStartMonth` | int (1–12) | Tháng bắt đầu năm tài chính (default: 1) |
| `FiscalYearStartDay` | int | Ngày bắt đầu (default: 1) |
| `AccountingStartDate` | DateTime | Ngày bắt đầu dùng hệ thống |
| `OpeningCash` | decimal | Tiền mặt quỹ đầu kỳ — **TK 111** |
| `OpeningEquity` | decimal | Vốn chủ sở hữu đầu kỳ — TK 411 + 421 |
| `DefaultTaxRate` | decimal | Thuế GTGT mặc định (0.10) |
| `CorporateTaxProvision` | decimal? | Ước tính thuế TNDN kỳ (manual) |
| `IsFinalized` | bool | Khóa sau khi xác nhận |
| `FinalizedOnUtc` | DateTime? | |
| `CreatedOnUtc` | DateTime | |

> **Lưu ý:** `OpeningBankDeposit` KHÔNG có ở đây. Mỗi TK ngân hàng có `OpeningBalance` riêng trong entity `BankAccount` (PRE-6).

---

### PRE-2 — `IsOpeningBalance` trên Debt entities

Thêm `bool IsOpeningBalance` vào `CustomerDebt` và `VendorDebt`:
- Tự động set = `true` khi tạo bằng constructor số dư đầu kỳ (không có `DeliveryNoteId`/`GoodsReceiptId`)

**Migration:** `AddIsOpeningBalanceToDebts`

---

### PRE-3 — Chiết khấu thương mại trên `DeliveryNote` (TK 521)

Thêm vào `DeliveryNoteItem`:
```
DiscountPercent   decimal?   (%, vd: 5.0 = 5%)
DiscountAmount    decimal    (= SubTotal × DiscountPercent hoặc nhập thẳng)
NetAmount         decimal    (= SubTotal - DiscountAmount)
```

Thêm vào `DeliveryNote`:
```
TotalDiscountAmount  decimal
```

**UI:** Thêm cột "Chiết khấu" per item khi tạo/xác nhận phiếu xuất kho.

**Migration:** `AddDiscountFieldsToDeliveryNote`

---

### PRE-4 — Thuế GTGT trên các giao dịch

#### 4a — Bán hàng: `DeliveryNoteItem` + `DeliveryNote`
```
TaxRate    decimal?   (0 / 0.05 / 0.08 / 0.10)
TaxAmount  decimal    (= NetAmount × TaxRate)
```
`DeliveryNote.TotalTaxAmount decimal`

#### 4b — Mua hàng: `GoodsReceiptItem` + `GoodsReceipt`
```
TaxRate    decimal?
TaxAmount  decimal
```
`GoodsReceipt.TotalTaxAmount decimal`

#### 4c — Chi phí: `Expense`
```
TaxRate             decimal?
TaxAmount           decimal
AmountExcludingTax  decimal    (= Amount - TaxAmount)
PaymentMethod       PaymentMethod?   ← giải quyết H1
BankAccountId       Guid?            ← liên kết TK NH (PRE-6)
```

#### 4d — Đơn nhập hàng: `PurchaseOrderItem` *(optional)*
```
TaxRate    decimal?
TaxAmount  decimal
```

**Migrations:**
- `AddTaxAndDiscountToDeliveryNote`
- `AddTaxFieldsToGoodsReceipt`
- `AddTaxPaymentMethodToExpense`
- `AddTaxFieldsToPurchaseOrder` *(optional)*

---

### PRE-5 — Số hóa đơn GTGT

Thêm vào `DeliveryNote`:
```
InvoiceNumber   string?    (số hóa đơn, vd: "0000123")
InvoiceSeries   string?    (ký hiệu, vd: "AA/24E")
InvoiceDate     DateTime?
```

Thêm vào `GoodsReceipt`:
```
VendorInvoiceNumber  string?
VendorInvoiceDate    DateTime?
```

**Migration:** `AddInvoiceFieldsToDocuments`

---

### PRE-6 — `BankAccount` entity *(tạo mới — quan trọng)*

**Module:** Finance

```csharp
BankAccount
  Id                Guid
  Code              string         // "VCB-001", "TCB-001"
  DisplayName       string         // "Vietcombank - CN Quận 1"
  BankName          string         // "Vietcombank"
  AccountNumber     string         // "1234567890"
  AccountHolderName string
  OpeningBalance    decimal        // Số dư đầu kỳ — TK 112 per account
  IsDefault         bool           // Tài khoản mặc định khi chuyển khoản
  IsActive          bool
  CreatedOnUtc      DateTime
```

**Link `BankAccountId` vào các entities thanh toán:**

| Entity | Field thêm |
|---|---|
| `CustomerPayment` | `BankAccountId Guid?` (khi PaymentMethod = BankTransfer/COD) |
| `VendorPayment` | `BankAccountId Guid?` |
| `CustomerRefund` | `BankAccountId Guid?` (khi PaymentMethod = BankTransfer) |
| `Expense` | `BankAccountId Guid?` (khi PaymentMethod = BankTransfer) |

**Công thức số dư từng TK ngân hàng:**
```
Balance(TK NH X, đến ngày T) =
  BankAccount[X].OpeningBalance
  + SUM(CustomerPayment.Amount   WHERE BankAccountId=X AND PaidOnUtc<=T)
  - SUM(CustomerRefund.Amount    WHERE BankAccountId=X AND RefundedOnUtc<=T AND Completed)
  - SUM(VendorPayment.Amount     WHERE BankAccountId=X AND PaidOnUtc<=T)
  - SUM(Expense.AmountExcludingTax WHERE BankAccountId=X AND IncurredDate<=T)

TK 112 tổng = SUM over all active BankAccounts
```

**Migrations:**
- `AddBankAccountTable`
- `AddBankAccountIdToPayments`

---

### PRE-7 — `FixedAsset` & `FixedAssetDepreciationEntry` *(tạo mới)*

**Module:** Finance / Assets

#### Entity `FixedAsset`

```csharp
FixedAsset
  Id                    Guid
  Code                  string            // "TSCĐ-001"
  Name                  string            // "Xe tải 1.5T"
  Description           string?
  Category              FixedAssetCategory enum
                          // Vehicle | Equipment | FurnitureAndFixtures | Computer | Other
  AcquisitionDate       DateTime
  AcquisitionCost       decimal           // Nguyên giá — TK 211
  ResidualValue         decimal           // Giá trị thu hồi ước tính
  UsefulLifeMonths      int               // Thời gian sử dụng (tháng)
  DepreciationMethod    DepreciationMethod enum  // StraightLine (đường thẳng)
  VendorId              Guid?
  VendorInvoiceNumber   string?
  Note                  string?
  Status                FixedAssetStatus  // Active | FullyDepreciated | Disposed
  DisposedOnUtc         DateTime?
  CreatedOnUtc          DateTime
```

**Tính khấu hao tháng:**
```
MonthlyDepreciation = (AcquisitionCost - ResidualValue) / UsefulLifeMonths
AccumulatedDepreciation(đến kỳ T) = MonthlyDepreciation × Min(months_elapsed, UsefulLifeMonths)
BookValue = AcquisitionCost - AccumulatedDepreciation
```

> **Lựa chọn thiết kế:** Không lưu `DepreciationEntry` riêng vì phương pháp đường thẳng tính được thuần túy từ `AcquisitionDate` + `UsefulLifeMonths` + `MonthlyDepreciation`. Chỉ lưu khi cần audit trail hoặc điều chỉnh.

**Migrations:**
- `AddFixedAssetTable`

---

### Tổng hợp tất cả entities cần tạo/sửa

| Entity | Action | Fields mới |
|---|---|---|
| `AccountingSetup` | **TẠO MỚI** | FiscalYearStartMonth, AccountingStartDate, OpeningCash, OpeningEquity, DefaultTaxRate, CorporateTaxProvision, IsFinalized |
| `BankAccount` | **TẠO MỚI** | Code, DisplayName, BankName, AccountNumber, AccountHolderName, OpeningBalance, IsDefault, IsActive |
| `FixedAsset` | **TẠO MỚI** | Code, Name, Category, AcquisitionDate, AcquisitionCost, ResidualValue, UsefulLifeMonths, DepreciationMethod, Status |
| `CustomerDebt` | Sửa | `IsOpeningBalance bool` |
| `VendorDebt` | Sửa | `IsOpeningBalance bool` |
| `CustomerPayment` | Sửa | `BankAccountId Guid?` |
| `VendorPayment` | Sửa | `BankAccountId Guid?` |
| `CustomerRefund` | Sửa | `BankAccountId Guid?` |
| `DeliveryNoteItem` | Sửa | `DiscountPercent decimal?`, `DiscountAmount decimal`, `TaxRate decimal?`, `TaxAmount decimal` |
| `DeliveryNote` | Sửa | `TotalDiscountAmount decimal`, `TotalTaxAmount decimal`, `InvoiceNumber string?`, `InvoiceSeries string?`, `InvoiceDate DateTime?` |
| `GoodsReceiptItem` | Sửa | `TaxRate decimal?`, `TaxAmount decimal` |
| `GoodsReceipt` | Sửa | `TotalTaxAmount decimal`, `VendorInvoiceNumber string?`, `VendorInvoiceDate DateTime?` |
| `Expense` | Sửa | `TaxRate decimal?`, `TaxAmount decimal`, `AmountExcludingTax decimal`, `PaymentMethod PaymentMethod?`, `BankAccountId Guid?` |
| `PurchaseOrderItem` | Sửa *(optional)* | `TaxRate decimal?`, `TaxAmount decimal` |

### Danh sách migrations (thứ tự)

```
1. AddAccountingSetup
2. AddBankAccountTable
3. AddFixedAssetTable
4. AddIsOpeningBalanceToDebts
5. AddBankAccountIdToPayments          ← CustomerPayment, VendorPayment, CustomerRefund
6. AddDiscountAndTaxToDeliveryNote
7. AddTaxFieldsToGoodsReceipt
8. AddTaxPaymentMethodBankAccountToExpense
9. AddInvoiceFieldsToDocuments
10. AddTaxFieldsToPurchaseOrder        ← optional
```

---

## PHẦN III — Module Kế toán (6 Phases)

---

### PHASE 1 — Khai báo đầu kỳ & Quản lý Tài khoản NH

#### 1a — `/Accounting/Setup`
- Form nhập `AccountingSetup`: năm tài chính, tiền mặt quỹ đầu kỳ, vốn CSH, thuế mặc định, ước tính TNDN
- Sau `IsFinalized = true` → khóa, chỉ xem
- Hướng dẫn: "Nợ KH/NCC đầu kỳ → Công nợ | HTK đầu kỳ → Nhập hàng | TK NH → Mục dưới"

#### 1b — `/Accounting/BankAccounts`
- Danh sách tài khoản ngân hàng: thêm, sửa, đặt mặc định, ẩn
- Mỗi TK: Tên ngân hàng, Số TK, Chủ TK, **Số dư đầu kỳ**
- Widget "Số dư hiện tại" (real-time từ công thức)

#### 1c — `/Accounting/FixedAssets`
- Danh sách TSCĐ: thêm, sửa, thanh lý
- Mỗi TSCĐ: Tên, Loại, Ngày mua, Nguyên giá, Thời gian KH, Phương pháp KH
- Bảng "Lịch khấu hao" (12 tháng × năm còn lại)
- Tổng: Nguyên giá | KH lũy kế | Giá trị còn lại

---

### PHASE 2 — Sổ quỹ tiền mặt & ngân hàng (`/Accounting/CashBook`)

**TK 111 — Tiền mặt:**
```
Số dư TK111 =
  AccountingSetup.OpeningCash
  + SUM(CustomerPayment WHERE PaymentMethod=Cash AND PaidOnUtc<=T)
  - SUM(CustomerRefund WHERE PaymentMethod=Cash AND Completed AND RefundedOnUtc<=T)
  - SUM(VendorPayment WHERE PaymentMethod=Cash AND PaidOnUtc<=T)
  - SUM(Expense.AmountExcludingTax WHERE PaymentMethod=Cash AND IncurredDate<=T)
```

**TK 112 — Ngân hàng (per account):**
```
Số dư NH[X] =
  BankAccount[X].OpeningBalance
  + SUM(CustomerPayment WHERE BankAccountId=X AND PaidOnUtc<=T)
  - SUM(CustomerRefund WHERE BankAccountId=X AND Completed AND RefundedOnUtc<=T)
  - SUM(VendorPayment WHERE BankAccountId=X AND PaidOnUtc<=T)
  - SUM(Expense.AmountExcludingTax WHERE BankAccountId=X AND IncurredDate<=T)

TK112 tổng = SUM(Số dư NH[X]) for all active BankAccounts
```

**UI:**
- Filter: Kỳ | Loại tài khoản (TK111 / từng TK NH / Tổng)
- Bảng: Ngày | Diễn giải | Loại | Tiền vào | Tiền ra | Số dư lũy kế
- Widget tóm tắt: Tiền mặt | Ngân hàng (từng TK) | Tổng

---

### PHASE 3 — B02-DN: Kết quả hoạt động kinh doanh

```
MÃ | CHỈ TIÊU                                          | NGUỒN DỮ LIỆU
----+----------------------------------------------------+----------------------------------------
 01 | Doanh thu bán hàng và cung cấp DV                | SUM(DeliveryNoteItem.SubTotal) [Delivered, trong kỳ]
 02 | Các khoản giảm trừ doanh thu:
    |   Chiết khấu thương mại (TK 521)                  | SUM(DeliveryNote.TotalDiscountAmount) [trong kỳ]
    |   Hàng bán bị trả lại (TK 531)                   | SUM(CustomerCreditNote.Amount) [trong kỳ]
 10 | Doanh thu thuần (01 - 02)                         | Tính toán
 11 | Giá vốn hàng bán (TK 632)                        | SUM(InventoryCostLedgerEntry[Dispatch].TotalCost) [trong kỳ]
    |   Điều chỉnh: Hàng trả NCC                       | - SUM(VendorReturn confirmed cost) [trong kỳ]
 20 | Lợi nhuận gộp (10 - 11)                          | Tính toán
 21 | Doanh thu hoạt động tài chính                    | 0
 22 | Chi phí tài chính                                 | 0
 25 | Chi phí bán hàng (TK 641)                        | SUM(Expense[Marketing, ReturnCost].AmountExcludingTax) [trong kỳ]
    |   + Khấu hao TSCĐ bán hàng                       | SUM(MonthlyDepreciation cho TSCĐ loại Vehicle/Equipment) [trong kỳ]
 26 | Chi phí quản lý doanh nghiệp (TK 642)            | SUM(Expense[Payroll, Rent, Utilities, General].AmountExcludingTax) [trong kỳ]
    |   + Khấu hao TSCĐ QLDN                           | SUM(MonthlyDepreciation cho TSCĐ loại FurnitureAndFixtures/Computer/Other) [trong kỳ]
 30 | Lợi nhuận thuần từ HĐKD (20 + 21 - 22 - 25 - 26)| Tính toán
 40 | Lợi nhuận khác                                    | 0
 50 | Tổng LNKT trước thuế (30 + 40)                   | Tính toán
 51 | Thuế TNDN phải nộp (TK 821)                      | AccountingSetup.CorporateTaxProvision (manual) hoặc = LNTT × 20%
 60 | Lợi nhuận sau thuế TNDN (50 - 51)               | Tính toán
```

---

### PHASE 4 — B03-DN: Lưu chuyển tiền tệ (phương pháp gián tiếp)

```
I. LƯU CHUYỂN TIỀN TỪ HOẠT ĐỘNG KINH DOANH

  1. Lợi nhuận trước thuế                              [B02 dòng 50]

  2. Điều chỉnh các khoản phi tiền mặt:
     + Khấu hao TSCĐ                                   [tổng khấu hao kỳ — cộng lại vì là chi phí không tiền mặt]
       = SUM(MonthlyDepreciation × tháng trong kỳ, tất cả FixedAsset Active)

  3. Thay đổi vốn lưu động:
     + Tăng/giảm Phải thu KH (TK 131)
       AR_end = SUM(CustomerDebt.RemainingAmount [Outstanding/Partial, đến cuối kỳ])
              - SUM(CustomerCreditNote.RemainingAmount [Unapplied/Partial, đến cuối kỳ])
       AR_beg = Tương tự tại ngày đầu kỳ
       Delta = AR_end - AR_beg  (tăng → dấu âm; giảm → dấu dương)

     + Tăng/giảm Phải trả NCC (TK 331)
       AP_end = SUM(VendorDebt.RemainingAmount) - SUM(VendorCreditNote.RemainingAmount)
       Delta = AP_end - AP_beg  (tăng → dương; giảm → âm)

     + Tăng/giảm Hàng tồn kho (TK 156)
       INV_end = SUM(InventoryStock.Quantity × AverageCost) [cuối kỳ]
       Delta = INV_end - INV_beg  (tăng → âm; giảm → dương)

     + Tăng/giảm Thuế GTGT phải nộp (TK 3331)
       VAT_payable = SUM(DeliveryNote.TotalTaxAmount) - SUM(GoodsReceipt.TotalTaxAmount) - SUM(Expense.TaxAmount)

     - Hoàn tiền KH (TK 111/112 ra):
       = SUM(CustomerRefund.Amount WHERE Status=Completed AND RefundedOnUtc in kỳ)

= Lưu chuyển tiền thuần từ HĐKD

II. LƯU CHUYỂN TIỀN TỪ HOẠT ĐỘNG ĐẦU TƯ
     - Mua sắm TSCĐ:
       = SUM(FixedAsset.AcquisitionCost WHERE AcquisitionDate in kỳ) × (-1)
     + Thanh lý TSCĐ:
       = 0 (chưa tracking tiền thu thanh lý — Phase sau)
= Lưu chuyển tiền thuần từ HĐĐT

III. LƯU CHUYỂN TIỀN TỪ HOẠT ĐỘNG TÀI CHÍNH = 0 (không có vay dài hạn)

IV. Tăng/giảm tiền thuần trong kỳ (I + II + III)

V. Tiền đầu kỳ
   Kỳ đầu tiên: AccountingSetup.OpeningCash + SUM(BankAccount.OpeningBalance)
   Kỳ sau:      Tiền cuối kỳ trước

VI. Tiền cuối kỳ (IV + V)
    [Phải khớp với: TK111 + TK112 từ Sổ quỹ Phase 2]
```

---

### PHASE 5 — B01-DN: Bảng cân đối kế toán

```
TÀI SẢN

A. TÀI SẢN NGẮN HẠN

  I. Tiền và tương đương tiền
     TK 111 — Tiền mặt quỹ         = Số dư TK111 từ Phase 2 tại thời điểm BC
     TK 112 — Tiền gửi ngân hàng   = Tổng số dư TK112 từ Phase 2 (hiển thị chi tiết per bank)

  II. Phải thu ngắn hạn
     TK 131 — Phải thu KH          = SUM(CustomerDebt.RemainingAmount WHERE Status IN [Outstanding, PartiallyPaid])
                                     - SUM(CustomerCreditNote.RemainingAmount WHERE Status IN [Unapplied, PartiallyApplied])
             [loại trừ các debt có IsOpeningBalance nếu đã qua AccStartDate + 1 year]

  III. Hàng tồn kho
     TK 156                        = SUM(InventoryStock.Quantity × AverageCost) per warehouse
                                     [hiển thị chi tiết per warehouse, tổng B01]

  IV. Tài sản ngắn hạn khác        = 0

B. TÀI SẢN DÀI HẠN

  II. Tài sản cố định
     TK 211 — Nguyên giá           = SUM(FixedAsset.AcquisitionCost WHERE Status != Disposed)
     TK 214 — KH lũy kế (-)        = SUM(AccumulatedDepreciation tại thời điểm BC cho từng FixedAsset)
     Giá trị còn lại               = TK211 - TK214

TỔNG TÀI SẢN

---

NGUỒN VỐN

A. NỢ PHẢI TRẢ

  I. Nợ ngắn hạn
     TK 331 — Phải trả NCC         = SUM(VendorDebt.RemainingAmount WHERE Status IN [Outstanding, PartiallyPaid])
                                     - SUM(VendorCreditNote.RemainingAmount WHERE Status IN [Unapplied, PartiallyApplied])

     TK 3331 — Thuế GTGT phải nộp = VAT lũy kế từ AccountingStartDate đến thời điểm BC:
                                     SUM(DeliveryNote.TotalTaxAmount [Delivered])
                                     - SUM(GoodsReceipt.TotalTaxAmount)
                                     - SUM(Expense.TaxAmount)

     TK 3334 — Thuế TNDN          = AccountingSetup.CorporateTaxProvision

B. VỐN CHỦ SỞ HỮU

     TK 411 — Vốn góp              = AccountingSetup.OpeningEquity

     TK 421 — LNST chưa phân phối = SUM(LN sau thuế B02 từng kỳ, lũy kế từ AccountingStartDate)

TỔNG NGUỒN VỐN  (= TỔNG TÀI SẢN — kiểm tra cân bằng)
```

**Kiểm tra cân bằng:** B01 phải cân `Tổng Tài sản = Tổng Nguồn vốn`. Nếu lệch → báo lỗi với dòng nguyên nhân.

---

### PHASE 6 — UI tổng hợp & xuất báo cáo

| URL | Tên trang |
|---|---|
| `/Accounting/Setup` | Khai báo kế toán đầu kỳ |
| `/Accounting/BankAccounts` | Quản lý tài khoản ngân hàng |
| `/Accounting/FixedAssets` | Quản lý tài sản cố định |
| `/Accounting/CashBook` | Sổ quỹ tiền mặt & ngân hàng |
| `/Accounting/IncomeStatement` | B02 — Kết quả HĐKD |
| `/Accounting/CashFlow` | B03 — Lưu chuyển tiền tệ |
| `/Accounting/BalanceSheet` | B01 — Bảng cân đối kế toán |

**Chức năng chung trên tất cả báo cáo:**
- Chọn kỳ (tháng / quý / năm tài chính)
- So sánh kỳ này vs. kỳ trước (B01 bắt buộc theo TT200)
- Nút **In** (CSS @media print — không cần PDF library)
- Nút **Xuất Excel** (Phase 7 — sau)
- Hiển thị cảnh báo nếu B01 không cân

---

## PHẦN IV — Thứ tự triển khai & Dependencies

```
PRE-1 (AccountingSetup)
PRE-2 (IsOpeningBalance)       ← độc lập, có thể song song
PRE-3 (Discount DeliveryNote)  ← độc lập
PRE-4 (VAT fields)             ← độc lập
PRE-5 (Invoice numbers)        ← độc lập
PRE-6 (BankAccount + link)     ← cần làm trước Phase 2
PRE-7 (FixedAsset)             ← cần làm trước Phase 1c + Phase 3 + 5

                ↓ tất cả PRE hoàn thành ↓

PHASE 1a (AccountingSetup UI)     ← cần PRE-1
PHASE 1b (BankAccounts UI)        ← cần PRE-6
PHASE 1c (FixedAssets UI)         ← cần PRE-7

PHASE 2 (Cash Book)               ← cần PRE-1, PRE-6
PHASE 3 (B02)                     ← cần PRE-3, PRE-4, PRE-7
PHASE 4 (B03)                     ← cần PHASE 2 + PHASE 3
PHASE 5 (B01)                     ← cần PHASE 2 + PHASE 3 + PRE-7
PHASE 6 (UI polish + Print)       ← sau PHASE 1-5
```

**Recommended order (tuần tự):**
1. PRE (tất cả migrations) → 2 sprints
2. Phase 1a + 1b + 1c (foundation UI) → 1 sprint
3. Phase 2 (Cash Book) → 1 sprint
4. Phase 3 (B02) → 1 sprint
5. Phase 4 + 5 (B03 + B01) → 2 sprints
6. Phase 6 (polish) → 1 sprint

**Tổng ước lượng:** ~8 sprints (nếu sprint = 1 tuần)

---

## PHẦN V — Ngoài phạm vi

| Mục | Lý do |
|---|---|
| Sổ cái tổng hợp (General Ledger với hệ thống tài khoản kép) | Quá phức tạp; dùng aggregate thay thế |
| Tích hợp phần mềm kế toán (MISA, Fast) | Export CSV làm cầu nối nếu cần |
| Báo cáo thuế GTGT (mẫu 01/GTGT) | Phase sau — data đã sẵn sàng sau PRE-4 |
| Chiết khấu thanh toán (early payment discount) | Không phổ biến trong mô hình VLXD nhỏ |
| Tỷ giá ngoại tệ | Không áp dụng |
| Thanh lý TSCĐ có thu tiền | Phase sau — B03 mục II hiện để = 0 |
| Vay nợ dài hạn (TK 341) | B03 mục III = 0 cho đến khi có vay |

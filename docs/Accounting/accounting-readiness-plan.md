# Kế hoạch hoàn thiện hệ thống theo hướng Kế toán

**Mục tiêu:** Chuẩn hóa hệ thống để có thể xuất 3 báo cáo tài chính theo VAS (Chuẩn mực kế toán Việt Nam):
- **B01-DN** — Bảng cân đối kế toán
- **B02-DN** — Báo cáo kết quả hoạt động kinh doanh
- **B03-DN** — Báo cáo lưu chuyển tiền tệ

---

## Phân tích hiện trạng (Gap Analysis)

### Đã có ✅
| Thành phần kế toán | Nguồn dữ liệu hiện tại |
|---|---|
| Doanh thu | `DeliveryNote` (Delivered) + `DeliveryNoteItem.SubTotal` |
| Giá vốn hàng bán | `InventoryCostLedgerEntry` (Dispatch movements) |
| Chi phí hoạt động | `Expense` (6 loại) |
| Phải thu KH (A/R) | `CustomerDebt` + `CustomerPayment` |
| Phải trả NCC (A/P) | `VendorDebt` + `VendorPayment` |
| Hàng tồn kho | `InventoryStock` × `AverageCost` |
| Phân loại nợ đầu kỳ KH | `CustomerDebt` constructor (không gắn đơn hàng) ✅ |
| Phân loại nợ đầu kỳ NCC | `VendorDebt` constructor (không gắn GoodsReceipt) ✅ |

### Chưa có / Cần bổ sung ❌
| Thiếu | Ảnh hưởng | Độ ưu tiên |
|---|---|---|
| **Kỳ kế toán** (fiscal year, start date) | B01/B02/B03 không xác định được "kỳ" | 🔴 Critical |
| **Số dư đầu kỳ** (tiền mặt, vốn CSH, nợ cũ) | B01 không cân được | 🔴 Critical |
| **Thuế GTGT** trên doanh thu (đầu ra) | B02 thiếu dòng thuế; sổ thuế sai | 🔴 Critical |
| **Thuế GTGT** trên mua hàng (đầu vào) | Thuế GTGT phải nộp sai | 🔴 Critical |
| **Thuế GTGT** trên chi phí | Như trên | 🟡 High |
| **Sổ quỹ tiền mặt** (cash book) | B03 không có số dư mở/đóng | 🟡 High |
| Phân biệt debt đầu kỳ vs phát sinh | B01 so sánh đầu/cuối kỳ sai | 🟡 High |

---

## Kế hoạch triển khai — 4 Phase

---

### PHASE 1 — Khai báo kế toán đầu kỳ `AccountingSetup`

**Mục tiêu:** Cho phép kế toán nhập số liệu gốc để hệ thống biết "bắt đầu từ đâu".

#### 1.1 Entity mới: `AccountingSetup`

```
Module: Finance / Accounting
Singleton entity (chỉ 1 record trong hệ thống)
```

| Field | Type | Ý nghĩa |
|---|---|---|
| `FiscalYearStartMonth` | int (1–12) | Tháng bắt đầu năm tài chính (mặc định: 1 = tháng 1) |
| `FiscalYearStartDay` | int (1–31) | Ngày bắt đầu (mặc định: 1) |
| `AccountingStartDate` | DateTime | Ngày bắt đầu sử dụng hệ thống kế toán này |
| `OpeningCash` | decimal | Tiền mặt + tiền gửi NH đầu kỳ (nhập thủ công) |
| `OpeningEquity` | decimal | Vốn chủ sở hữu đầu kỳ (vốn điều lệ + lợi nhuận chưa chia) |
| `IsFinalized` | bool | Khóa sau khi đã nhập và xác nhận |
| `FinalizedOnUtc` | DateTime? | Ngày xác nhận |
| `CreatedOnUtc` | DateTime | |

**Lưu ý về nợ đầu kỳ:**
- **Nợ phải thu KH đầu kỳ** → nhập qua màn hình Công nợ KH hiện có, dùng constructor `CustomerDebt(code, customerId, name, amount)` — đã sẵn sàng ✅
- **Nợ phải trả NCC đầu kỳ** → nhập qua màn hình Công nợ NCC hiện có, dùng constructor `VendorDebt(code, vendorId, name, amount)` — đã sẵn sàng ✅
- **Hàng tồn kho đầu kỳ** → dùng `GoodsReceipt(OpeningBalance)` — đã có ✅
- Chỉ cần thêm flag `IsOpeningBalance` vào CustomerDebt/VendorDebt để filter báo cáo

#### 1.2 Bổ sung `IsOpeningBalance` flag

Thêm field `bool IsOpeningBalance` vào `CustomerDebt` và `VendorDebt`:
- Set = true khi tạo từ constructor "số dư đầu kỳ" (constructor không có OrderId/GoodsReceiptId)
- Dùng để phân biệt khi tính số dư đầu kỳ trong B01

#### 1.3 UI cần làm

- `/Accounting/Setup` — Trang khai báo kế toán:
  - Form nhập `OpeningCash`, `OpeningEquity`, `FiscalYearStartMonth`, `AccountingStartDate`
  - Chỉ có thể nhập 1 lần, sau đó khóa (`IsFinalized = true`)
  - Hướng dẫn: "Nợ phải thu/trả đầu kỳ nhập riêng tại mục Công nợ"
- Menu mới: **Kế toán** (Accounting) trong navigation

---

### PHASE 2 — Thuế GTGT (VAT)

**Mục tiêu:** Thêm tracking thuế GTGT trên tất cả giao dịch, phục vụ:
- B02: Doanh thu thuần = Doanh thu gộp − Thuế đầu ra
- Tờ khai thuế GTGT (tương lai)
- Số dư thuế phải nộp trong B01

#### 2.1 Thuế GTGT đầu ra (trên bán hàng)

**Thêm vào `DeliveryNoteItem`:**
```
TaxRate   decimal?   (%, ví dụ: 0, 5, 8, 10)
TaxAmount decimal    (tính từ SubTotal × TaxRate)
```

**Thêm vào `DeliveryNote` (aggregate):**
```
TotalTaxAmount decimal  (SUM của TaxAmount các item)
```

**Logic:** Thuế tính theo từng dòng hàng, rate do người dùng chọn khi lập phiếu xuất.

#### 2.2 Thuế GTGT đầu vào (trên mua hàng)

**Thêm vào `GoodsReceiptItem`:**
```
TaxRate   decimal?
TaxAmount decimal
```

**Thêm vào `GoodsReceipt` (aggregate):**
```
TotalTaxAmount decimal
```

**Thêm vào `PurchaseOrderItem`:**
```
TaxRate   decimal?
TaxAmount decimal
```

**Thêm vào `PurchaseOrder` (aggregate):**
```
TotalTaxAmount decimal  (thay thế/bổ sung TaxAmount hiện có nếu có)
```

#### 2.3 Thuế trên chi phí

**Thêm vào `Expense`:**
```
TaxRate   decimal?
TaxAmount decimal    (thuế GTGT đầu vào trên chi phí)
AmountExcludingTax decimal  (= Amount - TaxAmount)
```

#### 2.4 Tax rate mặc định

Thêm setting `DefaultTaxRate` (decimal, default = 0.10 = 10%) vào `AccountingSetup` hoặc system settings. Người dùng có thể override per line item.

#### 2.5 UI cần sửa

- **DeliveryNote confirm/create**: Thêm cột TaxRate + TaxAmount per item, tổng thuế
- **GoodsReceipt/Create**: Thêm cột TaxRate + TaxAmount per item
- **Expense/Create & Edit**: Thêm field TaxRate, hiển thị AmountExcludingTax và TaxAmount
- **PurchaseOrder**: Thêm cột thuế per item

#### 2.6 EF Migrations

- `AddTaxFieldsToDeliveryNoteItems`
- `AddTaxFieldsToGoodsReceiptItems`
- `AddTaxFieldsToPurchaseOrderItems`
- `AddTaxFieldsToExpenses`

---

### PHASE 3 — Sổ quỹ & Số dư tiền mặt (Cash Book)

**Mục tiêu:** Tính được số dư tiền mặt tại bất kỳ thời điểm nào.

**Cách tiếp cận:** Không tạo entity mới — aggregate từ dữ liệu hiện có.

#### Công thức tính số dư tiền:

```
Số dư tiền kỳ N =
  AccountingSetup.OpeningCash                         [đầu kỳ đầu tiên]
+ SUM(CustomerPayment.Amount WHERE PaidOnUtc <= end)  [thu từ KH]
- SUM(VendorPayment.Amount WHERE PaidOnUtc <= end)    [trả cho NCC]
- SUM(Expense.Amount WHERE IncurredDate <= end)       [chi phí]
```

**Lưu ý:** Expense hiện không có PaymentMethod — giả định tất cả chi phí đã thanh toán bằng tiền. Nếu cần chính xác hơn, có thể thêm field `IsCashExpense bool` vào Expense sau.

#### UI cần làm

- `/Accounting/CashBook` — Sổ quỹ:
  - Chọn kỳ (từ ngày → đến ngày)
  - Bảng giao dịch: ngày, loại (Thu/Chi), diễn giải, số tiền, số dư lũy kế
  - Nguồn: CustomerPayment + VendorPayment + Expense gộp lại, sort theo ngày

---

### PHASE 4 — 3 Báo cáo tài chính

#### 4.1 B02-DN — Kết quả kinh doanh

```
I. Doanh thu bán hàng và cung cấp DV       = SUM(DeliveryNoteItem.SubTotal) [Delivered, trong kỳ]
II. Các khoản giảm trừ doanh thu           = SUM(CustomerReturn điều chỉnh giảm DT)
III. Doanh thu thuần (I - II)
IV. Giá vốn hàng bán                       = SUM(InventoryCostLedgerEntry[Dispatch, trong kỳ].TotalCost)
V. Lợi nhuận gộp (III - IV)
VI. Doanh thu tài chính                    = 0 (chưa có)
VII. Chi phí tài chính                     = 0 (chưa có)
VIII. Chi phí bán hàng                     = SUM(Expense[Marketing, ReturnCost])
IX. Chi phí quản lý DN                     = SUM(Expense[Payroll, Rent, Utilities, General])
X. Lợi nhuận thuần từ HĐKD (V+VI-VII-VIII-IX)
XI. Lợi nhuận khác                        = 0
XII. Tổng lợi nhuận kế toán trước thuế
XIII. Thuế TNDN (*)                        = 0 (chưa có)
XIV. Lợi nhuận sau thuế
```

(*) Thuế TNDN có thể thêm sau — Phase 5

#### 4.2 B03-DN — Lưu chuyển tiền tệ (phương pháp gián tiếp)

```
I. LƯU CHUYỂN TIỀN TỪ HĐKD
  1. Lợi nhuận trước thuế                 [từ B02]
  2. Điều chỉnh cho các khoản:
     + Tăng/giảm phải thu KH             = CustomerDebt.RemainingAmount (cuối kỳ - đầu kỳ)
     + Tăng/giảm phải trả NCC            = VendorDebt.RemainingAmount (cuối kỳ - đầu kỳ)
     + Tăng/giảm hàng tồn kho            = Inventory value (cuối kỳ - đầu kỳ)
  = Lưu chuyển tiền thuần từ HĐKD

II. LƯU CHUYỂN TIỀN TỪ HĐ ĐẦU TƯ       = 0 (không có tài sản cố định)

III. LƯU CHUYỂN TIỀN TỪ HĐ TÀI CHÍNH   = 0 (không có vay nợ dài hạn)

IV. Tăng/giảm tiền thuần trong kỳ (I+II+III)
V. Tiền đầu kỳ                          = AccountingSetup.OpeningCash (kỳ đầu) hoặc tiền cuối kỳ trước
VI. Tiền cuối kỳ (IV + V)
```

#### 4.3 B01-DN — Bảng cân đối kế toán

```
TÀI SẢN
  A. Tài sản ngắn hạn
    I. Tiền và tương đương tiền          = Số dư tiền (Phase 3)
    II. Phải thu ngắn hạn
       - Phải thu KH                     = SUM(CustomerDebt.RemainingAmount [Outstanding/Partial] đến cuối kỳ)
    III. Hàng tồn kho                    = SUM(InventoryStock.Quantity × AverageCost)
  B. Tài sản dài hạn                    = 0

TỔNG TÀI SẢN

NGUỒN VỐN
  A. Nợ phải trả
    I. Nợ ngắn hạn
       - Phải trả NCC                    = SUM(VendorDebt.RemainingAmount [Outstanding/Partial] đến cuối kỳ)
       - Thuế và các khoản phải nộp      = SUM(TaxAmount đầu ra) - SUM(TaxAmount đầu vào) [lũy kế]
  B. Vốn chủ sở hữu
       - Vốn góp                         = AccountingSetup.OpeningEquity
       - Lợi nhuận chưa phân phối        = SUM(Lợi nhuận sau thuế các kỳ trước + kỳ này)

TỔNG NGUỒN VỐN  (= TỔNG TÀI SẢN)
```

#### 4.4 UI báo cáo

- `/Accounting/IncomeStatement` — B02, chọn kỳ (tháng/quý/năm)
- `/Accounting/CashFlow` — B03, chọn kỳ
- `/Accounting/BalanceSheet` — B01, chọn thời điểm
- Nút **In / Xuất PDF** cho từng báo cáo (có thể dùng browser print)

---

## Thứ tự triển khai & Dependencies

```
Phase 1 (AccountingSetup)
    └→ Phase 2 (VAT) — độc lập, có thể song song
        └→ Phase 4 (Reports) — cần Phase 1 + Phase 2 + Phase 3
Phase 3 (Cash Book) — cần Phase 1
    └→ Phase 4
```

**Recommended order:**
1. Phase 1 → Phase 3 → Phase 2 → Phase 4

---

## Danh sách entities cần tạo/sửa

| Entity | Action | Fields mới |
|---|---|---|
| `AccountingSetup` | **TẠO MỚI** | FiscalYearStartMonth, AccountingStartDate, OpeningCash, OpeningEquity, IsFinalized |
| `CustomerDebt` | Sửa | `IsOpeningBalance bool` |
| `VendorDebt` | Sửa | `IsOpeningBalance bool` |
| `DeliveryNoteItem` | Sửa | `TaxRate decimal?`, `TaxAmount decimal` |
| `DeliveryNote` | Sửa | `TotalTaxAmount decimal` |
| `GoodsReceiptItem` | Sửa | `TaxRate decimal?`, `TaxAmount decimal` |
| `GoodsReceipt` | Sửa | `TotalTaxAmount decimal` |
| `PurchaseOrderItem` | Sửa | `TaxRate decimal?`, `TaxAmount decimal` |
| `PurchaseOrder` | Sửa | aggregate `TotalTaxAmount` |
| `Expense` | Sửa | `TaxRate decimal?`, `TaxAmount decimal` |

## Danh sách migrations

1. `AddAccountingSetup` — bảng mới
2. `AddIsOpeningBalanceToDebts` — CustomerDebt, VendorDebt
3. `AddTaxFieldsToDeliveryNote` — DeliveryNote + DeliveryNoteItem
4. `AddTaxFieldsToGoodsReceipt` — GoodsReceipt + GoodsReceiptItem
5. `AddTaxFieldsToPurchaseOrder` — PurchaseOrder + PurchaseOrderItem
6. `AddTaxFieldsToExpense` — Expense

---

## Ngoài phạm vi (không làm trong plan này)

- Thuế TNDN (Corporate Income Tax)
- Tài sản cố định & khấu hao
- Sổ cái chi tiết (General Ledger) với tài khoản kép
- Tích hợp phần mềm kế toán bên ngoài (MISA, Fast)
- Báo cáo thuế GTGT (tờ khai 01/GTGT)

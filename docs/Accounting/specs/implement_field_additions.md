# Implementation Spec: PRE-2 → PRE-5 (Field Additions)

> Các PRE này chỉ thêm fields vào entities có sẵn. Không cần Manager/AppService mới.
> Thực hiện theo thứ tự migration bên dưới.

---

## PRE-2 — `IsOpeningBalance` trên CustomerDebt & VendorDebt

### Domain changes

**File:** `NamEcommerce.Domain/Entities/Debts/CustomerDebt.cs`

Thêm property và tự set trong constructor số dư đầu kỳ:

```csharp
public bool IsOpeningBalance { get; private set; }

// Constructor số dư đầu kỳ (không có DeliveryNoteId) — thêm dòng:
internal CustomerDebt(string code, Guid customerId, string customerName,
    decimal totalAmount) : base(Guid.NewGuid())
{
    // ... existing code ...
    IsOpeningBalance = true;   // ← THÊM
}
```

**File:** `NamEcommerce.Domain/Entities/Debts/VendorDebt.cs` — tương tự.

### Migration

```csharp
// AddIsOpeningBalanceToDebts
migrationBuilder.AddColumn<bool>(
    name: "IsOpeningBalance",
    table: "CustomerDebts",
    nullable: false,
    defaultValue: false);

migrationBuilder.AddColumn<bool>(
    name: "IsOpeningBalance",
    table: "VendorDebts",
    nullable: false,
    defaultValue: false);
```

---

## PRE-3 — Chiết khấu thương mại trên `DeliveryNote` (TK 521)

### Quy tắc domain

- `DiscountPercent`: 0–100, nullable (null = không chiết khấu)
- `DiscountAmount`: tính tự động = `SubTotal × DiscountPercent / 100` nếu dùng %, hoặc nhập thẳng
- `NetAmount = SubTotal - DiscountAmount` (không âm)
- `TotalDiscountAmount` trên DeliveryNote = SUM(Item.DiscountAmount)
- Thuế GTGT (PRE-4) tính trên `NetAmount`, không phải `SubTotal`

### Domain changes

**File:** `NamEcommerce.Domain/Entities/DeliveryNotes/DeliveryNoteItem.cs`

```csharp
public decimal DiscountPercent { get; internal set; }   // 0–100
public decimal DiscountAmount { get; internal set; }    // = SubTotal × DiscountPercent/100
public decimal NetAmount => SubTotal - DiscountAmount;  // computed, không persist riêng
```

**File:** `NamEcommerce.Domain/Entities/DeliveryNotes/DeliveryNote.cs`

```csharp
public decimal TotalDiscountAmount => _items.Sum(i => i.DiscountAmount);  // computed
```

> `TotalDiscountAmount` là computed property, **không persist** vào DB — tính từ items.

### Domain Manager change

**File:** `NamEcommerce.Domain.Services/DeliveryNotes/DeliveryNoteManager.cs`

Khi `AddItem(...)`, bổ sung tham số discount (optional, default = 0):

```csharp
// Thêm overload hoặc sửa signature hiện tại của AddItem trong entity
internal void AddItem(Guid orderItemId, Guid productId, string productName,
    decimal quantity, decimal unitPrice,
    decimal discountPercent = 0)
{
    var item = new DeliveryNoteItem(...);
    item.DiscountPercent = discountPercent;
    item.DiscountAmount = Math.Round(item.SubTotal * discountPercent / 100, 0);
    _items.Add(item);
}
```

### Application DTO change

**File:** `NamEcommerce.Application.Contracts/Dtos/DeliveryNotes/DeliveryNoteAppDtos.cs`

```csharp
public sealed record DeliveryNoteLineAppDto
{
    // existing fields...
    public decimal DiscountPercent { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal NetAmount { get; init; }
}
```

### Migration

```csharp
// AddDiscountFieldsToDeliveryNoteItems
migrationBuilder.AddColumn<decimal>(
    name: "DiscountPercent",
    table: "DeliveryNoteItems",
    type: "decimal(5,2)",
    nullable: false,
    defaultValue: 0m);

migrationBuilder.AddColumn<decimal>(
    name: "DiscountAmount",
    table: "DeliveryNoteItems",
    type: "decimal(18,0)",
    nullable: false,
    defaultValue: 0m);
```

### UI change

**File:** `NamEcommerce.Web/Views/DeliveryNote/Create.cshtml` (và Confirm nếu có)

Thêm cột "Chiết khấu (%)" vào bảng sản phẩm, sau cột Đơn giá:
```html
<th class="text-end pe-2">CK (%)</th>
```
```html
<td class="text-end pe-2">
    <input name="Items[i].DiscountPercent" type="number" min="0" max="100"
           class="form-control form-control-sm text-end" step="0.01" value="0" />
</td>
```
JS: khi thay đổi DiscountPercent → tính lại NetAmount hiển thị realtime.

---

## PRE-4 — Thuế GTGT

### Quy tắc domain

- `TaxRate`: 0 / 0.05 / 0.08 / 0.10 (nullable — null = chưa áp dụng)
- `TaxAmount = NetAmount × TaxRate` (làm tròn về đơn vị đồng)
- Aggregate `TotalTaxAmount = SUM(item.TaxAmount)`
- `Expense.TaxAmount` tính trên `Amount` (chi phí chưa có chiết khấu)

### PRE-4a — DeliveryNoteItem & DeliveryNote

**File:** `DeliveryNoteItem.cs`

```csharp
public decimal? TaxRate { get; internal set; }           // null / 0 / 0.05 / 0.08 / 0.10
public decimal TaxAmount { get; internal set; }          // = NetAmount × TaxRate
public decimal AmountIncludingTax => NetAmount + TaxAmount;
```

**File:** `DeliveryNote.cs`

```csharp
public decimal TotalTaxAmount => _items.Sum(i => i.TaxAmount);   // computed
public string? InvoiceNumber { get; internal set; }
public string? InvoiceSeries { get; internal set; }
public DateTime? InvoiceDate { get; internal set; }
```

**Migration:** `AddTaxAndInvoiceToDeliveryNote`

```csharp
migrationBuilder.AddColumn<decimal>("TaxRate", "DeliveryNoteItems",
    type: "decimal(5,4)", nullable: true);
migrationBuilder.AddColumn<decimal>("TaxAmount", "DeliveryNoteItems",
    type: "decimal(18,0)", nullable: false, defaultValue: 0m);

migrationBuilder.AddColumn<string>("InvoiceNumber", "DeliveryNotes",
    maxLength: 20, nullable: true);
migrationBuilder.AddColumn<string>("InvoiceSeries", "DeliveryNotes",
    maxLength: 10, nullable: true);
migrationBuilder.AddColumn<DateTime>("InvoiceDate", "DeliveryNotes",
    nullable: true);
```

### PRE-4b — GoodsReceiptItem & GoodsReceipt

**File:** `GoodsReceiptItem.cs`

```csharp
public decimal? TaxRate { get; internal set; }
public decimal TaxAmount { get; internal set; }
```

**File:** `GoodsReceipt.cs`

```csharp
public decimal TotalTaxAmount => _items.Sum(i => i.TaxAmount);   // computed
public string? VendorInvoiceNumber { get; internal set; }
public DateTime? VendorInvoiceDate { get; internal set; }
```

**Migration:** `AddTaxAndVendorInvoiceToGoodsReceipt`

```csharp
migrationBuilder.AddColumn<decimal>("TaxRate", "GoodsReceiptItems",
    type: "decimal(5,4)", nullable: true);
migrationBuilder.AddColumn<decimal>("TaxAmount", "GoodsReceiptItems",
    type: "decimal(18,0)", nullable: false, defaultValue: 0m);

migrationBuilder.AddColumn<string>("VendorInvoiceNumber", "GoodsReceipts",
    maxLength: 20, nullable: true);
migrationBuilder.AddColumn<DateTime>("VendorInvoiceDate", "GoodsReceipts",
    nullable: true);
```

### PRE-4c — Expense

**File:** `NamEcommerce.Domain/Entities/Finance/Expense.cs`

```csharp
public decimal? TaxRate { get; private set; }
public decimal TaxAmount { get; private set; }
public decimal AmountExcludingTax => Amount - TaxAmount;
public PaymentMethod? PaymentMethod { get; private set; }
public Guid? BankAccountId { get; private set; }    // set khi PaymentMethod = BankTransfer
```

Cập nhật constructor và `UpdateInfo()` để nhận thêm tham số:

```csharp
internal Expense(Guid id, string title, decimal amount, ExpenseType expenseType,
    DateTime incurredDate, Guid? recordedByUserId,
    decimal? taxRate = null,
    PaymentMethod? paymentMethod = null,
    Guid? bankAccountId = null) : base(id)
{
    // ... existing ...
    TaxRate = taxRate;
    TaxAmount = taxRate.HasValue ? Math.Round(amount * taxRate.Value, 0) : 0;
    PaymentMethod = paymentMethod;
    BankAccountId = paymentMethod == Domain.Shared.Enums.Orders.PaymentMethod.BankTransfer
        ? bankAccountId : null;
}
```

**Migration:** `AddTaxPaymentMethodToExpenses`

```csharp
migrationBuilder.AddColumn<decimal>("TaxRate", "Expenses",
    type: "decimal(5,4)", nullable: true);
migrationBuilder.AddColumn<decimal>("TaxAmount", "Expenses",
    type: "decimal(18,0)", nullable: false, defaultValue: 0m);
migrationBuilder.AddColumn<int>("PaymentMethod", "Expenses",
    nullable: true);
migrationBuilder.AddColumn<Guid>("BankAccountId", "Expenses",
    nullable: true);
```

### PRE-4d — PurchaseOrderItem (optional)

```csharp
// PurchaseOrderItem.cs
public decimal? TaxRate { get; internal set; }
public decimal TaxAmount { get; internal set; }
```

**Migration:** `AddTaxFieldsToPurchaseOrderItems`

```csharp
migrationBuilder.AddColumn<decimal>("TaxRate", "PurchaseOrderItems",
    type: "decimal(5,4)", nullable: true);
migrationBuilder.AddColumn<decimal>("TaxAmount", "PurchaseOrderItems",
    type: "decimal(18,0)", nullable: false, defaultValue: 0m);
```

### UI changes cho VAT

**DeliveryNote Create/Confirm:** Thêm cột "Thuế (%)" sau cột Chiết khấu, dropdown: 0% / 5% / 8% / 10%

**GoodsReceipt Create:** Thêm cột "Thuế (%)" + fields "Số hóa đơn NCC" và "Ngày hóa đơn"

**Expense Create/Edit:** Thêm dropdown TaxRate + hiển thị TaxAmount và AmountExcludingTax

---

## Thứ tự implement

```
1. Migration AddIsOpeningBalanceToDebts          → update CustomerDebt + VendorDebt entities
2. Migration AddDiscountFieldsToDeliveryNoteItems → update DeliveryNoteItem entity + UI
3. Migration AddTaxAndInvoiceToDeliveryNote       → update DeliveryNoteItem + DeliveryNote + UI
4. Migration AddTaxAndVendorInvoiceToGoodsReceipt → update GoodsReceiptItem + GoodsReceipt + UI
5. Migration AddTaxPaymentMethodToExpenses        → update Expense entity + UI
6. Migration AddTaxFieldsToPurchaseOrderItems     → update PurchaseOrderItem (optional)
```

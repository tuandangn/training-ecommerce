# Plan: Nhập hàng nhanh (Quick Purchase Order)

## Vấn đề

Quy trình nhập hàng hiện tại yêu cầu 4+ bước:
```
Tạo Draft → Submit → Approve → Receive từng item → Complete
```

Cần một luồng ngắn hơn cho các tình huống khẩn hoặc volume nhỏ: **tạo + nhận hàng ngay trong một màn hình**.

---

## Quyết định kiến trúc

| Quyết định | Lý do |
|-----------|-------|
| Vẫn tạo PO | Giữ tracking, allocation, báo cáo nhập hàng |
| Bỏ Draft → Submitted → Approved | Transition tuần tự nội bộ (2 calls), không phá business rules |
| Giá vốn optional | Pending cost → VendorDebt sinh khi set sau |
| Ảnh optional | Remove validation bắt buộc ở AppService level cho luồng này |
| Multi-warehouse | 1 kho mặc định + override per item |

---

## Tính năng chi tiết (đã xác nhận)

### 1. Upload ảnh chứng từ — optional
- Vẫn có image uploader (tái sử dụng `_ImageUploader.cshtml`)
- Không bắt buộc — có thể submit không có ảnh

### 2. Giá vốn auto-fill từ NCC
- Khi chọn NCC: tự động fetch giá nhập gần nhất (per product + vendor) cho tất cả items
- Khi đổi NCC: chỉ refresh giá của items **chưa bị sửa thủ công** (track `isDirty` per row)
- Khi thêm item mới: tự động fetch giá từ NCC hiện tại
- API: tái sử dụng endpoint `/Product/PurchasePriceReference` (đã có, dùng trong PO Create)
- Nếu không có lịch sử giá → để trống (0 hoặc null)

### 3. Toggle "Đã nhận hàng" (mặc định ON)
- **ON**: PO tạo → auto Approved → BulkReceive → PO ở Receiving/Completed, GoodsReceipt tạo
- **OFF**: PO tạo → auto Approved → không receive, PO ở Approved (user receive sau qua PO Details)
- Khi OFF: "Đã thanh toán" bị disable (chưa có nợ nên không thể thanh toán)

### 4. Option "Đã thanh toán" (mặc định OFF, chỉ hiển thị khi "Đã nhận hàng" ON)
- Khi ON: hiện thêm 2 field:
  - **Hình thức thanh toán**: dropdown (Tiền mặt / Chuyển khoản / ...)
  - **Số tiền**: input số (default = tổng tiền hàng, có thể sửa thủ công — thanh toán một phần)
- Backend: sau khi tạo VendorDebt → tạo VendorPayment với số tiền và phương thức đã chọn
- Cần kiểm tra module `Debts` hiện tại để reuse VendorPayment creation

### 5. Multi-warehouse
- **Kho mặc định** ở cấp đơn (dropdown ở trên form)
- **Override per item**: mỗi item có thể chọn kho khác (hiển thị trong offcanvas edit trên mobile, inline trên desktop)
- Nếu item không chọn kho riêng → dùng kho mặc định khi submit
- BulkReceive tạo 1 GoodsReceipt per (warehouse group) — hoạt động đúng với existing logic

---

## UI Design

```
┌─────────────────────────────────────────────────────────┐
│  Nhập hàng nhanh                                        │
├─────────────────────────────────────────────────────────┤
│  Nhà cung cấp  [VendorPicker                    ▼]     │
│  Kho mặc định  [Kho A                           ▼]     │
│  Ngày nhập     [2026-06-06                      ]      │
│  Ghi chú       [                                ]      │
├─────────────────────────────────────────────────────────┤
│  HÀNG HÓA                                               │
│  [ProductBrowser]  │  Tên     | Kho    | SL | Giá nhập │
│                    │  Sp A    | Kho A  | 10 | 50,000   │
│                    │  Sp B    | Kho B  |  5 | (trống)  │
│                    │  + Thêm  |        |    |          │
├─────────────────────────────────────────────────────────┤
│  [_ImageUploader]   (tùy chọn)                          │
├─────────────────────────────────────────────────────────┤
│  ☑ Đã nhận hàng                                        │
│  ☐ Đã thanh toán  [Tiền mặt ▼]  [500,000 đ      ]     │
├─────────────────────────────────────────────────────────┤
│  Tổng tiền hàng:  500,000 đ                             │
│                   [Hủy]  [Nhập hàng ngay →]            │
└─────────────────────────────────────────────────────────┘
```

**Desktop**: Kho + Giá nhập hiển thị inline trong table row
**Mobile**: Tap row → `ItemEditOffcanvas` mở với thêm field Kho (dropdown trong offcanvas)

---

## Phạm vi thay đổi

| Layer | File | Thay đổi |
|-------|------|---------|
| Domain.Shared | `DTOs/PurchaseOrders/QuickCreatePurchaseOrderDto.cs` | Thêm mới |
| Domain.Services | `PurchaseOrders/PurchaseOrderManager.cs` | Thêm `QuickCreateAndReceiveAsync()` |
| Application.Contracts | `PurchaseOrders/IPurchaseOrderAppService.cs` | Thêm method |
| Application.Services | `PurchaseOrders/PurchaseOrderAppService.cs` | Implement method |
| Web.Contracts | `Commands/PurchaseOrders/QuickCreatePurchaseOrderCommand.cs` | Thêm mới |
| Web.Contracts | `Models/PurchaseOrders/QuickCreatePurchaseOrderModel.cs` | Thêm mới |
| Web.Framework | `Commands/Handlers/PurchaseOrderCommandHandlers.cs` | Thêm handler |
| Web | `Services/PurchaseOrders/PurchaseOrderModelFactory.cs` | Thêm `PrepareQuickCreateModel()` |
| Web | `Controllers/PurchaseOrderController.cs` | Thêm GET/POST QuickCreate |
| Web | `wwwroot/modules/QuickPurchaseOrderController.js` | Mới |
| Web | `Views/PurchaseOrder/QuickCreate.cshtml` | Mới |
| Web | `Views/PurchaseOrder/List.cshtml` hoặc nav | Thêm link |

**Không thay đổi**: Domain entities, migrations, GoodsReceipt flow, VendorDebt generation logic.

---

## Domain Layer

### `QuickCreatePurchaseOrderDto` (Domain.Shared)

```csharp
public record QuickCreatePurchaseOrderDto
{
    public Guid? VendorId { get; init; }
    public Guid DefaultWarehouseId { get; init; }
    public DateTime ReceivedOnUtc { get; init; }
    public string? Note { get; init; }
    public bool ReceiveImmediately { get; init; } = true;
    public List<Guid> PictureIds { get; init; } = [];       // optional
    public List<QuickCreatePurchaseOrderItemDto> Items { get; init; } = [];
    public QuickPaymentDto? Payment { get; init; }          // null = không thanh toán
}

public record QuickCreatePurchaseOrderItemDto
{
    public Guid ProductId { get; init; }
    public decimal Quantity { get; init; }
    public decimal? UnitCost { get; init; }                 // null = pending
    public Guid? WarehouseId { get; init; }                 // null = dùng DefaultWarehouseId
    public int QuantityDecimalPlaces { get; init; }
}

public record QuickPaymentDto
{
    public decimal Amount { get; init; }
    public string PaymentMethod { get; init; } = "";        // "Cash", "BankTransfer", ...
}

public record QuickCreatePurchaseOrderResultDto
{
    public Guid PurchaseOrderId { get; init; }
    public string PurchaseOrderCode { get; init; } = "";
    public List<Guid> GoodsReceiptIds { get; init; } = [];
    public Guid? VendorDebtId { get; init; }
    public Guid? VendorPaymentId { get; init; }
}
```

### `PurchaseOrderManager.QuickCreateAndReceiveAsync()`

```
1. Validate dto (items > 0, warehouse set, quantity > 0)
2. CreatePurchaseOrderAsync() → PO in Draft
3. ChangeStatusAsync(Submitted) → ChangeStatusAsync(Approved)  // 2 calls, bypass UI workflow
4. if (ReceiveImmediately):
   a. BulkReceiveItemsAsync() với lines = items (mỗi item dùng WarehouseId || DefaultWarehouseId)
   b. → GoodsReceipts tạo, tồn kho cập nhật, VendorDebt sinh nếu có vendor + cost
   c. if (Payment != null && VendorDebt created):
      → CreateVendorPaymentAsync(vendorDebtId, amount, paymentMethod)
5. return result với PO id, GR ids, debt id, payment id
```

**Lưu ý**: Cần kiểm tra `VendorDebt`/`VendorPayment` domain xem có method tạo payment từ debt không. Nếu chưa có → implement trong Payment phần riêng (T1.3+).

---

## Application Layer

### `QuickCreatePurchaseOrderAppDto`

```csharp
public class QuickCreatePurchaseOrderAppDto
{
    public Guid? VendorId { get; set; }
    public Guid DefaultWarehouseId { get; set; }
    public DateTime ReceivedOnUtc { get; set; }
    public string? Note { get; set; }
    public bool ReceiveImmediately { get; set; } = true;
    public List<Guid> PictureIds { get; set; } = [];
    public List<QuickCreateItemAppDto> Items { get; set; } = [];
    public QuickPaymentAppDto? Payment { get; set; }

    public (bool, string?) Validate()
    {
        if (DefaultWarehouseId == Guid.Empty) return (false, "Vui lòng chọn kho.");
        if (!Items.Any()) return (false, "Vui lòng thêm ít nhất 1 hàng hóa.");
        if (Items.Any(i => i.Quantity <= 0)) return (false, "Số lượng phải lớn hơn 0.");
        if (Payment != null && Payment.Amount <= 0) return (false, "Số tiền thanh toán phải lớn hơn 0.");
        return (true, null);
    }
}
```

---

## Web Command

```csharp
public record QuickCreatePurchaseOrderCommand(
    Guid? VendorId,
    Guid DefaultWarehouseId,
    DateTime ReceivedOnUtc,
    string? Note,
    bool ReceiveImmediately,
    List<Guid> PictureIds,
    List<QuickCreatePurchaseOrderItemCommand> Items,
    QuickPaymentCommand? Payment
) : IRequest<QuickCreatePurchaseOrderResult>;

public record QuickCreatePurchaseOrderItemCommand(
    Guid ProductId,
    decimal Quantity,
    decimal? UnitCost,
    Guid? WarehouseId,
    int QuantityDecimalPlaces
);

public record QuickPaymentCommand(decimal Amount, string PaymentMethod);

public record QuickCreatePurchaseOrderResult(
    bool Success,
    Guid? PurchaseOrderId,
    string? PurchaseOrderCode,
    string? ErrorMessage
);
```

---

## JS Controller (`QuickPurchaseOrderController.js`)

Tái sử dụng pattern từ `CreatePurchaseOrderController.js`:

**Thêm so với PO Create thông thường:**
```js
// Track manually-edited prices per item
#manualPriceSet = new Set();   // Set<itemIndex>

// Khi vendor đổi → refresh giá items chưa dirty
async #onVendorChange(vendor) {
    for (const [index, item] of this.#state.items.entries()) {
        if (this.#manualPriceSet.has(index)) continue;  // skip dirty
        const price = await this.#fetchPurchasePrice(item.productInfo.id, vendor.id);
        if (price !== null) this.#updateItem(index, { unitCost: price });
    }
}

// Khi user sửa giá thủ công → mark dirty
#onManualPriceEdit(index) {
    this.#manualPriceSet.add(index);
}

// Khi thêm item mới → fetch giá từ NCC hiện tại
async #onItemAdded(product, index) {
    if (this.#state.vendor) {
        const price = await this.#fetchPurchasePrice(product.id, this.#state.vendor.id);
        if (price !== null) this.#updateItem(index, { unitCost: price });
    }
}

// Toggle "đã nhận hàng"
#onReceiveToggle(checked) {
    getEl('paymentSection').style.display = checked ? '' : 'none';
    if (!checked) {
        getEl('payNowCheck').checked = false;
        this.#onPayNowToggle(false);
    }
}

// Toggle "đã thanh toán"
#onPayNowToggle(checked) {
    getEl('paymentDetails').style.display = checked ? '' : 'none';
    if (checked) this.#syncPaymentTotal();
}

// Sync payment amount = tổng tiền hàng (nếu chưa sửa)
#syncPaymentTotal() { ... }
```

**ItemEditOffcanvas** (mobile) cần thêm Kho dropdown:
- Truyền thêm `warehouseId` vào `open(item, callbacks)` và `onApply(qty, price, warehouseId)`
- Hoặc: Handle warehouse per-item chỉ ở desktop inline; mobile dùng global warehouse

> **Quyết định cần xác nhận khi implement**: Offcanvas mobile có cần chọn kho per item không, hay mobile chỉ dùng kho mặc định?

---

## Task List

### Phase 1: Domain + Application (P0)

- [ ] **T1.1** — Thêm DTOs: `QuickCreatePurchaseOrderDto`, item dto, result dto, payment dto (Domain.Shared)
- [ ] **T1.2** — Kiểm tra VendorDebt/VendorPayment module — có method tạo payment từ debt không?
- [ ] **T1.3** — Implement `PurchaseOrderManager.QuickCreateAndReceiveAsync()` (Domain.Services)
- [ ] **T1.4** — Thêm method `QuickCreateAndReceivePurchaseOrderAsync()` vào `IPurchaseOrderAppService` + implement

### Phase 2: Web Layer (P0)

- [ ] **T2.1** — Thêm `QuickCreatePurchaseOrderCommand`, result, item command, payment command (Web.Contracts)
- [ ] **T2.2** — Thêm `QuickCreatePurchaseOrderModel`, item model, payment model (Web.Contracts)
- [ ] **T2.3** — Implement Handler (Web.Framework)
- [ ] **T2.4** — Thêm `PrepareQuickCreatePurchaseOrderModel()` vào `PurchaseOrderModelFactory`
  - Load warehouses, load payment methods
- [ ] **T2.5** — Thêm `GET QuickCreate` + `POST QuickCreate` vào `PurchaseOrderController`

### Phase 3: UI (P0)

- [ ] **T3.1** — Tạo `QuickPurchaseOrderController.js`:
  - VendorPicker + giá auto-fill + dirty tracking
  - Items table với cột Kho (dropdown per item)
  - ProductBrowser + ItemEditOffcanvas (mobile, đã có)
  - Toggle "đã nhận hàng" / "đã thanh toán"
  - Payment amount sync với tổng
- [ ] **T3.2** — Tạo `Views/PurchaseOrder/QuickCreate.cshtml`
  - Include `_ImageUploader.cshtml` (optional)
  - Include `_ItemEditOffcanvas.cshtml`
- [ ] **T3.3** — Thêm link "Nhập hàng nhanh" trên PO List page (button hoặc dropdown)

### Phase 4: Polish (P1)

- [ ] **T4.1** — Xử lý edge case: items pending cost không tạo VendorDebt → hiển thị warning trên PO Details
- [ ] **T4.2** — Test multi-warehouse → verify BulkReceive tạo đúng số GoodsReceipts

---

## Build Order

```
T1.1 → T1.2 → T1.3 → T1.4
                          ↓
                      T2.1 → T2.2 → T2.3 → T2.4 → T2.5
                                                      ↓
                                                  T3.1 → T3.2 → T3.3
                                                                     ↓
                                                                  T4.1 → T4.2
```

T1.2 (VendorDebt research) phải làm trước T1.3.
T3.1 và T2.x có thể làm song song sau T2.2 (khi có model).

---

## Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| VendorDebt module chưa có CreatePayment method | High | T1.2 research trước — nếu thiếu, implement payment creation trong T1.3 |
| ChangeStatus(Draft→Submitted→Approved) — transaction bị rollback giữa 2 calls | Medium | Wrap toàn bộ QuickCreate trong 1 Unit of Work / transaction scope |
| `isDirty` tracking bị mất khi re-render items table | Low | Lưu `#manualPriceSet` theo `productId` thay vì index |
| BulkReceive nhiều warehouse → nhiều GoodsReceipts — VendorDebt tạo per GR | Low | Expected behavior, verify tổng debt = tổng hàng |
| "Đã thanh toán" khi cost pending (UnitCost null) → VendorDebt chưa sinh → không có gì để pay | Medium | Chỉ enable payment khi ít nhất 1 item có UnitCost |

---

## Acceptance Criteria

- [ ] Submit với "đã nhận hàng" ON → PO ở Receiving/Completed, GoodsReceipt tạo, tồn kho cập nhật, redirect PO Details
- [ ] Submit với "đã nhận hàng" OFF → PO ở Approved, không có GoodsReceipt, redirect PO Details
- [ ] Items không có giá → GoodsReceipt pending cost, VendorDebt chưa sinh
- [ ] Set giá trong GoodsReceipt Details → VendorDebt sinh tự động
- [ ] "Đã thanh toán" ON + có giá → VendorPayment tạo với amount và method đã chọn
- [ ] Đổi NCC → giá refresh trừ items user đã sửa thủ công
- [ ] Multi-warehouse: items nhập vào đúng kho đã chọn (mặc định hoặc override)
- [ ] Desktop và mobile đều hoạt động
- [ ] Ảnh là optional — submit không có ảnh vẫn OK

# IMPROVEMENT_PLAN.md — Audit + Kế hoạch cải thiện hệ thống

> Tài liệu này tổng hợp kết quả audit hệ thống NamEcommerce / VLXD Tuấn Khôi tại thời điểm 2026-05-06,
> kèm kế hoạch cải thiện và chỉnh sửa thiết kế module Returns dựa trên trao đổi với chủ dự án.
>
> **Trạng thái: chỉ là plan, chưa code.** Sau khi bạn duyệt, sẽ chuyển sang implement theo thứ tự
> ở mục 8.

---

## 1. Hiện trạng nghiệp vụ (audit)

### 1.1 Luồng bán hàng — đã có

```
Order ──(create)── DeliveryNote
                       │
                       ├── Confirm()   → Reserve stock (giữ hàng, chưa trừ thực)
                       ├── Delivering  → (tracking)
                       ├── Delivered() → DispatchStock (trừ QuantityOnHand) + sinh CustomerDebt
                       └── Cancel()    → Release reserved (nếu trước đó đã Confirm)
```

- Trừ tồn ở `DeliveryNoteManager.MarkDeliveredAsync` (gọi `stockManager.DispatchStockAsync` đồng bộ).
- Sinh `CustomerDebt`: qua `DeliveryNoteDeliveredEventHandler` (idempotent theo `DeliveryNoteId`).
- 1 `Order` có thể có nhiều `DeliveryNote` — đúng nghiệp vụ chia nhiều lần giao.

### 1.2 Luồng nhập hàng — đã có

```
PurchaseOrder ──(receive)── GoodsReceipt
                                │
                                ├── Created()           → cộng tồn + (nếu đủ điều kiện) sinh VendorDebt
                                ├── ItemUnitCostSet()   → Full-recalc AverageCost + thử sinh VendorDebt
                                ├── VendorChanged()     → thử sinh VendorDebt (idempotent)
                                └── Deleted()           → hoàn nguyên tồn + dọn ảnh
```

- `GoodsReceipt.PurchaseOrderId` **nullable** → cho phép tạo phiếu nhập độc lập (không qua PurchaseOrder).
- `VendorDebt` có **2 constructor**: từ `PurchaseOrder` và từ `GoodsReceipt`. Đây là điểm cần lưu ý
  cho `VendorReturn` (xem mục 6.3).

### 1.3 Tồn kho

- `InventoryStock`: `QuantityOnHand`, `QuantityReserved`, `QuantityAvailable` (computed), `AverageCost`.
- `AverageCost` tính theo **Full Recalculation** mỗi lần `GoodsReceiptItem` được set `UnitCost`:
  `Σ(qty × unitCost) / Σ(qty)` trên các item đã có UnitCost cùng `(ProductId, WarehouseId)`.
- `StockMovementLog` ghi mọi cộng/trừ với `(MovementType, ReferenceType, ReferenceId)`.

### 1.4 Báo cáo tài chính — đã có nhưng có vấn đề

`FinancialReportAppService.GetProfitLossSummaryAsync` hiện tại tính như sau:
- **Doanh thu**: tổng `Order.OrderTotal` theo `Order.CreatedOnUtc` trong kỳ.
- **COGS**: `Σ OrderItem.CostPrice × Quantity`.
- **OpEx**: `Σ Expense.Amount`.

**Phát hiện gap (xem mục 4.2)** — cách tính này chưa khớp với kế toán thật.

---

## 2. Invariant cốt lõi cần thực thi

> Đây là invariant chủ dự án đặt ra. Mọi thiết kế tiếp theo phải tôn trọng nó.

```
┌─────────────────────────────────────────────────────────────────────┐
│  InventoryStock CHỈ thay đổi qua đúng 2 cổng:                       │
│    ▲ Cộng tồn  →  GoodsReceipt    (mọi nguồn cộng đều phải qua đây) │
│    ▼ Trừ tồn   →  DeliveryNote    (mọi nguồn trừ đều phải qua đây)  │
└─────────────────────────────────────────────────────────────────────┘
```

**Hệ quả:**
- Returns cộng/trừ tồn → phải sinh ra `GoodsReceipt`/`DeliveryNote` tương ứng (không đụng
  `InventoryStock` trực tiếp).
- `PurchaseOrder.ReceiveItem` → phải sinh `GoodsReceipt`.
- Tạo `Product` có sẵn stock → phải qua `GoodsReceipt` (initial inventory).
- Kiểm kê / hư hao / điều chỉnh → để **phase sau**, hiện tại đóng cửa hoàn toàn.
- Không có ngoại lệ "admin override".

**Để track được nhiều nguồn nhập/xuất**, mở rộng `GoodsReceipt` và `DeliveryNote` với `SourceType`:

```csharp
public enum GoodsReceiptSourceType
{
    FromVendor = 0,         // Mặc định — nhập từ NCC (qua PurchaseOrder hoặc độc lập)
    FromCustomerReturn = 1, // Auto-sinh khi CustomerReturn.Confirmed
    FromAdjustment = 2,     // Phase sau — kiểm kê, khởi tạo product
}

public enum DeliveryNoteSourceType
{
    ToCustomer = 0,         // Mặc định — bán cho KH
    ToVendorReturn = 1,     // Auto-sinh khi VendorReturn.Confirmed
    ToAdjustment = 2,       // Phase sau
}
```

Field `SourceType` quyết định business rule khác nhau (ví dụ: phiếu xuất loại `ToVendorReturn`
**không** sinh `CustomerDebt`; phiếu nhập loại `FromCustomerReturn` **không** sinh `VendorDebt`).

---

## 3. Các điểm vi phạm invariant hiện tại — phải sửa

| # | Vị trí | Hành vi vi phạm | Hướng sửa |
|---|--------|-----------------|-----------|
| V1 | `PurchaseOrderManager.ReceiveItemAsync` (line 302) | Gọi `_stockManager.ReceiveStockAsync` thẳng, không tạo `GoodsReceipt` | **Sửa**: bên trong tự tạo 1 `GoodsReceipt(PurchaseOrderId, SourceType=FromVendor)` trong cùng transaction. UX không đổi. |
| V2 | `ProductAppService` (line 152) | Tạo Product có sẵn stock → `_inventoryStockManager.AdjustStockAsync` | **Sửa**: bỏ phần initial stock khỏi flow tạo Product. User muốn có stock thì phải tạo `GoodsReceipt` riêng sau. |
| V3 | `InventoryController` actions: `ReceiveStock`, `DispatchStock`, `ReserveStock`, `AdjustStock` | UI gọi thẳng `InventoryAppService` thao tác stock | **Bỏ tất cả 4 actions** + 4 commands + 4 handlers + 4 views. |
| V4 | `IInventoryAppService` public methods: `ReceiveStockAsync`, `DispatchStockAsync`, `ReserveStockAsync`, `ReleaseReservedStockAsync`, `AdjustStockAsync` | Public ra ngoài → bị Controller gọi | **Bỏ khỏi `IInventoryAppService`** (delete file `IInventoryAppService` các method này, hoặc remove luôn nếu không còn ai gọi). Logic giữ trong `IInventoryStockManager` (Domain layer) cho các Manager khác dùng nội bộ. |
| V5 | `GoodsReceiptDeletedEventHandler` dùng `AdjustStockAsync` để hoàn nguyên tồn | Đây là rollback giao dịch gốc, không phải "phiếu mới" | **Chấp nhận tạm**, nhưng đổi cách: handler dùng method internal `IInventoryStockManager.RevertReceiveAsync(...)` (rename `AdjustStockAsync` thành nhiều method semantic, không expose generic adjust). Hoặc đơn giản: chỉ cho xóa `GoodsReceipt` khi chưa có downstream movement (đã có check `InsufficientStockException`) → giữ nguyên logic, đổi naming. |

**Lưu ý quan trọng cho V4/V5:** `InventoryStockManager` (Domain) vẫn cần các method này để Manager
khác gọi (ví dụ `DeliveryNoteManager.MarkDeliveredAsync` gọi `DispatchStockAsync`). Việc "đóng cửa"
là ở **lớp public ngoài Domain** (Application + Presentation). Domain manager nội bộ vẫn giao tiếp
với nhau như cũ.

---

## 4. Các gap khác (không bắt buộc trước Returns)

### 4.1 Inconsistency: nhập hàng dùng event handler, xuất hàng dùng inline

- `GoodsReceiptCreated` → handler **cộng tồn + sinh VendorDebt** (chạy SAU SaveChanges).
- `DeliveryNoteManager.MarkDeliveredAsync` → **trừ tồn inline** trong manager, chỉ phần sinh
  CustomerDebt mới qua handler.

→ Pattern bất nhất nhưng không vi phạm invariant. Để follow-up sau.

### 4.2 Báo cáo tài chính chưa đúng kế toán

Hiện tính doanh thu theo `Order.CreatedOnUtc` + `Order.OrderTotal`. Vấn đề:
- Đơn hàng tạo nhưng **chưa giao** vẫn tính doanh thu.
- Đơn hàng **đã hủy** vẫn nằm trong báo cáo (không thấy filter status).
- COGS dùng `OrderItem.CostPrice` chốt tại thời điểm tạo Order — không phải `AverageCost` lúc xuất.
- **Sau khi có Returns: doanh thu/COGS không tự trừ ra** — phải sửa.

→ Đề xuất ghi nhận `CostAtDispatch` trên `DeliveryNoteItem` lúc `MarkDelivered`, đổi nguồn báo cáo
sang `DeliveryNote.DeliveredOnUtc`. Tách thành todo follow-up.

### 4.3 Thiếu `StockReferenceType` cho Returns

Enum hiện tại:
```
None, PurchaseOrder, SalesOrder, StockIssue, StockTransfer, Adjustment, GoodsReceipt
```

Returns auto-sinh GoodsReceipt/DeliveryNote rồi → đã có `StockReferenceType.GoodsReceipt` và
`StockReferenceType.SalesOrder` dùng tốt. Tuy nhiên để truy vết dễ, có thể bổ sung:
```
CustomerReturn = 7,  // optional, vì phiếu nhập từ Returns có SourceType riêng
VendorReturn = 8,
```
→ Optional, không bắt buộc.

### 4.4 Edge case: trả hàng khi đơn đã `Locked`

`Order.LockOrder()` được gọi khi tất cả items đã `Delivered`. Hiện không có cơ chế "unlock". Khuyến
nghị **giữ logic hiện tại**: `CustomerReturn.Confirmed` không unlock Order. Order locked phản ánh
"đã giao xong", việc trả hàng là phát sinh sau.

### 4.5 Ràng buộc số lượng trên Returns

Cần Domain Manager check:
- `CustomerReturnManager.ConfirmAsync`: với mỗi `(OrderId, ProductId)`, sum `AcceptedQuantity` các
  CustomerReturn `Confirmed` ≤ tổng đã giao thực tế (`Σ DeliveryNoteItem.Quantity` của
  DeliveryNote `Delivered` cùng SourceType=ToCustomer).
- Tương tự cho VendorReturn.

→ Yêu cầu **bắt buộc**, viết unit test trước khi viết code.

---

## 5. Tư vấn AverageCost khi VendorReturn — Phương án A (đã chốt)

### Ngữ nghĩa

`AverageCost` được tính bằng Full Recalculation: `Σ(qty × unitCost) / Σ(qty)` trên toàn bộ
GoodsReceiptItem đã định giá cho `(ProductId, WarehouseId)`. Đây không phải "giá vốn của tồn kho
còn lại" mà là "giá vốn trung bình của toàn bộ hàng đã từng nhập".

### Quyết định: Phương án A — **không recalculate**

Lý do:
1. AverageCost hiện không đại diện cho hàng tồn còn lại → recalculate không làm "đúng hơn" theo
   nghĩa kế toán FIFO/LIFO.
2. Trả hàng cho NCC ở VLXD thường ít (hỏng, lỗi mác, sai chủng loại) — sai số nhỏ.
3. Phương án B yêu cầu lưu thêm bảng mapping → tăng phức tạp đáng kể.
4. `StockMovementLog` đầy đủ → sau này muốn FIFO/LIFO chính xác là dự án riêng, độc lập với Returns.

---

## 6. Returns Module — thiết kế cuối cùng

### 6.1 Đối xứng

| Luồng | Phiếu nghiệp vụ | Quan hệ cha | Sinh phiếu kho |
|-------|-----------------|-------------|----------------|
| Khách trả hàng | `CustomerReturn` | `Order (1) ─ (*) CustomerReturn` | `GoodsReceipt(SourceType=FromCustomerReturn)` |
| Trả NCC        | `VendorReturn`   | `PurchaseOrder (1) ─ (*) VendorReturn` (chính) hoặc `GoodsReceipt (1) ─ (*) VendorReturn` (fallback khi không có PO) | `DeliveryNote(SourceType=ToVendorReturn)` |

**Returns không trực tiếp đụng `InventoryStock`.** Khi `Confirmed`, handler sinh ra phiếu kho
tương ứng, rồi handler hiện có của `GoodsReceipt`/`DeliveryNote` xử lý phần cộng/trừ tồn.

### 6.2 Entity: CustomerReturn

```
CustomerReturn (entity riêng — chỉ track quy trình duyệt)
├─ Id (Guid, PK)
├─ Code (string)                    -- "TKH-yyyymmdd-NNN"
├─ OrderId (Guid)
├─ CustomerId (Guid)
├─ WarehouseId (Guid)               -- kho nhập hàng trả về
├─ ReturnDate (DateTime UTC)
├─ Note (string?)
├─ Status (CustomerReturnStatus)    -- Draft → Inspecting → Confirmed | Cancelled
├─ ConfirmedOnUtc (DateTime?)
├─ GeneratedGoodsReceiptId (Guid?)  -- null khi Draft/Inspecting/Cancelled, set khi Confirmed
├─ CreatedByUserId (Guid?)
├─ CreatedOnUtc / UpdatedOnUtc

CustomerReturnItem
├─ Id, CustomerReturnId
├─ ProductId
├─ DeliveryNoteItemId?              -- optional, để tra giá bán gốc
├─ RequestedQuantity                -- KH đề nghị trả
├─ AcceptedQuantity                 -- sau Inspecting: chấp nhận (≤ Requested)
├─ UnitPrice                        -- giá hoàn (mặc định = giá bán gốc)
```

**Hiệu ứng khi `Confirmed`** (handler `CustomerReturnConfirmedEventHandler`):
1. Tạo `GoodsReceipt` mới với:
   - `SourceType = FromCustomerReturn`
   - Items map từ `CustomerReturnItem` (ProductId, AcceptedQuantity, UnitCost = AverageCost hiện tại
     của `(ProductId, WarehouseId)` để báo cáo tồn vẫn nhất quán)
   - `WarehouseId` từ phiếu return
   - Note "Trả hàng từ KH theo phiếu {CustomerReturn.Code}"
2. Gọi `GoodsReceiptManager.MarkCreated()` → handler hiện có cộng tồn.
3. **KHÔNG sinh `VendorDebt`** — `GoodsReceipt.SourceType=FromCustomerReturn` skip nhánh sinh
   VendorDebt trong `GoodsReceiptCreatedHandler.TryCreateVendorDebtAsync` (thêm guard).
4. Trừ `Σ(AcceptedQuantity × UnitPrice)` vào `CustomerDebt` của Order này (FIFO theo `CreatedOnUtc`).
   Cho phép `RemainingAmount` xuống âm. Phần hoàn tiền mặt → phase sau.
5. Set `CustomerReturn.GeneratedGoodsReceiptId = newGoodsReceipt.Id`.

### 6.3 Entity: VendorReturn

```
VendorReturn (entity riêng)
├─ Id (Guid, PK)
├─ Code (string)                    -- "TNCC-yyyymmdd-NNN"
├─ PurchaseOrderId (Guid?)          ─┐
├─ GoodsReceiptId (Guid?)           ─┴ ràng buộc: ít nhất 1 không null
├─ VendorId (Guid)
├─ WarehouseId (Guid)               -- kho xuất hàng trả đi
├─ ReturnDate (DateTime UTC)
├─ Note (string?)
├─ Status (VendorReturnStatus)
├─ ConfirmedOnUtc (DateTime?)
├─ GeneratedDeliveryNoteId (Guid?)  -- set khi Confirmed
├─ CreatedByUserId (Guid?)
├─ CreatedOnUtc / UpdatedOnUtc

VendorReturnItem
├─ Id, VendorReturnId
├─ ProductId
├─ GoodsReceiptItemId?              -- để tra giá nhập gốc
├─ RequestedQuantity, AcceptedQuantity, UnitCost
```

**Hiệu ứng khi `Confirmed`** (handler `VendorReturnConfirmedEventHandler`):
1. Tạo `DeliveryNote` mới với:
   - `SourceType = ToVendorReturn`
   - Items map từ `VendorReturnItem`
   - `WarehouseId` từ phiếu return
   - `OrderId = null` (DeliveryNote.OrderId hiện không nullable → cần migration)
2. Gọi luồng tương đương `MarkDelivered` (skip Reserve vì không có khách giữ hàng) → trừ tồn thực
   sự `QuantityOnHand`. Nếu thiếu → `InsufficientStockException`.
3. **KHÔNG sinh `CustomerDebt`** — `DeliveryNote.SourceType=ToVendorReturn` skip nhánh trong
   `DeliveryNoteDeliveredEventHandler` (thêm guard).
4. Trừ `Σ(AcceptedQuantity × UnitCost)` vào `VendorDebt` (FIFO theo `PurchaseOrderId` hoặc
   `GoodsReceiptId` tương ứng).
5. Set `VendorReturn.GeneratedDeliveryNoteId`.

**Lưu ý migration**: `DeliveryNote.OrderId` đang non-nullable (`internal DeliveryNote(...Guid orderId,...)`).
Phải đổi thành nullable để chứa được `DeliveryNote(SourceType=ToVendorReturn)`. Migration phải xét
data hiện có (toàn bộ đang `OrderId != null` → an toàn để mở rộng nullable).

### 6.4 Status flow (cả 2 luồng)

```
Draft ──→ Inspecting ──→ Confirmed
  │                          │
  └────── Cancelled ←────────┘
```

### 6.5 Domain Events

| Event | Trigger | Handler |
|-------|---------|---------|
| `CustomerReturnConfirmed` | `Confirm()` | `CustomerReturnConfirmedEventHandler` — tạo GoodsReceipt(FromCustomerReturn) + giảm CustomerDebt |
| `CustomerReturnCancelled` | `Cancel()` | (audit only) |
| `VendorReturnConfirmed` | `Confirm()` | `VendorReturnConfirmedEventHandler` — tạo DeliveryNote(ToVendorReturn) + giảm VendorDebt |
| `VendorReturnCancelled` | `Cancel()` | (audit only) |

Idempotent: handler check `CustomerReturn.GeneratedGoodsReceiptId` (hoặc tương đương) — nếu đã có
thì skip.

### 6.6 Mở rộng `CustomerDebt` / `VendorDebt`

Thêm method `ApplyReturn(decimal amount, Guid returnId)` lên Debt entity → giảm `RemainingAmount`,
log `SourceReturnId` để idempotent.

---

## 7. Tác động lên handler hiện có

### `GoodsReceiptCreatedHandler`

Sửa nhánh `TryCreateVendorDebtAsync`: thêm guard
```csharp
if (goodsReceipt.SourceType == GoodsReceiptSourceType.FromCustomerReturn) return;
```

### `DeliveryNoteDeliveredEventHandler`

Sửa: thêm guard
```csharp
if (deliveryNote.SourceType == DeliveryNoteSourceType.ToVendorReturn) return;
```

### `DeliveryNoteManager.ConfirmAsync`

Khi `SourceType=ToVendorReturn`, **bỏ qua bước Reserve** (vì không có khách hàng giữ chỗ — đây là
hàng cửa hàng đang chuẩn bị trả NCC). Hoặc thiết kế lại flow Confirm/Delivered cho
`ToVendorReturn` → có thể skip Confirm và đi thẳng tới Delivered.

→ **Đề xuất đơn giản**: Khi handler `VendorReturnConfirmedEventHandler` chạy, nó tạo DeliveryNote
ở status `Delivered` luôn (skip Draft/Confirmed/Delivering). Logic này nằm trong `DeliveryNoteManager`
qua method mới `CreateAsDeliveredAsync(internal)` chỉ cho handler return gọi.

---

## 8. Thứ tự thực hiện (khi bạn duyệt plan)

> Mỗi bước kèm unit test (TDD), không chạy migration — bạn tự chạy.
>
> **Phase A — Hardening invariant** phải làm **trước** Phase B (Returns). Lý do: Returns dựa vào
> `SourceType` của GoodsReceipt/DeliveryNote, và dựa vào việc các đường vi phạm đã đóng.

### Phase A — Stock Invariant Hardening (ưu tiên cao nhất)

1. **A1**: Thêm enum `GoodsReceiptSourceType`, `DeliveryNoteSourceType` + field `SourceType` vào 2
   entity (default = `FromVendor` / `ToCustomer`). Migration.
2. **A2**: Sửa `PurchaseOrderManager.ReceiveItemAsync` → bên trong tự tạo 1 `GoodsReceipt(SourceType=FromVendor, PurchaseOrderId=...)` thay vì gọi `ReceiveStockAsync` thẳng.
   Cập nhật unit test.
3. **A3**: Sửa `ProductAppService.CreateProductAsync` (line 152) → bỏ phần initial stock. Document
   cho UI: muốn có stock thì tạo Product trước, rồi tạo GoodsReceipt sau.
4. **A4**: Bỏ 4 actions khỏi `InventoryController` (`ReceiveStock`, `DispatchStock`, `ReserveStock`,
   `AdjustStock`) + 4 commands + 4 handlers + 4 views.
5. **A5**: Bỏ 4 method khỏi `IInventoryAppService` public interface (logic vẫn còn trong Domain
   `InventoryStockManager` cho Manager khác dùng).
6. **A6**: Đảm bảo không ai gọi `IInventoryStockManager.AdjustStockAsync` ngoài
   `GoodsReceiptDeletedEventHandler` (đó là rollback). Nếu muốn clean hơn → đổi tên semantic
   `RevertReceiveAsync(goodsReceiptItemId)`.
7. **A7**: Smoke test toàn luồng: tạo PO → ReceiveItem → kiểm tra có GoodsReceipt mới sinh ra +
   tồn kho cộng đúng + có VendorDebt.

### Phase B — Returns Module

1. **B1 — Domain.Shared**:
   - Enum `CustomerReturnStatus`, `VendorReturnStatus`
   - Events: `CustomerReturnConfirmed`/`Cancelled`, `VendorReturnConfirmed`/`Cancelled`
   - Exceptions: `CustomerReturnNotFoundException`, `ExceedsDeliveredQuantityException`, …

2. **B2 — Domain Layer**:
   - Entities: `CustomerReturn`, `CustomerReturnItem`, `VendorReturn`, `VendorReturnItem`
   - Mark methods: `MarkCreated`, `Confirm`, `Cancel`
   - Extensions: `ToDto`
   - Mở rộng `CustomerDebt` / `VendorDebt`: thêm `ApplyReturn(amount, returnId)` + unit test
   - Sửa `DeliveryNote` cho phép `OrderId` nullable + migration

3. **B3 — Domain.Services**:
   - `ICustomerReturnManager` / `CustomerReturnManager` (TDD)
   - `IVendorReturnManager` / `VendorReturnManager` (TDD)
   - Bổ sung `DeliveryNoteManager.CreateAsDeliveredAsync(internal)` — chỉ cho VendorReturn handler
     gọi
   - Bổ sung `GoodsReceiptManager.CreateFromCustomerReturnAsync(internal)` — chỉ cho
     CustomerReturn handler gọi

4. **B4 — Application**:
   - `ICustomerReturnAppService` / `IVendorReturnAppService` + AppDtos (Validate)
   - Event handlers:
     - `CustomerReturnConfirmedEventHandler` — tạo GoodsReceipt + giảm CustomerDebt
     - `VendorReturnConfirmedEventHandler` — tạo DeliveryNote + giảm VendorDebt
   - Sửa `GoodsReceiptCreatedHandler.TryCreateVendorDebtAsync`: skip khi
     `SourceType=FromCustomerReturn`
   - Sửa `DeliveryNoteDeliveredEventHandler`: skip khi `SourceType=ToVendorReturn`

5. **B5 — Infrastructure**:
   - EF Configurations: `CustomerReturnConfiguration`, `VendorReturnConfiguration`
   - Migration (bạn tự chạy)

6. **B6 — Presentation (Web)**:
   - `CustomerReturnController`, `VendorReturnController`
   - `ICustomerReturnModelFactory`, `IVendorReturnModelFactory`
   - Models + Validators (FluentValidation)
   - MediatR Commands/Queries + Handlers
   - Views (List, Create, Detail, Inspect, Confirm)

### Phase C — Follow-ups (sau Phase B)

1. **C1**: Phiếu chi/hoàn tiền khi `CustomerDebt.RemainingAmount < 0`.
2. **C2**: Sửa `FinancialReportAppService` — đổi nguồn tính sang `DeliveryNote.DeliveredOnUtc`,
   filter `SourceType=ToCustomer`, trừ Returns.
3. **C3**: Snapshot `CostAtDispatch` trên `DeliveryNoteItem`.
4. **C4**: Thiết kế `StockAdjustmentNote` (kiểm kê / hư hao) hoặc mở rộng GoodsReceipt/DeliveryNote
   với `SourceType=Adjustment` — bạn quyết.
5. **C5**: Khôi phục lối tạo Product có sẵn stock — auto-sinh `GoodsReceipt(SourceType=Adjustment)`
   nếu user nhập initial stock (sau khi C4 xong).
6. **C6**: Thống nhất pattern side-effect cho Delivery (chuyển từ inline → handler).

---

## 9. Tổng kết các quyết định đã chốt

| # | Quyết định | Đã chốt |
|---|-----------|---------|
| 1 | Phạm vi | Audit + plan, chưa code |
| 2 | Hoàn tiền KH đã thanh toán | Cho công nợ xuống âm; phiếu chi làm phase sau |
| 3 | AverageCost khi VendorReturn | Phương án A — không recalc |
| 4 | Số lần trả/Order | Cho phép nhiều phiếu, có check tổng |
| 5 | VendorReturn link | `PurchaseOrderId` (chính) + `GoodsReceiptId` (fallback) |
| 6 | Code prefix | `TKH` (CustomerReturn), `TNCC` (VendorReturn) |
| 7 | Cách giảm Debt | `Debt.ApplyReturn(amount, returnId)` |
| 8 | Returns ↔ Phiếu kho | Auto-sinh `GoodsReceipt`/`DeliveryNote` (qua `SourceType`) |
| 9 | `PurchaseOrder.ReceiveItem` | Sửa để auto-tạo `GoodsReceipt` |
| 10 | UI direct stock endpoints | Bỏ tất cả (Receive/Dispatch/Reserve/Adjust) |
| 11 | Initial stock khi tạo Product | Bỏ — user tạo `GoodsReceipt` sau |
| 12 | Kiểm kê / điều chỉnh tồn | Khóa AdjustStock, thiết kế phase sau |

---

## 10. Câu hỏi mở cuối cùng

Hết câu hỏi. Nếu bạn duyệt plan này, ta bắt đầu **Phase A1** (thêm `SourceType`).

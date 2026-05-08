# TodoList — VLXD Tuấn Khôi / NamEcommerce

> File theo dõi các hạng mục còn pending. Lịch sử hoàn thành xem tại [ToDoList1.md](ToDoList1.md).

---

### Quy tắc bắt buộc

- **Unit test**: KHÔNG viết unit test mới, KHÔNG sửa code trong bất kỳ project `*.Test` nào.
- **Migration**: AI KHÔNG tự chạy migration — báo Tuấn tự chạy.
- **Skills**: AI đọc skill `namcommerce` trước khi viết code domain.

---

## 🔧 Pending — Build & Migrations & Smoke Test

**Migrations cần chạy thủ công** (tích lũy từ các session trước):

```
Add-Migration AddAverageCostToInventoryStock
Add-Migration AddVendorToGoodsReceiptAndDebt
Add-Migration AddOutboxMessages
Add-Migration AddSourceTypeToGoodsReceiptAndDeliveryNote
Add-Migration AddReturnsModule
Add-Migration AddCostAtDispatchToDeliveryNoteItem
Add-Migration AddCustomerRefund
Update-Database
```

**Smoke test** (Tuấn tự chạy):
- [ ] **A7** — Smoke test Phase A (GoodsReceipt auto-tạo từ PO, Product không có initial stock, xóa phiếu rollback đúng)
- [ ] **B7** — Smoke test Phase B (CustomerReturn + VendorReturn toàn flow, validate qty, cancel flow)

**Phase 5 cleanup** — chờ Tuấn quyết định:
- [ ] Xóa `Application.Services/Events/Orders/OrderCreatedEventHandler.cs` — CHỈ xóa nếu không implement Reserve Stock; hiện là stub rỗng

---

## Phase C — ✅ Hoàn thành (cập nhật 2026-05-08)

- [x] **C4** — StockAdjustmentNote (Domain + Application + Infrastructure + Presentation)
- [x] **C5** — Khôi phục initial stock khi tạo Product
- [x] **C7** — Xóa `IInventoryStockManager.AdjustStockAsync`

---

## Phase D — Returns UX & Price Model

**Cấp độ:** Trung bình

> **Mục tiêu**: Form tạo Khách trả hàng / Trả hàng NCC có thể dùng được trong thực tế.
> Thay nhập ID thủ công bằng typeahead + AJAX load. Bổ sung giá trả về và chi phí phát sinh để báo cáo tài chính chính xác.

### Quyết định thiết kế đã chốt

| Điểm | Quyết định |
|---|---|
| `CustomerReturn.OrderId` | Đổi → `DeliveryNoteId? (nullable)` — chọn phiếu xuất hoặc tạo tự do (null) |
| `CustomerReturnItem` | Thêm `OriginalUnitPrice decimal?` (tham chiếu) + `ReturnUnitPrice decimal` (giá trả về thực) |
| `VendorReturnItem` | Thêm `OriginalUnitCost decimal?` + `ReturnUnitCost decimal` |
| `AdditionalCost` | Header-level trên cả 2 phiếu — chi phí phát sinh (xe, bồi thường); tự động sinh `Expense` khi Confirm |
| Net amount | `Σ(AcceptedQty × ReturnUnitPrice) − AdditionalCost` |
| CustomerReturn nhập kho | GoodsReceipt theo `ReturnUnitPrice` (hàng đã giảm giá trị) |
| VendorReturn xuất kho | DeliveryNote theo `AverageCost` (chuẩn kế toán) |

---

### D1 — Domain.Shared: Cập nhật DTOs ✅ Done

- [x] `CustomerReturnDtos.cs`: `OrderId` → `DeliveryNoteId?`, `CustomerId?` (free-form), `AdditionalCost`, `OriginalUnitPrice?`, `ReturnUnitPrice`, `NetRefundAmount` computed
- [x] `VendorReturnDtos.cs`: `AdditionalCost`, `OriginalUnitCost?`, `ReturnUnitCost`, bỏ require PO/GR, `NetRecoveryAmount` computed
- [x] `CustomerReturnEvents.cs`: `CustomerReturnConfirmed` đổi `OrderId` → `DeliveryNoteId?`
- [x] `GoodsReceiptDtos.cs`: thêm `ReturnUnitPrice` vào `CreateGoodsReceiptFromCustomerReturnItemDto`

---

### D2 — Domain Layer: Cập nhật Entities ✅ Done

- [x] `CustomerReturn`: `OrderId/OrderCode` → `DeliveryNoteId?/DeliveryNoteCode?`; thêm `AdditionalCost`; `AddItem` nhận `originalUnitPrice?` + `returnUnitPrice`
- [x] `CustomerReturnItem`: bỏ `UnitPrice` → `OriginalUnitPrice decimal?` + `ReturnUnitPrice decimal`; `AcceptedTotal` dùng `ReturnUnitPrice`
- [x] `VendorReturn`: thêm `AdditionalCost`; `AddItem` nhận `originalUnitCost?` + `returnUnitCost`
- [x] `VendorReturnItem`: bỏ `UnitCost` → `OriginalUnitCost decimal?` + `ReturnUnitCost decimal`
- [x] `CustomerReturnExtensions.ToDto()`: map trường mới
- [x] `VendorReturnExtensions.ToDto()`: map trường mới

---

### D3 — Domain.Services: Cập nhật Managers

- [x] `ICustomerReturnManager`: `GetListAsync` đổi `orderId` → `deliveryNoteId`; `GetTotalConfirmedReturnQuantityAsync` đổi param; doc `FinalizeConfirmAsync` cập nhật
- [ ] `CustomerReturnManager`: rewrite `CreateAsync` (load DeliveryNote hoặc Customer), `ConfirmAsync` (validate by DeliveryNoteId), `FinalizeConfirmAsync` (FIFO by CustomerId + AdditionalCost → Expense), `GetListAsync`, `GetTotalConfirmedReturnQuantityAsync`; thay `IEntityDataReader<Order>` → `IEntityDataReader<Customer>`
- [ ] `VendorReturnManager`: cập nhật `CreateAsync`/`FinalizeConfirmAsync` map `AdditionalCost` + `ReturnUnitCost`; `FinalizeConfirmAsync` dùng net amount
- [ ] `GoodsReceiptManager.CreateFromCustomerReturnAsync`: dùng `item.ReturnUnitPrice` làm `UnitCost` (fallback AverageCost nếu = 0)
- [ ] `IVendorReturnManager.FinalizeConfirmAsync`: cập nhật signature nếu cần

---

### D4 — Application Layer: AppDtos + AppServices

- [ ] `CustomerReturnAppDtos.cs`: thêm price fields vào tất cả DTOs (App + Create + Item)
- [ ] `VendorReturnAppDtos.cs`: tương tự
- [ ] `CustomerReturnAppService` + `VendorReturnAppService`: cập nhật mapping
- [ ] Thêm vào `ICustomerReturnAppService` + implementation:
  - `GetDeliveryNotesByCustomerAsync(customerId)` → `List<DeliveryNotePickerAppDto>` (id, code, deliveredOnUtc)
  - `GetDeliveryNoteItemsForReturnAsync(deliveryNoteId)` → `List<ReturnableItemAppDto>` (productId, productName, unit, deliveredQty, alreadyReturnedQty, unitPrice)
- [ ] Thêm vào `IVendorReturnAppService` + implementation:
  - `GetGoodsReceiptsByVendorAsync(vendorId)` → `List<GoodsReceiptPickerAppDto>` (id, code, createdOnUtc)
  - `GetGoodsReceiptItemsForReturnAsync(goodsReceiptId)` → `List<ReturnableItemAppDto>` (productId, productName, unit, receivedQty, alreadyReturnedQty, unitCost)

---

### D5 — Infrastructure: EF Mapping + Migration

- [ ] `CustomerReturnMapping`: đổi `OrderId` → `DeliveryNoteId`; thêm `AdditionalCost decimal(18,4) default 0`
- [ ] `CustomerReturnItemMapping`: thêm `OriginalUnitPrice decimal(18,4) nullable`, `ReturnUnitPrice decimal(18,4) not null default 0`
- [ ] `VendorReturnMapping`: thêm `AdditionalCost decimal(18,4) default 0`
- [ ] `VendorReturnItemMapping`: thêm `OriginalUnitCost decimal(18,4) nullable`, `ReturnUnitCost decimal(18,4) not null default 0`
- [ ] **Migration** (Tuấn tự chạy): `Add-Migration UpdateReturnsAddPriceAndDeliveryNoteRef`

---

### D6 — Web.Contracts: Commands / Queries / Models

- [ ] `CreateCustomerReturnCommand`: thêm `DeliveryNoteId?`, `AdditionalCost`; item thêm `OriginalUnitPrice?`, `ReturnUnitPrice`
- [ ] `UpdateCustomerReturnCommand`: cập nhật tương tự
- [ ] `CreateVendorReturnCommand` + Update: thêm `AdditionalCost`; item thêm `OriginalUnitCost?`, `ReturnUnitCost`
- [ ] Thêm 4 Queries tại `Web.Contracts/Queries/Models/Returns/`:
  - `GetDeliveryNotesByCustomerQuery { CustomerId }` → `IRequest<List<DeliveryNotePickerModel>>`
  - `GetDeliveryNoteItemsForReturnQuery { DeliveryNoteId }` → `IRequest<List<ReturnableItemModel>>`
  - `GetGoodsReceiptsByVendorQuery { VendorId }` → `IRequest<List<GoodsReceiptPickerModel>>`
  - `GetGoodsReceiptItemsForReturnQuery { GoodsReceiptId }` → `IRequest<List<ReturnableItemModel>>`
- [ ] Thêm Models: `DeliveryNotePickerModel { Id, Code, DeliveredOnUtc }`, `GoodsReceiptPickerModel { Id, Code, CreatedOnUtc }`, `ReturnableItemModel { ProductId, ProductName, Unit, OriginalQty, AlreadyReturnedQty, UnitPrice }`
- [ ] Cập nhật `CustomerReturnModel` + `VendorReturnModel`: thêm price fields

---

### D7 — Web.Framework: Handlers

- [ ] `GetDeliveryNotesByCustomerHandler`: query DeliveryNote → filter `CustomerId` + `SourceType=ToCustomer` + `Status=Delivered`
- [ ] `GetDeliveryNoteItemsForReturnHandler`: query items → tính `alreadyReturnedQty` từ CustomerReturn Confirmed cùng DeliveryNoteId
- [ ] `GetGoodsReceiptsByVendorHandler`: query GoodsReceipt → filter `VendorId` + `SourceType=FromVendor`
- [ ] `GetGoodsReceiptItemsForReturnHandler`: query items → tính `alreadyReturnedQty` từ VendorReturn Confirmed cùng GoodsReceiptId
- [ ] Cập nhật `CreateCustomerReturnHandler` + `CreateVendorReturnHandler`: map price fields mới

---

### D8 — Web: Controllers + Views

- [ ] `CustomerReturnController`: thêm 2 AJAX actions:
  - `GET /CustomerReturn/GetDeliveryNotes?customerId=` → JSON
  - `GET /CustomerReturn/GetDeliveryNoteItems?deliveryNoteId=` → JSON
- [ ] `VendorReturnController`: thêm 2 AJAX actions:
  - `GET /VendorReturn/GetGoodsReceipts?vendorId=` → JSON
  - `GET /VendorReturn/GetGoodsReceiptItems?goodsReceiptId=` → JSON
- [ ] Redesign `CustomerReturn/Create.cshtml`:
  - **Xóa** input OrderId / ProductId thủ công
  - Khách hàng: `<select>` searchable (Choices.js)
  - Phiếu xuất: `<select>` load AJAX theo customerId (nullable — để trống = tạo tự do)
  - Bảng items: nếu chọn phiếu → load AJAX + fill sẵn; nếu tự do → nút "+ Thêm hàng" với product search
  - Mỗi row: `Tên hàng | ĐVT | Đã giao | Đã trả | Còn lại | SL trả | Đơn giá gốc | Đơn giá trả về`
  - Footer: `Chi phí phát sinh` | `Tổng hoàn = Σ(SL × Đơn giá trả) − Chi phí`
  - Warehouse: `<select>` dropdown
- [ ] Redesign `VendorReturn/Create.cshtml`: tương tự — NCC → Phiếu nhập → Items
- [ ] Update `CustomerReturn/Details.cshtml`: hiển thị `ReturnUnitPrice` từng dòng, `AdditionalCost`, net amount
- [ ] Update `VendorReturn/Details.cshtml`: tương tự
- [ ] `CustomerReturnModelFactory` + `VendorReturnModelFactory`: cập nhật mapping nếu cần

---

### D9 — Migration (Tuấn tự chạy)

```
Add-Migration UpdateReturnsAddPriceAndDeliveryNoteRef
Update-Database
```

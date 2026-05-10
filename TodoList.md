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

### D3 — Domain.Services: Cập nhật Managers ✅ Done

- [x] `ICustomerReturnManager`: `GetListAsync` đổi `orderId` → `deliveryNoteId`; `GetTotalConfirmedReturnQuantityAsync` đổi param; doc `FinalizeConfirmAsync` cập nhật
- [x] `CustomerReturnManager`: rewrite `CreateAsync` (load DeliveryNote hoặc Customer), `ConfirmAsync` (validate by DeliveryNoteId), `FinalizeConfirmAsync` (FIFO by CustomerId + AdditionalCost → Expense), `GetListAsync`, `GetTotalConfirmedReturnQuantityAsync`; thay `IEntityDataReader<Order>` → `IEntityDataReader<Customer>`
- [x] `VendorReturnManager`: cập nhật `CreateAsync`/`FinalizeConfirmAsync` map `AdditionalCost` + `ReturnUnitCost`; `FinalizeConfirmAsync` dùng net amount + Expense
- [x] `GoodsReceiptManager.CreateFromCustomerReturnAsync`: dùng `item.ReturnUnitPrice` làm `UnitCost` (fallback AverageCost nếu = 0)
- [x] Event handlers: `CustomerReturnConfirmedEventHandler` + `VendorReturnConfirmedEventHandler` map trường mới, pass `NetRefundAmount`/`NetRecoveryAmount`

---

### D4 — Application Layer: AppDtos + AppServices ✅ Done

- [x] `CustomerReturnAppDtos.cs`: thêm `DeliveryNoteId?`, `DeliveryNoteCode?`, `AdditionalCost`, `NetRefundAmount`, `OriginalUnitPrice?`, `ReturnUnitPrice`; `Validate()` cập nhật
- [x] `VendorReturnAppDtos.cs`: thêm `AdditionalCost`, `NetRecoveryAmount`, `OriginalUnitCost?`, `ReturnUnitCost`; bỏ require PO/GR
- [x] `ICustomerReturnAppService.GetListAsync`: `orderId` → `deliveryNoteId`
- [x] `CustomerReturnAppService` + `VendorReturnAppService`: cập nhật mapping đầy đủ
- [x] App Extensions: `CustomerReturnAppExtensions` + `VendorReturnAppExtensions` map trường mới

> Note: các AJAX helper methods (GetDeliveryNotesByCustomer, GetDeliveryNoteItemsForReturn...) sẽ implement ở D6/D7 qua Query Handlers thay vì AppService.

---

### D5 — Infrastructure: EF Mapping + Migration ✅ Done

- [x] `CustomerReturnMapping`: đổi `OrderId/OrderCode` → `DeliveryNoteId?/DeliveryNoteCode?`; thêm `AdditionalCost decimal(18,4) default 0`; index đổi sang DeliveryNoteId
- [x] `CustomerReturnItemMapping`: thêm `OriginalUnitPrice decimal(18,4) nullable`, `ReturnUnitPrice decimal(18,4) default 0`
- [x] `VendorReturnMapping`: thêm `AdditionalCost decimal(18,4) default 0`
- [x] `VendorReturnItemMapping`: thêm `OriginalUnitCost decimal(18,4) nullable`, `ReturnUnitCost decimal(18,4) default 0`
- [ ] **Migration** (Tuấn tự chạy): `Add-Migration UpdateReturnsAddPriceAndDeliveryNoteRef`

---

### D6 — Web.Contracts: Commands / Queries / Models ✅ Done

- [x] `CreateCustomerReturnCommand`: `DeliveryNoteId?`, `CustomerId?`, `AdditionalCost`; item: `OriginalUnitPrice?`, `ReturnUnitPrice`
- [x] `CreateVendorReturnCommand`: `AdditionalCost`; item: `OriginalUnitCost?`, `ReturnUnitCost`; bỏ require PO/GR
- [x] `GetCustomerReturnListQuery`: `orderId` → `deliveryNoteId`; `CustomerReturnListModel`: đổi `OrderCode` → `DeliveryNoteCode`
- [x] 4 Queries: `GetDeliveryNotesByCustomerQuery`, `GetDeliveryNoteItemsForReturnQuery`, `GetGoodsReceiptsByVendorQuery`, `GetGoodsReceiptItemsForReturnQuery`
- [x] 3 Models mới: `DeliveryNotePickerModel`, `GoodsReceiptPickerModel`, `ReturnableItemModel` tại `ReturnPickerModels.cs`
- [x] `CustomerReturnModel` + `VendorReturnModel`: thêm `DeliveryNoteId?/Code?`, `AdditionalCost`, `NetRefundAmount`/`NetRecoveryAmount`, price fields trên items

---

### D7 — Web.Framework: Handlers ✅ Done

- [x] 4 AJAX Handlers: `GetDeliveryNotesByCustomerHandler`, `GetDeliveryNoteItemsForReturnHandler`, `GetGoodsReceiptsByVendorHandler`, `GetGoodsReceiptItemsForReturnHandler`
- [x] `CreateCustomerReturnHandler` + `CreateVendorReturnHandler`: map `AdditionalCost` + price fields
- [x] `GetCustomerReturnHandler` + `GetVendorReturnHandler`: map `AdditionalCost`, `DeliveryNoteId`, price fields
- [x] `GetCustomerReturnListHandler` + `GetVendorReturnListHandler`: dùng `NetRefundAmount`/`NetRecoveryAmount`
- [x] AppService interfaces: thêm 4 picker methods; AppService impl: inject `IEntityDataReader<DeliveryNote/GoodsReceipt/CustomerReturn/VendorReturn/Product/UnitMeasurement>`
- [x] `ReturnPickerAppDtos.cs`: `DeliveryNotePickerAppDto`, `GoodsReceiptPickerAppDto`, `ReturnableItemAppDto`

---

### D8 — Web: Controllers + Views ✅ Done (2026-05-09)

- [x] `CustomerReturnController`: thêm 2 AJAX actions (`GetDeliveryNotes`, `GetDeliveryNoteItems`)
- [x] `VendorReturnController`: thêm 2 AJAX actions (`GetGoodsReceipts`, `GetGoodsReceiptItems`)
- [x] Redesign `CustomerReturn/Create.cshtml`: CustomerPicker + AJAX delivery note picker + item grid (ĐVT | Đã giao | Đã trả | Còn lại | SL trả | Đ.Giá gốc | Đ.Giá trả) + footer (Chi phí | Tổng hoàn)
- [x] Redesign `VendorReturn/Create.cshtml`: VendorPicker + AJAX goods receipt picker + item grid tương tự
- [x] Update `CustomerReturn/Details.cshtml`: cột OriginalUnitPrice + ReturnUnitPrice, tfoot AdditionalCost + NetRefundAmount
- [x] Update `VendorReturn/Details.cshtml`: tương tự
- [x] `CustomerReturnModelFactory` + `VendorReturnModelFactory`: đơn giản hoá — PrepareDetails trả trực tiếp `CustomerReturnModel?`/`VendorReturnModel?`
- [x] `CreateCustomerReturnModel` + `CreateVendorReturnModel`: cập nhật fields (CustomerId, DeliveryNoteId, AdditionalCost, OriginalUnitPrice, ReturnUnitPrice...)
- [x] `CustomerReturnListSearchModel`: `OrderId` → `DeliveryNoteId`

---

### D9 — Migration (Tuấn tự chạy)

```
Add-Migration UpdateReturnsAddPriceAndDeliveryNoteRef
Update-Database
```

---

## Phase E — Returns UX & Initial Debt

### E1 — Công nợ ban đầu khi tạo Vendor ✅ Done (2026-05-10)

- [x] `CreateInitialVendorDebtDto` trong `VendorDebtDtos.cs`
- [x] `VendorDebt` entity: thêm constructor 5-tham số (không gắn PO/GR)
- [x] `IVendorDebtManager`: thêm `CreateInitialDebtAsync`
- [x] `VendorDebtManager`: implement `CreateInitialDebtAsync`
- [x] `CreateVendorCommand`: thêm `InitialDebt decimal?`
- [x] `CreateVendorHandler`: inject `IVendorDebtManager`, gọi sau khi tạo Vendor thành công
- [x] `CreateVendorModel`: thêm `InitialDebt decimal?`
- [x] `VendorController.Create`: map `InitialDebt`
- [x] `Vendor/Create.cshtml`: thêm field "Công nợ ban đầu" trong tab Cài đặt

### E2 — Công nợ ban đầu khi tạo Customer ✅ Done (2026-05-10)

- [x] `CreateInitialCustomerDebtDto` trong `DebtDtos.cs`
- [x] `CustomerDebt` entity: thêm constructor 5-tham số (không gắn DeliveryNote/Order)
- [x] `ICustomerDebtManager`: thêm `CreateInitialDebtAsync`
- [x] `CustomerDebtManager`: implement `CreateInitialDebtAsync`
- [x] `CreateCustomerCommand`: thêm `InitialDebt decimal?`
- [x] `CreateCustomerHandler`: inject `ICustomerDebtManager`, gọi sau khi tạo Customer thành công
- [x] `CreateCustomerModel`: thêm `InitialDebt decimal?`
- [x] `CustomerController.Create`: map `InitialDebt`
- [x] `Customer/Create.cshtml`: thêm field "Công nợ ban đầu"

### E3 — Cancel protection cho DeliveryNote/GoodsReceipt (Approach 2)

> Block cancel nếu có Confirmed returns. Auto-cancel Draft/Inspecting returns khi parent bị cancel.

- [ ] `DeliveryNoteManager.CancelAsync`: kiểm tra Confirmed CustomerReturns → block; auto-cancel Draft/Inspecting CustomerReturns
- [ ] `GoodsReceiptManager.CancelAsync`: tương tự cho VendorReturns

### E4 — Return info trên DeliveryNote/GoodsReceipt Details

> Cột "Đã trả" trong items table (chỉ khi có returns). Section "Khách trả hàng" / "Trả hàng NCC".

- [ ] Queries + Handlers để load linked returns
- [ ] Update `DeliveryNote/Details.cshtml`
- [ ] Update `GoodsReceipt/Details.cshtml`

### E5 — Quick-create return button trên Details pages

- [ ] `CustomerReturn/Create` GET nhận `deliveryNoteId` param → pre-fill form
- [ ] `VendorReturn/Create` GET nhận `goodsReceiptId` param → pre-fill form
- [ ] Button "Tạo phiếu trả" trên `DeliveryNote/Details.cshtml`
- [ ] Button "Tạo phiếu trả NCC" trên `GoodsReceipt/Details.cshtml`

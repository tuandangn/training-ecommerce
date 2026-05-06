# TodoList — VLXD Tuấn Khôi / NamEcommerce

> File theo dõi các hạng mục cần thực thi. Cập nhật trạng thái khi bắt đầu / hoàn thành.
> Các phần đã hoàn thành được lưu tại [CheckList.md](CheckList.md).

---

### Quy tắc khác
- **Unit test**: KHÔNG viết unit test mới, KHÔNG sửa code trong bất kỳ project `*.Test` nào — Tuấn tự bổ sung/cập nhật test sau.
- **Migration**: AI KHÔNG tự chạy migration — báo Tuấn tự chạy.
- **Skills**: AI đọc skill `namcommerce` trước khi viết code domain.

---

## 🔧 Pending Actions — Build & Smoke Test

*(Tích lũy qua các session 2026-04-28 → 2026-05-06)*

**1. Build verify** toàn bộ solution — `dotnet build NamEcommerce.sln`. Các thay đổi cần check compile sạch:

- Session 2026-04-28: DI cho `IEntityDataReader<StockMovementLog>` + `IEntityDataReader<GoodsReceipt>` + `IVendorDebtManager`; `VendorDebtManagerTests.cs` (helper 7→8 params); Notification + Vendor + Handler module.
- Session 2026-04-30: `PurchaseOrderManager` (12→11 deps, bỏ `IEventPublisher`); `PurchaseOrderManagerTests.cs` (22 constructor calls); `PurchaseOrderItemReceivedEventHandler` (mới); `PurchaseOrderUpdatedEventHandler.cs` (đang là stub — Tuấn xoá file thủ công).
- Session 2026-05-06 (Phase 4 Outbox): `OutboxMessage` entity (Domain), `OutboxMessageMapping` (Data.SqlServer), `IOutbox` + `OutboxAccessor`, `IIntegrationEvent` (kế thừa MediatR `INotification`), `DeliveryNoteConfirmedIntegrationEvent` + handler, `OutboxProcessor` background service. Csproj `Data.SqlServer` thêm 3 PackageReference: `Microsoft.Extensions.Hosting.Abstractions` 10.0.0, `Microsoft.Extensions.DependencyInjection.Abstractions` 10.0.0, `Microsoft.Extensions.Logging.Abstractions` 10.0.0. `Program.cs` đăng ký `IOutbox`, `OutboxProcessorOptions`, `AddHostedService<OutboxProcessor>`.
- Session 2026-05-07 (Phase A1 SourceType): `GoodsReceipt` + `DeliveryNote` entities có thêm `SourceType` (`internal set`) với default tương ứng. `GoodsReceiptMapping.cs` + `DeliveryNoteMap.cs` (Data.SqlServer) cấu hình cột `SourceType` với `IsRequired().HasDefaultValue(...).HasConversion<int>()`. Imports: `using NamEcommerce.Domain.Shared.Enums.GoodsReceipts;` đã thêm vào GoodsReceipt entity.
- Session 2026-05-07 (Phase A2 GoodsReceipt auto-create): `PurchaseOrderManager` (11→8 deps, bỏ `IInventoryStockManager` + `IEntityDataReader<InventoryStock>` + `IRepository<Product>` + `IRepository<ProductPriceHistory>`, thêm `IGoodsReceiptManager`); `GoodsReceiptManager` thêm method `CreateFromPurchaseOrderReceivingAsync`; `IGoodsReceiptManager` + `GoodsReceiptDtos.cs` thêm `CreateGoodsReceiptFromPurchaseOrderDto`; `PurchaseOrderManagerTests.cs` (constructor 11→8 params, usings cập nhật, 3 test cũ xoá, 2 test mới thêm).

**2. Migrations cần chạy thủ công:**
- `Add-Migration AddAverageCostToInventoryStock` (project Data.SqlServer)
- `Add-Migration AddVendorToGoodsReceiptAndDebt`
- `Add-Migration AddOutboxMessages` (Phase 4 — bảng `tbl.OutboxMessage` với index `IX_OutboxMessage_Pending`)
- `Add-Migration AddSourceTypeToGoodsReceiptAndDeliveryNote` (Phase A1 — cột `SourceType` cho 2 bảng, default = 0)
- `Update-Database`

**3. Smoke test** flow nghiệp vụ:

**AverageCost flow:**
- Tạo phiếu nhập có WarehouseId + UnitCost → tồn kho cộng + AverageCost cập nhật
- Tạo phiếu nhập KHÔNG có UnitCost → tồn cộng, AverageCost không thay đổi
- Set UnitCost cho item của phiếu nhập đó → AverageCost cập nhật theo Full Recalculation
- Tạo phiếu nhập thứ 2 cùng (Product, Warehouse) khác giá → set giá → AverageCost = trung bình cộng có trọng số

**Xóa phiếu:**
- Thử xóa phiếu đã sinh tồn → exception `GoodsReceiptHasStockMovementsException` (key VI hiển thị đúng)

**Vendor + sinh công nợ tự động:**
- Tạo phiếu KHÔNG vendor + items có UnitCost đầy đủ → KHÔNG sinh nợ
- Tạo phiếu CÓ vendor + items có UnitCost đầy đủ → sinh 1 phiếu `VendorDebt` với `GoodsReceiptId = phiếu.Id`, `TotalAmount = Σ(qty × cost)`
- Tạo phiếu CÓ vendor nhưng items chưa định giá → KHÔNG sinh nợ; sau khi set UnitCost cho item cuối → sinh nợ
- Phiếu CÓ items định giá đầy đủ nhưng KHÔNG vendor → POST `/GoodsReceipt/SetVendor` với `vendorId` → sinh nợ
- Gọi POST `/GoodsReceipt/SetVendor` 2 lần liên tiếp với cùng vendor → CHỈ 1 phiếu nợ (idempotency)

**Event Refactor smoke test:**
- App start lên + SaveChanges hoạt động bình thường với interceptor mới
- Order flow: tạo Order → `OrderPlaced` event dispatch đúng
- DeliveryNote confirmed flow → **Outbox enqueue** `DeliveryNoteConfirmedIntegrationEvent` → background service publish → n8n nhận notification (kiểm tra log + bảng `tbl.OutboxMessage` có dòng với `ProcessedOnUtc != null`)
- DeliveryNote delivered flow → `CustomerDebt` sinh tự động
- Tạo phiếu nhập (GoodsReceipt) → định giá đủ + vendor → `VendorDebtCreated` event dispatch
- Trả hết nợ → `VendorDebtFullyPaid` event publish
- **Outbox failure scenarios (Phase 4):**
  - Stop n8n → confirm DeliveryNote → message lưu vào Outbox với `Error` + `RetryCount` tăng → start n8n → message tự retry và `ProcessedOnUtc` được set
  - Rollback transaction nghiệp vụ → message KHÔNG được lưu vào Outbox (atomic)
  - Restart app khi có message chưa processed → background service pick up tiếp tục
- **PurchaseOrder flow (mới session 2026-04-30):**
  - Tạo PO → `PurchaseOrderCreated` event dispatch
  - Update PO → `PurchaseOrderUpdated` event dispatch
  - Add item → `PurchaseOrderItemAdded` event dispatch (KHÔNG còn trigger `VerifyStatus` thừa)
  - Change status (Draft→Submitted→Approved) → `PurchaseOrderStatusChanged` event dispatch với oldStatus/newStatus đúng
  - Receive item → `PurchaseOrderItemReceived` event dispatch → handler `PurchaseOrderItemReceivedEventHandler` gọi `VerifyStatusAsync` → đơn tự transition Approved → Receiving khi receivedQty > 0
  - Receive đủ qty cho mọi item → đơn tự transition Receiving → Completed
  - Delete item → `PurchaseOrderItemRemoved` event dispatch

---

## System - Event

**Cấp độ:** Khó

### [PRIORITY: HIGH] Refactor Event System theo DDD đúng chuẩn

> Phase 1 (Foundation) + Phase 2 (Orders/DeliveryNotes) + **Phase 3 hoàn tất 100%** + **Phase 4 hoàn tất 100% (session 2026-05-06)**.
> Xem lịch sử đầy đủ tại [CheckList.md](CheckList.md).

---

#### Phase 5 — Cleanup (0.5 ngày) — PREREQUISITE đã DONE 100%

> Audit session 2026-05-02: KHÔNG còn caller nào của `.EntityCreated()` / `.EntityUpdated()` / `.EntityDeleted()` extension methods trong toàn solution. `IEventPublisher` không còn được Manager / AppService / Handler nào inject. Sau session 3 (2026-05-02): KHÔNG còn subscriber nào của `EntityCreatedNotification<T>` / `EntityUpdatedNotification<T>` / `EntityDeletedNotification<T>` trong toàn solution → sẵn sàng xoá legacy types.

**Stub file còn lại (cần Tuấn quyết định):**
- [ ] `Application.Services/Events/Orders/OrderCreatedEventHandler.cs` — *CHỈ XOÁ* nếu Tuấn không có ý định implement Reserve Stock. Hiện tại đã subscribe concrete `OrderPlaced` event với body rỗng + TODO comment để giữ kiến trúc cho việc implement sau. Nếu xác nhận không implement → xoá file.

**Đã xoá session 5 (2026-05-03):** ✅ Toàn bộ legacy event chain + BaseEvent + 2 unused DeliveryNote events đã `mv` ra `.trash/` + remove DI registration. Xem chi tiết tại `CheckList.md`.

---

**Rủi ro / Lưu ý chung:**
- `AppAggregateEntity` là `record` — `_domainEvents` (List reference type) hoạt động bình thường nhưng phải `[NotMapped]`
- Test fixtures cũ verify `IEventPublisher` mock — đã được update qua các session trước; nếu phát hiện test còn sót, sửa sang assert `DomainEvents` collection
- Outbox (Phase 4) cần idempotency cho handler để tránh duplicate side effect khi retry

---

## Stock Invariant Hardening — Phase A (PREREQUISITE cho Returns)

**Cấp độ:** Khó

> **Mục tiêu**: thực thi invariant "InventoryStock CHỈ thay đổi qua GoodsReceipt (cộng) và DeliveryNote (trừ)".
> Phase này phải hoàn tất TRƯỚC Phase B (Returns) vì Returns dựa vào `SourceType` và việc các đường vi phạm đã đóng.
> Chi tiết tại [IMPROVEMENT_PLAN.md](IMPROVEMENT_PLAN.md) mục 2 + 3 + 8.

### ✅ A2 — Sửa `PurchaseOrderManager.ReceiveItemAsync` auto-tạo GoodsReceipt *(Done 2026-05-07)*

- [x] Refactor `ReceiveItemAsync`: thay vì gọi `_stockManager.ReceiveStockAsync` thẳng, nội bộ tạo 1 `GoodsReceipt(SourceType=FromVendor, PurchaseOrderId, VendorId từ PO)` với items map từ payload
- [x] Gọi `goodsReceipt.MarkCreated()` → handler `GoodsReceiptCreatedHandler` tự xử lý cộng tồn + sinh `VendorDebt`
- [x] Loại bỏ logic Product.UpdatePrice + ProductPriceHistory hiện đang nằm trong ReceiveItemAsync (line 280-300) — đảm bảo logic này đã được handle ở `GoodsReceiptItemUnitCostSetHandler` (verify duplicate)
- [x] Đảm bảo `MarkItemReceived` event của PurchaseOrder vẫn fire để `PurchaseOrderItemReceivedEventHandler` chạy `VerifyStatus` (Approved → Receiving → Completed)

### [PRIORITY: HIGH] A3 — Sửa `ProductAppService.CreateProductAsync` bỏ initial stock

- [ ] Loại bỏ phần gọi `_inventoryStockManager.AdjustStockAsync` (line 152) trong flow tạo Product
- [ ] Cập nhật DTO `CreateProductAppDto` / Form Model — bỏ field initial stock (hoặc đánh dấu deprecated)
- [ ] Cập nhật View tạo Product — thêm note "Để có tồn kho ban đầu, tạo phiếu nhập (GoodsReceipt) sau khi tạo sản phẩm"
- [ ] Cập nhật Validator nếu cần

### [PRIORITY: HIGH] A4 — Bỏ 4 actions InventoryController + Commands/Handlers/Views

- [ ] Bỏ `InventoryController.ReceiveStock` (GET + POST) + view `Views/Inventory/ReceiveStock.cshtml`
- [ ] Bỏ `InventoryController.DispatchStock` + view
- [ ] Bỏ `InventoryController.ReserveStock` + view
- [ ] Bỏ `InventoryController.AdjustStock` + view
- [ ] Xóa `ReceiveStockHandler`, `DispatchStockHandler`, `ReserveStockHandler`, `AdjustStockHandler` (Web.Framework/Commands/Handlers/Inventory)
- [ ] Xóa `ReceiveStockCommand`, `DispatchStockCommand`, `ReserveStockCommand`, `AdjustStockCommand` (Web.Contracts/Commands/Models/Inventory)
- [ ] Xóa `ReceiveStockModel`, `DispatchStockModel`, `ReserveStockModel`, `ReserveStockResultModel`, `AdjustStockModel` (Web.Contracts/Models/Inventory)
- [ ] Bỏ link/menu trong layout/sidebar trỏ tới các action đã xoá

### [PRIORITY: HIGH] A5 — Bỏ 4 method public stock thao tác khỏi `IInventoryAppService`

- [ ] Remove khỏi `IInventoryAppService`: `ReceiveStockAsync`, `DispatchStockAsync`, `ReserveStockAsync`, `ReleaseReservedStockAsync`, `AdjustStockAsync`
- [ ] Xóa implementation tương ứng trong `InventoryAppService`
- [ ] Xóa DTOs liên quan ở `Application.Contracts/Dtos/Inventory/InventoryAppDtos.cs` (`ReceiveStockAppDto`, `DispatchStockAppDto`, `ReserveStockAppDto`, `AdjustStockAppDto`, `ReleaseStockAppDto`) nếu không còn ai dùng
- [ ] Xác nhận `IInventoryStockManager` (Domain) **vẫn giữ** các method này — Manager khác (`DeliveryNoteManager`, `GoodsReceiptCreatedHandler`, `GoodsReceiptDeletedEventHandler`) dùng nội bộ

### [PRIORITY: MEDIUM] A6 — Đổi naming `AdjustStockAsync` cho rollback use-case

- [ ] Thêm method semantic mới `RevertReceiveAsync(productId, warehouseId, quantity, goodsReceiptId, modifiedByUserId)` vào `IInventoryStockManager`
- [ ] Implementation: tương tự `AdjustStockAsync` nhưng ghi `StockMovementLog` với `MovementType=Adjustment, ReferenceType=GoodsReceipt, Note='Hoàn nguyên do xóa phiếu {goodsReceiptId}'`
- [ ] Cập nhật `GoodsReceiptDeletedEventHandler` dùng API mới thay cho `AdjustStockAsync`
- [ ] (Optional) Đánh dấu `IInventoryStockManager.AdjustStockAsync` là `[Obsolete]` để dễ phát hiện caller mới — sẽ remove ở Phase C khi có `StockAdjustmentNote`

### [PRIORITY: HIGH] A7 — Smoke test Phase A

- [ ] Tạo PO → Receive item → kiểm tra: 1 `GoodsReceipt` mới với `SourceType=FromVendor`, `PurchaseOrderId` đúng; tồn kho cộng đúng; `VendorDebt` sinh đúng (nếu đủ điều kiện vendor + UnitCost)
- [ ] Tạo Product mới (không còn nút initial stock) → kiểm tra Product được tạo, không có stock; tạo `GoodsReceipt` riêng → tồn kho = đúng
- [ ] Truy cập 4 endpoint cũ trong InventoryController → 404 Not Found
- [ ] DeliveryNote.MarkDelivered → vẫn trừ tồn đúng (qua `DeliveryNoteManager` — internal call, không qua `IInventoryAppService` public)
- [ ] Xóa GoodsReceipt chưa có downstream movement → tồn kho hoàn nguyên đúng (qua method `RevertReceiveAsync`)
- [ ] Xóa GoodsReceipt đã có downstream movement → throw `GoodsReceiptHasStockMovementsException`

---

## Returns Module — Phase B (sau khi Phase A xong)

**Cấp độ:** Khó

> **Phụ thuộc**: Phase A (Stock Invariant Hardening) phải DONE 100%.
> Chi tiết thiết kế tại [IMPROVEMENT_PLAN.md](IMPROVEMENT_PLAN.md) mục 6.

### [PRIORITY: HIGH] B1 — Domain.Shared

- [ ] Enum `CustomerReturnStatus` (Draft=0, Inspecting=1, Confirmed=2, Cancelled=3) tại `Domain.Shared/Enums/Returns/`
- [ ] Enum `VendorReturnStatus` (Draft=0, Inspecting=1, Confirmed=2, Cancelled=3)
- [ ] Bổ sung `StockReferenceType.CustomerReturn = 7`, `VendorReturn = 8` (optional, để truy vết)
- [ ] Events: `CustomerReturnConfirmed(Id, OrderId, CustomerId, WarehouseId)`, `CustomerReturnCancelled(Id)` tại `Domain.Shared/Events/Returns/CustomerReturnEvents.cs`
- [ ] Events: `VendorReturnConfirmed(Id, PurchaseOrderId?, GoodsReceiptId?, VendorId, WarehouseId)`, `VendorReturnCancelled(Id)` tại `Domain.Shared/Events/Returns/VendorReturnEvents.cs`
- [ ] Exceptions: `CustomerReturnNotFoundException`, `VendorReturnNotFoundException`, `ExceedsDeliveredQuantityException`, `ExceedsReceivedQuantityException`, `ReturnCannotChangeStatusException`, `ReturnDataIsInvalidException`

### [PRIORITY: HIGH] B2 — Domain Layer (Entities + Debt extensions)

- [ ] Entity `CustomerReturn` tại `Domain/Entities/Returns/CustomerReturn.cs` với fields: Code, OrderId, CustomerId, WarehouseId, ReturnDate, Note, Status, ConfirmedOnUtc?, GeneratedGoodsReceiptId?, CreatedByUserId?, CreatedOnUtc, UpdatedOnUtc?
- [ ] Entity `CustomerReturnItem` (CustomerReturnId, ProductId, DeliveryNoteItemId?, RequestedQuantity, AcceptedQuantity, UnitPrice)
- [ ] Entity `VendorReturn` (Code, PurchaseOrderId?, GoodsReceiptId?, VendorId, WarehouseId, ReturnDate, Note, Status, ConfirmedOnUtc?, GeneratedDeliveryNoteId?, CreatedByUserId?, …)
- [ ] Entity `VendorReturnItem` (VendorReturnId, ProductId, GoodsReceiptItemId?, RequestedQuantity, AcceptedQuantity, UnitCost)
- [ ] Mark methods cho `CustomerReturn`: `MarkCreated`, `Confirm`, `Cancel` — raise event tương ứng
- [ ] Mark methods cho `VendorReturn`: `MarkCreated`, `Confirm`, `Cancel`
- [ ] Extensions `ToDto` cho 4 entity tại `Domain/Extensions/Returns/`
- [ ] Mở rộng `CustomerDebt`: thêm method `ApplyReturn(decimal amount, Guid returnId)` — giảm `RemainingAmount` (cho phép xuống âm), track `appliedReturnIds` để idempotent
- [ ] Mở rộng `VendorDebt`: thêm `ApplyReturn(amount, returnId)` tương tự
- [ ] Sửa `DeliveryNote.OrderId` thành `Guid?` (nullable) để chứa được phiếu xuất loại `ToVendorReturn` không có Order
- [ ] **Migration thủ công**: `Add-Migration MakeOrderIdNullableOnDeliveryNote` + `Add-Migration AddReturnsModule`

### [PRIORITY: HIGH] B3 — Domain.Services (Managers)

- [ ] DTOs cho Return Managers (kèm `Verify()`): `CreateCustomerReturnDto`, `UpdateCustomerReturnDto`, `CustomerReturnDto`, `CustomerReturnItemDto`; tương ứng cho VendorReturn
- [ ] `ICustomerReturnManager` + `CustomerReturnManager`:
  - `CreateAsync(dto)` — tạo phiếu Draft, validate Order tồn tại + items thuộc Order
  - `UpdateAsync(dto)` — chỉ khi Draft
  - `MoveToInspectingAsync(id)` — Draft → Inspecting
  - `ConfirmAsync(id)` — Inspecting → Confirmed; validate `Σ AcceptedQty ≤ Delivered cho (OrderId, ProductId)` — throw `ExceedsDeliveredQuantityException` nếu vượt; raise `CustomerReturnConfirmed`
  - `CancelAsync(id)` — chỉ khi Draft hoặc Inspecting
- [ ] `IVendorReturnManager` + `VendorReturnManager` (đối xứng, validate theo PurchaseOrder hoặc GoodsReceipt độc lập)
- [ ] Method internal mới `GoodsReceiptManager.CreateFromCustomerReturnAsync(dto)` — chỉ cho `CustomerReturnConfirmedEventHandler` gọi; tạo `GoodsReceipt(SourceType=FromCustomerReturn)` items map từ Return; UnitCost = `AverageCost` hiện tại của (ProductId, WarehouseId) để báo cáo nhất quán
- [ ] Method internal mới `DeliveryNoteManager.CreateAsDeliveredAsync(dto)` — chỉ cho `VendorReturnConfirmedEventHandler` gọi; tạo `DeliveryNote(SourceType=ToVendorReturn, OrderId=null)` ở status `Delivered` ngay (skip Reserve/Confirm/Delivering); trừ tồn thực sự

### [PRIORITY: HIGH] B4 — Application

- [ ] AppDtos cho Return (kèm `Validate()` return `(bool, string?)`): `CreateCustomerReturnAppDto`, `CustomerReturnAppDto`, `CustomerReturnListAppDto`; tương tự cho VendorReturn
- [ ] `ICustomerReturnAppService` + `CustomerReturnAppService` — nhận AppDto, gọi Manager qua DomainDto
- [ ] `IVendorReturnAppService` + `VendorReturnAppService`
- [ ] Event handler `CustomerReturnConfirmedEventHandler`: gọi `GoodsReceiptManager.CreateFromCustomerReturnAsync` + duyệt `CustomerDebt` của Order theo FIFO `CreatedOnUtc` để gọi `ApplyReturn`; set `CustomerReturn.GeneratedGoodsReceiptId`
- [ ] Event handler `VendorReturnConfirmedEventHandler`: gọi `DeliveryNoteManager.CreateAsDeliveredAsync` + giảm `VendorDebt` (FIFO theo PurchaseOrderId hoặc GoodsReceiptId); set `VendorReturn.GeneratedDeliveryNoteId`
- [ ] **Sửa** `GoodsReceiptCreatedHandler.TryCreateVendorDebtAsync`: thêm guard `if (goodsReceipt.SourceType == GoodsReceiptSourceType.FromCustomerReturn) return;`
- [ ] **Sửa** `DeliveryNoteDeliveredEventHandler`: thêm guard `if (deliveryNote.SourceType == DeliveryNoteSourceType.ToVendorReturn) return;`

### [PRIORITY: HIGH] B5 — Infrastructure (Data.SqlServer)

- [ ] `CustomerReturnConfiguration` + `CustomerReturnItemConfiguration` (mapping FK, indexes)
- [ ] `VendorReturnConfiguration` + `VendorReturnItemConfiguration`
- [ ] Index hỗ trợ truy vấn: `(OrderId, Status)` cho CustomerReturn; `(PurchaseOrderId, Status)` + `(GoodsReceiptId, Status)` cho VendorReturn
- [ ] Đăng ký Repository + EntityDataReader cho 4 entity mới trong DI

### [PRIORITY: HIGH] B6 — Presentation (Web)

- [ ] Models (Web.Contracts/Models/Returns): `CreateCustomerReturnModel`, `UpdateCustomerReturnModel`, `CustomerReturnDetailModel`, `CustomerReturnListModel`; tương tự VendorReturn
- [ ] FluentValidation Validators cho mỗi Model
- [ ] Commands/Queries (Web.Contracts/Commands+Queries) cho cả 2 module: `CreateCustomerReturnCommand`, `MoveToInspectingCommand`, `ConfirmCustomerReturnCommand`, `CancelCustomerReturnCommand`, `GetCustomerReturnByIdQuery`, `GetCustomerReturnsQuery`; tương tự VendorReturn
- [ ] Command/Query Handlers tương ứng (Web.Framework) — đều dùng IMediator + AppService
- [ ] `ICustomerReturnModelFactory` + `CustomerReturnModelFactory` (Web)
- [ ] `IVendorReturnModelFactory` + `VendorReturnModelFactory`
- [ ] `CustomerReturnController` (Index, Detail, Create [GET/POST], Update [GET/POST], Inspect, Confirm, Cancel) — dùng IMediator + ModelFactory, không inject AppService trực tiếp
- [ ] `VendorReturnController` (đối xứng)
- [ ] Views: `Index.cshtml`, `Create.cshtml`, `Detail.cshtml`, `Inspect.cshtml` cho cả 2 controller
- [ ] Thêm menu/sidebar link tới 2 module mới
- [ ] Code prefix: `TKH-yyyyMMdd-NNN` (CustomerReturn), `TNCC-yyyyMMdd-NNN` (VendorReturn) — đảm bảo qua `ICodeExistCheckingService`

### [PRIORITY: HIGH] B7 — Smoke test Phase B

- [ ] Tạo Order + DeliveryNote → Delivered. Tạo `CustomerReturn` từ Order → Inspecting → Confirmed → kiểm tra: `GoodsReceipt` mới sinh ra với `SourceType=FromCustomerReturn`, tồn kho cộng đúng, **KHÔNG có `VendorDebt` mới**, `CustomerDebt` của Order giảm đúng (có thể xuống âm khi return > debt còn)
- [ ] Tạo 2 `CustomerReturn` cùng 1 Order (mỗi phiếu trả 1 phần) → tổng `AcceptedQty` đúng, không vượt `Delivered`
- [ ] Tạo `CustomerReturn` với `AcceptedQty > Delivered` → throw `ExceedsDeliveredQuantityException`
- [ ] Cancel phiếu ở Draft → OK; cancel phiếu Confirmed → throw
- [ ] Tạo PO → Receive → tạo `VendorReturn` → Inspecting → Confirmed → kiểm tra: `DeliveryNote` mới sinh ra với `SourceType=ToVendorReturn, OrderId=null, Status=Delivered`, tồn kho trừ đúng, **KHÔNG có `CustomerDebt` mới**, `VendorDebt` giảm đúng
- [ ] Tạo `VendorReturn` với `AcceptedQty > Received` → throw `ExceedsReceivedQuantityException`
- [ ] Tạo `VendorReturn` từ GoodsReceipt độc lập (không có PO) → flow hoạt động đúng theo `GoodsReceiptId` fallback

---

## Phase C — Follow-ups (sau Phase B)

**Cấp độ:** Trung bình → Khó (tùy hạng mục)

> Các todo độc lập với Returns, làm sau khi Phase B đã ổn định.

- [ ] **C1** — Phiếu chi/hoàn tiền khi `CustomerDebt.RemainingAmount < 0`: thiết kế entity `CustomerRefund` (hoặc mở rộng `Expense`), flow hoàn tiền mặt cho khách
- [ ] **C2** — Sửa `FinancialReportAppService.GetProfitLossSummaryAsync`: đổi nguồn tính doanh thu sang `DeliveryNote.DeliveredOnUtc` + filter `SourceType=ToCustomer`; trừ doanh thu các CustomerReturn Confirmed; trừ COGS bù theo
- [ ] **C3** — Snapshot `CostAtDispatch` trên `DeliveryNoteItem`: ghi giá vốn (AverageCost) tại thời điểm `MarkDelivered` để báo cáo lãi/lỗ chính xác
- [ ] **C4** — Thiết kế kiểm kê/điều chỉnh tồn: chọn 1 trong 2 hướng:
  - Entity riêng `StockAdjustmentNote` (Draft → Approved → trigger cộng/trừ tồn)
  - `SourceType=Adjustment` trên 2 phiếu cũ — chia FromAdjustment/ToAdjustment
- [ ] **C5** — Khôi phục lối tạo Product có sẵn stock: auto-sinh phiếu Adjustment khi user nhập initial stock (sau khi C4 xong)
- [ ] **C6** — Thống nhất pattern side-effect: `DeliveryNoteManager.MarkDeliveredAsync` chuyển từ inline `DispatchStockAsync` sang event handler (như `GoodsReceiptCreatedHandler`)
- [ ] **C7** — Remove `IInventoryStockManager.AdjustStockAsync` (đã `[Obsolete]` từ A6) sau khi C4 thay thế bằng `StockAdjustmentNote`

---

## Đã hoàn thành

| Hạng mục | Phase | Session |
|----------|-------|---------|
| Audit hệ thống + viết IMPROVEMENT_PLAN.md | — | 2026-05-06 |
| A1 — Thêm SourceType cho GoodsReceipt + DeliveryNote (code, mapping; migration chờ Tuấn) | A | 2026-05-07 |

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

*(Tích lũy qua các session 2026-04-28 → 2026-05-07)*

**1. Build verify** toàn bộ solution — `dotnet build NamEcommerce.sln`. Các thay đổi cần check compile sạch:

- Session 2026-04-28: DI cho `IEntityDataReader<StockMovementLog>` + `IEntityDataReader<GoodsReceipt>` + `IVendorDebtManager`; `VendorDebtManagerTests.cs` (helper 7→8 params); Notification + Vendor + Handler module.
- Session 2026-04-30: `PurchaseOrderManager` (12→11 deps, bỏ `IEventPublisher`); `PurchaseOrderManagerTests.cs` (22 constructor calls); `PurchaseOrderItemReceivedEventHandler` (mới); `PurchaseOrderUpdatedEventHandler.cs` (đang là stub — Tuấn xoá file thủ công).
- Session 2026-05-06 (Phase 4 Outbox): `OutboxMessage` entity (Domain), `OutboxMessageMapping` (Data.SqlServer), `IOutbox` + `OutboxAccessor`, `IIntegrationEvent` (kế thừa MediatR `INotification`), `DeliveryNoteConfirmedIntegrationEvent` + handler, `OutboxProcessor` background service. Csproj `Data.SqlServer` thêm 3 PackageReference: `Microsoft.Extensions.Hosting.Abstractions` 10.0.0, `Microsoft.Extensions.DependencyInjection.Abstractions` 10.0.0, `Microsoft.Extensions.Logging.Abstractions` 10.0.0. `Program.cs` đăng ký `IOutbox`, `OutboxProcessorOptions`, `AddHostedService<OutboxProcessor>`.
- Session 2026-05-07 (Phase A1 SourceType): `GoodsReceipt` + `DeliveryNote` entities có thêm `SourceType` (`internal set`) với default tương ứng. `GoodsReceiptMapping.cs` + `DeliveryNoteMap.cs` (Data.SqlServer) cấu hình cột `SourceType` với `IsRequired().HasDefaultValue(...).HasConversion<int>()`. Imports: `using NamEcommerce.Domain.Shared.Enums.GoodsReceipts;` đã thêm vào GoodsReceipt entity.
- Session 2026-05-07 (Phase A2 GoodsReceipt auto-create): `PurchaseOrderManager` (11→8 deps, bỏ `IInventoryStockManager` + `IEntityDataReader<InventoryStock>` + `IRepository<Product>` + `IRepository<ProductPriceHistory>`, thêm `IGoodsReceiptManager`); `GoodsReceiptManager` thêm method `CreateFromPurchaseOrderReceivingAsync`; `IGoodsReceiptManager` + `GoodsReceiptDtos.cs` thêm `CreateGoodsReceiptFromPurchaseOrderDto`; `PurchaseOrderManagerTests.cs` (constructor 11→8 params, usings cập nhật, 3 test cũ xoá, 2 test mới thêm).
- Session 2026-05-07 (Phase A3–A6 Stock Invariant): A3: `ProductAppService` bỏ `IInventoryStockManager`/`ICurrentUserAccessor`/`IEntityDataReader<Warehouse>` + toàn bộ initial stock logic; `CreateProductAppDto` bỏ UnitPrice/CostPrice/ProductStocks; `CreateProductCommand` bỏ stock fields; `CreateProductModel` bỏ HasExistingStockQuantity/ProductInventory; `CreateProductValidator` bỏ ChildRules; `Create.cshtml` thay inventory tab = info note; `CreateProductHandler` bỏ stock mapping. A4: xóa 5 actions+views InventoryController; xóa 5 handlers; xóa 5 commands; xóa StockOperationModels.cs + AdjustStockModel.cs + 5 result models từ InventoryModels.cs; bỏ AdjustStock link trong StockList.cshtml. A5: `IInventoryAppService` + `InventoryAppService` bỏ 5 methods + `IInventoryValidator`; xóa 5 DTOs. A6: `StockMovementType.Revert` enum mới; `IInventoryStockManager.RevertReceiveAsync` interface + implementation; `GoodsReceiptDeletedEventHandler` dùng `RevertReceiveAsync`.
- Session 2026-05-07 (Phase B1–B3 Returns Domain + B4 Application một phần): B1: enums `CustomerReturnStatus`/`VendorReturnStatus`; events `CustomerReturnConfirmed/Cancelled` + `VendorReturnConfirmed/Cancelled`; 6 exception classes. B2: 4 entities (`CustomerReturn`, `CustomerReturnItem`, `VendorReturn`, `VendorReturnItem`) + `ApplyReturn` trên `CustomerDebt`/`VendorDebt`; thêm constructor `DeliveryNote(code, warehouseId, note, createdByUserId)` cho VendorReturn (OrderId=Guid.Empty sentinel) + `AddItemFromVendorReturn` + `MarkAsDeliveredFromVendorReturn`. B3: `CustomerReturnManager` + `VendorReturnManager`; `IInventoryStockManager.GetAverageCostAsync` mới; `GoodsReceiptManager.CreateFromCustomerReturnAsync`; `DeliveryNoteManager.CreateAsDeliveredAsync`; DTOs domain `CreateGoodsReceiptFromCustomerReturnDto` + `CreateDeliveryNoteFromVendorReturnDto`. B4 (một phần): `CustomerReturnAppDtos.cs` + `VendorReturnAppDtos.cs`; `ICustomerReturnAppService`; Extension `CustomerReturnAppExtensions` + `VendorReturnAppExtensions`; guard `FromCustomerReturn` trong `GoodsReceiptCreatedHandler`; guard `ToVendorReturn` trong `DeliveryNoteDeliveredEventHandler`.

**2. Migrations cần chạy thủ công:**
- `Add-Migration AddAverageCostToInventoryStock` (project Data.SqlServer)
- `Add-Migration AddVendorToGoodsReceiptAndDebt`
- `Add-Migration AddOutboxMessages` (Phase 4 — bảng `tbl.OutboxMessage` với index `IX_OutboxMessage_Pending`)
- `Add-Migration AddSourceTypeToGoodsReceiptAndDeliveryNote` (Phase A1 — cột `SourceType` cho 2 bảng, default = 0)
- Không cần migration cho `StockMovementType.Revert` — enum C# không ánh xạ schema DB (cột int)
- `Add-Migration AddReturnsModule` (Phase B5 — 4 bảng: `tbl.CustomerReturn`, `tbl.CustomerReturnItem`, `tbl.VendorReturn`, `tbl.VendorReturnItem`)
- `Add-Migration AddCostAtDispatchToDeliveryNoteItem` (C3 — cột `CostAtDispatch decimal(18,4) NULL` trên `tbl.DeliveryNoteItem`)
- `Add-Migration AddCustomerRefund` (C1 — bảng `tbl.CustomerRefund`, index `(CustomerId, Status)` + `CustomerReturnId`)
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

### ✅ A3 — Sửa `ProductAppService.CreateProductAsync` bỏ initial stock *(Done 2026-05-07)*

- [x] Loại bỏ phần gọi `_inventoryStockManager.AdjustStockAsync` trong flow tạo Product
- [x] Cập nhật DTO `CreateProductAppDto` / Form Model — bỏ UnitPrice, CostPrice, ProductStocks
- [x] Cập nhật View tạo Product — thêm note "Để có tồn kho ban đầu, tạo phiếu nhập (GoodsReceipt) sau khi tạo sản phẩm"
- [x] Cập nhật Validator — bỏ ProductInventory ChildRules block
- [x] Cập nhật Handler — bỏ mapping UnitPrice, CostPrice, ProductStocks

### ✅ A4 — Bỏ 4 actions InventoryController + Commands/Handlers/Views *(Done 2026-05-07)*

- [x] Bỏ `InventoryController.ReceiveStock`, `DispatchStock`, `ReserveStock`, `ReleaseReservedStock`, `AdjustStock` (GET + POST) + xóa 5 views tương ứng
- [x] Xóa `ReceiveStockHandler`, `DispatchStockHandler`, `ReserveStockHandler`, `ReleaseReservedStockHandler`, `AdjustStockHandler`
- [x] Xóa `ReceiveStockCommand`, `DispatchStockCommand`, `ReserveStockCommand`, `ReleaseReservedStockCommand`, `AdjustStockCommand` (InventoryCommands.cs)
- [x] Xóa `StockOperationModels.cs`; remove 5 result models từ `InventoryModels.cs`; xóa `AdjustStockModel.cs`
- [x] Bỏ link AdjustStock trong `StockList.cshtml`

### ✅ A5 — Bỏ 5 method public stock thao tác khỏi `IInventoryAppService` *(Done 2026-05-07)*

- [x] Remove khỏi `IInventoryAppService`: `ReceiveStockAsync`, `DispatchStockAsync`, `ReserveStockAsync`, `ReleaseReservedStockAsync`, `AdjustStockAsync`
- [x] Xóa implementation tương ứng trong `InventoryAppService`; bỏ `IInventoryValidator` khỏi constructor
- [x] Xóa 5 DTOs (`ReceiveStockAppDto`, `DispatchStockAppDto`, `ReserveStockAppDto`, `AdjustStockAppDto`, `ReleaseStockAppDto`) khỏi `InventoryAppDtos.cs`

### ✅ A6 — Đổi naming `AdjustStockAsync` cho rollback use-case *(Done 2026-05-07)*

- [x] Thêm method semantic mới `RevertReceiveAsync(productId, warehouseId, quantity, goodsReceiptId, modifiedByUserId)` vào `IInventoryStockManager`
- [x] Implementation: trừ đúng qty đã nhập, ghi `StockMovementLog` với `MovementType=Revert, ReferenceType=GoodsReceipt`
- [x] Thêm `StockMovementType.Revert` vào enum `StockMovementLog.cs`
- [x] Cập nhật `GoodsReceiptDeletedEventHandler` dùng `RevertReceiveAsync` thay cho `AdjustStockAsync`

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

### ✅ B1 — Domain.Shared *(Done 2026-05-07)*

- [x] Enum `CustomerReturnStatus` (Draft=0, Inspecting=1, Confirmed=2, Cancelled=3) tại `Domain.Shared/Enums/Returns/`
- [x] Enum `VendorReturnStatus` (Draft=0, Inspecting=1, Confirmed=2, Cancelled=3)
- [x] Events: `CustomerReturnConfirmed(Id, OrderId, CustomerId, WarehouseId)`, `CustomerReturnCancelled(Id)` tại `Domain.Shared/Events/Returns/CustomerReturnEvents.cs`
- [x] Events: `VendorReturnConfirmed(Id, PurchaseOrderId?, GoodsReceiptId?, VendorId, WarehouseId)`, `VendorReturnCancelled(Id)` tại `Domain.Shared/Events/Returns/VendorReturnEvents.cs`
- [x] Exceptions: `CustomerReturnNotFoundException`, `VendorReturnNotFoundException`, `ExceedsDeliveredQuantityException`, `ExceedsReceivedQuantityException`, `ReturnCannotChangeStatusException`, `ReturnDataIsInvalidException`

### ✅ B2 — Domain Layer (Entities + Debt extensions) *(Done 2026-05-07)*

- [x] Entity `CustomerReturn` + `CustomerReturnItem` tại `Domain/Entities/Returns/`
- [x] Entity `VendorReturn` + `VendorReturnItem` tại `Domain/Entities/Returns/`
- [x] Mark methods cho `CustomerReturn`: `MoveToInspecting`, `Confirm` (raise `CustomerReturnConfirmed`), `Cancel` (raise `CustomerReturnCancelled`), `MarkCreated`
- [x] Mark methods cho `VendorReturn`: tương tự
- [x] DTOs domain tại `Domain.Shared/Dtos/Returns/`: `CustomerReturnDto`, `CustomerReturnItemDto`, `CreateCustomerReturnDto`, `UpdateCustomerReturnDto`; tương ứng cho VendorReturn
- [x] `ApplyReturn(decimal amount, Guid returnId)` trên `CustomerDebt` — giảm `RemainingAmount` (cho phép xuống âm)
- [x] `ApplyReturn(decimal amount, Guid returnId)` trên `VendorDebt` — tương tự
- [x] Constructor thứ 2 `DeliveryNote(code, warehouseId, note, createdByUserId)` — sentinel `OrderId=Guid.Empty, CustomerId=Guid.Empty` cho VendorReturn flow; `AddItemFromVendorReturn`; `MarkAsDeliveredFromVendorReturn`
- [x] `ICustomerReturnManager` + `IVendorReturnManager` interface tại `Domain.Shared/Services/Returns/`

### ✅ B3 — Domain.Services (Managers) *(Done 2026-05-07)*

- [x] `CustomerReturnManager` — `CreateAsync`, `UpdateAsync`, `MoveToInspectingAsync`, `ConfirmAsync`, `CancelAsync`, `GetByIdAsync`, `GetListAsync`, `GetTotalConfirmedReturnQuantityAsync`
- [x] `VendorReturnManager` — đối xứng; validate `AcceptedQty ≤ (received − previouslyReturned)` theo GoodsReceipt hoặc PurchaseOrder; throw `ExceedsReceivedQuantityException`
- [x] `IInventoryStockManager.GetAverageCostAsync(productId, warehouseId)` — interface + implementation mới
- [x] `GoodsReceiptManager.CreateFromCustomerReturnAsync(dto)` — tạo `GoodsReceipt(SourceType=FromCustomerReturn)`, UnitCost = `AverageCost` hiện tại
- [x] `DeliveryNoteManager.CreateAsDeliveredAsync(dto)` — tạo `DeliveryNote(SourceType=ToVendorReturn)` status `Delivered` ngay, dispatch stock inline
- [x] DTOs mới: `CreateGoodsReceiptFromCustomerReturnDto/Item`, `CreateDeliveryNoteFromVendorReturnDto/Item` tại `Domain.Shared/Dtos/`
- [x] `IGoodsReceiptManager.CreateFromCustomerReturnAsync` + `IDeliveryNoteManager.CreateAsDeliveredAsync` — interface mới

### ✅ B4 — Application *(Done 2026-05-07)*

- [x] `Application.Contracts/Dtos/Returns/CustomerReturnAppDtos.cs` + `VendorReturnAppDtos.cs` — App DTOs + `Validate()`
- [x] `Application.Contracts/Returns/ICustomerReturnAppService.cs` + `IVendorReturnAppService.cs`
- [x] `Application.Services/Extensions/CustomerReturnExtensions.cs` + `VendorReturnAppExtensions.cs` — `ToAppDto()`
- [x] `Application.Services/Returns/CustomerReturnAppService.cs` + `VendorReturnAppService.cs`
- [x] `Application.Services/Events/Returns/CustomerReturnConfirmedEventHandler.cs` — tạo GoodsReceipt + FinalizeConfirmAsync (giảm CustomerDebt FIFO)
- [x] `Application.Services/Events/Returns/VendorReturnConfirmedEventHandler.cs` — tạo DeliveryNote + FinalizeConfirmAsync (giảm VendorDebt FIFO)
- [x] `ICustomerReturnManager.FinalizeConfirmAsync` + `IVendorReturnManager.FinalizeConfirmAsync` — interface + implementation (idempotency + FIFO ApplyReturn)
- [x] `GoodsReceiptCreatedHandler` — guard `SourceType == FromCustomerReturn` → skip `TryCreateVendorDebtAsync`
- [x] `DeliveryNoteDeliveredEventHandler` — guard `SourceType == ToVendorReturn` → return early

### ✅ B5 — Infrastructure (Data.SqlServer) *(Done 2026-05-07)*

- [x] `CustomerReturnMapping` + `CustomerReturnItemMapping` (table, FK, decimals, indexes)
- [x] `VendorReturnMapping` + `VendorReturnItemMapping`
- [x] Index: `(OrderId, Status)` + `(CustomerId, Status)` cho CustomerReturn; `(PurchaseOrderId, Status)` + `(GoodsReceiptId, Status)` + `(VendorId, Status)` cho VendorReturn
- [x] DI: `ICustomerReturnManager/AppService` + `IVendorReturnManager/AppService` đăng ký trong `Program.cs`

### ✅ B6 — Presentation (Web) *(Done 2026-05-07)*

- [x] `Web.Contracts/Commands/Returns/`: `CreateCustomerReturnCommand`, `UpdateCustomerReturnCommand`, `ChangeCustomerReturnStatusCommand` (3 commands); VendorReturn tương tự
- [x] `Web.Contracts/Queries/Returns/`: `GetCustomerReturnQuery`, `GetCustomerReturnListQuery`; VendorReturn tương tự
- [x] `Web.Contracts/Models/Returns/`: `CustomerReturnModel`, `CustomerReturnListModel`, `CreateCustomerReturnResultModel`, `UpdateCustomerReturnResultModel`; VendorReturn tương tự
- [x] Command Handlers (Web.Framework): `CreateCustomerReturnHandler`, `UpdateCustomerReturnHandler`, `ChangeCustomerReturnStatusHandlers`; VendorReturn tương tự
- [x] Query Handlers (Web.Framework): `GetCustomerReturnHandler`, `GetCustomerReturnListHandler`; VendorReturn tương tự
- [x] View Models (Web/Models/Returns): `CreateCustomerReturnModel`, `CustomerReturnDetailsModel`, `CustomerReturnListSearchModel`; VendorReturn tương tự
- [x] `ICustomerReturnModelFactory` + `CustomerReturnModelFactory`; VendorReturn tương tự
- [x] `CustomerReturnController` + `VendorReturnController` (Index, List, Create GET/POST, Details, Update, MoveToInspecting, Confirm, Cancel)
- [x] DI: cả 2 ModelFactory đăng ký trong `Program.cs`
- [x] Views: `List.cshtml`, `Create.cshtml`, `Details.cshtml` cho cả 2 controller
- [x] Thêm menu/sidebar link "Trả hàng" (CustomerReturn + VendorReturn) vào sidebar

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

- [x] **C1** — Phiếu chi/hoàn tiền khi `CustomerDebt.RemainingAmount < 0`: thiết kế entity `CustomerRefund` (hoặc mở rộng `Expense`), flow hoàn tiền mặt cho khách
- [x] **C2** — Sửa `FinancialReportAppService.GetProfitLossSummaryAsync`: đổi nguồn tính doanh thu sang `DeliveryNote.DeliveredOnUtc` + filter `SourceType=ToCustomer`; trừ doanh thu các CustomerReturn Confirmed; COGS từ `CostAtDispatch`
- [x] **C3** — Snapshot `CostAtDispatch` trên `DeliveryNoteItem`: ghi giá vốn (AverageCost) tại thời điểm `MarkDelivered` để báo cáo lãi/lỗ chính xác
- [ ] **C4** — Thiết kế kiểm kê/điều chỉnh tồn: chọn 1 trong 2 hướng:
  - Entity riêng `StockAdjustmentNote` (Draft → Approved → trigger cộng/trừ tồn)
  - `SourceType=Adjustment` trên 2 phiếu cũ — chia FromAdjustment/ToAdjustment
- [ ] **C5** — Khôi phục lối tạo Product có sẵn stock: auto-sinh phiếu Adjustment khi user nhập initial stock (sau khi C4 xong)
- [x] **C6** — Thống nhất pattern side-effect: `DeliveryNoteManager.MarkDeliveredAsync` chuyển từ inline `DispatchStockAsync` sang event handler (`DeliveryNoteDeliveredStockHandler`)
- [ ] **C7** — Remove `IInventoryStockManager.AdjustStockAsync` (đã `[Obsolete]` từ A6) sau khi C4 thay thế bằng `StockAdjustmentNote`

---

## Phase D — Returns UX & Price Model

**Cấp độ:** Trung bình

> **Mục tiêu**: Làm cho form tạo phiếu Khách trả hàng / Trả hàng NCC có thể sử dụng được trong thực tế.
> Thay thế nhập thủ công ID bằng typeahead + AJAX. Bổ sung trường giá trả về và chi phí phát sinh để phục vụ báo cáo tài chính.

### Quyết định thiết kế đã chốt

- `CustomerReturn`: đổi `OrderId` → `DeliveryNoteId? (nullable)` — tùy chọn, null = tạo tự do
- `CustomerReturnItem`: thêm `OriginalUnitPrice decimal?` (giá bán gốc, tham chiếu) + `ReturnUnitPrice decimal` (giá trả về thực tế)
- `VendorReturnItem`: thêm `OriginalUnitCost decimal?` + `ReturnUnitCost decimal`
- `AdditionalCost decimal` trên header cả 2 phiếu — chi phí phát sinh (xe, bồi thường hư hỏng); tự động ghi vào `Expense` khi Confirm
- Hoàn nợ / giảm nợ net: `Σ(AcceptedQty × ReturnUnitPrice) - AdditionalCost`
- **CustomerReturn**: nhập kho (GoodsReceipt) theo `ReturnUnitPrice` — hàng trả về đã giảm giá trị
- **VendorReturn**: xuất kho (DeliveryNote) theo `AverageCost` — chuẩn kế toán

---

### D1 — Domain.Shared: Cập nhật DTOs

- [ ] `CustomerReturnDtos.cs`:
  - `CreateCustomerReturnDto`: đổi `OrderId?` → `DeliveryNoteId?`; thêm `AdditionalCost decimal`
  - `CreateCustomerReturnItemDto`: thêm `OriginalUnitPrice decimal?`, `ReturnUnitPrice decimal`
  - `CustomerReturnDto` + `CustomerReturnItemDto`: thêm các trường tương ứng
- [ ] `VendorReturnDtos.cs`:
  - `CreateVendorReturnDto`: thêm `AdditionalCost decimal`
  - `CreateVendorReturnItemDto`: thêm `OriginalUnitCost decimal?`, `ReturnUnitCost decimal`
  - `VendorReturnDto` + `VendorReturnItemDto`: thêm các trường tương ứng

---

### D2 — Domain Layer: Cập nhật Entities

- [ ] `CustomerReturn`: đổi `OrderId Guid?` → `DeliveryNoteId Guid?`; thêm `AdditionalCost decimal`
- [ ] `CustomerReturnItem`: thêm `OriginalUnitPrice decimal?`, `ReturnUnitPrice decimal`
- [ ] `VendorReturn`: thêm `AdditionalCost decimal`
- [ ] `VendorReturnItem`: thêm `OriginalUnitCost decimal?`, `ReturnUnitCost decimal`
- [ ] Cập nhật `internal` constructor + `ToDto()` extension khớp với trường mới

---

### D3 — Domain.Services: Cập nhật Managers

- [ ] `CustomerReturnManager`:
  - `CreateAsync` / `UpdateAsync`: map `DeliveryNoteId`, `AdditionalCost`, `OriginalUnitPrice`, `ReturnUnitPrice`
  - `FinalizeConfirmAsync`: tính `totalAmount = Σ(AcceptedQty × ReturnUnitPrice) - AdditionalCost` (floor 0); dùng `totalAmount` trong FIFO ApplyReturn
  - Nếu `AdditionalCost > 0` → tạo `Expense` (ghi nhận chi phí phát sinh từ hoàn hàng)
- [ ] `VendorReturnManager`: tương tự với `ReturnUnitCost` + `AdditionalCost`
- [ ] `GoodsReceiptManager.CreateFromCustomerReturnAsync`: dùng `item.ReturnUnitPrice` làm `UnitCost` (thay vì `AverageCost`)
- [ ] `DeliveryNoteManager.CreateAsDeliveredAsync` (VendorReturn path): giữ nguyên — xuất theo `AverageCost`

---

### D4 — Application Layer: Cập nhật AppDtos + AppServices

- [ ] `CustomerReturnAppDtos.cs`: thêm price fields vào `CustomerReturnAppDto`, `CustomerReturnItemAppDto`, `CreateCustomerReturnAppDto`, `CreateCustomerReturnItemAppDto`
- [ ] `VendorReturnAppDtos.cs`: tương tự
- [ ] `CustomerReturnAppService` + `VendorReturnAppService`: cập nhật mapping
- [ ] Thêm 4 method vào `ICustomerReturnAppService` + implementation (phục vụ AJAX load):
  - `GetDeliveryNotesByCustomerAsync(customerId)` → danh sách phiếu xuất đã giao của khách
  - `GetDeliveryNoteItemsForReturnAsync(deliveryNoteId)` → items kèm `deliveredQty`, `alreadyReturnedQty`, `unitPrice`
- [ ] Thêm 2 method vào `IVendorReturnAppService`:
  - `GetGoodsReceiptsByVendorAsync(vendorId)` → danh sách phiếu nhập của NCC
  - `GetGoodsReceiptItemsForReturnAsync(goodsReceiptId)` → items kèm `receivedQty`, `alreadyReturnedQty`, `unitCost`

---

### D5 — Infrastructure: EF Mapping + Migration

- [ ] `CustomerReturnMapping`: đổi `OrderId` → `DeliveryNoteId`; thêm `AdditionalCost decimal(18,4) default 0`
- [ ] `CustomerReturnItemMapping`: thêm `OriginalUnitPrice decimal(18,4) nullable`, `ReturnUnitPrice decimal(18,4) not null default 0`
- [ ] `VendorReturnMapping`: thêm `AdditionalCost decimal(18,4) default 0`
- [ ] `VendorReturnItemMapping`: thêm `OriginalUnitCost decimal(18,4) nullable`, `ReturnUnitCost decimal(18,4) not null default 0`
- [ ] **Migration** (Tuấn tự chạy): `Add-Migration UpdateReturnsAddPriceAndDeliveryNoteRef`

---

### D6 — Web.Contracts: Commands / Queries / Models

- [ ] Cập nhật `CreateCustomerReturnCommand`: thêm `DeliveryNoteId?`, `AdditionalCost`, `OriginalUnitPrice?`, `ReturnUnitPrice` trên item
- [ ] Cập nhật `CreateVendorReturnCommand`: thêm `AdditionalCost`, `OriginalUnitCost?`, `ReturnUnitCost`
- [ ] Thêm 4 Queries mới tại `Web.Contracts/Queries/Models/Returns/`:
  - `GetDeliveryNotesByCustomerQuery { CustomerId }` → `IRequest<List<DeliveryNotePickerModel>>`
  - `GetDeliveryNoteItemsForReturnQuery { DeliveryNoteId }` → `IRequest<List<ReturnableItemModel>>`
  - `GetGoodsReceiptsByVendorQuery { VendorId }` → `IRequest<List<GoodsReceiptPickerModel>>`
  - `GetGoodsReceiptItemsForReturnQuery { GoodsReceiptId }` → `IRequest<List<ReturnableItemModel>>`
- [ ] Thêm Result Models: `DeliveryNotePickerModel { Id, Code, DeliveredOnUtc }`, `GoodsReceiptPickerModel { Id, Code, CreatedOnUtc }`, `ReturnableItemModel { ProductId, ProductName, Unit, OriginalQty, AlreadyReturnedQty, UnitPrice }`
- [ ] Cập nhật `CustomerReturnModel` + `VendorReturnModel`: thêm price fields

---

### D7 — Web.Framework: Handlers

- [ ] `GetDeliveryNotesByCustomerHandler`: query DeliveryNote filter `CustomerId` + `SourceType=ToCustomer` + `Status=Delivered`
- [ ] `GetDeliveryNoteItemsForReturnHandler`: query items → tính `alreadyReturnedQty` từ CustomerReturn confirmed có cùng DeliveryNoteId
- [ ] `GetGoodsReceiptsByVendorHandler`: query GoodsReceipt filter `VendorId` + `SourceType=FromVendor`
- [ ] `GetGoodsReceiptItemsForReturnHandler`: query items → tính `alreadyReturnedQty` từ VendorReturn confirmed
- [ ] Cập nhật `CreateCustomerReturnHandler` + `CreateVendorReturnHandler`: map price fields mới

---

### D8 — Web: Controllers + Views

- [ ] `CustomerReturnController`: thêm 2 AJAX actions:
  - `GET /CustomerReturn/GetDeliveryNotes?customerId=` → JSON `List<DeliveryNotePickerModel>`
  - `GET /CustomerReturn/GetDeliveryNoteItems?deliveryNoteId=` → JSON `List<ReturnableItemModel>`
- [ ] `VendorReturnController`: thêm 2 AJAX actions:
  - `GET /VendorReturn/GetGoodsReceipts?vendorId=` → JSON
  - `GET /VendorReturn/GetGoodsReceiptItems?goodsReceiptId=` → JSON
- [ ] Redesign `CustomerReturn/Create.cshtml`:
  - **Bỏ** input OrderId + ProductId thủ công
  - Khách hàng: `<select>` có Search (Select2 hoặc Choices.js), load danh sách
  - Phiếu xuất: `<select>` load AJAX theo customerId (nullable — để trống = tạo tự do)
  - Bảng items: nếu chọn phiếu → load AJAX + fill sẵn tên/qty/giá; nếu tự do → nút "+ Thêm hàng" với product search
  - Mỗi row: `Tên hàng | ĐVT | Đã giao | Đã trả | Còn lại | SL trả | Đơn giá gốc | Đơn giá trả về`
  - Footer: `Chi phí phát sinh (AdditionalCost)` | `Tổng hoàn = Σ(SL × Đơn giá trả) − Chi phí`
  - Kho: `<select>` Warehouse dropdown
- [ ] Redesign `VendorReturn/Create.cshtml`: tương tự — NCC → Phiếu nhập → Items
- [ ] Update `CustomerReturn/Details.cshtml`: hiển thị `ReturnUnitPrice` per item, `AdditionalCost`, net amount
- [ ] Update `VendorReturn/Details.cshtml`: tương tự

---

### D9 — Migration (Tuấn tự chạy)

```
Add-Migration UpdateReturnsAddPriceAndDeliveryNoteRef
Update-Database
```

---

## Đã hoàn thành

| Hạng mục | Phase | Session |
|----------|-------|---------|
| Audit hệ thống + viết IMPROVEMENT_PLAN.md | — | 2026-05-06 |
| A1 — Thêm SourceType cho GoodsReceipt + DeliveryNote (code, mapping; migration chờ Tuấn) | A | 2026-05-07 |
| A2 — PurchaseOrderManager.ReceiveItemAsync auto-tạo GoodsReceipt thay vì gọi stockManager trực tiếp | A | 2026-05-07 |
| A3 — ProductAppService bỏ initial stock logic; DTO/Model/View cập nhật | A | 2026-05-07 |
| A4 — Xóa 5 actions InventoryController + handlers + commands + models | A | 2026-05-07 |
| A5 — Bỏ 5 method stock thao tác khỏi IInventoryAppService | A | 2026-05-07 |
| A6 — RevertReceiveAsync thay AdjustStockAsync cho rollback use-case | A | 2026-05-07 |
| B1 — Domain.Shared: enums, events, exceptions cho Returns | B | 2026-05-07 |
| B2 — Domain Entities: CustomerReturn/Item + VendorReturn/Item; ApplyReturn trên Debts; DeliveryNote thêm constructor/methods | B | 2026-05-07 |
| B3 — Domain.Services: CustomerReturnManager + VendorReturnManager + extensions GoodsReceiptManager/DeliveryNoteManager | B | 2026-05-07 |
| B4 — Application: AppDtos + IAppServices + AppServices + Event Handlers (CustomerReturnConfirmed + VendorReturnConfirmed) + FinalizeConfirmAsync | B | 2026-05-07 |
| B5 — Infrastructure: EF mappings (4 bảng Returns) + DI registrations trong Program.cs | B | 2026-05-07 |
| B6 — Presentation: Commands/Queries/Models/Handlers/Controllers/ModelFactories/Views + sidebar menu | B | 2026-05-07 |
| C3 — Snapshot CostAtDispatch trên DeliveryNoteItem tại thời điểm MarkDelivered (+ CreateAsDeliveredAsync) | C | 2026-05-08 |
| C6 — DeliveryNoteDeliveredStockHandler mới; bỏ inline DispatchStockAsync khỏi Manager | C | 2026-05-08 |
| C2 — FinancialReportAppService đổi nguồn sang DeliveryNote.DeliveredOnUtc + COGS từ CostAtDispatch + trừ CustomerReturn | C | 2026-05-08 |
| C1 — CustomerRefund entity + event CustomerReturnOverRefunded + Manager + AppService + Handler + EF Mapping + Controller + Views | C | 2026-05-08 |

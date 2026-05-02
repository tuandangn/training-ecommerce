# CheckList — VLXD Tuấn Khôi / NamEcommerce

> Lưu trữ các hạng mục đã hoàn thành. Chỉ đọc — không cập nhật trạng thái.

---

## ✅ Module Inventory — GoodsReceipt

**Cấp độ:** Trung bình

### [PRIORITY: HIGH] Sửa 4 lỗi nghiệp vụ: tồn kho, giá vốn, và xóa phiếu

---

#### Vấn đề 1 — Tạo phiếu nhập không cộng tồn kho ✅ DONE 2026-04-25

`GoodsReceiptManager.CreateGoodsReceiptAsync` chỉ publish `EntityCreated` nhưng không có handler nào gọi `ReceiveStockAsync`. Hệ quả: nhập hàng xong tồn kho vẫn = 0.

**Việc đã làm:**
- [x] Thêm `StockReferenceType.GoodsReceipt = 6` vào enum trong `StockMovementLog.cs`
- [x] Tạo `GoodsReceiptCreatedHandler` tại `Application.Services/Events/GoodsReceipts/`
  - Implements `INotificationHandler<EntityCreatedNotification<GoodsReceipt>>`
  - Lặp qua `Items`, với mỗi item có `WarehouseId` → gọi `IInventoryStockManager.ReceiveStockAsync(..., referenceType: StockReferenceType.GoodsReceipt, referenceId: goodsReceipt.Id)`

---

#### Vấn đề 2 — Set giá vốn không cập nhật AverageCost ✅ DONE 2026-04-26

`SetGoodsReceiptItemUnitCostAsync` chỉ lưu `UnitCost` trên item, không cập nhật `InventoryStock.AverageCost`. Báo cáo giá vốn hàng bán sẽ sai.

**Thiết kế đã được xác nhận — Full Recalculation:**

Mỗi khi một item được định giá lần đầu, **tính lại AverageCost từ toàn bộ GoodsReceiptItems đã có giá** cho cùng `(ProductId, WarehouseId)`:

```
AverageCost = SUM(item.Quantity × item.UnitCost)   ← chỉ items đã có UnitCost
              ─────────────────────────────────
                       SUM(item.Quantity)
```

Lý do **không dùng cập nhật tăng dần (incremental)**:
- Nhiều phiếu nhập được tạo không biết giá trước, giá được điền vào sau ở các ngày khác nhau và không theo thứ tự
- Incremental sẽ cho kết quả sai tùy theo thứ tự set giá
- Full Recalculation luôn cho kết quả đúng bất kể thứ tự

**Lưu ý:** AverageCost trong giai đoạn còn item chưa định giá chỉ phản ánh phần đã biết giá — dùng `IsPendingCosting()` ở UI để cảnh báo người dùng.

**Việc đã làm:**
- [x] Thêm property `AverageCost decimal` vào `InventoryStock` entity *(2026-04-26 — Tuấn cần chạy `Add-Migration AddAverageCostToInventoryStock`)*
- [x] Thêm method `UpdateAverageCostAsync(Guid productId, Guid warehouseId, decimal newAverageCost)` vào `IInventoryStockManager` + implement trong `InventoryStockManager` *(2026-04-26 — kèm 5 unit tests TDD: negative, not found, unchanged-idempotent, valid, zero. Thêm error key `Error.StockAverageCostCannotBeNegative` vào cả 2 .resx)*
- [x] Trong `GoodsReceiptManager.SetGoodsReceiptItemUnitCostAsync`: pass `item.Id` vào `AdditionalData` khi publish `EntityUpdated` để handler phân biệt được đây là UnitCost update *(2026-04-26 — kèm unit test mới `SetGoodsReceiptItemUnitCostAsync_ValidDto_PublishesEntityUpdatedWithItemIdAsAdditionalData` verify AdditionalData == itemId)*
- [x] Cập nhật `GoodsReceiptUpdatedHandler` *(2026-04-26 — refactor toàn bộ handler. Inject `IInventoryStockManager` + `IEntityDataReader<GoodsReceipt>`. Bỏ deps cũ `IRepository<Picture>`/`IEntityDataReader<Picture>` vì logic xoá ảnh cũ đã bị comment-out từ trước. Dispatch theo pattern `notification.AdditionalData is Guid itemId` — chỉ kích hoạt khi là Guid (SetUnitCost flow), bỏ qua các flow khác (UpdateGoodsReceiptAsync truyền `IEnumerable<Guid>`). Defensive: kiểm tra null entity / item / WarehouseId / UnitCost. Query LINQ qua EF: `DataSource.SelectMany(gr => gr.Items).Where(i => i.ProductId == ... && i.WarehouseId == ... && i.UnitCost.HasValue)` rồi tính `Σ(qty × cost) / Σ(qty)`. Guard `totalQuantity <= 0` để tránh chia 0)*

---

#### Vấn đề 3 — Xóa phiếu không hoàn nguyên tồn kho ✅ DONE 2026-04-26

`DeleteGoodsReceiptAsync` xóa phiếu nhưng không trừ tồn đã cộng → dữ liệu tồn kho ảo.

**Quyết định thiết kế: Cấm xóa phiếu đã cộng tồn**

Thay vì reverse stock (phức tạp, dễ âm tồn nếu đã bán một phần), hệ thống sẽ block xóa nếu đã có `StockMovementLog` tham chiếu đến phiếu này.

**Việc đã làm:**
- [x] Tạo exception `GoodsReceiptHasStockMovementsException` tại `Domain.Shared/Exceptions/GoodsReceipts/`
- [x] Inject `IEntityDataReader<StockMovementLog>` vào `GoodsReceiptManager`
- [x] Trong `DeleteGoodsReceiptAsync`: kiểm tra tồn tại `StockMovementLog` nào có `ReferenceType == GoodsReceipt && ReferenceId == dto.GoodsReceiptId` → nếu có thì throw exception
- [x] Thêm localization key `"Error.GoodsReceipt.CannotDeleteHasStockMovements"` vào `Resources/SharedResource.vi-VN.resx` và `Resources/SharedResource.resx`. Value VI: `"Không thể xóa phiếu nhập đã phát sinh tồn kho. Hãy điều chỉnh tồn (Adjust) hoặc tạo phiếu xuất bù trừ thay vì xóa."` Value EN: `"Cannot delete a goods receipt that has stock movements. Please use stock adjustment or create an offsetting issue note instead."`

---

**Files đã đụng:**
- `NamEcommerce.Domain/Entities/Inventory/StockMovementLog.cs` — thêm enum value
- `NamEcommerce.Domain/Entities/Inventory/InventoryStock.cs` — thêm `AverageCost`
- `NamEcommerce.Domain.Shared/Services/Inventory/IInventoryStockManager.cs` — thêm `UpdateAverageCostAsync`
- `NamEcommerce.Domain.Services/Inventory/InventoryStockManager.cs` — implement method trên
- `NamEcommerce.Domain.Shared/Exceptions/GoodsReceipts/GoodsReceiptHasStockMovementsException.cs` — file mới
- `NamEcommerce.Domain.Services/GoodsReceipts/GoodsReceiptManager.cs` — inject StockMovementLog reader, sửa Delete + Set UnitCost
- `NamEcommerce.Application.Services/Events/GoodsReceipts/GoodsReceiptCreatedHandler.cs` — file mới
- `NamEcommerce.Application.Services/Events/GoodsReceipts/GoodsReceiptUpdatedHandler.cs` — sửa để handle AverageCost

---

## ✅ Module Inventory — Mapping GoodsReceipt ↔ PurchaseOrder

**Cấp độ:** Trung Bình | **Độ ưu tiên:** Cao | **Hoàn thành:** 2026-04-28

> Giao diện cho người quản lý chọn PurchaseOrder phù hợp với GoodsReceipt, hoặc tạo mới PO nhanh từ GR. Hệ thống tự gợi ý PO dựa trên ngày nhận (ReceivedOnUtc) và items.

---

#### Phase 0 — Chuẩn bị ✅ DONE

- [x] **Fix VendorId constraint** trong `GoodsReceiptManager.SetGoodsReceiptToPurchaseOrder()`
- [x] **Expose PurchaseOrderId / PurchaseOrderCode** vào toàn bộ DTO chain (Domain.Shared, Domain.Services, Application.Contracts, Application.Services, Web.Contracts, Web Models, Handler, ModelFactory)

#### Phase 1 — Domain Layer: "Suggested PO" logic ✅ DONE

- [x] DTO `SuggestedPurchaseOrderForGoodsReceiptDto` (Domain.Shared/Dtos/GoodsReceipts/)
- [x] Method `GetSuggestedPurchaseOrdersAsync(Guid goodsReceiptId)` vào `IGoodsReceiptManager`
- [x] Thuật toán implement trong `GoodsReceiptManager`:
  - Lọc PO hợp lệ: `PlacedOnUtc < GR.ReceivedOnUtc` **VÀ** Status `Approved | Receiving` — không lọc VendorId
  - Tính MatchScore: GR item có UnitCost → tìm exact `(ProductId, UnitCost)`; không UnitCost → ProductId bất kỳ; `MatchScore = matched / totalGrQty × 100`
  - Sắp xếp: `IsFullMatch` trước → `MatchScore` desc → `PlacedOnUtc` desc → top 20

#### Phase 2 — Application Layer ✅ DONE

- [x] AppDto `SuggestedPurchaseOrderForGoodsReceiptAppDto`
- [x] Method `GetSuggestedPurchaseOrdersAsync` trong `IGoodsReceiptAppService` + implement (enrich ProductName, convert DateTime sang local time)

#### Phase 3 — Presentation Layer ✅ DONE

- [x] Model `SuggestedPurchaseOrderModel`
- [x] Query `GetSuggestedPurchaseOrdersForGoodsReceiptQuery` + Handler
- [x] Endpoints `GoodsReceiptController`: `GET /GetSuggestedPurchaseOrders`, `POST /SetToPurchaseOrder`, `POST /SetToPurchaseOrderByCode`

#### Phase 4 — UI (View + JavaScript) ✅ DONE

- [x] Section "Phiếu Đặt Hàng" trong `GoodsReceipts/Details.cshtml` với badge link / panel gợi ý / progress bar MatchScore / modal tìm theo mã PO thủ công

#### Phase 5 — Quick-Create PO từ GoodsReceipt ✅ DONE

- [x] Command + ResultModel + Handler `QuickCreateAndLinkPurchaseOrder` (load GR → build PO items → tạo PO → link)
- [x] Endpoint `POST /GoodsReceipt/QuickCreateAndLink`
- [x] UI: nút "+ Tạo mới PO" + modal với VendorPicker, PlacedOn, items preview + JS submit

---

## ✅ Module GoodsReceipt — Vendor + Sinh công nợ tự động

**Mục tiêu:**
- `GoodsReceipt` có thể gắn 1 nhà cung cấp (optional, cập nhật được sau).
- Khi toàn bộ items được định giá (`IsPendingCosting() == false`) **VÀ** đã có VendorId → tự động tạo `VendorDebt` với tổng tiền = Σ(Quantity × UnitCost).
- Idempotent: gọi lại nhiều lần không tạo thêm phiếu nợ trùng.

---

#### Phase 1 — Domain: Mở rộng GoodsReceipt & VendorDebt ✅ DONE

**`GoodsReceipt` entity:**
- [x] Thêm 4 properties nullable: `VendorId`, `VendorName`, `VendorPhone`, `VendorAddress`
- [x] Thêm method `internal void SetVendor(Guid vendorId, string vendorName, string? phone, string? address)`
- [x] Thêm method `internal void ClearVendor()`

**`GoodsReceiptDtos`:**
- [x] Thêm `VendorId?`, `VendorName?`, `VendorPhone?`, `VendorAddress?` vào `BaseGoodsReceiptDto`
- [x] Thêm record `SetGoodsReceiptVendorDto(Guid GoodsReceiptId)` với `VendorId?`

**`IGoodsReceiptManager` + `GoodsReceiptManager`:**
- [x] Cập nhật `CreateGoodsReceiptAsync` / `UpdateGoodsReceiptAsync` để set vendor khi có trong dto
- [x] Thêm method `SetGoodsReceiptVendorAsync(SetGoodsReceiptVendorDto dto)`

**`VendorDebt` entity** — schema breaking change:
- [x] Đổi `PurchaseOrderId` + `PurchaseOrderCode` → nullable
- [x] Thêm `GoodsReceiptId Guid?`
- [x] Thêm internal constructor mới cho GoodsReceipt-based debt

**`IVendorDebtManager` + `VendorDebtManager`:**
- [x] Thêm `CreateDebtFromGoodsReceiptAsync` (idempotent)
- [x] Thêm `CreateVendorDebtFromGoodsReceiptDto`

#### Phase 2 — Application: Tự động sinh công nợ khi định giá xong ✅ DONE 2026-04-26

- [x] Mở rộng `GoodsReceiptUpdatedHandler`: sau bước tính AverageCost, kiểm tra điều kiện sinh công nợ
- [x] Thêm case xử lý khi `AdditionalData is "vendor-updated"`
- [x] Mở rộng `GoodsReceiptCreatedHandler` để xử lý edge case phiếu tạo với đủ vendor + UnitCost

#### Phase 3 — Presentation: UI + API ✅ DONE 2026-04-26

- [x] `POST /GoodsReceipt/SetVendor` — AJAX endpoint
- [x] **Create page** — thêm section chọn vendor (optional), dùng `VendorPicker.js`
- [x] **Details page** — card "Nhà cung cấp", modal Bootstrap, badge "Đã ghi nợ"
- [x] Cập nhật View Models + Handlers

---

**Files đã chạm session 2026-04-26:**
- `Presentation/NamEcommerce.Web/Resources/SharedResource.vi-VN.resx` — thêm key `Error.GoodsReceipt.CannotDeleteHasStockMovements` + `Error.StockAverageCostCannotBeNegative` (VI)
- `Presentation/NamEcommerce.Web/Resources/SharedResource.resx` — thêm 2 keys tương ứng (EN)
- `Domain/NamEcommerce.Domain/Entities/Inventory/InventoryStock.cs` — thêm property `AverageCost decimal`
- `Infrastructure/NamEcommerce.Data.SqlServer/Mappings/InventoryStockMapping.cs` — thêm `decimal(18,2)` mapping
- `Domain/NamEcommerce.Domain.Shared/Services/Inventory/IInventoryStockManager.cs` — thêm signature `UpdateAverageCostAsync`
- `Domain/NamEcommerce.Domain.Services/Inventory/InventoryStockManager.cs` — implement `UpdateAverageCostAsync`
- `Tests/NamEcommerce.Domain.Services.Test/Helpers/InventoryStockDataReader.cs` — file mới
- `Tests/NamEcommerce.Domain.Services.Test/Helpers/InventoryStockRepository.cs` — file mới
- `Tests/NamEcommerce.Domain.Services.Test/Services/InventoryStockManagerTests.cs` — file mới, 5 test cases TDD
- `Domain/NamEcommerce.Domain.Services/GoodsReceipts/GoodsReceiptManager.cs` — `SetGoodsReceiptItemUnitCostAsync` pass `item.Id` qua `AdditionalData`
- `Tests/NamEcommerce.Domain.Services.Test/Services/GoodsReceiptManagerTests.cs` — thêm 1 test verify AdditionalData
- `Application/NamEcommerce.Application.Services/Events/GoodsReceipts/GoodsReceiptUpdatedHandler.cs` — refactor toàn bộ
- `Presentation/NamEcommerce.Web/wwwroot/modules/order.details.js` — thay `fetch(...)` bằng `apiPost(...)`
- `Presentation/NamEcommerce.Web/wwwroot/modules/CreatePurchaseOrderController.js` — import `apiGet`
- `Presentation/NamEcommerce.Web/wwwroot/modules/OrderController.js` — import `apiGet`
- Application + Domain + Web files cho Vendor + Debt phases (xem chi tiết session notes bên dưới)

---

## ✅ UnitTest — InventoryReceipt — Suggested PO logic

**Cấp độ:** Trung Bình | **Độ ưu tiên:** Thấp | **Hoàn thành:** 2026-04-29

> Test cho `GetSuggestedPurchaseOrdersAsync` trong `GoodsReceiptManagerTests.cs`. 7 tests viết theo TDD bám sát thuật toán: filter PO theo ngày + status, tính MatchScore (exact match có UnitCost / partial match theo ProductId), sắp xếp `IsFullMatch` desc → `MatchScore` desc → `PlacedOnUtc` desc.

- [x] **Exact match**: GR items có UnitCost → PO có `(ProductId, UnitCost)` khớp đủ qty → `MatchScore = 100`, `IsFullMatch = true`
- [x] **Partial match (no UnitCost)**: GR item không có UnitCost → match theo ProductId bất kỳ → `MatchScore < 100` (vd 4/10 = 40)
- [x] **Lọc theo ngày**: PO `PlacedOnUtc > GR.ReceivedOnUtc` → không xuất hiện trong kết quả
- [x] **Lọc theo status**: PO `Draft / Submitted / Completed / Cancelled` → bị loại; chỉ `Approved | Receiving` mới qua
- [x] **Không có PO phù hợp**: return empty list, không throw
- [x] **Sắp xếp đúng**: 3 PO (newer/older full match + partial) → đúng thứ tự `IsFullMatch` desc → `MatchScore` desc → `PlacedOnUtc` desc
- [x] **VendorId không ảnh hưởng**: PO khác VendorId với GR vẫn được gợi ý nếu items khớp

**Files đã chạm:**
- `Tests/NamEcommerce.Domain.Services.Test/Services/GoodsReceiptManagerTests.cs` — thêm region `GetSuggestedPurchaseOrdersAsync` với 2 helper (`BuildGoodsReceiptWithItemsAsync`, `BuildPurchaseOrderForSuggestionAsync` — dùng reflection thêm items vào `_items` để bypass Product/Vendor lookup chains) + 7 tests `[Fact]`. Imports thêm `System.Reflection`, `Domain.Entities.PurchaseOrders`, `Domain.Shared.Enums.PurchaseOrders`.

⚠️ **Tuấn cần làm sau merge session 2026-04-29:**
- Build verify: `dotnet build NamEcommerce.sln`
- Run unit tests: `dotnet test Tests/NamEcommerce.Domain.Services.Test/ --filter "FullyQualifiedName~GoodsReceiptManagerTests"`

---

## ✅ UI/UX Notification

**Cấp độ:** Dễ

**Mục tiêu:** Gom 3 cơ chế thông báo (Bootstrap toast server-side, Bootstrap alert ở Layout, SweetAlert2 client-side) thành 1 quy trình & 1 UI duy nhất với **Notyf**.

#### Phase 1 — Foundation (server-side abstraction) ✅ DONE

- [x] Tạo `NotificationModel` (`Type`, `Message`, `Title?`, `DurationMs`) tại `NamEcommerce.Web.Contracts/Models/Common/`
- [x] Tạo enum `NotificationType { Success, Error, Warning, Info }`
- [x] Tạo `INotificationService` với `Success/Error/Warning/Info(message, title?)` + `ConsumeAll()`
- [x] Implement `TempDataNotificationService` — lưu `List<NotificationModel>` JSON-serialized vào key `"Messages.Notifications"`
- [x] Đăng ký scoped trong DI (`Program.cs`)
- [x] Thêm helper `NotifySuccess(key)` / `NotifyError(key)` vào `BaseController`
- [x] Tạo `JsonNotificationResult(Success, Message, NotificationType?, Data)` + extension `JsonOk/JsonError`

#### Phase 2 — Client-side (Notyf) ✅ DONE

- [x] Cài Notyf — copy `notyf.min.js` + `notyf.min.css` vào `wwwroot/lib/notyf/`
- [x] Đăng ký script + style trong `_Scripts.cshtml` và `_Styles.cshtml`
- [x] Tạo `wwwroot/js/notification-center.js` — wrap Notyf, expose `window.NotificationCenter`
- [x] Tạo `Views/Shared/_Notifications.cshtml`
- [x] Thêm `<partial name="_Notifications" />` vào `_Layout.cshtml`
- [x] Bỏ block alert `errorMessage` trong `_Layout.cshtml`
- [x] Alias `toast(...)` cũ trong `modals.js` trỏ vào `NotificationCenter`
- [x] Tạo `wwwroot/modules/ajax-helper.js`

#### Phase 3 — Migrate Controller ✅ DONE

- [x] Customer
- [x] Vendor
- [x] Category
- [x] Product
- [x] Warehouse
- [x] Inventory
- [x] PurchaseOrder
- [x] Order
- [x] GoodsReceipt
- [x] DeliveryNote ← fix bug key sai
- [x] CustomerDebt ← fix bug key sai
- [x] VendorDebt ← fix bug key sai
- [x] UnitMeasurement
- [x] Expense *(không có TempData notification — bỏ qua)*

#### Phase 4 — Cleanup ✅ DONE

- [x] Refactor `GlobalExceptionFilter` dùng `INotificationService.Error(...)`
- [x] Xoá toàn bộ `XxxSuccessMessage` / `XxxErrorMessage` trong `ViewConstants.cs`
- [x] Xoá `Views/Shared/_Messages.cshtml` *(thay nội dung bằng comment deprecated — Tuấn xoá file thủ công khi merge)*
- [x] Xoá block render TempData toast trong các view List
- [x] Migrate JS module sang `apiPost` helper ✅ DONE 2026-04-26

---

## ✅ System - Event Refactor

### Phase 1 — Foundation ✅ DONE 2026-04-27

**Files mới tạo:**
- `Domain/NamEcommerce.Domain.Shared/Events/IDomainEvent.cs` — marker interface kế thừa `MediatR.INotification`
- `Domain/NamEcommerce.Domain.Shared/Events/DomainEvent.cs` — `abstract record DomainEvent : IDomainEvent`
- `Infrastructure/NamEcommerce.Data.SqlServer/Interceptors/DomainEventDispatchInterceptor.cs` — `SaveChangesInterceptor`
- `Tests/NamEcommerce.Data.SqlServer.Test/Interceptors/DomainEventDispatchInterceptorTests.cs` — 6 tests TDD

**Files đã sửa:**
- `Domain.Shared.csproj` — thêm `MediatR.Contracts` 2.0.1
- `Domain.Shared/AppAggregateEntity.cs` — thêm `_domainEvents`, `DomainEvents`, `RaiseDomainEvent`, `ClearDomainEvents`
- `Data.SqlServer.csproj` — thêm `MediatR` 14.1.0
- `Presentation/NamEcommerce.Web/Program.cs` — đăng ký `DomainEventDispatchInterceptor` + chuyển `AddDbContext` sang `(sp, opts) =>`
- `Data.SqlServer.Test.csproj` — thêm `MediatR` 14.1.0 + `EFCore.InMemory` 10.0.5 + `Moq` 4.18.1

**Pattern decision:**
- Aggregate raise event qua `protected RaiseDomainEvent(...)` (chỉ subclass gọi được)
- Interceptor clear events TRƯỚC khi publish (tránh re-publish nếu handler gây nested SaveChanges)
- `IEventPublisher` cũ giữ nguyên để Phase 2/3 migrate dần từng module

---

### Phase 2 — Migrate Orders + DeliveryNotes ✅ DONE 2026-04-27

**Files mới tạo:**
- `Domain.Shared/Events/Orders/OrderEvents.cs` — 9 sealed records `: DomainEvent`
- `Domain.Shared/Events/DeliveryNotes/DeliveryNoteEvents.cs` — 5 sealed records `: DomainEvent`

**Files đã sửa:**
- `Domain/Entities/Orders/Order.cs` — thêm 4 method `Place()`, `MarkInfoUpdated()`, `MarkShippingUpdated()`, `MarkDeleted()` + raise in-place trong các method nghiệp vụ
- `Domain/Entities/DeliveryNotes/DeliveryNote.cs` — thêm `MarkCreated()` + raise event trong `Confirm()`/`MarkDelivering()`/`MarkDelivered()`/`Cancel()`
- `Domain.Services/Orders/OrderManager.cs` — bỏ `IEventPublisher` (7→6 deps), `ClearDomainEvents()` trước `Place()`
- `Domain.Services/DeliveryNotes/DeliveryNoteManager.cs` — bỏ `IEventPublisher`
- `Application.Services/Events/DeliveryNotes/DeliveryNoteConfirmedEventHandler.cs` — `INotificationHandler<DeliveryNoteConfirmed>`
- `Application.Services/Events/DeliveryNotes/DeliveryNoteDeliveredEventHandler.cs` — `INotificationHandler<DeliveryNoteDelivered>`
- `Tests/.../OrderManagerTests.cs` — 47 constructor calls 7→6 args + DomainEvents assertions

**Items evaluated / skipped:**
- `OrderPlacedReserveStockHandler` — skip (chưa có ReserveStock logic)
- `OrderCancelledReleaseStockHandler` — skip (chưa có concrete event `OrderCancelled`)
- Smoke test end-to-end — pending (Tuấn xác nhận sau khi merge)

**Pattern decisions:**
- `CreateOrderAsync`: `ClearDomainEvents()` trước `Place()` — tránh double-publish `OrderItemAdded` events lúc setup
- Notification cũ (`DeliveryNoteConfirmedNotification`, `DeliveryNoteDeliveredNotification`) chưa xoá — Phase 5 cleanup
- `EventPublisher.cs` chưa xoá — Phase 3 migrate dần

---

### Phase 3 — Migrate Catalog + Customers + Inventory (Partial) ✅ DONE 2026-04-28

**Modules đã migrate:**
- ✅ `UnitMeasurement` (Catalog)
- ✅ `Category` (Catalog) — thêm `CategoryParentChanged` event riêng
- ✅ `Vendor` (Catalog)
- ✅ `Customer` (Customers)
- ✅ `Warehouse` (Inventory)
- ✅ `Picture` (Media) — base events `PictureCreated`/`PictureDeleted` (`PictureOrphaned` để khi migrate Product)

**Files mới tạo (5):**
- `Domain.Shared/Events/Catalog/CategoryEvents.cs` — `CategoryCreated`, `CategoryUpdated`, `CategoryParentChanged`, `CategoryDeleted`
- `Domain.Shared/Events/Catalog/VendorEvents.cs` — `VendorCreated`, `VendorUpdated`, `VendorDeleted`
- `Domain.Shared/Events/Customers/CustomerEvents.cs` — `CustomerCreated`, `CustomerUpdated`, `CustomerDeleted`
- `Domain.Shared/Events/Inventory/WarehouseEvents.cs` — `WarehouseCreated`, `WarehouseUpdated`, `WarehouseDeleted`
- (UnitMeasurementEvents.cs đã có từ 2026-04-27)

**Files đã sửa entity (5):** UnitMeasurement, Category, Vendor, Customer, Warehouse — thêm 3-4 method `Mark*`

**Files đã sửa manager (5):**
- `UnitMeasurementManager` — 3→2 deps
- `CategoryManager` — 3→2 deps; `SetParentCategoryAsync` raise `CategoryParentChanged`
- `VendorManager` — 3→2 deps
- `CustomerManager` — đã không inject `IEventPublisher` từ trước
- `WarehouseManager` — 3→2 deps

**Files đã sửa test (4):** UnitMeasurementManagerTests, CategoryManagerTests, VendorManagerTests, WarehouseManagerTests

---

### Phase 3 — Migrate Debts module ✅ DONE 2026-04-28

**Modules đã migrate:**
- ✅ `CustomerDebt` (Debts)
- ✅ `CustomerPayment` (Debts)
- ✅ `VendorDebt` (Debts)
- ✅ `VendorPayment` (Debts)

**Files mới tạo (2):**
- `Domain.Shared/Events/Debts/CustomerDebtEvents.cs` — `CustomerDebtCreated`, `CustomerDebtUpdated`, `CustomerDebtFullyPaid`
- `Domain.Shared/Events/Debts/CustomerPaymentEvents.cs` — `CustomerPaymentRecorded`

**Files đã sửa entity (3):**
- `Entities/Debts/CustomerDebt.cs` — `MarkCreated()`, `MarkUpdated()` (raise `CustomerDebtFullyPaid` khi `Status == FullyPaid`)
- `Entities/Debts/CustomerPayment.cs` — `MarkCreated()`
- `Entities/Debts/VendorPayment.cs` — `MarkCreated()`

**Files đã sửa manager (2):**
- `Domain.Services/Debts/VendorDebtManager.cs` — bỏ `IEventPublisher` (8→7 deps)
- `Domain.Services/Debts/CustomerDebtManager.cs` — thêm `Mark*` calls (vốn không inject `IEventPublisher`)

**Files đã sửa test (1):**
- `VendorDebtManagerTests.cs` — helper 8→7 args + 2 DomainEvents assertions (`VendorDebtCreated`, `VendorDebtFullyPaid`)

**Pattern decisions:**
- `CustomerDebt`/`VendorDebt` raise `*FullyPaid` BÊN TRONG `MarkUpdated()` khi `Status == FullyPaid`
- Không có domain event handler nào subscribe các events này → không cần migrate handler

---

## 📝 Session Notes (reference)

### Session 2026-04-25 — GoodsReceipt Vấn đề 1

Handler `GoodsReceiptCreatedHandler` tại `Application.Services/Events/GoodsReceipts/` mới tạo, implements `INotificationHandler<EntityCreatedNotification<GoodsReceipt>>`. Enum `StockReferenceType.GoodsReceipt = 6` thêm vào `StockMovementLog.cs`.

### Session 2026-04-26 — GoodsReceipt Vấn đề 2 + 3 + Vendor + JS migrate

- `InventoryStock.AverageCost` property + migration cần chạy: `Add-Migration AddAverageCostToInventoryStock`
- `UpdateAverageCostAsync` với 5 unit tests TDD
- `GoodsReceiptUpdatedHandler` refactor hoàn toàn
- `GoodsReceiptHasStockMovementsException` + localization keys
- Vendor phases 1+2+3 hoàn chỉnh (domain → application → UI)
- Migration cần chạy: `Add-Migration AddVendorToGoodsReceiptAndDebt` + `Update-Database`
- JS modules `order.details.js`, `CreatePurchaseOrderController.js`, `OrderController.js` migrate sang `apiPost`/`apiGet`

### Session 2026-04-27 — Event Refactor Phase 1 + Phase 2

- Foundation: `IDomainEvent`, `DomainEvent`, `AppAggregateEntity` mở rộng, `DomainEventDispatchInterceptor` + 6 unit tests
- Phase 2: Orders (9 events) + DeliveryNotes (5 events) migrate hoàn chỉnh
- `OrderManager` 7→6 deps; `DeliveryNoteManager` bỏ `IEventPublisher`
- 2 handlers chuyển sang concrete domain events

⚠️ **Tuấn cần làm sau khi merge session 2026-04-27:**
- Restore packages: `dotnet restore NamEcommerce.sln`
- Build verify: `dotnet build NamEcommerce.sln`
- Run unit tests: `dotnet test Tests/NamEcommerce.Data.SqlServer.Test/` (6 tests mới) + `dotnet test Tests/NamEcommerce.Domain.Services.Test/` (47 constructor calls đã đổi)
- Smoke test app start + Order flow + DeliveryNote confirmed/delivered flow

### Session 2026-04-28 — Event Refactor Phase 3 (Catalog + Customers + Inventory.Warehouse + Debts)

- Catalog: UnitMeasurement, Category, Vendor — 3→2 deps mỗi manager, 80+ test constructor calls update
- Customers: Customer — đã không có `IEventPublisher` từ trước
- Inventory: Warehouse — 3→2 deps
- Media: Picture — base events
- Debts: CustomerDebt, CustomerPayment, VendorDebt, VendorPayment — VendorDebtManager 8→7 deps, 2 DomainEvents assertions mới

⚠️ **Tuấn cần làm sau merge session 2026-04-28:**
- Build verify: `dotnet build NamEcommerce.sln`
- Run unit tests: `dotnet test Tests/NamEcommerce.Domain.Services.Test/`
- Smoke test: tạo/sửa/xoá entity của 5 module qua UI; VendorDebt + CustomerDebt event dispatch

---

## ✅ System - Event Refactor — Phase 3 tiếp (Catalog/Product + Inventory/InventoryStock + PurchaseOrders/PurchaseOrder)

**Cấp độ:** Trung Bình | **Độ ưu tiên:** Cao | **Hoàn thành:** 2026-04-30

> Tiếp tục Phase 3 — migrate 3 module còn lại sang Domain Event mới.
> Lưu ý: session này KHÔNG viết unit test mới (Tuấn sẽ tự bổ sung sau).

---

#### Catalog/Product ✅ DONE 2026-04-30 (verify only)

Khi audit thì phát hiện Product **đã hoàn tất migration từ trước**:

- `Domain.Shared/Events/Catalog/ProductEvents.cs` — `ProductCreated`, `ProductUpdated`, `ProductDeleted`, `ProductPriceChanged` (đã có sẵn)
- `Domain/Entities/Catalog/Product.cs` — `MarkCreated()`, `MarkUpdated(IEnumerable<Guid> deletedPictureIds)`, `MarkDeleted()`, `MarkPriceChanged(oldUnitPrice, oldCostPrice)` (đã có sẵn)
- `Domain.Services/Catalog/ProductManager.cs` — không inject `IEventPublisher`, dùng `Mark*` methods (đã có sẵn)
- `Application.Services/Events/Catalog/ProductDeletedEventHandler.cs` — `INotificationHandler<ProductDeleted>` (đã có sẵn)
- `Application.Services/Events/Catalog/ProductUpdatedEventHandler.cs` — `INotificationHandler<ProductUpdated>` (đã có sẵn)

→ Không cần thay đổi gì cho Catalog/Product.

#### Inventory/InventoryStock ✅ DONE 2026-04-30 (verify only)

`InventoryStockManager` không inject `IEventPublisher`. `InventoryStock` entity không raise event nào (chỉ là dữ liệu nội bộ về tồn kho — manipulate qua `AdjustStock`, `ReceiveStock`, `DispatchStock`, `ReserveStock`, `ReleaseReservedStock`, `UpdateAverageCost`). Không có handler `EntityCreatedNotification<InventoryStock>` / `EntityUpdatedNotification<InventoryStock>` / `EntityDeletedNotification<InventoryStock>` nào tồn tại.

→ Module đã sạch khỏi pattern cũ. Nếu sau này cần publish event cho external system (low-stock alert, average-cost-changed) thì có thể thêm `Mark*` methods + concrete events — không scope của Phase 3.

#### PurchaseOrders/PurchaseOrder ✅ DONE 2026-04-30

**File mới tạo (2):**
- `Domain.Shared/Events/PurchaseOrders/PurchaseOrderEvents.cs` — 6 sealed records: `PurchaseOrderCreated`, `PurchaseOrderUpdated`, `PurchaseOrderStatusChanged`, `PurchaseOrderItemAdded`, `PurchaseOrderItemRemoved`, `PurchaseOrderItemReceived`
- `Application.Services/Events/PurchaseOrders/PurchaseOrderItemReceivedEventHandler.cs` — `INotificationHandler<PurchaseOrderItemReceived>` thay cho handler cũ. Chỉ chạy đúng khi item được nhận → gọi `VerifyStatusAsync` để transition Approved → Receiving → Completed.

**File đã sửa entity:**
- `Domain/Entities/PurchaseOrders/PurchaseOrder.cs` — thêm 6 method `MarkCreated()`, `MarkUpdated()`, `MarkStatusChanged(oldStatus)`, `MarkItemAdded(item)`, `MarkItemRemoved(itemId)`, `MarkItemReceived(itemId, qty)`

**File đã sửa manager:**
- `Domain.Services/PurchaseOrders/PurchaseOrderManager.cs` — bỏ inject `IEventPublisher` (12→11 deps). Tất cả `_eventPublisher.EntityCreated/EntityUpdated(...)` thay bằng `purchaseOrder.Mark*()` calls đặt trước `Insert/UpdateAsync`.
  - `CreatePurchaseOrderAsync` → `MarkCreated()`
  - `UpdatePurchaseOrderAsync` → `MarkUpdated()`
  - `AddPurchaseOrderItemAsync` → `MarkItemAdded(item)` (loại bỏ `EntityUpdated(po)` vì handler `VerifyStatus` không cần chạy khi chỉ thêm item — receivedQty vẫn = 0)
  - `ChangeStatusAsync` → `MarkStatusChanged(oldStatus)` (bắt `oldStatus` trước khi `ChangeStatus`)
  - `ReceiveItemsAsync` → `MarkItemReceived(itemId, qty)` (handler mới subscribe event này)
  - `DeleteOrderItemAsync` → `MarkItemRemoved(itemId)`

**File đã sửa test:**
- `Tests/NamEcommerce.Domain.Services.Test/Services/PurchaseOrderManagerTests.cs` — 22 constructor calls update: bỏ arg `Mock.Of<IEventPublisher>()` (12→11 args). Bỏ `using NamEcommerce.Domain.Shared.Events;`.

**File deprecated:**
- `Application.Services/Events/PurchaseOrders/PurchaseOrderUpdatedEventHandler.cs` — class bị xoá, nội dung file thay bằng comment migration note. Sandbox không cho xoá file → Tuấn xoá file thủ công khi review.

**Pattern decisions:**
- `MarkStatusChanged` cần bắt `oldStatus` BEFORE gọi `ChangeStatus` (immutable record nên không thể đọc state cũ sau khi đã thay đổi).
- `AddPurchaseOrderItemAsync` không raise `PurchaseOrderUpdated` nữa vì side-effect duy nhất của `EntityUpdated` cũ là `VerifyStatusAsync` — và verify không có ý nghĩa khi item mới có receivedQty = 0.
- `ReceiveItemsAsync` raise `PurchaseOrderItemReceived` TRƯỚC khi `UpdateAsync` để event được pickup bởi `DomainEventDispatchInterceptor` sau `SaveChanges`.

⚠️ **Tuấn cần làm sau merge session 2026-04-30:**
- Build verify: `dotnet build NamEcommerce.sln` (sandbox không có dotnet — chưa verify được build).
- Xoá file `PurchaseOrderUpdatedEventHandler.cs` (đang là stub comment).
- Smoke test: PurchaseOrder flow → tạo PO → submit → approve → receive items → đơn tự transition Receiving → Completed (handler mới `PurchaseOrderItemReceivedEventHandler` xử lý đúng).
- Nếu có existing test mock `IEventPublisher` ở chỗ khác (ngoài `PurchaseOrderManagerTests`) thì update tương tự.

---

## ✅ System - Event Refactor — Phase 3 Users (verify only)

**Cấp độ:** Dễ | **Độ ưu tiên:** Cao | **Hoàn thành:** 2026-05-01

> Audit Users module — phát hiện đã hoàn tất migration sang Domain Event mới từ trước (giống như Catalog/Product được phát hiện ở session 2026-04-30). Không cần thay đổi code.

#### Tình trạng các thành phần

- `Domain.Shared/Events/Users/UserEvents.cs` — đã có 4 sealed records: `UserCreated(Guid UserId, string Username, string FullName)`, `UserUpdated(Guid UserId)`, `UserPasswordChanged(Guid UserId)`, `UserDeleted(Guid UserId, string Username)`.
- `Domain/Entities/Users/User.cs` — đã có 4 method `MarkCreated()`, `MarkUpdated()`, `MarkPasswordChanged()`, `MarkDeleted()` trong region `Domain Event Markers`.
- `Domain.Services/Users/UserManager.cs` — KHÔNG inject `IEventPublisher` (3 deps: `IRepository<User>`, `IEntityDataReader<User>`, `ISecurityService`). `CreateUserAsync` đã gọi `user.MarkCreated()` trước `_userRepository.InsertAsync(user)`.
- `Application.Services/Users/UserAppService.cs` — KHÔNG dùng `IEventPublisher`.
- `Tests/NamEcommerce.Domain.Services.Test/Services/UserManagerTests.cs` — constructor đã 3 args, không cần update.
- Toàn solution không còn reference đến `EntityCreatedNotification<User>` / `EntityUpdatedNotification<User>` / `EntityDeletedNotification<User>`.

#### Pattern decision

`IUserManager` hiện tại chỉ expose 2 method (`CreateUserAsync`, `FindUserByUserNameAndPasswordAsync` + base `DoesUsernameExistAsync`). Không có Update / ChangePassword / Delete — nên `MarkUpdated`/`MarkPasswordChanged`/`MarkDeleted` đã chuẩn bị sẵn cho khi bổ sung manager method tương ứng. Khi cần, chỉ việc gọi `Mark*` trước `UpdateAsync`/`DeleteAsync`.

→ Phase 3 còn lại duy nhất `GoodsReceipts` (phức tạp).

---

## ✅ System - Event Refactor — Phase 5 Prerequisite (Migrate dead Order handlers)

**Cấp độ:** Dễ | **Độ ưu tiên:** Cao | **Hoàn thành:** 2026-05-02 (session 3)

> Block cuối cùng trước khi xoá `EntityCreatedNotification<T>` / `EntityUpdatedNotification<T>` legacy types: 2 dead handler trong module `Orders` còn subscribe legacy notification với body rỗng. Đã migrate / chuyển stub.

#### Files thay đổi

- `Application.Services/Events/Orders/OrderCreatedEventHandler.cs` — migrate sang `INotificationHandler<OrderPlaced>` (concrete event đã có sẵn trong `Domain.Shared/Events/Orders/OrderEvents.cs`). Body vẫn rỗng + giữ TODO comment cho việc implement Reserve Stock sau (logic chưa từng được implement). Constructor inject `IOrderManager` giữ nguyên.
- `Application.Services/Events/Orders/OrderUpdatedEventHandler.cs` — class bị xoá, nội dung file thay bằng comment migration note. Body cũ trả `Task.CompletedTask` → KHÔNG có logic mất mát. Nhu cầu phản ứng với việc Order thay đổi đã được phục vụ bởi 2 concrete event `OrderInfoUpdated` / `OrderShippingUpdated`. Sandbox không xoá được file → Tuấn xoá thủ công khi review.

#### Verify

- `grep` toàn solution: KHÔNG còn `INotificationHandler` nào subscribe `EntityCreatedNotification<T>` / `EntityUpdatedNotification<T>` / `EntityDeletedNotification<T>` (chỉ còn comments giải thích migration history + định nghĩa records trong `EventPublisher.cs` — sẽ xoá ở step Phase 5 tiếp theo).
- `OrderPlaced : DomainEvent : IDomainEvent : INotification` ✓ — handler hoạt động với MediatR.

#### Tuấn cần làm sau merge

- Build verify: `dotnet build NamEcommerce.sln` (sandbox không có dotnet — chưa verify được build).
- KHÔNG cần migration / smoke test (không có code logic thay đổi — cả 2 handler có body rỗng).
- (Optional) Xoá thủ công các stub file: `OrderUpdatedEventHandler.cs` (mới session 3), và các stub cũ `GoodsReceiptUpdatedHandler.cs`, `PurchaseOrderUpdatedEventHandler.cs`. `OrderCreatedEventHandler.cs` chỉ xoá nếu xác nhận không implement Reserve Stock.

---

## ✅ System - Event Refactor — Phase 5 (Step 1: Xoá 3 stub file)

**Cấp độ:** Dễ | **Độ ưu tiên:** Cao | **Hoàn thành:** 2026-05-02 (session 3, late stage)

> Sau khi prerequisite hoàn tất (zero subscriber còn dùng legacy notification), tiến hành xoá 3 stub file đã đánh dấu safe-to-delete. Sandbox Linux không xoá được file trên Windows mount (`rm: Operation not permitted`) → workaround dùng `mv` ra folder `.trash/` (đã thêm vào `.gitignore`). File rời source tree → MSBuild không include → tương đương xoá về mặt build.

#### Files đã ra khỏi source tree

- `Application.Services/Events/GoodsReceipts/GoodsReceiptUpdatedHandler.cs` → `.trash/GoodsReceiptUpdatedHandler.cs.deleted`
- `Application.Services/Events/PurchaseOrders/PurchaseOrderUpdatedEventHandler.cs` → `.trash/PurchaseOrderUpdatedEventHandler.cs.deleted`
- `Application.Services/Events/Orders/OrderUpdatedEventHandler.cs` → `.trash/OrderUpdatedEventHandler.cs.deleted`

#### Side fix

- `Domain.Shared/Services/Inventory/IInventoryStockManager.cs` — xref comment cho `UpdateAverageCostAsync` đổi từ `GoodsReceiptUpdatedHandler` → `GoodsReceiptItemUnitCostSetHandler` (handler thật sự gọi method này sau khi handler cũ bị split).

#### .gitignore

- Thêm rule `.trash/` để folder workaround không bị track lên git.

#### Tuấn cần làm sau merge

- (Khuyến nghị) Xoá hẳn folder `.trash/` trên Windows sau khi review:
  ```powershell
  Remove-Item D:\Learning\NamTraining\training-ecommerce\.trash\ -Recurse -Force
  ```
- Build verify: `dotnet build NamEcommerce.sln` (sandbox không có dotnet — chưa verify được build).
- Quyết định riêng cho `OrderCreatedEventHandler.cs`: giữ (nếu sẽ implement Reserve Stock) hay xoá (nếu không). File này KHÔNG bị xoá trong session vì có điều kiện.

#### Step Phase 5 còn lại

- Xoá `OrderCreatedEventHandler.cs` (conditional)
- Xoá legacy event chain: `EntityCreatedEvent`, `EntityUpdatedEvent`, `EntityDeletedEvent`, `EventPublisher.cs`, `IEventPublisher.cs`, `EventPublisherExtensions.cs`, DI registration trong `Program.cs:148`
- Xoá `BaseEvent` + 2 file event mồ côi (`DeliveryNoteConfirmedEvent.cs`, `DeliveryNoteDeliveredEvent.cs`)
- Update tài liệu (skill `namcommerce` + `SYSTEM_DOCUMENTATION.md`)

---

## ✅ System - Event Refactor — Phase 3 GoodsReceipts (verify only)

**Cấp độ:** Khó (theo TodoList) | **Độ ưu tiên:** Cao | **Hoàn thành:** 2026-05-02

> Audit GoodsReceipts module — phát hiện đã hoàn tất migration sang Domain Event mới từ trước (giống như Catalog/Product/Users đã được phát hiện ở các session trước). **Không cần thay đổi code production nào.**

#### Tình trạng các thành phần

- `Domain.Shared/Events/GoodsReceipts/GoodsReceiptEvents.cs` — đã có 7 sealed records concrete: `GoodsReceiptCreated`, `GoodsReceiptUpdated`, `GoodsReceiptItemUnitCostSet`, `GoodsReceiptVendorChanged`, `GoodsReceiptDeleted` (mang theo `IReadOnlyCollection<Guid> PictureIds` để handler dọn ảnh), `GoodsReceiptSetToPurchaseOrder`, `GoodsReceiptRemovedFromPurchaseOrder`. XML doc đầy đủ giải thích từng handler chịu trách nhiệm gì.
- `Domain/Entities/GoodsReceipts/GoodsReceipt.cs` — region `Events` chứa 7 method `Mark*()` raise concrete events. `MarkDeleted()` capture snapshot `_pictureIds.ToList()` để handler dọn ảnh sau khi entity bị xoá.
- `Domain.Services/GoodsReceipts/GoodsReceiptManager.cs` — KHÔNG inject `IEventPublisher` (11 deps khác — Repository, DataReader, settings, manager). Mọi mutation flow (`CreateGoodsReceiptAsync`, `UpdateGoodsReceiptAsync`, `SetGoodsReceiptItemUnitCostAsync`, `SetGoodsReceiptVendorAsync`, `DeleteGoodsReceiptAsync`) đều gọi `goodsReceipt.Mark*()` trước repository call.
- `Application.Services/Events/GoodsReceipts/GoodsReceiptCreatedHandler.cs` — subscribe `GoodsReceiptCreated`. Cộng tồn kho cho từng item có WarehouseId qua `IInventoryStockManager.ReceiveStockAsync` + try sinh `VendorDebt` cho edge case "tạo phiếu với đủ vendor + UnitCost ngay từ đầu" (idempotent qua `CreateDebtFromGoodsReceiptAsync`).
- `Application.Services/Events/GoodsReceipts/GoodsReceiptItemUnitCostSetHandler.cs` — subscribe `GoodsReceiptItemUnitCostSet`. Tính lại `InventoryStock.AverageCost` theo Full Recalculation `Σ(qty×cost)/Σ(qty)` trên toàn bộ DB cho cặp `(ProductId, WarehouseId)` + try sinh nợ NCC.
- `Application.Services/Events/GoodsReceipts/GoodsReceiptVendorChangedHandler.cs` — subscribe `GoodsReceiptVendorChanged`. Chỉ try sinh nợ NCC (idempotent), không đụng AverageCost (vì vendor thay đổi không liên quan đến giá vốn).
- `Application.Services/Events/GoodsReceipts/GoodsReceiptDeletedEventHandler.cs` — subscribe `GoodsReceiptDeleted`. Hoàn nguyên tồn kho qua `AdjustStockAsync` + xoá ảnh theo `notification.PictureIds` (đã capture trong event trước khi soft delete).
- `Application.Services/Events/GoodsReceipts/GoodsReceiptUpdatedHandler.cs` — chỉ chứa comment giải thích đã bị split → 2 concrete handlers (`GoodsReceiptItemUnitCostSetHandler` + `GoodsReceiptVendorChangedHandler`). File stub chờ Phase 5 cleanup.
- Toàn solution KHÔNG còn reference đến `EntityCreatedNotification<GoodsReceipt>` / `EntityUpdatedNotification<GoodsReceipt>` / `EntityDeletedNotification<GoodsReceipt>`.

#### Bonus — Phase 5 prerequisite gần như sẵn sàng

Audit cross-solution cho legacy publisher chain:

- `.EntityCreated()` / `.EntityUpdated()` / `.EntityDeleted()` extension methods — KHÔNG còn caller nào trong toàn `*.cs`.
- `IEventPublisher` injection — chỉ còn DI registration trong `Program.cs:148`. Không Manager / AppService / Handler nào inject.
- 2 handler subscribe legacy notification còn sống nhưng **dead code** (publish-side đã chết): `OrderCreatedEventHandler` (`INotificationHandler<EntityCreatedNotification<Order>>` — chỉ TODO comment về reserve stock, body trả `Task.CompletedTask`), `OrderUpdatedEventHandler` (body trả `Task.CompletedTask`).
- 2 stub file đã đánh dấu safe-to-delete trong file: `GoodsReceiptUpdatedHandler.cs`, `PurchaseOrderUpdatedEventHandler.cs`.

→ Phase 5 (Cleanup) có đầy đủ điều kiện để triển khai trong session tiếp theo có sự hiện diện của Tuấn — chỉ cần migrate hoặc xoá 2 OrderHandler, sau đó loại bỏ chuỗi `IEventPublisher` / `EntityCreatedEvent`/`EntityUpdatedEvent`/`EntityDeletedEvent` / 3 notification record / `EventPublisher` / `EventPublisherExtensions` / DI registration. Có thể tận dụng nốt cleanup `BaseEvent` + 2 file `DeliveryNoteConfirmedEvent` / `DeliveryNoteDeliveredEvent` (chỉ là class definition không ai khởi tạo, handlers thật subscribe `DeliveryNoteConfirmed` / `DeliveryNoteDelivered` concrete events khác tên).

→ **Phase 3 (System Event Refactor) hoàn tất 100% sau session này.**

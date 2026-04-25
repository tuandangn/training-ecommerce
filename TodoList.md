# TodoList — VLXD Tuấn Khôi / NamEcommerce

> File theo dõi các hạng mục cần thực thi. Cập nhật trạng thái khi bắt đầu / hoàn thành.

---

## Module Inventory — GoodsReceipt

**Cấp độ:** Trung bình

### [PRIORITY: HIGH] Sửa 4 lỗi nghiệp vụ: tồn kho, giá vốn, và xóa phiếu

---

#### Vấn đề 1 — Tạo phiếu nhập không cộng tồn kho ✅ DONE 2026-04-25

`GoodsReceiptManager.CreateGoodsReceiptAsync` chỉ publish `EntityCreated` nhưng không có handler nào gọi `ReceiveStockAsync`. Hệ quả: nhập hàng xong tồn kho vẫn = 0.

**Việc cần làm:**
- [x] Thêm `StockReferenceType.GoodsReceipt = 6` vào enum trong `StockMovementLog.cs`
- [x] Tạo `GoodsReceiptCreatedHandler` tại `Application.Services/Events/GoodsReceipts/`
  - Implements `INotificationHandler<EntityCreatedNotification<GoodsReceipt>>`
  - Lặp qua `Items`, với mỗi item có `WarehouseId` → gọi `IInventoryStockManager.ReceiveStockAsync(..., referenceType: StockReferenceType.GoodsReceipt, referenceId: goodsReceipt.Id)`

---

#### Vấn đề 2 — Set giá vốn không cập nhật AverageCost ⏳ NOT STARTED

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

**Việc cần làm:**
- [ ] Thêm property `AverageCost decimal` vào `InventoryStock` entity *(nhớ migration)*
- [ ] Thêm method `UpdateAverageCostAsync(Guid productId, Guid warehouseId, decimal newAverageCost)` vào `IInventoryStockManager` + implement trong `InventoryStockManager`
- [ ] Trong `GoodsReceiptManager.SetGoodsReceiptItemUnitCostAsync`: pass `item.Id` vào `AdditionalData` khi publish `EntityUpdated` để handler phân biệt được đây là UnitCost update
- [ ] Cập nhật (hoặc tách mới) `GoodsReceiptUpdatedHandler`:
  - Kiểm tra `notification.AdditionalData is Guid itemId` → là lần set giá
  - Lấy item từ `notification.Entity.Items.FirstOrDefault(i => i.Id == itemId)`
  - Nếu `item.WarehouseId == null` hoặc `item.UnitCost == null` → bỏ qua
  - Query toàn bộ `GoodsReceipt` có items cùng `(ProductId, WarehouseId)` đã có `UnitCost`
  - Tính `newAvg = Σ(qty × cost) / Σ(qty)`
  - Gọi `UpdateAverageCostAsync(productId, warehouseId, newAvg)`

---

#### Vấn đề 3 — Xóa phiếu không hoàn nguyên tồn kho 🔄 ALMOST DONE — chỉ còn localization

`DeleteGoodsReceiptAsync` xóa phiếu nhưng không trừ tồn đã cộng → dữ liệu tồn kho ảo.

**Quyết định thiết kế: Cấm xóa phiếu đã cộng tồn**

Thay vì reverse stock (phức tạp, dễ âm tồn nếu đã bán một phần), hệ thống sẽ block xóa nếu đã có `StockMovementLog` tham chiếu đến phiếu này.

**Việc cần làm:**
- [x] Tạo exception `GoodsReceiptHasStockMovementsException` tại `Domain.Shared/Exceptions/GoodsReceipts/`
- [x] Inject `IEntityDataReader<StockMovementLog>` vào `GoodsReceiptManager`
- [x] Trong `DeleteGoodsReceiptAsync`: kiểm tra tồn tại `StockMovementLog` nào có `ReferenceType == GoodsReceipt && ReferenceId == dto.GoodsReceiptId` → nếu có thì throw exception
- [ ] **TODO** Thêm localization key `"Error.GoodsReceipt.CannotDeleteHasStockMovements"` vào `Resources/SharedResource.vi-VN.resx` (và bản gốc nếu cần). VD value: `"Không thể xóa phiếu nhập đã phát sinh tồn kho. Hãy điều chỉnh tồn (Adjust) hoặc tạo phiếu xuất bù trừ thay vì xóa."`

> ⚠️ **Cần test sau khi thêm DI:** `GoodsReceiptManager` constructor đã thêm tham số `IEntityDataReader<StockMovementLog>`. `IEntityDataReader<>` đăng ký bằng generic open type ở `Program.cs` nên không cần register thêm — nhưng nên build verify ngay sau khi merge để chắc.

---

**Files chính cần đụng:**
- `NamEcommerce.Domain/Entities/Inventory/StockMovementLog.cs` — thêm enum value
- `NamEcommerce.Domain/Entities/Inventory/InventoryStock.cs` — thêm `AverageCost`
- `NamEcommerce.Domain.Shared/Services/Inventory/IInventoryStockManager.cs` — thêm `UpdateAverageCostAsync`
- `NamEcommerce.Domain.Services/Inventory/InventoryStockManager.cs` — implement method trên
- `NamEcommerce.Domain.Shared/Exceptions/GoodsReceipts/GoodsReceiptHasStockMovementsException.cs` — file mới
- `NamEcommerce.Domain.Services/GoodsReceipts/GoodsReceiptManager.cs` — inject StockMovementLog reader, sửa Delete + Set UnitCost
- `NamEcommerce.Application.Services/Events/GoodsReceipts/GoodsReceiptCreatedHandler.cs` — file mới
- `NamEcommerce.Application.Services/Events/GoodsReceipts/GoodsReceiptUpdatedHandler.cs` — sửa để handle AverageCost

**Phụ thuộc:**
- Vấn đề 3 phụ thuộc vào vấn đề 1 (cần có `StockReferenceType.GoodsReceipt` trước)
- Vấn đề 2 phụ thuộc vào vấn đề 1 (cần handler created chạy đúng trước mới có stock để tính avg)
- Cần tạo migration cho `InventoryStock.AverageCost` trước khi deploy vấn đề 2

---

### 📌 Trạng thái hiện tại (cập nhật 2026-04-25)

**Đã làm xong session này:**
- ✅ Toàn bộ module **UI/UX Notification** (Phase 1 → 4) — chỉ còn JS module migrate optional ở Phase 4
- ✅ GoodsReceipt **Vấn đề 1** (Tạo phiếu nhập cộng tồn kho) — handler đã sẵn sàng nhận event
- 🔄 GoodsReceipt **Vấn đề 3** (Cấm xóa) — code Domain xong, **chỉ còn thêm localization key**

**Lượt sau bắt đầu từ:**
1. **Ưu tiên cao nhất** — Thêm localization key `Error.GoodsReceipt.CannotDeleteHasStockMovements` vào `SharedResource.vi-VN.resx` (nhanh, hoàn tất Vấn đề 3)
2. **Build verify** toàn bộ solution — `dotnet build` để chắc DI cho `IEntityDataReader<StockMovementLog>` không vỡ + các thay đổi Notification module compile sạch
3. **Vấn đề 2** — AverageCost Full Recalculation. Cần làm theo thứ tự:
   - Thêm `AverageCost` decimal vào `InventoryStock` entity → **báo Tuấn tự chạy `Add-Migration`**
   - Thêm `UpdateAverageCostAsync` vào `IInventoryStockManager` + impl trong `InventoryStockManager`
   - Modify `GoodsReceiptManager.SetGoodsReceiptItemUnitCostAsync` pass `item.Id` qua `AdditionalData`
   - Refactor `GoodsReceiptUpdatedHandler` (file hiện tại đang rỗng/comment) để Full Recalculation
4. **Sau cùng** — Module **System - Event Refactor** (Khó + HIGH, ~1 tuần) — bắt đầu từ Phase 1 Foundation

**Files đã chạm session này (chưa build verify):**
- `Domain/Entities/Inventory/StockMovementLog.cs` — thêm `GoodsReceipt = 6`
- `Domain.Shared/Exceptions/GoodsReceipts/GoodsReceiptHasStockMovementsException.cs` — file mới
- `Domain.Services/GoodsReceipts/GoodsReceiptManager.cs` — inject `IEntityDataReader<StockMovementLog>` + check trong Delete
- `Application.Services/Events/GoodsReceipts/GoodsReceiptCreatedHandler.cs` — file mới

---

## System - Event

**Cấp độ:** Khó

### [PRIORITY: HIGH] Refactor Event System theo DDD đúng chuẩn

**Mục tiêu:** Thay thế mô hình CRUD-style notification (`EntityCreatedEvent<T>`, `EntityUpdatedEvent<T>`, `EntityDeletedEvent<T>`) bằng Domain Events đúng nghĩa DDD — event mang ngữ nghĩa nghiệp vụ, aggregate tự raise event, dispatch sau SaveChanges, tách Domain Event vs Integration Event với Outbox pattern.

**Lý do:**
- Event hiện tại không kể được "vì sao update" → handler `OrderUpdatedEventHandler` rỗng vì không phân biệt được 7 hành vi nghiệp vụ khác nhau cùng publish chung một event.
- `object? AdditionalData` mất type safety, dễ break runtime.
- Manager raise event hộ aggregate → vi phạm encapsulation DDD.
- Publish event sau `SaveChanges` riêng lẻ → không atomic, race condition với external system (n8n).
- `EventPublisher` dùng `switch` + `throw NotSupportedException()` → vi phạm Open/Closed.
- Handler nhận nguyên Entity → leak internal state.
- `BaseEvent` rỗng, thiếu `EventId`, `OccurredOnUtc`, `CorrelationId` → không trace, không replay được.

---

#### Phase 1 — Foundation (1-2 ngày)

- [ ] Tạo `IDomainEvent : INotification` trong `NamEcommerce.Domain.Shared/Events/`
- [ ] Tạo `abstract record DomainEvent : IDomainEvent` với `EventId`, `OccurredOnUtc`
- [ ] Cập nhật `AppAggregateEntity`:
  - Thêm `private readonly List<IDomainEvent> _domainEvents`
  - Expose `IReadOnlyCollection<IDomainEvent> DomainEvents` với `[NotMapped]`
  - Method `protected void RaiseDomainEvent(IDomainEvent)` và `public void ClearDomainEvents()`
- [ ] Viết `DomainEventDispatchInterceptor : SaveChangesInterceptor` trong `NamEcommerce.Data.SqlServer/Interceptors/`
- [ ] Đăng ký Interceptor trong `Program.cs` / `DbContext` configuration
- [ ] Unit test cho Interceptor (mock `IPublisher`, verify dispatch + clear events)
- [ ] Giữ song song `IEventPublisher` cũ — không xoá ngay để tránh break code hiện tại

#### Phase 2 — Migrate Orders + DeliveryNotes (2-3 ngày)

- [ ] Định nghĩa concrete events cho Orders:
  - `OrderPlaced(OrderId, CustomerId, TotalAmount, WarehouseId?)`
  - `OrderItemAdded(OrderId, OrderItemId, ProductId, Quantity)`
  - `OrderItemRemoved(OrderId, OrderItemId)`
  - `OrderLocked(OrderId)`
  - `OrderCancelled(OrderId, Reason)`
  - Thêm các event khác tương ứng với 7 chỗ gọi `EntityUpdated` hiện tại
- [ ] Định nghĩa concrete events cho DeliveryNotes:
  - `DeliveryNoteCreated(DeliveryNoteId, OrderId, CustomerId)`
  - `DeliveryNoteConfirmed(DeliveryNoteId)` — đã có, chuẩn hoá lại
  - `DeliveryNoteDelivered(DeliveryNoteId, CustomerId, TotalAmount)`
  - `DeliveryNoteCancelled(DeliveryNoteId)`
- [ ] Refactor `Order` aggregate: gọi `RaiseDomainEvent(...)` trong constructor + các method nghiệp vụ
- [ ] Refactor `DeliveryNote` aggregate tương tự
- [ ] Xoá `eventPublisher.EntityCreated/Updated/Deleted(...)` trong `OrderManager` và `DeliveryNoteManager`
- [ ] Refactor handler hiện tại theo từng concrete event:
  - `OrderPlacedReserveStockHandler` (thay cho `OrderCreatedEventHandler` đang trống)
  - `OrderCancelledReleaseStockHandler`
  - `DeliveryNoteDeliveredCreateDebtHandler` (refactor từ `DeliveryNoteDeliveredEventHandler`)
- [ ] Update unit test Manager: assert `aggregate.DomainEvents` chứa event mong đợi thay vì verify `eventPublisher.EntityCreated(...)`
- [ ] Smoke test end-to-end: tạo order → confirm delivery note → mark delivered → verify công nợ tự động tạo

#### Phase 3 — Migrate các module còn lại (2 ngày)

- [ ] Catalog: `Product`, `Category`, `Vendor`, `UnitMeasurement`
- [ ] Inventory: `InventoryStock`, `Warehouse`, stock movements
- [ ] PurchaseOrders: `PurchaseOrder` (note: `PurchaseOrderUpdatedEventHandler` hiện đang gọi `VerifyStatusAsync` — cần thay bằng concrete event như `PurchaseOrderItemReceived`)
- [ ] GoodsReceipts: `GoodsReceipt` (dọn handler trống `GoodsReceiptUpdatedHandler`)
- [ ] Debts: `CustomerDebt`, `CustomerPayment`, `VendorDebt`
- [ ] Customers: `Customer`
- [ ] Media: `Picture` (event `PictureOrphaned` thay cho logic xoá ảnh hiện đang nằm trong `ProductDeletedEventHandler`, `ProductUpdatedEventHandler`, `GoodsReceiptDeletedEventHandler`)

#### Phase 4 — Outbox Pattern cho Integration Event (1-2 ngày)

- [ ] Tạo entity `OutboxMessage` (Id, Type, Payload JSON, OccurredOnUtc, ProcessedOnUtc?, Error?)
- [ ] EF Configuration + DbSet trong `DbContext` (KHÔNG tự chạy migration — báo Tuấn tự làm)
- [ ] Tạo `IOutbox` interface + implementation lưu vào DbContext (cùng transaction với SaveChanges)
- [ ] Tạo `IIntegrationEvent` marker
- [ ] Tách integration event:
  - `DeliveryNoteConfirmedIntegrationEvent` — gửi notify n8n (thay cho call trực tiếp `_n8nAppService` hiện tại)
- [ ] `BackgroundService` đọc bảng `OutboxMessages` chưa processed → publish qua MediatR → mark processed; retry nếu lỗi
- [ ] Test failure scenarios: n8n down, DB transaction rollback, duplicate dispatch (idempotency)

#### Phase 5 — Cleanup (0.5 ngày)

- [ ] Xoá `EntityCreatedEvent<T>`, `EntityUpdatedEvent<T>`, `EntityDeletedEvent<T>`
- [ ] Xoá `EntityCreatedNotification<T>`, `EntityUpdatedNotification<T>`, `EntityDeletedNotification<T>`
- [ ] Xoá `IEventPublisher` cũ + `EventPublisher` implementation
- [ ] Xoá `EventPublisherExtensions.EntityCreated/Updated/Deleted`
- [ ] Xoá các handler trống / TODO comment đã được thay thế
- [ ] Update skill `namcommerce` — thay phần "Publish events qua `IEventPublisher`" bằng hướng dẫn raise domain event mới
- [ ] Update `SYSTEM_DOCUMENTATION.md` — ghi rõ pattern Domain Event mới

---

**Tổng effort ước tính:** ~1 tuần làm cẩn thận có test.

**Files chính cần đụng:**
- `NamEcommerce.Domain.Shared/Events/` — toàn bộ event types
- `NamEcommerce.Domain.Shared/Common/AppAggregateEntity.cs`
- `NamEcommerce.Domain.Services/**/*Manager.cs` — bỏ inject `IEventPublisher`, dọn các call publish
- `NamEcommerce.Domain/Entities/**/*.cs` — thêm `RaiseDomainEvent` trong methods nghiệp vụ
- `NamEcommerce.Application.Services/Events/**/*Handler.cs` — split theo concrete event
- `NamEcommerce.Data.SqlServer/Interceptors/DomainEventDispatchInterceptor.cs` — file mới
- `NamEcommerce.Data.SqlServer/Outbox/` — thư mục mới (Phase 4)
- `NamEcommerce.Web/Program.cs` — đăng ký Interceptor + Outbox BackgroundService

**Rủi ro cần lưu ý:**
- `AppAggregateEntity` là `record` — `_domainEvents` (List reference type) hoạt động bình thường nhưng phải `[NotMapped]` để EF không persist
- Test fixtures hiện tại verify `IEventPublisher` mock — cần update sang assert `DomainEvents` collection
- Outbox cần idempotency cho handler để tránh duplicate side effect khi retry

---

## UI/UX Notification

**Cấp độ:** Dễ

### [PRIORITY: LOW] Thống nhất hệ thống thông báo người dùng

**Mục tiêu:** Gom 3 cơ chế thông báo đang chạy song song (Bootstrap toast server-side, Bootstrap alert ở Layout, SweetAlert2 client-side) thành 1 quy trình & 1 UI duy nhất. Thay UI bằng **Notyf** cho đẹp, nhẹ, có animation chuẩn.

**Vấn đề hiện tại:**
- Key TempData không khớp giữa Controller và `_Messages.cshtml` → toast biến mất ở `DeliveryNoteController`, `VendorDebtController`, `CustomerDebtController` (set `CustomerErrorMessage` thay vì key tương ứng controller).
- `ViewConstants` hardcode 14 cặp key cho từng module → vi phạm DRY, dễ copy-paste sai.
- 2 UI khác hẳn cho cùng mục đích: Bootstrap toast (server) vs SweetAlert2 (client).
- Bootstrap toast không tự ẩn (thiếu `data-bs-autohide` + `data-bs-delay`), `position-absolute` sai khi page scroll dài.
- `GlobalExceptionFilter` render alert dạng band ngang → kiểu UI thứ 3.

---

#### Phase 1 — Foundation (server-side abstraction)

- [x] Tạo `NotificationModel` (`Type`, `Message`, `Title?`, `DurationMs`) tại `NamEcommerce.Web.Contracts/Models/Common/`
- [x] Tạo enum `NotificationType { Success, Error, Warning, Info }`
- [x] Tạo `INotificationService` với `Success/Error/Warning/Info(message, title?)` + `ConsumeAll()` tại `NamEcommerce.Web/Services/Notifications/`
- [x] Implement `TempDataNotificationService` — lưu `List<NotificationModel>` JSON-serialized vào 1 key duy nhất `"Messages.Notifications"`
- [x] Đăng ký scoped trong DI (`Program.cs`)
- [x] Thêm helper `NotifySuccess(key)` / `NotifyError(key)` vào `BaseController` (tự gọi `_localizer`)
- [x] Tạo `JsonNotificationResult(Success, Message, NotificationType?, Data)` + extension `JsonOk/JsonError` cho controller

#### Phase 2 — Client-side (Notyf)

- [x] Cài Notyf — copy `notyf.min.js` + `notyf.min.css` vào `wwwroot/lib/notyf/` (CDN: `cdn.jsdelivr.net/npm/notyf@3`)
- [x] Đăng ký script + style trong `_Scripts.cshtml` và `_Styles.cshtml`
- [x] Tạo `wwwroot/js/notification-center.js` — wrap Notyf với 4 method `success/error/warning/info`, expose `window.NotificationCenter`
  - Cấu hình `position: { x: 'right', y: 'top' }`, `duration: 4000`, `dismissible: true`, `ripple: true`
  - Thêm types `warning` + `info` qua `notyf.options.types` (mặc định Notyf chỉ có success + error)
- [x] Tạo `Views/Shared/_Notifications.cshtml` — `@inject INotificationService` → render JSON các pending notification → JS chạy `NotificationCenter.show(...)` trong `DOMContentLoaded`
- [x] Thêm `<partial name="_Notifications" />` vào `_Layout.cshtml` *(giữ tạm `_Messages` đến khi xong Phase 3 để không vỡ UX)*
- [x] Bỏ block alert `errorMessage` trong `_Layout.cshtml` (line 53–59)
- [x] Alias `toast(...)` cũ trong `wwwroot/modules/modals.js` trỏ vào `NotificationCenter` để JS module hiện có không cần sửa cùng lúc
- [x] Tạo `wwwroot/modules/ajax-helper.js` — wrapper fetch tự đọc `JsonNotificationResult` và gọi `NotificationCenter.show(...)`

#### Phase 3 — Migrate Controller (theo từng module, mỗi commit 1 module)

- [x] Customer
- [x] Vendor
- [x] Category
- [x] Product
- [x] Warehouse
- [x] Inventory
- [x] PurchaseOrder
- [x] Order
- [x] GoodsReceipt
- [x] DeliveryNote ← **fix bug key sai** (đang dùng `CustomerErrorMessage`)
- [x] CustomerDebt ← **fix bug key sai**
- [x] VendorDebt ← **fix bug key sai**
- [x] UnitMeasurement
- [x] Expense *(không có TempData notification — bỏ qua)*

Mỗi controller: thay `TempData[ViewConstants.XxxSuccessMessage] = ...` bằng `NotifySuccess(...)` / `NotifyError(...)`.

#### Phase 4 — Cleanup

- [x] Refactor `GlobalExceptionFilter` dùng `INotificationService.Error(...)` thay vì set `TempData[GlobalErrorMessage]` trực tiếp
- [x] Xoá toàn bộ `XxxSuccessMessage` / `XxxErrorMessage` trong `ViewConstants.cs`
- [x] Xoá `Views/Shared/_Messages.cshtml` *(scheduled task không thể xoá file — đã thay nội dung bằng comment deprecated, Tuấn xoá file thủ công khi merge)*
- [x] Xoá block render TempData toast trong các view List (`Customer/List.cshtml`, `Vendor/List.cshtml`, `Warehouse/List.cshtml`, `Category/List.cshtml`, ...)
- [ ] Migrate JS module sang `apiPost` helper (`order.details.js`, `CreatePurchaseOrderController.js`, `OrderController.js`) — tuỳ chọn, có thể làm sau

---

**Files chính cần đụng:**
- `NamEcommerce.Web.Contracts/Models/Common/NotificationModel.cs` — file mới
- `NamEcommerce.Web/Services/Notifications/INotificationService.cs` + `TempDataNotificationService.cs` — file mới
- `NamEcommerce.Web/Controllers/BaseController.cs` — thêm helper `NotifySuccess` / `NotifyError`
- `NamEcommerce.Web/Constants/ViewConstants.cs` — xoá hằng số `XxxSuccessMessage` / `XxxErrorMessage`
- `NamEcommerce.Web/Mvc/Filters/GlobalExceptionFilter.cs` — refactor dùng `INotificationService`
- `NamEcommerce.Web/Views/Shared/_Layout.cshtml` — thay partial, bỏ alert block
- `NamEcommerce.Web/Views/Shared/_Notifications.cshtml` — file mới (thay `_Messages.cshtml`)
- `NamEcommerce.Web/Views/Shared/_Scripts.cshtml` + `_Styles.cshtml` — đăng ký Notyf
- `NamEcommerce.Web/wwwroot/lib/notyf/` — thư mục thư viện mới
- `NamEcommerce.Web/wwwroot/js/notification-center.js` — file mới
- `NamEcommerce.Web/wwwroot/modules/modals.js` — alias `toast` cũ
- `NamEcommerce.Web/wwwroot/modules/ajax-helper.js` — file mới (tuỳ chọn)
- `NamEcommerce.Web/Controllers/*Controller.cs` — refactor theo Phase 3

**Phụ thuộc:**
- Phase 2 phụ thuộc Phase 1 (cần `INotificationService`)
- Phase 3 phụ thuộc Phase 1 + 2 (cần helper `NotifySuccess` và partial `_Notifications`)
- Phase 4 phải làm cuối cùng (xoá legacy chỉ khi tất cả controller đã migrate)

**Rủi ro cần lưu ý:**
- Notyf không có sẵn type `warning` / `info` → phải tự đăng ký qua `notyf.options.types` với màu + icon riêng
- Khi reload trang sau redirect, JSON serialize TempData phải dùng cùng config với client (camelCase / PascalCase) — cần test thử
- Một số JS module hiện có gọi `toast(...)` với 3 đối số `(title, body, type)` — alias phải map đúng thứ tự sang Notyf API
- Sau khi xoá hằng số trong `ViewConstants`, build sẽ fail ở các view List còn dùng → phải migrate Phase 3 trước khi Phase 4

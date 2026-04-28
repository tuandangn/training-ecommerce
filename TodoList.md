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

**Việc cần làm:**
- [x] Thêm property `AverageCost decimal` vào `InventoryStock` entity *(2026-04-26 — Tuấn cần chạy `Add-Migration AddAverageCostToInventoryStock`)*
- [x] Thêm method `UpdateAverageCostAsync(Guid productId, Guid warehouseId, decimal newAverageCost)` vào `IInventoryStockManager` + implement trong `InventoryStockManager` *(2026-04-26 — kèm 5 unit tests TDD: negative, not found, unchanged-idempotent, valid, zero. Thêm error key `Error.StockAverageCostCannotBeNegative` vào cả 2 .resx)*
- [x] Trong `GoodsReceiptManager.SetGoodsReceiptItemUnitCostAsync`: pass `item.Id` vào `AdditionalData` khi publish `EntityUpdated` để handler phân biệt được đây là UnitCost update *(2026-04-26 — kèm unit test mới `SetGoodsReceiptItemUnitCostAsync_ValidDto_PublishesEntityUpdatedWithItemIdAsAdditionalData` verify AdditionalData == itemId)*
- [x] Cập nhật `GoodsReceiptUpdatedHandler` *(2026-04-26 — refactor toàn bộ handler. Inject `IInventoryStockManager` + `IEntityDataReader<GoodsReceipt>`. Bỏ deps cũ `IRepository<Picture>`/`IEntityDataReader<Picture>` vì logic xoá ảnh cũ đã bị comment-out từ trước. Dispatch theo pattern `notification.AdditionalData is Guid itemId` — chỉ kích hoạt khi là Guid (SetUnitCost flow), bỏ qua các flow khác (UpdateGoodsReceiptAsync truyền `IEnumerable<Guid>`). Defensive: kiểm tra null entity / item / WarehouseId / UnitCost. Query LINQ qua EF: `DataSource.SelectMany(gr => gr.Items).Where(i => i.ProductId == ... && i.WarehouseId == ... && i.UnitCost.HasValue)` rồi tính `Σ(qty × cost) / Σ(qty)`. Guard `totalQuantity <= 0` để tránh chia 0)*
  - Kiểm tra `notification.AdditionalData is Guid itemId` → là lần set giá
  - Lấy item từ `notification.Entity.Items.FirstOrDefault(i => i.Id == itemId)`
  - Nếu `item.WarehouseId == null` hoặc `item.UnitCost == null` → bỏ qua
  - Query toàn bộ `GoodsReceipt` có items cùng `(ProductId, WarehouseId)` đã có `UnitCost`
  - Tính `newAvg = Σ(qty × cost) / Σ(qty)`
  - Gọi `UpdateAverageCostAsync(productId, warehouseId, newAvg)`

---

#### Vấn đề 3 — Xóa phiếu không hoàn nguyên tồn kho ✅ DONE 2026-04-26

`DeleteGoodsReceiptAsync` xóa phiếu nhưng không trừ tồn đã cộng → dữ liệu tồn kho ảo.

**Quyết định thiết kế: Cấm xóa phiếu đã cộng tồn**

Thay vì reverse stock (phức tạp, dễ âm tồn nếu đã bán một phần), hệ thống sẽ block xóa nếu đã có `StockMovementLog` tham chiếu đến phiếu này.

**Việc cần làm:**
- [x] Tạo exception `GoodsReceiptHasStockMovementsException` tại `Domain.Shared/Exceptions/GoodsReceipts/`
- [x] Inject `IEntityDataReader<StockMovementLog>` vào `GoodsReceiptManager`
- [x] Trong `DeleteGoodsReceiptAsync`: kiểm tra tồn tại `StockMovementLog` nào có `ReferenceType == GoodsReceipt && ReferenceId == dto.GoodsReceiptId` → nếu có thì throw exception
- [x] Thêm localization key `"Error.GoodsReceipt.CannotDeleteHasStockMovements"` vào `Resources/SharedResource.vi-VN.resx` và `Resources/SharedResource.resx` (bản gốc EN). Value VI: `"Không thể xóa phiếu nhập đã phát sinh tồn kho. Hãy điều chỉnh tồn (Adjust) hoặc tạo phiếu xuất bù trừ thay vì xóa."` Value EN: `"Cannot delete a goods receipt that has stock movements. Please use stock adjustment or create an offsetting issue note instead."`

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

### 📌 Trạng thái hiện tại (cập nhật 2026-04-28)

**Đã làm xong session này:**
- ✅ Toàn bộ module **UI/UX Notification** (Phase 1 → 4) — đã hoàn tất cả phần JS module migrate optional ở Phase 4
- ✅ GoodsReceipt **Vấn đề 1** (Tạo phiếu nhập cộng tồn kho) — handler đã sẵn sàng nhận event
- ✅ GoodsReceipt **Vấn đề 2** (Full Recalculation AverageCost) — `GoodsReceiptUpdatedHandler` đã refactor xong 2026-04-26
- ✅ GoodsReceipt **Vấn đề 3** (Cấm xóa) — đã thêm localization key vào cả `SharedResource.vi-VN.resx` và `SharedResource.resx` ngày 2026-04-26
- ✅ JS module migrate apiPost/apiGet (`order.details.js`, `CreatePurchaseOrderController.js`, `OrderController.js`) — 2026-04-26 (scheduled task)
- ✅ **Vendor + Sinh công nợ tự động** — Phase 1 Domain + Phase 2 Application + Phase 3 backend & UI — **2026-04-26 scheduled task (3 lần làm)**. UI Views Create.cshtml + Details.cshtml + VendorPicker integration đều đã hoàn thành.
- ✅ **Event Refactor Phase 1 Foundation** — `IDomainEvent`/`DomainEvent`/`AppAggregateEntity.RaiseDomainEvent`/`DomainEventDispatchInterceptor` + 6 unit tests — **2026-04-27 scheduled task**.
- ✅ **Event Refactor Phase 2 Migrate Orders + DeliveryNotes** — 14 concrete domain events, 2 aggregate refactor, 2 manager bỏ `IEventPublisher`, 2 handler refactor sang `INotificationHandler<TDomainEvent>`, 47 OrderManager constructor calls trong test — **2026-04-27 scheduled task**.
- ✅ **Event Refactor Phase 3 — UnitMeasurement migrate** — `UnitMeasurement` aggregate thêm 3 method `MarkCreated/MarkUpdated/MarkDeleted`, `UnitMeasurementManager` bỏ `IEventPublisher` (3 → 2 deps), 18 test constructor updated + 3 test bổ sung DomainEvents assertion — **2026-04-28 scheduled task**.
- ✅ **Event Refactor Phase 3 — Category/Vendor/Customer/Warehouse migrate** — 4 modules cùng pattern: tạo events file, aggregate thêm 3 method `Mark*`, Manager bỏ `IEventPublisher`, tests update 80+ constructor calls + 12 DomainEvents assertions — **2026-04-28 scheduled task (continuation)**.

**Lượt sau bắt đầu từ:**
1. **Build verify** toàn bộ solution — `dotnet build` để chắc:
   - DI cho `IEntityDataReader<StockMovementLog>` + `IEntityDataReader<GoodsReceipt>` + `IVendorDebtManager` (đã có sẵn trong DI) không vỡ
   - `VendorDebtManagerTests.cs` compile sạch sau khi fix helper 7→8 params
   - Các thay đổi Notification + Vendor + Handler module compile sạch (sandbox không có dotnet, Tuấn tự chạy)
2. **Smoke test** flow nghiệp vụ:
   - **AverageCost flow:**
     - Tạo phiếu nhập có WarehouseId + UnitCost → tồn kho cộng + AverageCost cập nhật
     - Tạo phiếu nhập KHÔNG có UnitCost → tồn cộng, AverageCost không thay đổi
     - Set UnitCost cho item của phiếu nhập đó → AverageCost cập nhật theo Full Recalculation
     - Tạo phiếu nhập thứ 2 cùng (Product, Warehouse) khác giá → set giá → AverageCost = trung bình cộng có trọng số
   - **Xóa phiếu:**
     - Thử xóa phiếu đã sinh tồn → exception `GoodsReceiptHasStockMovementsException` (key VI hiển thị đúng)
   - **Vendor + sinh công nợ tự động (mới):**
     - Tạo phiếu KHÔNG vendor + items có UnitCost đầy đủ → KHÔNG sinh nợ
     - Tạo phiếu CÓ vendor + items có UnitCost đầy đủ → sinh 1 phiếu `VendorDebt` với `GoodsReceiptId = phiếu.Id`, `TotalAmount = Σ(qty × cost)`
     - Tạo phiếu CÓ vendor nhưng items chưa định giá → KHÔNG sinh nợ; sau khi set UnitCost cho item cuối → sinh nợ
     - Phiếu CÓ items định giá đầy đủ nhưng KHÔNG vendor → POST `/GoodsReceipt/SetVendor` với `vendorId` → sinh nợ
     - Gọi POST `/GoodsReceipt/SetVendor` 2 lần liên tiếp với cùng vendor → CHỈ 1 phiếu nợ (idempotency)
3. **Sau cùng** — Module **System - Event Refactor** (Khó + HIGH, ~1 tuần) — Phase 1 Foundation + Phase 2 Migrate Orders + DeliveryNotes đã hoàn tất (2026-04-27, scheduled task), tiếp theo Phase 3 migrate các module còn lại (Catalog, Inventory, PurchaseOrders, GoodsReceipts, Debts, Customers, Media)

---

### 📝 Session 2026-04-27 (scheduled task) — Event Refactor Phase 1 Foundation

**Files mới tạo:**
- `Domain/NamEcommerce.Domain.Shared/Events/IDomainEvent.cs` — marker interface kế thừa `MediatR.INotification` với `EventId` + `OccurredOnUtc`
- `Domain/NamEcommerce.Domain.Shared/Events/DomainEvent.cs` — `abstract record DomainEvent : IDomainEvent`, constructor tự sinh `EventId = Guid.NewGuid()` + `OccurredOnUtc = DateTime.UtcNow`
- `Infrastructure/NamEcommerce.Data.SqlServer/Interceptors/DomainEventDispatchInterceptor.cs` — `SaveChangesInterceptor` quét `ChangeTracker.Entries<AppAggregateEntity>()`, clear events trước khi publish (tránh re-publish nếu handler gây nested SaveChanges), skip nếu `eventData.Context == null`. Override cả `SavedChangesAsync` và `SavedChanges` (sync block-on-async cho parity).
- `Tests/NamEcommerce.Data.SqlServer.Test/Interceptors/DomainEventDispatchInterceptorTests.cs` — 6 tests TDD: ctor null-check, single event publish + clear, multiple events in-order, aggregate không có event skip publish (`MockBehavior.Strict`), không aggregate tracked skip publish, save lần 2 không re-publish event đã dispatch. Dùng `FakeDomainEvent` + `FakeAggregate` (expose `Raise(...)` public) + `FakeDbContext` in-memory.

**Files đã sửa:**
- `Domain/NamEcommerce.Domain.Shared/NamEcommerce.Domain.Shared.csproj` — thêm `MediatR.Contracts` 2.0.1 (chỉ cần `INotification` interface, không cần full MediatR)
- `Domain/NamEcommerce.Domain.Shared/AppAggregateEntity.cs` — thêm `private readonly List<IDomainEvent> _domainEvents` + `[NotMapped] public IReadOnlyCollection<IDomainEvent> DomainEvents` + `protected void RaiseDomainEvent(IDomainEvent)` + `public void ClearDomainEvents()`
- `Infrastructure/NamEcommerce.Data.SqlServer/NamEcommerce.Data.SqlServer.csproj` — thêm `MediatR` 14.1.0 (cần full `IPublisher`)
- `Presentation/NamEcommerce.Web/Program.cs` — `using NamEcommerce.Data.SqlServer.Interceptors`; thêm `services.AddScoped<DomainEventDispatchInterceptor>()`; chuyển `AddDbContext<NamEcommerceEfDbContext>(opts =>)` sang `(sp, opts) =>` để gọi `opts.AddInterceptors(sp.GetRequiredService<DomainEventDispatchInterceptor>())`
- `Tests/NamEcommerce.Data.SqlServer.Test/NamEcommerce.Data.SqlServer.Test.csproj` — thêm `MediatR` 14.1.0 + `Microsoft.EntityFrameworkCore.InMemory` 10.0.5 + `Moq` 4.18.1

**Pattern decision:**
- Aggregate raise event qua `protected RaiseDomainEvent(...)` (chỉ subclass gọi được — không leak ra Manager/AppService)
- Interceptor clear events TRƯỚC khi publish — defensive cho trường hợp handler gây nested SaveChanges (tránh stack overflow / re-publish)
- `IEventPublisher` cũ giữ nguyên để Phase 2/3 migrate dần từng module — chưa break Manager nào hiện tại

⚠️ **Tuấn cần làm sau khi merge session 2026-04-27 (Phase 1 + Phase 2):**
- Restore packages: `dotnet restore NamEcommerce.sln` (3 csproj có thay đổi package reference: `Domain.Shared`, `Data.SqlServer`, `Data.SqlServer.Test`)
- Build verify: `dotnet build NamEcommerce.sln` — đặc biệt verify `Microsoft.EntityFrameworkCore.InMemory` 10.0.5 download được; nếu không có thì hạ xuống version EF Core compatible
- Run unit tests:
  - `dotnet test Tests/NamEcommerce.Data.SqlServer.Test/` — 6 tests mới cho `DomainEventDispatchInterceptor`
  - `dotnet test Tests/NamEcommerce.Domain.Services.Test/` — `OrderManagerTests` đã đổi 47 constructor calls (7→6 args) sau khi bỏ `IEventPublisher` dependency
- Smoke test app start + flows nghiệp vụ:
  - App start lên + SaveChanges hoạt động bình thường với interceptor mới đăng ký
  - **Order flow:** Tạo Order với items + discount → kiểm tra log/debug `DomainEvents` collection raise đúng `OrderPlaced` (1 event duy nhất sau khi `ClearDomainEvents()` clear các `OrderItemAdded` lúc setup)
  - **DeliveryNote confirmed flow:** Confirm phiếu xuất → n8n nhận notification (`DeliveryNoteConfirmedEventHandler` đã refactor handle `DeliveryNoteConfirmed` record mới)
  - **DeliveryNote delivered flow:** Mark delivered → công nợ khách hàng được sinh tự động (`DeliveryNoteDeliveredEventHandler` đã refactor handle `DeliveryNoteDelivered` record mới + tận dụng payload event)

---

### 📝 Session 2026-04-28 (scheduled task) — Event Refactor Phase 3: Migrate Catalog + Customers + Inventory.Warehouse

**Modules đã migrate trong session này:**
- ✅ `UnitMeasurement` (Catalog)
- ✅ `Category` (Catalog)
- ✅ `Vendor` (Catalog)
- ✅ `Customer` (Customers)
- ✅ `Warehouse` (Inventory)
- ✅ `Picture` (Media) — chỉ events base `PictureCreated`/`PictureDeleted`; `PictureOrphaned` sẽ làm khi migrate Product/GoodsReceipt

**Pattern chung áp dụng cho mọi module:**
1. Tạo `{Module}Events.cs` trong `Domain.Shared/Events/{Module}/` — 3 sealed records: `{Entity}Created`, `{Entity}Updated`, `{Entity}Deleted` (kế thừa `DomainEvent`)
2. Aggregate entity thêm 3 internal method: `MarkCreated()`, `MarkUpdated()`, `MarkDeleted()` raise event tương ứng + `using NamEcommerce.Domain.Shared.Events.{Module};`
3. Manager bỏ `IEventPublisher` khỏi constructor (3 deps → 2 deps, hoặc 4 → 3 nếu có dep khác); thay `_eventPublisher.EntityCreated/Updated/Deleted(...)` bằng `entity.MarkCreated/Updated/Deleted()` gọi TRƯỚC `Insert/Update/DeleteAsync`. Bỏ `using NamEcommerce.Domain.Shared.Events;`
4. Test file: replace_all constructor calls (giảm 1 arg), replace_all `, Mock.Of<IEventPublisher>()` → `)`, đổi `using` sang sub-namespace cụ thể, thêm assertion `DomainEvents.OfType<TEvent>().Any(...)` cho test create/update/delete success

**Files mới tạo (5):**
- `Domain.Shared/Events/Catalog/CategoryEvents.cs` — `CategoryCreated`, `CategoryUpdated`, `CategoryParentChanged`, `CategoryDeleted`
- `Domain.Shared/Events/Catalog/VendorEvents.cs` — `VendorCreated`, `VendorUpdated`, `VendorDeleted`
- `Domain.Shared/Events/Customers/CustomerEvents.cs` — `CustomerCreated`, `CustomerUpdated`, `CustomerDeleted`
- `Domain.Shared/Events/Inventory/WarehouseEvents.cs` — `WarehouseCreated`, `WarehouseUpdated`, `WarehouseDeleted`
- (`Domain.Shared/Events/Catalog/UnitMeasurementEvents.cs` đã có từ 2026-04-27)

**Files đã sửa entity (5):** `UnitMeasurement`, `Category`, `Vendor`, `Customer`, `Warehouse` — thêm 3-4 method `Mark*` + import events namespace tương ứng.

**Files đã sửa manager (5):**
- `UnitMeasurementManager` — 3 → 2 deps
- `CategoryManager` — 3 → 2 deps. `SetParentCategoryAsync` raise `CategoryParentChanged` thay cho `EntityUpdated`. `DeleteCategoryAsync` raise `CategoryDeleted` rồi update children với `MarkParentChanged()`.
- `VendorManager` — 3 → 2 deps
- `CustomerManager` — không thay constructor (vốn không inject `IEventPublisher`), chỉ thêm gọi `Mark*` trên entity
- `WarehouseManager` — 3 → 2 deps

**Files đã sửa test (4 — `Customer` không có test sẵn):**
- `UnitMeasurementManagerTests.cs` — 18 ctor → 2-args, 3 DomainEvents assertions
- `CategoryManagerTests.cs` — 21 ctor → 2-args, 3 DomainEvents assertions
- `VendorManagerTests.cs` — 18 ctor → 2-args, 3 DomainEvents assertions
- `WarehouseManagerTests.cs` — 23 ctor → 2-args, 3 DomainEvents assertions

**Pattern decisions:**
- Mỗi module CRUD thuần dùng pattern `MarkCreated/MarkUpdated/MarkDeleted` đơn giản — không cần method `Place()` hay state machine như `Order`/`DeliveryNote`.
- `Category` có thêm event `CategoryParentChanged` riêng cho flow `SetParentCategoryAsync` và auto-update children sau khi xoá parent (kéo thả trên cây khác về ngữ nghĩa với general update).
- Tất cả events raise TRƯỚC `Insert/Update/DeleteAsync` — `DomainEventDispatchInterceptor` publish sau khi `SaveChanges` thành công.
- KHÔNG có domain event handler nào cho 5 module này (đã grep verify) → không cần migrate handler.

⚠️ **Tuấn cần làm sau merge session 2026-04-28:**
- Build verify: `dotnet build NamEcommerce.sln` — đảm bảo 5 manager constructor đổi không vỡ DI registration (DI vẫn dùng open generic `IRepository<>`/`IEntityDataReader<>`).
- Run unit tests: `dotnet test Tests/NamEcommerce.Domain.Services.Test/` — đặc biệt 4 file test bị thay đổi (UnitMeasurement/Category/Vendor/Warehouse).
- Smoke test: tạo/sửa/xoá entity của 5 module qua UI → verify SaveChanges + dispatcher publish event không lỗi nested transaction.

---

### 📝 Session 2026-04-27 (scheduled task) — Event Refactor Phase 2 Migrate Orders + DeliveryNotes

**Files mới tạo:**
- `Domain/NamEcommerce.Domain.Shared/Events/Orders/OrderEvents.cs` — 9 sealed records `: DomainEvent` (OrderPlaced/OrderInfoUpdated/OrderItemAdded/OrderItemUpdated/OrderItemRemoved/OrderLocked/OrderShippingUpdated/OrderItemDelivered/OrderDeleted)
- `Domain/NamEcommerce.Domain.Shared/Events/DeliveryNotes/DeliveryNoteEvents.cs` — 5 sealed records `: DomainEvent` (DeliveryNoteCreated/Confirmed/Delivering/Delivered/Cancelled). Lưu ý: `DeliveryNoteConfirmed`/`DeliveryNoteDelivered` (record mới) khác tên với `DeliveryNoteConfirmedEvent`/`DeliveryNoteDeliveredEvent` (class cũ kế thừa `BaseEvent`) — cùng namespace, không trùng tên.

**Files đã sửa:**
- `Domain/NamEcommerce.Domain/Entities/Orders/Order.cs` — thêm 4 method `Place()`, `MarkInfoUpdated()`, `MarkShippingUpdated()`, `MarkDeleted()` để Manager gọi tại các điểm cần raise event không thuộc business method có sẵn. Raise event in-place trong `AddOrderItemAsync`/`UpdateOrderItem`/`RemoveOrderItem`/`LockOrder`/`MarkOrderItemDelivered` — sau khi state đã thay đổi.
- `Domain/NamEcommerce.Domain/Entities/DeliveryNotes/DeliveryNote.cs` — thêm `MarkCreated()` cho flow create; raise event trong `Confirm()`/`MarkDelivering()`/`MarkDelivered()`/`Cancel()`. `Cancel()` capture `wasReservingStock` (bool) trước khi đổi status để event payload có đủ context cho handler quyết định release stock.
- `Domain/NamEcommerce.Domain.Services/Orders/OrderManager.cs` — bỏ `IEventPublisher eventPublisher` khỏi constructor (giảm 7 → 6 deps). Trong `CreateOrderAsync` gọi `order.ClearDomainEvents()` TRƯỚC `order.Place()` — vì `AddOrderItemAsync` đã raise `OrderItemAdded` events lúc setup, nhưng phiếu chưa thật sự "placed" → clear hết để chỉ còn `OrderPlaced` đại diện lifecycle bắt đầu. Bỏ tất cả call `eventPublisher.EntityCreated/Updated/Deleted` (9 chỗ).
- `Domain/NamEcommerce.Domain.Services/DeliveryNotes/DeliveryNoteManager.cs` — bỏ `IEventPublisher eventPublisher` khỏi constructor. Manager gọi `deliveryNote.MarkCreated()` trong `CreateFromOrderAsync` trước khi insert. `Confirm()`/`MarkDelivering()`/`MarkDelivered()`/`Cancel()` đều tự raise event qua aggregate — Manager chỉ orchestrate.
- `Application/NamEcommerce.Application.Services/Events/DeliveryNotes/DeliveryNoteConfirmedEventHandler.cs` — chuyển từ `INotificationHandler<DeliveryNoteConfirmedNotification>` (class trong Application.Contracts/Events) → `INotificationHandler<DeliveryNoteConfirmed>` (record DomainEvent). Logic notify n8n giữ nguyên.
- `Application/NamEcommerce.Application.Services/Events/DeliveryNotes/DeliveryNoteDeliveredEventHandler.cs` — chuyển sang `INotificationHandler<DeliveryNoteDelivered>`. Tận dụng payload event (`OrderId/CustomerId/TotalAmount`) cho `CreateCustomerDebtDto` — vẫn fetch `deliveryNote` để lấy `CreatedByUserId` (audit).
- `Tests/NamEcommerce.Domain.Services.Test/Services/OrderManagerTests.cs` — replace_all 8 lần `Mock.Of<IEventPublisher>(), ` → empty; thay 47 OrderManager constructor calls từ 7-args (có IEventPublisher position 5) → 6-args; thêm assertion `o.DomainEvents.OfType<OrderPlaced>().Any(...) && o.DomainEvents.Count == 1` cho `CreateOrderAsync_ValidDto_*`. Thay `using NamEcommerce.Domain.Shared.Events;` → `using NamEcommerce.Domain.Shared.Events.Orders;`.

**Pattern decisions:**
- Không raise event trong constructor — vì lúc đó OrderTotal/CustomerId chưa biết. Thêm method `Place()` để Manager gọi sau khi setup xong (kiểu factory style).
- Trong `CreateOrderAsync`, `AddOrderItemAsync` (gọi từ trong setup) raise `OrderItemAdded` events — nhưng lifecycle đơn chưa "place". Manager `ClearDomainEvents()` trước `Place()` để chỉ event `OrderPlaced` cuối cùng được dispatch. Quyết định này tránh việc handler reserve stock chạy trước khi order được commit → tránh inconsistency.
- Notification cũ (`DeliveryNoteConfirmedNotification`, `DeliveryNoteDeliveredNotification`) trong `Application.Contracts/Events/DeliveryNotes/` chưa xoá — Phase 5 cleanup. Hiện không có publisher gọi → không impact.
- `EventPublisher.cs` (switch + NotSupportedException) chưa xoá — vẫn được Manager khác dùng (User/PurchaseOrder/Picture/Warehouse/GoodsReceipt/VendorDebt/Vendor/UnitMeasurement/Product/Category). Phase 3 sẽ migrate dần.
- `OrderCreatedEventHandler` + `OrderUpdatedEventHandler` (đang trống) chưa xoá — Manager Order hiện không publish `EntityCreatedNotification<Order>`/`EntityUpdatedNotification<Order>` nữa, 2 handler đó sẽ KHÔNG bao giờ trigger → an toàn để giữ tới Phase 5.

⚠️ **Lưu ý record equality cho Tuấn:**
`AppAggregateEntity` giờ có private field `_domainEvents`. Record's synthesized `Equals` sẽ so sánh field này (List → reference equality). 2 instance có cùng Id + state nhưng `_domainEvents` khác reference → KHÔNG equal nữa. Existing tests dùng cùng reference cho 2 vế của `Assert.Equal` → vẫn pass. Nếu future test tạo 2 instance riêng và expect equal → cần override Equals (chưa làm vì chưa cần).

**Files đã chạm session 2026-04-26:**
- `Presentation/NamEcommerce.Web/Resources/SharedResource.vi-VN.resx` — thêm key `Error.GoodsReceipt.CannotDeleteHasStockMovements` + `Error.StockAverageCostCannotBeNegative` (VI)
- `Presentation/NamEcommerce.Web/Resources/SharedResource.resx` — thêm 2 keys tương ứng (EN)
- `Domain/NamEcommerce.Domain/Entities/Inventory/InventoryStock.cs` — thêm property `AverageCost decimal` (default 0, internal set)
- `Infrastructure/NamEcommerce.Data.SqlServer/Mappings/InventoryStockMapping.cs` — thêm `decimal(18,2)` mapping cho `AverageCost`
- `Domain/NamEcommerce.Domain.Shared/Services/Inventory/IInventoryStockManager.cs` — thêm signature `UpdateAverageCostAsync`
- `Domain/NamEcommerce.Domain.Services/Inventory/InventoryStockManager.cs` — implement `UpdateAverageCostAsync` (validate negative, throw StockNotFound, idempotent skip nếu không đổi)
- `Tests/NamEcommerce.Domain.Services.Test/Helpers/InventoryStockDataReader.cs` — file mới (test helper)
- `Tests/NamEcommerce.Domain.Services.Test/Helpers/InventoryStockRepository.cs` — file mới (test helper)
- `Tests/NamEcommerce.Domain.Services.Test/Services/InventoryStockManagerTests.cs` — file mới, 5 test cases TDD cho `UpdateAverageCostAsync`
- `Domain/NamEcommerce.Domain.Services/GoodsReceipts/GoodsReceiptManager.cs` — `SetGoodsReceiptItemUnitCostAsync` pass `item.Id` qua `AdditionalData` của `EntityUpdated`
- `Tests/NamEcommerce.Domain.Services.Test/Services/GoodsReceiptManagerTests.cs` — thêm 1 test verify AdditionalData == itemId + import `Events.Entities` namespace
- `Application/NamEcommerce.Application.Services/Events/GoodsReceipts/GoodsReceiptUpdatedHandler.cs` — refactor toàn bộ. Bỏ deps `IRepository<Picture>`/`IEntityDataReader<Picture>` (logic cũ đã comment-out). Inject `IInventoryStockManager` + `IEntityDataReader<GoodsReceipt>`. Khi `AdditionalData is Guid itemId` → tính `newAvg = Σ(qty × cost) / Σ(qty)` từ tất cả `GoodsReceiptItem` cùng `(ProductId, WarehouseId)` đã có `UnitCost` rồi gọi `UpdateAverageCostAsync`. Defensive: skip nếu Entity null, item null, WarehouseId null, UnitCost null hoặc totalQuantity <= 0 (tránh chia 0)
- `Presentation/NamEcommerce.Web/wwwroot/modules/order.details.js` — thay tất cả `fetch(...)` bằng `apiPost(...)` từ `ajax-helper.js`. Thêm helper `submitFormAsync(form)` ở module scope; dùng `FormData` cho request POST (server vẫn nhận form-binding default, không phá `[FromBody]`-less controllers). `RemoveOrderItem` chuyển sang FormData (đã verify controller không có `[FromBody]`). `CreateFromPreparation` giữ JSON object (controller có `[FromBody]`)
- `Presentation/NamEcommerce.Web/wwwroot/modules/CreatePurchaseOrderController.js` — import `apiGet`, thay `fetch('/PurchaseOrder/RecentPurchasePrices...')` bằng `apiGet(...)`. Endpoint trả mảng thuần → đọc qua `result.data ?? result` (tương thích cả khi shape có/không có `success`)
- `Presentation/NamEcommerce.Web/wwwroot/modules/OrderController.js` — import `apiGet`, thay `fetch('/Product/PriceHistory...')` bằng `apiGet(...)`. Đọc payload qua `result.data ?? result`

**Vendor + Sinh công nợ tự động (Phase 2 + Phase 3 backend) — 2026-04-26:**
- `Application/NamEcommerce.Application.Services/Events/GoodsReceipts/GoodsReceiptUpdatedHandler.cs` — refactor lần 2: tách `RecalculateAverageCostAsync` + `TryCreateVendorDebtAsync` thành private helpers. `switch` trên `AdditionalData`: `Guid itemId` → recalc AverageCost + try sinh công nợ; `"vendor-updated"` → chỉ try sinh công nợ. Inject thêm `IVendorDebtManager`. Idempotency của `CreateDebtFromGoodsReceiptAsync` đảm bảo gọi nhiều lần chỉ tạo 1 phiếu nợ.
- `Application/NamEcommerce.Application.Services/Events/GoodsReceipts/GoodsReceiptCreatedHandler.cs` — mở rộng: sau cộng tồn kho, gọi `TryCreateVendorDebtAsync` để xử lý edge case phiếu tạo với đủ `VendorId` + `UnitCost` cho mọi item ngay từ đầu (`AddGoodsReceiptItemDto` cho phép set UnitCost lúc tạo). Inject thêm `IVendorDebtManager`.
- `Domain/NamEcommerce.Domain.Shared/Services/Debts/IVendorDebtManager.cs` — thêm signature `GetDebtByGoodsReceiptIdAsync(Guid)` trả `VendorDebtDto?`. Idempotency của `CreateDebtFromGoodsReceiptAsync` đảm bảo có tối đa 1 debt / 1 phiếu nhập.
- `Domain/NamEcommerce.Domain.Services/Debts/VendorDebtManager.cs` — implement `GetDebtByGoodsReceiptIdAsync`: filter `DataSource.FirstOrDefault(d => d.GoodsReceiptId == goodsReceiptId)` + load payments theo `debt.Id`.
- `Application/NamEcommerce.Application.Contracts/Debts/IVendorDebtAppService.cs` — thêm signature `GetDebtByGoodsReceiptIdAsync(Guid)` trả `VendorDebtAppDto?`.
- `Application/NamEcommerce.Application.Services/Debts/VendorDebtAppService.cs` — implement: gọi `_debtManager.GetDebtByGoodsReceiptIdAsync` rồi `.ToDto()`.
- `Presentation/NamEcommerce.Web.Contracts/Models/GoodsReceipts/GoodsReceiptModel.cs` — thêm 3 properties cho UI badge: `HasVendorDebt bool`, `VendorDebtId Guid?`, `VendorDebtTotalAmount decimal?`.
- `Presentation/NamEcommerce.Web.Framework/Queries/Handlers/GoodsReceipts/GetGoodsReceiptHandler.cs` — inject `IVendorDebtAppService`. Sau khi load `goodsReceipt`, gọi `GetDebtByGoodsReceiptIdAsync(request.Id)` để map `HasVendorDebt`/`VendorDebtId`/`VendorDebtTotalAmount`.
- `Tests/NamEcommerce.Domain.Services.Test/Services/VendorDebtManagerTests.cs` — fix pre-existing compile bug: thêm `Mock<IEntityDataReader<GoodsReceipt>>? goodsReceiptReader = null` vào helper `CreateManager` (constructor cần 8 params, helper trước đó passing 7). Thêm 3 unit tests TDD cho `GetDebtByGoodsReceiptIdAsync`: NotSinhYet → null; DebtExistsForGoodsReceipt → DTO + Payments; OtherGoodsReceiptHasDebt → null cho ID không liên quan.

**Vendor + Sinh công nợ tự động (Phase 3 UI Views) — 2026-04-26:**
- `Presentation/NamEcommerce.Web/Models/GoodsReceipts/CreateGoodsReceiptModel.cs` — thêm `VendorId Guid?` + 3 display fields (`VendorDisplayName/Phone/Address`) marked `[ValidateNever]` để re-render khi ModelState fail.
- `Presentation/NamEcommerce.Web.Contracts/Commands/Models/GoodsReceipts/CreateGoodsReceiptCommand.cs` — thêm `VendorId Guid?`.
- `Presentation/NamEcommerce.Web.Framework/Commands/Handlers/GoodsReceipts/CreateGoodsReceiptHandler.cs` — pass `VendorId` xuống `CreateGoodsReceiptAppDto` (AppDto đã có sẵn field này).
- `Presentation/NamEcommerce.Web/Controllers/GoodsReceiptController.cs` — pass `model.VendorId` vào `CreateGoodsReceiptCommand`.
- `Presentation/NamEcommerce.Web/Models/GoodsReceipts/GoodsReceiptDetailsModel.cs` — thêm 4 vendor snapshot fields + 3 vendor debt fields (`HasVendorDebt`, `VendorDebtId`, `VendorDebtTotalAmount`).
- `Presentation/NamEcommerce.Web/Services/GoodsReceipts/GoodsReceiptModelFactory.cs` — `PrepareGoodsReceiptDetailsModel` map vendor + debt fields từ `GoodsReceiptModel`. `PrepareCreateGoodsReceiptModel` re-query vendor info qua `GetVendorQuery` khi `model.VendorId.HasValue` mà display fields trống (round-trip fail từ form submit).
- `Presentation/NamEcommerce.Web/Views/GoodsReceipt/Create.cshtml` — thêm section "Nhà cung cấp" trong cột thông tin phiếu (giữa WarehouseId và TruckDriverName). Hidden `VendorId` + `<div id="vendorPicker">` data-* attributes pre-populated. Script khởi tạo `VendorPicker` từ `/modules/VendorPicker.js`, wire `select`/`remove` event vào hidden input. Initial vendor render bằng plain object (bypass Vendor constructor — pre-existing constructor không destructure `address`).
- `Presentation/NamEcommerce.Web/Views/GoodsReceipt/Details.cshtml` — thêm card "Nhà cung cấp" giữa "Thông tin phiếu" và "Ảnh chụp": hiển thị tên/SĐT/địa chỉ + badge "Đã ghi nợ [TotalAmount]" link sang `VendorDebt/Details?vendorId=...` hoặc badge "Sẽ ghi sau khi định giá xong". Modal `#editVendorModal` chứa VendorPicker + nút Lưu submit AJAX qua `apiPost('/GoodsReceipt/SetVendor', FormData{goodsReceiptId, vendorId?})`. Toast success rồi `location.reload()` để badge cập nhật. No-op khi vendor không đổi.

⚠️ **Tuấn cần chạy thủ công sau khi merge session 2026-04-26:**
- `Add-Migration AddAverageCostToInventoryStock` (project Data.SqlServer)
- `Add-Migration AddVendorToGoodsReceiptAndDebt` — schema cho 4 cột vendor trên `GoodsReceipt` + đổi `PurchaseOrderId/Code` của `VendorDebt` thành nullable + thêm `GoodsReceiptId` (Phase 1 Vendor)
- `Update-Database`
- `dotnet build NamEcommerce.sln` để verify toàn bộ compile sạch (sandbox không có dotnet). Đặc biệt verify `VendorDebtManagerTests.cs` (đã fix pre-existing helper bug 7→8 params) compile thành công.

**Files đã chạm session trước (chưa build verify):**
- `Domain/Entities/Inventory/StockMovementLog.cs` — thêm `GoodsReceipt = 6`
- `Domain.Shared/Exceptions/GoodsReceipts/GoodsReceiptHasStockMovementsException.cs` — file mới
- `Domain.Services/GoodsReceipts/GoodsReceiptManager.cs` — inject `IEntityDataReader<StockMovementLog>` + check trong Delete
- `Application.Services/Events/GoodsReceipts/GoodsReceiptCreatedHandler.cs` — file mới

---

### [PRIORITY: HIGH] Thêm nhà cung cấp (Vendor) vào phiếu nhập + sinh công nợ tự động

**Mục tiêu:**
- `GoodsReceipt` có thể gắn 1 nhà cung cấp (optional, cập nhật được sau).
- Khi toàn bộ items được định giá (`IsPendingCosting() == false`) **VÀ** đã có VendorId → tự động tạo `VendorDebt` với tổng tiền = Σ(Quantity × UnitCost).
- Idempotent: gọi lại nhiều lần không tạo thêm phiếu nợ trùng.

---

#### Phase 1 — Domain: Mở rộng GoodsReceipt & VendorDebt

**`GoodsReceipt` entity** (`Domain/Entities/GoodsReceipts/GoodsReceipt.cs`): ✅ DONE
- [x] Thêm 4 properties (tất cả nullable, snapshot thông tin NCC tại thời điểm nhập):
  ```csharp
  public Guid?   VendorId      { get; internal set; }
  public string? VendorName    { get; internal set; }
  public string? VendorPhone   { get; internal set; }
  public string? VendorAddress { get; internal set; }
  ```
- [x] Thêm method `internal void SetVendor(Guid vendorId, string vendorName, string? phone, string? address)`
- [x] Thêm method `internal void ClearVendor()` (xoá vendor nếu cần huỷ liên kết)

**`GoodsReceiptDtos`** (`Domain.Shared/Dtos/GoodsReceipts/GoodsReceiptDtos.cs`): ✅ DONE
- [x] Thêm `VendorId?`, `VendorName?`, `VendorPhone?`, `VendorAddress?` vào `BaseGoodsReceiptDto`
- [x] Thêm record `SetGoodsReceiptVendorDto(Guid GoodsReceiptId)` với `VendorId?` (null = xoá vendor)

**`IGoodsReceiptManager`** + `GoodsReceiptManager`: ✅ DONE
- [x] Cập nhật `CreateGoodsReceiptAsync` / `UpdateGoodsReceiptAsync` để set vendor khi có trong dto
  - Cần inject `IEntityDataReader<Vendor>` để validate vendor tồn tại trước khi set
- [x] Thêm method `SetGoodsReceiptVendorAsync(SetGoodsReceiptVendorDto dto)`:
  - Nếu `dto.VendorId` có giá trị: get Vendor by Id → throw nếu không tồn tại → gọi `goodsReceipt.SetVendor(...)`
  - Nếu `dto.VendorId == null`: gọi `goodsReceipt.ClearVendor()`
  - Publish `EntityUpdated(goodsReceipt, additionalData: "vendor-updated")` để handler nhận biết

**`VendorDebt` entity** (`Domain/Entities/Debts/VendorDebt.cs`) — **schema breaking change**: ✅ DONE
- [x] Đổi `public Guid PurchaseOrderId { get; private set; }` → `public Guid? PurchaseOrderId { get; private set; }`
- [x] Đổi `public string PurchaseOrderCode { get; private set; }` → `public string? PurchaseOrderCode { get; private set; }`
- [x] Thêm `public Guid? GoodsReceiptId { get; private set; }`
- [x] Thêm internal constructor mới cho GoodsReceipt-based debt:
  ```csharp
  internal VendorDebt(string code, Guid vendorId, string vendorName,
      Guid goodsReceiptId, decimal totalAmount, DateTime? dueDateUtc, Guid? createdByUserId)
  ```
  → Set `GoodsReceiptId = goodsReceiptId`, `PurchaseOrderId = null`, `PurchaseOrderCode = null`
- [x] Giữ constructor PurchaseOrder cũ **nguyên vẹn** — chỉ thêm constructor mới

**`IVendorDebtManager`** + `VendorDebtManager`: ✅ DONE
- [x] Thêm `CreateDebtFromGoodsReceiptAsync(CreateVendorDebtFromGoodsReceiptDto dto)` — idempotent:
  - Check tồn tại `VendorDebt` có `GoodsReceiptId == dto.GoodsReceiptId` → return existing nếu đã có
  - Nếu chưa → tạo mới với constructor GoodsReceipt
- [x] Thêm `CreateVendorDebtFromGoodsReceiptDto`:
  ```csharp
  public record CreateVendorDebtFromGoodsReceiptDto {
      public required Guid GoodsReceiptId  { get; init; }
      public required Guid VendorId        { get; init; }
      public required string VendorName    { get; init; }
      public string? VendorPhone           { get; init; }
      public string? VendorAddress         { get; init; }
      public required decimal TotalAmount  { get; init; }
      public DateTime? DueDateUtc          { get; init; }
      public Guid? CreatedByUserId         { get; init; }
  }
  ```

> ⚠️ **Migration cần thiết:**
> - `GoodsReceipt` table: thêm 4 columns `VendorId`, `VendorName`, `VendorPhone`, `VendorAddress` (nullable)
> - `VendorDebt` table: đổi `PurchaseOrderId` + `PurchaseOrderCode` thành nullable, thêm column `GoodsReceiptId` (nullable FK)
> - Tuấn chạy `Add-Migration AddVendorToGoodsReceiptAndDebt` sau khi merge phase này

---

#### Phase 2 — Application: Tự động sinh công nợ khi định giá xong ✅ DONE 2026-04-26

Mở rộng `GoodsReceiptUpdatedHandler` (đã có logic AverageCost):

- [x] Sau bước tính AverageCost, kiểm tra thêm điều kiện sinh công nợ:
  ```
  AdditionalData is Guid itemId           → là lần set UnitCost
  !goodsReceipt.IsPendingCosting()        → tất cả items đã có giá
  goodsReceipt.VendorId.HasValue          → đã gắn nhà cung cấp
  ```
  Nếu thoả tất cả → gọi `IVendorDebtManager.CreateDebtFromGoodsReceiptAsync(...)`:
  - `TotalAmount = goodsReceipt.Items.Sum(i => i.Quantity * i.UnitCost!.Value)`
  - `VendorId`, `VendorName`, `VendorPhone`, `VendorAddress` lấy từ `goodsReceipt`

- [x] Thêm case xử lý khi `AdditionalData is "vendor-updated"` (từ `SetGoodsReceiptVendorAsync`):
  - Kiểm tra `!goodsReceipt.IsPendingCosting()` → nếu đã đủ giá → cũng trigger sinh công nợ
  - Cho phép trường hợp: nhập trước, định giá xong, sau đó mới chọn vendor → vẫn tạo được nợ
- [x] **Bonus:** mở rộng `GoodsReceiptCreatedHandler` để xử lý edge case: phiếu nhập tạo với đủ vendor + UnitCost cho mọi item ngay từ đầu → sinh nợ luôn (do `AddGoodsReceiptItemDto` cho phép set UnitCost lúc tạo)

> **Idempotency đảm bảo:** `CreateDebtFromGoodsReceiptAsync` check `GoodsReceiptId` trước khi tạo — gọi 2 lần chỉ sinh 1 phiếu nợ.

---

#### Phase 3 — Presentation: UI + API

**Endpoint mới trong `GoodsReceiptController`:** ✅ DONE
- [x] `POST /GoodsReceipt/SetVendor` — AJAX, nhận `{ id, vendorId? }`:
  - Gọi `SetGoodsReceiptVendorAsync`
  - Return `JsonOk()` / `JsonError()`

**`GET /Vendor/Options`** (kiểm tra xem đã có chưa): ✅ DONE
- [x] Đã có sẵn trong `VendorController.Options(string? q)` — trả `[{ value, label }]`

**Create page** (`Views/GoodsReceipt/Create.cshtml`): ✅ DONE 2026-04-26
- [x] Thêm section chọn vendor (optional) — dùng `VendorPicker.js` (search live qua `/Vendor/Search`)
- [x] Hidden input `VendorId` + hiển thị tên/SĐT/địa chỉ vendor đã chọn
- [x] Nút "Xoá nhà cung cấp" (clear) — built-in trong VendorPicker.js
- [x] Re-populate vendor display khi ModelState fail (server query lại Vendor info qua `GetVendorQuery`)

**Details page** (`Views/GoodsReceipt/Details.cshtml`): ✅ DONE 2026-04-26
- [x] Hiển thị card "Nhà cung cấp" riêng biệt:
  - Nếu chưa có vendor: text mờ "— Chưa có nhà cung cấp" + nút "Gắn NCC"
  - Nếu đã có vendor: tên + SĐT + địa chỉ + nút "Thay đổi"
- [x] Inline vendor picker (modal Bootstrap) → AJAX `POST /GoodsReceipt/SetVendor` qua `apiPost` (helper từ `ajax-helper.js`, tự xử lý antiforgery token)
- [x] Sau khi set vendor thành công: toast "Đã cập nhật nhà cung cấp" rồi `location.reload()` (cần reload để render badge "Đã ghi nợ" nếu trigger sinh nợ)
- [x] Nếu đã sinh công nợ: badge `"Đã ghi nợ [tổng tiền]"` link sang `VendorDebt/Details?vendorId=...` (không phải debtId — controller hiện liệt kê toàn bộ debts theo vendorId)
- [x] Nếu chưa sinh nhưng có vendor + còn item chưa định giá: badge "Sẽ ghi sau khi định giá xong" (UX hint)

**Cập nhật View Models + Handlers:** ✅ DONE 2026-04-26
- [x] Thêm `VendorId?`, `VendorName?`, `VendorPhone?`, `VendorAddress?` vào `GoodsReceiptModel` (Details). `GoodsReceiptListModel.ItemModel` chưa bổ sung — có thể làm lúc Views nếu cần.
- [x] Thêm `HasVendorDebt bool` + `VendorDebtId Guid?` + `VendorDebtTotalAmount decimal?` vào `GoodsReceiptModel` để hiển thị badge
- [x] Cập nhật `GetGoodsReceiptHandler` — inject `IVendorDebtAppService.GetDebtByGoodsReceiptIdAsync` để map vendor fields + check VendorDebt tham chiếu GoodsReceiptId

---

**Phụ thuộc & thứ tự thực hiện:**
1. Phase 1 (Domain) trước — Phase 2 + 3 phụ thuộc vào
2. `IVendorDebtManager.CreateDebtFromGoodsReceiptAsync` cần có trước khi Phase 2 có thể gọi
3. Migration phải chạy trước khi test end-to-end
4. Phase 3 UI có thể làm song song Phase 2 sau khi Phase 1 xong

**Files chính cần đụng:**
- `Domain/Entities/GoodsReceipts/GoodsReceipt.cs`
- `Domain.Shared/Dtos/GoodsReceipts/GoodsReceiptDtos.cs`
- `Domain.Shared/Services/GoodsReceipts/IGoodsReceiptManager.cs`
- `Domain.Services/GoodsReceipts/GoodsReceiptManager.cs`
- `Domain/Entities/Debts/VendorDebt.cs` ← **schema change, cần migration**
- `Domain.Shared/Dtos/Debts/VendorDebtDtos.cs` — thêm `CreateVendorDebtFromGoodsReceiptDto`
- `Domain.Shared/Services/Debts/IVendorDebtManager.cs`
- `Domain.Services/Debts/VendorDebtManager.cs`
- `Application.Services/Events/GoodsReceipts/GoodsReceiptUpdatedHandler.cs`
- `Data.SqlServer/Mappings/GoodsReceiptMapping.cs` + `VendorDebtMapping.cs`
- `Web/Controllers/GoodsReceiptController.cs` + `VendorController.cs`
- `Web.Contracts/Models/GoodsReceipts/GoodsReceiptModel.cs`
- `Web.Framework/Queries/Handlers/GoodsReceipts/GetGoodsReceiptHandler.cs`
- `Views/GoodsReceipt/Create.cshtml` + `Details.cshtml`

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

- [x] Tạo `IDomainEvent : INotification` trong `NamEcommerce.Domain.Shared/Events/` *(2026-04-27 — thêm package `MediatR.Contracts` 2.0.1 vào `Domain.Shared.csproj`)*
- [x] Tạo `abstract record DomainEvent : IDomainEvent` với `EventId`, `OccurredOnUtc` *(2026-04-27 — `EventId = Guid.NewGuid()`, `OccurredOnUtc = DateTime.UtcNow` trong constructor; `init` setters để concrete record có thể override khi cần test/replay)*
- [x] Cập nhật `AppAggregateEntity`: *(2026-04-27)*
  - [x] Thêm `private readonly List<IDomainEvent> _domainEvents`
  - [x] Expose `IReadOnlyCollection<IDomainEvent> DomainEvents` với `[NotMapped]`
  - [x] Method `protected void RaiseDomainEvent(IDomainEvent)` và `public void ClearDomainEvents()`
- [x] Viết `DomainEventDispatchInterceptor : SaveChangesInterceptor` trong `NamEcommerce.Data.SqlServer/Interceptors/` *(2026-04-27 — quét `ChangeTracker.Entries<AppAggregateEntity>()` trong `SavedChangesAsync`/`SavedChanges`. Clear events trên aggregate TRƯỚC khi publish để tránh re-publish nếu handler trigger `SaveChanges` lồng nhau. Defensive: skip nếu `eventData.Context == null`. Thêm package `MediatR` 14.1.0 vào `NamEcommerce.Data.SqlServer.csproj`)*
- [x] Đăng ký Interceptor trong `Program.cs` / `DbContext` configuration *(2026-04-27 — `services.AddScoped<DomainEventDispatchInterceptor>()` + chuyển `AddDbContext` sang overload `(sp, opts) =>` để resolve interceptor từ scope)*
- [x] Unit test cho Interceptor (mock `IPublisher`, verify dispatch + clear events) *(2026-04-27 — 6 test cases TDD trong `NamEcommerce.Data.SqlServer.Test/Interceptors/DomainEventDispatchInterceptorTests.cs` dùng `Microsoft.EntityFrameworkCore.InMemory` với `FakeDbContext` + `FakeAggregate` test fixture: constructor null-check, single event, multiple events in-order, no events skip publish, no aggregate tracked skip publish, second SaveChanges không re-publish. Thêm packages `MediatR` + `EFCore.InMemory` + `Moq` vào test csproj)*
- [ ] Giữ song song `IEventPublisher` cũ — không xoá ngay để tránh break code hiện tại

#### Phase 2 — Migrate Orders + DeliveryNotes (2-3 ngày)

- [x] Định nghĩa concrete events cho Orders: *(2026-04-27 — `Domain.Shared/Events/Orders/OrderEvents.cs` — 9 sealed records)*
  - [x] `OrderPlaced(OrderId, OrderCode, CustomerId, OrderTotal)` *(thiếu WarehouseId vì Order entity hiện không có field này — bỏ qua)*
  - [x] `OrderInfoUpdated(OrderId)` — flow `UpdateOrderAsync` (note/discount/expectedShipping)
  - [x] `OrderItemAdded(OrderId, OrderItemId, ProductId, Quantity, UnitPrice)`
  - [x] `OrderItemUpdated(OrderId, OrderItemId, Quantity, UnitPrice)`
  - [x] `OrderItemRemoved(OrderId, OrderItemId)`
  - [x] `OrderLocked(OrderId, Reason)` — cả manual lock + auto-lock khi tất cả items đã giao
  - [x] `OrderShippingUpdated(OrderId)` — flow `UpdateShippingAsync`
  - [x] `OrderItemDelivered(OrderId, OrderItemId, PictureId)` — flow `MarkOrderItemDeliveredAsync`
  - [x] `OrderDeleted(OrderId, OrderCode)` — flow `DeleteOrderAsync`
  - ~~`OrderCancelled`~~ — bỏ (Order hiện không có method Cancel, chỉ Lock/Delete)
- [x] Định nghĩa concrete events cho DeliveryNotes: *(2026-04-27 — `Domain.Shared/Events/DeliveryNotes/DeliveryNoteEvents.cs` — 5 sealed records, KHÔNG xoá `DeliveryNoteConfirmedEvent`/`DeliveryNoteDeliveredEvent` cũ — Phase 5 cleanup)*
  - [x] `DeliveryNoteCreated(DeliveryNoteId, OrderId, CustomerId)`
  - [x] `DeliveryNoteConfirmed(DeliveryNoteId)` — record mới (cũ là class kế thừa `BaseEvent`)
  - [x] `DeliveryNoteDelivering(DeliveryNoteId)` — bonus, raise từ `MarkDelivering()`
  - [x] `DeliveryNoteDelivered(DeliveryNoteId, OrderId, CustomerId, TotalAmount)` — payload đầy đủ để handler không cần fetch lại nếu không muốn
  - [x] `DeliveryNoteCancelled(DeliveryNoteId, WasReservingStock)` — `WasReservingStock` để handler quyết định có release stock hay không
- [x] Refactor `Order` aggregate: gọi `RaiseDomainEvent(...)` trong các method nghiệp vụ *(2026-04-27 — không raise trong constructor; thêm 4 method `Place()`, `MarkInfoUpdated()`, `MarkShippingUpdated()`, `MarkDeleted()` để Manager gọi sau khi setup xong; raise event trong các method có sẵn `AddOrderItemAsync`/`UpdateOrderItem`/`RemoveOrderItem`/`LockOrder`/`MarkOrderItemDelivered`)*
- [x] Refactor `DeliveryNote` aggregate tương tự *(2026-04-27 — thêm `MarkCreated()` cho flow create; raise event trong `Confirm()`, `MarkDelivering()`, `MarkDelivered()`, `Cancel()`)*
- [x] Xoá `eventPublisher.EntityCreated/Updated/Deleted(...)` trong `OrderManager` và `DeliveryNoteManager` *(2026-04-27 — bỏ tham số `IEventPublisher` khỏi constructor cả 2 manager; trong `CreateOrderAsync` gọi `order.ClearDomainEvents()` trước `Place()` để các event AddOrderItem raise lúc setup không bị double-publish — chỉ event `OrderPlaced` cuối cùng phản ánh lifecycle bắt đầu)*
- [x] Refactor handler hiện tại theo từng concrete event: *(2026-04-27)*
  - [x] `DeliveryNoteConfirmedEventHandler` — chuyển từ `INotificationHandler<DeliveryNoteConfirmedNotification>` → `INotificationHandler<DeliveryNoteConfirmed>`. Logic giữ nguyên (notify n8n).
  - [x] `DeliveryNoteDeliveredEventHandler` — chuyển sang `INotificationHandler<DeliveryNoteDelivered>`. Tận dụng payload event (`OrderId/CustomerId/TotalAmount`) — chỉ fetch lại để lấy `CreatedByUserId`.
  - [ ] `OrderPlacedReserveStockHandler` (thay cho `OrderCreatedEventHandler` đang trống) *(skip — code hiện chưa có ReserveStock logic, là TODO comment cũ. Để Phase 3 hoặc khi có warehouse trên Order)*
  - [ ] `OrderCancelledReleaseStockHandler` *(skip — chưa có concrete event `OrderCancelled`; aggregate Order không có method Cancel)*
- [x] Update unit test Manager: assert `aggregate.DomainEvents` chứa event mong đợi thay vì verify `eventPublisher.EntityCreated(...)` *(2026-04-27 — `OrderManagerTests.cs` đã update toàn bộ 47 OrderManager constructor calls từ 7-args (có IEventPublisher) → 6-args; thêm assertion `o.DomainEvents.OfType<OrderPlaced>().Any(...) && o.DomainEvents.Count == 1` cho `CreateOrderAsync_ValidDto_*`. KHÔNG có `DeliveryNoteManagerTests` hiện tại — không cần update)*
- [ ] Smoke test end-to-end: tạo order → confirm delivery note → mark delivered → verify công nợ tự động tạo *(cần sandbox dotnet để chạy, Tuấn xác nhận sau khi merge)*

#### Phase 3 — Migrate các module còn lại (2 ngày)

- [ ] Catalog: `Product`, ~~`Category`~~, ~~`Vendor`~~, ~~`UnitMeasurement`~~ *(2026-04-28 — `UnitMeasurement`/`Category`/`Vendor` ✅ DONE scheduled task; còn lại Product)*
- [ ] Inventory: `InventoryStock`, ~~`Warehouse`~~, stock movements *(2026-04-28 — `Warehouse` ✅ DONE scheduled task)*
- [ ] PurchaseOrders: `PurchaseOrder` (note: `PurchaseOrderUpdatedEventHandler` hiện đang gọi `VerifyStatusAsync` — cần thay bằng concrete event như `PurchaseOrderItemReceived`)
- [ ] GoodsReceipts: `GoodsReceipt` (dọn handler trống `GoodsReceiptUpdatedHandler`)
- [ ] Debts: `CustomerDebt`, `CustomerPayment`, `VendorDebt`
- [x] Customers: `Customer` *(2026-04-28 — ✅ DONE scheduled task; CustomerManager đã sẵn không có IEventPublisher từ trước, chỉ thêm 3 method Mark* + raise events)*
- [x] Media: `Picture` *(2026-04-28 — ✅ DONE base events `PictureCreated`/`PictureDeleted`. Note: `PictureOrphaned` event chưa thêm — sẽ làm khi migrate `Product`/`GoodsReceipt` để thay thế `EntityDeletedNotification` flow trong các handler liên quan)*

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
- [x] Migrate JS module sang `apiPost` helper (`order.details.js`, `CreatePurchaseOrderController.js`, `OrderController.js`) ✅ DONE 2026-04-26

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

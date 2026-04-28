# TodoList — VLXD Tuấn Khôi / NamEcommerce

> File theo dõi các hạng mục cần thực thi. Cập nhật trạng thái khi bắt đầu / hoàn thành.
> Các phần đã hoàn thành được lưu tại [CheckList.md](CheckList.md).

---

## ⚙️ Quy tắc làm việc với AI Assistant

> **Áp dụng cho mọi phiên làm việc.**

- **Branch làm việc: `dev-assistant`** — Mọi thay đổi code đều commit lên branch này, KHÔNG commit trực tiếp lên `main`.
- **Sau mỗi phase/feature hoàn thành**: AI commit lên `dev-assistant`, bạn tự `git push` và merge vào `main` khi kiểm tra xong.
- **Git commit**: AI có thể tạo branch, add, commit trong sandbox. Không thể push (cần credentials của bạn).
- **Lệnh bạn cần tự chạy sau mỗi session**:
  ```bash
  git push origin dev-assistant
  # Sau khi review xong thì merge vào main
  ```

---

## 🔧 Pending Actions — Build & Smoke Test

*(Sau session 2026-04-28)*

**1. Build verify** toàn bộ solution — `dotnet build NamEcommerce.sln` để chắc:
- DI cho `IEntityDataReader<StockMovementLog>` + `IEntityDataReader<GoodsReceipt>` + `IVendorDebtManager` không vỡ
- `VendorDebtManagerTests.cs` compile sạch sau khi fix helper 7→8 params
- Các thay đổi Notification + Vendor + Handler module compile sạch

**2. Migrations cần chạy thủ công:**
- `Add-Migration AddAverageCostToInventoryStock` (project Data.SqlServer)
- `Add-Migration AddVendorToGoodsReceiptAndDebt`
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
- DeliveryNote confirmed flow → n8n nhận notification
- DeliveryNote delivered flow → `CustomerDebt` sinh tự động
- Tạo phiếu nhập (GoodsReceipt) → định giá đủ + vendor → `VendorDebtCreated` event dispatch
- Trả hết nợ → `VendorDebtFullyPaid` event publish

---

## System - Event

**Cấp độ:** Khó

### [PRIORITY: HIGH] Refactor Event System theo DDD đúng chuẩn

> Phase 1 (Foundation) + Phase 2 (Orders/DeliveryNotes) + Phase 3 một phần đã DONE.
> Xem lịch sử tại [CheckList.md](CheckList.md).

---

#### Phase 3 — Migrate các module còn lại

- [ ] **Catalog:** `Product` *(UnitMeasurement / Category / Vendor ✅ done)*
- [ ] **Inventory:** `InventoryStock`, stock movements *(Warehouse ✅ done)*
- [ ] **PurchaseOrders:** `PurchaseOrder`
  - ⚠️ `PurchaseOrderUpdatedEventHandler` hiện đang gọi `VerifyStatusAsync` — cần thay bằng concrete event như `PurchaseOrderItemReceived`
- [ ] **GoodsReceipts:** `GoodsReceipt` — dọn handler trống `GoodsReceiptUpdatedHandler`

---

#### Phase 4 — Outbox Pattern cho Integration Event (1-2 ngày)

- [ ] Tạo entity `OutboxMessage` (Id, Type, Payload JSON, OccurredOnUtc, ProcessedOnUtc?, Error?)
- [ ] EF Configuration + DbSet trong `DbContext` (KHÔNG tự chạy migration — báo Tuấn tự làm)
- [ ] Tạo `IOutbox` interface + implementation lưu vào DbContext (cùng transaction với SaveChanges)
- [ ] Tạo `IIntegrationEvent` marker
- [ ] Tách integration event:
  - `DeliveryNoteConfirmedIntegrationEvent` — gửi notify n8n (thay cho call trực tiếp `_n8nAppService` hiện tại)
- [ ] `BackgroundService` đọc bảng `OutboxMessages` chưa processed → publish qua MediatR → mark processed; retry nếu lỗi
- [ ] Test failure scenarios: n8n down, DB transaction rollback, duplicate dispatch (idempotency)

---

#### Phase 5 — Cleanup (0.5 ngày)

- [ ] Xoá `EntityCreatedEvent<T>`, `EntityUpdatedEvent<T>`, `EntityDeletedEvent<T>`
- [ ] Xoá `EntityCreatedNotification<T>`, `EntityUpdatedNotification<T>`, `EntityDeletedNotification<T>`
- [ ] Xoá `IEventPublisher` cũ + `EventPublisher` implementation
- [ ] Xoá `EventPublisherExtensions.EntityCreated/Updated/Deleted`
- [ ] Xoá các handler trống / TODO comment đã được thay thế
- [ ] Update skill `namcommerce` — thay phần "Publish events qua `IEventPublisher`" bằng hướng dẫn raise domain event mới
- [ ] Update `SYSTEM_DOCUMENTATION.md` — ghi rõ pattern Domain Event mới

---

**Files chính cần đụng (Phase 3 còn lại):**
- `NamEcommerce.Domain.Shared/Events/Catalog/ProductEvents.cs` — file mới
- `NamEcommerce.Domain.Shared/Events/Inventory/InventoryStockEvents.cs` — file mới
- `NamEcommerce.Domain.Shared/Events/PurchaseOrders/PurchaseOrderEvents.cs` — file mới
- `NamEcommerce.Domain/Entities/Catalog/Product.cs` — thêm `Mark*` methods
- `NamEcommerce.Domain/Entities/Inventory/InventoryStock.cs` — thêm `Mark*` methods
- `NamEcommerce.Domain/Entities/PurchaseOrders/PurchaseOrder.cs` — thêm `Mark*` methods
- `NamEcommerce.Domain.Services/**/*Manager.cs` — bỏ inject `IEventPublisher` cho các module còn lại
- `NamEcommerce.Application.Services/Events/PurchaseOrders/PurchaseOrderUpdatedEventHandler.cs` — refactor theo concrete event

**Rủi ro cần lưu ý:**
- `AppAggregateEntity` là `record` — `_domainEvents` (List reference type) hoạt động bình thường nhưng phải `[NotMapped]`
- Test fixtures hiện tại verify `IEventPublisher` mock — cần update sang assert `DomainEvents` collection
- Outbox cần idempotency cho handler để tránh duplicate side effect khi retry

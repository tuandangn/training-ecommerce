# TodoList — VLXD Tuấn Khôi / NamEcommerce

> File theo dõi các hạng mục cần thực thi. Cập nhật trạng thái khi bắt đầu / hoàn thành.
> Các phần đã hoàn thành được lưu tại [CheckList.md](CheckList.md).

---

## ⚙️ Quy tắc làm việc với AI Assistant

> **Áp dụng cho mọi phiên làm việc.** Chi tiết đầy đủ xem [CLAUDE.md](CLAUDE.md).

### Source Control
- **Branch làm việc: `dev-assistant`** — Mọi thay đổi code đều commit lên branch này, KHÔNG commit trực tiếp lên `main`.
- **Sau mỗi phiên hoàn thành**: AI commit lên `dev-assistant`, bạn tự `git push` và merge vào `main` khi kiểm tra xong.
- **Git commit**: AI có thể tạo branch, add, commit trong sandbox. Không thể push (cần credentials của bạn).
- **Lệnh bạn cần tự chạy sau mỗi session**:
  ```bash
  git push origin dev-assistant
  # Sau khi review xong thì merge vào main
  ```

### Quản lý Phiên Làm Việc
- **File session**: Sau khi lên kế hoạch đầy đủ, AI tạo file `sessions/session_[N]_[yyyyMMdd].md` trước khi bắt đầu làm. N là số toàn cục tăng dần.
- **Cập nhật session**: AI đánh dấu từng bước hoàn thành trong file session ngay sau khi làm xong.
- **Khi phát hiện uncommitted files từ phiên trước**: AI commit chúng với message `[uncompleted] ...`, đổi tên file session gần nhất thành `session_N_yyyyMMdd_uncompleted.md`, rồi mới bắt đầu phiên mới.

### Quy tắc khác
- **Unit test**: Tạm thời KHÔNG viết unit test mới (Tuấn tự bổ sung sau).
- **Migration**: AI KHÔNG tự chạy migration — báo Tuấn tự chạy.
- **Skills**: AI đọc skill `namcommerce` trước khi viết code domain.

---

## 🔧 Pending Actions — Build & Smoke Test

*(Tích lũy qua các session 2026-04-28 → 2026-04-30)*

**1. Build verify** toàn bộ solution — `dotnet build NamEcommerce.sln`. Các thay đổi cần check compile sạch:

- Session 2026-04-28: DI cho `IEntityDataReader<StockMovementLog>` + `IEntityDataReader<GoodsReceipt>` + `IVendorDebtManager`; `VendorDebtManagerTests.cs` (helper 7→8 params); Notification + Vendor + Handler module.
- Session 2026-04-30: `PurchaseOrderManager` (12→11 deps, bỏ `IEventPublisher`); `PurchaseOrderManagerTests.cs` (22 constructor calls); `PurchaseOrderItemReceivedEventHandler` (mới); `PurchaseOrderUpdatedEventHandler.cs` (đang là stub — Tuấn xoá file thủ công).

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

> Phase 1 (Foundation) + Phase 2 (Orders/DeliveryNotes) + Phase 3 phần lớn đã DONE.
> Xem lịch sử tại [CheckList.md](CheckList.md).

---

#### Phase 3 — Migrate các module còn lại

- [ ] **GoodsReceipts:** `GoodsReceipt` — migrate sang concrete events (`GoodsReceiptCreated`, `GoodsReceiptItemUnitCostSet`, `GoodsReceiptVendorChanged`, `GoodsReceiptDeleted`...). Hiện tại GoodsReceiptManager vẫn dùng `IEventPublisher` + `EntityCreatedNotification<GoodsReceipt>` / `EntityUpdatedNotification<GoodsReceipt>` / `EntityDeletedNotification<GoodsReceipt>` với `AdditionalData` để phân biệt loại update. Module này phức tạp (3 handler + AverageCost + Vendor debt) — cần thiết kế kỹ để không vỡ logic existing.
- [ ] **Users:** `User` (UserManager) — đang dùng `IEventPublisher`. Phạm vi nhỏ, có thể làm cùng phiên với Phase 5 cleanup.

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
- [ ] Xoá các handler trống / TODO comment đã được thay thế (bao gồm file stub `PurchaseOrderUpdatedEventHandler.cs` từ session 2026-04-30)
- [ ] Update skill `namcommerce` — thay phần "Publish events qua `IEventPublisher`" bằng hướng dẫn raise domain event mới
- [ ] Update `SYSTEM_DOCUMENTATION.md` — ghi rõ pattern Domain Event mới

---

**Files chính cần đụng (Phase 3 còn lại):**
- `NamEcommerce.Domain.Shared/Events/GoodsReceipts/GoodsReceiptEvents.cs` — extend với concrete events Created/UpdatedItemUnitCost/VendorChanged/Deleted
- `NamEcommerce.Domain.Shared/Events/Users/UserEvents.cs` — file mới
- `NamEcommerce.Domain/Entities/GoodsReceipts/GoodsReceipt.cs` — thêm `Mark*` methods
- `NamEcommerce.Domain/Entities/Users/User.cs` — thêm `Mark*` methods
- `NamEcommerce.Domain.Services/GoodsReceipts/GoodsReceiptManager.cs` — bỏ inject `IEventPublisher`
- `NamEcommerce.Domain.Services/Users/UserManager.cs` — bỏ inject `IEventPublisher`
- `NamEcommerce.Application.Services/Events/GoodsReceipts/*Handler.cs` — refactor 3 handler theo concrete events

**Rủi ro cần lưu ý:**
- `AppAggregateEntity` là `record` — `_domainEvents` (List reference type) hoạt động bình thường nhưng phải `[NotMapped]`
- Test fixtures hiện tại verify `IEventPublisher` mock — cần update sang assert `DomainEvents` collection
- Outbox cần idempotency cho handler để tránh duplicate side effect khi retry
- GoodsReceipt migration phức tạp — `AdditionalData` switch hiện tại (Guid itemId vs "vendor-updated" string) cần được tách thành 2 concrete event riêng (`GoodsReceiptItemUnitCostSet` + `GoodsReceiptVendorChanged`)

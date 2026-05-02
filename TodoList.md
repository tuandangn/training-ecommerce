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

> Phase 1 (Foundation) + Phase 2 (Orders/DeliveryNotes) + **Phase 3 hoàn tất 100%** (audit session 2026-05-02 phát hiện GoodsReceipts đã migrate từ trước).
> Xem lịch sử đầy đủ tại [CheckList.md](CheckList.md).

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

#### Phase 5 — Cleanup (0.5 ngày) — PREREQUISITE đã DONE 100%

> Audit session 2026-05-02: KHÔNG còn caller nào của `.EntityCreated()` / `.EntityUpdated()` / `.EntityDeleted()` extension methods trong toàn solution. `IEventPublisher` không còn được Manager / AppService / Handler nào inject. Sau session 3 (2026-05-02): KHÔNG còn subscriber nào của `EntityCreatedNotification<T>` / `EntityUpdatedNotification<T>` / `EntityDeletedNotification<T>` trong toàn solution → sẵn sàng xoá legacy types.

**Xoá 4 stub file (đã đánh dấu safe-to-delete trong file — chỉ còn comment, không compile thành handler nào):**
- [ ] `Application.Services/Events/GoodsReceipts/GoodsReceiptUpdatedHandler.cs`
- [ ] `Application.Services/Events/PurchaseOrders/PurchaseOrderUpdatedEventHandler.cs`
- [ ] `Application.Services/Events/Orders/OrderUpdatedEventHandler.cs` — stub từ session 2026-05-02 (session 3)
- [ ] `Application.Services/Events/Orders/OrderCreatedEventHandler.cs` — *CHỈ XOÁ* nếu Tuấn không có ý định implement Reserve Stock. Hiện tại đã subscribe concrete `OrderPlaced` event với body rỗng + TODO comment để giữ kiến trúc cho việc implement sau (session 3 2026-05-02). Nếu xác nhận không implement → xoá file.

**Xoá legacy event chain (sau khi block trên đã xong):**
- [ ] `Domain.Shared/Events/Entities/EntityCreatedEvent.cs`, `EntityUpdatedEvent.cs`, `EntityDeletedEvent.cs`
- [ ] `Application.Services/Events/EventPublisher.cs` (chứa `EntityCreatedNotification<T>`, `EntityUpdatedNotification<T>`, `EntityDeletedNotification<T>` records)
- [ ] `Domain.Shared/Events/IEventPublisher.cs`
- [ ] `Domain.Services/Extensions/EventPublisherExtensions.cs`
- [ ] DI registration trong `Web/Program.cs:148`: `services.AddScoped<IEventPublisher, EventPublisher>();`

**Xoá `BaseEvent` + 2 file event không dùng:**
- [ ] `Domain.Shared/Events/BaseEvent.cs` (chỉ còn dùng bởi 2 file dưới)
- [ ] `Domain.Shared/Events/DeliveryNotes/DeliveryNoteConfirmedEvent.cs` (KHÔNG ai khởi tạo — handlers thật subscribe concrete `DeliveryNoteConfirmed` khác tên)
- [ ] `Domain.Shared/Events/DeliveryNotes/DeliveryNoteDeliveredEvent.cs` (tương tự)

**Update tài liệu:**
- [ ] Update skill `namcommerce` — thay phần "Publish events qua `IEventPublisher`" bằng hướng dẫn `entity.Mark*()` raise concrete events
- [ ] Update `SYSTEM_DOCUMENTATION.md` — ghi rõ pattern Domain Event mới (raise trong entity, dispatch qua interceptor, INotificationHandler subscribe concrete event)

---

**Rủi ro / Lưu ý chung:**
- `AppAggregateEntity` là `record` — `_domainEvents` (List reference type) hoạt động bình thường nhưng phải `[NotMapped]`
- Test fixtures cũ verify `IEventPublisher` mock — đã được update qua các session trước; nếu phát hiện test còn sót, sửa sang assert `DomainEvents` collection
- Outbox (Phase 4) cần idempotency cho handler để tránh duplicate side effect khi retry

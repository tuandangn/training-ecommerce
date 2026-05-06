# TodoList — VLXD Tuấn Khôi / NamEcommerce

> File theo dõi các hạng mục cần thực thi. Cập nhật trạng thái khi bắt đầu / hoàn thành.
> Các phần đã hoàn thành được lưu tại [CheckList.md](CheckList.md).

---

### Quy tắc khác
- **Unit test**: Tạm thời KHÔNG viết unit test mới (Tuấn tự bổ sung sau).
- **Migration**: AI KHÔNG tự chạy migration — báo Tuấn tự chạy.
- **Skills**: AI đọc skill `namcommerce` trước khi viết code domain.

---

## 🔧 Pending Actions — Build & Smoke Test

*(Tích lũy qua các session 2026-04-28 → 2026-05-06)*

**1. Build verify** toàn bộ solution — `dotnet build NamEcommerce.sln`. Các thay đổi cần check compile sạch:

- Session 2026-04-28: DI cho `IEntityDataReader<StockMovementLog>` + `IEntityDataReader<GoodsReceipt>` + `IVendorDebtManager`; `VendorDebtManagerTests.cs` (helper 7→8 params); Notification + Vendor + Handler module.
- Session 2026-04-30: `PurchaseOrderManager` (12→11 deps, bỏ `IEventPublisher`); `PurchaseOrderManagerTests.cs` (22 constructor calls); `PurchaseOrderItemReceivedEventHandler` (mới); `PurchaseOrderUpdatedEventHandler.cs` (đang là stub — Tuấn xoá file thủ công).
- Session 2026-05-06 (Phase 4 Outbox): `OutboxMessage` entity (Domain), `OutboxMessageMapping` (Data.SqlServer), `IOutbox` + `OutboxAccessor`, `IIntegrationEvent` (kế thừa MediatR `INotification`), `DeliveryNoteConfirmedIntegrationEvent` + handler, `OutboxProcessor` background service. Csproj `Data.SqlServer` thêm 3 PackageReference: `Microsoft.Extensions.Hosting.Abstractions` 10.0.0, `Microsoft.Extensions.DependencyInjection.Abstractions` 10.0.0, `Microsoft.Extensions.Logging.Abstractions` 10.0.0. `Program.cs` đăng ký `IOutbox`, `OutboxProcessorOptions`, `AddHostedService<OutboxProcessor>`.

**2. Migrations cần chạy thủ công:**
- `Add-Migration AddAverageCostToInventoryStock` (project Data.SqlServer)
- `Add-Migration AddVendorToGoodsReceiptAndDebt`
- `Add-Migration AddOutboxMessages` (Phase 4 — bảng `tbl.OutboxMessage` với index `IX_OutboxMessage_Pending`)
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

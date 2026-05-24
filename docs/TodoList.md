# TodoList — VLXD Tuấn Khôi / NamEcommerce

> File chỉ giữ các hạng mục chưa xong. Các mục đã hoàn tất đã được dọn ngày 2026-05-16.

---

### Quy tắc bắt buộc

- **Unit test**: KHÔNG viết unit test mới, KHÔNG sửa code trong project `*.Test`.
- **Migration**: AI KHÔNG tự chạy migration — báo Tuấn tự chạy.
- **Skills**: using-superpowers, using-agent-skills, namcommerce.
- **Comments**: chỉ viết khi giải thích WHY không hiển nhiên.

---

### Ghi chú thực hiện

- Không viết/sửa unit test theo rule hiện tại.
- Mỗi mục cần build verify + manual smoke test nếu có UI/workflow.
- AI cập nhật checkbox `[x]` ngay khi xong từng mục, đính ngày `✅ YYYY-MM-DD`.

---

## Feature: Direct-Ship Workflow (Giao thẳng NCC → khách)

> Plan chi tiết: [`docs/DIRECT_SHIP_PLAN.md`](docs/DIRECT_SHIP_PLAN.md) — APPROVED, refactored sau code review 2026-05-16.
> Scope file này: chỉ giữ các điểm **cần cải thiện sau review 2026-05-17**. Các hạng mục đã làm / chưa làm ngoài scope review hiện tại không liệt kê lại.
> Nguyên tắc chính: không tạo flow nhận hàng song song, không double-count allocation received, reuse lifecycle `DeliveryNoteStatus` hiện có.

### DS-FIX-1 — Align DeliveryNote lifecycle cho Direct-Ship

**Mục tiêu:** Direct-ship DN dùng lifecycle chuẩn `Confirmed → Delivered/Cancelled`; Confirm khách nhận hàng phải đi qua `DeliveryNoteDelivered` để sinh `CustomerDebt` và dispatch tồn kho đúng pipeline hiện có.

**Files chính:**
- `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Enums/DeliveryNotes/DeliveryNoteSourceType.cs`
- `NamEcommerce/Domain/NamEcommerce.Domain/Entities/DeliveryNotes/DeliveryNote.cs`
- `NamEcommerce/Domain/NamEcommerce.Domain.Services/DeliveryNotes/DeliveryNoteManager.cs`
- `NamEcommerce/Domain/NamEcommerce.Domain.Services/PurchaseOrders/DirectShipManager.cs`
- `NamEcommerce/Application/NamEcommerce.Application.Services/Events/DeliveryNotes/DeliveryNoteDeliveredEventHandler.cs`
- `NamEcommerce/Application/NamEcommerce.Application.Services/Events/DeliveryNotes/DeliveryNoteDeliveredStockHandler.cs`

- [x] Thêm `DeliveryNoteSourceType.DirectShipToCustomer = 3` ở cuối enum, giữ nguyên value cũ. ✅ 2026-05-17
- [x] Sửa tạo DN direct-ship để set `SourceType = DirectShipToCustomer`, `Status = Confirmed`, `WarehouseId = DirectShipTransit`, giữ link `SourceGoodsReceiptId`. ✅ 2026-05-17
- [x] Ngừng dùng `DeliveryConfirmationStatus` làm trạng thái nghiệp vụ chính trong direct-ship flow; nếu DB đã có field thì giữ lại để tránh migration dọn dẹp trong patch này. ✅ 2026-05-17
- [x] Sửa pending query: pending direct-ship = `SourceType == DirectShipToCustomer && Status == Confirmed`. ✅ 2026-05-17
- [x] Sửa confirm khách nhận hàng để gọi lifecycle đưa DN sang `Delivered`, bảo đảm event `DeliveryNoteDelivered` chạy như DN thường. ✅ 2026-05-17
- [x] Sửa reject khách từ chối để đưa DN sang `Cancelled`, không chỉ set confirmation flag riêng. ✅ 2026-05-17
- [x] Verify build: `rtk dotnet build NamEcommerce\Presentation\NamEcommerce.Web\NamEcommerce.Web.csproj -p:OutDir=C:\tmp\nam-web-build\` ✅ 2026-05-17
- [ ] Manual smoke: receive direct-ship xong thấy DN `Confirmed`; bấm Confirm thấy DN `Delivered` và có `CustomerDebt`; bấm Reject thấy DN `Cancelled`.

### DS-FIX-2 — Khép kín stock movement qua Direct-Ship Transit

**Mục tiêu:** Hàng direct-ship vẫn đi qua tồn kho bằng audit trail rõ ràng: GR vào kho chính, transfer sang kho ảo, confirm thì dispatch từ kho ảo, reject/cancel thì transfer ngược về kho chính.

**Files chính:**
- `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Services/Inventory/IInventoryStockManager.cs`
- `NamEcommerce/Domain/NamEcommerce.Domain.Services/Inventory/InventoryStockManager.cs`
- `NamEcommerce/Domain/NamEcommerce.Domain.Services/PurchaseOrders/DirectShipManager.cs`
- `NamEcommerce/Application/NamEcommerce.Application.Services/Events/PurchaseOrders/DirectShipDeliveryRejectedHandler.cs`
- `NamEcommerce/Application/NamEcommerce.Application.Services/Events/DeliveryNotes/DeliveryNoteCancelledEventHandler.cs` nếu handler hiện có phù hợp, hoặc handler mới cùng module nếu chưa có.

- [x] Thêm `TransferStockAsync(productId, fromWarehouseId, toWarehouseId, quantity, unitCost, referenceId, userId, note)` vào inventory stock manager. ✅ 2026-05-17
- [x] Implement transfer bằng 2 movement log `StockMovementType.Transfer`: outbound ở kho nguồn, inbound ở kho đích. ✅ 2026-05-17
- [x] Khi allocation direct-ship nhận hàng, transfer đúng `receivedDelta` từ kho nhận GR sang `DirectShipTransit`. ✅ 2026-05-17
- [x] Khi khách Confirm, dispatch từ `DirectShipTransit` thông qua pipeline `DeliveryNoteDeliveredStockHandler`. ✅ 2026-05-17
- [x] Khi khách Reject, transfer từ `DirectShipTransit` về kho chính với `unitCost = PurchaseOrderItem.UnitCost`. ✅ 2026-05-17
- [x] Khi SO bị cancel sau khi hàng đã vào transit, transfer phần đã nhận từ `DirectShipTransit` về kho chính với reason rõ ràng. ✅ 2026-05-17
- [x] Verify build: `rtk dotnet build NamEcommerce\Presentation\NamEcommerce.Web\NamEcommerce.Web.csproj -p:OutDir=C:\tmp\nam-web-build\` ✅ 2026-05-17
- [ ] Manual smoke: GR 100, direct-ship 10 → kho chính net +90, transit +10; Confirm DN → transit về 0; Reject DN → kho chính nhận lại 10.

### DS-FIX-3 — Gom received allocation về một pipeline duy nhất

**Mục tiêu:** `PurchaseOrderAllocationManager.SyncReceivedForPurchaseOrderItemAsync` là nơi duy nhất cộng/trừ `ReceivedQuantity` trên allocation; `DirectShipManager` chỉ orchestration tạo DN/transfer, không tự phân bổ quantity.

**Files chính:**
- `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Services/PurchaseOrders/IPurchaseOrderAllocationManager.cs`
- `NamEcommerce/Domain/NamEcommerce.Domain.Services/PurchaseOrders/PurchaseOrderAllocationManager.cs`
- `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Services/PurchaseOrders/IDirectShipManager.cs`
- `NamEcommerce/Domain/NamEcommerce.Domain.Services/PurchaseOrders/DirectShipManager.cs`
- `NamEcommerce/Domain/NamEcommerce.Domain.Services/PurchaseOrders/PurchaseOrderManager.cs`
- `NamEcommerce/Domain/NamEcommerce.Domain.Services/GoodsReceipts/GoodsReceiptPurchaseOrderLinker.cs`

- [x] Refactor `SyncReceivedForPurchaseOrderItemAsync` sort allocation theo `IsDirectShip desc`, `DirectShipPriority desc`, `CreatedOnUtc asc`. ✅ 2026-05-17
- [x] Cho `SyncReceivedForPurchaseOrderItemAsync` trả về danh sách allocation delta vừa tăng/giảm received quantity. ✅ 2026-05-17
- [x] Với delta direct-ship vừa tăng, gọi hook orchestration `DirectShipManager.OnAllocationReceivedAsync(allocationId, receivedDelta, sourceGoodsReceiptId, receivedWarehouseId)`. ✅ 2026-05-17
- [x] Xóa hoặc ngừng dùng `DirectShipManager.DistributeReceivedQuantityAsync` khỏi single receive, bulk receive và các path link GR. ✅ 2026-05-17
- [x] Bảo đảm single receive, bulk receive, và `GoodsReceiptPurchaseOrderLinker` đều đi qua cùng một sync method. ✅ 2026-05-17
- [x] Giữ guard không allocate/receive vượt nhu cầu SO item đã thêm ở patch quantity split. ✅ 2026-05-17
- [x] Verify build: `rtk dotnet build NamEcommerce\Presentation\NamEcommerce.Web\NamEcommerce.Web.csproj -p:OutDir=C:\tmp\nam-web-build\` ✅ 2026-05-17
- [ ] Manual smoke: single receive và bulk receive cùng một scenario SO 10 / PO 100 đều tạo direct-ship allocation 10, không còn đường nào tạo allocation 100.

### DS-FIX-4 — Cập nhật UI/query để hiển thị trạng thái giao hàng thật

**Mục tiêu:** SO Details, PO Details, DN Details, Pending Direct-Ship và report đọc trạng thái từ DN `SourceType + Status`, không đọc trạng thái allocation hoặc `DeliveryConfirmationStatus` khi cần nói về delivery.

**Files chính:**
- `NamEcommerce/Domain/NamEcommerce.Domain.Services/PurchaseOrders/DirectShipManager.cs`
- `NamEcommerce/Application/NamEcommerce.Application.Services/Report/DirectShipReportAppService.cs`
- `NamEcommerce/Application/NamEcommerce.Application.Services/Extensions/DeliveryNoteExtensions.cs`
- `NamEcommerce/Presentation/NamEcommerce.Web/Services/DeliveryNotes/DeliveryNoteModelFactory.cs`
- `NamEcommerce/Presentation/NamEcommerce.Web/Views/DeliveryNote/Details.cshtml`
- `NamEcommerce/Presentation/NamEcommerce.Web/Services/Orders/OrderModelFactory.cs`
- `NamEcommerce/Presentation/NamEcommerce.Web/Views/Order/Details.cshtml`
- `NamEcommerce/Presentation/NamEcommerce.Web/Services/PurchaseOrders/PurchaseOrderModelFactory.cs`

- [x] Map direct-ship delivery status từ DN status: `Confirmed = chờ khách xác nhận`, `Delivered = khách đã nhận`, `Cancelled = khách từ chối/hủy`. ✅ 2026-05-17
- [x] SO Details hiển thị allocation quantity/received quantity riêng, delivery status riêng, có link DN nếu đã tạo. ✅ 2026-05-17
- [x] PO Details hiển thị PO line quantity, allocation quantity, received allocation quantity, DN status; không dùng PO line quantity làm direct-ship quantity. ✅ 2026-05-17
- [x] DN Details hiển thị banner direct-ship dựa trên `SourceType == DirectShipToCustomer`, không dựa riêng vào `IsDirectShip`. ✅ 2026-05-17
- [x] Pending Direct-Ship list/filter dùng `SourceType == DirectShipToCustomer && Status == Confirmed`. ✅ 2026-05-17
- [x] Direct-Ship report đổi confirmed/rejected/pending theo `DeliveryNoteStatus`, giữ field cũ chỉ để backward compatibility nếu cần hiển thị legacy. ✅ 2026-05-17
- [x] Verify build: `rtk dotnet build NamEcommerce\Presentation\NamEcommerce.Web\NamEcommerce.Web.csproj -p:OutDir=C:\tmp\nam-web-build\` ✅ 2026-05-17
- [ ] Manual smoke: sau Confirm/Reject DN, SO Details + PO Details + DN Details đổi trạng thái nhất quán.

### DS-FIX-5 — Cancel SO có hàng direct-ship đã received

**Mục tiêu:** Không cho hủy SO âm thầm khi hàng direct-ship đã nằm ở transit; user phải xác nhận chuyển hàng về kho chính.

**Files chính:**
- `NamEcommerce/Application/NamEcommerce.Application.Services/Orders/OrderAppService.cs`
- `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Services/PurchaseOrders/IDirectShipManager.cs`
- `NamEcommerce/Domain/NamEcommerce.Domain.Services/PurchaseOrders/DirectShipManager.cs`
- `NamEcommerce/Presentation/NamEcommerce.Web.Contracts/Commands/Models/Orders/*`
- `NamEcommerce/Presentation/NamEcommerce.Web.Framework/Commands/Handlers/Orders/*`
- `NamEcommerce/Presentation/NamEcommerce.Web/Views/Order/Details.cshtml`

- [x] Thêm check `HasReceivedDirectShipAllocationsAsync(orderId)` trước khi cancel SO. ✅ 2026-05-17
- [x] Nếu có hàng đã received và request chưa xác nhận, trả kết quả để UI bật modal cảnh báo. ✅ 2026-05-17
- [x] Khi user xác nhận, cancel SO và gọi `HandleSoCancelledForReceivedDirectShipAsync(orderId, userId, reason)`. ✅ 2026-05-17
- [x] Method handle cancel chuyển stock từ `DirectShipTransit` về kho chính, cancel DN direct-ship còn `Confirmed`, giữ audit note có mã SO. ✅ 2026-05-17
- [x] Verify build: `rtk dotnet build NamEcommerce\Presentation\NamEcommerce.Web\NamEcommerce.Web.csproj -p:OutDir=C:\tmp\nam-web-build\` ✅ 2026-05-17
- [ ] Manual smoke: SO có direct-ship received → bấm Cancel thấy modal; confirm thì stock về kho chính và SO cancelled; đóng modal thì SO giữ nguyên.

### DS-FIX-6 — Dọn comment/documentation drift sau khi code align

**Mục tiêu:** Code và plan không còn nói hai kiểu khác nhau về direct-ship; người sau đọc vào không nhầm `DeliveryConfirmationStatus` là lifecycle chính.

**Files chính:**
- `docs/DIRECT_SHIP_PLAN.md`
- `TodoList.md`
- Các XML comment trong `IDeliveryNoteManager`, `DirectShipManager`, `PurchaseOrderManager`, `DeliveryNoteDtos` nếu còn mô tả `DeliveryConfirmationStatus` là trạng thái chính.

- [x] Sau khi DS-FIX-1 đến DS-FIX-5 xong, đọc lại comment/XML docs có chữ `DeliveryConfirmationStatus`. ✅ 2026-05-17
- [x] Cập nhật comment để nói rõ source of truth là `DeliveryNote.Status`; field legacy nếu còn chỉ để compatibility. ✅ 2026-05-17
- [x] Cập nhật `docs/DIRECT_SHIP_PLAN.md` nếu implementation cuối cùng có khác biệt có chủ đích. ✅ 2026-05-17
- [x] Cập nhật checkbox trong `TodoList.md` ngay khi từng DS-FIX hoàn tất, kèm ngày `✅ YYYY-MM-DD`. ✅ 2026-05-17
- [x] Verify cuối: `rtk dotnet build NamEcommerce\Presentation\NamEcommerce.Web\NamEcommerce.Web.csproj -p:OutDir=C:\tmp\nam-web-build\` ✅ 2026-05-17
- [ ] Manual smoke cuối: chạy checklist SO 10 / PO 100 direct-ship, PO 10 direct-ship, allocate from existing PO, single receive, bulk receive, confirm, reject, cancel SO.

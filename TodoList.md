# TodoList — VLXD Tuấn Khôi / NamEcommerce

> File theo dõi các hạng mục đang làm. Đã hoàn tất + dọn dẹp Dashboard, Migration P4.7, Smoke test ledger (2026-05-14).

---

### Quy tắc bắt buộc (trùng CLAUDE.md, nhắc lại cho dễ thấy)

- **Branch hiện hành**: `dev-assistant` — AI checkout/tạo trước khi sửa code.
- **Unit test**: KHÔNG viết unit test mới, KHÔNG sửa code trong project `*.Test`.
- **Migration**: AI KHÔNG tự chạy migration — báo Tuấn chạy.
- **Skills**: AI đọc skill `namcommerce` trước khi viết code domain.
- **Comments**: chỉ viết khi giải thích WHY không hiển nhiên.

---

## 🆕 Feature: Quản lý hàng thiếu & Tự động đặt nhà cung cấp (kick-off 2026-05-15)

### Mục đích tổng thể

Khi đơn bán có mặt hàng vượt tồn kho, hệ thống tự nhận biết shortage, gợi ý nhà cung cấp dựa trên `ProductVendor` + lịch sử PO, và cho phép tạo Purchase Order ngay từ context Sale Order hoặc Delivery Note. Track liên kết PO Item ↔ SO Item để khi hàng về biết lô đó thuộc đơn nào.

### Bối cảnh nghiệp vụ

Cửa hàng VLXD đôi khi nhận đơn vượt tồn (khách đặt trước, giao sau khi nhập hàng). Hiện tại nhân viên phải tự nhớ đơn nào thiếu mặt hàng nào → dễ sót, dễ trùng PO. Confirm phiếu xuất bị fail vì thiếu hàng nhưng không có cách giải quyết liền tay.

### Phạm vi (trong scope)

- 3 entry point: trang chi tiết Sale Order, trang chi tiết Delivery Note, menu "Hàng cần nhập"
- Tạo nhiều PO trong 1 thao tác (group theo NCC)
- Smart detect PO Draft sẵn có → popup hỏi merge hay tạo mới
- Allocation PO Item ↔ SO Item để track khi nhận hàng
- Modal "Chi tiết hàng thiếu" tái sử dụng giữa SO Details và DN Details

### Ngoài scope (làm sau)

- "Gộp PO" từ PurchaseOrder/List (feature riêng, dùng chung domain service merge với Phase 3)
- Tự động phê duyệt PO (vẫn manual)
- Tự tính giá nhập (user nhập tay, default = giá lần trước)

### Công thức Shortage (thống nhất cho cả 5 phase)

```
StillNeeded      = OrderItem.Quantity − AlreadyShipped (qua DeliveryNote đã confirmed)
ReservedByOthers = TotalLedger(productId) − ReservedForThisOrder(productId, orderId)
AvailableForMe   = max(0, Σ QuantityOnHand − Σ WarehouseReserved − ReservedByOthers)
Shortage         = max(0, StillNeeded − AvailableForMe)
```

---

## Phase 1 — Domain logic tính shortage

> **Mục đích**: Foundation cho mọi entry point. Tính chính xác "hàng thiếu" cho 1 SO, 1 DN, hoặc toàn hệ thống. Không đụng UI, không entity mới.

- [ ] **P1.1** — DTO `OrderItemShortageDto` trong `Domain.Shared/Dtos/Inventory/`: `OrderItemId`, `ProductId`, `ProductName`, `RequiredQuantity`, `ShippedQuantity`, `AvailableQuantity`, `ShortageQuantity`, `AllocatedFromPurchaseOrders` (list { POId, POCode, AllocatedQty, ExpectedReceiveDate }).
- [ ] **P1.2** — DTO `DeliveryNoteItemShortageDto` tương đương nhưng cho DN context (có thêm `DeliveryNoteItemId`).
- [ ] **P1.3** — Helper internal trong `InventoryStockManager`: `ComputeAvailableForOrderAsync(productId, orderId)` reuse `GetGlobalAvailableForProductAsync` trừ `ReservedForOrder`.
- [ ] **P1.4** — Interface + impl `IShortageQueryService.GetOrderItemShortagesAsync(Guid orderId)` trong `Domain.Services/Inventory/`.
- [ ] **P1.5** — `IShortageQueryService.GetDeliveryNoteShortagesAsync(Guid deliveryNoteId)`.
- [ ] **P1.6** — `IShortageQueryService.GetGlobalShortagesAsync(ShortageFilterDto filter)` cho aggregation page (filter null trả tất cả).
- [ ] **P1.7** — Register DI `IShortageQueryService` → `ShortageQueryService`.
- [ ] **P1.8** — Build verify (`dotnet build`).

**Verify P1**: Tạo SO vượt tồn, gọi method qua AppService stub, expect `ShortageQuantity > 0`. Confirm DN một phần, gọi lại, expect `Shortage` giảm tương ứng.

---

## Phase 2 — Entity allocation PO ↔ SO

> **Mục đích**: Khi tạo PO từ shortage, lưu liên kết "PO Item này tạo cho SO Item kia, số lượng X". Khi GoodsReceipt nhận hàng → biết phân phối cho đơn nào. Cần migration.

- [ ] **P2.1** — Entity `PurchaseOrderItemAllocation` trong `Domain/Entities/PurchaseOrders/`: `Id`, `PurchaseOrderItemId`, `OrderItemId`, `AllocatedQuantity`, `ReceivedQuantity` (default 0), `CreatedOnUtc`.
- [ ] **P2.2** — Mapping `PurchaseOrderItemAllocationMapping.cs` trong `Data.SqlServer/Mappings/` (index trên `PurchaseOrderItemId` và `OrderItemId`).
- [ ] **P2.3** — AI chuẩn bị model snapshot sẵn sàng; **Tuấn chạy `Add-Migration AddPurchaseOrderItemAllocation` + `Update-Database`**.
- [ ] **P2.4** — Interface + impl `IPurchaseOrderAllocationManager` trong `Domain.Services/PurchaseOrders/`:
  - `AllocateAsync(poItemId, orderItemId, quantity)`
  - `IncreaseReceivedAsync(allocationId, receivedQty)` (Phase 5 dùng)
  - `GetAllocationsForOrderItemAsync(orderItemId)`
  - `GetAllocationsForPurchaseOrderItemAsync(poItemId)`
- [ ] **P2.5** — Register DI.
- [ ] **P2.6** — Build verify.

**Verify P2**: Sau migration, Tuấn kiểm tra bảng tồn tại trong DB. AI test allocate qua AppService stub, kiểm tra entity sinh đúng.

---

## Phase 3 — Trang "Hàng cần nhập" (aggregation page)

> **Mục đích**: Trang trung tâm để thực sự "bấm tạo phiếu nhập". List shortage toàn hệ thống, group theo NCC gợi ý, cho phép tạo nhiều PO 1 lần. Là output chính của workflow.

**Entry points** (cùng 1 route, khác query string):
- `/PurchaseOrders/ShortageAggregation` (global, mọi shortage)
- `?orderId={guid}` (chỉ shortage của SO này)
- `?deliveryNoteId={guid}` (DN scope, có toggle expand sang SO scope)

**Domain layer:**

- [ ] **P3.1** — `ISupplierSuggestionService.SuggestVendorsForProductAsync(productId)` — kết hợp `ProductVendor` (sort `DisplayOrder`) + lịch sử PO (vendor đã từng nhập product này nhưng chưa có trong `ProductVendor`). Return tối đa 5 gợi ý kèm `LastPurchaseDate` và `LastUnitPrice`.
- [ ] **P3.2** — `IPurchaseOrderManager.FindDraftForVendorAsync(vendorId)` — trả PO Draft cùng NCC để detect trùng. Return null nếu không có.
- [ ] **P3.3** — `IPurchaseOrderManager.CreatePurchaseOrderFromShortageAsync(CreatePoFromShortageDto)` — tạo PO Draft mới + gọi `IPurchaseOrderAllocationManager.AllocateAsync` cho từng item.
- [ ] **P3.4** — `IPurchaseOrderManager.AddItemsToExistingDraftAsync(poId, items, allocations)` — merge vào PO Draft sẵn có. **Tách riêng để feature "Gộp PO" tương lai reuse.**

**Application layer:**

- [ ] **P3.5** — AppDto + `IShortageAggregationAppService.GetAggregatedShortagesAsync(filter)` trả `List<VendorShortageGroupAppDto>`. Group theo VendorId gợi ý chính, kèm section "NoVendorGroup" cho product không có ProductVendor.
- [ ] **P3.6** — `IShortageAggregationAppService.CreatePurchaseOrdersFromShortageAsync(CreatePosFromShortageAppDto)` — chấp nhận list group, mỗi group có flag `MergeIntoExistingPoId` (nullable). Return danh sách `CreatedPurchaseOrderResultDto`.
- [ ] **P3.7** — `IShortageAggregationAppService.CheckExistingDraftsAsync(List<Guid> vendorIds)` — return mapping `VendorId → ExistingDraftPo` để UI hiển thị popup confirm.

**Presentation layer:**

- [ ] **P3.8** — Query `GetAggregatedShortagesQuery` (filter) + Handler + `ShortageAggregationModel`.
- [ ] **P3.9** — Command `CheckExistingDraftsCommand` + Handler + `ExistingDraftsResultModel`.
- [ ] **P3.10** — Command `CreatePurchaseOrdersFromShortageCommand` + Handler + Validator + `CreatePurchaseOrdersFromShortageResultModel`.
- [ ] **P3.11** — Controller `PurchaseOrderController.ShortageAggregation()` GET, `CheckExistingDrafts` POST (return JSON), `CreateFromShortage` POST (return JSON).
- [ ] **P3.12** — View `Views/PurchaseOrder/ShortageAggregation.cshtml` theo wireframe v2: filter chip + card NCC (header + ngày hẹn nhận + ghi chú + list mặt hàng với SL/Giá inputs) + section "Chưa có NCC gợi ý" + sticky footer.
- [ ] **P3.13** — JS:
  - Bind SL/Giá inputs → tổng tạm tính realtime per-card và global
  - Toggle "Bao gồm cả SO shortage" khi `?deliveryNoteId` (re-fetch data)
  - Submit → POST `CheckExistingDrafts` → cho mỗi vendor có draft → mở popup "Merge vào PO #X" / "Tạo PO mới" → POST `CreateFromShortage` với quyết định
- [ ] **P3.14** — Modal/popup component "Đã có PO Draft cho NCC X — Merge hay Tạo mới?" (Bootstrap modal, render từng NCC trùng).
- [ ] **P3.15** — Build verify + manual smoke test.

**Verify P3**:
- Happy path: SO vượt tồn → trang → tạo PO → DB có PO + allocation đúng
- Merge: PO Draft sẵn có → smart detect → chọn merge → PO cũ có thêm item
- No-vendor: product không có `ProductVendor` → section đỏ hiện đúng
- Multi-vendor: 1 SO có 3 product 3 NCC khác nhau → tạo 3 PO trong 1 click

---

## Phase 4 — UI integration trang chi tiết SO và DN

> **Mục đích**: Đem shortage info vào nơi user gặp vấn đề (trang SO Details, DN Details). Passive alert + modal "Xem thêm" + nút "Nhập hàng thiếu" chuyển sang Phase 3.

### Phase 4a — Trang chi tiết Sale Order

- [ ] **P4a.1** — Query `GetOrderShortageInfoQuery` + Handler trả `OrderShortageInfoModel` (list shortage items + supplier suggestions).
- [ ] **P4a.2** — Bổ sung field vào `OrderDetailsModel`: `HasShortage`, `ShortageItems`.
- [ ] **P4a.3** — Service trong `Web/Services/Order/`: gọi `IShortageQueryService` + `ISupplierSuggestionService` → map sang model.
- [ ] **P4a.4** — Razor view `Views/Order/Details.cshtml`:
  - Alert warning Bootstrap ở đầu trang khi `HasShortage` = true
  - Nút "Xem hàng thiếu" trong alert → mở modal Bootstrap
  - Modal hiển thị: list shortage items, gợi ý NCC mỗi item, nút "Nhập hàng thiếu" footer
- [ ] **P4a.5** — JS: handle nút modal → redirect `/PurchaseOrders/ShortageAggregation?orderId={id}`.

### Phase 4b — Trang chi tiết Delivery Note

- [ ] **P4b.1** — Query `GetDeliveryNoteShortageInfoQuery` + Handler + Model.
- [ ] **P4b.2** — Bổ sung field vào `DeliveryNoteDetailsModel`: `HasShortage`, `ShortageItems`.
- [ ] **P4b.3** — Razor view `Views/DeliveryNote/Details.cshtml`: alert + modal (cấu trúc giống SO, reuse partial view nếu được).
- [ ] **P4b.4** — Khi confirm DN bị `InsufficientStockException`:
  - Catch trong `ConfirmDeliveryNoteHandler`, return failure result kèm shortage list
  - JS hiển thị modal "Phiếu xuất chưa đủ hàng" — **reuse modal component từ P4b.3**
  - Bắt buộc nhập đủ mới cho confirm (không có option "Vẫn xác nhận")
- [ ] **P4b.5** — Trang aggregation hỗ trợ toggle "Chỉ items phiếu xuất này" (default) ↔ "Bao gồm cả SO chưa có phiếu xuất" khi vào từ `?deliveryNoteId`.

**Verify P4**:
- SO thiếu hàng → mở Details → alert hiện, modal mở, redirect đúng
- DN thiếu hàng → mở Details → alert hiện
- Bấm Confirm DN thiếu → modal mở, không cho confirm
- Sửa SO/DN đủ hàng → alert tự biến mất sau refresh

---

## Phase 5 — GoodsReceipt integration

> **Mục đích**: Khi nhập hàng từ PO có allocation → tự động đánh dấu SO Item "đã có hàng" tương ứng. Đóng vòng tròn workflow: SO thiếu → PO → nhận hàng → SO hết thiếu.

- [ ] **P5.1** — Event `PurchaseOrderItemReceivedEvent` (`purchaseOrderItemId`, `productId`, `quantityReceived`) — phát từ `GoodsReceiptManager` khi confirm phiếu nhập.
- [ ] **P5.2** — Handler `OnPurchaseOrderItemReceived` trong `Application.Services/Events/PurchaseOrders/`:
  - Query allocations theo `PurchaseOrderItemId`
  - Distribute `quantityReceived` theo tỷ lệ `AllocatedQuantity` (FIFO theo `CreatedOnUtc` cho deterministic)
  - Gọi `IPurchaseOrderAllocationManager.IncreaseReceivedAsync` cho từng allocation
- [ ] **P5.3** — Edge case: nếu `quantityReceived > Σ AllocatedQuantity` → ghi log warning + phần dư về free stock (không gắn allocation).
- [ ] **P5.4** — Update tự động cho UI: shortage đã tính realtime từ stock nên không cần invalidate cache. Verify: sau GoodsReceipt, refresh trang SO Details → alert biến mất.
- [ ] **P5.5** — Build verify + end-to-end test.

**Verify P5 (end-to-end)**:
1. Tạo SO 100 bao xi măng, tồn 30 → SO Details alert "Thiếu 70"
2. Bấm "Nhập hàng thiếu" → trang aggregation → tạo PO 70 bao
3. PO Details thấy allocation về SO này
4. Tạo GoodsReceipt từ PO, nhận đủ 70 → confirm
5. Refresh SO Details → alert biến mất, tồn = 100, `Allocation.ReceivedQuantity = 70`
6. Tạo DN xuất 100 bao → confirm thành công (không còn shortage)

---

## Quy ước commit

- Mỗi Phase = 1 commit hoặc loạt commit liên quan, push xong báo Tuấn review.
- Migration P2 do Tuấn chạy trước khi AI tiếp tục Phase 3.
- Không gộp Phase trong 1 commit để dễ revert nếu vướng.

## Ghi chú thực hiện

- Không viết/sửa unit test theo rule hiện tại. Mỗi mục cần build verify + manual smoke test.
- Nếu build bin Web bị IIS Express lock: dùng `-p:OutDir=C:\tmp\NamEcommerceShortageBuild\`.
- AI cập nhật checkbox `[x]` ngay khi xong từng mục, đính ngày `✅ YYYY-MM-DD`.

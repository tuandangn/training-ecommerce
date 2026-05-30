# TodoList — VLXD Tuấn Khôi / NamEcommerce

> File chỉ giữ các hạng mục chưa xong. Các mục đã hoàn tất đã được dọn ngày 2026-05-16.

---

### Quy tắc bắt buộc

- **Unit test**: KHÔNG viết unit test mới, KHÔNG sửa code trong project `*.Test`.
- **Migration**: AI KHÔNG tự chạy migration — báo Tuấn tự chạy.
- **Skills**: AI đọc skill `namcommerce` trước khi viết code domain.
- **Comments**: chỉ viết khi giải thích WHY không hiển nhiên.

---

### Ghi chú thực hiện

- Không viết/sửa unit test theo rule hiện tại.
- Mỗi mục cần build verify + manual smoke test nếu có UI/workflow.
- AI cập nhật checkbox `[x]` ngay khi xong từng mục, đính ngày `✅ YYYY-MM-DD`.

---

## Feature: Direct-Ship Workflow (Giao thẳng NCC → khách)

> Plan chi tiết: [`docs/DIRECT_SHIP_PLAN.md`](docs/DIRECT_SHIP_PLAN.md) — APPROVED, refactored sau code review 2026-05-16.
> Extend `PurchaseOrderItemAllocation` từ feature Shortage đã hoàn tất.
> Nguyên tắc chính: không tạo flow nhận hàng song song, không double-count allocation received, reuse lifecycle `DeliveryNoteStatus` hiện có.

### Phase D1 — Domain extension

- [x] **D1.1** — Add enum `WarehouseType` (Physical, DirectShipTransit) trong Domain. ✅ 2026-05-16
- [x] **D1.2** — Add column `WarehouseType` vào entity `Warehouse` (default Physical). ✅ 2026-05-16 (đã có sẵn)
- [x] **D1.3** — Extend `PurchaseOrderItemAllocation`: thêm 5 field direct-ship + enum `AllocationStatus` mở rộng (Allocated, PartiallyReceived, FullyReceived, DeliveryPending, DeliveryConfirmed, Cancelled). ✅ 2026-05-16
- [x] **D1.4** — Domain validation: `IsDirectShip = true` → `DirectShipAddress` bắt buộc. ✅ 2026-05-16
- [x] **D1.5** — Extend `DeliveryNote`: `DeliveryConfirmationStatus`, `IsDirectShip`, `ConfirmedAt`, `ConfirmedNote`, `SourceGoodsReceiptId`. ✅ 2026-05-16
- [x] **D1.6** — Entity mới `DirectShipAddressChangeLog` (audit log edit địa chỉ). ✅ 2026-05-16
- [x] **D1.7** — Interface `IDirectShipManager` + skeleton methods (chưa implement). ✅ 2026-05-16
- [x] **D1.8** — Domain events: 7 events ở mục 3.5 plan. ✅ 2026-05-16

### Phase D2 — Domain services

- [x] **D2.1** — Implement `DirectShipManager.MarkAllocationAsDirectShipAsync`. ✅ 2026-05-16
- [x] **D2.2** — Implement `DirectShipManager.DistributeReceivedQuantityAsync` (sort theo IsDirectShip desc + Priority desc + CreatedAt asc, phân chia ưu tiên direct-ship). ✅ 2026-05-17
- [x] **D2.3** — Implement `DirectShipManager.ConfirmDeliveryAsync` + `RejectDeliveryAsync` (giá vốn = giá PO). ✅ 2026-05-17
- [x] **D2.4** — `IDirectShipAppService` + 4 methods. ✅ 2026-05-17
- [x] **D2.5** — Extend `IPurchaseOrderAppService` nhận `DirectShipInfo` per item + method `UpdateAllocationDirectShipInfoAsync`. ✅ 2026-05-17
- [x] **D2.6** — Extend `IGoodsReceiptAppService.ReceiveAsync` invoke `DistributeReceivedQuantityAsync`, auto-tạo DN PendingConfirmation cho direct-ship. ✅ 2026-05-17
- [x] **D2.7** — Handler cho `SoCancelledWithDirectShipReceivedEvent`: chuyển stock kho ảo → kho chính khi user confirm. ✅ 2026-05-17 (skeleton — stock transfer impl khi D3 xong)
- [x] **D2.8** — Handler cho `DirectShipDeliveryRejectedEvent`: stock kho ảo → kho chính với giá vốn = giá PO. ✅ 2026-05-17 (skeleton — stock transfer impl khi D3 xong)

### Phase D3 — Application layer + receive-flow integration

- [ ] **D3.1** — `IDirectShipAppService`: update direct-ship info, confirm receipt, reject receipt, list pending deliveries.
- [ ] **D3.2** — Extend shortage create/merge DTOs và `IPurchaseOrderAppService` để nhận `DirectShipInfo` per item/allocation.
- [ ] **D3.3** — Relax validation receive/bulk receive: thêm `AcceptOversupply`, lần đầu trả `Error.PurchaseOrderOversupplyRequiresConfirmation` nếu NCC giao thừa.
- [ ] **D3.4** — Hook `PurchaseOrderItemReceivedEventHandler` sau `SyncReceivedForPurchaseOrderItemAsync` để gọi `IDirectShipManager.OnAllocationReceivedAsync` cho allocation direct-ship vừa receive.
- [ ] **D3.5** — Thêm `IGoodsReceiptManager.CreateFreeStockFromOversupplyAsync` cho phần NCC giao thừa được user chấp nhận.
- [ ] **D3.6** — `OrderAppService.CancelAsync`: detect received direct-ship allocation, trả flag cho UI; khi user confirm thì gọi `HandleSoCancelledForReceivedDirectShipAsync`.

### Phase D4 — Migration, seed, verify

- [ ] **D4.1** — Tuấn chạy `Add-Migration AddDirectShipFieldsToAllocationAndDeliveryNote` + `Update-Database`.
- [ ] **D4.2** — Seed Warehouse "Direct-Ship Transit" với `WarehouseType = DirectShipTransit`.
- [ ] **D4.3** — Build verify.

### Phase D5 — Print service

- [x] **D5.1** — Mỗi item card NCC: checkbox "Giao thẳng tới khách" + inline form (Address/Contact/Phone/Priority). ✅ 2026-05-17
- [x] **D5.2** — Default Address/Contact = info khách trong SO khi tích checkbox. ✅ 2026-05-17
- [x] **D5.3** — Badge xanh "Giao thẳng" + icon truck-fast cho item đã tích. ✅ 2026-05-17
- [x] **D5.4** — Footer summary: "X items giao thẳng / Y items giao về kho". ✅ 2026-05-17
- [x] **D5.5** — Submission flow: gửi `DirectShipInfo` per item lên backend. ✅ 2026-05-17

### Phase D6 — UI Shortage Aggregation

- [ ] **D6.1** — Mỗi item card NCC: checkbox "Giao thẳng tới khách" + inline form (Address/Contact/Phone/Priority).
- [ ] **D6.2** — Default Address/Contact = info khách trong SO khi tích checkbox.
- [ ] **D6.3** — Badge xanh "Giao thẳng" + icon truck-fast cho item đã tích.
- [ ] **D6.4** — Footer summary: "X items giao thẳng / Y items giao về kho".
- [ ] **D6.5** — Submission flow: gửi `DirectShipInfo` per item/action lên backend.

- [x] **D6.1** — Menu mới: Bán hàng → Giao hàng trực tiếp NCC. ✅ 2026-05-17
- [x] **D6.2** — Trang `/DirectShipDelivery/Pending` với filter (khách, keyword, ngày). ✅ 2026-05-17
- [x] **D6.3** — Modal Confirm Delivery: note + ngày confirm. ✅ 2026-05-17
- [x] **D6.4** — Modal Reject Delivery: reason bắt buộc + cảnh báo hàng chuyển về kho chính. ✅ 2026-05-17
- [x] **D6.5** — Auto refresh sau action (row fade-out sau confirm/reject). ✅ 2026-05-17

- [ ] **D7.1** — Menu mới: Bán hàng → Giao hàng trực tiếp NCC.
- [ ] **D7.2** — Trang `/DirectShipDeliveries/Pending` với filter (NCC, khách, ngày).
- [ ] **D7.3** — Query pending = `DeliveryNote.Status = Confirmed` + `SourceType = DirectShipToCustomer`.
- [ ] **D7.4** — Modal Confirm: note + ngày confirm → DN `Delivered`.
- [ ] **D7.5** — Modal Reject: reason bắt buộc + cảnh báo hàng chuyển về kho chính.
- [ ] **D7.6** — Auto refresh sau action.

- [x] **D7.1** — SO Details: tab "Direct-ship status" list allocation + status + link DN. ✅ 2026-05-17
- [x] **D7.2** — DN Details: banner highlight nếu `IsDirectShip = true`. ✅ 2026-05-17
- [x] **D7.3** — Button Confirm/Reject trên DN Details nếu PendingConfirmation. ✅ 2026-05-17
- [x] **D7.4** — Cancel SO flow: detect allocation `FullyReceived` → modal cảnh báo chuyển kho. ✅ 2026-05-17
- [x] **D7.5** — Submit cancel SO → trigger `SoCancelledWithDirectShipReceivedEvent`. ✅ 2026-05-17

- [ ] **D8.1** — SO Details: tab "Direct-ship status" list allocation + derived receipt status + DN status + link DN.
- [ ] **D8.2** — DN Details: banner highlight nếu `SourceType = DirectShipToCustomer` + link PO/GR nguồn.
- [ ] **D8.3** — Button Confirm/Reject trên DN Details nếu `Status = Confirmed` và `SourceType = DirectShipToCustomer`.
- [ ] **D8.4** — Cancel SO flow: detect received direct-ship allocation → modal cảnh báo chuyển kho.
- [ ] **D8.5** — Submit cancel SO sau user confirm → transfer kho ảo → kho chính + cancel DN nếu cần.

- [x] **D8.1** — PO Details allocation list: button "Sửa địa chỉ giao" + modal edit (Address/Contact/Phone). ✅ 2026-05-17
- [x] **D8.2** — Save edit → ghi `DirectShipAddressChangeLog` + raise event `DirectShipAddressUpdatedEvent`. ✅ 2026-05-17
- [x] **D8.3** — Banner cảnh báo "PO đã có phiếu cũ — gửi lại phiếu mới cho NCC" sau khi edit. ✅ 2026-05-17
- [x] **D8.4** — GR Confirm screen: detect `receivedQty > orderedQty` → modal 3 lựa chọn (Nhập kho chính / Từ chối / Hủy GR). ✅ 2026-05-17
- [x] **D8.5** — Backend xử lý "Nhập kho chính" cho phần thừa: stock-in kho chính + tăng công nợ NCC giá PO. ✅ 2026-05-17

- [ ] **D9.1** — PO Details allocation list: button "Sửa địa chỉ giao" + modal edit (Address/Contact/Phone/Reason).
- [ ] **D9.2** — Save edit → ghi `DirectShipAddressChangeLog` + raise `AllocationDirectShipInfoUpdatedEvent`.
- [ ] **D9.3** — Banner cảnh báo "PO đã có phiếu cũ — gửi lại phiếu mới cho NCC" sau khi edit.
- [ ] **D9.4** — Receive/BulkReceive UI: detect error `Error.PurchaseOrderOversupplyRequiresConfirmation` → modal 3 lựa chọn (Nhập kho chính / Từ chối / Hủy GR).
- [ ] **D9.5** — "Nhập kho chính": submit lại với `AcceptOversupply = true`; backend tạo GR free stock + tăng công nợ NCC giá PO.
- [ ] **D9.6** — "Từ chối": submit lại với `ReceivedQuantity = QuantityOrdered`; "Hủy GR": đóng modal, không submit.

### Phase D10 — Báo cáo direct-ship

- [ ] **D10.1** — BC direct-ship theo NCC (tháng/quý/năm).
- [ ] **D10.2** — BC direct-ship theo khách (top khách).
- [ ] **D10.3** — BC direct-ship theo SP.
- [ ] **D10.4** — BC Pending Confirmation > 7 ngày: `Status=Confirmed AND SourceType=DirectShipToCustomer`.
- [ ] **D10.5** — BC tỷ lệ Reject Delivery + lý do (`Cancelled`, source direct-ship).
- [ ] **D10.6** — Menu Báo cáo → Direct-Ship.

### Phase D11 — Manual smoke checklist + build verify

- [x] **D11.1** — Tạo `docs/DIRECT_SHIP_SMOKE_CHECKLIST.md` theo phụ lục D của plan. ✅ 2026-05-17
- [ ] **D11.2** — Scenario happy path: SO 30 + PO 100 direct-ship → GR 100 vào kho chính → Transfer 30 sang DirectShipTransit → DN Confirmed → Confirm → DN Delivered → `CustomerDebt`.
- [ ] **D11.3** — Scenario giao thiếu: NCC giao 80/100 → direct-ship đủ 30 trước, 50 về kho chính.
- [ ] **D11.4** — Scenario giao thừa: NCC giao 110 → modal → chọn "Nhập kho chính" → PO received tối đa 100, GR free stock 10 + `VendorDebt`.
- [ ] **D11.5** — Scenario Reject DN: DN Confirmed → Reject → DN Cancelled → hàng kho ảo về kho chính, giá vốn theo PO.
- [ ] **D11.6** — Scenario Cancel SO sau GR: modal cảnh báo → confirm → hàng kho ảo về kho chính → SO Cancelled.
- [ ] **D11.7** — Scenario edit địa chỉ: audit log + banner gửi lại phiếu NCC.
- [ ] **D11.8** — Scenario N-N allocation: 1 SO chia 2 PO, direct-ship + thường, SO Details hiển thị đủ DN.
- [x] **D11.9** — Build verify sau khi hoàn tất implementation. ✅ 2026-05-17

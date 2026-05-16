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

> Plan chi tiết: [`docs/DIRECT_SHIP_PLAN.md`](docs/DIRECT_SHIP_PLAN.md) — APPROVED 2026-05-16.
> Extend `PurchaseOrderItemAllocation` từ feature Shortage đã hoàn tất.

### Phase D1 — Domain extension

- [ ] **D1.1** — Add enum `WarehouseType` (Physical, DirectShipTransit) trong Domain.
- [ ] **D1.2** — Add column `WarehouseType` vào entity `Warehouse` (default Physical).
- [ ] **D1.3** — Extend `PurchaseOrderItemAllocation`: thêm 5 field direct-ship + enum `AllocationStatus` mở rộng (Allocated, PartiallyReceived, FullyReceived, DeliveryPending, DeliveryConfirmed, Cancelled).
- [ ] **D1.4** — Domain validation: `IsDirectShip = true` → `DirectShipAddress` bắt buộc.
- [ ] **D1.5** — Extend `DeliveryNote`: `DeliveryConfirmationStatus`, `IsDirectShip`, `ConfirmedAt`, `ConfirmedNote`, `SourceGoodsReceiptId`.
- [ ] **D1.6** — Entity mới `DirectShipAddressChangeLog` (audit log edit địa chỉ).
- [ ] **D1.7** — Interface `IDirectShipManager` + skeleton methods (chưa implement).
- [ ] **D1.8** — Domain events: 7 events ở mục 3.5 plan.

### Phase D2 — Application layer + GR integration

- [ ] **D2.1** — Implement `DirectShipManager.MarkAllocationAsDirectShipAsync`.
- [ ] **D2.2** — Implement `DirectShipManager.DistributeReceivedQuantityAsync` (sort theo IsDirectShip desc + Priority desc + CreatedAt asc, phân chia ưu tiên direct-ship).
- [ ] **D2.3** — Implement `DirectShipManager.ConfirmDeliveryAsync` + `RejectDeliveryAsync` (giá vốn = giá PO).
- [ ] **D2.4** — `IDirectShipAppService` + 4 methods.
- [ ] **D2.5** — Extend `IPurchaseOrderAppService` nhận `DirectShipInfo` per item + method `UpdateAllocationDirectShipInfoAsync`.
- [ ] **D2.6** — Extend `IGoodsReceiptAppService.ReceiveAsync` invoke `DistributeReceivedQuantityAsync`, auto-tạo DN PendingConfirmation cho direct-ship.
- [ ] **D2.7** — Handler cho `SoCancelledWithDirectShipReceivedEvent`: chuyển stock kho ảo → kho chính khi user confirm.
- [ ] **D2.8** — Handler cho `DirectShipDeliveryRejectedEvent`: stock kho ảo → kho chính với giá vốn = giá PO.

### Phase D3 — Migration & seed

- [ ] **D3.1** — Tuấn chạy `Add-Migration AddDirectShipFieldsToAllocationAndDeliveryNote` + `Update-Database`.
- [ ] **D3.2** — Seed Warehouse "Direct-Ship Transit" với `WarehouseType = DirectShipTransit`.
- [ ] **D3.3** — Build verify (Tuấn chạy `dotnet build`).

### Phase D4 — Print service

- [ ] **D4.1** — `IPurchaseOrderPrintService.GenerateVendorDeliveryInstructionAsync(poId)` — split section direct-ship + section về kho.
- [ ] **D4.2** — Template HTML/PDF cho phiếu giao NCC.
- [ ] **D4.3** — Action controller `/PurchaseOrders/PrintVendorDelivery/{id}` trả file.
- [ ] **D4.4** — Button "In phiếu giao hàng cho NCC" trên PO Details.

### Phase D5 — UI Shortage Aggregation (extend trang đã có)

- [ ] **D5.1** — Mỗi item card NCC: checkbox "Giao thẳng tới khách" + inline form (Address/Contact/Phone/Priority).
- [ ] **D5.2** — Default Address/Contact = info khách trong SO khi tích checkbox.
- [ ] **D5.3** — Badge xanh "Giao thẳng" + icon truck-fast cho item đã tích.
- [ ] **D5.4** — Footer summary: "X items giao thẳng / Y items giao về kho".
- [ ] **D5.5** — Submission flow: gửi `DirectShipInfo` per item lên backend.

### Phase D6 — UI Pending Deliveries

- [ ] **D6.1** — Menu mới: Bán hàng → Giao hàng trực tiếp NCC.
- [ ] **D6.2** — Trang `/DirectShipDeliveries/Pending` với filter (NCC, khách, ngày).
- [ ] **D6.3** — Modal Confirm Delivery: note + ngày confirm.
- [ ] **D6.4** — Modal Reject Delivery: reason bắt buộc + cảnh báo hàng chuyển về kho chính.
- [ ] **D6.5** — Auto refresh sau action.

### Phase D7 — UI SO Details + DN Details + Cancel flow

- [ ] **D7.1** — SO Details: tab "Direct-ship status" list allocation + status + link DN.
- [ ] **D7.2** — DN Details: banner highlight nếu `IsDirectShip = true` + link PO + GR nguồn.
- [ ] **D7.3** — Button Confirm/Reject trên DN Details nếu PendingConfirmation.
- [ ] **D7.4** — Cancel SO flow: detect allocation `FullyReceived` → modal cảnh báo chuyển kho.
- [ ] **D7.5** — Submit cancel SO → trigger `SoCancelledWithDirectShipReceivedEvent`.

### Phase D8 — UI edit địa chỉ + popup giao thừa

- [ ] **D8.1** — PO Details allocation list: button "Sửa địa chỉ giao" + modal edit (Address/Contact/Phone).
- [ ] **D8.2** — Save edit → ghi `DirectShipAddressChangeLog` + raise event `DirectShipAddressUpdatedEvent`.
- [ ] **D8.3** — Banner cảnh báo "PO đã có phiếu cũ — gửi lại phiếu mới cho NCC" sau khi edit.
- [ ] **D8.4** — GR Confirm screen: detect `receivedQty > orderedQty` → modal 3 lựa chọn (Nhập kho chính / Từ chối / Hủy GR).
- [ ] **D8.5** — Backend xử lý "Nhập kho chính" cho phần thừa: stock-in kho chính + tăng công nợ NCC giá PO.

### Phase D9 — Báo cáo direct-ship

- [ ] **D9.1** — BC direct-ship theo NCC (tháng/quý/năm).
- [ ] **D9.2** — BC direct-ship theo khách (top khách).
- [ ] **D9.3** — BC direct-ship theo SP.
- [ ] **D9.4** — BC Pending Confirmation > 7 ngày (alert).
- [ ] **D9.5** — BC tỷ lệ Reject Delivery + lý do.
- [ ] **D9.6** — Menu Báo cáo → Direct-Ship.

### Phase D10 — E2E smoke test

- [ ] **D10.1** — Scenario 1: SO 30 bao + PO 100 bao + 30 direct-ship → GR đúng 100 → 70 vào kho chính, 30 vào kho ảo, auto-tạo DN Pending → Confirm → HĐ xuất.
- [ ] **D10.2** — Scenario 2: NCC giao thiếu (80/100) → ưu tiên đủ 30 direct-ship, 50 vào kho chính.
- [ ] **D10.3** — Scenario 3: NCC giao thừa (110/100) → popup 3 lựa chọn → chọn nhập kho chính.
- [ ] **D10.4** — Scenario 4: Reject DN → hàng chuyển về kho chính với giá PO.
- [ ] **D10.5** — Scenario 5: Cancel SO sau khi GR → modal cảnh báo → chuyển kho.
- [ ] **D10.6** — Scenario 6: Edit địa chỉ direct-ship sau PO confirm → audit log + cảnh báo tái in.


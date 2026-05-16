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

- [ ] **D1.1** — Append enum value `WarehouseType.DirectShipTransit` (giữ nguyên `Main`, `SubWarehouse`, `ReturnWarehouse`).
- [ ] **D1.2** — Append enum value `DeliveryNoteSourceType.DirectShipToCustomer = 3`.
- [ ] **D1.3** — Extend `PurchaseOrderItemAllocation`: thêm `IsDirectShip`, `DirectShipAddress`, `DirectShipContactName`, `DirectShipContactPhone`, `DirectShipPriority` (KHÔNG thêm allocation status enum).
- [ ] **D1.4** — Domain validation: `IsDirectShip = true` → `DirectShipAddress` bắt buộc.
- [ ] **D1.5** — Bổ sung link audit cần thiết cho DN direct-ship, ưu tiên `SourcePurchaseOrderItemId` nếu chưa có field phù hợp.
- [ ] **D1.6** — Entity mới `DirectShipAddressChangeLog` (audit log edit địa chỉ).
- [ ] **D1.7** — Interface `IDirectShipManager` + skeleton orchestration methods.
- [ ] **D1.8** — Domain events tối thiểu: `AllocationDirectShipInfoUpdatedEvent`, `DirectShipDeliveryNoteCreatedEvent`, `VendorOversupplyAcceptedEvent`.

### Phase D2 — Domain services

- [ ] **D2.1** — Implement `IInventoryStockManager.TransferStockAsync`: chuyển hàng giữa 2 warehouse, ghi 2 movement log type `Transfer`, bảo toàn `unitCost`.
- [ ] **D2.2** — Refactor `PurchaseOrderAllocationManager.SyncReceivedForPurchaseOrderItemAsync`: sort `IsDirectShip desc`, `DirectShipPriority desc`, `CreatedOnUtc asc`.
- [ ] **D2.3** — Đảm bảo `SyncReceivedForPurchaseOrderItemAsync` là nơi DUY NHẤT cộng `ReceivedQuantity` vào allocation.
- [ ] **D2.4** — Implement `DirectShipManager.UpdateAllocationDirectShipInfoAsync` + ghi `DirectShipAddressChangeLog`.
- [ ] **D2.5** — Implement `DirectShipManager.OnAllocationReceivedAsync`: tạo DN `Status=Confirmed`, `SourceType=DirectShipToCustomer`, warehouse `DirectShipTransit`, rồi transfer kho chính → kho ảo.
- [ ] **D2.6** — Implement `DirectShipManager.ConfirmCustomerReceiptAsync`: chuyển DN sang `Delivered` để handler hiện có sinh `CustomerDebt`.
- [ ] **D2.7** — Implement `DirectShipManager.RejectCustomerReceiptAsync`: DN → `Cancelled` + transfer kho ảo → kho chính, giá vốn = giá PO.
- [ ] **D2.8** — Implement `DirectShipManager.HandleSoCancelledForReceivedDirectShipAsync`: SO hủy sau khi đã received thì transfer kho ảo → kho chính + cancel DN nếu còn.

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

- [ ] **D5.1** — `IPurchaseOrderPrintService.GenerateVendorDeliveryInstructionAsync(poId)` — split section direct-ship + section về kho.
- [ ] **D5.2** — Template HTML/PDF cho phiếu giao NCC.
- [ ] **D5.3** — Action controller `/PurchaseOrders/PrintVendorDelivery/{id}` trả file.
- [ ] **D5.4** — Button "In phiếu giao hàng cho NCC" trên PO Details.

### Phase D6 — UI Shortage Aggregation

- [ ] **D6.1** — Mỗi item card NCC: checkbox "Giao thẳng tới khách" + inline form (Address/Contact/Phone/Priority).
- [ ] **D6.2** — Default Address/Contact = info khách trong SO khi tích checkbox.
- [ ] **D6.3** — Badge xanh "Giao thẳng" + icon truck-fast cho item đã tích.
- [ ] **D6.4** — Footer summary: "X items giao thẳng / Y items giao về kho".
- [ ] **D6.5** — Submission flow: gửi `DirectShipInfo` per item/action lên backend.

### Phase D7 — UI Pending Deliveries

- [ ] **D7.1** — Menu mới: Bán hàng → Giao hàng trực tiếp NCC.
- [ ] **D7.2** — Trang `/DirectShipDeliveries/Pending` với filter (NCC, khách, ngày).
- [ ] **D7.3** — Query pending = `DeliveryNote.Status = Confirmed` + `SourceType = DirectShipToCustomer`.
- [ ] **D7.4** — Modal Confirm: note + ngày confirm → DN `Delivered`.
- [ ] **D7.5** — Modal Reject: reason bắt buộc + cảnh báo hàng chuyển về kho chính.
- [ ] **D7.6** — Auto refresh sau action.

### Phase D8 — UI SO Details + DN Details + Cancel flow

- [ ] **D8.1** — SO Details: tab "Direct-ship status" list allocation + derived receipt status + DN status + link DN.
- [ ] **D8.2** — DN Details: banner highlight nếu `SourceType = DirectShipToCustomer` + link PO/GR nguồn.
- [ ] **D8.3** — Button Confirm/Reject trên DN Details nếu `Status = Confirmed` và `SourceType = DirectShipToCustomer`.
- [ ] **D8.4** — Cancel SO flow: detect received direct-ship allocation → modal cảnh báo chuyển kho.
- [ ] **D8.5** — Submit cancel SO sau user confirm → transfer kho ảo → kho chính + cancel DN nếu cần.

### Phase D9 — UI edit địa chỉ + popup giao thừa

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

- [ ] **D11.1** — Tạo/ cập nhật `docs/DIRECT_SHIP_SMOKE_CHECKLIST.md` theo phụ lục D của plan, KHÔNG viết test code trong project `*.Test`.
- [ ] **D11.2** — Scenario happy path: SO 30 + PO 100 direct-ship → GR 100 vào kho chính → Transfer 30 sang DirectShipTransit → DN Confirmed → Confirm → DN Delivered → `CustomerDebt`.
- [ ] **D11.3** — Scenario giao thiếu: NCC giao 80/100 → direct-ship đủ 30 trước, 50 về kho chính.
- [ ] **D11.4** — Scenario giao thừa: NCC giao 110 → modal → chọn "Nhập kho chính" → PO received tối đa 100, GR free stock 10 + `VendorDebt`.
- [ ] **D11.5** — Scenario Reject DN: DN Confirmed → Reject → DN Cancelled → hàng kho ảo về kho chính, giá vốn theo PO.
- [ ] **D11.6** — Scenario Cancel SO sau GR: modal cảnh báo → confirm → hàng kho ảo về kho chính → SO Cancelled.
- [ ] **D11.7** — Scenario edit địa chỉ: audit log + banner gửi lại phiếu NCC.
- [ ] **D11.8** — Scenario N-N allocation: 1 SO chia 2 PO, direct-ship + thường, SO Details hiển thị đủ DN.
- [ ] **D11.9** — Build verify sau khi hoàn tất implementation.

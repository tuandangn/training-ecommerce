# Plan — Direct-Ship Workflow (Giao thẳng từ NCC đến khách)

> Trạng thái: **APPROVED — open questions đã chốt 2026-05-16**
> Ngày soạn: 2026-05-16
> Liên hệ: feature "Shortage & Auto-PO" đã hoàn tất, plan này extend `PurchaseOrderItemAllocation` đã có

---

## 1. Bối cảnh nghiệp vụ

Tình huống thực tế tại cửa hàng VLXD:

- Khách đặt 30 bao xi măng.
- Cửa hàng đặt NCC 100 bao.
- Khi NCC giao hàng: 30 bao giao thẳng tới địa chỉ khách, 70 bao về kho cửa hàng.
- Cửa hàng ký nhận đủ 100 bao với NCC.

Hệ thống hiện chưa hỗ trợ workflow này — đang phải tách thành nhập 100 bao về kho rồi xuất 30 bao đi, không phản ánh đúng vật lý và tốn thao tác.

## 2. Quyết định thiết kế đã chốt

| # | Quyết định | Lý do |
|---|------------|-------|
| 1 | Mở rộng `PurchaseOrderItemAllocation` (entity đã có ở Shortage feature) thay vì tạo entity mới | Tái sử dụng infrastructure đã build, ít rủi ro hơn |
| 2 | Khi NCC giao thiếu → **ưu tiên đủ cho khách trước**, phần dư về kho | Giữ uy tín với khách quan trọng hơn cân bằng tồn kho |
| 3 | Support **N-N** giữa SO Item và PO Item (đã có sẵn) | Cho phép 1 đơn lớn gộp nhiều NCC, 1 PO chia nhiều khách |
| 4 | DN direct-ship cần bước **Confirm Delivery** mới xuất hóa đơn VAT | Tránh rủi ro xuất HĐ khi khách chưa nhận đủ |
| 5 | Dùng 1 **Warehouse ảo** "Direct-Ship Transit" để stock di chuyển qua | Sạch về kế toán + audit trail, mà không làm sai tồn kho thật |

## 3. Thay đổi Domain

### 3.1. Extend `PurchaseOrderItemAllocation`

Thêm fields:

- `IsDirectShip` (bool, default false)
- `DirectShipAddress` (string, max 500, nullable)
- `DirectShipContactName` (string, max 200, nullable)
- `DirectShipContactPhone` (string, max 50, nullable)
- `DirectShipPriority` (int, default 0) — số càng cao càng ưu tiên khi NCC giao thiếu
- `AllocationStatus` enum mở rộng: `Allocated`, `PartiallyReceived`, `FullyReceived`, `DeliveryPending`, `DeliveryConfirmed`, `Cancelled`

Constraint domain:
- Nếu `IsDirectShip = true` thì `DirectShipAddress` bắt buộc không null/empty.

### 3.2. Bổ sung Warehouse Virtual

- `Warehouse` thêm cột `WarehouseType` enum: `Physical` (mặc định), `DirectShipTransit`, `ReturnTransit` (mở rộng tương lai).
- Seed 1 record `Warehouse` tên "Direct-Ship Transit", `WarehouseType = DirectShipTransit`, không hiển thị trong dropdown chọn kho bình thường.
- Toàn bộ logic kiểm kê/tồn kho cuối kỳ filter `WarehouseType = Physical`.

### 3.3. Extend `DeliveryNote`

- `DeliveryConfirmationStatus` enum: `NotApplicable` (default cho DN thường), `PendingConfirmation`, `Confirmed`, `Rejected`
- `IsDirectShip` (bool, default false)
- `ConfirmedAt` (DateTime UTC, nullable)
- `ConfirmedNote` (string, nullable) — ghi chú khi confirm/reject
- `SourceGoodsReceiptId` (FK, nullable) — link tới GR đã tạo ra DN này (khác với DN bình thường tạo từ SO)

### 3.4. Domain Manager mới: `IDirectShipManager`

Methods chính:

```
Task<Result> MarkAllocationAsDirectShipAsync(
    Guid allocationId,
    string address, string contactName, string contactPhone, int priority,
    CancellationToken ct);

Task<DistributionResult> DistributeReceivedQuantityAsync(
    Guid purchaseOrderItemId, decimal receivedQty,
    CancellationToken ct);
// Logic: sort allocations theo (IsDirectShip desc, DirectShipPriority desc, CreatedAt asc),
//        chia receivedQty theo thứ tự đó.
//        Trả về 2 nhóm: AllocationReceipts (direct-ship → kho ảo + tạo DN Pending),
//                       WarehouseReceipts (phần dư → kho chính).

Task<Result> ConfirmDeliveryAsync(
    Guid deliveryNoteId, DateTime confirmedAtUtc, string note,
    CancellationToken ct);
// Effect: DN PendingConfirmation → Confirmed,
//         StockMovement: kho ảo "Direct-Ship Transit" out,
//         Trigger event để Invoice/Debt module tạo phải thu.

Task<Result> RejectDeliveryAsync(
    Guid deliveryNoteId, string reason,
    CancellationToken ct);
// Effect: DN PendingConfirmation → Rejected,
//         StockMovement: kho ảo → kho chính (chuyển hàng về kho để bán cho khách khác).
```

### 3.5. Domain Event

- `AllocationMarkedAsDirectShipEvent`
- `DirectShipDeliveryPendingEvent` (raised khi GR sinh DN Pending)
- `DirectShipDeliveryConfirmedEvent` (Invoice/Debt module subscribe)
- `DirectShipDeliveryRejectedEvent` (Inventory module subscribe để di chuyển kho — giá vốn = giá PO)
- `SoCancelledWithDirectShipReceivedEvent` (Inventory subscribe để chuyển hàng kho ảo → kho chính, giá vốn giá PO)
- `DirectShipAddressUpdatedEvent` (audit log + cảnh báo tái in phiếu)
- `VendorOversupplyAcceptedEvent` (raised khi user chọn nhận phần thừa NCC giao)

> Các event này đi qua **Outbox Pattern** đã hoàn thiện Phase 4 (2026-05-06).

### 3.6. Entity audit: `DirectShipAddressChangeLog`

Lưu lịch sử edit địa chỉ giao trên allocation:
- `Id`, `AllocationId` (FK), `OldAddress`, `NewAddress`, `OldContactName`, `NewContactName`, `OldContactPhone`, `NewContactPhone`, `EditedByUserId`, `EditedAt` (UTC), `Reason` (nullable string user nhập).

## 4. Thay đổi Application Layer

### 4.1. `IDirectShipAppService`

```
Task<DirectShipAllocationDto> CreateAsync(CreateDirectShipAllocationRequestDto);
Task ConfirmDeliveryAsync(ConfirmDeliveryRequestDto);
Task RejectDeliveryAsync(RejectDeliveryRequestDto);
Task<PagedResult<DirectShipPendingDeliveryDto>> GetPendingDeliveriesAsync(FilterDto);
```

### 4.2. Extend `IPurchaseOrderAppService`

- `CreateAsync` / `AddItemsToExistingDraftAsync` (đã có ở Shortage P6.6): nhận thêm tham số `DirectShipInfo` cho từng item.
- Method mới `UpdateAllocationDirectShipInfoAsync` cho phép sửa địa chỉ giao sau khi PO đã tạo.

### 4.3. Extend `IGoodsReceiptAppService`

- Khi gọi `ReceiveAsync`, sau bước cập nhật tồn kho thường → invoke `IDirectShipManager.DistributeReceivedQuantityAsync`.
- Auto sinh `DeliveryNote` với `DeliveryConfirmationStatus = PendingConfirmation` cho phần direct-ship.
- Trả về `GoodsReceiptResultDto` thêm field `GeneratedDirectShipDeliveryNoteIds` để UI hiển thị link.

### 4.4. Print Service

- `IPurchaseOrderPrintService.GenerateVendorDeliveryInstructionAsync(poId)` — sinh PDF phiếu cho NCC, tách rõ 2 section:
  - **Giao thẳng tới khách**: liệt kê địa chỉ + người liên hệ + SP + qty.
  - **Giao về kho**: kho đích + SP + qty.

## 5. Thay đổi Presentation Layer

### 5.1. Trang Shortage Aggregation (đã có)

- Mỗi item trong card NCC: thêm checkbox **"Giao thẳng tới khách"**.
- Khi tích → mở inline form nhỏ:
  - Address (default = địa chỉ khách trong SO)
  - Contact name + phone (default = info khách)
  - Priority (default = 1)
- Visual: badge xanh "Giao thẳng" + icon truck-fast bên cạnh tên SP.
- Footer summary: hiển thị "X items giao thẳng / Y items giao về kho".

### 5.2. PO Details

- Section "Allocation list" (đã có ở P6.7) bổ sung column "Direct-ship?" với icon + tooltip địa chỉ.
- Button mới **"In phiếu giao hàng cho NCC"** → call print service.

### 5.3. Goods Receipt Confirm screen

- Khi quantity nhận **< quantity PO** (giao thiếu): hiển thị bảng phân bổ ưu tiên trước khi confirm.
  - Bảng cho phép user override thứ tự ưu tiên (drag-drop hoặc edit số priority).
- Khi quantity nhận **> quantity PO** (giao thừa): bật modal "NCC giao thừa Y đơn vị" với 3 lựa chọn:
  - **Nhập kho chính** → phần thừa stock-in kho chính, ghi nhận công nợ NCC tăng theo giá PO.
  - **Từ chối nhận phần thừa** → chỉ ghi nhận quantity PO, phần thừa không vào hệ thống.
  - **Hủy GR** → đóng modal, không lưu gì.

### 5.4. Trang mới: `/DirectShipDeliveries/Pending`

- Menu: Bán hàng → Giao hàng trực tiếp NCC.
- Filter: NCC, khách, ngày tạo, ngày NCC giao.
- Mỗi row: SO code | Khách | NCC | SP | Qty | Địa chỉ giao | Ngày GR | Action [Confirm] [Reject].
- Modal Confirm: nhập note (vd "Khách gọi xác nhận lúc 10h sáng") + ngày confirm.
- Modal Reject: nhập reason bắt buộc, cảnh báo hàng sẽ chuyển về kho chính.

### 5.5. SO Details

- Tab/section "Direct-ship status": liệt kê các allocation direct-ship của SO này + trạng thái + link DN.
- **Khi user bấm Cancel SO** mà có allocation `FullyReceived` đang ở kho ảo: bật modal cảnh báo:
  - "Có X bao đang ở kho ảo Direct-Ship Transit. Chuyển về kho chính khi hủy SO?"
  - User confirm → tạo `StockMovement` từ kho ảo → kho chính (giá vốn = giá PO của allocation), ghi reason "SO cancelled — return to stock".
  - User cancel modal → SO không hủy, allocation giữ nguyên.

### 5.7. Edit địa chỉ direct-ship sau confirm PO

- Trên PO Details + Allocation list: mỗi allocation direct-ship có button **"Sửa địa chỉ giao"**.
- Modal nhỏ cho phép edit Address / Contact name / Contact phone.
- Sau khi save:
  - Audit log lưu `OldAddress` / `NewAddress` / `EditedBy` / `EditedAt`.
  - Tự động re-generate "Phiếu giao hàng cho NCC" và hiển thị banner cảnh báo "PO này đã có phiếu cũ — vui lòng gửi lại phiếu mới cho NCC".

### 5.6. DN Details

- Khi `IsDirectShip = true`: highlight banner, hiển thị PO nguồn + GR nguồn.
- Button Confirm/Reject nếu trạng thái Pending.

## 6. Migration & DB

Tuấn chạy (không phải AI):

```
Add-Migration AddDirectShipFieldsToAllocationAndDeliveryNote
Update-Database
```

Sau migration, chạy seed:
- Insert 1 Warehouse "Direct-Ship Transit" với type = DirectShipTransit.

## 7. Phân chia Phase triển khai

| Phase | Tên | Phụ thuộc | Estimate |
|-------|-----|-----------|----------|
| **D1** | Domain entity + enum + DirectShipManager (stubs) | Sau khi Shortage P6.7.10 done | 1 ngày |
| **D2** | Application service + GR integration | D1 | 1.5 ngày |
| **D3** | Migration + seed (Tuấn chạy) + build verify | D2 | 0.5 ngày |
| **D4** | Print service + phiếu giao NCC | D2 | 1 ngày |
| **D5** | UI Shortage Aggregation — direct-ship checkbox + form | D2 | 1 ngày |
| **D6** | UI Trang Pending Deliveries + Confirm/Reject flow | D2 | 1.5 ngày |
| **D7** | UI SO Details + DN Details direct-ship indicators + flow cancel SO (modal xác nhận chuyển kho) | D2 | 0.75 ngày |
| **D8** | UI edit địa chỉ direct-ship + audit log + popup NCC giao thừa | D2 | 1 ngày |
| **D9** | Báo cáo direct-ship: theo NCC / khách / SP, tỷ lệ direct-ship, pending > 7 ngày | D2 | 1 ngày |
| **D10** | E2E smoke test workflow đầy đủ | D3-D9 | 0.5 ngày |

**Tổng:** ~9.75 ngày work.

## 8. Liên hệ với feature đang dở

- **Phải đợi** Shortage feature P6.7.10 build verify xong (1 mục còn lại trong TodoList).
- Sau đó, plan này tạo phase D1..D8 thay thế dần các mục Shortage còn lại nếu có overlap.
- Branch: tiếp tục `dev-assistant`.

## 9. Quyết định trên Open Questions (chốt 2026-05-16)

1. **Cancel SO khi hàng đã ở kho ảo**: ✅ **Cần user xác nhận** trước khi chuyển về kho chính.
   - Khi user bấm cancel SO mà allocation đã `FullyReceived` → bật modal cảnh báo "Có X bao đang ở kho ảo Direct-Ship Transit, chuyển về kho chính?"
   - User confirm → tạo `StockMovement` từ kho ảo sang kho chính (lý do: "SO cancelled — return to stock"), cập nhật giá vốn = giá PO của allocation.

2. **Reject Delivery — giá vốn**: ✅ **Theo giá PO của allocation đó**, không phải giá bình quân.
   - Lý do: hàng vẫn nguyên gốc từ PO này, không trộn với lô khác → giá vốn phải khớp PO.

3. **Combined Delivery (mixed direct-ship + kho)**: ✅ **Tạo 2 DN riêng**.
   - DN 1: trạng thái `PendingConfirmation`, `IsDirectShip = true`, lấy 20 bao direct-ship.
   - DN 2: trạng thái thường, xuất 10 bao từ kho chính.
   - Hệ thống tự động liên kết cả 2 DN với SO; UI SO Details hiển thị cả 2.

4. **Edit DirectShipAddress sau confirm PO**: ✅ **Cho phép edit**.
   - Endpoint `UpdateAllocationDirectShipInfoAsync` cho phép sửa Address/Contact/Phone của allocation kể cả khi PO đã confirm.
   - Audit log lại mỗi lần edit (who/when/old/new) — quan trọng vì NCC có thể đã in phiếu cũ.
   - Khi edit → re-generate "Phiếu giao hàng cho NCC" tự động + cảnh báo "Hãy gửi lại phiếu mới cho NCC".

5. **NCC giao thừa**: ✅ **Hỏi user mỗi lần**.
   - Khi `receivedQty > orderedQty` trên GR → modal "NCC giao thừa Y đơn vị. Xử lý: [Nhập kho chính] / [Từ chối nhận phần thừa] / [Hủy GR]".
   - Nếu user chọn "Nhập kho chính": phần thừa stock-in kho chính, ghi nhận tăng công nợ NCC tương ứng (giá PO).

6. **Hóa đơn VAT mua từ NCC**: ⏸️ **Ngoài scope phase này** — chưa làm.
   - Workflow xuất HĐ VAT mua hiện chưa được hệ thống hóa.
   - Plan này chỉ focus HĐ VAT bán (cho khách) thông qua Confirm Delivery.
   - Move qua mục 11 (Ngoài scope).

7. **Báo cáo direct-ship**: ✅ **Làm luôn trong scope** — thêm Phase D9.

## 10. Rủi ro

| Rủi ro | Mức | Mitigation |
|--------|-----|------------|
| Phá vỡ flow Shortage Aggregation hiện có khi extend | Cao | Test kỹ scenario P6.6.7 sau khi merge |
| Tính toán phân bổ khi giao thiếu sai logic | Cao | Manual smoke test với 3 scenario (đủ / thiếu vừa / thiếu nhiều) |
| User quên Confirm Delivery → tồn kho ảo phình to | Trung | Báo cáo "Direct-ship pending > 7 ngày" + email reminder (phase sau) |
| Hóa đơn VAT chốt sai timing | Trung | Confirm Delivery workflow đã được thiết kế để tránh, nhưng cần test |
| Migration trên DB production có data: enum default | Thấp | Default `NotApplicable` cho DN cũ, default `false` cho IsDirectShip |

## 11. Ngoài scope (làm sau)

- **Hệ thống hóa hóa đơn VAT mua từ NCC** — workflow mua chưa số hóa, sẽ làm thành feature riêng.
- Tự động phân bổ PO khi tạo Sales Order lớn (gợi ý gộp/chia NCC).
- Tự động liên lạc khách qua SMS/Zalo khi NCC chuẩn bị giao.
- Tracking GPS xe NCC.
- App mobile cho NCC xác nhận đã giao.

---

## Phụ lục A — Sơ đồ trạng thái

```
SO created → Shortage detected → PO created with allocations
                                       │
                                       ▼
                          [Allocation: IsDirectShip=true]
                                       │
                                       ▼
                              NCC giao hàng
                                       │
                          ┌────────────┴────────────┐
                          ▼                         ▼
                  GR phần direct-ship       GR phần kho chính
                  → Kho ảo Transit          → Kho thật
                          │
                          ▼
                  Auto-tạo DN PendingConfirmation
                          │
                  ┌───────┴───────┐
                  ▼               ▼
              Confirm          Reject
                  │               │
                  ▼               ▼
          DN Confirmed       Hàng chuyển về
          + Invoice xuất     kho chính
          + Debt created     + Notify Sales
```

## Phụ lục B — Field mapping nhanh

| Layer | Field cũ | Field mới |
|-------|----------|-----------|
| PurchaseOrderItemAllocation | (đã có) | + IsDirectShip, DirectShipAddress, DirectShipContactName, DirectShipContactPhone, DirectShipPriority, AllocationStatus |
| Warehouse | + WarehouseType |
| DeliveryNote | + DeliveryConfirmationStatus, IsDirectShip, ConfirmedAt, ConfirmedNote, SourceGoodsReceiptId |
| DirectShipAddressChangeLog (new) | (new entity) | Id, AllocationId, OldAddress, NewAddress, OldContactName, NewContactName, OldContactPhone, NewContactPhone, EditedByUserId, EditedAt, Reason |

## Phụ lục C — Báo cáo Direct-Ship (Phase D9)

1. **Báo cáo direct-ship theo NCC**: tháng/quý/năm. Cột: NCC, Số PO, Số bao direct-ship, Số bao về kho, Tỷ lệ direct-ship.
2. **Báo cáo direct-ship theo khách**: top khách nhận hàng giao thẳng. Cột: Khách, Số SO, Số bao, Tổng tiền.
3. **Báo cáo direct-ship theo SP**: SP nào hay direct-ship. Cột: SP, Qty direct-ship, Qty qua kho, Tỷ lệ.
4. **Báo cáo Pending Confirmation quá hạn**: DN PendingConfirmation > 7 ngày — alert cho user.
5. **Báo cáo Reject Delivery**: tỷ lệ DN bị reject + lý do — đánh giá chất lượng giao thẳng.

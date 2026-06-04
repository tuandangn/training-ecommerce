# Plan — Direct-Ship Workflow (Giao thẳng từ NCC đến khách)

> Trạng thái: **APPROVED — refactored sau code review 2026-05-16**
> Ngày soạn: 2026-05-16
> Liên hệ: feature "Shortage & Auto-PO" đã hoàn tất, plan này extend `PurchaseOrderItemAllocation` đã có
> Đã align với code thật (xem AGENTS.md), tránh double-count receive, không phá lifecycle DN/PO hiện có

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

### 3.1. Extend `PurchaseOrderItemAllocation` — CHỈ thêm direct-ship fields

> File: `Domain/Entities/PurchaseOrders/PurchaseOrderItemAllocation.cs`
> Hiện entity chỉ có `AllocatedQuantity`, `ReceivedQuantity`, `CreatedOnUtc` + 2 method `IncreaseReceived` / `ReduceAllocationToReceived`. Status partial/full hoàn toàn derive được từ quantity, **KHÔNG thêm status enum** vào allocation (tránh trộn receipt-status với delivery-status).

Thêm fields:

- `IsDirectShip` (bool, default false)
- `DirectShipAddress` (string, max 500, nullable)
- `DirectShipContactName` (string, max 200, nullable)
- `DirectShipContactPhone` (string, max 50, nullable)
- `DirectShipPriority` (int, default 0) — số càng cao càng ưu tiên khi NCC giao thiếu

Constraint domain:
- Nếu `IsDirectShip = true` thì `DirectShipAddress` bắt buộc không null/empty.

Status được lookup runtime:
- **Receipt status** (Pending / Partial / Full): derive từ `ReceivedQuantity` vs `AllocatedQuantity`.
- **Delivery status** (cho direct-ship): lookup từ `DeliveryNote.Status` của DN liên kết.

### 3.2. `WarehouseType` — chỉ thêm value mới

> File: `Domain.Shared/Enums/Inventory/WarehouseType.cs`
> Enum hiện có: `Main`, `SubWarehouse`, `ReturnWarehouse`. **Giữ nguyên thứ tự**, chỉ append `DirectShipTransit` vào cuối để tránh ảnh hưởng data migration.

```csharp
public enum WarehouseType
{
    Main,
    SubWarehouse,
    ReturnWarehouse,
    DirectShipTransit  // NEW — kho ảo cho hàng direct-ship đang chờ khách confirm
}
```

- Seed 1 record `Warehouse` tên "Direct-Ship Transit", `WarehouseType = DirectShipTransit`, **không hiển thị** trong dropdown chọn kho bình thường (filter theo type).
- Logic kiểm kê / báo cáo tồn cuối kỳ filter `WarehouseType != DirectShipTransit` (hoặc whitelist `Main + SubWarehouse + ReturnWarehouse` tuỳ context hiện tại).

### 3.3. `DeliveryNoteSourceType` — thêm value `DirectShipToCustomer`

> File: `Domain.Shared/Enums/DeliveryNotes/DeliveryNoteSourceType.cs`
> Hiện có: `ToCustomer = 0`, `ToVendorReturn = 1`, `ToAdjustment = 2`. Thêm value mới:

```csharp
DirectShipToCustomer = 3
```

**KHÔNG thêm field `DeliveryConfirmationStatus`.** Tận dụng lifecycle hiện có:
- `DeliveryNoteStatus.Draft (10)` → tạo nháp ngay khi GR ghi nhận direct-ship.
- `DeliveryNoteStatus.Confirmed (20)` → đang chờ khách xác nhận (mặc định status sau khi GR done).
- `DeliveryNoteStatus.Delivered (40)` → khách đã xác nhận nhận đủ → `DeliveryNoteDeliveredEventHandler` tự sinh `CustomerDebt` (timing chuẩn cho HĐ VAT).
- `DeliveryNoteStatus.Cancelled (50)` → khách từ chối / SO huỷ.

Bonus field cần thiết (nếu chưa có): `SourcePurchaseOrderItemId` (FK, nullable) — link tới `PurchaseOrderItem` đã sinh ra DN này (audit). Có thể thêm bằng cách reuse field hiện có nếu phù hợp, **xác minh khi đụng code D1**.

> Filter "Pending Direct-Ship Confirmation" = `Status = Confirmed AND SourceType = DirectShipToCustomer`.

### 3.4. `IInventoryStockManager` — thêm `TransferStockAsync`

> File: `Domain.Shared/Services/Inventory/IInventoryStockManager.cs`
> Hiện có: `ReceiveStockAsync`, `RevertReceiveAsync`, `ReserveStockAsync`, `ReleaseReservedStockAsync`, `DispatchStockAsync`. **KHÔNG có** method transfer giữa 2 warehouse.
> `StockMovementType.Transfer` đã có trong enum (`StockMovementLog.cs:47`).

```csharp
/// <summary>
/// Chuyển hàng giữa 2 warehouse trong cùng 1 product.
/// Ghi 2 log: Outbound từ fromWarehouseId + Inbound vào toWarehouseId (movementType = Transfer).
/// Bảo toàn AverageCost: vào kho đích với UnitCost được truyền (= giá PO khi reject direct-ship).
/// </summary>
Task<(StockMovementLogDto? OutLog, StockMovementLogDto? InLog)> TransferStockAsync(
    Guid productId,
    Guid fromWarehouseId,
    Guid toWarehouseId,
    decimal quantity,
    decimal unitCost,
    Guid? referenceId,
    Guid userId,
    string? note = null);
```

Dùng bởi:
- Reject DeliveryNote → transfer kho ảo → kho chính, unitCost = giá PO của allocation.
- Cancel SO khi allocation `FullyReceived` → transfer kho ảo → kho chính, unitCost = giá PO của allocation.

### 3.5. Refactor `PurchaseOrderAllocationManager.SyncReceivedForPurchaseOrderItemAsync`

> File: `Domain.Services/PurchaseOrders/PurchaseOrderAllocationManager.cs:85`
> Hiện sort `OrderBy(CreatedOnUtc)` (FIFO thuần). Đổi sang **direct-ship priority FIFO**:

```csharp
var allocations = allocationReader.DataSource
    .Where(a => a.PurchaseOrderItemId == purchaseOrderItemId)
    .OrderByDescending(a => a.IsDirectShip)       // direct-ship trước
    .ThenByDescending(a => a.DirectShipPriority)  // priority cao trước
    .ThenBy(a => a.CreatedOnUtc)                  // sau cùng FIFO
    .ToList();
```

Quan trọng: **đây là method DUY NHẤT phân bổ received quantity vào allocations.** Không tạo manager mới chạy song song → tránh double-count.

Sau khi `IncreaseReceivedAsync` cho 1 allocation direct-ship: thêm hook gọi `IDirectShipManager.OnAllocationReceivedAsync(allocation, receivedDelta)` để orchestration tạo DN draft + transfer kho ảo (xem 3.6).

### 3.6. Domain Manager mới: `IDirectShipManager` (chỉ orchestration)

> File: `Domain.Services/DirectShip/DirectShipManager.cs` (mới)
> **KHÔNG tự phân bổ received quantity** (đã có `SyncReceivedForPurchaseOrderItemAsync` lo). Chỉ làm orchestration:

```csharp
// Set/update direct-ship info cho allocation (kèm audit log)
Task UpdateAllocationDirectShipInfoAsync(
    Guid allocationId, string address, string contactName, string contactPhone,
    int priority, Guid editedByUserId, string? reason);

// Hook gọi từ SyncReceivedForPurchaseOrderItemAsync khi 1 allocation direct-ship vừa
// IncreaseReceived. Tạo DN status=Confirmed, source=DirectShipToCustomer, warehouse=DirectShipTransit
// (chưa Delivered → CustomerDebt chưa sinh, chờ khách confirm).
// Transfer hàng từ warehouse chính (vừa nhập) sang DirectShipTransit qua IInventoryStockManager.
Task OnAllocationReceivedAsync(Guid allocationId, decimal receivedDelta);

// Khách xác nhận: chuyển DN sang Delivered → DeliveryNoteDeliveredEventHandler tự sinh CustomerDebt.
Task ConfirmCustomerReceiptAsync(Guid deliveryNoteId, DateTime confirmedAtUtc, string? note);

// Khách từ chối: DN → Cancelled + Transfer kho ảo → kho chính (giá PO).
Task RejectCustomerReceiptAsync(Guid deliveryNoteId, string reason);

// SO huỷ trong khi allocation đã FullyReceived: Transfer kho ảo → kho chính (giá PO),
// cancel DN nếu còn.
Task HandleSoCancelledForReceivedDirectShipAsync(Guid orderId);
```

### 3.7. Domain Events

- `AllocationDirectShipInfoUpdatedEvent` (audit log: who/when/old/new).
- `DirectShipDeliveryNoteCreatedEvent` (sau khi `OnAllocationReceivedAsync` tạo DN draft) — optional, dùng để notify user "có 1 DN chờ confirm".
- `VendorOversupplyAcceptedEvent` (user chấp nhận phần thừa NCC giao — sinh GR riêng cho free stock + VendorDebt).

> KHÔNG cần event riêng cho "DirectShipConfirmed" / "DirectShipRejected" — đã có `DeliveryNoteDelivered` / `DeliveryNoteCancelled` raised từ status lifecycle, và `DeliveryNoteDeliveredEventHandler` đã sinh `CustomerDebt`.
> Các event đi qua **Outbox Pattern** đã hoàn thiện Phase 4 (2026-05-06).

### 3.8. Entity mới: `DirectShipAddressChangeLog`

Lưu lịch sử edit địa chỉ giao trên allocation:
- `Id`, `AllocationId` (FK), `OldAddress`, `NewAddress`, `OldContactName`, `NewContactName`, `OldContactPhone`, `NewContactPhone`, `EditedByUserId`, `EditedAt` (UTC), `Reason` (nullable string user nhập).

## 4. Thay đổi Application Layer

> Entry point nhận hàng KHÔNG ở `IGoodsReceiptAppService.ReceiveAsync`. Flow thật:
> `PurchaseOrderAppService.ReceiveAsync` / `BulkReceiveGoodsAsync` → `PurchaseOrderManager.ReceiveItemsAsync` (line 481) → `MarkItemReceived` fire event → `PurchaseOrderItemReceivedEventHandler` gọi `SyncReceivedForPurchaseOrderItemAsync` → đồng thời `_goodsReceiptManager.CreateFromPurchaseOrderReceivingAsync` tạo GR.
> Plan này tích hợp đúng vào chuỗi đó, **không tạo entry point song song**.

### 4.1. `IDirectShipAppService`

```
Task UpdateAllocationDirectShipInfoAsync(UpdateDirectShipInfoDto);
Task ConfirmCustomerReceiptAsync(ConfirmReceiptDto);
Task RejectCustomerReceiptAsync(RejectReceiptDto);
Task<PagedResult<DirectShipPendingDeliveryDto>> GetPendingDeliveriesAsync(FilterDto);
```

> KHÔNG có `CreateAsync` riêng — direct-ship info được attach vào `PurchaseOrderItemAllocation` lúc tạo PO qua Shortage Aggregation (xem 4.2).

### 4.2. Extend `IPurchaseOrderAppService`

- `CreateAsync` / `AddItemsToExistingDraftAsync` (đã có ở Shortage P6.6): nhận thêm tham số `DirectShipInfo` per item (nullable). Khi có, set 5 field direct-ship lúc tạo allocation.
- **Relax validation `ReceiveAsync` (`PurchaseOrderAppService.cs:368`) và `BulkReceiveGoodsAsync` (`:438`)** — hiện chặn `QuantityReceived + dto.ReceivedQuantity > QuantityOrdered`. Đổi thành:
  - Default (`dto.AcceptOversupply == false`): giữ behaviour chặn cũ → return error code mới `Error.PurchaseOrderOversupplyRequiresConfirmation` (UI phát hiện code này → bật modal).
  - Khi `dto.AcceptOversupply == true`: cho phép vượt, **nhưng phần thừa không tăng `PurchaseOrderItem.QuantityReceived`** (giữ ≤ ordered). Phần thừa xử lý riêng (xem 4.4).

### 4.3. Hook direct-ship vào flow nhận hàng

- **Sửa `PurchaseOrderItemReceivedEventHandler.Handle`**: sau khi gọi `SyncReceivedForPurchaseOrderItemAsync`, manager đã trả về (hoặc query lại) danh sách allocation vừa `IncreaseReceived` mà `IsDirectShip = true` → gọi `IDirectShipManager.OnAllocationReceivedAsync` để:
  1. Tạo `DeliveryNote` status = `Confirmed`, source = `DirectShipToCustomer`, warehouse = Direct-Ship Transit, link `OrderId` từ `OrderItemId` của allocation.
  2. Transfer hàng từ warehouse chính (vừa nhập) sang Direct-Ship Transit qua `IInventoryStockManager.TransferStockAsync(unitCost = PurchaseOrderItem.UnitCost)`.
- **KHÔNG sửa** `PurchaseOrderManager.ReceiveItemsAsync` — vẫn auto-tạo GR vào warehouse chính như cũ. Việc tách kho xử lý hậu kỳ qua Transfer.

> Lý do approach Transfer post-receipt thay vì split GR: ít rủi ro phá `GoodsReceiptCreatedHandler` đã wire `VendorDebt + AverageCost`. Audit trail vẫn rõ: 1 GR vào kho chính + 1 Transfer + 1 DN.

### 4.4. Xử lý NCC giao thừa (oversupply)

Khi `AcceptOversupply = true` và `receivedQty > orderedQty`:
- `PurchaseOrderItem.QuantityReceived` chỉ tăng tối đa = `QuantityOrdered`.
- Phần dư (`receivedQty - orderedQty`) tạo riêng 1 `GoodsReceipt` "free stock" qua method mới `IGoodsReceiptManager.CreateFreeStockFromOversupplyAsync` — link reference tới PO + vendor + product + warehouse chính, **không link** `PurchaseOrderItemId` (vì PO line đã đủ).
- `GoodsReceiptCreatedHandler` đã có sẵn → tự cộng tồn + sinh `VendorDebt` cho phần thừa theo `UnitCost` của PO line.
- Raise `VendorOversupplyAcceptedEvent` cho báo cáo / audit.

### 4.5. Cancel SO / Reject DN — flow chuyển kho

- `OrderAppService.CancelAsync`: trước khi cancel, gọi `IDirectShipManager.HasReceivedDirectShipAllocationsAsync(orderId)`. Nếu có → trả flag về UI để hiện modal cảnh báo. Sau khi user confirm → cancel + gọi `IDirectShipManager.HandleSoCancelledForReceivedDirectShipAsync`.
- `IDirectShipAppService.RejectCustomerReceiptAsync`: gọi `IDirectShipManager.RejectCustomerReceiptAsync` (chuyển DN sang `Cancelled` + Transfer kho ảo → kho chính, unitCost = giá PO của allocation).

### 4.6. Print Service

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

- Khi quantity nhận **< quantity PO** (giao thiếu): không cần UI riêng — `SyncReceivedForPurchaseOrderItemAsync` đã ưu tiên direct-ship theo `IsDirectShip + Priority`. UI chỉ hiển thị summary "Đã phân bổ: 30 direct-ship / 50 về kho" sau khi confirm.
- Khi quantity nhận **> quantity PO** (giao thừa): submit lần đầu với `AcceptOversupply = false` → AppService trả error code `Error.PurchaseOrderOversupplyRequiresConfirmation`. UI catch code này → bật modal "NCC giao thừa Y đơn vị" với 3 lựa chọn:
  - **Nhập kho chính** → submit lại với `AcceptOversupply = true`. AppService giữ `QuantityReceived = QuantityOrdered`, phần thừa tạo GR free stock + `VendorDebt`.
  - **Từ chối nhận phần thừa** → submit lại với `ReceivedQuantity = QuantityOrdered` (cắt phần thừa khỏi DTO).
  - **Hủy GR** → đóng modal, không submit gì.

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
| **D1** | Domain — enum extensions + 5 field allocation + `DirectShipAddressChangeLog` + interfaces (stubs) | Shortage feature đã done | 1 ngày |
| **D2** | Domain services — `TransferStockAsync`, refactor `SyncReceivedForPurchaseOrderItemAsync` (priority sort), implement `DirectShipManager` orchestration | D1 | 1.5 ngày |
| **D3** | Application — extend `IPurchaseOrderAppService` (relax oversupply, accept DirectShipInfo), `IDirectShipAppService`, hook `PurchaseOrderItemReceivedEventHandler`, `CreateFreeStockFromOversupplyAsync` | D2 | 1.5 ngày |
| **D4** | Migration + seed (Tuấn chạy) + build verify | D3 | 0.5 ngày |
| **D5** | Print service + phiếu giao NCC | D3 | 1 ngày |
| **D6** | UI Shortage Aggregation — direct-ship checkbox + form | D3 | 1 ngày |
| **D7** | UI Trang Pending Deliveries + Confirm/Reject flow | D3 | 1.5 ngày |
| **D8** | UI SO Details + DN Details + flow cancel SO (modal xác nhận chuyển kho) | D3 | 0.75 ngày |
| **D9** | UI edit địa chỉ direct-ship + audit log + modal NCC giao thừa | D3 | 1 ngày |
| **D10** | Báo cáo direct-ship: theo NCC / khách / SP, tỷ lệ direct-ship, pending > 7 ngày | D3 | 1 ngày |
| **D11** | Manual smoke checklist (documented) + build verify — KHÔNG viết test code | D4-D10 | 0.5 ngày |

**Tổng:** ~11.25 ngày work.

## 8. Liên hệ với feature đang dở

- Feature Shortage & Auto-PO **đã hoàn tất** (TodoList đã clear hết các mục Shortage). Plan này build trên hạ tầng đã có:
  - `PurchaseOrderItemAllocation` entity + repo + manager.
  - `SyncReceivedForPurchaseOrderItemAsync` (sẽ refactor priority sort).
  - `_GlobalOffcanvas.cshtml` + `cowork-offcanvas.js` — reuse cho UI Pending Deliveries / SO Details.
  - Outbox Pattern đã wire — events đi qua đó tự nhiên.
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

5. **NCC giao thừa**: ✅ **Hỏi user mỗi lần** — cơ chế:
   - AppService relax validation: lần submit đầu (`AcceptOversupply = false`) → trả error code `Error.PurchaseOrderOversupplyRequiresConfirmation`. UI bật modal "NCC giao thừa Y đơn vị. Xử lý: [Nhập kho chính] / [Từ chối nhận phần thừa] / [Hủy GR]".
   - Nếu user chọn "Nhập kho chính" → submit lại với `AcceptOversupply = true`. `PurchaseOrderItem.QuantityReceived` chỉ tăng tới `QuantityOrdered`; phần thừa được tạo GR free stock riêng (link tới PO/vendor/product, không link `PurchaseOrderItemId`) → `GoodsReceiptCreatedHandler` đã có sẵn sinh `VendorDebt` theo giá PO.
   - Nếu user chọn "Từ chối": submit lại với `ReceivedQuantity = QuantityOrdered`.

6. **Hóa đơn VAT mua từ NCC**: ⏸️ **Ngoài scope phase này** — chưa làm.
   - Workflow xuất HĐ VAT mua hiện chưa được hệ thống hóa.
   - Plan này chỉ focus HĐ VAT bán (cho khách) thông qua Confirm Delivery.
   - Move qua mục 11 (Ngoài scope).

7. **Báo cáo direct-ship**: ✅ **Làm luôn trong scope** — thêm Phase D9.

## 10. Rủi ro

| Rủi ro | Mức | Mitigation |
|--------|-----|------------|
| **Double-count allocation received** (manager mới + `SyncReceivedForPurchaseOrderItemAsync` chạy song song) | Rất cao | Plan này KHÔNG tạo phân bổ song song — chỉ sửa `SyncReceivedForPurchaseOrderItemAsync` thêm priority sort. `DirectShipManager` chỉ orchestration (tạo DN + transfer), không tự cộng `ReceivedQuantity`. |
| Tính toán phân bổ khi giao thiếu sai priority | Cao | Manual smoke D11 với 3 scenario (đủ / thiếu vừa / thiếu nhiều), kiểm tra direct-ship được nhận trước. |
| Phá vỡ `GoodsReceiptCreatedHandler` đã wire VendorDebt / AverageCost | Cao | Approach Transfer post-receipt — KHÔNG sửa logic tạo GR / pipeline handler hiện có. |
| User quên Confirm DN → tồn kho ảo phình to | Trung | Báo cáo "Pending > 7 ngày" (D10) — email reminder làm sau. |
| HĐ VAT chốt sai timing | Trung | Tận dụng lifecycle DN có sẵn: chỉ `Delivered` mới sinh `CustomerDebt`. Confirm flow KHÔNG bypass status. |
| Migration trên DB có data: enum default | Thấp | `IsDirectShip` default false (allocation cũ không ảnh hưởng); `DeliveryNoteSourceType.DirectShipToCustomer = 3` chỉ DN mới mới có; `WarehouseType.DirectShipTransit` chỉ seed 1 record. |
| `Order.CancelAsync` chưa biết về direct-ship → hủy SO trong khi hàng đã ở kho ảo, không transfer về | Cao | D8 gate `OrderAppService.CancelAsync` qua `IDirectShipManager.HasReceivedDirectShipAllocationsAsync` trước khi cho phép cancel. |

## 11. Ngoài scope (làm sau)

- **Hệ thống hóa hóa đơn VAT mua từ NCC** — workflow mua chưa số hóa, sẽ làm thành feature riêng.
- Tự động phân bổ PO khi tạo Sales Order lớn (gợi ý gộp/chia NCC).
- Tự động liên lạc khách qua SMS/Zalo khi NCC chuẩn bị giao.
- Tracking GPS xe NCC.
- App mobile cho NCC xác nhận đã giao.

---

## Phụ lục A — Sơ đồ trạng thái (đã align với code)

```
SO created → Shortage → PO + allocation (IsDirectShip=true, Address...)
                                  │
                                  ▼
                  PurchaseOrderManager.ReceiveItemsAsync
                  → GR vào WAREHOUSE CHÍNH (100 bao)
                  → fire PurchaseOrderItemReceived
                                  │
                                  ▼
          PurchaseOrderItemReceivedEventHandler:
          ① SyncReceivedForPurchaseOrderItemAsync
             (sort: IsDirectShip↓, Priority↓, CreatedOnUtc↑)
             → IncreaseReceived cho từng allocation
          ② Với allocation direct-ship vừa receive:
             IDirectShipManager.OnAllocationReceivedAsync
             → Tạo DN Status=Confirmed, Source=DirectShipToCustomer,
                Warehouse=DirectShipTransit
             → TransferStockAsync(MAIN → DirectShipTransit, qty, unitCost=PO)
                                  │
                                  ▼
                  [DN Confirmed, chờ khách xác nhận]
                                  │
                  ┌───────────────┴───────────────┐
                  ▼                               ▼
        Confirm (khách nhận đủ)            Reject (khách từ chối)
        DN: Confirmed → Delivered          DN: Confirmed → Cancelled
        ↓ DeliveryNoteDelivered            TransferStockAsync
        DeliveryNoteDeliveredEventHandler  (DirectShipTransit → MAIN,
        → CustomerDebt sinh                 unitCost = giá PO)
        (timing chuẩn HĐ VAT)
```

## Phụ lục B — Field mapping nhanh (refactored)

| Layer | Thay đổi |
|-------|----------|
| `PurchaseOrderItemAllocation` | + `IsDirectShip`, `DirectShipAddress`, `DirectShipContactName`, `DirectShipContactPhone`, `DirectShipPriority` (KHÔNG có status enum) |
| `WarehouseType` enum | + `DirectShipTransit` (giữ Main, SubWarehouse, ReturnWarehouse) |
| `DeliveryNoteSourceType` enum | + `DirectShipToCustomer = 3` (giữ ToCustomer, ToVendorReturn, ToAdjustment) |
| `DeliveryNote` | (KHÔNG thêm status / confirmation field — reuse `Status` lifecycle hiện có) |
| `DirectShipAddressChangeLog` (new entity) | Id, AllocationId, OldAddress, NewAddress, OldContactName, NewContactName, OldContactPhone, NewContactPhone, EditedByUserId, EditedAt, Reason |
| `IInventoryStockManager` | + `TransferStockAsync(productId, fromWarehouseId, toWarehouseId, qty, unitCost, referenceId, userId, note)` |
| `PurchaseOrderAllocationManager.SyncReceivedForPurchaseOrderItemAsync` | Refactor sort: `IsDirectShip↓, Priority↓, CreatedOnUtc↑` |
| `PurchaseOrderAppService.ReceiveAsync/BulkReceive` | Relax validation: thêm flag `AcceptOversupply`, error code `Error.PurchaseOrderOversupplyRequiresConfirmation` |
| `IGoodsReceiptManager` | + `CreateFreeStockFromOversupplyAsync(po, vendor, product, warehouse, qty, unitCost)` |

## Phụ lục C — Báo cáo Direct-Ship (Phase D10)

1. **Báo cáo direct-ship theo NCC**: tháng/quý/năm. Cột: NCC, Số PO, Số bao direct-ship, Số bao về kho, Tỷ lệ direct-ship.
2. **Báo cáo direct-ship theo khách**: top khách nhận hàng giao thẳng. Cột: Khách, Số SO, Số bao, Tổng tiền.
3. **Báo cáo direct-ship theo SP**: SP nào hay direct-ship. Cột: SP, Qty direct-ship, Qty qua kho, Tỷ lệ.
4. **Báo cáo Pending Confirmation quá hạn**: DN `Status=Confirmed AND SourceType=DirectShipToCustomer` quá 7 ngày — alert cho user.
5. **Báo cáo Reject Delivery**: tỷ lệ DN bị `Cancelled` (source DirectShipToCustomer) + lý do — đánh giá chất lượng giao thẳng.

## Phụ lục D — Manual Smoke Checklist (Phase D11)

> Theo AGENTS.md: KHÔNG viết test code trong project `*.Test`. Phase D11 = checklist tay, có thể ghi vào `docs/DIRECT_SHIP_SMOKE_CHECKLIST.md` riêng. Tuấn chạy local server + UI thật, đánh dấu pass/fail.

1. **Happy path**: SO 30 + PO 100 direct-ship → NCC giao 100 → kiểm tra: GR 100 vào kho chính → Transfer 30 sang DirectShipTransit → DN tạo Status=Confirmed → bấm Confirm → DN Delivered → `CustomerDebt` sinh đúng giá bán.
2. **Giao thiếu**: NCC giao 80/100 → kiểm tra: 30 direct-ship đủ → 50 vào kho chính → DN tạo đủ 30.
3. **Giao thừa**: NCC giao 110 → modal hiện ra → chọn "Nhập kho chính" → kiểm tra: PO QuantityReceived = 100, 1 GR free stock 10 + `VendorDebt` tăng theo giá PO × 10.
4. **Reject DN**: DN Status=Confirmed → bấm Reject với reason → DN Cancelled → 30 bao về kho chính, AverageCost tính lại theo giá PO.
5. **Cancel SO sau GR**: SO có allocation FullyReceived → bấm Cancel → modal cảnh báo → confirm → hàng về kho chính → SO Cancelled.
6. **Edit địa chỉ**: PO đã confirm có allocation direct-ship → bấm "Sửa địa chỉ giao" → save → audit log có entry mới + banner "Gửi lại phiếu cho NCC".
7. **N-N allocation**: 1 SO chia 2 PO (1 direct-ship, 1 thường) → cả 2 PO receive đủ → SO Details hiển thị đủ 2 DN.

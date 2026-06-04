# Improvement: Goods Receiving for Direct Ship

## Vấn đề

Khi một PO item đã có **phân bổ thông thường** (`IsDirectShip = false`) cho đơn bán, người dùng không thể chuyển phân bổ đó thành "giao thẳng" trong lúc nhận hàng.

### Root Cause

`GetEligibleOrderItemsForPoItemAsync` lọc `.Where(dto => dto.AvailableToAllocate > 0)`. Khi order item đã được phân bổ đủ số lượng vào PO item này thì `AvailableToAllocate = 0` → bị loại ra khỏi danh sách chọn trong receive modal và bulk receive modal.

Kết quả: cả hai đường đều bị chặn:
1. Toggle "Giao thẳng" trong receive modal không hiện order item đã phân bổ.
2. Không có nút nào trên dòng phân bổ để chuyển thành giao thẳng.

---

## Giải pháp

Ba hướng bổ sung nhau:

- **Hướng B**: Thêm nút "Giao thẳng" trực tiếp trên dòng phân bổ trong bảng PO Details.
- **Hướng C**: Cập nhật receive modal (nhận đơn lẻ) để hiển thị phân bổ hiện tại và cho phép nâng cấp thành giao thẳng.
- **Hướng D**: Cập nhật bulk receive modal tương tự.

---

## Kế hoạch triển khai

### Phase A — Backend chung

**A1 — `MarkAllocationAsDirectShipCommand` + Handler**
- File: `DirectShipDeliveryCommands.cs`, `DirectShipDeliveryCommandHandlers.cs`
- Command: `{ AllocationId, Address, ContactName, ContactPhone }`
- Handler gọi `IDirectShipAppService.MarkAllocationAsDirectShipAsync`

**A2 — Endpoint `GET /PurchaseOrder/NonDirectShipAllocations?purchaseOrderItemId=`**
- File: `PurchaseOrderController.cs`
- Trả về non-DS allocations của PO item: `IsDirectShip=false`, `Status != Cancelled`, `ReceivedQty < AllocatedQty`
- Response: `[{ allocationId, orderId, orderItemId, orderCode, customerName, allocatedQty, remainingQty, shippingAddress, customerPhone }]`

**A3 — Thêm `DirectShipExistingAllocationId` vào commands**
- `ReceivePurchaseOrderItemCommand.cs`: `public Guid? DirectShipExistingAllocationId { get; set; }`
- `BulkReceiveLineCommand.cs`: `public Guid? DirectShipExistingAllocationId { get; init; }`

**A4 — `GetAllocationRemainingQuantityAsync(Guid allocationId)`**
- File: `IPurchaseOrderAppService.cs`, `PurchaseOrderAppService.cs`
- Đọc `_purchaseOrderItemAllocationDataReader` → trả về `Max(0, AllocatedQty - ReceivedQty)`
- Dùng trong handler để tính warehouse requirement khi upgrade

**A5 — Cập nhật `ReceivePurchaseOrderItemHandler`**

Logic mới khi `DirectShipExistingAllocationId` có giá trị:
```
maxAllocationQty = GetAllocationRemainingQuantityAsync(DirectShipExistingAllocationId)
SKIP AllocatePoItemForOrderItemCommand (không tạo allocation mới)
Gọi MarkAllocationAsDirectShipCommand để mark allocation hiện tại
Proceed với receive bình thường
```

**A6 — Cập nhật `BulkReceivePurchaseOrderHandler`**
- Pass `DirectShipExistingAllocationId` từ `BulkReceiveLineCommand` sang `ReceivePurchaseOrderItemCommand`

---

### Phase B — Nút trên allocation row

**B1 — `POST /DirectShipDelivery/MarkAsDirectShip`**
- File: `DirectShipDeliveryController.cs`
- Body: `{ allocationId, address, contactName, contactPhone }`
- Dùng A1 command qua MediatR

**B2 — UI trong `Details.cshtml`**
- Thêm nút trên dòng `AllocationsPerItem` khi: `!allocation.IsDirectShip && allocation.Status != 5 && allocation.Status != 2`
- Nút mở modal nhập địa chỉ / SĐT
- Submit → AJAX POST → reload trang khi success

---

### Phase C — Cập nhật receive modal (đơn lẻ)

**C1 — `Details.cshtml` receive modal**

Thêm hidden input:
```html
<input type="hidden" name="DirectShipExistingAllocationId" id="modalDsExistingAllocationId" value="" />
```

Cập nhật `loadReceiveDsItems()`:
- Song song fetch `EligibleOrderItems` (mới) + `NonDirectShipAllocations` (hiện có)
- Render 2 nhóm trong danh sách:
  - Nhóm 1 (badge xanh): **"Tạo phân bổ + Giao thẳng"** — order items chưa có allocation
  - Nhóm 2 (badge cam): **"Nâng cấp giao thẳng"** — non-DS allocations hiện có
- Khi chọn nhóm 2: set `modalDsExistingAllocationId`, clear `modalDsOrderId` + `modalDsOrderItemId`
- Khi chọn nhóm 1: set `modalDsOrderId` + `modalDsOrderItemId`, clear `modalDsExistingAllocationId`

Cập nhật `toggleDirectShipCheckbox()`:
- Disable chỉ khi `qty <= totalDsRem` VÀ không có non-DS allocations cho item này

Cập nhật `syncWarehouseVisibility()`:
- Tính `maxDsQty` bao gồm cả `currentDsAvailableQty` từ selection hiện tại

---

### Phase D — Cập nhật bulk receive modal

**D1 — `BulkReceiveController.js`**

DS sub-row: thêm hidden input:
```html
<input type="hidden" name="Items[N].DirectShipExistingAllocationId" class="bulk-ds-existing-allocation-id" value="" />
```

Cập nhật `#loadDsItems()`:
- Song song fetch `EligibleOrderItems` + `NonDirectShipAllocations`
- Render 2 nhóm, mỗi nhóm là list-group
- Click nhóm 2: set `bulk-ds-existing-allocation-id`, clear `bulk-ds-order-item-id` + `bulk-ds-order-id`

Cập nhật validation trong `#onSubmit()`:
- Chấp nhận HOẶC `existingAllocationId` HOẶC `orderItemId` (không cần cả 2)

---

## Tổng hợp file thay đổi

| File | Phase | Thay đổi |
|---|---|---|
| `DirectShipDeliveryCommands.cs` | A1 | +1 command class |
| `DirectShipDeliveryCommandHandlers.cs` | A1 | +1 handler |
| `PurchaseOrderController.cs` | A2 | +1 GET endpoint |
| `ReceivePurchaseOrderItemCommand.cs` | A3 | +1 field |
| `BulkReceivePurchaseOrderCommand.cs` | A3 | +1 field |
| `IPurchaseOrderAppService.cs` | A4 | +1 method signature |
| `PurchaseOrderAppService.cs` | A4 | +1 method impl |
| `ReceivePurchaseOrderItemHandler.cs` | A5 | update logic |
| `BulkReceivePurchaseOrderHandler.cs` | A6 | pass-through |
| `DirectShipDeliveryController.cs` | B1 | +1 POST endpoint |
| `Details.cshtml` | B2, C1 | nút+modal + receive modal update |
| `BulkReceiveController.js` | D1 | update DS load+submit |

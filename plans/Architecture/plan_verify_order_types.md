# Plan: Rà soát và fix IReliableDomainEvent

## Vấn đề

Phân tích toàn bộ DomainEvents cho thấy nhiều events có handler thực hiện write operations quan trọng (stock, debt, allocation, costing) nhưng **chưa được đánh dấu `IReliableDomainEvent`**. Đây là documentation contract — nếu routing logic thay đổi sau này, những event này sẽ mất đi reliability guarantee.

Nghiêm trọng nhất: `DeliveryNoteDelivering` có comment ghi "không có handler" nhưng `DeliveryNoteDeliveringStockHandler` đang thực hiện `DispatchStockUpToAsync` + `RegisterOutboundAsync` (trừ tồn kho thực + ghi giá vốn).

## Danh sách fix

### Nhóm 1 — Orders (7 events)

File: `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Events/Orders/OrderEvents.cs`

- [ ] `OrderItemAdded` → thêm `: IReliableDomainEvent`
- [ ] `OrderItemUpdated` → thêm `: IReliableDomainEvent`
- [ ] `OrderItemRemoved` → thêm `: IReliableDomainEvent`
- [ ] `OrderCompleted` → thêm `: IReliableDomainEvent`
- [ ] `OrderCancelled` → thêm `: IReliableDomainEvent`
- [ ] `OrderDeleted` → thêm `: IReliableDomainEvent`
- [ ] `OrderFullyDelivered` → thêm `: IReliableDomainEvent`

### Nhóm 2 — GoodsReceipts (4 events)

File: `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Events/GoodsReceipts/GoodsReceiptEvents.cs`

- [ ] `GoodsReceiptCreated` → thêm `: IReliableDomainEvent`
- [ ] `GoodsReceiptItemUnitCostSet` → thêm `: IReliableDomainEvent`
- [ ] `GoodsReceiptVendorChanged` → thêm `: IReliableDomainEvent`
- [ ] `GoodsReceiptDeleted` → thêm `: IReliableDomainEvent`

### Nhóm 3 — DeliveryNotes (2 events + fix comment)

File: `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Events/DeliveryNotes/DeliveryNoteEvents.cs`

- [ ] `DeliveryNoteCreated` → thêm `: IReliableDomainEvent`
- [ ] `DeliveryNoteDelivering` → thêm `: IReliableDomainEvent` + **sửa comment** (hiện ghi sai "không có handler")

### Nhóm 4 — PurchaseOrders (5 events)

File: `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Events/PurchaseOrders/PurchaseOrderEvents.cs`

- [ ] `PurchaseOrderCancelled` → thêm `: IReliableDomainEvent`
- [ ] `PurchaseOrderItemReceived` → thêm `: IReliableDomainEvent`
- [ ] `PurchaseOrderBulkReceived` → thêm `: IReliableDomainEvent`

File: `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Events/PurchaseOrders/DirectShipEvents.cs`

- [ ] `AllocationMarkedAsDirectShip` → thêm `: IReliableDomainEvent`
- [ ] `VendorOversupplyAccepted` → thêm `: IReliableDomainEvent`

### Nhóm 5 — Stock operations (3 events)

File: `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Events/StockAdjustment/StockAdjustmentNoteEvents.cs`
- [ ] `StockAdjustmentNoteApproved` → thêm `: IReliableDomainEvent`

File: `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Events/StockTransfer/StockTransferNoteEvents.cs`
- [ ] `StockTransferNoteApproved` → thêm `: IReliableDomainEvent`

File: `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Events/Inventory/ProductReservationLedgerEvents.cs`
- [ ] `ProductReservationLedgerCreated` → thêm `: IReliableDomainEvent`

### Nhóm 6 — Debts / Returns / Finance (3 events)

File: `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Events/Debts/VendorRefundEvents.cs`
- [ ] `VendorRefundCompleted` → thêm `: IReliableDomainEvent`

File: `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Events/Returns/VendorReturnEvents.cs`
- [ ] `VendorReturnOverRecovered` → thêm `: IReliableDomainEvent`

File: `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Events/Finance/FixedAssetEvents.cs`
- [ ] `FixedAssetDisposed` → thêm `: IReliableDomainEvent`

### Nhóm 7 — Borderline / storage leak (2 events)

File: `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Events/Catalog/ProductEvents.cs`
- [ ] `ProductUpdated` → thêm `: IReliableDomainEvent` (handler xóa orphaned pictures)
- [ ] `ProductDeleted` → thêm `: IReliableDomainEvent` (handler xóa toàn bộ pictures)

---

## Tổng cộng

| Nhóm | Số events |
|------|-----------|
| Orders | 7 |
| GoodsReceipts | 4 |
| DeliveryNotes | 2 |
| PurchaseOrders | 5 |
| Stock operations | 3 |
| Debts/Returns/Finance | 3 |
| Borderline | 2 |
| **Tổng** | **26** |

---

## Cách implement

Mỗi event chỉ cần thêm `, IReliableDomainEvent` vào phần kế thừa:

```csharp
// Trước
public sealed record OrderItemAdded(...) : DomainEvent;

// Sau
public sealed record OrderItemAdded(...) : DomainEvent, IReliableDomainEvent;
```

Không thay đổi logic, không migration, không ảnh hưởng runtime (tất cả events đã đi qua Outbox).

---

## Không cần fix

Events sau **đúng khi không có `IReliableDomainEvent`**:
- Tất cả events không có handler (audit/tracking)
- `OrderPlaced`, `DirectShipDeliveryConfirmed`, `DirectShipDeliveryRejected` — handler no-op
- `PurchaseOrderCreated`, `PurchaseOrderStatusChanged` — notification only
- `DeliveryNoteConfirmed` — chỉ enqueue integration event cho n8n
- `PurchaseOrderItemAdded/Updated/Removed`, `OrderItemAdded/Updated/Removed` (phần audit handler) — audit log không critical
- `InventoryCostReturnReversalLost` — warning notification

Xem chi tiết tại `docs/Architecture/domain_event.md`.

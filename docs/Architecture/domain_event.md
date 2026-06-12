# Domain Events — Hướng dẫn phân loại

## Kiến trúc dispatch hiện tại

Tất cả `IDomainEvent` đều đi qua **Outbox Pattern**:

1. `DomainEventDispatchInterceptor` — serialize **mọi** event từ `AppAggregateEntity` vào bảng `OutboxMessages` trong cùng transaction với business write.
2. `OutboxProcessor` (BackgroundService) — đọc OutboxMessages, publish qua MediatR trong DI scope riêng (retry khi fail, dead-letter sau `MaxRetryCount` lần).

Không còn inline dispatch sau `SaveChanges`.

---

## Hai loại event

### `IDomainEvent` (base)

Event bình thường — đi qua Outbox như mọi event khác. Dùng khi:
- Không có handler (chỉ để audit/tracking)
- Handler là no-op hoặc legacy
- Handler chỉ tạo `SystemNotification` (mất không gây data corruption)
- Handler chỉ enqueue integration event cho external system (n8n...) — reliability là trách nhiệm của integration event đó

### `IReliableDomainEvent : IDomainEvent, IIntegrationEvent`

**Documentation contract**: đánh dấu rằng handler của event này thực hiện **write operation quan trọng về nghiệp vụ** — nếu mất sẽ gây inconsistent state. Dùng khi handler:

- Cộng/trừ/adjust tồn kho (`IInventoryStockManager.*`)
- Ghi inventory cost (`IInventoryCostingManager.*`)
- Tạo/điều chỉnh công nợ (`*DebtManager.Create*`, `*ApplyReturn*`)
- Release/reserve allocations (`IPurchaseOrderAllocationManager.Release*`)
- Tạo entities mới từ event (GoodsReceipt, DeliveryNote, Expense, Warehouse)

**Rule 1 câu**: nếu handler fail mà không retry → tồn kho sai / nợ sai / allocation không release → **phải là `IReliableDomainEvent`**.

---

## Phân loại toàn bộ events

### ✅ Phải là `IReliableDomainEvent`

#### Orders — stock reservation / allocation

| Event | Handler | Write op |
|-------|---------|----------|
| `OrderItemAdded` | `OrderItemAddedEventHandler` | `ReserveAsync` → tạo `ProductReservationLedger` |
| `OrderItemUpdated` | `OrderItemUpdatedEventHandler` | `AdjustAsync` reservation |
| `OrderItemRemoved` | `OrderItemRemovedEventHandler` + `OrderItemRemovedAllocationReleaseHandler` | `ReleaseAsync` reservation + release allocation |
| `OrderCompleted` | `OrderCompletedEventHandler` | Release reservation còn lại |
| `OrderCancelled` | `OrderCancelledEventHandler` + `OrderCancelledAllocationReleaseHandler` | Release reservation + allocation |
| `OrderDeleted` | `OrderDeletedEventHandler` + `OrderDeletedAllocationReleaseHandler` | Release reservation + allocation |
| `OrderFullyDelivered` | `OrderFullyDeliveredAllocationReleaseHandler` | `ReleaseAllocationsForOrderAsync` |

#### GoodsReceipts — stock + costing + vendor debt

| Event | Handler | Write op |
|-------|---------|----------|
| `GoodsReceiptCreated` | `GoodsReceiptCreatedHandler` | `ReceiveStockUpToAsync` + `RegisterInboundAsync` + tạo `VendorDebt` |
| `GoodsReceiptItemUnitCostSet` | `GoodsReceiptItemUnitCostSetHandler` | `AssignGoodsReceiptItemCostAsync` + tạo `VendorDebt` |
| `GoodsReceiptVendorChanged` | `GoodsReceiptVendorChangedHandler` | Tạo `VendorDebt` |
| `GoodsReceiptDeleted` | `GoodsReceiptDeletedEventHandler` | `RevertReceiveUpToAsync` + `RegisterReceiptReversalAsync` |

#### DeliveryNotes — stock dispatch + costing

| Event | Handler | Write op |
|-------|---------|----------|
| `DeliveryNoteCreated` | `DeliveryNoteCreatedHandler` | Release global reservation + `ReserveStockAsync` warehouse |
| `DeliveryNoteDelivering` | `DeliveryNoteDeliveringStockHandler` | `DispatchStockUpToAsync` + `RegisterOutboundAsync` — **trừ tồn kho + ghi giá vốn** |
| `DeliveryNoteDelivered` | `DeliveryNoteDeliveredHandler` | Tạo `CustomerDebt` + mark items delivered |

> ⚠️ `DeliveryNoteDelivering`: comment trong event file ghi "không có handler" — **SAI**. Handler `DeliveryNoteDeliveringStockHandler` thực hiện hai operations quan trọng nhất hệ thống.

#### PurchaseOrders

| Event | Handler | Write op |
|-------|---------|----------|
| `PurchaseOrderCancelled` | `PurchaseOrderCancelledHandler` | `ReleaseAllocationsOfPurchaseOrderItemAsync` |
| `PurchaseOrderItemReceived` | `PurchaseOrderItemReceivedHandler` | `VerifyStatusAsync` → transition PO status |
| `PurchaseOrderBulkReceived` | `PurchaseOrderBulkReceivedHandler` | `VerifyStatusAsync` → transition PO status |
| `AllocationMarkedAsDirectShip` | `AllocationMarkedAsDirectShipHandler` | Tạo `Warehouse` (DirectTransit) nếu chưa có |
| `VendorOversupplyAccepted` | `VendorOversupplyAcceptedHandler` | Tạo `GoodsReceipt` |

#### Stock operations

| Event | Handler | Write op |
|-------|---------|----------|
| `StockAdjustmentNoteApproved` | handler | Adjust tồn kho theo delta từng item |
| `StockTransferNoteApproved` | handler | Move stock kho A → kho B |
| `ProductReservationLedgerCreated` | `ProductReservationLederCreatedEventHandler` | `InitializeStockAsync` tạo `InventoryStock` records trên tất cả warehouses |

#### Debts / Returns / Finance

| Event | Handler | Write op |
|-------|---------|----------|
| `CustomerReturnConfirmed` | `CustomerReturnConfirmedEventHandler` | Tạo `GoodsReceipt` + giảm `CustomerDebt` |
| `CustomerReturnOverRefunded` | `CustomerReturnOverRefundedEventHandler` | Tạo `CustomerRefund` |
| `CustomerRefundCompleted` | `CustomerRefundCompletedEventHandler` | Ghi ledger, cập nhật số dư |
| `VendorReturnConfirmed` | `VendorReturnConfirmedEventHandler` | Tạo `DeliveryNote` + giảm `VendorDebt` |
| `VendorReturnOverRecovered` | `VendorReturnOverRecoveredEventHandler` | Tạo `VendorRefund` |
| `VendorRefundCompleted` | `VendorRefundCompletedEventHandler` | `ConsumeCreditNoteByRefundAsync` → ghi VendorDebt |
| `FixedAssetDisposed` | `FixedAssetDisposedHandler` | Tạo `Expense` |

#### Borderline (nên thêm — storage leak nếu fail)

| Event | Handler | Write op |
|-------|---------|----------|
| `ProductUpdated` | `ProductUpdatedEventHandler` | Xóa orphaned pictures khỏi storage + DB |
| `ProductDeleted` | `ProductDeletedEventHandler` | Xóa toàn bộ pictures khỏi storage + DB |

---

### ❌ Không cần `IReliableDomainEvent`

| Nhóm | Events |
|------|--------|
| Không có handler (audit/tracking) | `CategoryCreated/Updated/ParentChanged/Deleted`, `VendorCreated/Updated/Deleted`, `ProductCreated/PriceChanged`, `CustomerCreated/Updated/Deleted`, `OrderInfoUpdated/ShippingUpdated/ItemDelivered`, `GoodsReceiptUpdated/SetToPurchaseOrder/RemovedFromPurchaseOrder/ItemSplitOnLinking`, `DeliveryNoteCancelled`, `WarehouseCreated/Updated/Deleted`, `CustomerDebt*/CustomerPaymentRecorded`, `CustomerRefundCreated/Cancelled`, `VendorDebt*/VendorPaymentRecorded`, `VendorRefundCreated/Cancelled`, `CustomerLedgerEntryRecorded`, `VendorLedgerEntryRecorded`, `CustomerReturnCancelled`, `VendorReturnCancelled`, `UserCreated/Updated/PasswordChanged/Deleted`, `PictureCreated/Deleted`, `FixedAssetCreated` |
| No-op / Legacy | `OrderPlaced` (handler = `Task.CompletedTask`), `DirectShipDeliveryConfirmed` (no-op), `DirectShipDeliveryRejected` (no-op) |
| Notification only | `PurchaseOrderCreated`, `PurchaseOrderStatusChanged` (SystemNotification — mất không sao) |
| Integration event routing | `DeliveryNoteConfirmed` (enqueue cho n8n — integration event tự handle reliability) |
| Audit log only | `PurchaseOrderItemAdded/Updated/Removed` (PO item change audit), `OrderItemAdded/Updated/Removed` (order item change audit — handler audit log, không phải stock) |
| Warning notification | `InventoryCostReturnReversalLost` (SystemNotification cảnh báo kế toán) |

---

## Checklist khi thêm event mới

1. Event có handler không?
   - Không → `DomainEvent` thường, không cần `IReliableDomainEvent`
2. Handler có write DB không?
   - Không (read-only, notification, no-op) → `DomainEvent` thường
3. Write đó có gây data corruption nếu mất không?
   - Stock sai / nợ sai / allocation treo → ✅ `IReliableDomainEvent`
   - Chỉ là storage leak / notification miss → borderline, ưu tiên thêm
4. Handler có `Task.CompletedTask` không?
   - Legacy / no-op → `DomainEvent` thường, cân nhắc xóa handler

## Lưu ý với handlers có nhiều `INotificationHandler<SameEvent>`

Một event có thể có cả handler write (reserve stock) lẫn handler audit (ghi audit log) cùng subscribe. Ví dụ `OrderItemAdded`:
- `OrderItemAddedEventHandler` → write (reserve)
- `OrderItemAddedAuditEventHandler` → audit

Trong trường hợp này, event phải là `IReliableDomainEvent` vì có ít nhất một handler thực hiện write quan trọng.

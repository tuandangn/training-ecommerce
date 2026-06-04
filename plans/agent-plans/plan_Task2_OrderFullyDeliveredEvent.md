# Implementation Plan: Task 2 — Event khi đơn hàng đã được giao đầy đủ

## Mô tả vấn đề

Khi toàn bộ sản phẩm trong đơn hàng đã được giao đến khách hàng, hệ thống chưa tự động nhận biết và xử lý nghiệp vụ tiếp theo. Cụ thể:

1. Không có event `OrderFullyDelivered` — sau khi tất cả items được đánh dấu `IsDelivered`, không có gì xảy ra tự động.
2. `PurchaseOrderItemAllocation` còn đang ở trạng thái `Allocated` / `PartiallyReceived` nhưng chưa được resolve (release) — gây tắc nghẽn allocation cho các đơn hàng khác.

## Kết quả mong muốn

1. Khi tất cả `OrderItem.IsDelivered == true` → hệ thống tự động fire event `OrderFullyDelivered`.
2. Event chỉ được fire **một lần** (idempotent).
3. Handler xử lý event → release toàn bộ pending allocations của đơn hàng đó (`ReleaseAllocationsForOrderAsync`).
4. Dễ mở rộng thêm handler nghiệp vụ tiếp theo (tạo debt, gửi email, v.v.).

---

## Hiện trạng (từ điều tra)

### Flow hiện tại khi giao hàng

```
DeliveryNote.MarkDeliveredAsync()
  → foreach noteItem:
      if deliveredQty >= orderedQty → OrderManager.MarkOrderItemDeliveredAsync()
          → order.MarkOrderItemDelivered() → raises OrderItemDelivered event
  → raises DeliveryNoteDelivered event
      → DeliveryNoteDeliveredHandler: tạo CustomerDebt
      → DeliveryNoteDeliveredStockHandler: dispatch stock
```

### Vấn đề

- Sau khi tất cả items được mark delivered, **không có bước nào check xem toàn bộ order đã giao xong chưa**.
- `CompleteOrderAsync()` phải được gọi thủ công (admin action), không tự động.
- Pending allocations chỉ được release khi: order bị cancel, order bị xóa, hoặc order item bị remove.

### Idempotency đã được đảm bảo sẵn

- `MarkOrderItemDeliveredAsync` check `!orderItem.IsDelivered` trước khi mark → không thể mark 2 lần.
- Do đó, khi check "tất cả items delivered" ngay sau khi mark item cuối cùng → chỉ fire event đúng 1 lần.

---

## Quyết định kiến trúc

1. **Event được raise trong `OrderManager`**, không phải trong `DeliveryNoteManager` — vì "fully delivered" là trạng thái của Order, không phải DeliveryNote.
2. **Check "fully delivered" trong `MarkOrderItemDeliveredAsync`** — sau khi mark item xong, check `order.OrderItems.All(i => i.IsDelivered)` → nếu true thì raise event.
3. **Event raise từ entity `Order`** — consistent với pattern hiện tại (OrderItemDelivered, OrderCompleted đều raise từ entity).
4. **Handler trong Application layer** — `OrderFullyDeliveredAllocationReleaseHandler` inject `IPurchaseOrderAllocationManager`.

---

## Task List

### Phase 1: Domain Event

#### Task 1.1 — Thêm `OrderFullyDelivered` vào `OrderEvents.cs`
**Mô tả:** Định nghĩa domain event mới cho trạng thái "giao hàng đầy đủ".

**File:** `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Events/Orders/OrderEvents.cs`

**Nội dung cần thêm:**
```csharp
public sealed record OrderFullyDelivered(Guid OrderId, Guid CustomerId) : DomainEvent;
```

**Acceptance criteria:**
- [ ] Record mới được thêm vào file
- [ ] Build thành công

**Estimated scope:** XS (1 file, 1 dòng)

---

### Phase 2: Domain Entity — Raise Event

#### Task 2.1 — Thêm method `RaiseFullyDeliveredIfComplete()` vào `Order` entity
**Mô tả:** Method internal được gọi sau khi mark item delivered. Check toàn bộ items, nếu tất cả delivered thì raise event `OrderFullyDelivered`.

**File:** `NamEcommerce/Domain/NamEcommerce.Domain/Entities/Orders/Order.cs`

**Logic:**
```csharp
internal void RaiseFullyDeliveredIfComplete()
{
    if (OrderItems.Any() && OrderItems.All(i => i.IsDelivered))
        RaiseDomainEvent(new OrderFullyDelivered(Id, CustomerId));
}
```

**Acceptance criteria:**
- [ ] Method chỉ raise event khi `OrderItems.Any() && All(i => i.IsDelivered)`
- [ ] Không raise khi order items rỗng
- [ ] Method `internal` (không public)

**Estimated scope:** XS (1 file, ~5 dòng)

---

### Phase 3: Domain Service — Gọi check sau khi mark delivered

#### Task 3.1 — Cập nhật `MarkOrderItemDeliveredAsync` trong `OrderManager`
**Mô tả:** Sau khi `order.MarkOrderItemDelivered()` thành công, gọi `order.RaiseFullyDeliveredIfComplete()`.

**File:** `NamEcommerce/Domain/NamEcommerce.Domain.Services/Orders/OrderManager.cs`

**Vị trí:** Method `MarkOrderItemDeliveredAsync` — sau dòng save order.

**Acceptance criteria:**
- [ ] `order.RaiseFullyDeliveredIfComplete()` được gọi sau khi save
- [ ] Chỉ gọi khi mark thành công (item trước đó chưa delivered)
- [ ] Event được dispatch đúng thứ tự (sau save changes)

**Estimated scope:** XS (1 file, 1-2 dòng)

---

### Phase 4: Application Layer — Event Handler

#### Task 4.1 — Tạo `OrderFullyDeliveredAllocationReleaseHandler`
**Mô tả:** Handler lắng nghe `OrderFullyDelivered` và release tất cả pending allocations của order đó.

**File:** `NamEcommerce/Application/NamEcommerce.Application.Services/Events/Orders/OrderFullyDeliveredAllocationReleaseHandler.cs` (mới)

**Logic:**
```csharp
public sealed class OrderFullyDeliveredAllocationReleaseHandler 
    : INotificationHandler<OrderFullyDelivered>
{
    private readonly IPurchaseOrderAllocationManager _allocationManager;
    
    public Task Handle(OrderFullyDelivered notification, CancellationToken cancellationToken)
        => _allocationManager.ReleaseAllocationsForOrderAsync(notification.OrderId);
}
```

**Acceptance criteria:**
- [ ] Handler implement `INotificationHandler<OrderFullyDelivered>`
- [ ] Gọi `ReleaseAllocationsForOrderAsync(orderId)` 
- [ ] Không throw exception nếu không có allocation nào (ReleaseAllocationsForOrderAsync tự xử lý)
- [ ] DI registration tự động qua MediatR scan

**Estimated scope:** S (1 file mới)

---

### Checkpoint: Toàn bộ flow

- [ ] Tạo đơn hàng có sản phẩm với allocation từ PO
- [ ] Tạo delivery note và mark Delivered
- [ ] Verify: tất cả OrderItems.IsDelivered = true
- [ ] Verify: `OrderFullyDelivered` event được raise (kiểm tra qua handler logs hoặc DB state)
- [ ] Verify: PurchaseOrderItemAllocation cho order đó đã được release (status = FullyReceived hoặc deleted)

---

## Risks and Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| Event fire nhiều lần nếu có race condition | Medium | `MarkOrderItemDeliveredAsync` check `!IsDelivered` → safe; `All(i.IsDelivered)` check ở item cuối cùng |
| Order có items = 0 (không có sản phẩm) | Low | Check `OrderItems.Any()` trước khi raise |
| `ReleaseAllocationsForOrderAsync` idempotent? | Medium | Kiểm tra implementation: nếu allocation đã FullyReceived, không thay đổi gì → safe |
| Partial delivery với nhiều DeliveryNotes | Low | Mỗi DN mark items → sau item cuối cùng check all delivered → fire event đúng 1 lần |

## Open Questions

1. **`ReleaseAllocationsForOrderAsync` có idempotent không?** — Cần verify rằng gọi lại lần 2 không gây lỗi nếu allocation đã released. Theo investigation, nó check `allocation.AllocatedQuantity > allocation.ReceivedQuantity` trước khi update → có vẻ safe.

2. **Có cần thêm timeline entry khi order fully delivered không?** — Nghiệp vụ không yêu cầu, nhưng sẽ cải thiện UX. Có thể thêm handler thứ 2 cập nhật timeline.

3. **Handler thứ 2 cần thiết không?** — Tạo nợ khách hàng (CustomerDebt) hiện do `DeliveryNoteDeliveredHandler` xử lý từng DN. Khi fully delivered, có nghiệp vụ nào khác cần trigger không? (Email thông báo, auto-complete order?)

## Build Order

```
Task 1.1 (define event)
    → Task 2.1 (entity method)  
        → Task 3.1 (manager calls method)
            → Task 4.1 (handler releases allocations)
```

Các tasks phụ thuộc tuần tự, không thể song song hóa.

## Tổng quan phạm vi

| Phase | Task | Files | Scope | Ghi chú |
|-------|------|-------|-------|---------|
| 1. Event | 1.1 | 1 | XS | Add record |
| 2. Entity | 2.1 | 1 | XS | Add method |
| 3. Manager | 3.1 | 1 | XS | Add 1-2 lines |
| 4. Handler | 4.1 | 1 | S | New file |
| **Tổng** | **4 tasks** | **4 files** | **S** | Nhỏ gọn |

Đây là feature nhỏ, tập trung, không có breaking change. Toàn bộ thay đổi nằm trong 4 files, 3 trong số đó chỉ thêm vài dòng.

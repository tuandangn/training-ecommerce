# Sales Workflow Hardening Plan

Fix toàn bộ findings từ đợt review workflow: đặt hàng → chuẩn bị hàng → giao hàng → thanh toán → trả hàng → hoàn tiền (review ngày 2026-06-10).

## Assumptions / Quyết định mặc định

- **Deposit dư** (tiền cọc chưa áp hết): chỉ track chính xác phần đã áp (`AppliedAmount`), KHÔNG tự động áp deposit chung (không gắn OrderId) vào nợ mới — kế toán chủ động dùng qua màn hình thanh toán linh động.
- **AmountToCollect vượt quá phần còn lại của đơn**: chặn cứng (throw domain exception). Cho phép thu THIẾU (phần còn lại thu sau qua debt của DN kế tiếp).
- **Order đã Completed/Cancelled**: chặn Confirm các DN còn ở Draft. DN đã Confirmed/Delivering trước khi complete vẫn được chạy tiếp (giữ behavior hiện tại của `OrderCompletedEventHandler`).
- **OrderFullyDelivered**: chỉ bắn system notification, KHÔNG auto-complete đơn (cần đối soát tiền trước khi tất toán).
- **Overdue**: tính động (`RemainingAmount > 0 && DueDateUtc < UtcNow`), không thêm giá trị vào `DebtStatus` để tránh đụng state machine.
- Agent không chạy migration trên DB thật; chỉ tạo migration code, user tự `dotnet ef database update`.
- TDD: mỗi phase viết/ chỉnh unit test trước khi sửa logic.

## Success Criteria

- Công nợ tạo từ phiếu xuất có đúng `CustomerAddress`.
- Cọc lớn hơn nợ phiếu đầu: phần dư còn nguyên, áp tiếp cho phiếu sau; mọi debt luôn thỏa `TotalAmount − PaidAmount == RemainingAmount`.
- DN chuyển `Delivered` mà handler tạo nợ/trừ kho fail → outbox tự retry, không mất sự kiện; có job đối soát phát hiện DN Delivered thiếu debt/stock movement.
- Không thể Confirm DN Draft của order đã đóng; không thể tạo/sửa DN làm Σ AmountToCollect vượt phần còn phải thu của order.
- Không thể sinh 2 chứng từ trùng `Code` khi thao tác đồng thời.
- Công nợ có hạn thanh toán, danh sách lọc được nợ quá hạn; khách có hạn mức công nợ được chặn khi vượt.
- Dead code (`PaymentStatus`, `ShippingStatus`, `ApplyReturn`) bị xoá, solution build sạch, toàn bộ test xanh.

---

## Phase 1 — Bug công nợ & tiền cọc (ưu tiên cao nhất, ảnh hưởng tiền thật)

**Files chính:** `Domain/NamEcommerce.Domain.Services/Debts/CustomerDebtManager.cs`, `Domain/NamEcommerce.Domain/Entities/Debts/CustomerPayment.cs`, `Domain/NamEcommerce.Domain/Entities/Debts/CustomerDebt.cs`

### 1.1 Fix sai field address
- `CreateDebtFromDeliveryNoteAsync` (~line 101): `CustomerAddress = customer.PhoneNumber` → `customer.Address`.

### 1.2 `CustomerDebt.ApplyPayment` không cho overshoot
- Đổi signature: `internal decimal ApplyPayment(decimal amount)` — áp `applied = Math.Min(amount, RemainingAmount)`, `PaidAmount += applied`, return `applied`. Bỏ clamp âm (không còn cần).
- Invariant mới: `PaidAmount + RemainingAmount == TotalAmount` (trừ nhánh `ApplyCreditNote` — giữ nguyên).

### 1.3 Áp cọc một phần (partial deposit application)
- `CustomerPayment`: thêm `AppliedAmount { get; private set; }`; `RemainingApplicableAmount => Amount − AppliedAmount`; đổi `MarkAsApplied()` → `ApplyAmount(decimal applied)`: cộng dồn `AppliedAmount`, set `IsApplied = AppliedAmount >= Amount`, set `AppliedOnUtc` lần đầu.
- `CreateDebtFromDeliveryNoteAsync`: vòng lặp deposit theo `PaidOnUtc` (FIFO), mỗi deposit áp `Math.Min(deposit.RemainingApplicableAmount, debt.RemainingAmount)`; dừng khi debt hết remaining.
- Query deposit đổi điều kiện `!p.IsApplied` → `p.AppliedAmount < p.Amount`.
- Migration: thêm cột `AppliedAmount` (default 0) + backfill `IsApplied = 1 → AppliedAmount = Amount`.

### 1.4 Tiền dư từ thanh toán linh động
- `RecordFlexiblePaymentForCustomerAsync`: phần dư đang lưu `PaymentType.Deposit` không OrderId → đổi sang `PaymentType.General` (đúng ngữ nghĩa "thu tiền chung") để không lẫn với cọc theo đơn; hiển thị tổng "tiền khách dư" (Σ General/Deposit có `RemainingApplicableAmount > 0`) trong `GetCustomerDebtSummaryAsync` để kế toán thấy và xử lý.

### TodoList Phase 1
- [ ] Unit tests: overshoot deposit, partial apply, FIFO nhiều deposit, invariant Total = Paid + Remaining
- [ ] Fix 1.1 address
- [ ] Sửa `ApplyPayment` + update callers (`RecordPaymentAsync`, `RecordFlexiblePaymentForCustomerAsync`, deposit loop)
- [ ] Thêm `AppliedAmount` vào `CustomerPayment` + EF mapping + migration + backfill
- [ ] Sửa deposit auto-allocation loop
- [ ] Đổi PaymentType phần tiền dư + cập nhật summary DTO
- [ ] `dotnet test --filter "FullyQualifiedName~CustomerDebt"`

---

## Phase 2 — Đưa side-effect tài chính/kho qua Transactional Outbox

**Vấn đề:** `DomainEventDispatchInterceptor` publish event **post-commit, in-process, không persist**. Handler fail → DN đã Delivered nhưng không có debt/không trừ kho, event mất vĩnh viễn (retry cùng idempotency key còn return sớm).

**Giải pháp:** marker interface để event quan trọng đi qua Outbox (đã có sẵn `IOutbox` + `OutboxProcessor`, atomic cùng transaction).

### Thiết kế
- Thêm `IReliableDomainEvent : IDomainEvent` trong `Domain.Shared/Events`.
- Sửa `DomainEventDispatchInterceptor`: override thêm `SavingChangesAsync` — các event implement `IReliableDomainEvent` được serialize thành `OutboxMessage` **trong cùng SaveChanges/transaction**; các event thường giữ nguyên publish post-commit. `SavedChangesAsync` bỏ qua reliable events (đã vào outbox).
- `OutboxProcessor` deserialize → publish qua MediatR → **handlers hiện tại giữ nguyên signature**, gần như không phải refactor handler.
- Đánh dấu reliable: `DeliveryNoteDelivered`, `CustomerReturnConfirmed`, `CustomerReturnOverRefunded`, `VendorReturnConfirmed`, `CustomerRefundCompleted`, `OrderCancelled`, `OrderCompleted`.
- Rà idempotency từng handler (đa số đã có: debt check theo `DeliveryNoteId`, `DispatchStockUpToAsync`, `GeneratedGoodsReceiptId` guard, refund check theo `CustomerReturnId`) — bổ sung nếu thiếu.
- Job đối soát (HostedService chạy định kỳ): DN `Delivered` quá X phút mà không có `CustomerDebt` (khi `AmountToCollect > 0`) hoặc không có `StockMovementLog` tương ứng → ghi system notification cảnh báo.

### TodoList Phase 2
- [ ] Unit/integration tests: reliable event vào outbox cùng transaction; handler fail → message giữ lại để retry; idempotent khi xử lý 2 lần
- [ ] `IReliableDomainEvent` + sửa interceptor (SavingChanges path)
- [ ] OutboxProcessor: deserialize domain event + retry policy + dead-letter (đánh dấu failed sau N lần, có màn hình/log xem)
- [ ] Đánh dấu các event reliable + rà idempotency handlers
- [ ] Job đối soát DN Delivered thiếu debt/stock movement
- [ ] `dotnet test` toàn bộ

---

## Phase 3 — Ràng buộc workflow Order ↔ DeliveryNote

**Files chính:** `DeliveryNoteManager.cs`, `OrderAppService.cs`, `Order.cs`

### 3.1 Chặn DN mồ côi
- `DeliveryNoteManager.ConfirmAsync`: load Order, throw nếu `OrderStatus` là Completed/Cancelled (chỉ với `SourceType == ToCustomer`).
- `CompleteOrderAsync` (AppService): cảnh báo/chặn nếu order còn DN **Draft** (yêu cầu xác nhận hoặc hủy trước khi tất toán).

### 3.2 Đối soát AmountToCollect
- Khi `CreateFromOrderAsync` và khi sửa AmountToCollect: `remaining = Order.OrderTotal + dto.Surcharge − Σ AmountToCollect(DN active của order)`; nếu `dto.AmountToCollect > remaining` → throw `AmountToCollectExceedsOrderRemainingException` (exception mới trong `Domain.Shared/Exceptions/DeliveryNotes/`).
- UI tạo DN: prefill AmountToCollect = remaining gợi ý.

### 3.3 OrderFullyDelivered có handler
- Handler mới: bắn system notification ("Đơn X đã giao đủ — kiểm tra tất toán") qua NotificationCenter.

### 3.4 Dọn exception handling
- `CancelOrderAsync`/`CompleteOrderAsync`: catch `NamEcommerceDomainException` (message đã localize) thay vì `Exception`; lỗi lạ để bubble lên middleware.

### TodoList Phase 3
- [ ] Unit tests: confirm DN của order đã đóng bị chặn; AmountToCollect vượt remaining bị chặn; thu thiếu vẫn cho phép
- [ ] 3.1 guard Confirm DN + guard Complete order còn DN Draft
- [ ] 3.2 validation + exception + prefill UI
- [ ] 3.3 notification handler
- [ ] 3.4 narrow catch
- [ ] `dotnet test --filter "FullyQualifiedName~DeliveryNote"`

---

## Phase 4 — Sinh mã chứng từ an toàn (chống trùng khi đồng thời)

- Unique index trên `Code`: Order, DeliveryNote, CustomerDebt, CustomerPayment, CustomerCreditNote, CustomerRefund, CustomerReturn, VendorReturn, GoodsReceipt, PurchaseOrder (1 migration; kiểm tra data trùng trước khi add index).
- Helper chung `Domain.Services/Common/DocumentCodeRetry` (hoặc extension trên repository): wrap `InsertAsync`, catch unique-violation (`DbUpdateException` → SQL error 2601/2627), regenerate code, retry tối đa 3 lần.
- Áp vào toàn bộ chỗ `Generate*CodeAsync` count-based hiện tại (giữ format mã hiện có để không gãy nghiệp vụ).

### TodoList Phase 4
- [ ] Script kiểm tra Code trùng hiện có trên từng bảng (SQL trong plan implement)
- [ ] Migration unique indexes
- [ ] Retry helper + áp vào các manager
- [ ] Test mô phỏng trùng code (insert 2 lần cùng code → lần 2 regenerate)

---

## Phase 5 — Công nợ nâng cao + dọn dẹp

### 5.1 Hạn thanh toán (DueDate)
- `Customer.PaymentTermDays (int?)` — null = dùng default từ AppConfig (`DefaultCustomerPaymentTermDays`).
- `CreateDebtFromDeliveryNoteAsync`: `DueDateUtc = DeliveredOn + termDays`.
- DTO/Model thêm `IsOverdue` computed; màn hình công nợ: filter + badge quá hạn; migration cột mới.

### 5.2 Hạn mức công nợ (Credit limit)
- `Customer.CreditLimit (decimal?)` — null = không giới hạn.
- Check tại `DeliveryNoteManager.ConfirmAsync`: `Σ RemainingAmount(khách) + AmountToCollect > CreditLimit` → throw (message nêu rõ số vượt). Màn hình customer edit thêm 2 field.

### 5.3 Dọn dead code
- Xoá `Enums/Orders/PaymentStatus.cs`, `ShippingStatus.cs` (không nơi nào dùng).
- Xoá `CustomerDebt.ApplyReturn` + `VendorDebt.ApplyReturn` (đã thay bằng CreditNote flow; chỉ còn doc-comment tham chiếu — cập nhật doc `VendorReturnEvents.cs`).

### TodoList Phase 5
- [ ] Unit tests: DueDate set đúng theo term; credit limit chặn đúng ngưỡng
- [ ] 5.1 fields + migration + UI công nợ
- [ ] 5.2 field + guard + UI customer
- [ ] 5.3 xoá dead code, build sạch
- [ ] `dotnet test` toàn bộ

---

## Thứ tự thực hiện & phụ thuộc

```
Phase 1 (độc lập, làm ngay)
Phase 2 (độc lập, nên làm trước Phase 3 để guard mới cũng đi qua outbox-tested path)
Phase 3 (sau Phase 2)
Phase 4 (độc lập)
Phase 5 (cuối, có 2 migration)
```

Mỗi phase là 1 PR/commit riêng, viết `plans/Orders/implement_sales_workflow_hardening_phase{N}.md` khi bắt đầu code.

## Verification Plan

- `dotnet build NamEcommerce.sln`
- `dotnet test` (toàn bộ sau mỗi phase)
- Filter nhanh: `dotnet test --filter "FullyQualifiedName~CustomerDebtManager"`, `~DeliveryNoteManager`, `~CustomerReturnManager`
- Smoke test tay sau Phase 2: giao 1 DN, kill OutboxProcessor giữa chừng, restart → debt + stock movement vẫn được tạo đúng 1 lần
- Sau Phase 4/5: tạo migration, user tự chạy `dotnet ef database update` và xác nhận

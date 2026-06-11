# Return Module — Bug Review Plan

> Reviewed: 2026-06-10
> Scope: CustomerReturn, VendorReturn (Domain + Application layer)
> Status: **PENDING APPROVAL**

---

## CRITICAL

### [1] TOCTOU — Double financial write khi confirm đồng thời

**Files:**
- `Domain/NamEcommerce.Domain.Services/Returns/CustomerReturnManager.cs` — `FinalizeConfirmAsync`
- `Domain/NamEcommerce.Domain.Services/Returns/VendorReturnManager.cs` — `FinalizeConfirmAsync`

**Problem:** Guard check `GeneratedGoodsReceiptId.HasValue` đọc từ in-memory reader (không lock DB). Hai request confirm đồng thời đều pass guard → tạo 2 GoodsReceipt, 2 credit note, 2 expense cho cùng 1 return.

**Fix options:**
- (A) Thêm unique constraint DB trên cột `GeneratedGoodsReceiptId` / `GeneratedDeliveryNoteId`
- (B) Dùng optimistic concurrency (`RowVersion`) trên entity → request thứ 2 fail với concurrency exception

---

### [2] Free-form VendorReturn (không có GR/PO) — validation logic lỗi

**File:** `Domain/NamEcommerce.Domain.Services/Returns/VendorReturnManager.cs`
- `GetTotalReceivedQuantity` — khi cả 2 ID null → trả về `0` → mọi `AcceptedQuantity > 0` đều throw (block hoàn toàn)
- `GetTotalConfirmedReturnQuantityAsync` — không lọc gì khi cả 2 ID null → aggregate **tất cả** confirmed returns của mọi vendor → `previouslyReturned` sai

**Fix options:**
- (A) Block rõ ràng free-form return ở tầng create (throw nếu cả 2 ID null)
- (B) Implement lookup đúng cho free-form case (lọc theo `VendorId` + period)

---

## HIGH

### [3] Magic number `2` cho `Confirmed` status — dễ fix

**File:** `Domain/NamEcommerce.Domain.Services/Returns/VendorReturnManager.cs:277`

```csharp
// HIỆN TẠI (SAI)
.Where(r => (int)r.Status == 2 // Confirmed

// FIX
.Where(r => r.Status == VendorReturnStatus.Confirmed)
```

---

### [4] `ReserveCompensatedQuantityAsync` duplicate + gọi 2 lần

**Files:**
- `Domain/NamEcommerce.Domain.Services/Returns/CustomerReturnManager.cs` (lines 271–296)
- `Application/NamEcommerce.Application.Services/Events/Returns/CustomerReturnConfirmedEventHandler.cs` (lines 67–123)

Logic reservation giống hệt ở 2 chỗ. Handler gọi lại reservation lúc Confirm dù đã gọi lúc Inspecting.

**Fix:** Xóa call reservation trong `CustomerReturnConfirmedEventHandler` — chỉ gọi 1 lần ở `MoveToInspectingAsync`.

---

### [5] `netRefundAmount` tính từ DTO trong handler, `FinalizeConfirmAsync` re-fetch entity độc lập

**Files:**
- `Application/NamEcommerce.Application.Services/Events/Returns/CustomerReturnConfirmedEventHandler.cs:62`
- `Domain/NamEcommerce.Domain.Services/Returns/CustomerReturnManager.cs:453`

Hai nguồn data có thể lệch (cached snapshot vs. DB). `AdditionalCost` trong Expense có thể bị sai.

**Fix:** Tính `netRefundAmount` bên trong `FinalizeConfirmAsync` từ entity vừa re-fetch, không nhận từ ngoài.

---

### [6] Source debt lookup không exclude cancelled debts

**Files:**
- `Domain/NamEcommerce.Domain.Services/Debts/CustomerDebtManager.cs` — `ApplyCreditNoteFromCustomerReturnAsync`
- `Domain/NamEcommerce.Domain.Services/Debts/VendorDebtManager.cs` — `ResolveSourceDebt`

Không filter `Status != Cancelled` → có thể allocate credit note vào debt đã cancel.

**Fix:** Thêm `&& d.Status != DebtStatus.Cancelled` vào where clause.

---

### [7] Code generation có race condition

**Files:**
- `CustomerReturnManager.cs` — `GenerateCode()`
- `VendorReturnManager.cs` — `GenerateCode()`

`Count().StartsWith()` không atomic → 2 request đồng thời tạo cùng `Code`.

**Fix:** Dùng retry loop với unique constraint, hoặc DB sequence.

---

### [8] `MarkOverRefunded` pass `null` cho `overRefundedDebtId`

**File:** `Domain/NamEcommerce.Domain.Services/Returns/CustomerReturnManager.cs:488`

```csharp
customerReturn.MarkOverRefunded(creditNote.RemainingAmount, null); // debtId luôn null
```

`CustomerRefund.CustomerDebtId` sẽ luôn `null` → mất traceability.

**Fix:** Sau FIFO loop, pass ID của debt cuối cùng bị partial-clear nếu có.

---

## MEDIUM

### [9] B02 Income Statement — temporal mismatch

**File:** `Application/NamEcommerce.Application.Services/Finance/AccountingReportService.cs`

`SalesReturns` filter theo `CreditNote.CreatedOnUtc`, nhưng `InventoryCostLedgerEntry` filter theo `OccurredAtUtc`. Return tháng này nhưng credit note tạo tháng sau → revenue deduction và cost deduction lệch period.

**Fix:** Filter credit note theo `SourceDeliveryNote.DeliveredOnUtc` hoặc document rõ policy.

---

## LOW / SUGGESTIONS

### [S1] Commented-out validation chưa implement

**File:** `Domain/NamEcommerce.Domain.Services/Returns/CustomerReturnManager.cs:514`
```csharp
//*TODO*
//if (deliveryNote.Status != DeliveryNoteStatus.Delivered)
//    throw new ReturnDataIsInvalidException(...);
```
Hiện cho phép tạo return cho DeliveryNote chưa delivered.

### [S2] VendorReturn.WarehouseId dùng `Guid.Empty` fallback thay vì `Guid?`

Inconsistent với `CustomerReturn` dùng `Guid?`. `VendorReturn` constructor dùng `warehouseId ?? Guid.Empty`.

### [S3] Unbounded in-memory load

`CustomerReturnManager.GetDeliveredNotesForReturnAsync` load all delivery notes của customer khi `deliveryNoteId = null`. Chấp nhận được với dataset nhỏ, cần phân trang khi scale.

### [S4] Async methods without await

`CustomerDebtManager.GenerateDebtCodeAsync`, `GeneratePaymentCodeAsync` và tương tự trong `VendorDebtManager` — mark `async` nhưng không `await`.

---

## Priority Matrix

| # | Issue | Severity | Effort | Priority |
|---|-------|----------|--------|----------|
| 1 | TOCTOU double financial write | CRITICAL | Medium | P0 |
| 3 | Magic number status | HIGH | Low (1 line) | P1 |
| 2 | Free-form VendorReturn logic | CRITICAL | Medium | P1 |
| 6 | Cancelled debt allocation | HIGH | Low | P2 |
| 8 | null debtId in OverRefunded | HIGH | Low | P2 |
| 4 | Duplicate reservation logic | HIGH | Medium | P2 |
| 7 | Code generation race | HIGH | Medium | P3 |
| 5 | netRefundAmount stale data | HIGH | Medium | P3 |
| 9 | B02 temporal mismatch | MEDIUM | Medium | P3 |
| S1 | TODO disabled validation | LOW | Low | P4 |
| S2 | WarehouseId Guid.Empty | LOW | Low | P4 |
| S3 | Unbounded memory load | LOW | Low | P4 |
| S4 | Async without await | LOW | Low | P4 |

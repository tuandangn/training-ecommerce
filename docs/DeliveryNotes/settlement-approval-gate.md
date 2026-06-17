# Duyệt thu hụt khi giao hàng (Settlement Approval Gate)

> **Module**: `DeliveryNotes`
> **Ngày triển khai**: 2026-06-17
> **Phạm vi**: Chặn shipper tự hoàn tất giao hàng khi **thu hụt** (trả hàng / khách không thanh toán đủ); bắt buộc admin duyệt số tiền trước.
> Plan: `plans/DeliveryNotes/settlement_approval_gate_plan.md` · Tracking: `..._implement.md`

---

## 1. Vấn đề & mục tiêu

Trước đây shipper tự nhập số lượng trả về + tiền thu rồi bấm "Đã giao" — không ai kiểm soát việc giao nợ, và số tiền khách phải thanh toán khi trả hàng mơ hồ (nhánh non-ShowPrice **không** trừ giá trị hàng trả).

Mục tiêu: **bất kỳ khi nào thu < COD gốc** (do trả hàng hoặc khách từ chối/giảm) → phiếu treo **chờ admin duyệt**; admin xem số tự tính, sửa/chốt số phải thu; shipper thu **đúng** số đó rồi mới xác nhận đã giao.

---

## 2. Luồng nghiệp vụ

```
Shipper giao hàng (DeliveryNote ở Delivering, stock đã xuất, chưa sinh công nợ)
 ├─ Giao đủ + thu đủ COD ───────────────────► Đã giao (như cũ, KHÔNG cần admin)
 └─ Thu hụt (trả hàng HOẶC thu < COD)
        └─► Shipper "Gửi admin duyệt"  → SettlementApproval = PendingApproval
              └─► Admin xem (số tự tính = COD − giá trị hàng trả)
                    ├─ Duyệt: chốt "số thu duyệt" (+phí phát sinh tùy chọn) → Approved
                    │     └─► Shipper thu đúng số đó → "Xác nhận đã giao" → Delivered
                    │           (phần COD − số thu = công nợ khách)
                    └─ Từ chối: → Rejected → tự CancelAsync (nhập lại kho + hủy phiếu + mang hàng về)
```

**Trigger thu hụt (server-side, không tin client):**
`Σ RejectedQty > 0` **HOẶC** `cashCollected < amountToCollect (sau khi trừ hàng trả)`.

---

## 3. Mô hình tiền (quan trọng)

Không ghi đè `AmountToCollect` bằng số admin duyệt. Hai con số tách bạch:

| Khái niệm | Công thức / nguồn | Vai trò |
|---|---|---|
| **Bill** (khách phải thanh toán) | `COD gốc − Σ(RejectedQty×UnitPrice) + AgreedCustomerCharge` | → `note.AmountToCollect`, cơ sở **công nợ** |
| **ApprovedAmountToCollect** (thu ngay) | Admin chốt, `0 ≤ x ≤ Bill` | → `DeliveryCashCollectedAmount` |
| **Công nợ còn lại** (giao nợ) | `Bill − ApprovedAmountToCollect` | **tự phát sinh** qua double-entry hiện có |

Double-entry giữ nguyên: công nợ tạo full ở `DeliveryNoteDelivered`; CustomerReturn (tự sinh từ hàng trả) credit phần trả khi Confirmed; cashier credit phần tiền thu khi `ConfirmCashHandover` → phần hụt nằm lại thành công nợ.

> **Fix kèm theo:** `DeliveryNoteManager.ResolveDeliveryAcceptance` nhánh non-ShowPrice đổi sang
> `amountToCollect = Max(0, AmountToCollect − rejectedGoodsAmount + agreedCharge)` (trước đây không trừ hàng trả).

---

## 4. Trạng thái

Sub-state **song song** với `DeliveryNoteStatus` (không thêm giá trị status mới — tránh sửa loạt switch), giống cách `DeliveryConfirmationStatus` chạy cho direct-ship.

`DeliverySettlementApprovalStatus`: `NotRequired=0` · `PendingApproval=1` · `Approved=2` · `Rejected=3`.

`PendingApproval`/`Rejected` chỉ tồn tại khi `Status = Delivering`. Sau khi shipper hoàn tất từ `Approved` → `Status = Delivered`, sub-state giữ `Approved` để audit.

---

## 5. Thành phần theo tầng

### Domain (`NamEcommerce.Domain` / `.Domain.Shared` / `.Domain.Services`)
- Enum `DeliverySettlementApprovalStatus`.
- Entity con `DeliveryNoteSettlementItem` (snapshot accepted/rejected mỗi dòng — đọc lại lúc completion).
- `DeliveryNote`: fields settlement + methods `RequestSettlementApproval` / `ApproveSettlement` / `RejectSettlement`; guard chặn `MarkDelivered` khi `PendingApproval`.
- Events: `DeliverySettlementApprovalRequested` / `Approved` / `Rejected`.
- `DeliveryNoteManager`: `RequestSettlementApprovalAsync`, `ApproveSettlementAsync`, `RejectSettlementAsync` (→ `CancelAsync` tự động), `CompleteApprovedSettlementAsync` (dựng acceptance từ `SettlementItems` → reuse `MarkDeliveredAsync`); **server guard** chặn shipper bypass.
- EF: `DeliveryNoteSettlementItemMap` + cột mới trong `DeliveryNoteMap`.

### Application
- App DTOs `RequestDeliverySettlementAppDto` / `ApproveDeliverySettlementAppDto` (+`Validate()`).
- `IDeliveryNoteAppService`: Request / Approve / Reject / CompleteApproved (trả `CommonActionResultDto`).
- Settlement fields nối qua `DeliveryNoteDto` → `DeliveryNoteAppDto`.
- Notification `DeliverySettlementApprovalRequested` (type 603) → nhóm quản lý khi shipper gửi duyệt.

### Web (Presentation)
- Commands + handlers: `RequestDeliverySettlement` / `ApproveDeliverySettlement` / `RejectDeliverySettlement` / `CompleteApprovedDelivery`.
- `DeliveryNoteController`: `ApproveSettlement` / `RejectSettlement` — **chỉ Admin** (`User.IsInRole(SystemUserRoleNames.Admin)`).
- `DeliveryMobileController`: `RequestSettlement` (upload ảnh + acceptance + lý do) · `ConfirmApprovedDelivery` (không upload lại).
- UI:
  - Admin `DeliveryNote/Details.cshtml`: panel duyệt (bảng nhận/trả, số tự tính, form Duyệt/Từ chối).
  - Mobile `DeliveryMobile/Run.cshtml` + `wwwroot/modules/DeliverySettlement.js`: phát hiện thu hụt → nút "Gửi duyệt" / trạng thái chờ / đã duyệt-thu / bị từ chối.
  - `DeliveryRun/Details.cshtml`: chip "Chờ duyệt thu hụt".
- Resource: `Error.DeliverySettlement.NotPending` / `AlreadyPending` / `ReasonRequired` / `NotApproved` / `ApprovalRequired`.

---

## 6. Phân quyền & chống bypass

- **Duyệt/Từ chối: chỉ Admin** (kiểm tra role trong controller, không chỉ policy `DeliveryNotes.Manage`).
- **Chống bypass JS:** `MarkDeliveredAsync` chặn hoàn tất khi `Source=MobilePwa` + thu hụt + chưa `Approved` → `Error.DeliverySettlement.ApprovalRequired`. Admin hoàn tất trực tiếp (Source≠MobilePwa) không bị chặn — admin là người có thẩm quyền.

---

## 7. Migration cần tạo

Schema additive (an toàn cho phiếu cũ — mặc định `NotRequired`):
- Bảng mới `DeliveryNoteSettlementItem` (Id, DeliveryNoteId FK, DeliveryNoteItemId, AcceptedQuantity, RejectedQuantity, RejectReason).
- Cột mới trên `DeliveryNote`: `SettlementApproval` (int, default 0), `ProposedAmountToCollect`, `ApprovedAmountToCollect`, `ApprovedAgreedCustomerCharge` (decimal 18,2 null), `ApprovedAgreedChargeReason`, `SettlementReason`, `SettlementAdminNote` (nvarchar null), `SettlementRequestedByUserId`, `SettlementRequestedOnUtc`, `SettlementApprovedByUserId`, `SettlementApprovedOnUtc`.

---

## 8. Kịch bản kiểm thử

1. **Giao đủ, thu đủ** → hoàn tất thẳng, không qua duyệt.
2. **Trả 1 phần, thu đủ phần nhận** → gửi duyệt → admin để số thu = số tự tính → duyệt → shipper xác nhận → Delivered, công nợ 0, CustomerReturn tạo cho phần trả.
3. **Khách thanh toán thiếu** → gửi duyệt → admin hạ số thu → duyệt → Delivered, phần hụt thành công nợ.
4. **Khách từ chối** → gửi duyệt → admin Từ chối → phiếu Cancelled, hàng nhập lại kho.

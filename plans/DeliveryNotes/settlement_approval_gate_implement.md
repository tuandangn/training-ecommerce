# Settlement Approval Gate — Implementation Tracking

Plan: `settlement_approval_gate_plan.md`. Branch: dev-assistant.

## Phase 1 — Domain ✅ (build xanh)
- [x] Enum `DeliverySettlementApprovalStatus` (NotRequired/PendingApproval/Approved/Rejected).
- [x] Entity `DeliveryNoteSettlementItem` (owned child).
- [x] `DeliveryNote`: fields settlement + `_settlementItems` + methods `RequestSettlementApproval`/`ApproveSettlement`/`RejectSettlement` + guard PendingApproval trong `MarkDelivered`.
- [x] 3 events trong `DeliveryNoteEvents.cs`.
- [x] EF: `DeliveryNoteSettlementItemMap` + cột mới trong `DeliveryNoteMap`.

## Phase 2 — Domain Services ✅ (build xanh)
- [x] Domain DTOs `RequestDeliverySettlementDto`, `ApproveDeliverySettlementDto`.
- [x] `IDeliveryNoteManager`: 4 method (Request/Approve/Reject/CompleteApproved).
- [x] Manager impl: Request (tính proposed = COD − giá trị trả), Approve, Reject (auto `CancelAsync`), CompleteApproved (dựng acceptance từ SettlementItems → reuse `MarkDeliveredAsync`).
- [x] **Fix money**: `ResolveDeliveryAcceptance` nhánh non-ShowPrice trừ `rejectedGoodsAmount`.

## ⚠️ MIGRATION CẦN TẠO (user tự làm)
Sau Phase 1+2, schema đã đổi:
- Bảng mới `DeliveryNoteSettlementItem` (Id, DeliveryNoteId FK, DeliveryNoteItemId, AcceptedQuantity, RejectedQuantity, RejectReason).
- Cột mới trên `DeliveryNote`: `SettlementApproval`(int default 0), `ProposedAmountToCollect`, `ApprovedAmountToCollect`, `ApprovedAgreedCustomerCharge` (decimal null), `ApprovedAgreedChargeReason`, `SettlementReason`, `SettlementAdminNote` (nvarchar null), `SettlementRequestedByUserId`, `SettlementRequestedOnUtc`, `SettlementApprovedByUserId`, `SettlementApprovedOnUtc`.

## Phase 3 — Application ✅ (build xanh)
- [x] App DTOs `RequestDeliverySettlementAppDto`, `ApproveDeliverySettlementAppDto` + `Validate()`.
- [x] `IDeliveryNoteAppService` + impl 4 method (Request/Approve/Reject/CompleteApproved), return `CommonActionResultDto`, không throw.
- [x] Settlement fields chuỗi mapping: `DeliveryNoteDto`(+MapToDto manager) → `DeliveryNoteAppDto`(+ToDto ext); thêm `DeliveryNoteSettlementItemDto`/`AppDto`.
- [x] Notification: type `DeliverySettlementApprovalRequested=603` + composer; tạo trực tiếp trong app service khi request (→ Admin/Manager). Approved/Rejected: shipper xem qua refresh (chưa push, đủ cho VLXD đang chờ).

## Phase 4 — Web ✅ (build xanh)
- [x] Commands `RequestDeliverySettlementCommand`/`ApproveDeliverySettlementCommand`/`RejectDeliverySettlementCommand`/`CompleteApprovedDeliveryCommand` (all `ICommand<CommonActionResultModel>`) + handlers.
- [x] `DeliveryNoteController`: `ApproveSettlement`/`RejectSettlement` (chỉ Admin: `User.IsInRole(SystemUserRoleNames.Admin)` + Forbid) → redirect Details. Guard MarkDelivered đã ở domain.
- [x] `DeliveryMobileController`: `RequestSettlement` (upload proof + acceptance + reason) + `ConfirmApprovedDelivery` (no re-upload → CompleteApproved).
- [x] Resource strings 4 ErrorCode (resx + vi-VN).

## Phase 5 — UI ✅ (build xanh)
- [x] Settlement fields vào `DeliveryRunItemModel` (+factory) và `DeliveryNoteDetailsModel` (+factory, join lấy tên/đơn giá).
- [x] Admin panel duyệt thu hụt trong `DeliveryNote/Details.cshtml` (chỉ Admin: `User.IsInRole(Admin)`): bảng nhận/trả, COD/giá trị trả/số tự tính, form Duyệt (số thu + phí + ghi chú) + form Từ chối (confirm). Banner "đã duyệt" khi Approved.
- [x] Mobile `Run.cshtml`: 3 nhánh trạng thái (chờ duyệt / đã duyệt-thu+nút xác nhận / bị từ chối) + form augment (data-unit-price, reason wrap, nút "Gửi duyệt"/"Đã giao") + module `DeliverySettlement.js` (phát hiện thu hụt → toggle nút + POST RequestSettlement / ConfirmApprovedDelivery).
- [x] Run `Details.cshtml`: chip "Chờ duyệt thu hụt".
- [x] **Server guard** trong `MarkDeliveredAsync`: shipper (Source=MobilePwa) không hoàn tất được khi thu hụt mà chưa Approved → `Error.DeliverySettlement.ApprovalRequired` (+resx). Chống bypass JS.

## Phase 6 — Docs ✅
- [x] `docs/DeliveryNotes/settlement-approval-gate.md` — flow, money model, states, thành phần theo tầng, migration, kịch bản test.

## ✅ FEATURE FUNCTIONALLY COMPLETE — cần: (1) user tạo migration, (2) smoke test thủ công 3 case (giao đủ / trả hàng / từ chối thanh toán).

# Delivery Settlement Approval Gate — Implementation Plan

> Nối tiếp `partial_acceptance_cash_handover_plan.md`. Bổ sung phần đã defer trong plan đó:
> "Cash shortage/loss settlement accounting" và "Allowing cashier/admin to confirm an amount
> different from shipper-reported cash".

**Goal:** Khi shipper giao hàng mà **thu hụt** so với COD gốc (do khách trả lại hàng, xin giảm,
hoặc từ chối thanh toán), shipper **không được tự hoàn tất**. Phiếu treo chờ **admin duyệt**.
App tự tính số tiền gợi ý sau trả hàng; admin xem / sửa / chốt số phải thu; shipper thu **đúng**
số admin chốt rồi mới xác nhận đã giao.

**Architecture:** Giữ `DeliveryNote` là source of truth. Thêm **một sub-state song song** với
`Status` (giống cách `DeliveryConfirmationStatus` đang chạy song song cho direct-ship) — KHÔNG
thêm giá trị mới vào `DeliveryNoteStatus` để tránh phải sửa hàng loạt switch. Tái dùng
`ResolveDeliveryAcceptance`, `CustomerReturn` tự sinh, `ICustomerDebtManager`, và bước thủ quỹ
ở `DeliveryRun`.

**Tech stack:** ASP.NET Core MVC/Razor, MediatR, DDD layers, EF Core SQL Server, DeliveryMobile PWA.

---

## 1. Quy tắc lõi (đã chốt với user)

- **Giao bình thường** = khách nhận đủ 100% hàng **và** trả đủ COD gốc → hoàn tất trực tiếp như hiện tại, KHÔNG cần admin.
- **Thu hụt** = bất kỳ trường hợp nào sau khi giao mà số tiền thu < COD gốc:
  - khách trả lại một phần/toàn bộ hàng (`Σ RejectedQuantity > 0`), HOẶC
  - khách thu ít hơn số phải thu (từ chối/xin giảm) — shipper chủ động bấm "Khách từ chối/thu thiếu".
- Mọi trường hợp thu hụt → phiếu vào trạng thái **chờ admin duyệt**; shipper **bị chặn** hoàn tất.
- Admin duyệt: xem số tự tính, **được sửa** số phải thu cuối, **Approve** hoặc **Reject**.
- Reject → shipper **mang hàng về**, phiếu KHÔNG chuyển Delivered (giao thất bại, xử lý lại sau).
- Shipper **chờ** admin online (VLXD chờ vài phút là bình thường).

### Trigger chính xác (server-side, không tin client)
```
requiresApproval =
    (Σ line.RejectedQuantity > 0)
    || (proposedCashCollected < deliveryNote.AmountToCollect)   // COD gốc
    || shipperFlaggedShortfall
```

---

## 1b. Money model (REFINED — authoritative, 2026-06-17)

Sau khi rà lại double-entry hiện có, **không ghi đè `AmountToCollect` bằng số admin duyệt**. Hai con số tách bạch:

- **Bill (số khách phải thanh toán)** = `COD gốc − Σ(RejectedQty×UnitPrice) + AgreedCustomerCharge`.
  → set vào `note.AmountToCollect` lúc completion (qua acceptance resolution). Đây là cơ sở **công nợ**.
- **`ApprovedAmountToCollect`** = **tiền thu ngay** admin duyệt, `0 ≤ x ≤ Bill`.
  → lúc completion set vào `DeliveryCashCollectedAmount` (tiền shipper thu).
- **Công nợ còn lại (giao nợ)** = `Bill − ApprovedAmountToCollect` → **tự phát sinh**, không cần code thêm:
  công nợ tạo full ở `DeliveryNoteDelivered`, return credit phần trả, cashier credit phần thu → phần hụt nằm lại.

Vì sao hợp lý nhất:
- Case trả hàng + trả đủ tiền kept-goods: admin để `ApprovedAmountToCollect = Bill` (default) → công nợ 0. Rõ "số khách phải thanh toán".
- Case từ chối/thu thiếu: admin hạ `ApprovedAmountToCollect < Bill` → phần hụt thành công nợ (giao nợ).
- Phí restocking: admin nhập `AgreedCustomerCharge` (default 0) → tăng Bill (khách nợ thêm). Cơ chế đã có sẵn.

**Fix kèm theo (sửa chỗ mơ hồ gốc):** trong `ResolveDeliveryAcceptance`, nhánh non-ShowPrice đổi
`amountToCollect = Max(0, note.AmountToCollect − rejectedGoodsAmount + agreedCharge)` (hiện đang KHÔNG trừ trả hàng).
Khi không trả hàng (`rejectedGoodsAmount=0`) giữ nguyên hành vi cũ → surgical, không vỡ happy path.

### 3 quyết định user chốt (2026-06-17)
1. **Phí restocking**: dùng `AgreedCustomerCharge` (đã có), expose ở panel duyệt, default 0.
2. **Ai duyệt**: **chỉ Admin** (`User.IsInRole(SystemUserRoleNames.Admin)`), không phải WarehouseManager.
3. **Khi admin Reject**: **tự động** — gọi luồng `CancelAsync` hiện có để nhập lại kho + restore reservation + hủy phiếu + cascade cancel CustomerReturn. Shipper được notify "mang hàng về".

### Lưu acceptance giữa request → completion
Dùng **owned child collection `DeliveryNoteSettlementItem`** (DeliveryNoteItemId, AcceptedQuantity,
RejectedQuantity, RejectReason) trên `DeliveryNote`. Tạo lúc request, đọc lúc completion để feed acceptance
vào `MarkDeliveredAsync` (giữ nguyên flow tạo return + công nợ hiện có). Không tạo CustomerReturn sớm.

---

## 2. Luồng thực tế (state walkthrough)

Tại thời điểm shipper đứng trước cửa khách: `Status = Delivering`, **stock đã xuất** (đã dispatch ở
`DeliveryNoteDelivering` event khi bàn giao chuyến), **chưa sinh công nợ** (công nợ chỉ sinh ở
`DeliveryNoteDelivered`). Đây là điều kiện lý tưởng để "treo" mà không đụng tồn kho.

### Case A — Giao đủ, thu đủ (đường happy, giữ nguyên)
1. Shipper nhập trả về = 0 cho mọi dòng, thu = COD.
2. Submit → `MarkDelivered` chạy thẳng → `Delivered`, sinh công nợ, cashier xác nhận sau.

### Case B — Trả hàng một phần (cần duyệt)
1. Shipper nhập SL trả từng dòng + lý do + ảnh, bấm **"Gửi duyệt"**.
2. App tính `ProposedAmountToCollect = AmountToCollect − Σ(RejectedQty × UnitPrice)` (floor 0).
3. Phiếu → `SettlementApproval = PendingApproval`. Tạo `CustomerReturn (Draft→Inspecting)` cho phần trả (tái dùng `CreateCustomerReturnFromRejectedAcceptanceAsync`). Lưu proof + receiver + reason + proposed amount lên note. Notify admin.
4. **Admin** mở phiếu: thấy bảng nhận/trả, số tự tính. Sửa số phải thu nếu cần (vd tính phí restocking qua `AgreedCustomerCharge`). Bấm **Duyệt** → `Approved`, lưu `ApprovedAmountToCollect`. Notify shipper.
5. **Shipper** thấy "Đã duyệt — thu đúng X". Thu X, bấm **"Xác nhận đã giao"** → `MarkDelivered` với `AmountToCollect = ApprovedAmountToCollect`, cash = X → `Delivered`.

### Case C — Khách từ chối thanh toán / thu thiếu (cần duyệt)
1. Shipper bấm **"Khách từ chối/thu thiếu"**, nhập số đề xuất thu (có thể 0) + lý do + ảnh, **"Gửi duyệt"**.
2. Phiếu → `PendingApproval`, `ProposedAmountToCollect = số shipper đề xuất`. Notify admin.
3. Admin chọn:
   - **Duyệt giao nợ:** set `ApprovedAmountToCollect` (phần thu thực), phần còn lại thành công nợ. → `Approved`.
   - **Reject:** lý do → `SettlementApproval = Rejected`. Shipper mang hàng về, phiếu vẫn `Delivering` (không Delivered). Xử lý giao lại/hủy theo nghiệp vụ hiện có.
4. Nếu duyệt: shipper thu đúng `ApprovedAmountToCollect`, xác nhận đã giao → `Delivered`.

---

## 3. State model

Thêm sub-state, **không đổi** `DeliveryNoteStatus`:

```
New enum: NamEcommerce.Domain.Shared.Enums.DeliveryNotes.DeliverySettlementApprovalStatus
    NotRequired    = 0   // mặc định / giao bình thường
    PendingApproval = 1  // shipper đã gửi duyệt, chờ admin
    Approved        = 2  // admin đã duyệt số phải thu
    Rejected        = 3  // admin từ chối, shipper mang về
```

Quan hệ với `Status`:
- `PendingApproval`/`Rejected` chỉ tồn tại khi `Status = Delivering`.
- Khi shipper hoàn tất sau Approved → `Status = Delivered`, sub-state giữ `Approved` để audit.

---

## 4. Domain layer — `DeliveryNote`

### Fields mới
```csharp
public DeliverySettlementApprovalStatus SettlementApproval { get; private set; } = NotRequired;
public decimal? ProposedAmountToCollect { get; private set; }
public decimal? ApprovedAmountToCollect { get; private set; }
public string? SettlementReason { get; private set; }        // shipper
public string? SettlementAdminNote { get; private set; }     // admin
public Guid? SettlementRequestedByUserId { get; private set; }
public DateTime? SettlementRequestedOnUtc { get; private set; }
public Guid? SettlementApprovedByUserId { get; private set; }
public DateTime? SettlementApprovedOnUtc { get; private set; }
```
Proof + receiver lúc gửi duyệt: tái dùng `DeliveryProofPictureIds` / `DeliveryReceiverName` (set sớm
ở bước request, completion không ghi đè nếu đã có).

### Methods mới (accessibility `internal`)
```csharp
internal void RequestSettlementApproval(
    decimal proposedAmountToCollect, string reason,
    IReadOnlyList<Guid> proofPictureIds, string? receiverName,
    Guid? requestedByUserId, DateTime requestedOnUtc)
{
    if (Status != DeliveryNoteStatus.Delivering && Status != DeliveryNoteStatus.Confirmed)
        throw new DeliveryNoteCannotChangeStatusException(Status, Status);
    if (SettlementApproval == PendingApproval)
        throw new NamEcommerceDomainException("Error.DeliverySettlement.AlreadyPending");
    if (proofPictureIds is null || proofPictureIds.Count == 0 || proofPictureIds[0] == Guid.Empty)
        throw new DeliveryProofRequiredException();
    if (string.IsNullOrWhiteSpace(reason))
        throw new NamEcommerceDomainException("Error.DeliverySettlement.ReasonRequired");

    SettlementApproval = PendingApproval;
    ProposedAmountToCollect = Math.Max(0m, proposedAmountToCollect);
    SettlementReason = reason.Trim();
    DeliveryProofPictureId = proofPictureIds[0];
    DeliveryProofPictureIds = proofPictureIds.ToList().AsReadOnly();
    DeliveryReceiverName = receiverName;
    SettlementRequestedByUserId = requestedByUserId;
    SettlementRequestedOnUtc = requestedOnUtc;
    UpdatedOnUtc = DateTime.UtcNow;
    RaiseDomainEvent(new DeliverySettlementApprovalRequested(Id, OrderId, Code));
}

internal void ApproveSettlement(decimal approvedCashToCollect, decimal agreedCustomerCharge,
    string? agreedChargeReason, string? adminNote, Guid? approvedByUserId, DateTime approvedOnUtc)
{
    // approvedCashToCollect = TIỀN THU NGAY (≤ Bill). Bill tính ở completion qua acceptance + charge.
    if (SettlementApproval != PendingApproval)
        throw new NamEcommerceDomainException("Error.DeliverySettlement.NotPending");
    if (approvedCashToCollect < 0 || agreedCustomerCharge < 0)
        throw new NamEcommerceDomainException("Error.CashCollectedAmountCannotBeNegative");

    SettlementApproval = Approved;
    ApprovedAmountToCollect = approvedCashToCollect;   // = DeliveryCashCollectedAmount lúc completion
    ApprovedAgreedCustomerCharge = agreedCustomerCharge;
    ApprovedAgreedChargeReason = agreedChargeReason?.Trim();
    SettlementAdminNote = adminNote?.Trim();
    SettlementApprovedByUserId = approvedByUserId;
    SettlementApprovedOnUtc = approvedOnUtc;
    UpdatedOnUtc = DateTime.UtcNow;
    RaiseDomainEvent(new DeliverySettlementApproved(Id, OrderId, Code, approvedCashToCollect));
}

internal void RejectSettlement(string reason, Guid? approvedByUserId, DateTime approvedOnUtc)
{
    if (SettlementApproval != PendingApproval)
        throw new NamEcommerceDomainException("Error.DeliverySettlement.NotPending");
    if (string.IsNullOrWhiteSpace(reason))
        throw new NamEcommerceDomainException("Error.DeliverySettlement.ReasonRequired");

    SettlementApproval = Rejected;
    SettlementAdminNote = reason.Trim();
    SettlementApprovedByUserId = approvedByUserId;
    SettlementApprovedOnUtc = approvedOnUtc;
    UpdatedOnUtc = DateTime.UtcNow;
    RaiseDomainEvent(new DeliverySettlementRejected(Id, OrderId, Code, reason.Trim()));
}
```

### Guard trong `MarkDelivered`
- Nếu `SettlementApproval == PendingApproval` → throw `Error.DeliverySettlement.NotPending` (không cho hoàn tất khi đang chờ).
- Nếu hoàn tất sau Approved → caller phải truyền `AmountToCollect = ApprovedAmountToCollect`; `cashCollectedAmount <= ApprovedAmountToCollect`.

### Events mới (`DeliveryNoteEvents.cs`)
```csharp
public sealed record DeliverySettlementApprovalRequested(Guid DeliveryNoteId, Guid OrderId, string Code) : DomainEvent;
public sealed record DeliverySettlementApproved(Guid DeliveryNoteId, Guid OrderId, string Code, decimal ApprovedAmount) : DomainEvent;
public sealed record DeliverySettlementRejected(Guid DeliveryNoteId, Guid OrderId, string Code, string Reason) : DomainEvent;
```
(Dùng cho system notification, không cần Outbox reliable trừ khi muốn audit — để `DomainEvent` thường.)

---

## 5. Domain Services — `DeliveryNoteManager`

### `RequestSettlementApprovalAsync(RequestDeliverySettlementDto dto)`
1. Load note (tracked, `GetByIdAsync`).
2. `ResolveDeliveryAcceptance` để validate accepted+rejected = qty và tính `rejectedGoodsAmount`.
3. `proposed = Math.Max(0, note.AmountToCollect − rejectedGoodsAmount + agreedCharge)` HOẶC số shipper đề xuất (case C, không trả hàng) — lấy `Math.Min` của hai nếu cả hai có.
4. `note.RequestSettlementApproval(proposed, reason, proofIds, receiver, userId, now)`.
5. `CreateCustomerReturnFromRejectedAcceptanceAsync(note, acceptance)` nếu có dòng trả (tái dùng nguyên hàm hiện có) — chuyển thời điểm tạo return từ lúc Delivered sang lúc request.
6. `UpdateAsync`.

### `ApproveSettlementAndDeliver` — **2 lựa chọn**, plan đề xuất tách 2 bước:
- **`ApproveSettlementAsync(Guid id, decimal approvedAmount, string? adminNote, Guid adminUserId)`** → chỉ set Approved + lưu số. KHÔNG Delivered. Shipper hoàn tất sau (đúng "shipper thu đúng số đó").
- **`RejectSettlementAsync(Guid id, string reason, Guid adminUserId)`** → set Rejected.

### Hoàn tất sau Approved
Tái dùng `MarkDeliveredAsync` hiện có, nhưng:
- Trước khi `MarkDelivered`, set `note.AmountToCollect = note.ApprovedAmountToCollect ?? note.AmountToCollect`.
- Bỏ qua `CreateCustomerReturnFromRejectedAcceptanceAsync` nếu return đã tạo ở bước request (guard: kiểm tra đã có CustomerReturn linked Draft/Inspecting cho note này).
- `debtAmount` giữ semantics double-entry hiện tại: công nợ ghi đủ giá trị, phần trả được credit khi `CustomerReturn` Confirmed. Số thu thực (`ApprovedAmountToCollect`) là phần thu tại chỗ; phần hụt nằm lại thành công nợ.

> **Lưu ý money:** `ResolveDeliveryAcceptance` hiện tính `amountToCollect` cho nhánh non-ShowPrice =
> `AmountToCollect + charge` (KHÔNG trừ trả hàng) — đây chính là chỗ mơ hồ user nêu. Trong flow mới,
> số phải thu cuối là `ApprovedAmountToCollect` do admin chốt, nên override giá trị này. Cần đảm bảo
> `proposed` luôn trừ `rejectedGoodsAmount` để admin thấy gợi ý đúng.

---

## 6. Application layer

- `IDeliveryNoteAppService`:
  - `RequestSettlementApprovalAsync(RequestDeliverySettlementAppDto)` → result `{Success, ErrorMessage}`.
  - `ApproveSettlementAsync(ApproveDeliverySettlementAppDto)`.
  - `RejectSettlementAsync(RejectDeliverySettlementAppDto)`.
- DTOs trong `Application.Contracts/Dtos/DeliveryNotes/`. Mỗi DTO có `Validate()` theo convention (return `(bool, string?)`).
- App service không throw — trả result `Success=false` + ErrorCode; catch `NamEcommerceDomainException`.
- Map sang domain DTO trong Domain.Shared (`RequestDeliverySettlementDto`, ...).
- Mở rộng `DeliveryNoteAppDto` + `MapToDto`: thêm `SettlementApproval`, `ProposedAmountToCollect`, `ApprovedAmountToCollect`, `SettlementReason`, `SettlementAdminNote`, audit fields.

### Notifications
Thêm vào `DeliverySystemNotificationComposer` + một handler cho 3 event mới:
- `SettlementApprovalRequested` → notify nhóm Admin/WarehouseManager ("Phiếu {Code} chờ duyệt thu hụt").
- `SettlementApproved` / `SettlementRejected` → notify shipper được assign.

---

## 7. Web layer

### Contracts — Commands
`Web.Contracts/Commands/Models/DeliveryNotes/`:
- `RequestDeliverySettlementCommand` (DeliveryNoteId, Reason, ProposedAmount?, ProofPictureIds/PictureId, ReceiverName, Items[accepted/rejected]).
- `ApproveDeliverySettlementCommand` (DeliveryNoteId, ApprovedAmountToCollect, AdminNote?).
- `RejectDeliverySettlementCommand` (DeliveryNoteId, Reason).
Mỗi cái có Result `: ICommandResult` (Success=false → skip commit). Handlers trong `Web.Framework`.

### Controllers
`DeliveryNoteController` (admin):
- `[HttpPost] ApproveSettlement(Guid id, decimal approvedAmountToCollect, string? adminNote)` → `[Authorize(DeliveryNotes.Manage)]`.
- `[HttpPost] RejectSettlement(Guid id, string reason)`.
- (`MarkDelivered` hiện có: thêm guard — nếu note đang PendingApproval thì trả lỗi.)

`DeliveryMobileController` (shipper):
- `[HttpPost] RequestSettlement(...)` — giống `CompleteDeliveryNote` (upload proof + acceptance) nhưng gọi `RequestDeliverySettlementCommand` thay vì complete.
- `CompleteDeliveryNote` hiện có: thêm nhánh — nếu `requiresApproval` (server tự tính) mà chưa `Approved` → trả lỗi "cần gửi duyệt"; nếu đã `Approved` → cho complete với `ApprovedAmountToCollect`.

---

## 8. UI

### Mobile `DeliveryMobile/Run.cshtml` (shipper) — 2 bước
- Khi nhập SL trả > 0 hoặc tiền thu < COD: nút submit đổi thành **"Gửi duyệt thu hụt"** (thay "Đã giao"). Hiện dòng "Số tiền đề xuất thu: X" (JS tự tính từ `returned × unitPrice`).
- Thêm nút riêng **"Khách từ chối / thu thiếu"** mở ô nhập số đề xuất + lý do.
- Khi phiếu `PendingApproval`: card hiện badge "Chờ admin duyệt", khóa form, nút "Làm mới".
- Khi `Approved`: hiện "Đã duyệt — thu đúng **X**", mở lại nút **"Xác nhận đã giao"** (thu X, ảnh đã có).
- Khi `Rejected`: hiện lý do admin + hướng dẫn "Mang hàng về".
- JS tính tiền: tái dùng `DecimalFields` (đã import sẵn). Cập nhật `acceptedQuantity`/proposed trong handler `returned-quantity-input`.

### Admin `_DeliveryNote.ConfirmDelivered.cshtml` / Details
- Khi note `PendingApproval`: thay modal xác nhận giao bằng **panel duyệt thu hụt**:
  - Bảng SL giao / nhận / trả (readonly từ request shipper).
  - "Phải thu gốc (COD)" / "Số tự tính sau trả hàng" / ô **"Số phải thu duyệt"** (editable, default = proposed) / "Chênh lệch (công nợ)".
  - Ảnh shipper, lý do shipper.
  - Nút **Duyệt** (POST ApproveSettlement) + **Từ chối** (POST RejectSettlement + lý do).

### Run `DeliveryRun/Details.cshtml` (cashier)
- Cột trạng thái: thêm chip "Chờ duyệt thu hụt" cho phiếu PendingApproval (để cashier biết chưa chốt).
- Tiền "Phải thu sau trả hàng" lấy theo `ApprovedAmountToCollect` khi đã Approved+Delivered.

---

## 9. Data / Migration (user tự tạo)

EF config `DeliveryNoteConfiguration` — thêm cột **additive, nullable**:
- `SettlementApproval` (int, default 0), `ProposedAmountToCollect`, `ApprovedAmountToCollect` (decimal 18,2 null),
  `SettlementReason`, `SettlementAdminNote` (nvarchar), 4 audit cột.
- Không cột nào NOT NULL không default → phiếu cũ an toàn (`NotRequired`).

> Theo memory: **không chạy migration**. Sau khi sửa entity + mapping, báo user tạo migration.

---

## 10. Edge cases

- **Idempotency:** request duyệt 2 lần → method throw `AlreadyPending`. Approve/Reject khi không PendingApproval → throw `NotPending`.
- **Return tạo sớm:** chuyển `CreateCustomerReturnFromRejectedAcceptanceAsync` về bước request; completion check tránh tạo trùng (đã có return Draft/Inspecting linked).
- **Reject rồi shipper sửa lại:** cho phép request lại từ `Rejected` (reset về PendingApproval). Method `RequestSettlementApproval` cho phép từ `Rejected`.
- **Admin sửa số > COD gốc:** chặn ở domain? Không — admin có thể cộng `AgreedCustomerCharge`. Cho phép, nhưng cảnh báo UI.
- **Hủy phiếu khi PendingApproval:** `CancelAsync` hiện chặn khi có CustomerReturn Confirmed; return ở đây mới Inspecting nên hủy được, cascade cancel return (đã có logic).
- **Concurrency:** load-for-write qua `IRepository.GetByIdAsync` (tracked) cho mọi transition.

---

## 11. Money/debt — giữ nguyên semantics

- Công nợ vẫn sinh ở `DeliveryNoteDelivered` = `debtAmount` (full value, gồm phần trả → credit sau khi return Confirmed).
- `ApprovedAmountToCollect` = tiền thu tại chỗ; phần `AmountToCollect − collected` = công nợ còn lại của khách (đúng bản chất "giao nợ").
- KHÔNG sinh `CustomerPayment` ở đây — vẫn qua cashier `ConfirmCashHandoverAsync` (giữ boundary cũ).

---

## 12. Task list (phased)

### Phase 1 — Domain
- [ ] Thêm enum `DeliverySettlementApprovalStatus`.
- [ ] Thêm fields + 3 methods (`RequestSettlementApproval`/`ApproveSettlement`/`RejectSettlement`) vào `DeliveryNote`.
- [ ] Guard PendingApproval trong `MarkDelivered`.
- [ ] Thêm 3 domain events.
- [ ] Verify: build `NamEcommerce.Web.csproj`.

### Phase 2 — Domain Services + Data mapping
- [ ] `DeliveryNoteManager`: `RequestSettlementApprovalAsync`, `ApproveSettlementAsync`, `RejectSettlementAsync`; chuyển thời điểm tạo CustomerReturn sang request; guard tránh tạo trùng ở completion; override `AmountToCollect` = approved khi hoàn tất.
- [ ] EF config thêm cột nullable. Báo user tạo migration.
- [ ] Verify build.

### Phase 3 — Application
- [ ] App DTOs + `Validate()`; method trên `IDeliveryNoteAppService` + impl.
- [ ] Mở rộng `DeliveryNoteAppDto` + `MapToDto`.
- [ ] Notification composer + handler cho 3 event.
- [ ] Verify build.

### Phase 4 — Web (commands/handlers/controllers)
- [ ] 3 commands + results (`ICommandResult`) + handlers.
- [ ] `DeliveryNoteController`: ApproveSettlement / RejectSettlement + guard MarkDelivered.
- [ ] `DeliveryMobileController`: RequestSettlement + nhánh trong CompleteDeliveryNote.
- [ ] Resource strings (SharedResource.resx + vi-VN) cho mọi ErrorCode + label mới.
- [ ] Verify build.

### Phase 5 — UI
- [ ] Mobile Run.cshtml: 2 bước (gửi duyệt / chờ / đã duyệt-thu / bị từ chối) + JS tính tiền đề xuất.
- [ ] Admin panel duyệt thu hụt (thay modal khi PendingApproval).
- [ ] Run Details: chip trạng thái + số tiền theo approved.
- [ ] Verify build + smoke test thủ công.

### Phase 6 — Docs
- [ ] `docs/DeliveryNotes/` cập nhật flow xác nhận giao + settlement approval.
- [ ] File implement tracking: `plans/DeliveryNotes/settlement_approval_gate_implement.md`.

---

## 13. Quyết định đã chốt
- App tự tính số gợi ý; **admin sửa được & xác nhận**; shipper thu đúng số duyệt. (user: Q1=B + auto-suggest)
- **Mọi thu hụt** đều phải admin duyệt trước. (user)
- Shipper **chờ** admin; admin **Reject** → shipper mang hàng về. (user: Q2=A)
- Không thêm giá trị `DeliveryNoteStatus`; dùng sub-state song song. (giảm rủi ro hồi quy)
- Hai bước shipper (gửi duyệt → sau duyệt mới thu & xác nhận). (khớp "admin xác nhận đồng ý thì shipper mới xác nhận đã giao")

## 14. Open questions — RESOLVED (2026-06-17), xem mục 1b
1. ✅ Phí restocking: dùng `AgreedCustomerCharge`, expose ở panel duyệt, default 0.
2. ✅ Ai duyệt: chỉ Admin.
3. ✅ Reject: tự động cancel (nhập lại kho + restore reservation + hủy phiếu + cascade cancel return).

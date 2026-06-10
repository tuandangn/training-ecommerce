# Account-Based Debt Ledger Plan — bỏ FIFO allocation, chuyển sang số dư công nợ

Chuyển mô hình công nợ từ **open-item** (gán từng khoản thanh toán vào từng phiếu nợ theo FIFO, track PaidAmount/RemainingAmount/Status per phiếu) sang **balance-forward** (sổ cái theo đối tác: chỉ quan tâm tổng nợ, đã trả đủ chưa, thiếu/thừa bao nhiêu). Áp dụng cho cả CustomerDebt và VendorDebt.

## Vì sao mô hình hiện tại gây khó chịu (xác nhận từ code)

- Mỗi lần thu tiền phải chạy FIFO phân bổ (`RecordFlexiblePaymentForCustomerAsync`) → sinh N bản ghi payment con, N lần update debt → phức tạp, nhiều bug (đã dính: mất write do tracking, cọc áp lặp, overshoot PaidAmount).
- Tiền cọc phải special-case (`PaymentType.Deposit` + `IsApplied` + auto-apply lúc tạo debt) — trong mô hình số dư, cọc đơn giản là khách dư tiền.
- Trả hàng phải đẻ thêm `CreditNoteAllocation` per debt + flow over-refund riêng (`MarkOverRefunded` → event → refund) — trong mô hình số dư, balance âm là xong.
- Casso/bank-transfer intent phải đoán trước thanh toán này thuộc debt nào (`intent.CustomerDebtId`).
- 84 files đang gánh độ phức tạp này trong khi nhu cầu thực: **"khách còn nợ bao nhiêu / dư bao nhiêu"**.

## Mô hình mới

```
┌─ Chứng từ (giữ nguyên vai trò pháp lý/in ấn, BẤT BIẾN sau confirm) ─┐
│ CustomerDebt   = phiếu ghi nợ (từ DN/đầu kỳ) — bỏ Paid/Remaining/Status │
│ CustomerPayment = phiếu thu — bỏ CustomerDebtId/IsApplied              │
│ CustomerCreditNote = phiếu ghi có (từ trả hàng) — bỏ Allocations       │
│ CustomerRefund = phiếu chi hoàn tiền                                   │
└────────────────────────────┬───────────────────────────────────────┘
                             │ mỗi chứng từ confirm = đúng 1 entry
                             ▼
CustomerLedgerEntry (append-only, immutable)
  Id, CustomerId, EntryType, Amount (signed: nợ +, có −),
  ReferenceType, ReferenceId, ReferenceCode, Note,
  OccurredAtUtc, CreatedByUserId, CreatedOnUtc

EntryType: DeliveryNoteCharge(+), OpeningBalance(+), Payment(−),
           ReturnCredit(−), RefundPayout(+), Correction(±)

CustomerAccountBalance (cache, 1 row/khách, update cùng transaction)
  CustomerId, Balance, LastEntryOnUtc
  Balance > 0: khách còn nợ │ = 0: đủ │ < 0: khách dư (cửa hàng nợ khách)
```

- **Append-only + bút toán đảo**: sai thì ghi `Correction` đảo dấu kèm ghi chú, không sửa/xoá entry — chuẩn kế toán, audit miễn phí.
- VendorLedgerEntry/VendorAccountBalance đối xứng (EntryType: GoodsReceiptCharge, OpeningBalance, Payment, ReturnCredit, RefundReceipt, Correction).

## Assumptions / Quyết định mặc định (cần anh duyệt)

1. **Mất trạng thái "phiếu nợ này trả chưa"** — đúng yêu cầu của anh. Phiếu ghi nợ vẫn xem/in được nhưng không còn cột đã trả/còn lại per phiếu. Nếu sau này cần aging (nợ quá 30/60/90 ngày), tính FIFO **lúc chạy báo cáo** (áp credit vào charge cũ nhất), không lưu allocation — để Phase 6 optional.
2. **`PaymentType` thu gọn**: bỏ phân biệt Deposit/DebtPayment/General về mặt xử lý (giữ field để phân loại hiển thị nếu muốn lọc "tiền cọc"). Khách đưa tiền lúc nào cũng chỉ là `Payment(−)`.
3. **Hoàn tiền do balance âm là hành động chủ động**: màn hình công nợ hiện "khách dư X" + nút "Tạo phiếu chi hoàn tiền". Bỏ flow tự động `CustomerReturnOverRefunded` → refund. (Khách dư thường để trừ lần mua sau — đúng thực tế VLXD.)
4. **Chứng từ cũ giữ vĩnh viễn** làm lịch sử; các cột allocation (PaidAmount, RemainingAmount, Status, IsApplied, CustomerDebtId trên payment, bảng `CustomerCreditNoteAllocation`) ngừng ghi, đánh dấu obsolete, xoá ở migration cuối sau 1 thời gian chạy ổn.
5. **Làm Customer trước, Vendor sau** (2 phase riêng, Vendor copy pattern).
6. **Thứ tự với các plan khác**: Bước 0 hotfix `UpdateAsync` (plan Architecture) làm TRƯỚC plan này (đang mất write ở chính module Debts). Plan này **thay thế hoàn toàn Orders Phase 1** (fix deposit — không còn cần). Nếu UoW (Architecture P.A 2 Bước 1-2) xong trước thì entry+balance tự nguyên tử; chưa xong thì wrap `BeginTransactionAsync` tay tại `CustomerLedgerManager` (1 chỗ duy nhất).
7. Agent không chạy migration DB thật; cutover cần script đối soát chạy trên bản sao production trước.

## Success Criteria

- Thu tiền khách = 1 phiếu thu + 1 entry, không FIFO, không vòng update N debts.
- Màn hình công nợ trả lời ngay: còn nợ bao nhiêu / dư bao nhiêu / lịch sử sao kê (running balance).
- Cọc trước → balance âm → phiếu nợ mới tự nhiên bù trừ, không code đặc biệt.
- Trả hàng → balance giảm; balance âm hiển thị "khách dư" + tạo refund thủ công được.
- Casso reconciliation chỉ cần CustomerId + Amount.
- Tổng balance toàn hệ thống khớp báo cáo tài chính (AccountingReport/CashBook đối chiếu được).
- Sau cutover: `CustomerDebtManager` giảm ≥ 50% LOC; xoá `ApplyPayment/ApplyCreditNote/ApplyReturn/MarkOverRefunded/CustomerDebtFullyPaid` và toàn bộ allocation.

---

## Phase 1 — Domain Customer ledger (mới hoàn toàn, chưa đụng code cũ)

**Files mới:** `Domain/.../Entities/Debts/CustomerLedgerEntry.cs`, `CustomerAccountBalance.cs`; `Domain.Shared/Enums/Debts/LedgerEntryType.cs`; `Domain.Shared/Dtos/Debts/CustomerLedgerDtos.cs`; `Domain.Services/Debts/CustomerLedgerManager.cs`; mapping + migration; events `CustomerLedgerEntryRecorded`.

`ICustomerLedgerManager`:
- `RecordChargeAsync(customerId, amount, refType, refId, refCode, occurredAt)` — idempotent theo (EntryType, ReferenceId)
- `RecordPaymentAsync(...)`, `RecordReturnCreditAsync(...)`, `RecordRefundPayoutAsync(...)`, `RecordCorrectionAsync(amount±, note, byUserId)`
- `GetBalanceAsync(customerId)`, `GetStatementAsync(customerId, from, to, paging)` — entry + running balance
- `GetBalancesAsync(keywords, paging)` — danh sách khách kèm số dư (từ cache table)
- Entry + balance update trong 1 transaction; balance là cache — job đối soát so `Balance` vs `Σ entries` (tận dụng job Phase E plan Inventory).

### TodoList 1
- [ ] Tests (TDD): record các loại entry, idempotency theo reference, balance đúng dấu, correction đảo, statement running balance, đối soát cache
- [ ] Entities + enums + DTOs + mapping + migration
- [ ] `CustomerLedgerManager` + transaction
- [ ] `dotnet test --filter "FullyQualifiedName~CustomerLedger"`

## Phase 2 — Rewire flows bán hàng sang ledger (dual-write tạm thời)

Giai đoạn chuyển tiếp: flow ghi **cả hai** (ledger mới + debt cũ) để so sánh, UI vẫn đọc cũ. 1-2 tuần chạy song song, đối soát khớp thì cutover Phase 3.

- `DeliveryNoteDeliveredHandler`: thêm `RecordChargeAsync(AmountToCollect, DeliveryNote, dnId)`.
- `CustomerDebtManager.RecordPaymentAsync`/`RecordFlexiblePaymentForCustomerAsync`: thêm `RecordPaymentAsync` ledger (1 entry tổng, không FIFO).
- `CustomerReturnManager.FinalizeConfirmAsync`: thêm `RecordReturnCreditAsync(netRefundAmount)` (credit note doc vẫn tạo, allocation cũ vẫn chạy tạm).
- `CustomerRefundManager.CompleteAsync`: thêm `RecordRefundPayoutAsync`.
- Đầu kỳ (`CreateInitialDebtAsync`): thêm `OpeningBalance` entry.
- FastSale (`FastSaleAppService`), Casso (`BankTransferPaymentIntentManager`): thêm ghi ledger tương ứng.
- Job đối soát dual-write: `Σ ledger balance` vs `Σ (RemainingAmount − credit dư)` per khách, log lệch hằng đêm.

### TodoList 2
- [ ] Tests integration cho từng flow: 1 action → đúng 1 entry, retry không double
- [ ] 7 điểm rewire trên
- [ ] Job đối soát dual-write + báo cáo lệch
- [ ] `dotnet test` toàn bộ

## Phase 3 — Cutover UI + API đọc từ ledger

- **Web** `CustomerDebtController` → màn hình mới:
  - Danh sách: khách + số dư (nợ/dư) + ngày phát sinh cuối — từ `GetBalancesAsync`.
  - Chi tiết khách: **sao kê** (statement) thay cho danh sách phiếu nợ: ngày, chứng từ (link DN/phiếu thu/phiếu trả/hoàn tiền), phát sinh nợ, phát sinh có, số dư chạy. Đây là dạng "sổ ghi nợ" quen thuộc với cửa hàng.
  - Form thu tiền: chỉ nhập số tiền + phương thức (+ ghi chú) — bỏ chọn phiếu nợ, bỏ preview FIFO.
  - Balance âm: badge "Khách dư X" + nút tạo phiếu hoàn tiền.
- **Customer Portal** (`DebtsController` API + Client): trả balance + statement thay vì list debts.
- **Casso intent**: bỏ `CustomerDebtId` khỏi intent (nullable → ngừng dùng), match theo khách.
- **MenuNavigation/OrderModelFactory/FastSaleOrderController**: các chỗ hiển thị "nợ còn lại của đơn" đổi sang balance khách hoặc tổng charge − payment theo OrderId từ ledger (quyết định per màn hình lúc implement, ghi vào implement doc).
- Reports (`AccountingReportService`, `CashBookService`): payments/refunds đọc từ chứng từ — không đổi; phần "tổng công nợ" đổi nguồn sang balance.

### TodoList 3
- [ ] Models/Queries/Commands mới ở Web.Contracts + handlers (tuân thủ rule Web.Contracts isolation)
- [ ] 2 màn hình Web + form thu tiền đơn giản hoá
- [ ] Customer Portal API + client
- [ ] Casso + các model factory liên quan
- [ ] Build 3 project Presentation + smoke test tay

## Phase 4 — Migration dữ liệu + tắt đường cũ

1. Script build ledger từ chứng từ lịch sử (chạy sau khi hotfix Bước 0 đã sửa dữ liệu lệch):
   - `CustomerDebt` → `DeliveryNoteCharge`/`OpeningBalance` (TotalAmount, occurredAt = CreatedOnUtc)
   - `CustomerPayment` → `Payment` (Amount — bỏ qua chuyện nó từng allocate vào đâu)
   - `CustomerCreditNote` active → `ReturnCredit` (Amount)
   - `CustomerRefund` Completed → `RefundPayout` (Amount)
2. Đối soát: balance mới vs `Σ RemainingAmount` cũ per khách → bảng lệch để anh duyệt từng trường hợp (dữ liệu cũ vốn lệch do bug — đây là dịp làm sạch, chốt số với khách).
3. Tắt dual-write: gỡ code allocation khỏi flow (xoá `ApplyPayment`/`ApplyCreditNote`/`ApplyReturn`/`MarkOverRefunded`/`CustomerDebtFullyPaid`/`CustomerReturnOverRefundedEventHandler`/auto-apply deposit/FIFO loop), obsolete các cột.
4. Cập nhật `docs/Debts/` + `CLAUDE.md` (module Debts mô tả mới).

### TodoList 4
- [ ] Script migration + script đối soát (xuất bảng lệch)
- [ ] Chạy trên bản sao production, anh duyệt số
- [ ] Gỡ dual-write + xoá code cũ + obsolete cột
- [ ] Full `dotnet test` + smoke toàn flow bán hàng

## Phase 5 — Vendor side (mirror)

Lặp Phase 1→4 cho NCC: `VendorLedgerEntry`, `VendorAccountBalance`, rewire `GoodsReceiptCreatedHandler`/`VendorDebtManager`/`VendorReturnManager`/`VendorRefundManager`, màn hình công nợ NCC, migration. Khác biệt cần chú ý: `RecordAdvancePaymentAsync` (ứng trước NCC) = Payment(−) thường; `ReverseCreditNoteFromVendorReturnAsync` (đảo trả hàng NCC) = `Correction(+)`; guard xoá GoodsReceipt đang check "debt chưa đụng" đổi thành check "chưa có payment entry sau charge này" → đơn giản hơn: chỉ cho xoá khi tạo bút toán đảo.

## Phase 6 (optional, để sau) — Aging & hạn mức

- Aging report: FIFO computed lúc chạy (áp credits vào charges cũ nhất) → bảng nợ 0-30/31-60/61-90 ngày. Không lưu allocation.
- Credit limit (từ Orders Phase 5): check `Balance + AmountToCollect > CreditLimit` lúc confirm DN — giờ chỉ 1 phép so sánh.

---

## Thứ tự tổng thể với các plan khác

```
Architecture Bước 0 (hotfix UpdateAsync + đối soát)   ← làm ngay
→ Debts Phase 1-2 (ledger + dual-write)               ← plan này
→ (song song) Architecture Bước 1 nếu duyệt UoW
→ Debts Phase 3-4 (cutover Customer)
→ Debts Phase 5 (Vendor)
→ Orders Phase 3-5 (bỏ Phase 1, Phase 2 đã hợp nhất vào Architecture)
→ Inventory A-E
```

## Verification Plan

- `dotnet build NamEcommerce.sln` + `dotnet test` mỗi phase
- Dual-write đối soát 0 lệch ≥ 1 tuần trước khi cutover Phase 4
- Smoke checklist Phase 4: cọc trước → mua → giao → thu thiếu → trả hàng → balance âm → hoàn tiền; in sao kê đối chiếu tay với máy tính bỏ túi
- Script đối soát migration chạy trên bản sao production, anh duyệt bảng lệch trước khi chạy thật

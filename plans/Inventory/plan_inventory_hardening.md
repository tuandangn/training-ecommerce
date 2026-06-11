# Inventory Hardening Plan

Fix các lỗi tiềm ẩn của quản lý tồn kho (nhập, xuất, chuyển kho, điều chỉnh, giá vốn) từ đợt review 2026-06-10. Plan này độc lập với `plans/Orders/plan_sales_workflow_hardening.md` nhưng có 2 điểm giao nhau (ghi rõ ở mục Phụ thuộc).

## Bối cảnh kỹ thuật (xác minh từ code)

- `NamEcommerceEfDbContext`: mỗi `InsertAsync`/`UpdateAsync` của repository gọi `SaveChangesAsync` ngay → các thao tác nhiều bước KHÔNG nguyên tử trừ khi tự wrap `BeginTransactionAsync` (mới chỉ `DeliveryRunManager`, `FastSaleAppService` làm).
- Chỉ `PurchaseOrder` có `RowVersion`; `InventoryStock` và cost ledger không có concurrency token.
- `InventoryStock` có 1 cặp `QuantityReserved` + `ReservedUntilUtc` dùng CHUNG cho mọi phiếu xuất; `ReserveStockAsync` ghi đè expiry và tự xoá toàn bộ reserved khi quá hạn; `ReleaseExpiredReservationsAsync` tồn tại nhưng hiện chưa có scheduler gọi.
- Cost ledger (`InventoryCostingManager`) tính balance **global theo product** (`GetLastProductBalance(productId)`), trong khi `InventoryStock.AverageCost` là per-warehouse và được `TransferStockAsync` tự tính lại — 2 hệ giá vốn song song.

## Assumptions / Quyết định mặc định (cần anh duyệt)

1. **Chiến lược concurrency**: dùng `RowVersion` (optimistic) + retry tối đa 3 lần ở manager, KHÔNG dùng `ExecuteUpdate` raw để giữ nguyên Repository pattern + domain entity. Trade-off: chậm hơn chút khi va chạm, nhưng không phá kiến trúc.
2. **Bỏ hẳn cơ chế expiry giữ chỗ kho** (`ReservedUntilUtc`, auto-release trong `ReserveStockAsync`, `ReleaseExpiredReservationsAsync`): vòng đời giữ chỗ đã được quản lý đúng bởi DN (cancel → release, delivering → dispatch release). DN treo lâu là việc nghiệp vụ (nhắc nhở), không phải việc tự xoá giữ chỗ. Thay bằng notification "DN Confirmed quá N ngày".
3. **Giữ chỗ kho chuyển sang ledger per-reference** (`StockReservationEntry`): cộng/trừ theo từng DN, `InventoryStock.QuantityReserved` trở thành cache đồng bộ cùng transaction (giữ nguyên cột để không gãy query hiện có).
4. **Side effect của Adjustment chuyển từ event handler về trong `ApproveAsync`** (đồng bộ, cùng transaction) — event chỉ còn dùng cho notification. Lý do: điều chỉnh kho là thao tác admin tần suất thấp, cần chắc chắn nguyên tử hơn là cần decouple.
5. **Giá vốn**: cost ledger là source of truth; `InventoryStock.AverageCost` chỉ là cache hiển thị, được set từ kết quả ledger sau mỗi movement. Giữ valuation scope GLOBAL theo policy hiện tại (không đổi sang per-warehouse trong plan này — đổi scope là việc lớn, làm riêng nếu cần).
6. **Serialize cost ledger per product** bằng `sp_getapplock` (SQL Server app lock, key = `inv-cost-{productId}`) trong transaction — chịu được multi-process (Web + Customer.Api cùng ghi).
7. Clamp-âm-về-0 đổi thành **throw** (`InsufficientStockException`) — chấp nhận operation fail rõ ràng thay vì sổ sách lệch ngầm.
8. Agent không chạy migration trên DB thật; user tự `dotnet ef database update`.
9. TDD: viết/sửa test trước mỗi thay đổi logic.

## Success Criteria

- 2 thao tác xuất kho đồng thời cùng (product, warehouse) không bao giờ mất update: một cái thành công, cái kia retry với số liệu mới hoặc fail `InsufficientStock` rõ ràng.
- Không còn đường code nào tự xoá `QuantityReserved`; hủy/giao DN release đúng phần của DN đó kể cả khi dữ liệu lệch một phần (release `Min`).
- Approve phiếu chuyển kho: hoặc toàn bộ item chuyển + phiếu Approved (1 transaction), hoặc không gì cả; double-click/retry không nhân đôi.
- Approve phiếu điều chỉnh: nguyên tử như trên; điều chỉnh giảm quá available bị chặn ngay khi approve với message rõ.
- `StockMovementLog.QuantityBefore/After` luôn khớp `Quantity` (không còn clamp ngầm); tổng movement log khớp `QuantityOnHand` (job đối soát xác nhận định kỳ).
- Chuyển kho dùng đúng 1 nguồn giá vốn (ledger); 2 outbound đồng thời cùng product không tạo sequence trùng/balance sai.
- Phiếu nhập vượt MaxStockLevel bị chặn từ lúc TẠO phiếu; handler cộng tồn không bao giờ throw vì capacity.
- `dotnet build` + `dotnet test` xanh sau từng phase.

---

## Phase A — Optimistic concurrency cho InventoryStock (nền tảng, làm trước)

**Files:** `Domain/NamEcommerce.Domain/Entities/Inventory/InventoryStock.cs`, `Infrastructure/.../Mappings/InventoryStockMapping.cs`, `Domain/NamEcommerce.Domain.Services/Inventory/InventoryStockManager.cs`, migration mới.

### Thay đổi
- Thêm `byte[] RowVersion` vào `InventoryStock` + `builder.Property(p => p.RowVersion).IsRowVersion()` (theo mẫu `PurchaseOrderMapping`).
- Helper private trong `InventoryStockManager`: `ExecuteWithStockRetryAsync(Func<Task> action, int maxRetries = 3)` — catch `DbUpdateConcurrencyException` → xoá entry khỏi `_cachedInventoryStocks` → reload → chạy lại action (toàn bộ read-check-write nằm trong action). Hết retry → rethrow.
- Áp vào TẤT CẢ method mutate stock: `ReceiveStockAsync`, `RevertReceiveAsync`, `DispatchStockAsync`, `TransferStockAsync`, `ReserveStockAsync`, `ReleaseReservedStockAsync`, `ApplyAdjustmentAsync`, `SetStockLevelsAsync`.
- Lưu ý cache: `_cachedInventoryStocks` phải invalidate khi retry; thêm comment cảnh báo manager phải là Scoped (hiện đúng — `Program.cs:208`).

### TodoList A
- [ ] Test: 2 dispatch song song (mô phỏng bằng 2 DbContext) → tổng trừ đúng, không lost update; dispatch vượt available → `InsufficientStockException`
- [ ] Entity + mapping + migration `AddInventoryStockRowVersion`
- [ ] Retry helper + áp vào các method mutate
- [ ] `dotnet test --filter "FullyQualifiedName~InventoryStock"`

---

## Phase B — Giữ chỗ kho theo ledger, bỏ expiry

**Files:** entity mới `Domain/.../Entities/Inventory/StockReservationEntry.cs`, mapping + migration, `InventoryStockManager.cs`, `DeliveryNoteManager.cs`, `DeliveryNoteCreatedHandler.cs`, xoá logic expiry.

### Thiết kế
- `StockReservationEntry` (aggregate, ghi-append như `ProductReservationLedger`): `ProductId`, `WarehouseId`, `QuantityDelta` (+/−), `ReferenceType` (enum: DeliveryNote, Manual...), `ReferenceId`, `Note`, `CreatedOnUtc`. Index `(ProductId, WarehouseId)`, `(ReferenceType, ReferenceId)`.
- `ReserveStockAsync(..., referenceType, referenceId)`: trong cùng transaction — insert entry (+qty) và cập nhật cache `stock.QuantityReserved += qty` (check available như cũ). Bỏ tham số `reservationDaysValid`, bỏ toàn bộ logic `ReservedUntilUtc`.
- `ReleaseReservedStockAsync(...)`: release theo reference — `released = Min(requested, Σ entry của reference đó)`; insert entry (−released) + giảm cache. Idempotent tự nhiên (gọi lại → Σ còn 0 → no-op).
- `DispatchStockAsync(releaseReservedStock: true)`: trừ reserved theo reference của DN (qua entry −), không còn so sánh mù với counter tổng → hết lỗi `CannotReleaseMoreThanReserved` oan.
- `DeliveryNoteManager.ReleaseReservedStockIfPresentAsync`: bỏ silent-skip, luôn release `Min` (giờ nằm sẵn trong manager mới).
- Xoá: `ReleaseExpiredReservationsAsync`, `ReservedUntilUtc` (giữ cột nullable trong DB 1 release để rollback an toàn, đánh dấu obsolete; xoá hẳn ở migration sau).
- Notification thay thế: job nhỏ (HostedService có sẵn pattern outbox processor) — DN ở `Confirmed`/`Delivering` quá N ngày (AppConfig, default 7) → system notification nhắc xử lý, KHÔNG đụng vào giữ chỗ.
- Migration backfill: với mỗi DN đang `Confirmed`/`Delivering` (ToCustomer, không direct-ship), tạo entry (+) tương ứng; sau đó set lại `QuantityReserved = Σ entries` cho từng stock row (sửa luôn dữ liệu đã bị expiry xoá sai trước đây).

### TodoList B
- [ ] Tests: reserve/release theo reference, release quá phần của mình bị chặn, release idempotent, dispatch release đúng DN, cancel DN khi counter lệch vẫn release phần còn lại
- [ ] Entity + mapping + migration + backfill script
- [ ] Refactor `ReserveStockAsync`/`ReleaseReservedStockAsync`/`DispatchStockAsync` + update 2 call sites (`DeliveryNoteCreatedHandler`, `DeliveryNoteManager`)
- [ ] Xoá expiry logic + obsolete `ReservedUntilUtc`
- [ ] Job notification DN treo lâu
- [ ] `dotnet test --filter "FullyQualifiedName~Reservation"`

---

## Phase C — Chuyển kho & điều chỉnh: nguyên tử + idempotent + hết clamp ngầm

**Files:** `StockTransferNoteManager.cs`, `StockAdjustmentNoteManager.cs`, `StockAdjustmentNoteApprovedEventHandler.cs` (gỡ side effect), `InventoryStockManager.cs`.

### C1. `StockTransferNoteManager.ApproveAsync` viết lại
```
1. Load note → guard Status == Draft NGAY ĐẦU (note.EnsureCanApprove())
2. await using transaction = BeginTransactionAsync()
3. Với từng item:
   - RegisterOutboundAsync TRƯỚC (lấy unitCost chuẩn từ ledger — bỏ GetCurrentCostSummaryAsync pre-read)
   - TransferStockAsync(..., unitCost: transferOutCost.UnitCost)
   - RegisterTransferInAsync (như hiện tại)
   - item.UnitCost = transferOutCost.UnitCost
4. note.Approve() + Update
5. transaction.Commit()
```
- Thêm `TransferStockUpToAsync` (idempotency theo `GetMovedQuantity(Transfer, StockTransfer, noteId)` per product+warehouse hướng đi) — phòng hờ retry sau commit một phần do lỗi hạ tầng cũ còn sót.
- `CancelAsync`: guard hiện tại (không cancel Approved) giữ nguyên; nếu sau này cần "reverse approved transfer" thì làm phiếu chuyển ngược, không sửa in-place.

### C2. `StockAdjustmentNoteManager.ApproveAsync` ôm side effect
- Di chuyển toàn bộ logic từ `StockAdjustmentNoteApprovedEventHandler` vào `ApproveAsync`, wrap transaction, guard status đầu method. Handler chỉ giữ notification (hoặc xoá nếu không có).
- `ApplyAdjustmentAsync`:
  - Bỏ clamp 0 → nếu `delta < 0` và `|delta| > stock.QuantityAvailable` (tính cả reserved, không chỉ OnHand) → throw `InsufficientStockException` với message nêu rõ phần đang giữ chỗ.
  - Log `Quantity` = |delta thực|, đảm bảo Before/After khớp.

### C3. `RevertReceiveAsync` hết clamp
- `quantity > stock.QuantityAvailable` → throw (delete-goods-receipt flow đã pre-check available ở app layer, nhưng manager phải tự bảo vệ vì có race window).

### TodoList C
- [ ] Tests: approve transfer fail item giữa chừng → rollback toàn bộ, phiếu vẫn Draft; double approve → throw status; adjustment giảm quá available → throw; revert quá available → throw
- [ ] C1 viết lại ApproveAsync + TransferStockUpToAsync
- [ ] C2 di chuyển side effect + sửa ApplyAdjustmentAsync
- [ ] C3 RevertReceiveAsync
- [ ] `dotnet test --filter "FullyQualifiedName~StockTransfer|FullyQualifiedName~StockAdjustment"`

---

## Phase D — Giá vốn: 1 nguồn sự thật + serialize per product

**Files:** `InventoryCostingManager.cs`, `InventoryStockManager.cs`, helper lock mới ở Infrastructure.

- `IProductCostingLock` (Domain.Shared) + implementation SQL `sp_getapplock` (Infrastructure): `await using var _ = await lock.AcquireAsync(productId)` bao quanh body của `RegisterInboundAsync`/`RegisterOutboundAsync`/`RegisterTransferInAsync`/`RegisterReceiptReversalAsync` (sau idempotency check). Lock + insert ledger nằm cùng DB transaction để lock giữ tới commit.
- `GetNextSequenceNumber()` → SQL Sequence object (migration `CREATE SEQUENCE InventoryCostSequence`) — hết trùng, chấp nhận gap.
- `InventoryStock.AverageCost` = cache: sau mỗi `RegisterInbound/Outbound/TransferIn`, manager set `stock.AverageCost` từ `averageCost` balance của ledger (global) — hoặc tối thiểu: `TransferStockAsync` bỏ công thức weighted-average tự chế, nhận giá từ caller duy nhất là ledger result (đã làm ở C1). Ghi rõ trong XML doc: "AverageCost chỉ để hiển thị, định giá dùng cost ledger".
- Job đối soát (gộp với Phase E): cảnh báo khi `stock.AverageCost` lệch ledger balance quá ngưỡng.

### TodoList D
- [ ] Tests: 2 RegisterOutbound song song cùng product → sequence không trùng, balance dây chuyền đúng (test integration với SQL LocalDB/testcontainer nếu có sẵn pattern; nếu không, test applock helper riêng + test tuần tự cho balance)
- [ ] Applock helper + áp vào 4 method Register*
- [ ] SQL Sequence migration + thay GetNextSequenceNumber
- [ ] AverageCost cache hoá + doc
- [ ] `dotnet test --filter "FullyQualifiedName~InventoryCosting"`

---

## Phase E — GoodsReceipt capacity + job đối soát + dọn dẹp

- **Capacity check chuyển về lúc tạo phiếu**: `GoodsReceiptManager.CreateGoodsReceiptAsync` (+ các biến thể CreateFrom*) validate `MaxStockLevel` per (product, warehouse) trước khi insert; `ReceiveStockAsync` đổi throw `WarehouseCapacityExceededException` → chỉ throw khi gọi trực tiếp ngoài flow phiếu (thêm flag `enforceCapacity`, handler truyền `false` + bắn notification cảnh báo vượt mức thay vì fail nửa chừng).
- **Job đối soát tồn kho** (HostedService chạy đêm, hoặc lệnh admin chạy tay):
  1. `QuantityOnHand` vs Σ `StockMovementLog` per (product, warehouse) — lệch → notification + record `StockAuditLog`.
  2. `QuantityReserved` vs Σ `StockReservationEntry` (sau Phase B).
  3. Ledger quantity balance (global) vs Σ OnHand các kho Physical.
- **Dọn dẹp nhỏ**: bỏ `Task.Run` trong code đọc DataSource; comment ràng buộc Scoped cho `_cachedInventoryStocks`; mã `PCT-` dùng chung giải pháp unique index + retry của `plans/Orders/plan_sales_workflow_hardening.md` Phase 4 (không làm trùng ở đây).

### TodoList E
- [ ] Tests: tạo phiếu nhập vượt MaxStockLevel bị chặn; handler không throw capacity
- [ ] Capacity validation + flag
- [ ] Job đối soát 3 phép so + notification
- [ ] Dọn dẹp nhỏ
- [ ] `dotnet test` toàn bộ

---

## Phụ thuộc & thứ tự

```
Phase A (nền tảng — mọi phase sau hưởng lợi từ retry)
Phase B (sau A; đụng DeliveryNote flow)
Phase C (sau A; C1 đụng costing nhẹ, nên làm trước D để D chỉ còn lock/sequence)
Phase D (sau C)
Phase E (cuối)
```

- Giao với plan Orders: Phase 2 (outbox) của plan Orders làm cho `GoodsReceiptCreated`/`DeliveryNoteDelivered` handler có retry — **nên làm trước Phase E** của plan này; mã phiếu PCT nằm trong Phase 4 plan Orders.
- Mỗi phase 1 PR riêng + file `plans/Inventory/implement_inventory_hardening_phase{X}.md` khi bắt đầu code.
- Migration tổng cộng: A (RowVersion), B (StockReservationEntry + backfill), D (SQL Sequence), E (không có). Mỗi cái user tự chạy `dotnet ef database update` sau khi duyệt.

## Verification Plan

- `dotnet build NamEcommerce.sln` + `dotnet test` sau mỗi phase
- Filter: `~InventoryStockManager`, `~StockTransferNote`, `~StockAdjustment`, `~InventoryCosting`, `~Reservation`
- Smoke test tay sau Phase B: tạo DN → confirm → để counter lệch giả lập (sửa DB tay) → cancel DN → reserved release phần còn lại, không throw
- Smoke test tay sau Phase C: approve phiếu chuyển có 1 item lỗi (sản phẩm thiếu tồn) → kiểm tra DB: không có movement nào được ghi, phiếu vẫn Draft
- Chạy job đối soát trên bản sao DB production trước khi deploy để biết mức lệch hiện hữu (kết quả quyết định có cần script sửa dữ liệu lịch sử không)

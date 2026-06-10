# Data Access Refactor Proposal — thay thế ngữ nghĩa Repository hiện tại

Đề xuất phương án cải tổ tầng data access TRƯỚC khi thực thi `plans/Orders/plan_sales_workflow_hardening.md` và `plans/Inventory/plan_inventory_hardening.md`, đồng thời làm nền cho hệ thống về sau.

---

## Lưu ý cho AI:
- KHÔNG VIẾT TEST
- KHÔNG TẠO MIGRATION HOẶC UPDATE DATABASE


## 1. Chẩn đoán — nhược điểm cụ thể của pattern hiện tại (có dẫn chứng code)

### 1.1 🔴 BẪY DATA LOSS đang hoạt động: `UpdateAsync` không attach

```csharp
// NamEcommerceEfDbContext.cs
async Task<TEntity> IDbContext.UpdateAsync<TEntity>(TEntity entity, CancellationToken ct)
{
    await SaveChangesAsync(ct).ConfigureAwait(false);   // ← không Attach/Update entity!
    return entity;
}
```

```csharp
// EntityDataReader.cs
public IQueryable<TEntity> DataSource => _dbContext.GetDataSource<TEntity>().AsNoTracking();
```

Entity lấy từ `DataSource` là **untracked** → mutate rồi gọi `repository.UpdateAsync(entity)` = **không ghi gì xuống DB, không lỗi, không domain event** (interceptor chỉ quét ChangeTracker).

Các chỗ đang dính thật (rà nhanh, chưa đầy đủ):

| Vị trí | Hậu quả |
|---|---|
| `CustomerDebtManager.RecordPaymentAsync` (nhánh tìm debt theo `DeliveryNoteId`, line ~166) | Payment được insert nhưng debt KHÔNG giảm nợ |
| `CustomerDebtManager.RecordFlexiblePaymentForCustomerAsync` (line ~246: `debtReader.DataSource.Where(...)`) | Thanh toán linh động: tiền ghi nhận, nợ không giảm |
| `CustomerDebtManager.CreateDebtFromDeliveryNoteAsync` (deposit loop, line ~106) | `deposit.MarkAsApplied()` không lưu → cọc bị áp LẶP LẠI cho mọi phiếu nợ sau |
| `CustomerDebtManager.ApplyCreditNoteFromCustomerReturnAsync` (line ~344, ~360) | Credit note trừ nợ không được lưu trên debt |

Bằng chứng dev đã biết bẫy này nhưng API không ngăn được: `ConsumeCreditNoteByRefundAsync` phải tự reload `var tracked = await creditNoteRepository.GetByIdAsync(creditNote.Id)` trước khi update. Unit test không bắt được vì test dùng fake repository (fake update hoạt động bình thường).

> **Việc cần làm NGAY (hotfix, trước mọi refactor):** xem mục 4.

### 1.2 Không có Unit of Work — SaveChanges per write

Mỗi `InsertAsync`/`UpdateAsync` = 1 `SaveChangesAsync` = 1 transaction riêng. Mọi nghiệp vụ đa bước (approve chuyển kho, ghi nợ + áp cọc, giao hàng + trừ kho) **không nguyên tử**. Đây là gốc của ≥ 6 issue trong 2 plan kia. `BeginTransactionAsync` có sẵn nhưng chỉ 2/30+ flow dùng (`DeliveryRunManager`, `FastSaleAppService`) — kỷ luật "tự nhớ wrap" không scale.

### 1.3 Tracked vs untracked không nhất quán trong cùng 1 interface

`IEntityDataReader.GetByIdAsync` → `FindAsync` → **tracked**; `IEntityDataReader.DataSource` → **untracked**. Người viết manager không thể biết entity mình cầm có "sống" không nếu không thuộc lòng implementation → chính là nguồn sinh bug 1.1.

### 1.4 Domain event dispatch post-commit, in-process, không retry

`DomainEventDispatchInterceptor` publish sau commit; handler fail → side effect tài chính/kho mất vĩnh viễn (đã phân tích ở 2 review trước). Pattern hiện tại buộc mọi handler phải tự chế idempotency ("UpTo", check-exists) — nhiều, lặp, dễ sót.

### 1.5 Generic repository không tôn trọng aggregate boundary

`IRepository<T>` mở cho mọi `AppEntity` → có thể insert/update `OrderItem`, `DeliveryNoteItem`... lẻ, vòng qua aggregate root và invariant của nó. DDD mà entity con sửa được ngoài root chỉ còn là quy ước miệng.

### 1.6 Thiếu convention concurrency

Chỉ `PurchaseOrder` có `RowVersion`. Mọi read-modify-write trên `InventoryStock`, `CustomerDebt`... đều có thể lost-update (chi tiết ở plan Inventory Phase A).

### 1.7 Hệ quả phụ

Managers tự chế cache entity (`_cachedInventoryStocks`), generate code bằng `Count()+1` không khóa, `Task.Run` bọc query — đều là triệu chứng của việc tầng dưới không cho công cụ đúng.

---

## 2. Ba phương án

### Phương án 1 — Ambient transaction per command (ít xâm lấn nhất)

Giữ nguyên contract repository (vẫn SaveChanges per write), thêm:
- MediatR `IPipelineBehavior` mở transaction đầu mỗi Command, commit cuối → các SaveChanges con nằm trong 1 transaction → nguyên tử per request.
- Hotfix `UpdateAsync` attach-if-detached (mục 4).
- Outbox cho reliable events (plan Orders Phase 2 giữ nguyên).

**Ưu:** triển khai ~vài ngày; managers không đổi. **Nhược:** transaction kéo dài suốt request (kể cả lúc render/IO phụ) → lock contention; background jobs & event handlers ngoài pipeline không được bọc, vẫn phải wrap tay; không sửa tật thiết kế (1.3, 1.5); SaveChanges nhiều lần tốn round-trip. Đây là **giảm đau**, không phải chữa.

### Phương án 2 — Unit of Work thật + domain event qua Outbox ⭐ KHUYẾN NGHỊ

Đổi **ngữ nghĩa** (không đổi nhiều **chữ ký**) của tầng data:

1. **Repository chỉ stage, không save**: `InsertAsync` = `Add`, `UpdateAsync` = `Attach nếu detached`, `DeleteAsync` = `Remove`. Không còn `SaveChangesAsync` bên trong.
2. **`IUnitOfWork.CommitAsync()`** là điểm save DUY NHẤT, được gọi ở 3 chỗ:
   - MediatR pipeline behavior — cuối mỗi Command (Web.Framework handlers);
   - cuối mỗi AppService method cho flow ngoài pipeline (Customer.Api, jobs);
   - OutboxProcessor — cuối mỗi event handler scope.
3. **Domain event → Outbox trong cùng transaction**: interceptor chuyển từ "publish post-commit" sang "serialize event thành OutboxMessage tại `SavingChanges`". `OutboxProcessor` (đã có) publish qua MediatR sau commit, **mỗi handler 1 scope + 1 commit + retry + dead-letter**. Handler hiện tại giữ nguyên `INotificationHandler<T>` — gần như zero refactor phía handler.
4. **Concurrency convention**: `RowVersion` đưa vào `AppAggregateEntity` + mapping base → mọi aggregate có optimistic concurrency mặc định; retry policy đặt 1 chỗ ở pipeline behavior (bắt `DbUpdateConcurrencyException`, retry command tối đa N lần).
5. **Vá 1.3**: `IEntityDataReader` tuyên bố rõ là READ-ONLY — bỏ `GetByIdAsync` tracked khỏi reader (chuyển caller sang repository), DTO-projection được khuyến khích. Quy ước: *muốn sửa → load qua `IRepository`*.
6. **Vá 1.5 (mức quy ước, rẻ)**: constraint `IRepository<T> where T : AppAggregateEntity` — entity con không còn repository riêng; ai cần sửa con phải đi qua root. (Các chỗ đang vi phạm sẽ lộ ra ở compile-time — đó là điều ta muốn.)

**Ưu:**
- Sửa tận gốc 1.1, 1.2, 1.3, 1.4, 1.6 cùng lúc; 1.5 sửa được phần lớn bằng generic constraint.
- 2 plan kia **mỏng đi đáng kể** (xem mục 5).
- Chữ ký method không đổi → managers/AppServices compile như cũ; thay đổi nằm ở *thời điểm* dữ liệu chạm DB.
- Chuẩn lâu dài: multi-process an toàn (outbox + rowversion), thêm module mới không phải tự nhớ wrap transaction.

**Nhược / rủi ro:**
- Mọi code đang **ngầm dựa vào "Insert xong là có trong DB ngay"** sẽ gãy — ví dụ: generate code bằng `Count()` ngay sau insert, đọc lại qua `DataSource` ngay sau insert, gọi service ngoài giữa chừng. Phải rà có hệ thống (grep các pattern: đọc DataSource sau Insert trong cùng method; gọi `_n8n`/HTTP giữa các write).
- Event handlers in-process hiện tại đang chạy "ngay sau save" — chuyển qua outbox là **eventual consistency** (trễ vài giây). UI nào đọc kết quả side effect ngay sau action (vd: bấm Delivered rồi redirect sang trang công nợ) cần kiểm tra UX.
- Big-bang nguy hiểm → bắt buộc rollout từng bước (mục 3).

### Phương án 3 — Aggregate repository chuyên biệt + bỏ generic repo (DDD thuần)

`IOrderRepository`, `IInventoryRepository`... per aggregate root; DbContext dùng trực tiếp làm UoW; read side tách hẳn (Dapper/read model).

**Ưu:** đúng sách giáo khoa, kiểm soát boundary tuyệt đối. **Nhược:** refactor ~14 module + toàn bộ test + CLAUDE.md conventions; chi phí không tương xứng quy mô 1 cửa hàng VLXD; những lợi ích thực chất (atomicity, reliability, concurrency) Phương án 2 đã đạt được với 1/5 công sức. **Không khuyến nghị bây giờ** — có thể tiến hóa dần lên sau này nếu hệ thống mở rộng đa chi nhánh, vì Phương án 2 không chặn đường này.

---

## 3. Lộ trình triển khai Phương án 2 (3 bước, mỗi bước deploy được)

### Bước 0 — Hotfix (trước mọi thứ, deploy ngay)
- `UpdateAsync` attach-if-detached (mục 4) + rà 4 vị trí ở bảng 1.1, viết test tích hợp tái hiện (dùng SQLite in-memory hoặc LocalDB thay vì fake repo để bắt được tracking semantics).
- Đối soát dữ liệu production: các debt có payment nhưng `PaidAmount` không khớp Σ payments; deposit `IsApplied=false` nhưng đã nằm trong PaidAmount của debt nào đó → script liệt kê + sửa tay có kiểm soát.

### Bước 1 — Hạ tầng UoW song song (không đổi behavior)
- Thêm `IUnitOfWork` + `CommitAsync`; pipeline behavior gọi `CommitAsync` cuối command (lúc này vẫn no-op vì repo còn autosave).
- Interceptor: thêm nhánh `SavingChanges` ghi outbox cho event đánh dấu `IReliableDomainEvent` (chính là plan Orders Phase 2 — hợp nhất vào đây).
- `RowVersion` vào `AppAggregateEntity` + migration (cột thêm vào các bảng aggregate — 1 migration lớn nhưng an toàn, default null→generated).

### Bước 2 — Tắt autosave từng nhóm module
Thứ tự theo độ hưởng lợi và rủi ro:
1. **Debts + Inventory + StockTransfer/Adjustment** (nhiều bug nhất, hưởng lợi nguyên tử nhiều nhất)
2. Orders + DeliveryNotes + Returns + GoodsReceipts
3. Catalog, Customers, Users, phần còn lại

Kỹ thuật: flag per-module không khả thi ở mức DbContext chung → thay vào đó làm theo **call-site**: chuyển `IRepository` đăng ký DI sang bản "staged"; giữ adapter `IAutoSaveRepository` tạm cho các module chưa chuyển; module chuyển xong thì handler/AppService của nó được pipeline commit. Mỗi nhóm: rà pattern "đọc sau ghi" + chạy full test + smoke test tay các flow chính của module đó.

### Bước 3 — Dọn dẹp
- Gỡ adapter autosave; gỡ `GetByIdAsync` khỏi `IEntityDataReader`; constraint `IRepository<T> : AppAggregateEntity`; xoá các "UpTo"/check-exists idempotency hack không còn cần (giữ lại cái nào bảo vệ retry outbox); cập nhật `CLAUDE.md` + `docs/domain.md`/`application.md`.

**Ước lượng:** Bước 0: 1-2 ngày. Bước 1: 3-5 ngày. Bước 2: 2-4 ngày/nhóm module (nặng nhất là rà "đọc sau ghi"). Bước 3: 2 ngày. Có thể dừng an toàn sau bất kỳ bước nào.

---

## 4. Hotfix `UpdateAsync` (chi tiết, làm ngay cả khi chưa duyệt phần còn lại)

```csharp
async Task<TEntity> IDbContext.UpdateAsync<TEntity>(TEntity entity, CancellationToken ct)
{
    var entry = Entry(entity);
    if (entry.State == EntityState.Detached)
        Update(entity);            // attach + mark Modified toàn bộ
    await SaveChangesAsync(ct).ConfigureAwait(false);
    return entity;
}
```

- Trade-off: `Update()` mark Modified mọi cột → bản ghi untracked cũ có thể đè thay đổi mới hơn (chưa có RowVersion). Vẫn **tốt hơn tuyệt đối** so với hiện tại (mất hẳn write). RowVersion ở Bước 1 sẽ đóng nốt lỗ này.
- Lưu ý domain events: entity untracked được attach thì interceptor sẽ thấy và dispatch — đúng hành vi mong muốn.
- Kèm theo: sửa 4 call site ở bảng 1.1 sang load tracked qua `repository.GetByIdAsync` (phòng thủ 2 lớp), viết integration test với real DbContext cho cả 4.

---

## 5. Ảnh hưởng lên 2 plan đã viết (nếu duyệt Phương án 2)

| Plan | Mục | Thay đổi |
|---|---|---|
| Orders | Phase 2 (outbox reliable events) | **Hợp nhất** vào Bước 1 — không làm riêng |
| Orders | Phase 1, 3, 4, 5 | Giữ nguyên; Phase 1 nên làm SAU Bước 0 (vì bug deposit thật ra là bug 1.1 + bug all-or-nothing chồng nhau) |
| Inventory | Phase A (RowVersion InventoryStock) | Thu nhỏ — RowVersion convention làm ở Bước 1 cho mọi aggregate; chỉ còn retry helper |
| Inventory | Phase C (transaction wrap tay cho transfer/adjustment) | **Biến mất phần transaction** — UoW lo; chỉ còn guard status đầu method + bỏ clamp + idempotency |
| Inventory | Phase B, D, E | Giữ nguyên |

Thứ tự tổng thể đề xuất: **Bước 0 → Orders Phase 1 → Bước 1 → Bước 2 (nhóm 1) → Inventory A-B-C-D-E (đã mỏng) → Bước 2 (nhóm 2-3) → Orders Phase 3-5 → Bước 3.**

---

## 6. Cho tương lai xa hơn (ghi nhận, chưa cần quyết)

- Read side: `IEntityDataReader` + Specification đang ổn cho quy mô hiện tại; nếu báo cáo nặng dần → tách read model/materialized views, không cần CQRS framework.
- Nếu mở đa chi nhánh/đa tiến trình lớn: outbox đã sẵn; cân nhắc tách OutboxProcessor thành worker riêng.
- Integration test infra: thêm fixture chạy SQL thật (Testcontainers/LocalDB) cho tầng Data — fake repository đã chứng minh là che bug nghiêm trọng (1.1).

## 7. Verification

- Bước 0: integration tests tái hiện 4 bug data-loss → xanh sau fix; script đối soát chạy trên bản sao production.
- Bước 1: test outbox atomicity (kill process giữa chừng → message còn, side effect chạy lại đúng 1 lần).
- Bước 2: full `dotnet test` + smoke checklist per module (bán → giao → thu; nhập → nợ NCC; chuyển kho; điều chỉnh).
- `dotnet build NamEcommerce.sln` sau mỗi bước.

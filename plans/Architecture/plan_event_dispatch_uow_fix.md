# Plan: Hoàn tất Data Access Refactor — Event dispatch qua Outbox + Load-for-write sweep

Tiếp nối `plan_data_access_refactor.md` (Phương án 2). Bước 1–2 đã triển khai (StagedRepository toàn bộ entity, UnitOfWorkBehavior, Outbox cho `IReliableDomainEvent`), nhưng implementation hiện tại còn 5 lỗ hổng khiến PurchaseOrder/QuickCreate crash ("instance with same key already tracked") và **âm thầm mất dữ liệu** ở mọi flow có event handler.

## Lưu ý cho AI
- KHÔNG VIẾT TEST MỚI (chỉ chạy test có sẵn)
- KHÔNG TẠO MIGRATION HOẶC UPDATE DATABASE — nếu cần migration, báo user tự làm

---

## 1. Chẩn đoán (đã xác minh trên code)

### 1.1 🔴 Event non-reliable dispatch inline trên cùng DbContext — 2 hệ quả chết người

`DomainEventDispatchInterceptor.SavedChangesAsync` publish mọi event KHÔNG phải `IReliableDomainEvent` **inline, trong cùng request scope, trên cùng DbContext** mà ChangeTracker vẫn còn giữ toàn bộ entity của command:

1. **Mất write im lặng**: handler stage thay đổi qua StagedRepository SAU lần `SaveChangesAsync` duy nhất của request (UnitOfWorkBehavior đã commit xong) → không ai save nữa. Ảnh hưởng hiện tại (đã xác minh từng handler):
   - `GoodsReceiptCreatedHandler`: cộng tồn kho + cost layer + sinh VendorDebt → **mất**
   - `OrderReservationEventHandlers` (OrderItemAdded/Updated/Removed): ProductReservationLedger → **mất**
   - Audit handlers (OrderItemChangeAudit, PurchaseOrderItemChangeAudit) → **mất**
   - Mọi SystemNotification handler → **mất**
   - `StockAdjustmentNoteApproved`, `StockTransferNoteApproved`, `DeliveryNoteCreated/Delivering`, `VendorRefundCompleted`, picture-cleanup handlers... → **mất**
2. **Crash duplicate tracking** (lỗi QuickCreate): handler load lại entity vừa save qua `IEntityDataReader` (AsNoTracking → instance MỚI) rồi `repository.UpdateAsync` → `_context.Update(copy)` trong khi instance gốc còn tracked → `InvalidOperationException`.
   - Trace cụ thể QuickCreate: command insert VendorPayment (AdvancePayment, tracked) → commit → `GoodsReceiptCreated` dispatch inline → `VendorDebtManager.CreateDebtFromGoodsReceiptAsync` → query `paymentReader.DataSource` thấy payment vừa save (copy untracked) → `paymentRepository.UpdateAsync(advance)` → 💥

> Trước refactor, repo autosave nên mỗi write của handler tự save → flow chạy được. Sau khi chuyển staged, **toàn bộ inline handler hoặc mất write hoặc crash**. Đây là lỗ hổng phải vá TRƯỚC TIÊN.

### 1.2 🔴 Pattern load-for-write sai trong managers (bom hẹn giờ)

Nhiều manager load entity qua `reader.GetByIdAsync`/`DataSource` (untracked) → mutate → `repository.UpdateAsync` (attach). Chạy được khi đứng một mình, nhưng crash khi cùng scope đã có instance tracked (chuỗi nhiều manager / handler outbox đụng nhau / 2 lần load cùng entity trong 1 command). Pattern ĐÚNG đã có sẵn làm mẫu: `InventoryStockManager.TryGetInventoryStockForProductAsync` — lấy Id qua reader, load entity qua `repository.GetByIdAsync` (FindAsync dedupe qua ChangeTracker).

### 1.3 🟡 Fallback autosave còn sống

`Program.cs:281` — `AddScoped(typeof(IRepository<>), typeof(NamEcommerceEfRepository<>))`. Entity nào không nằm trong ~70 dòng đăng ký explicit sẽ dùng repo autosave → SaveChanges GIỮA command → phá atomicity + kích hoạt dispatch event giữa chừng (re-entrant).

### 1.4 🟡 Flow ngoài MediatR pipeline không commit

- `DataSeederRunner` + toàn bộ seeders: dùng staged repo nhưng **không gọi `IUnitOfWork.CommitAsync`** → seed không ghi gì xuống DB.
- Cần audit các entry-point khác ngoài pipeline (background jobs, Customer.Api nếu có flow không qua MediatR).

### 1.5 🟡 Sinh code bằng `Count()+1` — trùng code trong cùng command

17 vị trí (đã grep). Với staged repo, N entity tạo trong 1 command đều `Count()` trên DB chưa có bản ghi mới → cùng code. QuickCreate nhiều dòng = nhiều GoodsReceipt cùng code `PN-yyMM-xxx`.

---

## 2. Nguyên tắc sau khi fix (sẽ cập nhật CLAUDE.md + docs)

1. **Mọi domain event đi qua Outbox.** Không còn inline dispatch. Handler chạy trong scope DI riêng của OutboxProcessor, có commit riêng + retry + dead-letter. Side effect của handler là eventual (trễ ~polling interval).
2. **Load-to-write = `IRepository<T>.GetByIdAsync`** (tracked, dedupe). `IEntityDataReader<T>` chỉ để đọc — projection/validate/hiển thị. Cần entity phức tạp để sửa: lấy Id qua reader, load qua repository.
3. **Không bao giờ** mutate + `UpdateAsync` một entity lấy từ `DataSource`.
4. Trong command KHÔNG đọc lại từ DB thứ vừa stage (read-after-write) — dùng chính instance đang cầm.

---

## 3. Các phase triển khai

### Phase 1 — Route TOÀN BỘ domain event qua Outbox ⭐ (fix gốc QuickCreate + mất write)

Mục tiêu: xoá nhánh inline dispatch; mọi event serialize vào outbox trong cùng transaction.

1. `DomainEvent` base (Domain.Shared/Events/DomainEvent.cs): implement thêm `IIntegrationEvent` (hoặc nới check ở OutboxProcessor — chọn 1, ưu tiên sửa OutboxProcessor check `is not IDomainEvent` để khỏi đụng base record equality).
2. `DomainEventDispatchInterceptor`:
   - `SerializeReliableEventsToOutbox` → đổi filter từ `e is IReliableDomainEvent` thành **mọi `IDomainEvent`**; clear toàn bộ events sau khi serialize.
   - **Xoá** `SavedChanges`/`SavedChangesAsync` overrides + `DispatchDomainEventsAsync` (không còn inline dispatch).
3. `OutboxProcessor`: sửa check deserialize (`IIntegrationEvent` → `IDomainEvent`); xác nhận `db.SaveChangesAsync` cuối `ProcessSingleAsync` commit cả side effect của handler lẫn `MarkAsProcessed` (đã đúng — line 142).
4. Giảm `PollingIntervalSeconds` xuống 1–2s (appsettings) để UX không cảm nhận trễ. (Tuỳ chọn nâng cao, làm sau nếu cần: signal channel đánh thức processor ngay sau commit.)
5. Giữ nguyên interface `IReliableDomainEvent` (giờ mọi event đều reliable — không cần sửa ~30 file event; sẽ dọn ở Bước 3 plan gốc).
6. **Rà UI đọc-ngay-sau-action** (rủi ro eventual consistency — plan gốc mục 2 đã cảnh báo): các redirect sau Create/Receive/Delivered có hiển thị dữ liệu do handler sinh (tồn kho, công nợ, notification) → chấp nhận trễ 1–2s hoặc thêm reload nhẹ. Liệt kê và test tay ở Phase 5.
7. Outbox sẽ phình nhanh hơn (mọi event đều ghi) → thêm 1 việc nhỏ: purge message processed sau N ngày trong tick của OutboxProcessor (tuỳ chọn, có thể tách).

**Verify:** build; tạo GoodsReceipt thường → tồn kho cộng đúng + VendorDebt sinh ra (qua outbox); bảng OutboxMessages: message processed, không message failed.

**Lưu ý đặc biệt:** sau Phase 1, lỗi duplicate-track của QuickCreate biến mất (handler chạy ở scope mới), payment QuickCreate sẽ thành AdvancePayment rồi được auto-apply vào debt khi `CreateDebtFromGoodsReceiptAsync` chạy qua outbox — hành vi hội tụ đúng, chỉ trễ vài giây.

### Phase 2 — Load-for-write sweep trong Domain.Services (defense in depth)

Quy tắc cơ học cho từng manager: tìm mọi chỗ `X = reader.GetByIdAsync(...)` hoặc `X = ...DataSource...First/Where...` mà sau đó `X` bị mutate rồi `repository.UpdateAsync(X)` / `DeleteAsync(X)` → đổi nguồn load sang `repository.GetByIdAsync(id)`. Nếu cần filter phức tạp: query **chỉ lấy Id** qua reader rồi load qua repository (mẫu: `InventoryStockManager.TryGetInventoryStockForProductAsync`).

Thứ tự theo độ nóng (các site đã xác định trước; khi sweep phải rà cả file):

| Nhóm | File | Site đã biết |
|---|---|---|
| 2a Debts | `VendorDebtManager` | `CreateDebtFromPurchaseOrderAsync` + `CreateDebtFromGoodsReceiptAsync` (advance loop: `paymentReader.DataSource` → `paymentRepository.UpdateAsync`), `RecordFlexiblePaymentForVendorAsync` (`debtReader.DataSource` → `debtRepository.UpdateAsync`) |
| 2a Debts | `CustomerDebtManager` | 4 vị trí bảng 1.1 plan gốc (RecordPayment theo DeliveryNoteId, RecordFlexiblePayment, deposit loop CreateDebtFromDeliveryNote, ApplyCreditNote) — kiểm tra hiện trạng từng cái |
| 2b PurchaseOrders | `PurchaseOrderManager` | ~11 method: `UpdatePurchaseOrderAsync`, `AddPurchaseOrderItemAsync`, `UpdatePurchaseOrderItemAsync`, `ClosePartialAsync`, `ChangeStatusAsync`, `DeleteOrderItemAsync`, `SplitPurchaseOrderAsync`, `AddItemsToExistingDraftAsync`, `VerifyStatusAsync`, `SetGoodsReceiptToPurchaseOrderAsync` (cả GR lẫn PO), `RemoveGoodsReceiptFromPurchaseOrderAsync` (cả GR lẫn PO) |
| 2b GoodsReceipts | `GoodsReceiptManager` | các method load GR qua `goodsReceiptDataReader.GetByIdAsync` rồi update/delete |
| 2c Orders + DeliveryNotes | `OrderManager`, `DeliveryNoteManager`, `DeliveryRunManager` | sweep theo quy tắc |
| 2c Returns | `CustomerReturnManager`, `VendorReturnManager` | sweep |
| 2d Inventory + còn lại | `InventoryCostingManager`, `PurchaseOrderAllocationManager`, `DirectShipManager`, `StockTransfer/StockAdjustment`, Finance, Catalog, Customers, CustomerPortal | sweep (~33 file có `repository.UpdateAsync`, 168 call-site — đa số đã đúng, chỉ đổi chỗ sai) |

Lưu ý kỹ thuật khi đổi sang `repository.GetByIdAsync` (= `FindAsync`):
- Navigation collection chỉ load nếu mapping có `AutoInclude()` (PurchaseOrder.Items đã có). **Checklist phụ:** kiểm tra mapping các aggregate có collection được code truy cập sau khi load (Order.Items, GoodsReceipt.Items, DeliveryNote.Items, CustomerReturn/VendorReturn items, DeliveryRun...) — thiếu thì thêm `AutoInclude()` vào mapping (không cần migration).
- `FindAsync` bỏ qua query filter soft-delete → giữ nguyên hành vi cũ của reader? Reader có filter. Nếu method cũ dựa vào filter, thêm guard `IsDeleted` sau khi load.

**Verify:** `dotnet build` + `dotnet test` sau mỗi nhóm; grep xác nhận không còn pattern `DataSource`-mutate-`UpdateAsync` trong nhóm đã sweep.

### Phase 3 — Gỡ fallback autosave + commit cho flow ngoài pipeline

1. `Program.cs`: thay `AddScoped(typeof(IRepository<>), typeof(NamEcommerceEfRepository<>))` bằng `AddScoped(typeof(IRepository<>), typeof(StagedRepository<>))` — và **xoá ~70 dòng đăng ký explicit** (mặc định mới cover tất cả).
2. Xoá class `NamEcommerceEfRepository` + các method autosave trên `IDbContext`/`NamEcommerceEfDbContext` (`AddAsync`, `UpdateAsync`, `RemoveAsync`) nếu không còn caller (grep trước khi xoá).
3. `DataSeederRunner.RunAsync`: inject `IUnitOfWork`, gọi `CommitAsync` sau mỗi seeder (per-seeder để seeder fail không kéo nhau).
4. Audit entry-point ngoài pipeline: grep nơi gọi AppService/Manager không qua `IMediator` (background services, minimal API, Customer.Api) → bổ sung `CommitAsync`. `E2ETestDataService` đã dùng — xác nhận lại.

**Verify:** build; chạy app từ DB trống → seeders ghi đủ (Roles, Warehouses, Customer hệ thống, UnitMeasurement, Admin, AccountingSetup, Permissions).

### Phase 4 — Chống trùng code sinh bằng `Count()+1` trong cùng command

Phạm vi tối thiểu (không migration): tạo helper dùng chung `EntityCodeGenerator` (Domain.Services) — per-scope (Scoped DI), bọc logic: `count DB theo prefix` + **bộ đếm in-memory theo prefix trong scope** để N lần gọi trong cùng command ra code khác nhau. Thay 17 call-site `GenerateXxxCodeAsync` hiện tại dùng helper này.

- Đây là fix "trùng trong 1 request". Trùng giữa 2 request đồng thời vẫn có thể xảy ra (như hiện tại) — ghi nhận, để sau (DB sequence + migration, user tự quyết).

**Verify:** QuickCreate 2+ dòng khác warehouse → 2 GoodsReceipt code khác nhau.

### Phase 5 — Smoke test end-to-end các flow nóng

Checklist tay (sau khi tất cả phase xong, app chạy local):

1. **PO QuickCreate** (nhiều dòng + ReceiveImmediately + Payment): không exception; PO Approved/Receiving đúng; GR sinh đủ với code khác nhau; tồn kho cộng đúng (chờ outbox ~2s); VendorDebt sinh + AdvancePayment được auto-apply; movement log đủ.
2. Tạo GR thường → định giá sau (`GoodsReceiptItemUnitCostSet`) → cost layer + debt sinh đúng.
3. Bán hàng: tạo Order (reservation ledger ghi), giao DeliveryNote → Delivered → trừ kho + CustomerDebt (đường outbox cũ vẫn chạy).
4. Returns 2 chiều: CustomerReturn confirm, VendorReturn confirm.
5. StockTransfer approve + StockAdjustment approve.
6. Xoá GR → hoàn tồn.
7. SystemNotification xuất hiện (trễ nhẹ chấp nhận được).
8. Bảng OutboxMessages: không message failed tồn đọng; message processed tăng đều.

### Phase 6 — Cập nhật tài liệu

- `CLAUDE.md` mục Repository semantics: bổ sung "mọi domain event qua outbox (eventual, retry); handler phải idempotent; load-to-write qua IRepository".
- `docs/domain.md` + `docs/application.md`: quy tắc mục 2 của plan này.
- Đánh dấu `plan_data_access_refactor.md` Bước 2 hoàn tất, Bước 3 còn lại (gỡ `GetByIdAsync` khỏi reader + constraint `AppAggregateEntity`) — chưa làm trong plan này.

---

## 4. Rủi ro & giảm thiểu

| Rủi ro | Giảm thiểu |
|---|---|
| Eventual consistency làm UI thiếu dữ liệu ngay sau action (tồn kho, nợ, notification) | Polling 1–2s; Phase 5 rà từng màn hình; nếu màn nào bắt buộc sync → cân nhắc làm việc đó trong command thay vì handler |
| Handler không idempotent bị retry chạy 2 lần | Outbox retry đã có MaxRetryCount; các handler debt/stock đã có idempotency ("UpTo", check-exists) — giữ nguyên, KHÔNG xoá ở plan này |
| Event không round-trip JSON (collection, record lồng nhau) | `OrderCancelled` (có collection) đã chạy outbox từ trước — pattern đã chứng minh; Phase 1 verify bằng cách quét OutboxMessages failed |
| FindAsync không load navigation → NullRef/logic sai sau sweep Phase 2 | Checklist AutoInclude per-aggregate trước khi đổi |
| Gỡ fallback autosave làm lộ flow đang lén dựa vào autosave | Compile vẫn pass (cùng interface) — phát hiện bằng smoke test Phase 5; đổi từng bước, commit riêng để bisect |

## 5. Thứ tự & ước lượng

Phase 1 (½–1 ngày) → Phase 3.3 seeders (nhanh, gộp sớm vì đang hỏng) → Phase 2a/2b (1–2 ngày) → Phase 3 còn lại (½ ngày) → Phase 4 (½ ngày) → Phase 2c/2d (1–2 ngày) → Phase 5 + 6 (½–1 ngày).

Mỗi phase một commit riêng, build + test xanh trước khi sang phase kế.

---

## 6. TodoList implementation

- [x] **P1.1** OutboxProcessor: check `INotification` thay `IIntegrationEvent` (cover cả IDomainEvent lẫn IIntegrationEvent thuần)
- [x] **P1.2** Interceptor: serialize mọi event vào outbox; xoá inline dispatch (`SavedChanges*` + `DispatchDomainEventsAsync` + bỏ IServiceProvider)
- [x] **P1.3** PollingIntervalSeconds = 2 (appsettings.json, section `Outbox`)
- [ ] **P1.4** Smoke (cần user chạy app): tạo GR thường → stock/debt qua outbox OK; QuickCreate hết crash
- [x] **P3.3** DataSeederRunner: CommitAsync sau mỗi seeder (đã có sẵn)
- [x] **P2a** Sweep VendorDebtManager + CustomerDebtManager
- [x] **P2b** Sweep PurchaseOrderManager + GoodsReceiptManager (kèm checklist AutoInclude)
- [x] **P2c** Sweep Orders/DeliveryNotes/Returns
- [x] **P2d** Sweep Inventory/Allocation/DirectShip/StockTransfer/StockAdjustment/Finance/Catalog/Customers/CustomerPortal
- [x] **P3.1** Đổi default `IRepository<>` → StagedRepository, xoá ~70 đăng ký explicit
- [ ] **P3.2** Xoá NamEcommerceEfRepository + autosave methods trên IDbContext (Customer.Api còn dùng — skip)
- [x] **P3.4** Audit entry-point ngoài pipeline: CassoReconciliation + DeliveryNoteReconciliation bổ sung CommitAsync
- [x] **P4** EntityCodeGenerator per-scope (12 manager implementations)
- [ ] **P5** Smoke test checklist 8 mục
- [x] **P6** Cập nhật CLAUDE.md (Repository semantics section)

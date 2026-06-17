# Plan — DeliveryMobile: theo dõi lấy hàng đa kho + gộp hiển thị theo sản phẩm

## Bối cảnh / vấn đề (user nêu)

Phiếu giao hàng có thể cần lấy hàng từ **nhiều kho**. Ba bất cập:

1. **Không theo dõi được quá trình lấy hàng** giữa lúc Shipper "Nhận chuyến" và lúc WarehouseManager "Bàn giao". Hiện chỉ có cache/manifest rồi bàn giao một phát — không biết kho nào đã lấy, lấy gì.
2. **Giao cho khách hiển thị rối**: 1 sản phẩm tách kho (5 bao kho chính + 4 bao kho phụ) hiện **2 dòng** vì khóa theo `DeliveryNoteItemId`. Shipper chỉ cần thấy **tổng** (9 bao).
3. **Trả hàng tương tự**: shipper chỉ cần "đang giao 9 bao, khách trả 3 bao" ở cấp sản phẩm.

## Tiền đề

- Đã refactor bỏ `DeliveryNote.WarehouseId` (header) — single source of truth là `DeliveryNoteItem.WarehouseId`. **Migration refactor đó phải được áp trước** (xem [[project_remove_dn_header_warehouse]]).
- Kho nhập lại hàng trả quyết định lúc **duyệt phiếu trả** (CustomerReturn.WarehouseId chọn ở confirm), KHÔNG suy từ kho item. → Phân bổ số trả về các item-theo-kho chỉ là bookkeeping, an toàn.
- Trong 1 phiếu xuất, các item cùng sản phẩm (tách kho) **cùng UnitPrice** (cùng OrderItem) → thứ tự phân bổ số trả không ảnh hưởng tiền.

## Quyết định chốt

- #1 chọn **mức B**: có cổng xác nhận lấy hàng theo từng kho; "Bàn giao" chỉ mở khi **mọi kho đã xác nhận "đã lấy đủ"**. **Không thêm enum** mới (giữ `DeliveryRunStatus` nguyên) — trạng thái lấy hàng lưu ở bảng phụ.
- #2/#3: gộp hiển thị theo **sản phẩm** (tổng SL) ở màn shipper; số khách trả nhập ở cấp sản phẩm; backend mở rộng về `DeliveryNoteItemId` (phân bổ tuần tự).

### Giả định cần user xác nhận khi duyệt (mặc định đã chọn)
- Người bấm "đã lấy đủ" theo kho = **thủ kho/quản lý trên màn DeliveryRun Details (admin)**.
- Giữ luôn điều kiện cache/manifest hiện có; cổng lấy hàng là điều kiện **bổ sung** cho "Bàn giao".

## Ngoài phạm vi
- Không lấy hàng từng phần/thiếu theo dòng (đó là mức C).
- Không đổi mô hình thanh toán/công nợ.
- Không thêm `OrderStatus`/`DeliveryRunStatus` mới.

---

## Phase 1 — Gộp hiển thị theo sản phẩm (giao + trả) — KHÔNG migration

Làm trước vì độc lập, không cần DB, cải thiện UX ngay.

### 1a. Read model gộp theo sản phẩm
`Web.Contracts/Models/DeliveryNotes/DeliveryRunModels.cs`:
- Thêm `DeliveryRunProductLineModel { Guid ProductId, string ProductName, decimal TotalQuantity, string? UnitMeasurement, int QuantityDecimalPlaces }`.
- `DeliveryRunItemModel`: thêm `IList<DeliveryRunProductLineModel> ProductLines` (gộp theo sản phẩm). Giữ `ProductItems` (per-warehouse) cho nội bộ nếu cần, nhưng màn shipper dùng `ProductLines`.
- Tương tự cho settlement: `SettlementProductLines` gộp `SettlementItems` theo sản phẩm (tổng Accepted/Rejected).

### 1b. Model factory
`Web/Services/DeliveryNotes/DeliveryRunModelFactory.cs`:
- Build `ProductLines` = `note.Items.GroupBy(ProductId)` → `TotalQuantity = Sum(Quantity)`.
- Build `SettlementProductLines` = group settlement items theo ProductId.

### 1c. Mobile UI
`Web/Views/DeliveryMobile/Run.cshtml` (+ `wwwroot/modules/DeliveryMobileCache.js` nếu cache offline render):
- Render **1 dòng/sản phẩm** từ `ProductLines` (tổng SL), bỏ cột kho.
- Ô nhập "khách trả" theo sản phẩm; submit dạng product-level.

### 1d. Wire contract + mở rộng về item ở backend
- `Web.Contracts/Commands/.../CompleteMobileDeliveryNoteCommand.cs` & `RequestDeliverySettlementCommand`: đổi `Items` sang product-level `{ Guid ProductId, decimal ReturnedQuantity, string? RejectReason }` (giữ tên field acceptance JSON ở client).
- `DeliveryMobileController.ParseAcceptanceItems`: parse product-level.
- Handler `CompleteMobileDeliveryNoteHandler` + handler RequestSettlement: nạp delivery note, **mở rộng product→ per-DeliveryNoteItem** (phân bổ tuần tự `RejectedQuantity` vào các item cùng ProductId tới khi hết), tạo `DeliveryAcceptanceAppDto.Items` (khóa `DeliveryNoteItemId`) như cũ. Domain `ResolveDeliveryAcceptance` **không đổi**.
- Validate: tổng `ReturnedQuantity` của 1 sản phẩm ≤ tổng SL sản phẩm đó trong phiếu.

### TodoList 1
- [ ] Model + factory gộp `ProductLines` / `SettlementProductLines`
- [ ] Run.cshtml render theo sản phẩm + ô nhập trả theo sản phẩm
- [ ] Command product-level + controller parse + 2 handler mở rộng về item (phân bổ tuần tự) + validate tổng
- [ ] Build `NamEcommerce.Web.csproj`; smoke: phiếu 1 sản phẩm 2 kho → 1 dòng; trả 1 phần → tạo CustomerReturn đúng tổng

---

## Phase 2 — Cổng lấy hàng theo kho (mức B) — CÓ migration

### 2a. Domain
- Entity mới `Domain/Entities/DeliveryNotes/DeliveryRunWarehousePick.cs`: `DeliveryRunId, WarehouseId, ConfirmedByUserId, ConfirmedByFullName, ConfirmedOnUtc` (append; 1 dòng/kho đã xác nhận).
- `DeliveryRun`:
  - `_warehousePicks` + `IReadOnlyCollection<DeliveryRunWarehousePick> WarehousePicks`.
  - `ConfirmWarehousePick(Guid warehouseId, Guid? userId, string? fullName, DateTime onUtc)` — idempotent (đã có thì bỏ qua); chỉ khi `Status == ReadyForHandover`.
  - `HandOver(IReadOnlyCollection<Guid> requiredWarehouseIds, Guid? handedOverByUserId, DateTime onUtc)`: thêm điều kiện `requiredWarehouseIds` ⊆ các kho đã xác nhận, thiếu → throw `Error.DeliveryRunPickingIncomplete`. Giữ điều kiện cache/manifest hiện có.
- Mapping `DeliveryRunWarehousePickMap` + migration (tạo bảng). **User chạy migration.**

### 2b. Manager
`DeliveryRunManager`:
- `ConfirmWarehousePickAsync(Guid runId, Guid warehouseId)`: validate `warehouseId` thuộc tập kho của chuyến (suy từ delivery-note items); ghi nhận theo current user.
- `GetPickingManifestAsync(Guid runId)`: gom item của tất cả phiếu trong chuyến theo `WarehouseId → ProductId → SUM(Quantity)`, kèm cờ `Confirmed` mỗi kho.
- `HandOverAsync`: tính `requiredWarehouseIds` = distinct kho từ items các phiếu; truyền vào `run.HandOver(...)`.

Lưu ý: cần đọc item theo kho — nạp delivery notes của chuyến (đã có `deliveryNoteReader`).

### 2c. Presentation (admin DeliveryRun Details)
- `DeliveryRunModels.cs`: `DeliveryRunPickingManifestModel { IList<WarehousePickGroup> }`, mỗi group `{ Guid WarehouseId, string WarehouseName, bool Confirmed, string? ConfirmedByFullName, DateTime? ConfirmedOnUtc, IList<ProductLine> Products }`.
- `DeliveryRunModelFactory`: build manifest cho Details.
- `Web.Contracts/Commands/.../DeliveryRunCommands.cs`: `ConfirmDeliveryRunWarehousePickCommand(Guid Id, Guid WarehouseId)`.
- Handler + `DeliveryRunController` action `ConfirmWarehousePick`.
- `Views/DeliveryRun/Details.cshtml`: khối "Phiếu lấy hàng theo kho" — mỗi kho 1 card: bảng sản phẩm + tổng SL + nút **"Đã lấy đủ"** + badge đã xác nhận (ai/lúc nào). Nút **"Bàn giao"** disable tới khi mọi kho confirmed (hiện tiến độ X/Y kho).
- Resource (en/vi): `Error.DeliveryRunPickingIncomplete`, nhãn "Phiếu lấy hàng theo kho", "Đã lấy đủ", "Đã lấy X/Y kho".

### TodoList 2
- [ ] Entity + mapping + migration (user chạy) — bảng `DeliveryRunWarehousePick`
- [ ] DeliveryRun: WarehousePicks + ConfirmWarehousePick + HandOver gate
- [ ] Manager: ConfirmWarehousePickAsync + GetPickingManifestAsync + HandOverAsync gate
- [ ] Command/handler/controller + model + Details.cshtml UI + resources
- [ ] Build `NamEcommerce.Web.csproj`; smoke: chuyến 2 kho → xác nhận từng kho → bàn giao bị chặn tới khi đủ

---

## Verification
- Build `NamEcommerce.Web.csproj` mỗi phase (không build .sln, không viết test — theo quy ước).
- Smoke #2/#3: phiếu 1 sản phẩm 2 kho → màn shipper 1 dòng tổng; khách trả 1 phần → CustomerReturn tổng đúng, kho chọn lúc duyệt.
- Smoke #1: chuyến gồm phiếu lấy ở ≥2 kho → Details hiện manifest theo kho; xác nhận thiếu 1 kho → "Bàn giao" chặn; đủ → bàn giao OK.

## Migration
- Phase 1: không.
- Phase 2: tạo bảng `DeliveryRunWarehousePick` (user tự tạo + chạy). Refactor bỏ `DeliveryNote.WarehouseId` phải áp trước.

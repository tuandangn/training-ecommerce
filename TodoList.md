# TodoList — VLXD Tuấn Khôi / NamEcommerce

> File theo dõi các hạng mục còn pending.

---

### Quy tắc bắt buộc

- **Unit test**: KHÔNG viết unit test mới, KHÔNG sửa code trong bất kỳ project `*.Test` nào.
- **Migration**: AI KHÔNG tự chạy migration — báo Tuấn tự chạy.
- **Skills**: AI đọc skill `namcommerce` trước khi viết code domain.
- **Comments**: chỉ viết comment khi thật cần thiết.

---

## 🔧 Pending — Build & Migrations & Smoke Test

**Phase 5 cleanup** — chờ Tuấn quyết định:
- ~~Xóa `OrderCreatedEventHandler.cs`~~ → **Giữ + implement** (xem mục P2 bên dưới — Tuấn chốt reserve stock khi Order tạo, 2026-05-13)

---

## 🩹 Workflow Order → DeliveryNote → CustomerReturn — Plan sửa (2026-05-13)

> Kết quả review workflow đơn bán → phiếu xuất kho → trả hàng.
> Đã thống nhất với Tuấn:
> - Bỏ tính năng phiếu trả "tự do" (luôn yêu cầu DeliveryNoteId).
> - Reserve stock NGAY khi Order tạo.
> - Race condition: quick fix (gộp Inspecting vào query Confirmed), Option 2 (RowVersion) để future.
> - Thứ tự: bug-first (P0 → P1 → P2 → P3).

### P0 — Bug must-fix (rủi ro mất dữ liệu)

- [x] **Fix `OrderManager.DeleteOrderAsync` (line 299-303)** — điều kiện logic ngược ✅ 2026-05-13
  - Đã sửa: `Status != Draft && Status == Cancelled` → `Status != Draft && Status != Cancelled`
  - File: `Domain/NamEcommerce.Domain.Services/Orders/OrderManager.cs:301`
  - Không có unit test hiện có nào của `DeleteOrderAsync` (verified) → không break test
  - **Verify (Tuấn manual)**: tạo Order → tạo DN Confirmed → gọi DeleteOrder → phải throw `InvalidOperationException("Order cannot deleted because it is processing.")`

### P1 — Validation gaps + cleanup

- [ ] **Bỏ field nullable `CustomerReturn.DeliveryNoteId`** — luôn yêu cầu DN
  - Entity `CustomerReturn`: `Guid? DeliveryNoteId` → `Guid DeliveryNoteId`; `string? DeliveryNoteCode` → `string DeliveryNoteCode`
  - DTO `CreateCustomerReturnDto.DeliveryNoteId` → required (`Guid` thay vì `Guid?`); cập nhật `Verify()`
  - DTO `UpdateCustomerReturnDto` & AppDto tương ứng
  - `CustomerReturnManager.CreateAsync`: xóa nhánh "trả tự do" (line 45-66), luôn lấy customer từ DN
  - `CustomerReturnManager.ConfirmAsync`: bỏ `if (customerReturn.DeliveryNoteId.HasValue)` — luôn validate qty (line 132-145)
  - Model + Validator (Web layer) + Controller: enforce required, cập nhật ModelFactory nếu có nhánh phụ thuộc null
  - EF Configuration `CustomerReturnMapping`: đổi `.IsRequired(false)` → `.IsRequired()`
  - **Migration cần** — Tuấn tự chạy (`Add-Migration RequireDeliveryNoteOnCustomerReturn`)
  - Verify: chạy app → tạo CustomerReturn không có DN → trả về lỗi validation
  - Lưu ý: nếu DB hiện đang có row `DeliveryNoteId IS NULL` thì migration sẽ fail → cần data migration script trước (Tuấn xác nhận DB hiện chưa có data tự do)

- [x] **Magic number `(int)r.Status == 2` + race quick fix** trong `CustomerReturnManager` ✅ 2026-05-13
  - Đã rename `GetTotalConfirmedReturnQuantityAsync` → `GetTotalReservedReturnQuantityAsync` (interface + impl)
  - Đã thay `(int)r.Status == 2` bằng `r.Status == CustomerReturnStatus.Inspecting || r.Status == CustomerReturnStatus.Confirmed`
  - Đã update internal caller (line 138) + thêm `using NamEcommerce.Domain.Shared.Enums.Returns;`
  - XML doc cập nhật để document race window thu hẹp (vẫn còn race nhỏ khi 2 phiếu cùng `MoveToInspecting`)
  - Line 176 `GetListAsync` filter `(int)r.Status == status.Value` **giữ nguyên** — vì status input từ UI là int, không phải refactor cùng scope
  - **Verify (Tuấn manual)**: tạo 2 phiếu trả cùng DN → A `MoveToInspecting` qty=5 (deliveredQty=10) → B `Confirm` qty=8 → phải reject (`maxAllowed = 10 - 5 = 5 < 8`)
  - ⚠️ **Note**: `VendorReturnManager.GetTotalConfirmedReturnQuantityAsync` (line 245-265) có CÙNG bug magic number + race. Đối xứng với CustomerReturn. **Cần Tuấn chốt** có mở rộng scope sang VendorReturn trong cùng session hay làm sau.

### P2 — Reserve Stock khi Order tạo (feature — cần phân tích trước)

- [ ] **Thiết kế Reserve Stock cho Order** — Hiện `OrderCreatedEventHandler` là stub rỗng
  - **Blocker**: Order entity hiện KHÔNG có `WarehouseId` — không biết reserve ở kho nào
  - **Phương án A**: Thêm `WarehouseId` vào Order (required field). Cần migration + UI thay đổi. Rõ ràng nhất.
  - **Phương án B**: Reserve theo "default warehouse" trong AppConfig. Đơn giản nhưng cứng nhắc.
  - **Phương án C**: Reserve cấp `InventoryStock.TotalReserved` không gắn warehouse. Đến lúc tạo DN thì allocate kho cụ thể.
  - **Action**: Tuấn quyết định phương án. Sau khi chốt mới breakdown thành sub-task.
  - **Ràng buộc khi implement**:
    - Khi Order add/remove/update item → phải release+re-reserve.
    - Khi Order delete → release toàn bộ.
    - Khi DeliveryNote.Confirm → KHÔNG reserve lần nữa (vì đã reserve từ Order); chỉ allocate sang `Reserved-by-DN`.
    - Khi DeliveryNote.Cancel (DN có OrderId) → trả về `Reserved-by-Order`, KHÔNG release hẳn.

### P3 — Dọn dẹp / Documentation

- [ ] Cập nhật `RETURNS_MODULE_PLAN.md` → mark "Đã implement" (migration `20260509023752_RefactorReturns`)
- [ ] Document design intent vào XML comment của `CustomerReturnManager.FinalizeConfirmAsync`: **FIFO theo customer (không theo DN gốc)** — đây là chủ ý, không phải bug
- [ ] Review event "mồ côi" (không có handler): `DeliveryNoteCancelled`, `DeliveryNoteDelivering`, `CustomerReturnCancelled`, `OrderItemAdded/Updated/Removed`, `OrderInfoUpdated`, `OrderShippingUpdated`, `OrderLocked`, `OrderItemDelivered`, `OrderDeleted` → quyết định cho từng cái: (a) implement handler, (b) xóa event, hoặc (c) document là audit-only
- [ ] **Cross-aggregate refactor (long-term)**: tách logic update Order trong `DeliveryNoteManager.MarkDeliveredAsync` (line 150-165) và `CancelAsync` (release stock + cascade cancel CustomerReturn line 224-245) sang event handler riêng để tuân Domain Event pattern. Không ưu tiên — code hiện vẫn chạy đúng.

### Lưu ý chung khi thực hiện

- **Test**: theo rule hiện hành ở file này (KHÔNG viết unit test mới) — verify bằng smoke test/manual. Nếu Tuấn muốn TDD đúng theo skill `namcommerce`, gỡ rule này trước.
- **Migration**: P1 (bỏ nullable) và P2 (thêm WarehouseId — nếu chọn phương án A) cần migration — Tuấn tự chạy, AI chỉ chuẩn bị Configuration code.

---
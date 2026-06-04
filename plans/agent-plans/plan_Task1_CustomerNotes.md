# Implementation Plan: Task 1 — Thông tin dặn dò từ khách hàng

## Mô tả vấn đề

Sau khi đặt hàng, khách hàng có thể phát sinh thêm yêu cầu, dặn dò (ví dụ: giao trước 5h chiều, gọi điện trước khi giao, đóng gói đặc biệt...). Hiện tại không có cơ chế nào để khách hàng thêm dặn dò này từ cổng khách hàng, và admin cũng không được thông báo khi có dặn dò mới.

## Kết quả mong muốn

1. Khách hàng vào Customer Portal có thể thêm/cập nhật ghi chú/dặn dò cho đơn hàng của họ.
2. Admin thấy ghi chú trên trang chi tiết đơn hàng.
3. Mỗi lần khách hàng thêm/cập nhật ghi chú → xuất hiện sự kiện trên timeline của đơn hàng.

---

## Hiện trạng (từ điều tra)

### Đã có sẵn
- `Order.Note` field tồn tại trong entity và DB column (migration đã tạo)
- `UpdateOrderNoteCommand` + handler đã có
- Admin UI modal để sửa note đã có (`#changeNoteModal` trên `/Order/Details`)
- Timeline system đã có framework sự kiện
- `OrderInfoUpdated` domain event đã được định nghĩa (nhưng chưa có handler)

### Đang thiếu / bị lỗi
| Vấn đề | Mô tả |
|--------|-------|
| **Bug: EF Core mapping thiếu** | `OrderMapping.cs` không có `.Property(o => o.Note)` → Note không được persist đúng cách |
| **Customer Portal thiếu** | Không có form để khách nhập dặn dò; note từ đơn hàng không hiển thị trong portal |
| **Timeline không có sự kiện note** | Khi note được thêm/cập nhật, timeline không ghi nhận |
| **Không phân biệt nguồn ghi chú** | Cần biết note được thêm bởi admin hay bởi khách hàng |

---

## Quyết định kiến trúc

1. **Dùng lại `Order.Note` field** — không tạo thêm entity mới. Note là single-value, không cần history.
2. **Phân biệt actor bằng event metadata** — khi raise `OrderInfoUpdated`, ghi thêm `UpdatedBy` (admin/customer) để timeline hiển thị đúng label.
3. **Customer Portal gọi endpoint riêng** — tạo `UpdateCustomerOrderNoteCommand` riêng biệt với `UpdateOrderNoteCommand` của admin, để phân quyền rõ ràng và kiểm soát từng phía.
4. **Timeline event khi note thay đổi** — tạo handler cho `OrderInfoUpdated` để build timeline entry.

---

## Task List

### Phase 1: Sửa bug EF Core mapping

#### Task 1.1 — Fix `Order.Note` mapping trong `OrderMapping.cs`
**Mô tả:** Thêm property mapping cho `Order.Note` trong EF Core configuration.

**File:** `NamEcommerce/Infrastructure/NamEcommerce.Data.SqlServer/Mappings/OrderMapping.cs`

**Acceptance criteria:**
- [ ] `builder.Property(o => o.Note).HasMaxLength(4000)` được thêm vào
- [ ] Build thành công
- [ ] Note lưu/đọc được từ DB

**Estimated scope:** XS (1 file, 1 dòng)

---

### Phase 2: Customer Portal — Hiển thị và thêm dặn dò

#### Task 2.1 — Tạo `UpdateCustomerOrderNoteCommand`
**Mô tả:** Command để khách hàng cập nhật ghi chú đơn hàng từ portal. Cần verify `CustomerId` khớp với order để bảo mật.

**Files:**
- `NamEcommerce.Web.Contracts/Commands/Models/Orders/UpdateCustomerOrderNoteCommand.cs` (mới)
- `NamEcommerce.Web.Framework/Commands/Handlers/Orders/UpdateCustomerOrderNoteHandler.cs` (mới)

**Acceptance criteria:**
- [ ] Command có `OrderId`, `CustomerId`, `Note` (max 1000 chars)
- [ ] Handler verify `order.CustomerId == command.CustomerId` trước khi update
- [ ] Handler gọi `orderAppService.UpdateOrderAsync(...)` với note mới
- [ ] Raise `OrderInfoUpdated` event với actor là "Customer"
- [ ] Trả `CommonActionResultModel` với thông báo phù hợp

**Estimated scope:** S (2 files)

#### Task 2.2 — Thêm endpoint trong `CustomerPortalController`
**Mô tả:** Thêm action `UpdateOrderNote` vào CustomerPortalController để xử lý request từ portal.

**File:** `NamEcommerce.Web/Controllers/CustomerPortalController.cs`

**Acceptance criteria:**
- [ ] `POST /CustomerPortal/UpdateOrderNote` nhận `UpdateCustomerOrderNoteCommand`
- [ ] Chỉ cho phép khi `order.CanUpdateInfo()` là true
- [ ] Trả JSON result cho AJAX call

**Estimated scope:** S (1 file)

#### Task 2.3 — Cập nhật Customer Portal View
**Mô tả:** Thêm section hiển thị note hiện tại + form cho khách nhập dặn dò trên trang Order Detail trong portal.

**File:** `NamEcommerce.Web/Views/CustomerPortal/OrderDetail.cshtml` (hoặc view tương đương)

**Acceptance criteria:**
- [ ] Hiển thị note hiện tại (nếu có)
- [ ] Nút "Thêm dặn dò" / "Sửa dặn dò" mở modal/inline form
- [ ] Textarea max 1000 ký tự
- [ ] Submit qua AJAX → cập nhật UI không reload trang
- [ ] Chỉ hiển thị khi order còn có thể update (`CanUpdateInfo`)

**Estimated scope:** M (1 view file + inline JS)

### Checkpoint Phase 2
- [ ] Khách hàng có thể thêm/sửa dặn dò từ portal
- [ ] Note được lưu đúng vào DB
- [ ] Admin thấy note trên trang chi tiết đơn hàng

---

### Phase 3: Timeline — Ghi nhận sự kiện dặn dò

#### Task 3.1 — Mở rộng `OrderInfoUpdated` event để carry actor info
**Mô tả:** `OrderInfoUpdated` hiện có nhưng chưa có handler. Cần thêm thông tin actor (admin/customer) vào event để timeline phân biệt.

**File:** `NamEcommerce.Domain.Shared/Events/Orders/OrderEvents.cs`

**Acceptance criteria:**
- [ ] `OrderInfoUpdated` record có thêm `string? UpdatedBy` và `bool NoteChanged` property
- [ ] Domain entity Order raise event với `UpdatedBy = "Customer"` khi gọi từ customer portal, `"Admin"` từ admin

**Estimated scope:** XS (1 file)

#### Task 3.2 — Tạo `OrderNoteUpdatedEventHandler`
**Mô tả:** Handler lắng nghe `OrderInfoUpdated` khi `NoteChanged == true`, lưu timeline entry.

**Vấn đề:** Timeline hiện được build tại `OrderModelFactory.BuildTimeline()` từ dữ liệu static (không có event log storage). Cần quyết định: lưu vào `OrderAuditLog` entity mới, hay chỉ hiển thị note hiện tại trong timeline mà không track history?

**Quyết định:** Vì không yêu cầu history log, timeline chỉ cần hiển thị "Khách hàng đã thêm dặn dò" dựa trên sự tồn tại của `Order.Note` + timestamp `Order.UpdatedOnUtc`. Không cần handler event mới, chỉ cần cập nhật `BuildTimeline()`.

**File:** `NamEcommerce.Web/Services/ModelFactories/Orders/OrderModelFactory.cs`

**Acceptance criteria:**
- [ ] `BuildTimeline()` thêm entry "Khách hàng đã để lại dặn dò" khi `order.Note` không null/empty
- [ ] Entry xuất hiện với timestamp `order.UpdatedOnUtc` (hoặc `CreatedOnUtc` nếu note từ lúc đặt)
- [ ] Hiển thị nội dung note trong Description của timeline entry
- [ ] Icon phù hợp (ví dụ: `bi-chat-square-text`)

**Estimated scope:** S (1 file)

### Checkpoint Phase 3
- [ ] Timeline Admin hiển thị entry dặn dò với nội dung note

---

### Phase 4 (Optional): Admin notification khi khách dặn dò mới

> Có thể làm sau nếu user yêu cầu. Hiện tại chưa có notification system.

---

## Risks and Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| EF Core bug khiến test khó | High | Fix mapping trước tiên (Task 1.1) |
| Customer portal view chưa rõ (chưa tìm thấy file exact) | Medium | Xác nhận file path trước khi code |
| Timeline không có event store → không track history | Low | Chấp nhận: chỉ hiển thị note hiện tại, không cần history ở phase này |
| `CanUpdateInfo()` bị block khi order đã Complete | Low | Hiển thị note read-only khi không thể edit |

## Open Questions

1. **Customer Portal view file**: Tên chính xác của view trang chi tiết đơn hàng trong portal là gì? Cần xác nhận trước khi code Task 2.3.
2. **Note từ lúc đặt hàng**: Nếu khách nhập note lúc đặt, timestamp là `CreatedOnUtc`. Nếu update sau, dùng `UpdatedOnUtc`. Có cần phân biệt 2 trường hợp trên timeline không?
3. **Giới hạn ký tự**: 1000 ký tự có đủ chưa? DB hiện là `nvarchar(max)`.

## Build Order

```
Task 1.1 (bug fix EF mapping)
    → Task 2.1 (command + handler)
        → Task 2.2 (controller endpoint)
            → Task 2.3 (portal view)
                → Task 3.2 (timeline entry)
```

Task 3.1 (event extension) song song với Task 2.1 nếu cần.

## Tổng quan phạm vi

| Phase | Tasks | Scope | Ưu tiên |
|-------|-------|-------|---------|
| 1. Bug fix | 1 task | XS | P0 — phải làm đầu tiên |
| 2. Customer Portal | 3 tasks | S-M | P1 — core feature |
| 3. Timeline | 1 task | S | P2 — UX improvement |

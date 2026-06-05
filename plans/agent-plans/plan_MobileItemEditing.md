# Plan: Mobile-Friendly Item Editing

## Vấn đề

Thao tác thêm/sửa hàng hóa trên mobile gặp 4 vấn đề cốt lõi:

1. **Inline inputs quá nhỏ** — `row-qty` và `row-price` nằm trong `<td>` hẹp, touch target không đủ rộng để thao tác chính xác.
2. **Chỉ sửa được 1 field 1 lần** — debounce 1500ms tính từ lần gõ cuối, nghĩa là sau khi xong qty phải đợi 1.5s rồi mới sửa price được mà không bị conflict.
3. **Luồng thêm hàng nhiều bước** — ProductPicker search → chờ load → nhập qty → nhập price → bấm "Thêm vào" → modal đóng. Trên mobile từng bước là 1 context switch.
4. **ProductBrowser bị đẩy xuống dưới** — trên mobile `col-md-4` thành full-width nằm sau main form, phải scroll xuống để dùng.

## Kiến trúc hiện tại (relevant)

```
OrderController.js
  ├── #buildItemRow()       → tạo <tr> với inline inputs (row-qty, row-price)
  ├── debounce(1500ms)      → cập nhật state khi ngừng gõ
  ├── AddItemController     → quản lý modal addProductForm
  └── ProductBrowser        → duyệt danh sách nhanh

CreatePurchaseOrderController.js  → same #buildItemRow() pattern
```

Cả hai controller đều share pattern: inline editable inputs → debounce → state update.

## Quyết định kiến trúc

**Desktop**: Giữ nguyên inline editing — hoạt động tốt.

**Mobile (≤ 768px)**:
- Table rows là **read-only** — hiển thị summary (tên + qty × giá = total)
- Tap vào row → mở **Bottom Offcanvas** với full edit experience
- Offcanvas ghi ngược lại vào `row-qty`/`row-price` hidden inputs → form submit giữ nguyên, **không cần backend changes**
- Thêm item via ProductBrowser/ProductPicker → **bỏ qua addProductModal** → mở thẳng offcanvas với qty/price

**Tại sao Bottom Offcanvas thay vì Modal:**
- Offcanvas từ bottom là pattern native mobile (giống drawer trên app native)
- Keyboard không che nội dung khi input focused (trên modal thường bị overlap)
- Bootstrap đã có `offcanvas-bottom` sẵn — không cần thư viện mới

**Tại sao không thay toàn bộ table bằng card list:**
- Card list phức tạp hơn, cần redesign view hoàn toàn
- Offcanvas approach: Desktop giữ nguyên, Mobile chỉ thêm tap-to-edit layer
- Dễ maintain, ít thay đổi code hơn

## Phạm vi

| Module | Áp dụng | Ghi chú |
|--------|---------|---------|
| Order/Create | ✅ | OrderController.js |
| Order/Create (FastSale) | ✅ | Nằm trong Order/Create page |
| PurchaseOrder/Create | ✅ | CreatePurchaseOrderController.js — same pattern |
| FastSale standalone | ❌ | Đã mobile-first, khác UX paradigm |

---

## Task List

### Phase 1: Shared Module `ItemEditOffcanvas` (P0 — Core)

#### Task 1.1 — Tạo `wwwroot/modules/ItemEditOffcanvas.js`

Module độc lập, không biết về OrderController. Expose API:

```js
class ItemEditOffcanvas {
    constructor(offcanvasEl, callbacks)
    // callbacks: { onApply(qty, price), onDelete() }

    open(item)
    // item: { name, picture, quantity, unitPrice, quantityDecimalPlaces }

    close()
}
```

**Nội dung offcanvas:**
- Header: tên sản phẩm + ảnh thumbnail
- Quantity row: nút `−`, input số (data-decimal="quantity"), nút `+`
- Price row: input full-width (data-decimal="currency")
- Live total: `qty × price = X đ` — cập nhật realtime (không debounce)
- Footer: nút "Xóa dòng" (link-danger) + nút "Xác nhận" (btn-primary)

**Acceptance criteria:**
- [ ] Offcanvas mở từ bottom trên mobile, centered modal trên desktop (≥768px)
- [ ] Nút `+`/`−` tăng/giảm quantity theo step (1 nếu int, 0.1 nếu decimal)
- [ ] Input quantity: `inputmode="decimal"`, step phụ thuộc `quantityDecimalPlaces`
- [ ] Input price: `inputmode="decimal"`, keyboard số trên mobile
- [ ] Live total cập nhật ngay khi thay đổi qty hoặc price (không debounce)
- [ ] Nút "Xác nhận" gọi `onApply(qty, price)` rồi tự đóng
- [ ] Nút "Xóa dòng" confirm bằng `sweetalert2` rồi gọi `onDelete()`
- [ ] Focus tự động vào quantity input khi offcanvas mở

#### Task 1.2 — Tạo `Views/Shared/_ItemEditOffcanvas.cshtml`

HTML cho offcanvas. Include trong `Views/Order/Create.cshtml` và `Views/PurchaseOrder/Create.cshtml`.

```html
<div class="offcanvas offcanvas-bottom" id="itemEditOffcanvas" 
     tabindex="-1" style="height:auto; max-height:85vh; border-radius:16px 16px 0 0;">
  <div class="offcanvas-header border-bottom pb-3">
    <!-- product name + close button -->
  </div>
  <div class="offcanvas-body py-3">
    <!-- qty stepper + price input + live total -->
  </div>
  <div class="offcanvas-footer border-top p-3 d-flex justify-content-between">
    <!-- delete + apply buttons -->
  </div>
</div>
```

**Acceptance criteria:**
- [ ] `border-radius` top ở mobile trông như drawer
- [ ] `max-height: 85vh` để không che hết màn hình
- [ ] Keyboard-aware: khi keyboard mở, offcanvas scroll để không bị che
- [ ] Include partial trong các view cần thiết

---

### Phase 2: OrderController.js — Mobile Row Mode (P0)

#### Task 2.1 — `#buildItemRow()` thêm mobile/desktop mode

Detect mobile tại thời điểm render (`window.innerWidth < 768`):

- **Desktop**: render như hiện tại (inline inputs)
- **Mobile**: render simplified row:

```html
<tr data-item-index="0" class="order-item-row order-item-row--mobile">
  <td class="ps-3">
    <div class="fw-medium">[Tên sản phẩm]</div>
    <div class="text-muted small">[qty] × [price] đ</div>
  </td>
  <td class="text-end fw-bold text-primary pe-3">
    [total] đ
    <i class="bi bi-chevron-right text-muted ms-1 small"></i>
  </td>
  <!-- hidden inputs vẫn có để form submit -->
  <input type="hidden" class="row-qty" ... />
  <input type="hidden" class="row-price" ... />
</tr>
```

**Acceptance criteria:**
- [ ] Mobile row tap → mở `ItemEditOffcanvas` với dữ liệu item hiện tại
- [ ] `onApply(qty, price)` → cập nhật hidden inputs → re-render row summary → cập nhật state
- [ ] `onDelete()` → xóa item khỏi state → re-render
- [ ] Desktop: hoàn toàn không thay đổi behavior

#### Task 2.2 — `#addOrIncrementItem()` mobile flow

Trên mobile, sau khi add/increment item qua ProductBrowser:
- Add mới: thêm vào state → **mở offcanvas ngay** với default qty=1, price=suggested
- Increment (đã có): cập nhật qty + **mở offcanvas** để user confirm hoặc adjust

Trên desktop: giữ nguyên flow hiện tại.

**Acceptance criteria:**
- [ ] Mobile: ProductBrowser tap → offcanvas mở với item mới
- [ ] Mobile: addProductModal không mở khi ProductBrowser được dùng
- [ ] Desktop: không thay đổi

#### Task 2.3 — Remove debounce friction trên mobile

Trên mobile, inline inputs không tồn tại nên debounce không cần thiết. State chỉ được cập nhật khi `onApply` được gọi từ offcanvas. Kết quả: **zero debounce delay** trên mobile.

---

### Phase 3: CreatePurchaseOrderController.js (P1)

#### Task 3.1 — Apply same mobile row pattern

`CreatePurchaseOrderController.js` có `#buildItemRow()` tương tự với `row-qty`/`row-price`. Apply cùng pattern:
- Mobile: simplified row + tap-to-edit via offcanvas
- Desktop: giữ nguyên

**Khác biệt so với Order:**
- PO dùng "đơn giá nhập" (unit cost) thay vì "đơn giá bán"
- Label trong offcanvas cần parameterize: truyền `priceLabel` vào `ItemEditOffcanvas`

**Acceptance criteria:**
- [ ] Same mobile UX như Order/Create
- [ ] Label "Đơn giá nhập" đúng
- [ ] View PurchaseOrder/Create include `_ItemEditOffcanvas.cshtml`

---

### Phase 4: ProductBrowser Position on Mobile (P2 — Optional)

Vấn đề: ProductBrowser (`col-md-4`) nằm dưới main form trên mobile → phải scroll.

**Option A (Simple):** Thêm fixed bottom bar trên mobile với nút "Duyệt hàng hóa" → mở ProductBrowser trong offcanvas.
**Option B (Current):** Đặt ProductBrowser trong offcanvas riêng kích hoạt bằng FAB button.

Recommendation: **Option A** — ít thay đổi hơn, tận dụng `ProductBrowser` hiện có.

**Acceptance criteria:**
- [ ] Trên mobile: ProductBrowser accessible không cần scroll
- [ ] Trên desktop: layout hiện tại giữ nguyên

---

## Build Order

```
Task 1.1 (ItemEditOffcanvas.js module)
  → Task 1.2 (HTML partial)
    → Task 2.1 (OrderController mobile rows)
      → Task 2.2 (add item mobile flow)
        → Task 2.3 (remove debounce on mobile)
          → Task 3.1 (PurchaseOrderController)
            → [optional] Task 4 (ProductBrowser position)
```

Tasks 1.1 và 1.2 có thể làm song song.

---

## Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| Bootstrap offcanvas keyboard behavior khác nhau trên iOS/Android | Medium | Test thực tế trên device; fallback: dùng `scrollIntoView` khi input focused |
| Responsive breakpoint detection via `window.innerWidth` không đủ (resize) | Low | Chỉ detect khi `#buildItemRow()` chạy; không cần reactivity |
| Hidden inputs bị jQuery validation bỏ qua | Low | Set `data-val="true"` như input visible, hoặc skip validation cho hidden qty/price nếu form validation không cần |
| `sweetalert2` confirm trên delete — Dependency đã có | None | Đã có trong project |

---

## Tổng quan phạm vi

| Phase | Tasks | Files | Scope | Priority |
|-------|-------|-------|-------|---------|
| 1. Shared module | 2 | 2 mới | M | P0 |
| 2. OrderController | 3 | 2 sửa | M | P0 |
| 3. PO Controller | 1 | 2 sửa | S | P1 |
| 4. Browser position | 1 | 2 sửa | S | P2 |

**Không cần thay đổi backend.** Toàn bộ là frontend-only.

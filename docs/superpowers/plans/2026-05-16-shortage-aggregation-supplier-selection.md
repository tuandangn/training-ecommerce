# Supplier Selection on "Hàng cần nhập" Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Cho phép người dùng đổi nhà cung cấp cho từng mặt hàng trên trang "Hàng cần nhập" khi sản phẩm có nhiều NCC; dòng mặt hàng được di chuyển sang card NCC đã chọn và đơn giá tự cập nhật theo NCC mới.

**Architecture:** Thay đổi UI thuần trên một file Razor view (`ShortageAggregation.cshtml`) — markup + inline IIFE script. Dữ liệu `SupplierSuggestions` đã có sẵn trên mỗi item. Mọi logic phía sau (payload tạo PO, modal đơn liên quan, tính tổng) đọc NCC từ card chứa dòng, nên chỉ cần di chuyển DOM của dòng sang card đích là đủ. Không sửa C#, DTO, hay migration.

**Tech Stack:** ASP.NET Core Razor (`.cshtml`), vanilla JavaScript (IIFE), Bootstrap 5.

> **Project rules (CLAUDE.md):** KHÔNG viết unit test mới, KHÔNG sửa project `*.Test`, AI KHÔNG tự chạy migration. Verification = `dotnet build` + manual walkthrough. Comment chỉ khi thật sự cần (giải thích WHY).

---

## File Structure

Tất cả thay đổi nằm trong **một file**:

- Modify: `NamEcommerce/Presentation/NamEcommerce.Web/Views/PurchaseOrder/ShortageAggregation.cshtml`
  - Markup: thêm `data-suggestions` lên dòng `.shortage-order-item`, thêm `<select class="item-vendor">` trong `.shortage-item-inputs`, thêm `<template id="vendorCardTemplate">`.
  - Script (IIFE): helper đọc gợi ý, hàm tìm/tạo card NCC, handler `change` cho `.item-vendor`, wiring.

Không tạo file mới. Không file nào khác bị ảnh hưởng (đã xác minh: payload `CreateFromShortage`, modal, totals đều đọc NCC từ `.shortage-group` chứa dòng).

---

## Task 1: Markup — `data-suggestions` trên dòng + selector NCC

**Files:**
- Modify: `NamEcommerce/Presentation/NamEcommerce.Web/Views/PurchaseOrder/ShortageAggregation.cshtml` (vùng dòng mặt hàng, hiện ~ dòng 194-266)

- [ ] **Step 1: Thêm `data-suggestions` vào phần tử `.shortage-order-item`**

Tìm khối mở thẻ dòng mặt hàng (hiện tại):

```html
                        <div class="shortage-order-item @(item.IsFromPrimaryOrder ? "" : "shortage-item-secondary")"
                             data-order-item-id="@item.OrderItemId"
                             data-order-code="@item.OrderCode"
                             data-product-id="@item.ProductId"
                             data-product-name="@item.ProductName"
                             data-unit-name="@unitName"
                             data-is-primary="@(item.IsFromPrimaryOrder ? "true" : "false")">
```

Ngay trước dòng `<div class="shortage-order-item ...` đó, thêm biến dựng JSON gợi ý (đặt cùng khối `@{ ... }` ngay trên, nơi đã khai báo `unitName`, `currentSuggestion`, `lastPrice`, `lastPurchaseText`, `itemTotal`):

```csharp
                        var suggestionsJson = System.Text.Json.JsonSerializer.Serialize(
                            item.SupplierSuggestions.Select(s => new
                            {
                                vendorId = s.VendorId,
                                vendorName = s.VendorName,
                                lastUnitPrice = s.LastUnitPrice,
                                lastPurchaseDate = s.LastPurchaseDateUtc.HasValue
                                    ? s.LastPurchaseDateUtc.Value.ToString("dd/MM")
                                    : null
                            }));
```

Rồi thêm thuộc tính `data-suggestions` vào thẻ dòng (thêm một dòng attribute, giữ nguyên các attribute cũ):

```html
                        <div class="shortage-order-item @(item.IsFromPrimaryOrder ? "" : "shortage-item-secondary")"
                             data-order-item-id="@item.OrderItemId"
                             data-order-code="@item.OrderCode"
                             data-product-id="@item.ProductId"
                             data-product-name="@item.ProductName"
                             data-unit-name="@unitName"
                             data-is-primary="@(item.IsFromPrimaryOrder ? "true" : "false")"
                             data-suggestions="@suggestionsJson">
```

- [ ] **Step 2: Thêm `<select class="item-vendor">` vào `.shortage-item-inputs`**

Tìm khối input SL/Giá hiện tại:

```html
                                <div class="shortage-item-inputs">
                                    <label>
                                        <span>SL:</span>
                                        <input type="number"
                                               class="form-control quantity-to-order"
                                               min="0"
                                               step="any"
                                               value="@item.QuantityToOrder.ToString("0.##", CultureInfo.InvariantCulture)" />
                                        <span>@unitName</span>
                                    </label>
                                    <span class="shortage-input-separator">×</span>
                                    <label>
                                        <span>Giá:</span>
                                        <input type="number"
                                               class="form-control unit-cost"
                                               min="0"
                                               step="any"
                                               value="@item.UnitCost.ToString("0.##", CultureInfo.InvariantCulture)" />
                                        <span>đ</span>
                                    </label>
                                </div>
```

Thêm khối selector NCC ngay sau `</label>` cuối (trước `</div>` đóng `.shortage-item-inputs`), chỉ render khi có >1 gợi ý:

```html
                                    @if (item.SupplierSuggestions.Count > 1)
                                    {
                                        <label class="shortage-item-vendor-field">
                                            <span>NCC:</span>
                                            <select class="form-select form-select-sm item-vendor">
                                                @foreach (var suggestion in item.SupplierSuggestions)
                                                {
                                                    <option value="@suggestion.VendorId"
                                                            selected="@(suggestion.VendorId == group.VendorId)">
                                                        @suggestion.VendorName
                                                    </option>
                                                }
                                            </select>
                                        </label>
                                    }
```

- [ ] **Step 3: Build verify**

Run: `dotnet build NamEcommerce/Presentation/NamEcommerce.Web/NamEcommerce.Web.csproj`
Expected: Build succeeded (0 errors). Nếu `JsonSerializer` báo lỗi thiếu namespace, dùng tên đầy đủ `System.Text.Json.JsonSerializer` như trong Step 1 (đã dùng FQN nên không cần `@using`).

- [ ] **Step 4: Commit**

```bash
git add NamEcommerce/Presentation/NamEcommerce.Web/Views/PurchaseOrder/ShortageAggregation.cshtml
git commit -m "feat: render per-item vendor selector on shortage page"
```

---

## Task 2: Markup — template card NCC rỗng

**Files:**
- Modify: `NamEcommerce/Presentation/NamEcommerce.Web/Views/PurchaseOrder/ShortageAggregation.cshtml` (sau `</div>` đóng `.shortage-order-page`, hiện ~ dòng 299; trước `<div class="modal fade" id="existingDraftModal">`)

- [ ] **Step 1: Thêm `<template id="vendorCardTemplate">`**

Khung này là bản rút gọn của card NCC tĩnh (`.shortage-order-card.shortage-group`) nhưng không có item, dùng JS để clone khi NCC đích chưa có card. Placeholder `__VENDOR_NAME__` sẽ được JS thay. Thêm ngay sau thẻ `</div>` đóng `<div class="shortage-order-page">`:

```html
<template id="vendorCardTemplate">
    <div class="shortage-order-card shortage-group" data-vendor-id="" data-vendor-name="">
        <div class="shortage-order-card-header">
            <div class="shortage-vendor-heading">
                <input type="checkbox" class="form-check-input group-check" />
                <span class="shortage-vendor-icon"><i class="bi bi-building"></i></span>
                <div>
                    <div class="shortage-vendor-name">__VENDOR_NAME__</div>
                    <div class="shortage-vendor-meta"></div>
                </div>
            </div>
            <div class="shortage-group-money">
                <strong class="group-total">0đ</strong>
                <span>tạm tính</span>
            </div>
        </div>
        <div class="shortage-order-card-controls">
            <label class="shortage-inline-field">
                <span><i class="bi bi-calendar2-week"></i>Hẹn nhận:</span>
                <input type="date" class="form-control expected-date" value="@defaultExpectedDate" />
            </label>
            <label class="shortage-inline-field shortage-note-field">
                <span><i class="bi bi-stickies"></i>Ghi chú:</span>
                <input type="text" class="form-control group-note" maxlength="1000" />
            </label>
        </div>
        <div class="shortage-order-items"></div>
    </div>
</template>
```

- [ ] **Step 2: Build verify**

Run: `dotnet build NamEcommerce/Presentation/NamEcommerce.Web/NamEcommerce.Web.csproj`
Expected: Build succeeded (0 errors). `@defaultExpectedDate` đã khai báo sẵn ở đầu view (dòng ~14) nên hợp lệ.

- [ ] **Step 3: Commit**

```bash
git add NamEcommerce/Presentation/NamEcommerce.Web/Views/PurchaseOrder/ShortageAggregation.cshtml
git commit -m "feat: add empty vendor card template for dynamic regrouping"
```

---

## Task 3: Script — helper tìm/tạo card NCC

**Files:**
- Modify: `NamEcommerce/Presentation/NamEcommerce.Web/Views/PurchaseOrder/ShortageAggregation.cshtml` (trong IIFE `@section Scripts`, sau khối khai báo `const ... = document.getElementById(...)` và trước `function parseDecimal`, hiện ~ dòng 336)

- [ ] **Step 1: Thêm tham chiếu template + hàm wiring group + hàm tìm/tạo card**

`groups` hiện được khai báo là `const groups = Array.from(...)`. Đổi sang `let` để có thể thêm card mới, và thêm các helper. Tìm dòng:

```javascript
            const groups = Array.from(document.querySelectorAll('.shortage-group'));
```

Đổi thành:

```javascript
            let groups = Array.from(document.querySelectorAll('.shortage-group'));
            const vendorCardTemplate = document.getElementById('vendorCardTemplate');
            const shortagePage = document.querySelector('.shortage-order-page');
```

Sau đó, ngay trước `function parseDecimal(value) {`, thêm hai hàm:

```javascript
            function wireGroup(group) {
                group.querySelector('.group-check')?.addEventListener('change', event => {
                    group.querySelectorAll('.item-check').forEach(check => {
                        check.checked = event.target.checked;
                    });
                    refreshTotals();
                });
                group.querySelectorAll('.item-check').forEach(check => {
                    check.addEventListener('change', refreshTotals);
                });
            }

            function findOrCreateVendorGroup(vendorId, vendorName) {
                const existing = groups.find(group => equalsId(group.dataset.vendorId, vendorId));
                if (existing) return existing;

                const fragment = vendorCardTemplate.content.cloneNode(true);
                const card = fragment.querySelector('.shortage-group');
                card.dataset.vendorId = vendorId;
                card.dataset.vendorName = vendorName;
                const nameEl = card.querySelector('.shortage-vendor-name');
                if (nameEl) nameEl.textContent = vendorName;

                const createBar = shortagePage.querySelector('.shortage-create-bar');
                shortagePage.insertBefore(card, createBar);

                groups.push(card);
                wireGroup(card);

                if (vendorFilter && !Array.from(vendorFilter.options).some(option => equalsId(option.value, vendorId))) {
                    const option = document.createElement('option');
                    option.value = vendorId;
                    option.textContent = vendorName;
                    vendorFilter.appendChild(option);
                }

                return card;
            }
```

> WHY `equalsId`: vendor id render từ Razor là GUID có thể khác hoa/thường so với giá trị `<option>`; `equalsId` đã có sẵn trong IIFE để so sánh không phân biệt hoa thường.

- [ ] **Step 2: Build verify**

Run: `dotnet build NamEcommerce/Presentation/NamEcommerce.Web/NamEcommerce.Web.csproj`
Expected: Build succeeded (0 errors). (View compile; lỗi cú pháp JS không chặn build nhưng vẫn kiểm tra nhanh trang load được ở Task 5.)

- [ ] **Step 3: Commit**

```bash
git add NamEcommerce/Presentation/NamEcommerce.Web/Views/PurchaseOrder/ShortageAggregation.cshtml
git commit -m "feat: add find-or-create vendor card helpers"
```

---

## Task 4: Script — handler đổi NCC + wiring

**Files:**
- Modify: `NamEcommerce/Presentation/NamEcommerce.Web/Views/PurchaseOrder/ShortageAggregation.cshtml` (trong IIFE: thêm handler trước phần wiring `groups.forEach(...)`, và sửa khối `groups.forEach` thành dùng `wireGroup`)

- [ ] **Step 1: Thêm hàm `handleItemVendorChange`**

Thêm ngay sau hàm `findOrCreateVendorGroup` (kết thúc ở Task 3 Step 1):

```javascript
            function handleItemVendorChange(select) {
                const row = select.closest('.shortage-order-item');
                if (!row) return;

                const sourceGroup = row.closest('.shortage-group');
                let suggestions = [];
                try {
                    suggestions = JSON.parse(row.dataset.suggestions || '[]');
                } catch {
                    suggestions = [];
                }

                const vendorId = select.value;
                const suggestion = suggestions.find(s => equalsId(s.vendorId, vendorId));
                if (!suggestion) return;

                const targetGroup = findOrCreateVendorGroup(vendorId, suggestion.vendorName);
                if (targetGroup === sourceGroup) return;

                targetGroup.querySelector('.shortage-order-items').appendChild(row);

                if (suggestion.lastUnitPrice != null) {
                    const unitCostInput = row.querySelector('.unit-cost');
                    if (unitCostInput) unitCostInput.value = formatInputNumber(Number(suggestion.lastUnitPrice));
                }

                const metaEl = row.querySelector('.shortage-item-meta');
                if (metaEl) {
                    const priceText = suggestion.lastUnitPrice != null
                        ? ` · Giá lần trước ${formatMoney(Number(suggestion.lastUnitPrice))}` +
                          (suggestion.lastPurchaseDate ? ` (${suggestion.lastPurchaseDate})` : '')
                        : '';
                    const unitName = row.dataset.unitName || 'sp';
                    metaEl.innerHTML = metaEl.innerHTML.replace(/ · Giá lần trước[^<]*/, '') + escapeHtml(priceText).replace(/&middot;/g, '·');
                }

                if (sourceGroup) syncGroupCheck(sourceGroup);
                syncGroupCheck(targetGroup);
                refreshTotals();
                applyFilters();
            }
```

> WHY regex replace trên `.shortage-item-meta`: text "Giá lần trước" được Razor render thẳng trong meta; thay theo NCC mới mà không phá phần "Tồn / Cần thêm" phía trước. Nếu NCC mới không có giá, phần giá bị bỏ.

- [ ] **Step 2: Refactor wiring `groups.forEach` dùng `wireGroup` + bind selector NCC**

Tìm khối wiring hiện tại:

```javascript
            groups.forEach(group => {
                group.querySelector('.group-check')?.addEventListener('change', event => {
                    group.querySelectorAll('.item-check').forEach(check => {
                        check.checked = event.target.checked;
                    });
                    refreshTotals();
                });

                group.querySelectorAll('.item-check').forEach(check => {
                    check.addEventListener('change', refreshTotals);
                });
            });
```

Thay bằng:

```javascript
            groups.forEach(wireGroup);

            document.querySelectorAll('.item-vendor').forEach(select => {
                select.addEventListener('change', () => handleItemVendorChange(select));
            });
```

- [ ] **Step 3: Build verify**

Run: `dotnet build NamEcommerce/Presentation/NamEcommerce.Web/NamEcommerce.Web.csproj`
Expected: Build succeeded (0 errors).

- [ ] **Step 4: Commit**

```bash
git add NamEcommerce/Presentation/NamEcommerce.Web/Views/PurchaseOrder/ShortageAggregation.cshtml
git commit -m "feat: move shortage item to chosen vendor card on selector change"
```

---

## Task 5: Manual verification

**Files:** không sửa file. Chạy app và kiểm tra theo checklist.

- [ ] **Step 1: Build toàn giải pháp**

Run: `dotnet build NamEcommerce/Presentation/NamEcommerce.Web/NamEcommerce.Web.csproj`
Expected: Build succeeded, 0 errors.

- [ ] **Step 2: Chạy app, mở trang "Hàng cần nhập"**

Run app (Tuấn chạy local), điều hướng: Trang chủ → Đơn nhập → Hàng cần nhập (`/PurchaseOrders/ShortageAggregation`). Cần dữ liệu có ít nhất 1 sản phẩm thiếu thuộc sản phẩm có ≥2 NCC (NCC ưu tiên hoặc lịch sử mua).

- [ ] **Step 3: Checklist hành vi**

Xác nhận từng mục:
- [ ] Mặt hàng có >1 NCC hiển thị dropdown "NCC:" trong dòng; mặt hàng chỉ 1 NCC KHÔNG có dropdown (giữ nguyên).
- [ ] Dropdown mặc định chọn đúng NCC của card đang chứa dòng.
- [ ] Đổi sang NCC khác **đã có card**: dòng di chuyển sang card đó; tổng tiền card nguồn & card đích cập nhật; tổng chung dưới chân trang đúng.
- [ ] Đổi sang NCC **chưa có card**: card mới được tạo với tên NCC đúng, có ô "Hẹn nhận" (mặc định +7 ngày) và "Ghi chú"; dòng nằm trong card mới; option NCC mới xuất hiện trong bộ lọc "NCC".
- [ ] Đơn giá ô "Giá" tự đổi theo `LastUnitPrice` của NCC mới; nếu NCC mới không có giá lần trước → giữ nguyên giá cũ.
- [ ] Text "Giá lần trước (dd/MM)" trong dòng đổi theo NCC mới; phần "Tồn / Cần thêm" không bị hỏng.
- [ ] Di chuyển dòng cuối khỏi một card phía server → card đó ẩn (qua `applyFilters`).
- [ ] Checkbox nhóm (`group-check`) của card nguồn & đích đồng bộ trạng thái sau khi di chuyển.
- [ ] Bấm "Tạo phiếu nhập": phiếu được tạo dưới đúng NCC đã chọn cho từng mặt hàng. Modal "Đơn nhập liên quan" (nếu hiện) gom theo đúng NCC mới.
- [ ] Bộ lọc "NCC" và "Mặt hàng" vẫn hoạt động sau khi đổi NCC.

- [ ] **Step 4: Commit (nếu có chỉnh sửa nhỏ phát sinh từ kiểm thử)**

Nếu phát hiện lỗi nhỏ, sửa trong cùng file, build lại, rồi:

```bash
git add NamEcommerce/Presentation/NamEcommerce.Web/Views/PurchaseOrder/ShortageAggregation.cshtml
git commit -m "fix: address manual verification findings for vendor selector"
```

---

## Self-Review

**Spec coverage:**
- "Render selector chỉ khi >1 gợi ý" → Task 1 Step 2 (`@if (item.SupplierSuggestions.Count > 1)`).
- "data-suggestions JSON trên dòng" → Task 1 Step 1.
- "Template card NCC rỗng" → Task 2.
- "Tìm/tạo card NCC + đăng ký groups + thêm option filter" → Task 3 (`findOrCreateVendorGroup`).
- "Di chuyển DOM dòng sang card đích" → Task 4 Step 1.
- "Tự cập nhật đơn giá theo NCC mới; giữ nguyên nếu không có giá" → Task 4 Step 1 (`if (suggestion.lastUnitPrice != null)`).
- "Cập nhật meta giá lần trước" → Task 4 Step 1.
- "syncGroupCheck nguồn+đích, refreshTotals, applyFilters; ẩn card rỗng" → Task 4 Step 1 + `applyFilters` (cơ chế ẩn card rỗng có sẵn).
- "Không sửa server/DTO/migration; không unit test mới" → toàn plan chỉ chạm 1 file view; verification là build + manual.
- Edge "Chưa có nhà cung cấp" không có selector → đảm bảo bởi điều kiện `Count > 1` (nhóm no-vendor item không có suggestions).
- Modal/payload không cần sửa → xác nhận trong Architecture; Task 5 checklist kiểm chứng.

**Placeholder scan:** Không có TBD/TODO; mọi step có code hoặc lệnh cụ thể.

**Type/name consistency:** `wireGroup`, `findOrCreateVendorGroup`, `handleItemVendorChange`, `equalsId`, `formatInputNumber`, `formatMoney`, `escapeHtml`, `syncGroupCheck`, `refreshTotals`, `applyFilters`, `vendorFilter`, `groups`, `shortagePage`, `vendorCardTemplate` — tên dùng nhất quán giữa Task 3 và 4; các hàm `equalsId/formatInputNumber/formatMoney/escapeHtml/syncGroupCheck/refreshTotals/applyFilters` đã tồn tại trong IIFE hiện tại (đã xác minh khi đọc file). `groups` đổi từ `const`→`let` ở Task 3 trước khi `.push()` ở Task 3/được dùng ở Task 4.

Không phát hiện gap; không cần thêm task.

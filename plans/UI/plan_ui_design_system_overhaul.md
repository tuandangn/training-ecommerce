# UI/UX Design System Overhaul Plan

Cải thiện UI/UX một cách HỆ THỐNG thay vì vá từng màn. Plan này trả lời câu hỏi: vì sao đã yêu cầu sửa nhiều lần mà không cải thiện đáng kể — và chặn đứng vòng lặp đó.

## 1. Chẩn đoán — vì sao sửa mãi không khá lên (số liệu từ code 2026-06-10)

### 1.1 🔴 DESIGN.md viết cho Tailwind — app chạy Bootstrap
`DESIGN.md` quy định token theo Tailwind (`indigo-600`, `rounded-xl`, `text-3xl font-bold tracking-tight`...) nhưng `_Styles.cshtml` load **Bootstrap 5 + bootstrap-icons**. Mọi chỉ dẫn "tuân thủ DESIGN.md" đều KHÔNG thực thi được trực tiếp: người sửa (cả AI lẫn người) phải tự "phiên dịch" Tailwind → CSS tay → mỗi lần dịch một kiểu → càng sửa càng phân mảnh. **Đây là nguyên nhân gốc số 1.**

### 1.2 🔴 CSS phân mảnh nghiêm trọng
- `site.css`: 3.449 dòng
- **2.687 dòng CSS nằm rải trong 32 view** (`<style>` blocks) — gần bằng cả site.css
- **325 inline `style="..."`** trong views
→ Sửa đẹp màn A không tự lan sang màn B vì màn B có CSS riêng của nó. "Không cải thiện đáng kể" là hệ quả tất yếu về mặt cấu trúc, không phải do sửa chưa đủ nhiều.

### 1.3 Không có ngôn ngữ component chung
Nút bấm dùng 12+ biến thể (`btn-light` 52, `btn-link` 40, `btn-outline-secondary` 63, `btn-icon`/`btn-xs` tự chế...) — không có quy tắc phân cấp hành động (primary/secondary/danger, mỗi màn 1 CTA). Shared partials mới có ~10 cái (`_Pager`, `_ResponsiveListToolbar`, EditorTemplates) trên 158 views — phần lớn màn hình tự dựng layout từ đầu.

### 1.4 Người sửa UI không nhìn thấy UI
Các lần sửa trước (đặc biệt khi giao cho AI) là **sửa mù**: đổi markup/CSS rồi build xong là hết — không chụp màn hình, không so trước/sau, không checklist nghiệm thu. Không có vòng phản hồi thị giác thì chất lượng thị giác không thể hội tụ.

> Kết luận: vấn đề không phải "chưa sửa đủ" mà là **thiếu nền tảng (1.1), thiếu cấu trúc (1.2-1.3), thiếu vòng phản hồi (1.4)**. Plan này xử lý cả 4 theo thứ tự đó.

## 2. Quyết định nền tảng (cần anh duyệt trước tiên)

**Chọn Bootstrap-first, KHÔNG migrate Tailwind.** Lý do: 158 views Razor + đội hình hiện tại đã quen Bootstrap; giá trị của DESIGN.md nằm ở *token* (màu, font, bo góc, bóng) — những thứ map được 100% vào Bootstrap 5 CSS variables. Migrate Tailwind = đập 158 views, rủi ro cao, không tăng giá trị cho người dùng cuối.

Cách map: 1 file `theme.css` override Bootstrap tokens:
```css
:root {
  --bs-primary: #4F46E5;          /* indigo-600 từ DESIGN.md */
  --bs-body-bg: #F8FAFC;          /* slate-50 */
  --bs-body-color: #475569;       /* slate-600 */
  --bs-emphasis-color: #0F172A;   /* slate-900 */
  --bs-border-color: #E2E8F0;     /* slate-200 */
  --bs-border-radius: .5rem; --bs-border-radius-lg: .75rem;
  --bs-body-font-family: 'Inter', system-ui, sans-serif;
  /* + success/danger/info, shadows 2 cấp */
}
```
→ Toàn bộ nút/form/card/table Bootstrap đổi diện mạo theo DESIGN.md **ngay lập tức, trên cả 158 màn**, không sửa markup. Đây là cú "cải thiện đáng kể" đầu tiên nhìn thấy được trong 1-2 ngày.

## Assumptions / Quyết định mặc định

- Bootstrap-first như trên; DESIGN.md viết lại thành phiên bản Bootstrap (giữ nguyên hệ màu/triết lý, đổi cách diễn đạt sang `--bs-*` + class chuẩn + component nội bộ).
- Không redesign nghiệp vụ màn hình trong plan này (không đổi flow) — chỉ chuẩn hoá thị giác + bố cục controls. Redesign flow từng màn (nếu cần) là việc riêng sau khi có nền.
- Mobile delivery PWA đã ổn (có screenshot polished trong repo) — không đụng.
- Ưu tiên theo tần suất sử dụng thực tế của cửa hàng: **FastSale/Order → Công nợ → DeliveryNote → Tồn kho/Nhập hàng** (anh chỉnh nếu khác).

## Success Criteria

- 0 dòng `<style>` mới và 0 `style=""` mới được merge sau khi Phase 4 xong (có cơ chế chặn tự động).
- 32 view có `<style>` giảm về 0; 325 inline style giảm ≥ 90% (cho phép ngoại lệ có ghi chú).
- Mọi màn list/form dùng chung bộ component: PageHeader, FilterToolbar, DataTable, StatusBadge, FormSection, EmptyState, ConfirmModal — đếm được bằng grep.
- Có trang `/design` nội bộ render toàn bộ component làm chuẩn đối chiếu.
- Mỗi PR UI có screenshot trước/sau; agent sửa UI bắt buộc tự chụp + tự xem màn hình trước khi báo xong.

---

## Phase 0 — Theme nền (1-2 ngày, hiệu ứng toàn cục ngay)

- `wwwroot/css/theme.css`: map token DESIGN.md → Bootstrap variables (mẫu ở mục 2) + font Inter (self-host woff2, không CDN) + shadow/border-radius 2 cấp.
- Tách responsive CSS thành các file riêng theo Bootstrap breakpoint boundaries trong `wwwroot/css/responsive/`: `sm.css` (`576px`), `md.css` (`768px`), `lg.css` (`992px`), `xl.css` (`1200px`), `xxl.css` (`1400px`). Mỗi file có thể chứa rule `min-width` hoặc `max-width` quanh boundary đó khi cần. Chỉ đặt responsive rule dùng chung ở đây; responsive riêng của module vẫn đi theo `pages/{module}.css` nếu không tái sử dụng.
- Load sau bootstrap.min.css trong `_Styles.cshtml`.
- Viết lại `DESIGN.md` → phiên bản Bootstrap: hệ màu (giữ nguyên hex), quy tắc nút (xem Phase 1), spacing theo Bootstrap utilities (`p-2/3/4`, `gap-2/3`), cấm hex màu mới ngoài token.
- Chụp 6 màn chính trước/sau làm baseline.

### TodoList 0
- [x] theme.css + font + load order
- [x] Responsive CSS tách theo Bootstrap breakpoints (`sm/md/lg/xl/xxl`)
- [x] DESIGN.md viết lại cho Bootstrap
- [x] Screenshot baseline 6 màn (FastSale, Order list/details, CustomerDebt, DeliveryNote details, Inventory list)

## Phase 1 — Bộ component nội bộ + trang /design

Partials/ViewComponents mới trong `Views/Shared/Components/` (tận dụng pattern `_Pager`, `_ResponsiveListToolbar` sẵn có):

| Component | Thay cho | Quy tắc |
|---|---|---|
| `_PageHeader` | mỗi màn tự dựng h1 + nút | Title + breadcrumb + vùng actions (tối đa 1 `btn-primary`) |
| `_FilterToolbar` | form filter mỗi nơi một kiểu | Search + dropdowns + nút Lọc/Xoá lọc, responsive sẵn |
| `_DataTable` (+ partial row slot) | 158 bảng tự chế | Header sticky, hover, empty state tích hợp, cột số căn phải `text-end`, tiền dùng format chung |
| `_StatusBadge` | badge màu tuỳ hứng | Map enum→màu đặt 1 chỗ (OrderStatus, DebtStatus, DN Status...) |
| `_FormSection` / `_FormRow` | form layout lệch nhau | Label trên input, helptext, validation chỗ thống nhất |
| `_EmptyState` | bảng trống trơ trọi | Icon + message + CTA |
| `_ConfirmModal` | modal confirm trùng lặp (`_ReusedModals` mở rộng) | 1 modal dùng chung qua data-attributes |
| `_MoneyDisplay`/`_QuantityDisplay` | format tiền/số lượng mỗi nơi một kiểu | Mở rộng EditorTemplates Currency/Quantity sẵn có |

- **Quy tắc nút (ghi vào DESIGN.md):** mỗi màn tối đa 1 `btn-primary` (hành động chính); phụ = `btn-outline-secondary`; nguy hiểm = `btn-outline-danger` (solid chỉ trong modal confirm); bỏ hẳn `btn-light`, `btn-link` cho action (link chỉ để điều hướng); icon-only phải có `title`.
- Trang `/design` (controller DesignController, env Development only): render mọi component + bảng màu + typography — vừa là tài liệu sống, vừa là nơi agent đối chiếu.

### TodoList 1
- [x] 8 components + CSS module riêng (`components.css`)
- [x] Trang /design
- [x] Quy tắc nút + component vào DESIGN.md

## Phase 2 — Migrate màn hình ưu tiên (cuốn chiếu, mỗi đợt 1 PR)

Đợt 1: **FastSale + Order (list/details/create)** — màn dùng nhiều nhất.
Đợt 2: **CustomerDebt/VendorDebt** (phối hợp: nếu plan `plans/Debts/plan_account_based_debt_ledger.md` Phase 3 làm UI mới, thì màn công nợ mới build thẳng bằng component — không migrate màn cũ 2 lần).
Đợt 3: **DeliveryNote + GoodsReceipt**.
Đợt 4: **Inventory + PurchaseOrder + Returns**.
Đợt 5: phần còn lại (Catalog, Users, Settings...).

Mỗi màn khi migrate: thay layout tự chế bằng components; gỡ `<style>` block (CSS thực sự cần giữ → chuyển vào `components.css`, `pages/{module}.css`, hoặc `wwwroot/css/responsive/{breakpoint}.css` nếu là responsive rule dùng chung theo Bootstrap breakpoint); gỡ inline style; rà bố cục controls theo checklist Phase 3; chụp trước/sau đính vào PR.

### TodoList 2
- [x] Slice 1: Order List CSS/action cleanup, giảm baseline `<style>`/`style=""`
- [x] Slice 2: Order QuickCreate CSS/action cleanup, giảm baseline `<style>`
- [x] Slice 3: Order Create CSS/action cleanup, giảm baseline `style=""`
- [x] Slice 4: Order Details/workflow/offcanvas CSS/action cleanup, giảm baseline `style=""`
- [x] Đợt 1: FastSale + Order list/details/create (migrate + screenshot + smoke test tay)
- [x] Đợt 2: CustomerDebt/VendorDebt list/details (migrate + screenshot + smoke test tay)
- [x] Đợt 3: DeliveryNote/GoodsReceipt list/details/create (migrate + screenshot + smoke test tay)
- [x] Đợt 4: Inventory/PurchaseOrder/Returns (migrate + screenshot + smoke test tay)
- [x] Đợt 5: phần còn lại (static cleanup + screenshot + smoke test tay)
- [x] Đếm lại metric: `<style>` views còn 0, inline style còn 0

## Phase 3 — Checklist UX bố cục controls (áp khi migrate từng màn)

Checklist nghiệm thu per màn (đưa vào DESIGN.md, reviewer + agent đối chiếu từng mục):
1. Hành động chính nằm góc phải PageHeader, duy nhất, màu primary.
2. Hành động trên dòng bảng: tối đa 3 icon + dropdown "⋯" cho phần còn lại.
3. Form: label trên input; nhóm field liên quan bằng FormSection; nút Lưu/Huỷ cố định cuối form (sticky nếu form dài); Huỷ luôn bên trái Lưu.
4. Bảng: cột số/tiền căn phải + format nghìn; ngày giờ định dạng `dd/MM/yyyy HH:mm` thống nhất (theo quy ước DateTime layer hiện có); trạng thái dùng StatusBadge.
5. Mọi thao tác huỷ/xoá/đảo có ConfirmModal nêu rõ hậu quả.
6. Sau submit: toast thành công + redirect/refresh nhất quán (dùng NotificationCenter, không alert()).
7. Loading state cho nút submit (disable + spinner) — chống double-click (liên quan trực tiếp loạt bug double-approve ở plan Inventory).
8. Empty state có hướng dẫn hành động tiếp theo.
9. Responsive: bảng rộng có scroll ngang có chủ đích hoặc ẩn cột phụ ở `md↓`.

## Phase 4 — Vòng phản hồi + chống tái phát (điểm mấu chốt với AI agent)

- **Screenshot loop cho agent:** script Playwright (`tools/ui-screenshot/`) chạy được bằng 1 lệnh: login E2E user (tận dụng `E2ETestDataService`) → chụp danh sách URL → xuất PNG. Quy trình bắt buộc ghi vào `CLAUDE.md`: *agent sửa UI phải chạy script, TỰ XEM ảnh, đối chiếu checklist Phase 3 rồi mới báo xong; PR đính ảnh trước/sau.* (Đây là thứ chấm dứt "sửa mù".)
- **Chặn regression tự động:** script `tools/ui-lint.ps1` (chạy CI/pre-commit): fail nếu view chứa `<style` mới hoặc số `style="` tăng so với baseline được commit (`ui-lint-baseline.json` giảm dần theo Phase 2).
- **CLAUDE.md cập nhật:** mục Design trỏ tới DESIGN.md mới + quy tắc "chỉ dùng components Shared, cấm <style> trong view, cấm hex ngoài token, cấm inline style".

### TodoList 4
- [x] Playwright screenshot script + hướng dẫn 1 lệnh (requires local Playwright CLI)
- [x] ui-lint + baseline
- [x] Gắn ui-lint vào quy trình build/CI
- [x] AGENTS.md + DESIGN.md chốt quy trình

---

## Thứ tự & phối hợp

```
Phase 0 (1-2 ngày, thấy ngay khác biệt toàn hệ thống)
→ Phase 1 (components + /design)
→ Phase 4 (dựng vòng phản hồi TRƯỚC khi migrate hàng loạt)
→ Phase 2 đợt 1→5 (áp checklist Phase 3, có screenshot loop giám sát)
```
- Phối hợp plan Debts: màn công nợ mới (Debts Phase 3) build thẳng bằng component mới — xếp sau Phase 1 plan này.
- Không phụ thuộc plan Architecture/Inventory (chỉ chạm Presentation).

## Verification Plan

- Build Web project sau mỗi đợt: `dotnet build NamEcommerce/Presentation/NamEcommerce.Web/NamEcommerce.Web.csproj`
- Metric kiểm đếm sau mỗi đợt (grep count: `<style`, `style="`, biến thể btn, responsive rule còn nằm sai chỗ) — ghi vào implement doc
- So screenshot trước/sau từng đợt; anh duyệt trên ảnh, không duyệt trên mô tả
- Trang /design review tổng thể sau Phase 1

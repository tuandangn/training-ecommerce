[ROLE: Định nghĩa hệ màu, font, component rules, và guardrails UI cho NamEcommerce]

# NamEcommerce UI/UX Design System

Ứng dụng dùng Bootstrap 5 + Razor MVC. Tài liệu này là chuẩn Bootstrap-first: không dùng Tailwind class trong code mới, không tự dịch token Tailwind sang CSS tay từng màn.

## 1. Design Philosophy

NamEcommerce là dashboard vận hành bán hàng/kho/công nợ, nên giao diện phải yên tĩnh, rõ dữ liệu và thao tác nhanh:

- Sạch, ít trang trí, ưu tiên khả năng scan bảng/form.
- Mỗi màn có phân cấp hành động rõ: một hành động chính, các hành động phụ giảm độ nổi.
- Không đổi flow nghiệp vụ chỉ để làm đẹp. UI mới phải giúp thao tác lặp lại nhanh hơn.
- Mobile dùng bố cục thực dụng: ưu tiên đọc dữ liệu và bấm đúng, không nhồi cột.

## 2. Bootstrap Token Map

Token nằm trong `NamEcommerce/Presentation/NamEcommerce.Web/wwwroot/css/theme.css` và được load ngay sau `bootstrap.min.css`.

| Vai trò | Bootstrap/CSS variable | Hex | Dùng cho |
|---|---|---:|---|
| Primary | `--bs-primary` | `#4F46E5` | CTA chính, active state, link quan trọng |
| Accent/Info | `--bs-info`, `--app-accent` | `#0EA5E9` | Badge/hint/highlight phụ |
| App background | `--bs-body-bg` | `#F8FAFC` | Nền toàn trang |
| Surface | `--app-surface` | `#FFFFFF` | Card, table, form, modal |
| Text primary | `--bs-emphasis-color` | `#0F172A` | Heading, số liệu quan trọng |
| Text secondary | `--bs-body-color` | `#475569` | Body text, label, mô tả |
| Muted/Secondary action | `--bs-secondary` | `#64748B` | Text phụ, `btn-outline-secondary` |
| Border | `--bs-border-color` | `#E2E8F0` | Divider, input/card border |
| Success | `--bs-success` | `#10B981` | Trạng thái thành công |
| Warning | `--bs-warning` | `#D97706` | Cảnh báo cần chú ý |
| Danger | `--bs-danger` | `#EF4444` | Lỗi, xoá, huỷ nguy hiểm |

Không thêm hex màu mới trong view/page CSS. Nếu thiếu token, thêm vào `theme.css` trước.

## 3. Typography, Spacing, Radius

- Font stack: `Inter, "Segoe UI", system-ui, -apple-system, BlinkMacSystemFont, sans-serif`; Inter được self-host ở `wwwroot/fonts/inter/InterVariable.woff2`.
- H1/page title: 1.5-1.8rem, font-weight 700, màu `--bs-emphasis-color`.
- Section/card title: 1-1.15rem, font-weight 600.
- Body/form label: 0.875-1rem, màu `--bs-body-color`.
- Spacing dùng Bootstrap utilities: `p-2/3/4`, `gap-2/3`, `mb-2/3/4`.
- Radius mặc định: input/button 8px (`--bs-border-radius`), card/modal 12px (`--bs-border-radius-lg`) trừ khi component hiện có yêu cầu khác.
- Shadow chỉ dùng 2 cấp trong `theme.css`: `--app-shadow-soft`, `--app-shadow-lifted`.

## 4. CSS Ownership

- `theme.css`: Bootstrap variables, màu/font/radius/shadow toàn cục, override component Bootstrap chung.
- `components.css`: CSS cho partials dùng chung trong `Views/Shared/Components/`.
- `site.css`: layout/app shell và component cũ chưa migrate.
- `loading.css`: loading mask dùng chung.
- `responsive/sm.css`: rule dùng chung quanh Bootstrap `sm` boundary.
- `responsive/md.css`: rule dùng chung quanh Bootstrap `md` boundary.
- `responsive/lg.css`, `responsive/xl.css`, `responsive/xxl.css`: rule dùng chung cho breakpoint lớn khi phát sinh.
- `pages/{module}.css`: CSS riêng module, chỉ dùng khi không tái sử dụng được.

View Razor không được thêm `<style>` mới và không thêm `style=""` mới. Khi migrate màn cũ, chuyển CSS sang đúng file ở trên.

## 5. Component Rules

Các partial dùng chung nằm trong `NamEcommerce/Presentation/NamEcommerce.Web/Views/Shared/Components/` và nhận model từ `NamEcommerce.Web.Models.DesignSystem`.

- `_PageHeader`: title, subtitle, breadcrumb, actions.
- `_FilterToolbar`: search, select filters, Lọc/Xoá lọc, responsive sẵn.
- `_DataTable`: table wrapper, header style, hover, empty state.
- `_StatusBadge`: status badge theo tone `success/info/warning/danger/muted`.
- `_FormSection` / `_FormRow`: nhóm field form, label/help text/required marker.
- `_EmptyState`: icon, message, CTA.
- `_ConfirmModal`: modal confirm dùng Bootstrap data attributes.
- `_MoneyDisplay` / `_QuantityDisplay`: format số/tiền thống nhất.

- Page header: title + breadcrumb/context + vùng actions bên phải.
- Button: mỗi màn tối đa một `btn-primary`. Hành động phụ dùng `btn-outline-secondary`. Hành động nguy hiểm dùng `btn-outline-danger`; nút danger solid chỉ dùng trong confirm modal.
- Table: số/tiền căn phải, date thống nhất `dd/MM/yyyy HH:mm`, status dùng badge chuẩn.
- Form: label nằm trên input; nhóm field bằng FormSection; footer form đặt Huỷ bên trái, Lưu bên phải.
- Empty state: có message ngắn và hành động tiếp theo nếu có.
- Modal confirm: nêu rõ hậu quả của thao tác huỷ/xoá/đảo.
- Icon-only button phải có `title` hoặc `aria-label`.

## 6. Guardrails

- Không dùng Tailwind class trong code mới.
- Không dùng màu nguyên bản chói hoặc hex tự phát trong view.
- Không dùng shadow đậm, gradient trang trí, blob/orb nền.
- Không tạo card lồng card nếu có thể dùng section/full-width layout.
- Không dùng `btn-light`, `btn-link` cho action nghiệp vụ; link chỉ dùng điều hướng.
- Không import font/CDN mới trong view. Font self-host nếu cần thêm file font.

## 7. Verification

Mỗi PR UI phải có:

- Build Web project: `dotnet build NamEcommerce/Presentation/NamEcommerce.Web/NamEcommerce.Web.csproj`.
- UI lint: `powershell -ExecutionPolicy Bypass -File tools/ui-lint.ps1`.
- UI screenshot: `powershell -ExecutionPolicy Bypass -File tools/ui-screenshot.ps1 -Urls /design` (requires Playwright CLI: `npm install --save-dev @playwright/test && npx playwright install chromium`).
- Screenshot trước/sau cho màn đã sửa.
- Kiểm tra không tăng `<style>` và `style=""`.
- Kiểm tra responsive ở mobile và desktop cho màn liên quan.

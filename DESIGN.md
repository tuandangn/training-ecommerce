[ROLE: Định nghĩa Hệ màu, Font, và Linh hồn của Thương hiệu]

# Web App UI/UX Design System & Guidelines

Mục đích của tài liệu này là cung cấp các nguyên tắc thiết kế, hệ thống design token và quy chuẩn giao diện hiện đại (Modern Web App) để AI tuân thủ tuyệt đối khi xây dựng Components, Pages và Layouts.

---

## 1. Phong cách Thiết kế Chủ đạo (Design Philosophy)

AI cần áp dụng phong cách **Modern Minimalist / SaaS Dashboard** hiện đại với các đặc tính sau:
* **Sạch sẽ & Rộng rãi:** Sử dụng khoảng trắng (whitespace) hợp lý để giao diện "thở", không nhồi nhét thông tin.
* **Phẳng có chiều sâu (Semi-flat / Soft Shadows):** Sử dụng các lớp đổ bóng rất nhẹ (subtle shadows) và bo góc để tạo phân cấp trực quan (visual hierarchy).
* **Trọng tâm vào dữ liệu:** Giảm thiểu các chi tiết trang trí thừa thãi, làm nổi bật nội dung của người dùng.

---

## 2. Hệ màu sắc (Color Palette - Tailwind CSS Tokens)

Tất cả các thành phần giao diện phải sử dụng hệ màu nhất quán dưới đây (hoặc các class Tailwind tương đương):

| Quy định màu | Mã Màu (Hex) | Tailwind Class | Mục đích sử dụng |
| :--- | :--- | :--- | :--- |
| **Primary (Chủ đạo)** | `#4F46E5` | `indigo-600` | Các nút chính (CTA), trạng thái Active, Link quan trọng. |
| **Secondary** | `#0EA5E9` | `sky-500` | Các thành phần bổ trợ, badge, highlight nhẹ. |
| **Background App** | `#F8FAFC` | `slate-50` | Nền toàn trang (Light mode). |
| **Surface/Card** | `#FFFFFF` | `white` | Nền của các thẻ, bảng, form, sidebar. |
| **Text Primary** | `#0F172A` | `slate-900` | Tiêu đề chính, văn bản quan trọng. |
| **Text Secondary**| `#475569` | `slate-600` | Văn bản phụ, chú thích, label của form. |
| **Border / Divider**| `#E2E8F0` | `slate-200` | Đường kẻ phân chia, viền của input/card. |
| **Success** | `#10B981` | `emerald-500` | Trạng thái thành công, thông báo tích cực. |
| **Danger/Error** | `#EF4444` | `red-500` | Trạng thái lỗi, nút xóa, hành động nguy hiểm. |

---

## 3. Hệ thống Typography & Spacing

### Typography
* **Font chữ:** Ưu tiên hệ font Sans-serif hiện đại: `Inter`, `Plus Jakarta Sans`, hoặc hệ font hệ thống (`system-ui`).
* **Cấp bậc Font (Font Hierarchy):**
    * H1 (Trang chính): `text-3xl font-bold tracking-tight text-slate-900`
    * H2 (Tiêu đề phân khu): `text-xl font-semibold text-slate-900`
    * Body text: `text-sm font-normal text-slate-600 leading-relaxed`
    * Small text (Chú thích): `text-xs text-slate-400`

### Spacing & Border Radius (Quy tắc bo góc)
* **Padding/Margin:** Luôn tuân thủ hệ nhân 4 của Tailwind (`p-2`, `p-4`, `p-6`, `p-8`). Không dùng các khoảng cách lẻ.
* **Bo góc (Border Radius):**
    * Nút nhỏ, Input form: `rounded-lg` (8px)
    * Thẻ (Card), Bảng (Table), Dialog: `rounded-xl` (12px) hoặc `rounded-2xl` (16px) để tạo cảm giác mềm mại, hiện đại.

---

## 4. Quy chuẩn Components Hiện đại

AI khi tạo code cho các Component phải tuân theo các layout pattern sau:

### A. Layout Tổng thể (App Layout)
* Sử dụng cấu trúc **Sticky Sidebar** bên trái (hoặc Top Navigation cố định) kết hợp với vùng nội dung chính có `max-w-7xl` hoặc `w-full` kèm padding lớn (`p-6` hoặc `p-8`).
* Sidebar phải có hiệu ứng `hover:bg-slate-100` và trạng thái `active` rõ ràng với màu `text-indigo-600`.

### B. Thẻ Nội dung (Cards)
* **Khuyến nghị:** Cấu trúc nền trắng, bo góc `rounded-xl`, đổ bóng cực nhẹ `shadow-sm`, viền mờ `border border-slate-100`.
* *Code mẫu Tailwind định hướng:* `bg-white p-6 rounded-xl border border-slate-100 shadow-sm transition-all hover:shadow-md`

### C. Nút bấm (Buttons)
* **Primary Button:** Nền màu chủ đạo, chữ trắng, không đổ bóng đậm. Hiệu ứng hover giảm độ sáng nhẹ (`hover:bg-indigo-700`).
* **Secondary/Ghost Button:** Nền trong suốt hoặc xám nhạt (`bg-slate-50`), viền `border-slate-200`, `text-slate-700`.
* Luôn có hiệu ứng `transition-colors duration-200` để chuyển đổi mượt mà.

### D. Biểu mẫu (Form Inputs)
* Input phải có chiều cao vừa phải (`py-2 px-3`), viền `border-slate-200`, bo góc `rounded-lg`.
* Khi focus: Viền chuyển sang màu primary và có hiệu ứng ring mờ: `focus:border-indigo-500 focus:ring-4 focus:ring-indigo-100 focus:outline-none`.

---

## 5. Rào chắn AI nghiêm cấm (Design Guardrails - DO NOT DO)

Để tránh giao diện bị lỗi thời hoặc thô kệch, **AI KHÔNG ĐƯỢC PHÉP**:
1.  **KHÔNG** dùng các màu nguyên bản quá chói (như `bg-blue-500` thuần, `bg-red-600` thuần của hệ màu cũ) mà không có sự phối hợp.
2.  **KHÔNG** dùng bo góc quá nhọn (`rounded-sm` hoặc không bo góc) trừ khi có yêu cầu đặc biệt.
3.  **KHÔNG** dùng đổ bóng quá đậm (`shadow-xl`, `shadow-2xl` màu đen sì). Hãy dùng shadow mờ, mịn.
4.  **KHÔNG** thiết kế các bảng dữ liệu (Tables) có đường viền đen đậm ngăn cách từng ô. Hãy dùng viền ngang mờ `border-b border-slate-100` và padding rộng rãi cho hàng.
5.  **KHÔNG** tự ý dùng icon từ nhiều thư viện khác nhau. Hãy dùng nhất quán **Lucide Icons** hoặc **Heroicons**.

---

## 6. Trạng thái và Hiệu ứng động (States & Animations)

* **Hover State:** Tất cả các thành phần tương tác được (Clickable) bắt buộc phải có phản hồi khi hover (đổi màu nền, đổi màu chữ, hoặc nâng nhẹ shadow).
* **Loading State:** Khi tải dữ liệu, sử dụng skeleton loading (`animate-pulse` với nền `bg-slate-200`) thay vì vòng xoay loading thô sơ.
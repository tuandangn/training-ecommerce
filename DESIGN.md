# Hệ thống Quản lý VLXD Tuấn Khôi - Hướng dẫn Thiết kế Giao diện (UI/UX Design System)

Tài liệu này quy định các tiêu chuẩn về giao diện, mã màu, font chữ, layout và component để đảm bảo tính nhất quán trên toàn bộ hệ thống website quản lý nội bộ.

---

## 1. Nguyên tắc Thiết kế Chủ đạo (Design Philosophy)
* **Phong cách:** Hiện đại, tối giản, ưu tiên không gian trắng để tăng khả năng tập trung vào dữ liệu số.
* **Giao diện:** Clean Admin Dashboard với các góc bo tròn mềm mại (`border-radius`) và đổ bóng nhẹ (`box-shadow`) để phân tách các khối nội dung.
* **Trải nghiệm người dùng:** Hiển thị rõ ràng các trạng thái bằng màu sắc tương phản mạnh.

---

## 2. Hệ thống Màu sắc (Color Palette)

### Màu sắc Thương hiệu & Điều hướng (Brand & Navigation)
* **Primary (Màu chủ đạo):** `#5346E0` (Tím Indigo) - Dùng cho nút hành động chính, menu đang active, badge nổi bật.
* **Sidebar Background:** `#14172A` (Xanh đen đậm) - Tạo chiều sâu và độ tương phản cao cho thanh điều hướng.
* **Sidebar Text Link:** `#8F9BB3` (Xám xanh) | **Active Text/Icon:** `#FFFFFF` (Trắng).

### Trạng thái & Tài chính (Status Colors)
* **Success (Thành công / Đã thanh toán / Doanh số):** `#00A389` hoặc `#10B981` (Xanh lá cây ngọc) - Biểu thị dòng tiền dương hoặc trạng thái an toàn.
* **Danger / Warning (Nợ / Quá hạn):** `#EF4444` (Đỏ cam) - Dùng cho số tiền công nợ, cảnh báo quá hạn.
* **Alert Text (Nổi bật phụ):** `#D97706` (Vàng cam) - Dùng cho text thông báo số lượng khách hàng quá hạn.

### Màu nền & Đường viền (Background & Borders)
* **Main Background:** `#F8FAFC` (Xám trắng nhạt) - Nền toàn trang.
* **Card Background:** `#FFFFFF` (Trắng thuần) - Nền cho các khối chứa dữ liệu, bảng biểu.
* **Border Color:** `#E2E8F0` (Xám nhạt) - Đường viền phân cách mỏng cho table và các trường nhập liệu.

---

## 3. Hệ thống Chữ & Định dạng (Typography & Formatting)

### Font Chữ
* **Font Family:** `Inter`, `Segoe UI`, `Roboto`, sans-serif.
* **Định dạng Số tiền (Tiền tệ):** * Luôn đi kèm ký hiệu tiền tệ viết tắt có gạch chân ở đuôi (`đ`). Ví dụ: `1.24 tỷđ`, `128Mđ`, `45.2Mđ`, `35,100,000đ`.
    * Sử dụng dấu chấm `.` cho viết tắt hàng triệu/tỷ (`45.2Mđ`) và dấu phẩy `,` cho hiển thị đầy đủ số (`35,100,000đ`).

### Phân cấp Tiêu đề (Hierarchy)
* **Page Title (Tên trang):** `font-weight: 700`, màu `#1E293B`, kích thước lớn (khoảng `20px` - `22px`).
* **Sub-title / Breadcrumb:** Kèm ngay dưới tên trang, màu `#94A3B8`, cỡ chữ `12px`.
* **Card Number (Chỉ số KPI):** `font-weight: 700`, kích thước lớn `24px` - `28px`.

---

## 4. Bố cục & Thành phần Giao diện (Layout & Components)

### 4.1. Thanh điều hướng bên trái (Sidebar)
* Width: Cố định khoảng `240px` - `260px`.
* Phần trên cùng: Logo thương hiệu dạng Icon tròn (`VK`) bên cạnh Tên hệ thống (`VLXD Tuấn Khôi`) và Subtext (`Quản lý hệ thống`).
* Cấu trúc Menu Item: `[Icon] + [Tên chức năng]`. Nếu có thông báo số lượng (ví dụ: Đơn hàng), hiển thị Badge tròn màu tím sáng ở phía bên phải.
* Phần dưới cùng: Thông tin User Admin (`Tuấn Khôi - Admin`) cố định ở góc đáy.

### 4.2. Thẻ chỉ số (Metric Cards / Dashboard Overview)
* Nằm ngang ở đầu trang, chia đều theo lưới (Grid layout).
* Cấu trúc bên trong Card:
    * Hàng trên: Tiêu đề chỉ số (Cỡ chữ nhỏ, màu xám `#94A3B8`).
    * Hàng dưới: Số liệu lớn, tô màu theo thuộc tính (Doanh số -> Xanh lá, Công nợ -> Đỏ).

### 4.3. Bảng dữ liệu (Data Tables)
* **Header bảng:** Nền xám nhạt rất nhẹ hoặc trắng, chữ in hoa, màu `#8F9BB3`, font-weight `600`.
* **Dòng (Rows):** Khoảng cách dòng thoáng (`padding: 12px 16px`), ngăn cách bởi border bottom mỏng `#F1F5F9`.
* **Cột Khách hàng:** Chứa Avatar là ký tự đầu của tên nằm trong vòng tròn màu Pastel nhạt, kế bên là Tên khách hàng (Chữ đậm).
* **Badges phân loại:** Bo góc hoàn toàn (`border-radius: 9999px`), nền nhạt chữ đậm (Ví dụ: `Doanh nghiệp` nền xanh dương nhạt, `Cá nhân` nền xanh sky nhạt, `HTX` nền xanh cyan nhạt).

### 4.4. Form & Hộp thoại Nhập liệu (Input & Forms)
* **Trường tìm kiếm/Nhập liệu:** Bo góc nhẹ (`6px` - `8px`), có icon gợi ý bên trong (như kính lúp), border màu nhạt.
* **Alert Box (Hộp cảnh báo công nợ):** Nền màu đỏ nhạt/hồng nhạt, viền đỏ, chữ màu đỏ đậm, hiển thị rõ số tiền nợ và hạn thanh toán.
* **Layout Tạo Đơn Hàng:** Chia làm 2 cột:
    * *Cột trái (nhỏ):* Thông tin khách hàng, thông tin đơn hàng (ngày đặt, hình thức thanh toán, giao hàng, ghi chú).
    * *Cột phải (lớn):* Giỏ hàng chọn sản phẩm (Tên sản phẩm, Số lượng - SL, Đơn vị, Đơn giá, Thành tiền, nút xóa).
    * *Footer Form:* Thanh tổng kết tài chính (Tạm tính, Chiết khấu, Tổng cộng) nằm cố định ở đáy, bên phải có cụm nút `Lưu nháp` (Nền trắng, viền xám) và `✓ Xác nhận đơn` (Nền xanh lá, chữ trắng).

---

## 5. Quy định về Trạng thái Nút (Buttons)
* **Nút Hành động Chính (Primary Button):** Nền `#5346E0`, chữ trắng, bo góc, có dấu `+` phía trước đối với hành động "Thêm mới".
* **Nút Xác nhận Đơn (Success Button):** Nền `#00A389`, chữ trắng, có icon check `✓`.
* **Nút Phụ (Secondary Button):** Nền trắng, viền `#E2E8F0`, chữ đen/xám (Ví dụ: `Lưu nháp`, `Xem...`).

---

## Hướng dẫn cho AI:
Khi tạo trang mới (ví dụ: Tài chính, Kho hàng, Nhập hàng), hãy tuân thủ nghiêm ngặt bảng màu, cấu trúc bảng, font chữ tiền tệ `đ` và layout chia khối như trên để đảm bảo trang mới không bị lệch tone với hệ thống hiện tại.
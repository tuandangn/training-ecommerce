# Triển khai giao một phần và bàn giao tiền COD

## TodoList

- [x] Đổi xác nhận giao hàng sang nhập số lượng trả về, tự tính số lượng khách nhận.
- [x] Cho mobile PWA gửi chi tiết số lượng trả về và lý do trả hàng, kể cả khi lưu offline.
- [x] Ghi nhận số tiền shipper đã thu khi xác nhận giao hàng, không tự suy diễn từ số tiền phải thu.
- [x] Chặn tiền shipper thu vượt số tiền phải thu của phiếu đã giao.
- [x] Bỏ luồng tự tạo tiền cọc/ứng trước khi COD thu dư.
- [x] Chỉ giảm công nợ khi thủ quỹ xác nhận bàn giao tiền.
- [x] Thêm thao tác cập nhật tiền shipper đã thu cho phiếu đã giao trong chi tiết chuyến giao.
- [x] Chặn xác nhận bàn giao tiền khi còn phiếu đã giao nhưng chưa khai báo tiền thu.
- [x] Thêm node timeline Order riêng cho khoản thủ quỹ nhận tiền COD.
- [x] Verify bằng build Web project và UI lint.

## Verify

- `dotnet build NamEcommerce/Presentation/NamEcommerce.Web/NamEcommerce.Web.csproj`
- `powershell -ExecutionPolicy Bypass -File tools/ui-lint.ps1`

Không tạo migration và không viết test theo yêu cầu.

# Todo Backlog

- [x] **Task 1: Thêm thông tin dặn dò của khách hàng **
  - *Mô tả:* Sau khi khách hàng đặt hàng thì có thể họ có thêm những yêu cầu, dặn dò mới cần mình lưu ý khi xử lý hàng cho họ, mình cần có cơ chế để thu thập hiển thị cho admin biết.
  - *Kết quả mong muốn:* Thêm phần ghi chú, dặn dò từ khách hàng và hiển thị cho admin biết, hiển thị cả trên timeline.
  - *Phạm vi:* Admin, Customer portal: khách hàng có thể dặn dò từ cổng khách hàng.


- [x] **Task 2: Cần thêm event khi đơn hàng đã được giao đầy đủ **
  - *Mô tả:* Khi hàng đã được giao đến khách hàng, hệ thống sẽ phát hiện là hàng của đơn hàng đã được giao đủ và fire event hàng đã được giao đầy đủ để xử lý nghiệp vụ tiếp theo. Đầu tiên đó là release allocation còn đang pending (chưa resolve hết).
  - *Kết quả mong muốn:* Fire event khi đơn hàng đã được giao đầy đủ và tránh fire event liên tục. Pending allocation được release đúng đắn.


- [x] **Task 3: Cần tự nhập `giá bán/giá vốn` từng `bán cho khách hàng/nhập từ nhà cung cấp` hoặc `giá bán/giá nhập` gần nhất khi dùng ProductBrowser **
  - *Mô tả:* Khi thêm sản phẩm vào từ ProductBrowser thì giá bán/giá vốn hiện chỉ có 0 dù đã từng bán cho khách rồi hoặc nhập từ nhà cung cấp rồi.
  - *Kết quả mong muốn:* Hiện giá bán/giá vốn từng bán/nhập hoặc giá gần nhất.



# Customer Portal Smoke Checklist

Tài liệu này dùng để chạy kiểm tra thủ công sau khi tạo migration và cập nhật database cho cổng khách hàng.

## Điều kiện trước khi chạy

- Tuấn tạo và chạy EF migration cho các bảng customer portal. Codex không chạy migration trong project này.
- SQL Server dev trỏ về database `NamEcommerceDb`.
- Customer API dùng `NamEcommerce/Presentation/Customer/NamEcommerce.Customer.Api/appsettings.Development.json`.
- Web admin dùng `NamEcommerce/Presentation/NamEcommerce.Web/appsettings.Development.json`.
- React client dùng `VITE_CUSTOMER_API_URL=https://localhost:7001` hoặc URL API thật.
- Khi deploy sau reverse proxy, bật `CustomerPortal:ForwardedHeaders:Enabled` cho Customer API và `ForwardedHeaders:Enabled` cho Web admin.
- Khi deploy nhiều instance hoặc container, cấu hình `CustomerPortal:DataProtection:KeysPath` cho Customer API và `DataProtection:KeysPath` cho Web admin để persist key.

Các bảng cần có migration:

- `CustomerPortalAccount`
- `CustomerOtpChallenge`
- `CustomerPortalSession`
- `CustomerSecurityEvent`
- `DeliveryNoteAccessToken`
- `CustomerDeliveryFeedback`
- `CustomerOrderRequest`
- `CustomerOrderRequestItem`
- `CustomerReturnRequest`
- `CustomerReturnRequestItem`
- `CustomerPaymentIntent`

## Chạy local

Terminal 1:

```powershell
rtk dotnet run --project NamEcommerce\Presentation\NamEcommerce.Web\NamEcommerce.Web.csproj
```

Terminal 2:

```powershell
rtk dotnet run --project NamEcommerce\Presentation\Customer\NamEcommerce.Customer.Api\NamEcommerce.Customer.Api.csproj
```

Terminal 3:

```powershell
cd NamEcommerce\Presentation\Customer\NamEcommerce.Customer.Client
rtk npm.cmd run dev
```

Nếu Vite không chạy ở port `5173`, cập nhật tạm `CustomerPortal:ClientBaseUrl` trong Web admin và `CustomerPortal:AllowedOrigins` trong Customer API cho đúng port.

## Luồng QR và OTP

1. Đăng nhập Web admin.
2. Mở một phiếu giao hàng đã có khách hàng và hàng hóa.
3. In phiếu giao hàng.
4. Kiểm tra phiếu in có QR.
5. Quét QR hoặc mở URL `/d/{token}` trên React client.
6. Trang public chỉ hiển thị mã phiếu, mã đơn, trạng thái, hàng hóa và số lượng.
7. Bấm `Xác thực`.
8. Với provider mock, OTP được trả về trong response và client auto-fill ở màn xác thực.
9. Nhập/xác nhận OTP.
10. Sau xác thực, khách vào `/app`.

Kỳ vọng:

- Token không hợp lệ trả 404.
- Token hợp lệ không lộ công nợ, email, số điện thoại, giá hoặc danh sách đơn khác ở màn public.
- Session dùng cookie session-only, không lưu token trong localStorage/sessionStorage.
- Đóng phiên trình duyệt thì lần sau phải xác thực lại, trừ khi khách đã đặt mật khẩu và dùng login bằng mật khẩu.

## Luồng quản lý khách hàng

1. Dashboard hiển thị đơn gần đây, phiếu giao gần đây và công nợ.
2. Danh sách đơn hàng chỉ gồm đơn của customer đang đăng nhập.
3. Tạo yêu cầu đặt hàng mới.
4. Web admin mở `Cổng khách hàng > Đơn khách tạo`.
5. Admin duyệt hoặc từ chối yêu cầu.
6. Nếu duyệt, admin chuyển yêu cầu thành đơn nội bộ.

Kỳ vọng:

- Yêu cầu khách tạo không tự sinh đơn nội bộ trước khi admin duyệt.
- CustomerId luôn lấy từ session, không lấy từ body request.

## Luồng phiếu giao, phản hồi và trả hàng

1. Khách mở chi tiết phiếu giao.
2. Xác nhận đã giao.
3. Gửi phản hồi.
4. Tạo yêu cầu trả hàng theo item và số lượng.
5. Web admin mở `Cổng khách hàng > Yêu cầu trả hàng`.
6. Admin nhận hoặc từ chối.
7. Nếu nhận, admin chọn kho và chuyển thành phiếu khách trả hàng nội bộ.

Kỳ vọng:

- Khách không thao tác được phiếu giao của customer khác.
- Khách bị khóa không xác nhận giao, phản hồi hoặc tạo trả hàng.

## Luồng công nợ và thanh toán mock

1. Khách mở công nợ.
2. Chọn thanh toán một khoản nợ.
3. Tạo payment intent.
4. Hoàn tất mock payment.
5. Web admin mở `Cổng khách hàng > Thanh toán online`.
6. Admin đối soát.

Kỳ vọng:

- Mock payment thành công chỉ chuyển về `SucceededPendingReconciliation`.
- Công nợ chỉ được ghi nhận thanh toán sau khi admin đối soát.

## Kiểm tra chống quấy phá

Các ngưỡng nằm trong `CustomerPortal:Security` ở appsettings:

- `OtpExpiryMinutes`
- `SessionExpiryHours`
- `OtpCooldownSeconds`
- `MaxOtpRequestsPerCustomerPerHour`
- `MaxOtpRequestsPerIpPerHour`
- `MaxPasswordFailuresPerCustomerPerHour`
- `MaxPasswordFailuresPerIpPerHour`
- `DeliveryAccessTokenExpiryDays`
- `RevokeExistingDeliveryTokensOnCreate`

- Gửi OTP nhiều lần trong 60 giây cho cùng phiếu giao: bị chặn.
- Gửi quá 5 OTP/giờ theo customer: bị chặn.
- Gửi quá 20 OTP/giờ theo IP: bị chặn.
- Nhập sai OTP 5 lần: challenge bị khóa.
- Sai mật khẩu quá 5 lần/giờ theo customer hoặc 20 lần/giờ theo IP: bị chặn.
- Admin khóa customer: OTP, password login và session hiện hữu đều không dùng được nữa.
- QR token hết hạn sau số ngày cấu hình.
- Nếu bật `RevokeExistingDeliveryTokensOnCreate`, in lại QR sẽ vô hiệu hóa các token cũ của phiếu giao đó.

## Build kiểm tra nhanh

```powershell
rtk dotnet build NamEcommerce\Presentation\NamEcommerce.Web\NamEcommerce.Web.csproj
rtk dotnet build NamEcommerce\Presentation\Customer\NamEcommerce.Customer.Api\NamEcommerce.Customer.Api.csproj
cd NamEcommerce\Presentation\Customer\NamEcommerce.Customer.Client
rtk npm.cmd run build
```

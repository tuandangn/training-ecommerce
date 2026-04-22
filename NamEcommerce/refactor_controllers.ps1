$controllersDir = "d:\Learning\NamTraining\training-ecommerce\NamEcommerce\Presentation\NamEcommerce.Web\Controllers"
$files = Get-ChildItem -Path $controllersDir -Filter "*.cs" -Recurse

foreach ($file in $files) {
    if ($file.Name -match "BaseController") { continue }

    $content = Get-Content -Path $file.FullName -Raw
    $original = $content
    
    # 1. message = "Thông tin không hợp lệ." -> message = LocalizeError("Error.InvalidRequest")
    $content = [regex]::Replace($content, 'message\s*=\s*"Thông tin không hợp lệ\."', 'message = LocalizeError("Error.InvalidRequest")')
    
    # 2. TempData[...] = "Thêm mới ... thành công!" -> TempData[...] = LocalizeError("Msg.SaveSuccess")
    $content = [regex]::Replace($content, '=\s*"Thêm mới.*?thành công!"', '= LocalizeError("Msg.SaveSuccess")')

    # 3. TempData[...] = "Chỉnh sửa ... thành công!" -> TempData[...] = LocalizeError("Msg.SaveSuccess")
    $content = [regex]::Replace($content, '=\s*"Chỉnh sửa.*?thành công!"', '= LocalizeError("Msg.SaveSuccess")')

    # 4. TempData[...] = "Xóa ... thành công!" -> TempData[...] = LocalizeError("Msg.DeleteSuccess")
    $content = [regex]::Replace($content, '=\s*"Xóa.*?thành công!"', '= LocalizeError("Msg.DeleteSuccess")')

    # 5. TempData[...] = "Cập nhật ... thành công!" -> TempData[...] = LocalizeError("Msg.SaveSuccess")
    $content = [regex]::Replace($content, '=\s*"Cập nhật.*?thành công!"', '= LocalizeError("Msg.SaveSuccess")')
    
    # 6. TempData[...] = "Tạo đơn bán thành công!" -> TempData[...] = LocalizeError("Msg.SaveSuccess")
    $content = [regex]::Replace($content, '=\s*"Tạo đơn bán thành công!"', '= LocalizeError("Msg.SaveSuccess")')

    # 7. TempData[...] = "Không tìm thấy kho hàng." -> TempData[...] = LocalizeError("Error.WarehouseIsNotFound")
    $content = [regex]::Replace($content, '=\s*"Không tìm thấy kho hàng\."', '= LocalizeError("Error.WarehouseIsNotFound")')
    
    # 8. message = "Hình ảnh bằng chứng giao hàng là bắt buộc." -> message = LocalizeError("Error.DeliveryProofRequired")
    $content = [regex]::Replace($content, 'message\s*=\s*"Hình ảnh bằng chứng giao hàng là bắt buộc\."', 'message = LocalizeError("Error.DeliveryProofRequired")')

    # 9. Success messages in JSON
    $content = [regex]::Replace($content, 'message\s*=\s*"Đã ghi nhận thanh toán thành công\."', 'message = LocalizeError("Msg.SaveSuccess")')
    $content = [regex]::Replace($content, 'message\s*=\s*"Đã ghi nhận chi tiền thành công\."', 'message = LocalizeError("Msg.SaveSuccess")')
    $content = [regex]::Replace($content, 'message\s*=\s*"Đã ghi nhận tiền ứng trước thành công\."', 'message = LocalizeError("Msg.SaveSuccess")')
    $content = [regex]::Replace($content, 'message\s*=\s*"Đã tạo đơn nhập hàng thành công\."', 'message = LocalizeError("Msg.SaveSuccess")')
    $content = [regex]::Replace($content, 'message\s*=\s*"Đã xác nhận phiếu xuất\."', 'message = LocalizeError("Msg.SaveSuccess")')
    $content = [regex]::Replace($content, 'message\s*=\s*"Đang giao hàng\."', 'message = LocalizeError("Msg.SaveSuccess")')
    $content = [regex]::Replace($content, 'message\s*=\s*"Đã giao hàng thành công\."', 'message = LocalizeError("Msg.SaveSuccess")')
    $content = [regex]::Replace($content, 'message\s*=\s*"Đã đánh dấu giao hàng thành công\."', 'message = LocalizeError("Msg.SaveSuccess")')
    $content = [regex]::Replace($content, 'message\s*=\s*"Đã hủy phiếu xuất kho\."', 'message = LocalizeError("Msg.SaveSuccess")')
    $content = [regex]::Replace($content, 'message\s*=\s*"Tạo phiếu xuất kho thành công\."', 'message = LocalizeError("Msg.SaveSuccess")')

    # 10. message = "Số lượng xuất phải lớn hơn 0."
    $content = [regex]::Replace($content, 'message\s*=\s*"Số lượng xuất phải lớn hơn 0\."', 'message = LocalizeError("Error.OrderItemQuantityMustBePositive")')

    if ($content -ne $original) {
        Set-Content -Path $file.FullName -Value $content -Encoding UTF8
        Write-Host "Refactored $($file.Name)"
    }
}

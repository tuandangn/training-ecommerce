namespace NamEcommerce.Domain.Shared.Enums.Returns;

/// <summary>
/// Trạng thái của phiếu trả hàng nhà cung cấp.
/// </summary>
public enum VendorReturnStatus
{
    /// <summary>Bản nháp — chưa xác nhận, có thể sửa hoặc huỷ.</summary>
    Draft = 0,

    /// <summary>Đang kiểm tra hàng trả — đã chuẩn bị hàng, đang đối chiếu trước khi gửi NCC.</summary>
    Inspecting = 1,

    /// <summary>Đã xác nhận — sinh <c>DeliveryNote(SourceType=ToVendorReturn)</c> và giảm công nợ NCC.</summary>
    Confirmed = 2,

    /// <summary>Đã huỷ — chỉ cho phép từ Draft hoặc Inspecting.</summary>
    Cancelled = 3
}

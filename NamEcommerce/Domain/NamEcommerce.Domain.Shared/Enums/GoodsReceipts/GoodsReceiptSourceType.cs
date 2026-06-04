namespace NamEcommerce.Domain.Shared.Enums.GoodsReceipts;

/// <summary>
/// Nguồn gốc của phiếu nhập kho. Mỗi nguồn quyết định business rule khác nhau cho handler
/// (ví dụ: <see cref="FromVendor"/> sinh <c>VendorDebt</c>; <see cref="FromCustomerReturn"/> KHÔNG sinh nợ).
/// <para>
/// Giá trị 0-based để dữ liệu hiện có (chưa có cột) sau migration tự map về <see cref="FromVendor"/>.
/// </para>
/// </summary>
public enum GoodsReceiptSourceType
{
    /// <summary>Nhập từ nhà cung cấp — qua PurchaseOrder hoặc phiếu độc lập (default).</summary>
    FromVendor = 0,

    /// <summary>Nhập do khách trả hàng — auto-sinh khi <c>CustomerReturn.Confirmed</c>.</summary>
    FromCustomerReturn = 1,

    /// <summary>Nhập do điều chỉnh / kiểm kê. Phase C — chưa dùng.</summary>
    FromAdjustment = 2,

    /// <summary>Tồn kho đầu kỳ khi tạo sản phẩm mới — không thể hủy, xóa, hoặc trả NCC.</summary>
    OpeningBalance = 3
}

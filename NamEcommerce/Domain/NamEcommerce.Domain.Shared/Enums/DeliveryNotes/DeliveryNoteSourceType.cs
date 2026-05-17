namespace NamEcommerce.Domain.Shared.Enums.DeliveryNotes;

/// <summary>
/// Nguồn gốc của phiếu xuất kho. Mỗi nguồn quyết định business rule khác nhau cho handler
/// (ví dụ: <see cref="ToCustomer"/> sinh <c>CustomerDebt</c>; <see cref="ToVendorReturn"/> KHÔNG sinh nợ).
/// <para>
/// Giá trị 0-based để dữ liệu hiện có (chưa có cột) sau migration tự map về <see cref="ToCustomer"/>.
/// </para>
/// </summary>
public enum DeliveryNoteSourceType
{
    /// <summary>Xuất bán cho khách hàng — qua Order (default).</summary>
    ToCustomer = 0,

    /// <summary>Xuất do trả hàng cho nhà cung cấp — auto-sinh khi <c>VendorReturn.Confirmed</c>.</summary>
    ToVendorReturn = 1,

    /// <summary>Xuất do điều chỉnh / kiểm kê / hư hao. Phase C — chưa dùng.</summary>
    ToAdjustment = 2,

    /// <summary>Xuất giao thẳng từ nhà cung cấp tới khách hàng.</summary>
    DirectShipToCustomer = 3
}

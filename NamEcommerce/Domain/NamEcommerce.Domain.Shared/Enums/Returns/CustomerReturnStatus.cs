namespace NamEcommerce.Domain.Shared.Enums.Returns;

/// <summary>
/// Trạng thái của phiếu trả hàng khách hàng.
/// </summary>
public enum CustomerReturnStatus
{
    /// <summary>Bản nháp — chưa xác nhận, có thể sửa hoặc huỷ.</summary>
    Draft = 0,

    /// <summary>Đang kiểm tra hàng trả — đã nhận hàng, đang đối chiếu chất lượng/số lượng.</summary>
    Inspecting = 1,

    /// <summary>Đã xác nhận — sinh <c>GoodsReceipt(SourceType=FromCustomerReturn)</c> và giảm công nợ khách.</summary>
    Confirmed = 2,

    /// <summary>Đã huỷ — chỉ cho phép từ Draft hoặc Inspecting.</summary>
    Cancelled = 3
}

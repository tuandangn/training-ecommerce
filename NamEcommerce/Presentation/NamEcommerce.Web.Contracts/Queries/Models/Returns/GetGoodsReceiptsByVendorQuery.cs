using MediatR;
using NamEcommerce.Web.Contracts.Models.Returns;

namespace NamEcommerce.Web.Contracts.Queries.Models.Returns;

/// <summary>
/// Lấy danh sách phiếu nhập kho (FromVendor) của một NCC — dùng cho AJAX picker khi tạo phiếu Trả hàng NCC.
/// </summary>
[Serializable]
public sealed class GetGoodsReceiptsByVendorQuery : IRequest<List<GoodsReceiptPickerModel>>
{
    public required Guid VendorId { get; init; }
}

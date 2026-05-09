using MediatR;
using NamEcommerce.Web.Contracts.Models.Returns;

namespace NamEcommerce.Web.Contracts.Queries.Models.Returns;

/// <summary>
/// Lấy danh sách phiếu xuất kho (Delivered) của một khách hàng — dùng cho AJAX picker khi tạo phiếu Khách trả hàng.
/// </summary>
[Serializable]
public sealed class GetDeliveryNotesByCustomerQuery : IRequest<List<DeliveryNotePickerModel>>
{
    public required Guid CustomerId { get; init; }
}

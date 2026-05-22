using MediatR;
using NamEcommerce.Web.Contracts.Models.Returns;

namespace NamEcommerce.Web.Contracts.Queries.Models.Returns;

[Serializable]
public sealed class GetVendorReturnListQuery : IRequest<VendorReturnListModel>
{
    public Guid? VendorId { get; init; }
    public Guid? PurchaseOrderId { get; init; }
    public Guid? GoodsReceiptId { get; init; }
    public int? Status { get; init; }
    public int PageIndex { get; init; }
    public int PageSize { get; init; }
}

using MediatR;
using NamEcommerce.Web.Contracts.Models.GoodsReceipts;

namespace NamEcommerce.Web.Contracts.Commands.Models.GoodsReceipts;

[Serializable]
public sealed class SetGoodsReceiptVendorCommand : IRequest<SetGoodsReceiptVendorResultModel>
{
    public required Guid GoodsReceiptId { get; init; }

    /// <summary>null = xoá vendor khỏi phiếu.</summary>
    public Guid? VendorId { get; init; }
}

using MediatR;
using NamEcommerce.Web.Contracts.Models.GoodsReceipts;

namespace NamEcommerce.Web.Contracts.Commands.Models.GoodsReceipts;

[Serializable]
public sealed class QuickCreateAndLinkPurchaseOrderCommand : IRequest<QuickCreateAndLinkPurchaseOrderResultModel>
{
    public required Guid GoodsReceiptId { get; init; }

    public required Guid VendorId { get; init; }

    public required DateTime PlacedOn { get; init; }

    public Guid? WarehouseId { get; init; }

    public string? Note { get; init; }
}

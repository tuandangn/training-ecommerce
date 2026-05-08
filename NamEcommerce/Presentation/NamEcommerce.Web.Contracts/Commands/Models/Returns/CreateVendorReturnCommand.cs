using MediatR;
using NamEcommerce.Web.Contracts.Models.Returns;

namespace NamEcommerce.Web.Contracts.Commands.Models.Returns;

[Serializable]
public sealed class CreateVendorReturnCommand : IRequest<CreateVendorReturnResultModel>
{
    public required Guid VendorId { get; init; }
    public Guid? PurchaseOrderId { get; init; }
    public Guid? GoodsReceiptId { get; init; }
    public required Guid WarehouseId { get; init; }
    public string? Note { get; init; }
    public IList<CreateVendorReturnItemCommand> Items { get; init; } = [];
}

[Serializable]
public sealed class CreateVendorReturnItemCommand
{
    public required Guid ProductId { get; init; }
    public Guid? GoodsReceiptItemId { get; init; }
    public required decimal RequestedQuantity { get; init; }
    public required decimal AcceptedQuantity { get; init; }
    public required decimal UnitCost { get; init; }
}

using MediatR;
using NamEcommerce.Web.Contracts.Models.PurchaseOrders;

namespace NamEcommerce.Web.Contracts.Commands.Models.PurchaseOrders;

[Serializable]
public sealed class BulkReceivePurchaseOrderCommand : IRequest<BulkReceivePurchaseOrderResultModel>
{
    public required Guid PurchaseOrderId { get; init; }
    public IList<BulkReceiveLineCommand> Items { get; init; } = [];

    /// <summary>Phí vận chuyển cộng thêm vào đơn (số tuyệt đối).</summary>
    public decimal AdditionalShipping { get; set; }

    /// <summary>Thuế cộng thêm vào đơn — UI đã quy đổi từ % nếu cần (số tuyệt đối).</summary>
    public decimal AdditionalTax { get; set; }
}

[Serializable]
public sealed class BulkReceiveLineCommand
{
    public required Guid ItemId { get; init; }
    public required decimal Quantity { get; init; }
    public required Guid? WarehouseId { get; init; }
    public decimal? ActualUnitCost { get; init; }
}

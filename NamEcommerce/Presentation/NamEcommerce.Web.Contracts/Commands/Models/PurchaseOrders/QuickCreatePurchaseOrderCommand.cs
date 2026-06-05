using MediatR;
using NamEcommerce.Web.Contracts.Models.PurchaseOrders;

namespace NamEcommerce.Web.Contracts.Commands.Models.PurchaseOrders;

[Serializable]
public sealed class QuickCreatePurchaseOrderCommand : IRequest<QuickCreatePurchaseOrderResultModel>
{
    public required Guid VendorId { get; init; }
    public required Guid DefaultWarehouseId { get; init; }
    public required DateTime ReceivedOn { get; init; }
    public string? Note { get; init; }
    public bool ReceiveImmediately { get; init; } = true;
    public IList<Guid> PictureIds { get; init; } = [];
    public required IList<QuickCreatePurchaseOrderItemCommand> Items { get; init; }
    public QuickCreatePaymentCommand? Payment { get; init; }
}

[Serializable]
public sealed class QuickCreatePurchaseOrderItemCommand
{
    public required Guid ProductId { get; init; }
    public required decimal Quantity { get; init; }
    public decimal? UnitCost { get; init; }
    public Guid? WarehouseId { get; init; }
    public int QuantityDecimalPlaces { get; init; }
}

[Serializable]
public sealed class QuickCreatePaymentCommand
{
    public required decimal Amount { get; init; }
    public required int PaymentMethod { get; init; }
}

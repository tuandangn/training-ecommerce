using NamEcommerce.Web.Contracts.Models.PurchaseOrders;

namespace NamEcommerce.Web.Contracts.Commands.Models.PurchaseOrders;

[Serializable]
public sealed class PurchaseOrderQuickCreateCommand : ICommand<QuickCreatePurchaseOrderResultModel>
{
    public required DateTime PlacedOn { get; init; }
    public required Guid VendorId { get; init; }
    public required IList<PurchaseOrderQuickCreateItemModel> Items { get; init; }
    public string? Note { get; set; }

    public required bool IsReceived { get; init; }
    public Guid? DefaultWarehouseId { get; set; }
    public DateTime? ReceivedOn { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public IList<Guid>? PictureIds { get; set; }
    public decimal? ShippingAmount { get; set; }

    public required bool IsPaid { get; init; }
    public PurchaseOrderQuickCreatePaymentModel? PaymentInfo { get; set; }

    [Serializable]
    public sealed class PurchaseOrderQuickCreateItemModel
    {
        public required Guid ProductId { get; init; }
        public required decimal Quantity { get; init; }
        public decimal? UnitCost { get; set; }
        public Guid? WarehouseId { get; set; }
    }

    [Serializable]
    public sealed class PurchaseOrderQuickCreatePaymentModel
    {
        public required decimal PaidAmount { get; init; }
        public required int PaymentMethod { get; init; }
    }
}


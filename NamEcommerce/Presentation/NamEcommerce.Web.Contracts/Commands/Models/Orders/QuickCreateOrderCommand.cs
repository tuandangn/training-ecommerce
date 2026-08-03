using NamEcommerce.Web.Contracts.Models.FastSales;

namespace NamEcommerce.Web.Contracts.Commands.Models.Orders;

[Serializable]
public sealed class QuickCreateOrderCommand : ICommand<QuickCreateOrderResultModel>
{
    public required Guid CustomerId { get; init; }
    public required IList<QuickCreateOrderItemModel> Items { get; init; }
    public required bool DeliveryNow { get; init; }
    public decimal OrderDiscount { get; set; }

    public DateTime? ExpectedShippingDate { get; set; }
    public string? ShippingAddress { get; set; }
    public string? ShippingPhoneNumber { get; set; }

    public string? Note { get; set; }

    [Serializable]
    public sealed class QuickCreateOrderItemModel
    {
        public required Guid ProductId { get; init; }
        public Guid? WarehouseId { get; init; }
        public decimal Quantity { get; init; }
        public decimal UnitPrice { get; init; }
    }
}

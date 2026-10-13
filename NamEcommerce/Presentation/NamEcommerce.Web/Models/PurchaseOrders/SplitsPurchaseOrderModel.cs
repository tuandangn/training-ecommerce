using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using NamEcommerce.Web.Contracts.Models.PurchaseOrders;

namespace NamEcommerce.Web.Models.PurchaseOrders;

[Serializable]
public sealed class SplitsPurchaseOrderModel 
{
    public Guid PurchaseOrderId { get; set; }
    [ValidateNever]
    public string? PurchaseOrderCode { get; set; }

    public IList<SplitsPurchaseOrderItemModel> Items { get; set; } = [];

    [ValidateNever]
    public IList<PurchaseOrderModel.ItemModel> AvailableSplitableItems { get; set; }

    [Serializable]
    public sealed class SplitsPurchaseOrderItemModel
    {
        public Guid ItemId { get; set; }
        public decimal Quantity { get; set; }
        public decimal QuantityDecimalPlaces { get; set; }
    }
}


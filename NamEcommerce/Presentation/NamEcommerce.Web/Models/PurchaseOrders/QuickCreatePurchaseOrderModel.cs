using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Models.PurchaseOrders;

[Serializable]
public sealed class QuickCreatePurchaseOrderModel
{
    public DateTime ReceivedOn { get; set; } = DateTime.Now;

    public Guid? VendorId { get; set; }
    [ValidateNever]
    public string? VendorName { get; set; }

    public Guid? DefaultWarehouseId { get; set; }

    public string? Note { get; set; }

    public bool ReceiveImmediately { get; set; } = true;

    public IList<QuickCreatePurchaseOrderItemModel> Items { get; set; } = [];

    [ValidateNever]
    public IEnumerable<EntityOptionListModel.EntityOptionModel> AvailableWarehouses { get; set; } = [];
}

[Serializable]
public sealed class QuickCreatePurchaseOrderItemModel
{
    public Guid? ProductId { get; set; }
    [ValidateNever]
    public string? ProductName { get; set; }
    [ValidateNever]
    public string? ProductPicture { get; set; }
    [ValidateNever]
    public int QuantityDecimalPlaces { get; set; }
    public decimal? Quantity { get; set; }
    public decimal? UnitCost { get; set; }
    public Guid? WarehouseId { get; set; }
}

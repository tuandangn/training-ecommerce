using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using NamEcommerce.Web.Contracts.Models.Common;
using NamEcommerce.Web.Contracts.Models.PurchaseOrders;
using System.ComponentModel.DataAnnotations;

namespace NamEcommerce.Web.Models.PurchaseOrders;

[Serializable]
public sealed class PurchaseOrderSingleReceiveItemsModel
{
    public Guid PurchaseOrderId { get; set; }
    public Guid PurchaseOrderItemId { get; set; }

    [Display(Name = "Ngày nhận")]
    public DateTime? ReceivedOn { get; set; }
    [ValidateNever]
    public DateTime PurchaseOrderPlacedOn { get; set; }

    [Display(Name = "Phí vận chuyển")]
    public decimal? AdditionalShipping { get; set; }

    public IList<Guid> PictureIds { get; set; } = [];

    [Display(Name = "Thuế")]
    public decimal? TaxRate { get; set; }
    [ValidateNever]
    public decimal[] AvailableTaxRates { get; set; } = [];

    [ValidateNever]
    public string ProductName { get; set; } = default!;
    [ValidateNever]
    public string? UnitMeasurement { get; set; }

    [Display(Name = "Số lượng")]
    public decimal Quantity { get; set; }

    public int QuantityDecimalPlaces { get; set; }

    [Display(Name = "Kho hàng")]
    public Guid? WarehouseId { get; set; }
    [ValidateNever]
    public IList<EntityOptionListModel.EntityOptionModel> AvailableWarehouses { get; set; } = [];

    [Display(Name = "Giá vốn thực tế")]
    public decimal? ActualUnitCost { get; set; }

    [Display(Name = "Giá bán")]
    public decimal? SellingPrice { get; set; }

    [ValidateNever]
    public decimal RemainingDirectShipQuantity { get; set; }

    public Guid? DirectShipOrderId { get; set; }
    public Guid? DirectShipOrderItemId { get; set; }
    public string? DirectShipAddress { get; set; }
    public string? DirectShipContactName { get; set; }
    public string? DirectShipContactPhone { get; set; }
    public Guid? DirectShipExistingAllocationId { get; set; }
}

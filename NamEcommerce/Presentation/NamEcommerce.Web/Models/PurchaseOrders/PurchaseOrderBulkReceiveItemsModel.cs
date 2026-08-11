using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using NamEcommerce.Web.Contracts.Models.Common;
using NamEcommerce.Web.Contracts.Models.PurchaseOrders;
using System.ComponentModel.DataAnnotations;

namespace NamEcommerce.Web.Models.PurchaseOrders;

[Serializable]
public sealed class PurchaseOrderBulkReceiveItemsModel
{
    public Guid PurchaseOrderId { get; set; }

    [Display(Name = "Phí vận chuyển")]
    public decimal? AdditionalShipping { get; set; }

    [Display(Name = "Thuế")]
    public decimal? TaxRate { get; set; }
    public decimal[] AvailableTaxRates { get; set; } = [];

    [Display(Name = "Hàng hóa nhận")]
    public IList<BulkReceiveLineModel> Items { get; set; } = [];

    [ValidateNever]
    public IList<PurchaseOrderModel.ItemModel> RemainingReceivableItems { get; set; } = [];

    [ValidateNever]
    public IDictionary<Guid, IList<PurchaseOrderDetailsModel.DirectShipAllocationForPoModel>> DirectShipAllocationsPerItem { get; set; }
        = new Dictionary<Guid, IList<PurchaseOrderDetailsModel.DirectShipAllocationForPoModel>>();

    [ValidateNever]
    public Guid? DefaultWarehouseId { get; set; }
    [ValidateNever]
    public IList<EntityOptionListModel.EntityOptionModel> AvailableWarehouses { get; set; } = [];

    [Serializable]
    public sealed class BulkReceiveLineModel
    {
        public Guid ItemId { get; set; }

        [Display(Name = "Số lượng")]
        public decimal Quantity { get; set; }

        [Display(Name = "Kho hàng")]
        public Guid? WarehouseId { get; set; }

        [Display(Name = "Giá vốn thực tế")]
        public decimal? ActualUnitCost { get; set; }

        public Guid? DirectShipOrderId { get; set; }
        public Guid? DirectShipOrderItemId { get; set; }
        public string? DirectShipAddress { get; set; }
        public string? DirectShipContactName { get; set; }
        public string? DirectShipContactPhone { get; set; }
        public Guid? DirectShipExistingAllocationId { get; set; }
    }
}

using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using NamEcommerce.Domain.Shared.Enums.Orders;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Models.PurchaseOrders;

[Serializable]
public sealed class PurchaseOrderQuickCreateModel
{
    public Guid? VendorId { get; set; }
    [ValidateNever]
    public string? VendorName { get; set; }
    [ValidateNever]
    public string? VendorPhone { get; set; }
    [ValidateNever]
    public string? VendorAddress { get; set; }
    [ValidateNever]
    public bool NotHasAppropriatedVendor { get; set; }
    [ValidateNever]
    public IEnumerable<Guid> ValidVendorIds => Items.SelectMany(item => item.AvailableVendors.Select(v => v.Id))
        .Distinct()
        .Where(id => Items.All(item => item.AvailableVendors.Any(v => v.Id == id)))
        .ToList();

    public string? Note { get; set; }
    public DateTime PlacedOn { get; set; } = DateTime.Now;
    public DateTime? ReceivedOn { get; set; }
    public bool IsReceived { get; set; }
    public IList<Guid> PictureIds { get; set; } = [];
    public DateTime? ExpectedDeliveryDate { get; set; }

    public bool IsPaid { get; set; }
    public decimal? PaidAmount { get; set; }
    public PaymentMethod PaymentMethod { get; set; }

    public Guid? DefaultWarehouseId { get; set; }
    [ValidateNever]
    public IEnumerable<EntityOptionListModel.EntityOptionModel> AvailableWarehouses { get; set; } = [];

    public IList<QuickCreatePurchaseOrderItemModel> Items { get; set; } = [];

    [Serializable]
    public sealed class QuickCreatePurchaseOrderItemModel
    {
        public Guid? ProductId { get; set; }
        [ValidateNever]
        public string? ProductDisplayName { get; set; }
        [ValidateNever]
        public string? ProductDisplayPicture { get; set; }
        [ValidateNever]
        public int QuantityDecimalPlaces { get; set; }
        public decimal? Quantity { get; set; }
        public decimal? UnitCost { get; set; }

        [ValidateNever]
        public decimal SubTotal => (Quantity ?? 0) * (UnitCost ?? 0);

        [ValidateNever]
        public IEnumerable<EntityOptionListModel.EntityOptionModel> AvailableVendors { get; set; } = [];
    }
}

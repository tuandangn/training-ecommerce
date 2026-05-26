using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using NamEcommerce.Web.Contracts.Models.Common;
using NamEcommerce.Web.Contracts.Models.PurchaseOrders;

namespace NamEcommerce.Web.Models.PurchaseOrders;

[Serializable]
public sealed class PurchaseOrderDetailsModel
{
    [ValidateNever]
    public required PurchaseOrderModel Info { get; init; }

    public bool CanModifyInfo { get; set; }
    public EditPurchaseOrderModel? ModifyInfo { get; set; }

    [ValidateNever]
    public AddPurchaseOrderItemModel? AddItemModel { get; set; }

    [ValidateNever]
    public IList<ReceivePurchaseOrderItemModel> ReceiveItemModels { get; set; } = [];

    [ValidateNever]
    public required EntityOptionListModel AvailableWarehouses { get; set; }

    [ValidateNever]
    public IList<RelatedGoodsReceiptModel> RelatedGoodsReceipts { get; set; } = [];

    [ValidateNever]
    public IList<RelatedVendorReturnModel> RelatedVendorReturns { get; set; } = [];

    public bool CanAllocateItems { get; set; }

    [ValidateNever]
    public IDictionary<Guid, IList<DirectShipAllocationForPoModel>> DirectShipAllocationsPerItem { get; set; }
        = new Dictionary<Guid, IList<DirectShipAllocationForPoModel>>();

    [ValidateNever]
    public IDictionary<Guid, IList<AllocationForPoModel>> AllocationsPerItem { get; set; }
        = new Dictionary<Guid, IList<AllocationForPoModel>>();

    [Serializable]
    public sealed record AllocationForPoModel
    {
        public Guid AllocationId { get; init; }
        public Guid OrderId { get; init; }
        public Guid OrderItemId { get; init; }
        public string OrderCode { get; init; } = string.Empty;
        public decimal AllocatedQuantity { get; init; }
        public decimal ReceivedQuantity { get; init; }
        public int Status { get; init; }
        public bool IsDirectShip { get; init; }
    }

    [Serializable]
    public sealed record DirectShipAllocationForPoModel
    {
        public Guid AllocationId { get; init; }
        public string DirectShipAddress { get; init; } = string.Empty;
        public string? DirectShipContactName { get; init; }
        public string? DirectShipContactPhone { get; init; }
        public decimal AllocatedQuantity { get; init; }
        public decimal ReceivedQuantity { get; init; }
        public int Status { get; init; }
        public int? DeliveryStatus { get; init; }
        public Guid? DeliveryNoteId { get; init; }
        public string? DeliveryNoteCode { get; init; }
    }
}

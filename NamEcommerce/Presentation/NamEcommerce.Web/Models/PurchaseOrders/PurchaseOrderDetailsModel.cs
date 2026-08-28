using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using NamEcommerce.Web.Contracts.Models.Common;
using NamEcommerce.Web.Contracts.Models.PurchaseOrders;

namespace NamEcommerce.Web.Models.PurchaseOrders;

[Serializable]
public sealed class PurchaseOrderDetailsModel
{
    public enum WorkflowStage
    {
        Ordering = 1,
        Receiving = 2,
        Allocating = 3,
        Settlement = 4
    }

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

    public WorkflowModel Workflow { get; set; } = new();

    public OrderingModel Ordering { get; set; } = new();

    public ReceivingModel Receiving { get; set; } = new();

    public ReturnModel Returns { get; set; } = new();

    public SettlementModel Settlement { get; set; } = new();

    public IList<TimelineEventModel> Timeline { get; set; } = [];

    [Serializable]
    public sealed record WorkflowModel
    {
        public WorkflowStage ActiveStage { get; init; } = WorkflowStage.Ordering;
        public string StatusText { get; init; } = string.Empty;
        public string StatusClass { get; init; } = "secondary";
        public string ReceivingStatusText { get; init; } = string.Empty;
        public string ReceivingStatusClass { get; init; } = "secondary";
        public IList<WorkflowStepModel> Steps { get; init; } = [];
    }

    [Serializable]
    public sealed record WorkflowStepModel
    {
        public required WorkflowStage Stage { get; init; }
        public required string Title { get; init; }
        public required string Icon { get; init; }
        public required string StatusText { get; init; }
        public bool IsActive { get; init; }
        public bool IsDone { get; init; }
    }

    [Serializable]
    public sealed record OrderingModel
    {
        public int ItemCount { get; init; }
        public decimal TotalQuantity { get; init; }
        public decimal Subtotal { get; init; }
        public int AllocationCount { get; init; }
        public decimal AllocatedQuantity { get; init; }
    }

    [Serializable]
    public sealed record ReceivingModel
    {
        public decimal OrderedQuantity { get; init; }
        public decimal ReceivedQuantity { get; init; }
        public decimal RemainingQuantity { get; init; }
        public int ReceiptCount { get; init; }
        public decimal ReceiptTotalQuantity { get; init; }
        public decimal? ReceiptTotalValue { get; init; }
        public bool HasPendingCosting { get; init; }
    }

    [Serializable]
    public sealed record ReturnModel
    {
        public int ReturnCount { get; init; }
        public decimal RequestedQuantity { get; init; }
        public decimal AcceptedQuantity { get; init; }
        public decimal TotalAmount { get; init; }
        public decimal AdditionalCost { get; init; }
    }

    [Serializable]
    public sealed record SettlementModel
    {
        public decimal PurchaseSubTotal { get; init; }
        public decimal PurchaseTotal { get; init; }
        public decimal ReturnTotal { get; init; }
        public decimal NetPayable { get; init; }
        public decimal DebtTotal { get; init; }
        public decimal PaidTotal { get; init; }
        public decimal RemainingDebt { get; init; }
        public int DebtCount { get; init; }
        public int PaymentCount { get; init; }
        public decimal VendorAccountBalance { get; init; }
        public IList<SettlementDebtModel> Debts { get; init; } = [];
        public IList<SettlementPaymentModel> Payments { get; init; } = [];
        public IList<SettlementCreditModel> Credits { get; init; } = [];
    }

    [Serializable]
    public sealed record SettlementPaymentModel
    {
        public required Guid Id { get; init; }
        public required string Code { get; init; }
        public decimal Amount { get; init; }
        public string PaymentMethodText { get; init; } = string.Empty;
        public string PaymentTypeText { get; init; } = string.Empty;
        public string? Note { get; init; }
        public DateTime PaidOn { get; init; }
    }

    [Serializable]
    public sealed record SettlementCreditModel
    {
        public required Guid Id { get; init; }
        public required string Code { get; init; }
        public Guid VendorReturnId { get; set; }
        public string VendorReturnCode { get; set; } = string.Empty;
        public decimal Amount { get; init; }
        public string? Note { get; init; }
        public DateTime CreatedOn { get; init; }
    }

    [Serializable]
    public sealed record SettlementDebtModel
    {
        public required Guid Id { get; init; }
        public required string Code { get; init; }
        public decimal Amount { get; init; }
        public DateTime CreatedOn { get; init; }
    }


    [Serializable]
    public sealed record TimelineEventModel
    {
        public required DateTime OccurredOn { get; init; }
        public required string Title { get; init; }
        public string? Description { get; init; }
        public required string Icon { get; init; }
        public required string Tone { get; init; }
        public WorkflowStage Stage { get; init; }
    }

    [Serializable]
    public sealed record AllocationForPoModel
    {
        public Guid AllocationId { get; init; }
        public Guid OrderId { get; init; }
        public Guid OrderItemId { get; init; }
        public string OrderCode { get; init; } = string.Empty;
        public string? CustomerName { get; init; }
        public string? CustomerPhone { get; init; }
        public string? ShippingAddress { get; init; }
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

using NamEcommerce.Domain.Shared.Enums.DeliveryNotes;
using NamEcommerce.Web.Contracts.Models.Common;
using NamEcommerce.Web.Contracts.Models.Inventory;
using NamEcommerce.Web.Contracts.Models.PurchaseOrders;

namespace NamEcommerce.Web.Models.Orders;

[Serializable]
public sealed record OrderDetailsModel
{
    public enum WorkflowStage
    {
        Order = 1,
        Preparation = 2,
        Delivery = 3,
        Settlement = 4
    }

    public enum OrderDeliverySummaryStatus
    {
        Pending = 0,
        Shipping = 1,
        PartialDelivered = 2,
        Delivered = 3
    }

    public required Guid Id { get; init; }
    public required string Code { get; init; }
    public required decimal OrderSubTotal { get; init; }
    public required decimal TotalAmount { get; init; }
    public required Guid CustomerId { get; init; }

    public string? CustomerName { get; set; }
    public string? CustomerAddress { get; set; }
    public string? CustomerPhoneNumber { get; set; }

    public decimal OrderDiscount { get; set; }
    public int Status { get; set; }
    public DateTime? CompletedOn { get; set; }

    public string? Note { get; set; }

    public DateTime? ExpectedShippingDate { get; set; }
    public string? ShippingAddress { get; set; }

    public bool CanUpdateInfo { get; init; }
    public bool CanCompleteOrder { get; set; }
    public bool CanCancelOrder { get; set; }
    public bool CanDeleteOrder { get; set; }
    public bool CanUpdateOrderItems { get; init; }
    public int FullyReceivedDirectShipCount { get; set; }
    public IList<OrderItemModel> Items { get; init; } = [];
    public bool AreAllItemsFullyCovered
    {
        get
        {
            if (Items.Count == 0)
                return false;

            foreach (var item in Items)
            {
                var totalDeliveredQty = DeliveryNotes
                    .Where(dn => dn.Status != (int)DeliveryNoteStatus.Cancelled)
                    .SelectMany(dn => dn.Items)
                    .Where(dni => dni.OrderItemId == item.Id)
                    .Sum(dni => Math.Max(0m, dni.Quantity - dni.CompensatedReturnQuantity));

                if (totalDeliveredQty < item.Quantity)
                    return false;
            }

            return true;
        }
    }

    public IList<DeliveryNoteBasicModel> DeliveryNotes { get; init; } = [];
    public IList<CustomerReturnProgressModel> CustomerReturns { get; set; } = [];
    public OrderAllocatedPurchaseOrderListModel? AllocatedPurchaseOrders { get; set; }
    public ShortageInfoModel ShortageInfo { get; set; } = new();
    public IList<DirectShipAllocationModel> DirectShipAllocations { get; set; } = [];
    public IList<ReturnWarehouseOptionModel> ReturnWarehouseOptions { get; set; } = [];
    public EntityOptionListModel? AvailableWarehouses { get; set; }
    public WorkflowModel Workflow { get; set; } = new();
    public PreparationModel Preparation { get; set; } = new();
    public DeliveryWorkflowModel DeliveryWorkflow { get; set; } = new();
    public SettlementModel Settlement { get; set; } = new();
    public IList<TimelineEventModel> Timeline { get; set; } = [];

    [Serializable]
    public sealed record DirectShipAllocationModel
    {
        public Guid AllocationId { get; init; }
        public Guid OrderItemId { get; init; }
        public string ProductName { get; init; } = string.Empty;
        public int Status { get; init; }
        public int? DeliveryStatus { get; init; }
        public decimal AllocatedQuantity { get; init; }
        public decimal ReceivedQuantity { get; init; }
        public Guid? DeliveryNoteId { get; init; }
        public string? DeliveryNoteCode { get; init; }
    }

    public DateTime CreatedOn { get; set; }

    #region Inner classes

    [Serializable]
    public sealed record ReturnWarehouseOptionModel
    {
        public required Guid Id { get; init; }
        public required string Name { get; init; }
    }

    [Serializable]
    public sealed record OrderItemModel(Guid Id)
    {
        public required Guid ProductId { get; init; }
        public required decimal Quantity { get; init; }
        public required decimal UnitPrice { get; init; }
        public string? ProductName { get; set; }
        public string? ProductPicture { get; set; }
        public decimal? ProductAvailableQty { get; set; }
        public int QuantityDecimalPlaces { get; set; }
        public decimal SubTotal => UnitPrice * Quantity;

        public decimal GetDeliveredQuantity(IList<DeliveryNoteBasicModel> deliveryNotes)
        {
            return deliveryNotes
                .SelectMany(dn => dn.Items)
                .Where(dni => dni.OrderItemId == Id)
                .Sum(dni => dni.Quantity);
        }

        public decimal GetRemainingQuantity(IList<DeliveryNoteBasicModel> deliveryNotes)
        {
            var delivered = GetDeliveredQuantity(deliveryNotes);
            return Quantity - delivered;
        }

        public int GetDeliveryNoteCount(IList<DeliveryNoteBasicModel> deliveryNotes)
        {
            return deliveryNotes.Count(dn => dn.Items.Any(dni => dni.OrderItemId == Id));
        }

        public decimal GetDeliveredToCustomerQuantity(IList<DeliveryNoteBasicModel> deliveryNotes)
        {
            return deliveryNotes
                .Where(dn => dn.Status == (int)DeliveryNoteStatus.Delivered)
                .SelectMany(dn => dn.Items)
                .Where(dni => dni.OrderItemId == Id)
                .Sum(dni => Math.Max(0m, dni.Quantity - dni.CompensatedReturnQuantity));
        }

        public decimal GetCustomerKeptQuantity(IList<DeliveryNoteBasicModel> deliveryNotes)
        {
            return deliveryNotes
                .Where(dn => dn.Status == (int)DeliveryNoteStatus.Delivered)
                .SelectMany(dn => dn.Items)
                .Where(dni => dni.OrderItemId == Id)
                .Sum(dni => dni.CustomerKeptQuantity);
        }

        public decimal GetCompensatedReturnQuantity(IList<DeliveryNoteBasicModel> deliveryNotes)
        {
            return deliveryNotes
                .SelectMany(dn => dn.Items)
                .Where(dni => dni.OrderItemId == Id)
                .Sum(dni => dni.CompensatedReturnQuantity);
        }
    }

    [Serializable]
    public sealed record DeliveryNoteBasicModel
    {
        public required Guid Id { get; init; }
        public required string Code { get; init; }
        public int Status { get; init; }
        public int SourceType { get; init; }
        public bool IsDirectShip { get; init; }
        public int DeliveryConfirmationStatus { get; init; }
        public Guid WarehouseId { get; init; }
        public string? WarehouseName { get; init; }
        public DateTime CreatedOn { get; init; }
        public DateTime? UpdatedOn { get; init; }
        public DateTime? DeliveredOn { get; init; }
        public DateTime? ConfirmedAt { get; init; }
        public string? ConfirmedNote { get; init; }
        public Guid? DeliveryProofPictureId { get; init; }
        public string? DeliveryProofPictureUrl { get; set; }
        public string? DeliveryReceiverName { get; init; }
        public string? Note { get; init; }
        public decimal TotalAmount { get; init; }
        public decimal Surcharge { get; init; }
        public string? SurchargeReason { get; init; }
        public decimal AmountToCollect { get; init; }
        public IList<DeliveryNoteItemModel> Items { get; init; } = [];
    }

    [Serializable]
    public sealed record DeliveryNoteItemModel
    {
        public required Guid OrderItemId { get; init; }
        public string ProductName { get; init; } = string.Empty;
        public required decimal Quantity { get; init; }
        public required decimal UnitPrice { get; init; }
        public required decimal SubTotal { get; init; }
        public decimal? CostAtDispatch { get; init; }
        public decimal? TotalCost => CostAtDispatch.HasValue ? CostAtDispatch.Value * Quantity : null;
        public decimal ConfirmedReturnQuantity { get; init; }
        public decimal PendingReturnQuantity { get; init; }
        public decimal CompensatedReturnQuantity { get; init; }
        public decimal ReturnedQuantity => ConfirmedReturnQuantity + PendingReturnQuantity;
        public decimal CustomerKeptQuantity => Math.Max(0m, Quantity - ReturnedQuantity);
    }

    [Serializable]
    public sealed record WorkflowModel
    {
        public WorkflowStage ActiveStage { get; set; } = WorkflowStage.Order;
        public OrderDeliverySummaryStatus DeliveryStatus { get; set; } = OrderDeliverySummaryStatus.Pending;
        public string DeliveryStatusText { get; set; } = string.Empty;
        public string DeliveryStatusClass { get; set; } = "secondary";
        public IList<WorkflowStepModel> Steps { get; set; } = [];
    }

    [Serializable]
    public sealed record WorkflowStepModel
    {
        public required WorkflowStage Stage { get; init; }
        public required string Key { get; init; }
        public required string Title { get; init; }
        public required string Icon { get; init; }
        public required string Summary { get; init; }
        public bool IsActive { get; init; }
        public bool IsComplete { get; init; }
        public bool HasWarnings { get; set; }
    }

    [Serializable]
    public sealed record PreparationModel
    {
        public IList<PreparationItemModel> Items { get; set; } = [];
        public IList<PreparationPurchaseOrderModel> PurchaseOrders { get; set; } = [];
        public bool HasShortage { get; set; }
        public decimal TotalShortageQuantity { get; set; }
    }

    [Serializable]
    public sealed record PreparationItemModel
    {
        public required Guid OrderItemId { get; init; }
        public required string ProductName { get; init; }
        public decimal OrderedQuantity { get; init; }
        public decimal AvailableQuantity { get; init; }
        public decimal ShortageQuantity { get; init; }
        public decimal IssuedQuantity { get; init; }
        public decimal DeliveredQuantity { get; init; }
        public decimal CompensatedReturnQuantity { get; init; }
        public decimal DirectShipQuantity { get; init; }
        public decimal DirectShipReceivedQuantity { get; init; }
        public string DirectShipStatusText { get; init; } = string.Empty;
        public decimal? UnitCost { get; init; }
        public decimal RemainingDeliveryQuantity => Math.Max(0, OrderedQuantity - DeliveredQuantity);
        public bool IsFullyDelivered => OrderedQuantity > 0 && RemainingDeliveryQuantity == 0;
        public IList<PreparationPurchaseOrderModel> RelatedPurchaseOrders { get; set; } = [];
    }

    [Serializable]
    public sealed record PreparationPurchaseOrderModel
    {
        public required Guid PurchaseOrderId { get; init; }
        public required string PurchaseOrderCode { get; init; }
        public required string VendorName { get; init; }
        public required string ProductName { get; init; }
        public required string StatusText { get; init; }
        public required string StatusClass { get; init; }
        public DateTime PlacedOn { get; init; }
        public DateTime? ExpectedDeliveryDate { get; init; }
        public decimal AllocatedQuantity { get; init; }
        public decimal ReceivedQuantity { get; init; }
        public decimal PendingQuantity => Math.Max(0, AllocatedQuantity - ReceivedQuantity);
        public bool IsFullyReceived => AllocatedQuantity > 0 && PendingQuantity == 0;
    }

    [Serializable]
    public sealed record DeliveryWorkflowModel
    {
        public OrderDeliverySummaryStatus Status { get; set; } = OrderDeliverySummaryStatus.Pending;
        public string StatusText { get; set; } = string.Empty;
        public string StatusClass { get; set; } = "secondary";
        public IList<DeliveryItemProgressModel> Items { get; set; } = [];
        public IList<DeliveryProgressModel> Notes { get; set; } = [];
        public IList<CustomerReturnProgressModel> Returns { get; set; } = [];
    }

    [Serializable]
    public sealed record DeliveryItemProgressModel
    {
        public required Guid OrderItemId { get; init; }
        public required string ProductName { get; init; }
        public decimal OrderedQuantity { get; init; }
        public decimal AvailableQuantity { get; init; }
        public decimal IssuedQuantity { get; init; }
        public decimal DeliveredQuantity { get; init; }
        public decimal DirectShipQuantity { get; init; }
        public decimal DirectShipReceivedQuantity { get; init; }
        public string DirectShipStatusText { get; init; } = string.Empty;
        public bool IsFullyDelivered => OrderedQuantity > 0 && DeliveredQuantity >= OrderedQuantity;
        public bool HasDeliveryGap => IssuedQuantity > DeliveredQuantity;
        public decimal DeliveryGapQuantity => Math.Max(0m, IssuedQuantity - DeliveredQuantity);
    }

    [Serializable]
    public sealed record DeliveryProgressModel
    {
        public required Guid DeliveryNoteId { get; init; }
        public required string Code { get; init; }
        public required int Status { get; init; }
        public required string StatusText { get; init; }
        public required string StatusClass { get; init; }
        public required bool IsDirectShip { get; init; }
        public required int DeliveryConfirmationStatus { get; init; }
        public required Guid WarehouseId { get; init; }
        public string? WarehouseName { get; init; }
        public string? Note { get; init; }
        public DateTime CreatedOn { get; init; }
        public DateTime? UpdatedOn { get; init; }
        public DateTime? DeliveredOn { get; init; }
        public DateTime? ConfirmedAt { get; init; }
        public string? ConfirmedNote { get; init; }
        public string? DeliveryProofPictureUrl { get; init; }
        public string? DeliveryReceiverName { get; init; }
        public decimal TotalQuantity { get; init; }
        public decimal CompensatedReturnQuantity { get; init; }
        public decimal ReturnedQuantity { get; init; }
        public decimal TotalAmount { get; init; }
        public decimal Surcharge { get; init; }
        public string? SurchargeReason { get; init; }
        public decimal AmountToCollect { get; init; }
        public IList<DeliveryNoteItemModel> Items { get; init; } = [];
    }

    [Serializable]
    public sealed record CustomerReturnProgressModel
    {
        public required Guid Id { get; init; }
        public required string Code { get; init; }
        public required Guid DeliveryNoteId { get; init; }
        public required string DeliveryNoteCode { get; init; }
        public int Status { get; init; }
        public string StatusText { get; init; } = string.Empty;
        public string StatusClass { get; init; } = "secondary";
        public DateTime ReturnDate { get; init; }
        public DateTime? ConfirmedOn { get; init; }
        public DateTime CreatedOn { get; init; }
        public DateTime? UpdatedOn { get; init; }
        public decimal AdditionalCost { get; init; }
        public decimal GrossRefundAmount { get; init; }
        public decimal NetRefundAmount { get; init; }
        public bool CompensateInNextDelivery { get; init; }
        public decimal CompensatedQuantity { get; init; }
        public string? Note { get; init; }
        public IList<CustomerReturnItemProgressModel> Items { get; init; } = [];
    }

    [Serializable]
    public sealed record CustomerReturnItemProgressModel
    {
        public required string ProductName { get; init; }
        public decimal RequestedQuantity { get; init; }
        public decimal AcceptedQuantity { get; init; }
        public decimal ReturnUnitPrice { get; init; }
        public decimal AcceptedTotal { get; init; }
    }

    [Serializable]
    public sealed record SettlementModel
    {
        public decimal Revenue { get; set; }
        public decimal TotalCost { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal Profit { get; set; }
        public bool IsProfitFinal { get; set; }
        public decimal ReturnGrossAmount { get; set; }
        public decimal ReturnAdditionalCost { get; set; }
        public decimal ReturnNetRefundAmount { get; set; }
        public decimal ReturnCompensatedQuantity { get; set; }
        public IList<SettlementDebtModel> Debts { get; set; } = [];
        public IList<SettlementExpenseModel> Expenses { get; set; } = [];
        public IList<SettlementCostModel> Costs { get; set; } = [];
        public IList<SettlementReturnModel> Returns { get; set; } = [];
        public decimal TotalDebtAmount => Debts.Sum(item => item.TotalAmount);
        public decimal TotalPaidAmount => Debts.Sum(item => item.PaidAmount);
        public decimal TotalRemainingAmount => Debts.Sum(item => item.RemainingAmount);
    }

    [Serializable]
    public sealed record SettlementDebtModel
    {
        public required Guid Id { get; init; }
        public required string Code { get; init; }
        public required string DeliveryNoteCode { get; init; }
        public decimal TotalAmount { get; init; }
        public decimal PaidAmount { get; init; }
        public decimal RemainingAmount { get; init; }
        public string StatusText { get; init; } = string.Empty;
        public DateTime? DueDate { get; init; }
        public DateTime CreatedOn { get; init; }
    }

    [Serializable]
    public sealed record SettlementExpenseModel
    {
        public required Guid Id { get; init; }
        public required string Title { get; init; }
        public string? Description { get; init; }
        public string ExpenseTypeText { get; init; } = string.Empty;
        public decimal Amount { get; init; }
        public DateTime IncurredDate { get; init; }
    }

    [Serializable]
    public sealed record SettlementCostModel
    {
        public required Guid DeliveryNoteId { get; init; }
        public required string DeliveryNoteCode { get; init; }
        public required string ProductName { get; init; }
        public decimal Quantity { get; init; }
        public decimal? UnitCost { get; init; }
        public decimal? TotalCost { get; init; }
        public DateTime DispatchedOn { get; init; }
        public bool IsDirectShip { get; init; }
    }

    [Serializable]
    public sealed record SettlementReturnModel
    {
        public required Guid Id { get; init; }
        public required string Code { get; init; }
        public required string DeliveryNoteCode { get; init; }
        public string StatusText { get; init; } = string.Empty;
        public string StatusClass { get; init; } = "secondary";
        public decimal GrossRefundAmount { get; init; }
        public decimal AdditionalCost { get; init; }
        public decimal NetRefundAmount { get; init; }
        public decimal CompensatedQuantity { get; init; }
        public DateTime ReturnDate { get; init; }
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

    #endregion
}

using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Models.DeliveryNotes;

public sealed class DeliveryRunSearchModel
{
    public string? Keywords { get; set; }
    public Guid? AssignedDeliveryUserId { get; set; }
    public int? Status { get; set; }
    public int? PageNumber { get; set; }
    public int? PageSize { get; set; }
}

public sealed class DeliveryRunListModel
{
    public string? Keywords { get; set; }
    public Guid? AssignedDeliveryUserId { get; set; }
    public int? Status { get; set; }
    public EntityOptionListModel? AvailableDeliveryUsers { get; set; }
    public required IPagedDataModel<DeliveryRunListItemModel> Data { get; init; }
}

public sealed record DeliveryRunListItemModel
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }
    public required Guid AssignedDeliveryUserId { get; init; }
    public required string AssignedDeliveryFullName { get; init; }
    public required int Status { get; init; }
    public required string StatusName { get; init; }
    public int ItemCount { get; init; }
    public int DeliveredItemCount { get; init; }
    public bool IsDeliveryCompleted { get; init; }
    public decimal AmountToCollect { get; init; }
    public DateTime? DriverCachedOnUtc { get; init; }
    public bool PaperManifestIssued { get; init; }
    public DateTime? HandedOverOnUtc { get; init; }
    public DateTime? CashHandoverConfirmedOnUtc { get; init; }
    public decimal? CashHandoverAmount { get; init; }
    public required DateTime CreatedOnUtc { get; init; }
}

public sealed record DeliveryMobileRunListItemModel
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }
    public required int Status { get; init; }
    public required string StatusName { get; init; }
    public int ItemCount { get; init; }
    public int DeliveredItemCount { get; init; }
    public bool IsDeliveryCompleted { get; init; }
    public decimal AmountToCollect { get; init; }
    public DateTime? DriverCachedOnUtc { get; init; }
    public bool PaperManifestIssued { get; init; }
    public DateTime? HandedOverOnUtc { get; init; }
    public required DateTime CreatedOnUtc { get; init; }
    public IList<DeliveryRunItemModel> Items { get; init; } = [];
}

public sealed class CreateDeliveryRunModel
{
    public Guid AssignedDeliveryUserId { get; set; }
    public IList<Guid> DeliveryNoteIds { get; set; } = [];
    public string? Note { get; set; }
    public EntityOptionListModel? AvailableDeliveryUsers { get; set; }
    public IList<DeliveryRunCandidateDeliveryNoteModel> AvailableDeliveryNotes { get; set; } = [];
}

public sealed record DeliveryRunCandidateDeliveryNoteModel
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }
    public string? OrderCode { get; init; }
    public required string CustomerName { get; init; }
    public string? ShippingPhoneNumber { get; init; }
    public required string ShippingAddress { get; init; }
    public string? AssignedDeliveryFullName { get; init; }
    public required string ProductSummary { get; init; }
    public decimal TotalQuantity { get; init; }
    public decimal AmountToCollect { get; init; }
    public DateTime CreatedOnUtc { get; init; }
}

[Serializable]
public sealed class DeliveryRunDetailsModel
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;

    public int Status { get; init; }
    public string DeliveryStatusInfo { get; init; } = string.Empty;

    public bool IsDeliveryCompleted { get; init; }

    public IList<DeliveryRunItemModel> Items { get; init; } = [];
    public IList<DeliveryRunWarehousePickGroupModel> PickingManifest { get; init; } = [];

    public Guid AssignedDeliveryUserId { get; init; }
    public string AssignedDeliveryUsername { get; init; } = string.Empty;
    public string AssignedDeliveryFullName { get; init; } = string.Empty;

    public int DeliveredItemCount { get; init; }

    public Guid? PreparedByUserId { get; init; }
    public DateTime? PreparedOn { get; init; }

    public Guid? HandedOverByUserId { get; init; }
    public DateTime? HandedOverOn { get; init; }

    public DateTime? DriverCachedOn { get; init; }
    public string? DriverCacheDeviceId { get; init; }

    public bool PaperManifestIssued { get; init; }
    public DateTime? PaperManifestIssuedOnUtc { get; init; }

    public Guid? CashHandoverConfirmedByUserId { get; init; }
    public string? CashHandoverConfirmedByUsername { get; init; }
    public string? CashHandoverConfirmedByFullName { get; init; }
    public DateTime? CashHandoverConfirmedOn { get; init; }
    public decimal? CashHandoverAmount { get; init; }
    public string? CashHandoverNote { get; init; }

    public string? Note { get; init; }

    public DateTime CreatedOn { get; init; }
    public DateTime? UpdatedOn { get; init; }

    public bool CanManageDeliveryRun { get; set; }
    public bool CanConfirmDeliveryRunCashHandover { get; set; }
    public bool CanIssuePaperManifest { get; set; }
    public bool CanCloseIfDelivered { get; set; }
    public bool CanCancel { get; set; }
    public bool CanReviewCashCollected { get; set; }
    public bool CanReconcileDeliveryRunItems { get; set; }
}

public sealed record DeliveryRunWarehousePickGroupModel
{
    public required Guid WarehouseId { get; init; }
    public required string WarehouseName { get; init; }
    public bool Confirmed { get; init; }
    public string? ConfirmedByFullName { get; init; }
    public DateTime? ConfirmedOnUtc { get; init; }
    public IList<DeliveryRunProductLineModel> Products { get; init; } = [];
}

public sealed record DeliveryRunItemModel
{
    public required Guid Id { get; init; }
    public required Guid DeliveryNoteId { get; init; }
    public required string DeliveryNoteCode { get; init; }
    public string? OrderCode { get; init; }
    public required Guid CustomerId { get; init; }
    public required string CustomerName { get; init; }
    public string? CustomerPhone { get; init; }
    public string? ShippingPhoneNumber { get; init; }
    public required string ShippingAddress { get; init; }
    public decimal AmountToCollect { get; init; }
    public DateTime? AmountToCollectOverriddenAt { get; init; }
    public decimal? CashCollectedAmount { get; init; }
    public string? ReceiverName { get; init; }
    public Guid? DeliveryProofPictureId { get; init; }
    public int? DeliveryNoteStatus { get; init; }
    public DateTime? DeliveredOnUtc { get; init; }

    public int SettlementApproval { get; init; }
    public decimal? ProposedAmountToCollect { get; init; }
    public decimal? ApprovedAmountToCollect { get; init; }
    public string? SettlementReason { get; init; }
    public string? SettlementAdminNote { get; init; }

    public IList<DeliveryRunProductItemModel> ProductItems { get; init; } = [];
    public IList<DeliveryRunProductLineModel> ProductLines { get; init; } = [];
    public IList<DeliveryRunSettlementProductItemModel> SettlementItems { get; init; } = [];
    public IList<DeliveryRunSettlementProductLineModel> SettlementProductLines { get; init; } = [];
}

public sealed record DeliveryRunProductLineModel
{
    public required Guid ProductId { get; init; }
    public required string ProductName { get; init; }
    public decimal TotalQuantity { get; init; }
    public string? UnitMeasurement { get; set; }
    public int QuantityDecimalPlaces { get; init; } = 2;
}

public sealed record DeliveryRunProductItemModel
{
    public required Guid DeliveryNoteItemId { get; init; }
    public required Guid ProductId { get; init; }
    public required string ProductName { get; init; }
    public string? WarehouseName { get; init; }
    public decimal Quantity { get; init; }
    public string? UnitMeasurement { get; set; }
    public decimal UnitPrice { get; init; }
    public decimal SubTotal { get; init; }
    public int QuantityDecimalPlaces { get; init; } = 2;
}

public sealed record DeliveryRunSettlementProductItemModel
{
    public required Guid DeliveryNoteItemId { get; init; }
    public required Guid ProductId { get; init; }
    public required string ProductName { get; init; }
    public decimal Quantity { get; init; }
    public decimal AcceptedQuantity { get; init; }
    public decimal RejectedQuantity { get; init; }
    public string? UnitMeasurement { get; set; }
    public int QuantityDecimalPlaces { get; init; } = 2;
}

public sealed record DeliveryRunSettlementProductLineModel
{
    public required Guid ProductId { get; init; }
    public required string ProductName { get; init; }
    public decimal Quantity { get; init; }
    public decimal AcceptedQuantity { get; init; }
    public decimal RejectedQuantity { get; init; }
    public string? UnitMeasurement { get; set; }
    public int QuantityDecimalPlaces { get; init; } = 2;
}

public sealed class DeliveryMobileIndexModel
{
    public required Guid CurrentUserId { get; init; }
    public required string CurrentUserFullName { get; init; }
    public bool ShowCompleted { get; init; }
    public IList<DeliveryMobileRunListItemModel> Runs { get; init; } = [];
}

public sealed class DeliveryMobileRunModel
{
    public required Guid CurrentUserId { get; init; }
    public required string CurrentUserFullName { get; init; }
    public required DeliveryRunDetailsModel Run { get; init; }
}

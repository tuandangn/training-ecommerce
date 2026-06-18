using NamEcommerce.Web.Contracts.Models.Common;
using NamEcommerce.Web.Contracts.Models.Inventory;

namespace NamEcommerce.Web.Contracts.Models.DeliveryNotes;

public sealed class DeliveryNoteDetailsModel
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    
    public Guid OrderId { get; set; }
    public string OrderCode { get; set; } = string.Empty;

    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerPhone { get; set; }
    public string? CustomerAddress { get; set; }
    public string ShippingAddress { get; set; } = string.Empty;
    public string? ShippingPhoneNumber { get; set; }

    public string? WarehouseName { get; set; }
    public EntityOptionListModel? AvailableWarehouses { get; set; }
    public Guid? AssignedDeliveryUserId { get; set; }
    public string? AssignedDeliveryUsername { get; set; }
    public string? AssignedDeliveryFullName { get; set; }
    public DateTime? AssignedDeliveryOnUtc { get; set; }
    public EntityOptionListModel? AvailableDeliveryUsers { get; set; }

    public bool ShowPrice { get; set; }
    public string? Note { get; set; }
    
    public int Status { get; set; }
    public int SourceType { get; set; }
    public bool IsDirectShip { get; set; }
    public int DeliveryConfirmationStatus { get; set; }
    public string StatusName { get; set; } = string.Empty;
    
    public DateTime CreatedOnUtc { get; set; }
    public DateTime? DeliveredOnUtc { get; set; }
    public Guid? DeliveryProofPictureId { get; set; }
    public string? DeliveryProofPictureUrl { get; set; }
    public string? DeliveryReceiverName { get; set; }
    public double? DeliveryLatitude { get; set; }
    public double? DeliveryLongitude { get; set; }
    public string? DeliveryLocationAddress { get; set; }
    public string? DeliveryCompletionNote { get; set; }
    public string? DeliveryCompletionSource { get; set; }
    public string? DeliveryCompletionIdempotencyKey { get; set; }
    public decimal? DeliveryCashCollectedAmount { get; set; }

    public decimal TotalAmount { get; set; }
    public decimal Surcharge { get; set; }
    public string? SurchargeReason { get; set; }
    public decimal AmountAlreadyPaidForOrder { get; set; }
    public decimal AmountToCollect { get; set; }
    public string? CustomerPortalUrl { get; set; }
    public string? CustomerPortalQrCodeSvg { get; set; }

    public string? RejectionReason { get; set; }
    public string? ReturnedToWarehouseName { get; set; }

    public int SettlementApproval { get; set; }
    public decimal? ProposedAmountToCollect { get; set; }
    public decimal? ApprovedAmountToCollect { get; set; }
    public string? SettlementReason { get; set; }
    public string? SettlementAdminNote { get; set; }
    public IList<DeliveryNoteSettlementLineModel> SettlementItems { get; set; } = [];

    public IList<DeliveryNoteItemModel> Items { get; set; } = [];
    public ShortageInfoModel ShortageInfo { get; set; } = new();
    public DeliveryRunInfoModel? DeliveryRunInfo { get; set; }
}

public sealed class DeliveryNoteSettlementLineModel
{
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal AcceptedQuantity { get; set; }
    public decimal RejectedQuantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string? RejectReason { get; set; }
    public int QuantityDecimalPlaces { get; set; } = 2;
}

public sealed class DeliveryRunInfoModel
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public int Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public string AssignedDeliveryFullName { get; set; } = string.Empty;
    public DateTime? HandedOverOnUtc { get; set; }
    public DateTime CreatedOnUtc { get; set; }
}

public sealed class DeliveryNoteItemModel
{
    public Guid Id { get; set; }
    public Guid WarehouseId { get; set; }
    public string? WarehouseName { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public int QuantityDecimalPlaces { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal SubTotal { get; set; }

    /// <summary>Tổng đã trả (Confirmed).</summary>
    public decimal ReturnedQuantity { get; set; }
    public string? RejectReason { get; set; }

    /// <summary>Số đang giữ trong VR Draft/Inspecting.</summary>
    public decimal PendingReturnQuantity { get; set; }
    public decimal CompensatedReturnQuantity { get; set; }
}

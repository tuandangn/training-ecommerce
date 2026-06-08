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
    public required string ShippingAddress { get; init; }
    public decimal AmountToCollect { get; init; }
    public DateTime CreatedOnUtc { get; init; }
}

public sealed class DeliveryRunDetailsModel
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }
    public required Guid AssignedDeliveryUserId { get; init; }
    public required string AssignedDeliveryUsername { get; init; }
    public required string AssignedDeliveryFullName { get; init; }
    public required int Status { get; init; }
    public required string StatusName { get; init; }
    public Guid? PreparedByUserId { get; init; }
    public DateTime? PreparedOnUtc { get; init; }
    public Guid? HandedOverByUserId { get; init; }
    public DateTime? HandedOverOnUtc { get; init; }
    public DateTime? DriverCachedOnUtc { get; init; }
    public string? DriverCacheDeviceId { get; init; }
    public bool PaperManifestIssued { get; init; }
    public DateTime? PaperManifestIssuedOnUtc { get; init; }
    public Guid? CashHandoverConfirmedByUserId { get; init; }
    public string? CashHandoverConfirmedByUsername { get; init; }
    public string? CashHandoverConfirmedByFullName { get; init; }
    public DateTime? CashHandoverConfirmedOnUtc { get; init; }
    public decimal? CashHandoverAmount { get; init; }
    public string? CashHandoverNote { get; init; }
    public string? Note { get; init; }
    public required DateTime CreatedOnUtc { get; init; }
    public DateTime? UpdatedOnUtc { get; init; }
    public IList<DeliveryRunItemModel> Items { get; init; } = [];
}

public sealed record DeliveryRunItemModel
{
    public required Guid Id { get; init; }
    public required Guid DeliveryNoteId { get; init; }
    public required string DeliveryNoteCode { get; init; }
    public string? OrderCode { get; init; }
    public required string CustomerName { get; init; }
    public string? CustomerPhone { get; init; }
    public required string ShippingAddress { get; init; }
    public decimal AmountToCollect { get; init; }
    public decimal? CashCollectedAmount { get; init; }
    public string? ReceiverName { get; init; }
    public Guid? DeliveryProofPictureId { get; init; }
    public int? DeliveryNoteStatus { get; init; }
    public DateTime? DeliveredOnUtc { get; init; }
    public IList<DeliveryRunProductItemModel> ProductItems { get; init; } = [];
}

public sealed record DeliveryRunProductItemModel
{
    public required string ProductName { get; init; }
    public decimal Quantity { get; init; }
}

public sealed class DeliveryMobileIndexModel
{
    public required Guid CurrentUserId { get; init; }
    public required string CurrentUserFullName { get; init; }
    public IList<DeliveryMobileRunListItemModel> Runs { get; init; } = [];
}

public sealed class DeliveryMobileRunModel
{
    public required Guid CurrentUserId { get; init; }
    public required string CurrentUserFullName { get; init; }
    public required DeliveryRunDetailsModel Run { get; init; }
}

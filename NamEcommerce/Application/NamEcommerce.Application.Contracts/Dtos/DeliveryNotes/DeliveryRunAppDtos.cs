using NamEcommerce.Application.Contracts.Dtos.Common;

namespace NamEcommerce.Application.Contracts.Dtos.DeliveryNotes;

[Serializable]
public sealed record DeliveryRunAppDto
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }
    public required Guid AssignedDeliveryUserId { get; init; }
    public required string AssignedDeliveryUsername { get; init; }
    public required string AssignedDeliveryFullName { get; init; }
    public required int Status { get; init; }
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
    public IList<DeliveryRunItemAppDto> Items { get; init; } = [];
    public IList<DeliveryRunWarehousePickAppDto> WarehousePicks { get; init; } = [];

    public bool CanIssuePaperManifest { get; set; }
    public bool CanCloseIfDelivered { get; set; }
    public bool CanCancel { get; set; }
    public bool CanReviewCashCollected { get; set; }
    public bool CanReconcileDeliveryRunItems { get; set; }
}

[Serializable]
public sealed record DeliveryRunWarehousePickAppDto
{
    public required Guid WarehouseId { get; init; }
    public string? ConfirmedByFullName { get; init; }
    public DateTime ConfirmedOnUtc { get; init; }
}

[Serializable]
public sealed record DeliveryRunListAppDto
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }
    public required Guid AssignedDeliveryUserId { get; init; }
    public required string AssignedDeliveryFullName { get; init; }
    public required int Status { get; init; }
    public int ItemCount { get; init; }
    public decimal AmountToCollect { get; init; }
    public DateTime? DriverCachedOnUtc { get; init; }
    public bool PaperManifestIssued { get; init; }
    public DateTime? HandedOverOnUtc { get; init; }
    public DateTime? CashHandoverConfirmedOnUtc { get; init; }
    public decimal? CashHandoverAmount { get; init; }
    public required DateTime CreatedOnUtc { get; init; }
}

[Serializable]
public sealed record DeliveryRunItemAppDto
{
    public required Guid Id { get; init; }
    public required Guid DeliveryNoteId { get; init; }
    public required string DeliveryNoteCode { get; init; }
    public string? OrderCode { get; init; }
    public required string CustomerName { get; init; }
    public string? ShippingPhoneNumber { get; init; }
    public required string ShippingAddress { get; init; }
    public decimal AmountToCollect { get; init; }
}

[Serializable]
public sealed record ConfirmDeliveryRunCashHandoverAppDto
{
    public required Guid DeliveryRunId { get; init; }
    public decimal Amount { get; init; }
    public string? Note { get; init; }

    public (bool valid, string? errorMessage) Validate()
    {
        if (DeliveryRunId == Guid.Empty)
            return (false, "Error.DeliveryRunRequired");
        if (Amount < 0)
            return (false, "Error.CashHandoverAmountCannotBeNegative");

        return (true, null);
    }
}

[Serializable]
public sealed record UpdateDeliveryRunDeliveredNoteCashCollectedAppDto
{
    public required Guid DeliveryRunId { get; init; }
    public required Guid DeliveryNoteId { get; init; }
    public decimal CashCollectedAmount { get; init; }

    public (bool valid, string? errorMessage) Validate()
    {
        if (DeliveryRunId == Guid.Empty)
            return (false, "Error.DeliveryRunRequired");
        if (DeliveryNoteId == Guid.Empty)
            return (false, "Error.DeliveryNoteRequired");
        if (CashCollectedAmount < 0)
            return (false, "Error.CashCollectedAmountCannotBeNegative");

        return (true, null);
    }
}

[Serializable]
public sealed record CreateDeliveryRunAppDto
{
    public required Guid AssignedDeliveryUserId { get; init; }
    public required IList<Guid> DeliveryNoteIds { get; init; } = [];
    public string? Note { get; init; }

    public (bool valid, string? errorMessage) Validate()
    {
        if (AssignedDeliveryUserId == Guid.Empty)
            return (false, "Error.DeliveryUserRequired");
        if (DeliveryNoteIds is null || DeliveryNoteIds.Count == 0)
            return (false, "Error.DeliveryRunItemsRequired");
        if (DeliveryNoteIds.Any(id => id == Guid.Empty))
            return (false, "Error.DeliveryNoteRequired");
        if (DeliveryNoteIds.Distinct().Count() != DeliveryNoteIds.Count)
            return (false, "Error.DeliveryRunDuplicateDeliveryNote");

        return (true, null);
    }
}

[Serializable]
public sealed record CreateDeliveryRunResultAppDto : CommonActionResultDto
{
    public Guid? CreatedId { get; init; }
}

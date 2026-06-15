using NamEcommerce.Domain.Shared.Enums.DeliveryNotes;
using NamEcommerce.Domain.Shared.Exceptions;

namespace NamEcommerce.Domain.Shared.Dtos.DeliveryNotes;

[Serializable]
public sealed record DeliveryRunDto
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }
    public required Guid AssignedDeliveryUserId { get; init; }
    public required string AssignedDeliveryUsername { get; init; }
    public required string AssignedDeliveryFullName { get; init; }
    public required DeliveryRunStatus Status { get; init; }
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
    public IList<DeliveryRunItemDto> Items { get; init; } = [];
}

[Serializable]
public sealed record ConfirmDeliveryRunCashHandoverDto
{
    public required Guid DeliveryRunId { get; init; }
    public decimal Amount { get; init; }
    public string? Note { get; init; }

    public void Verify()
    {
        if (DeliveryRunId == Guid.Empty)
            throw new NamEcommerceDomainException("Error.DeliveryRunRequired");
        if (Amount < 0)
            throw new NamEcommerceDomainException("Error.CashHandoverAmountCannotBeNegative");
    }
}

[Serializable]
public sealed record UpdateDeliveryRunDeliveredNoteCashCollectedDto
{
    public required Guid DeliveryRunId { get; init; }
    public required Guid DeliveryNoteId { get; init; }
    public decimal CashCollectedAmount { get; init; }

    public void Verify()
    {
        if (DeliveryRunId == Guid.Empty)
            throw new NamEcommerceDomainException("Error.DeliveryRunRequired");
        if (DeliveryNoteId == Guid.Empty)
            throw new NamEcommerceDomainException("Error.DeliveryNoteRequired");
        if (CashCollectedAmount < 0)
            throw new NamEcommerceDomainException("Error.CashCollectedAmountCannotBeNegative");
    }
}

[Serializable]
public sealed record DeliveryRunItemDto
{
    public required Guid Id { get; init; }
    public required Guid DeliveryRunId { get; init; }
    public required Guid DeliveryNoteId { get; init; }
    public required string DeliveryNoteCode { get; init; }
    public string? OrderCode { get; init; }
    public required string CustomerName { get; init; }
    public string? ShippingPhoneNumber { get; init; }
    public required string ShippingAddress { get; init; }
    public decimal AmountToCollect { get; init; }
}

[Serializable]
public sealed record CreateDeliveryRunDto
{
    public required Guid AssignedDeliveryUserId { get; init; }
    public required string AssignedDeliveryUsername { get; init; }
    public required string AssignedDeliveryFullName { get; init; }
    public required IList<Guid> DeliveryNoteIds { get; init; } = [];
    public string? Note { get; init; }

    public void Verify()
    {
        if (AssignedDeliveryUserId == Guid.Empty)
            throw new NamEcommerceDomainException("Error.DeliveryUserRequired");
        if (string.IsNullOrWhiteSpace(AssignedDeliveryUsername))
            throw new NamEcommerceDomainException("Error.DeliveryUsernameRequired");
        if (string.IsNullOrWhiteSpace(AssignedDeliveryFullName))
            throw new NamEcommerceDomainException("Error.DeliveryFullNameRequired");
        if (DeliveryNoteIds is null || DeliveryNoteIds.Count == 0)
            throw new NamEcommerceDomainException("Error.DeliveryRunItemsRequired");
        if (DeliveryNoteIds.Any(id => id == Guid.Empty))
            throw new NamEcommerceDomainException("Error.DeliveryNoteRequired");
        if (DeliveryNoteIds.Distinct().Count() != DeliveryNoteIds.Count)
            throw new NamEcommerceDomainException("Error.DeliveryRunDuplicateDeliveryNote");
    }
}

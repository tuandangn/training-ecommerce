using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Commands.Models.DeliveryNotes;

[Serializable]
public sealed class CompleteMobileDeliveryNoteCommand : ICommand<CommonActionResultModel>
{
    public Guid DeliveryRunId { get; init; }
    public Guid DeliveryNoteId { get; init; }
    public Guid PictureId { get; init; }
    public string? ReceiverName { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public string? LocationAddress { get; init; }
    public string? Note { get; init; }
    public string? IdempotencyKey { get; init; }
    public decimal? CashCollectedAmount { get; init; }
    public IList<CompleteMobileDeliveryNoteItemCommand> Items { get; init; } = [];
}

[Serializable]
public sealed class CompleteMobileDeliveryNoteItemCommand
{
    public Guid DeliveryNoteItemId { get; init; }
    public decimal ReturnedQuantity { get; init; }
    public string? RejectReason { get; init; }
}

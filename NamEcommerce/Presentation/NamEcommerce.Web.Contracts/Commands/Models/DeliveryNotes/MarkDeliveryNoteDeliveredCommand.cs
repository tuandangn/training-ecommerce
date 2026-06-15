
namespace NamEcommerce.Web.Contracts.Commands.Models.DeliveryNotes;

public sealed record MarkDeliveryNoteDeliveredResult(bool Success, string? ErrorMessage = null) : ICommandResult;

public sealed class MarkDeliveryNoteDeliveredCommand : ICommand<MarkDeliveryNoteDeliveredResult>
{
    public Guid DeliveryNoteId { get; init; }
    public string? ReceiverName { get; init; }
    public decimal AgreedCustomerCharge { get; init; }
    public string? AgreedCustomerChargeReason { get; init; }
    public bool CompensateInNextDelivery { get; init; }
    public decimal? CashCollectedAmount { get; init; }
    public IList<MarkDeliveryNoteDeliveredItemCommand> Items { get; init; } = [];
    
    public IList<Guid> PictureIds { get; init; } = [];
}

public sealed class MarkDeliveryNoteDeliveredItemCommand
{
    public required Guid DeliveryNoteItemId { get; init; }
    public decimal AcceptedQuantity { get; init; }
    public decimal RejectedQuantity { get; init; }
    public string? RejectReason { get; init; }
}

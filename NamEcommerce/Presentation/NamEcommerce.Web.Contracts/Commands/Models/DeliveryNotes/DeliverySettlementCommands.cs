using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Commands.Models.DeliveryNotes;

[Serializable]
public sealed class RequestDeliverySettlementCommand : ICommand<CommonActionResultModel>
{
    public Guid DeliveryRunId { get; init; }
    public Guid DeliveryNoteId { get; init; }
    public Guid PictureId { get; init; }
    public string? ReceiverName { get; init; }
    public string? Reason { get; init; }
    public decimal? ProposedAmountToCollect { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public string? LocationAddress { get; init; }
    public Guid? RequestedByUserId { get; init; }
    public IList<CompleteMobileDeliveryNoteItemCommand> Items { get; init; } = [];
}

[Serializable]
public sealed class ApproveDeliverySettlementCommand : ICommand<CommonActionResultModel>
{
    public Guid DeliveryNoteId { get; init; }
    public decimal ApprovedAmountToCollect { get; init; }
    public decimal AgreedCustomerCharge { get; init; }
    public string? AgreedCustomerChargeReason { get; init; }
    public string? AdminNote { get; init; }
    public Guid? ApprovedByUserId { get; init; }
}

[Serializable]
public sealed class RejectDeliverySettlementCommand : ICommand<CommonActionResultModel>
{
    public Guid DeliveryNoteId { get; init; }
    public required string Reason { get; init; }
    public Guid? ApprovedByUserId { get; init; }
}

[Serializable]
public sealed class AdminUpdateAmountToCollectCommand : ICommand<CommonActionResultModel>
{
    public Guid DeliveryNoteId { get; init; }
    public decimal NewAmount { get; init; }
    public string? Note { get; init; }
    public Guid? AdminUserId { get; init; }
}

[Serializable]
public sealed class CompleteApprovedDeliveryCommand : ICommand<CommonActionResultModel>
{
    public Guid DeliveryRunId { get; init; }
    public Guid DeliveryNoteId { get; init; }
    public Guid? PictureId { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public string? LocationAddress { get; init; }
    public string? IdempotencyKey { get; init; }
}

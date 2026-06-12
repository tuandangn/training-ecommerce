using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Commands.Models.PurchaseOrders;

public sealed class ConfirmDirectShipDeliveryCommand : ICommand<CommonActionResultModel>
{
    public required Guid DeliveryNoteId { get; init; }
    public required DateTime ConfirmedAtUtc { get; init; }
    public string? Note { get; init; }
}

public sealed class RejectDirectShipDeliveryCommand : ICommand<CommonActionResultModel>
{
    public required Guid DeliveryNoteId { get; init; }
    public required Guid ReturnWarehouseId { get; init; }
    public required string Reason { get; init; }
}

public sealed class UpdateDirectShipAddressCommand : ICommand<CommonActionResultModel>
{
    public required Guid AllocationId { get; init; }
    public required string NewAddress { get; init; }
    public string? NewContactName { get; init; }
    public string? NewContactPhone { get; init; }
    public string? Reason { get; init; }
}

public sealed class MarkAllocationAsDirectShipCommand : ICommand<CommonActionResultModel>
{
    public required Guid AllocationId { get; init; }
    public required string Address { get; init; }
    public string? ContactName { get; init; }
    public string? ContactPhone { get; init; }
    public int Priority { get; init; }
}

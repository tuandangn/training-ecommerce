using MediatR;
using NamEcommerce.Web.Contracts.Models.PurchaseOrders;

namespace NamEcommerce.Web.Contracts.Commands.Models.PurchaseOrders;

[Serializable]
public sealed class CheckExistingDraftsCommand : IRequest<ExistingDraftPurchaseOrdersResultModel>
{
    public IList<Guid> VendorIds { get; init; } = [];
}

[Serializable]
public sealed class CheckRelatedPurchaseOrdersCommand : IRequest<RelatedPurchaseOrdersResultModel>
{
    public IList<Guid> VendorIds { get; init; } = [];
    public IList<Guid> ProductIds { get; init; } = [];
}

[Serializable]
public sealed class CreatePurchaseOrdersFromShortageCommand : IRequest<CreatePurchaseOrdersFromShortageResultModel>
{
    public IList<CreatePurchaseOrderFromShortageGroupCommand> Groups { get; init; } = [];
}

[Serializable]
public sealed class CreatePurchaseOrderFromShortageGroupCommand
{
    public Guid? VendorId { get; init; }
    public Guid? WarehouseId { get; init; }
    public Guid? MergeIntoExistingPoId { get; init; }
    public DateTime? ExpectedDeliveryDate { get; init; }
    public string? Note { get; init; }
    public IList<CreatePurchaseOrderFromShortageItemCommand> Items { get; init; } = [];
}

[Serializable]
public sealed class DirectShipInfoCommand
{
    public string Address { get; init; } = string.Empty;
    public string? ContactName { get; init; }
    public string? ContactPhone { get; init; }
    public int Priority { get; init; }
}

[Serializable]
public sealed class CreatePurchaseOrderFromShortageItemCommand
{
    public Guid OrderItemId { get; init; }
    public Guid ProductId { get; init; }
    public decimal Quantity { get; init; }
    public decimal UnitCost { get; init; }
    public string? Note { get; init; }
    public DirectShipInfoCommand? DirectShipInfo { get; init; }
    public IList<CreatePurchaseOrderFromShortageActionCommand> Actions { get; init; } = [];
}

[Serializable]
public sealed class CreatePurchaseOrderFromShortageActionCommand
{
    public string ActionType { get; init; } = string.Empty;
    public Guid? PurchaseOrderId { get; init; }
    public Guid? PurchaseOrderItemId { get; init; }
    public decimal Quantity { get; init; }
    public decimal? UnitCost { get; init; }
}

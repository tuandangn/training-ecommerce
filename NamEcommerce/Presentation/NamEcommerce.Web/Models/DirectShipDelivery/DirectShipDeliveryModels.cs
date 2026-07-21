using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Models.DirectShipDelivery;

public sealed class DirectShipDeliveryFilterModel
{
    public string? Keywords { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

public sealed class PendingDirectShipDeliveryItemModel
{
    public required string ProductName { get; init; }
    public required decimal Quantity { get; init; }
    public required decimal UnitPrice { get; init; }
}

public sealed class PendingDirectShipDeliveryModel
{
    public required Guid Id { get; init; }
    public required Guid? WarehouseId { get; init; }
    public required string Code { get; init; }
    public required Guid OrderId { get; init; }
    public string? OrderCode { get; init; }
    public required string CustomerName { get; init; }
    public string? CustomerPhone { get; init; }
    public required string ShippingAddress { get; init; }
    public required DateTime CreatedOn { get; init; }
    public required IList<PendingDirectShipDeliveryItemModel> Items { get; init; }
}

public sealed class PendingDirectShipDeliveryListModel
{
    public required DirectShipDeliveryFilterModel Filter { get; init; }
    public required IList<PendingDirectShipDeliveryModel> Items { get; init; }
    public EntityOptionListModel? ReturnWarehouseOptions { get; init; }
}


using NamEcommerce.Web.Contracts.Models.Common;
using NamEcommerce.Web.Contracts.Models.DeliveryNotes;

namespace NamEcommerce.Web.Contracts.Models.Preparation;

public sealed class PreparationListModel
{
    public bool IsGrouped { get; set; }

    /// <summary>
    /// Used when IsGrouped = false (default)
    /// </summary>
    public IPagedDataModel<PreparationItemModel>? Items { get; set; }

    /// <summary>
    /// Used when IsGrouped = true
    /// </summary>
    public IPagedDataModel<PreparationGroupedItemModel>? GroupedItems { get; set; }
}

[Serializable]
public sealed record PreparationItemModel
{
    public required Guid OrderItemId { get; init; }
    public required Guid OrderId { get; init; }
    public required string OrderCode { get; init; }

    public required Guid ProductId { get; init; }
    public required string ProductName { get; init; }

    public required decimal Quantity { get; init; }
    public required decimal UnitPrice { get; init; }

    public required Guid CustomerId { get; init; }
    public required string CustomerName { get; init; }
    public string? CustomerPhone { get; init; }

    public DateTime? ExpectedShippingDate { get; init; }

    public bool IsDelivered { get; init; }

    public decimal DeliveredQuantity { get; init; }
    
    public decimal StockQuantityAvailable { get; init; }

    public IList<DeliveryNoteLinkModel> DeliveryNoteLinks { get; init; } = [];
}

[Serializable]
public sealed record PreparationGroupedItemModel
{
    public required Guid ProductId { get; init; }
    public required string ProductName { get; init; }

    public required decimal TotalQuantity { get; init; }
    public DateTime? EarliestShippingDate { get; init; }

    public required IList<PreparationGroupedCustomerDetailModel> CustomerDetails { get; init; }

    public decimal StockQuantityAvailable { get; init; }
}

[Serializable]
public sealed record PreparationGroupedCustomerDetailModel
{
    public required Guid OrderItemId { get; init; }
    public required Guid OrderId { get; init; }
    public required string OrderCode { get; init; }

    public required Guid CustomerId { get; init; }
    public required string CustomerName { get; init; }
    public string? CustomerPhone { get; init; }

    public required decimal Quantity { get; init; }
    public required decimal UnitPrice { get; init; }
    public DateTime? ExpectedShippingDate { get; init; }

    public bool IsDelivered { get; init; }

    public decimal DeliveredQuantity { get; init; }

    public IList<DeliveryNoteLinkModel> DeliveryNoteLinks { get; init; } = [];
}

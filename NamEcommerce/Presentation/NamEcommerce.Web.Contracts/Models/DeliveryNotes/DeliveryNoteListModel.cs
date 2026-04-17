using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Models.DeliveryNotes;

public sealed class DeliveryNoteSearchModel
{
    public string? Keywords { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 15;
}

public sealed class DeliveryNoteListModel
{
    public string? Keywords { get; set; }
    
    public required IPagedDataModel<DeliveryNoteListItemModel> Data { get; init; }
}

public sealed record DeliveryNoteListItemModel
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }
    public required string CustomerName { get; init; }
    public required string ShippingAddress { get; init; }

    public Guid WarehouseId { get; init; }
    public string? WarehouseName { get; set; }

    public required Guid OrderId { get; set; }
    public required string OrderCode { get; set; }

    public string? CustomerPhone { get; init; }
    public decimal TotalAmount { get; init; }
    public int Status { get; init; }
    public string StatusName { get; init; } = string.Empty;
    public DateTime CreatedOnUtc { get; init; }
    public DateTime? DeliveredOnUtc { get; init; }
}

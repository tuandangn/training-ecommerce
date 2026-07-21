using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Models.DeliveryNotes;

[Serializable]
public sealed class DeliveryNoteListModel
{
    public string? Keywords { get; init; }
    public EntityOptionListModel? AvailableWarehouses { get; set; }

    public required IPagedDataModel<DeliveryNoteListItemModel> Data { get; init; }

    [Serializable]
    public sealed record DeliveryNoteListItemModel
    {
        public Guid Id { get; init; }
        public string Code { get; init; } = string.Empty;
        public string CustomerName { get; init; } = string.Empty;
        public string ShippingAddress { get; init; } = string.Empty;
        public bool IsCounterPickup { get; set; }
        public string? ShippingPhoneNumber { get; init; }

        public string? WarehouseName { get; set; }
        public Guid? AssignedDeliveryUserId { get; init; }
        public string? AssignedDeliveryFullName { get; init; }

        public bool IsDirectShip { get; set; }

        public Guid OrderId { get; set; }
        public string OrderCode { get; set; } = string.Empty;

        public string? CustomerPhone { get; init; }
        public decimal TotalAmount { get; init; }
        public int Status { get; init; }
        public DateTime CreatedOn { get; init; }
        public DateTime? DeliveredOn { get; init; }
        public IList<DeliveryNoteListItemProductModel> Items { get; init; } = [];
    }

    [Serializable]
    public sealed record DeliveryNoteListItemProductModel
    {
        public required Guid Id { get; init; }
        public Guid WarehouseId { get; init; }
        public string? WarehouseName { get; set; }
        public required string ProductName { get; init; }
        public decimal Quantity { get; init; }
    }
}

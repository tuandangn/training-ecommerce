namespace NamEcommerce.Web.Contracts.Models.DeliveryNotes;

public sealed class DeliveryNoteDetailsModel
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    
    public Guid OrderId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerPhone { get; set; }
    public string? CustomerAddress { get; set; }
    public string ShippingAddress { get; set; } = string.Empty;
    public Guid WarehouseId { get; set; }
    public string? WarehouseName { get; set; }
    
    public bool ShowPrice { get; set; }
    public string? Note { get; set; }
    
    public int Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    
    public DateTime CreatedOnUtc { get; set; }
    public DateTime? DeliveredOnUtc { get; set; }
    public Guid? DeliveryProofPictureId { get; set; }
    public string? DeliveryProofPictureUrl { get; set; }
    public string? DeliveryReceiverName { get; set; }

    public decimal TotalAmount { get; set; }
    public decimal Surcharge { get; set; }
    public string? SurchargeReason { get; set; }
    public decimal AmountToCollect { get; set; }

    public IList<DeliveryNoteItemModel> Items { get; set; } = [];
}

public sealed class DeliveryNoteItemModel
{
    public Guid Id { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal SubTotal { get; set; }
}

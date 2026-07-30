namespace NamEcommerce.Web.Contracts.Models.Orders;

[Serializable]
public sealed record OrderModel
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }
    public required decimal OrderSubTotal { get; init; }
    public decimal OrderDiscount { get; set; }
    public required decimal TotalAmount { get; init; }
    public DateTime? ExpectedShippingDate { get; set; }
    public int Status { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedOn { get; set; }

    public required Guid CustomerId { get; init; }
    public string? CustomerName { get; set; }
    public string? CustomerAddress { get; set; }
    public string? CustomerPhoneNumber { get; set; }
    public bool IsRetailWalkInCustomer { get; set; }

    public IList<OrderItemModel> Items { get; init; } = [];

    public DateTime? CompletedOn { get; set; }
    public string? ShippingAddress { get; set; }
    public string? ShippingPhoneNumber { get; set; }

    public bool CanUpdateInfo { get; init; }
    public bool CanCompleteOrder { get; init; }
    public bool CanUpdateOrderItems { get; init; }
    public bool CanProcess { get; init; }
    public bool ProcessRequiresPayment { get; init; }
    public bool PayOffRequired { get; init; }
    public decimal PaidAmount { get; set; }


    [Serializable]
    public sealed record OrderItemModel(Guid Id)
    {
        public required Guid ProductId { get; init; }
        public required decimal Quantity { get; init; }
        public required decimal UnitPrice { get; init; }
        public string? ProductName { get; set; }
        public string? ProductPicture { get; set; }
    }
}

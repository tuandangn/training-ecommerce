namespace NamEcommerce.Application.Contracts.Dtos.PurchaseOrders;

[Serializable]
public sealed record PurchaseOrderQuickCreateAppDto
{
    public required DateTime PlacedOnUtc { get; init; }
    public required Guid VendorId { get; init; }
    public string? Note { get; set; }

    public required bool IsReceived { get; init; }
    public DateTime? ReceivedOnUtc { get; set; }
    public Guid? DefaultWarehouseId { get; set; }
    public IList<Guid> PictureIds { get; init; } = [];
    public decimal? ShippingAmount { get; set; }
    public decimal? TaxAmount { get; set; }
    public DateTime? ExpectedDeliveryOnUtc { get; set; }

    public required bool IsPaid { get; init; }
    public PurchaseOrderQuickCreatePaymentAppDto? Payment { get; init; }

    public required IList<PurchaseOrderQuickCreateItemAppDto> Items { get; init; }

    public (bool valid, string? errorMessage) Validate()
    {
        if (VendorId == Guid.Empty)
            return (false, "Error.VendorRequired");
        if (PlacedOnUtc > DateTime.UtcNow)
            return (false, "Error.PlacedOrderDateCannotBeInFuture");
        if (Items.Count == 0)
            return (false, "Error.PurchaseOrder.ItemsRequired");
        if (IsReceived)
        {
            if (!DefaultWarehouseId.HasValue || DefaultWarehouseId == Guid.Empty)
                return (false, "Error.WarehouseRequired");
            if (ReceivedOnUtc > DateTime.UtcNow)
                return (false, "Error.ReceivedDateCannotBeInFuture");
            if (ShippingAmount.HasValue && ShippingAmount <= 0)
                return (false, "Error.ShippingAmountCannotBeNegative");
        }
        else
        {
            if (ExpectedDeliveryOnUtc.HasValue && ExpectedDeliveryOnUtc < DateTime.UtcNow)
                return (false, "Error.ExpectedDeliveryDateCannotBeInPast");
        }

        if (TaxAmount.HasValue && TaxAmount <= 0)
            return (false, "Error.TaxAmountCannotBeNegative");

        if (IsPaid)
        {
            if (Payment is null)
                return (false, "Error.PaymentInfoRequired");
            if (Payment.PaidAmount <= 0)
                return (false, "Error.PaymentAmountMustBePositive");
            if (Payment.PaidAmount > Items.Sum(item => item.Quantity * (item.UnitCost ?? 0)) + (ShippingAmount ?? 0) + (TaxAmount ?? 0))
                return (false, "Error.PaidAmountExceedsOrderTotal");
        }
        return (true, null);
    }


    [Serializable]
    public sealed record PurchaseOrderQuickCreateItemAppDto
    {
        public required Guid ProductId { get; init; }
        public required decimal Quantity { get; init; }
        public decimal? UnitCost { get; set; }
        public Guid? WarehouseId { get; set; }
    }

    [Serializable]
    public sealed record PurchaseOrderQuickCreatePaymentAppDto
    {
        public required decimal PaidAmount { get; init; }
        public required int PaymentMethod { get; init; }
        public Guid? BankAccountId { get; set; }
    }
}

[Serializable]
public sealed record PurchaseOrderQuickCreateResultAppDto
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public Guid? CreatedId { get; init; }
}

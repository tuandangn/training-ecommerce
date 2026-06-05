namespace NamEcommerce.Application.Contracts.Dtos.PurchaseOrders;

[Serializable]
public sealed record QuickCreatePurchaseOrderAppDto
{
    public required Guid VendorId { get; init; }
    public required Guid DefaultWarehouseId { get; init; }
    public required DateTime ReceivedOnUtc { get; init; }
    public string? Note { get; init; }
    public bool ReceiveImmediately { get; init; } = true;
    public IList<Guid> PictureIds { get; init; } = [];
    public required IList<QuickCreatePurchaseOrderItemAppDto> Items { get; init; }
    public QuickCreatePaymentAppDto? Payment { get; init; }

    public (bool valid, string? errorMessage) Validate()
    {
        if (VendorId == Guid.Empty)
            return (false, "Error.VendorRequired");
        if (DefaultWarehouseId == Guid.Empty)
            return (false, "Error.WarehouseRequired");
        if (Items.Count == 0)
            return (false, "Error.PurchaseOrder.ItemsRequired");
        if (Payment is not null && Payment.Amount <= 0)
            return (false, "Error.PaymentAmountMustBePositive");
        return (true, null);
    }
}

[Serializable]
public sealed record QuickCreatePurchaseOrderItemAppDto
{
    public required Guid ProductId { get; init; }
    public required decimal Quantity { get; init; }
    public decimal? UnitCost { get; init; }
    public Guid? WarehouseId { get; init; }
    public int QuantityDecimalPlaces { get; init; }
}

[Serializable]
public sealed record QuickCreatePaymentAppDto
{
    public required decimal Amount { get; init; }
    public required int PaymentMethod { get; init; }
}

[Serializable]
public sealed record QuickCreatePurchaseOrderResultAppDto
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public Guid? PurchaseOrderId { get; init; }
    public string? PurchaseOrderCode { get; init; }
    public IReadOnlyList<Guid> GoodsReceiptIds { get; init; } = [];

    public static QuickCreatePurchaseOrderResultAppDto CreateSuccess(Guid poId, string poCode, IReadOnlyList<Guid> receiptIds)
        => new() { Success = true, PurchaseOrderId = poId, PurchaseOrderCode = poCode, GoodsReceiptIds = receiptIds };

    public static QuickCreatePurchaseOrderResultAppDto CreateError(string errorMessage)
        => new() { Success = false, ErrorMessage = errorMessage };
}

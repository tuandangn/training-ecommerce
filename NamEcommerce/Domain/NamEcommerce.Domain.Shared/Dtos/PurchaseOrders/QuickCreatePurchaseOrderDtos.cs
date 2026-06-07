using NamEcommerce.Domain.Shared.Enums.Debts;
using NamEcommerce.Domain.Shared.Enums.Orders;
using NamEcommerce.Domain.Shared.Exceptions.Inventory;

namespace NamEcommerce.Domain.Shared.Dtos.PurchaseOrders;

[Serializable]
public sealed record QuickCreatePurchaseOrderDto
{
    public required Guid VendorId { get; init; }
    public required Guid DefaultWarehouseId { get; init; }
    public required DateTime ReceivedOnUtc { get; init; }
    public string? Note { get; init; }
    public bool ReceiveImmediately { get; init; } = true;
    public IList<Guid> PictureIds { get; init; } = [];
    public required IList<QuickCreatePurchaseOrderItemDto> Items { get; init; }
    public QuickCreatePaymentDto? Payment { get; init; }

    public void Verify()
    {
        if (VendorId == Guid.Empty)
            throw new PurchaseOrderDataIsInvalidException("Error.VendorRequired");
        if (DefaultWarehouseId == Guid.Empty)
            throw new PurchaseOrderDataIsInvalidException("Error.WarehouseRequired");
        if (Items.Count == 0)
            throw new PurchaseOrderDataIsInvalidException("Error.PurchaseOrder.ItemsRequired");
        if (Payment is not null && Payment.Amount <= 0)
            throw new PurchaseOrderDataIsInvalidException("Error.PaymentAmountMustBePositive");
    }
}

[Serializable]
public sealed record QuickCreatePurchaseOrderItemDto
{
    public required Guid ProductId { get; init; }
    public required decimal Quantity { get; init; }
    public decimal? UnitCost { get; init; }
    public Guid? WarehouseId { get; init; }
    public int QuantityDecimalPlaces { get; init; }
}

[Serializable]
public sealed record QuickCreatePaymentDto
{
    public required decimal Amount { get; init; }
    public required PaymentMethod PaymentMethod { get; init; }
    public PaymentType PaymentType { get; init; } = PaymentType.VendorDebtPayment;
}

[Serializable]
public sealed record QuickCreatePurchaseOrderResultDto
{
    public required Guid PurchaseOrderId { get; init; }
    public required string PurchaseOrderCode { get; init; }
    public IReadOnlyList<Guid> GoodsReceiptIds { get; init; } = [];
}

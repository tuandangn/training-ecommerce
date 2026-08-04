using NamEcommerce.Domain.Shared.Enums.Debts;
using NamEcommerce.Domain.Shared.Enums.Orders;
using NamEcommerce.Domain.Shared.Exceptions.Inventory;

namespace NamEcommerce.Domain.Shared.Dtos.PurchaseOrders;

[Serializable]
public sealed record PurchaseOrderQuickCreateDto
{
    public required DateTime PlacedOnUtc { get; set; }
    public required Guid VendorId { get; init; }
    public Guid? DefaultWarehouseId { get; set; }
    public DateTime? ReceivedOnUtc { get; set; }
    public string? Note { get; set; }
    public required bool IsReceived { get; init; }
    public required bool IsPaid { get; init; }
    public IList<Guid> PictureIds { get; set; } = [];
    public required IList<PurchaseOrderQuickCreateItemDto> Items { get; init; }
    public PurchaseOrderQuickCreatePaymentDto? Payment { get; init; }

    public void Verify()
    {
        if (VendorId == Guid.Empty)
            throw new PurchaseOrderDataIsInvalidException("Error.VendorRequired");

        if (IsReceived)
        {
            if (!DefaultWarehouseId.HasValue || DefaultWarehouseId == Guid.Empty)
                throw new PurchaseOrderDataIsInvalidException("Error.WarehouseRequired");
        }

        if (Items.Count == 0)
            throw new PurchaseOrderDataIsInvalidException("Error.PurchaseOrder.ItemsRequired");

        if (IsPaid)
        {
            if (Payment is null)
                throw new PurchaseOrderDataIsInvalidException("Error.PaymentInfoRequired");
            if (Payment.PaidAmount <= 0)
                throw new PurchaseOrderDataIsInvalidException("Error.PaymentAmountMustBePositive");
            if (Payment.PaidAmount > Items.Sum(item => item.Quantity * (item.UnitCost ?? 0)))
                throw new PurchaseOrderDataIsInvalidException("Error.PaidAmountExceedsOrderTotal");
        }
    }

    [Serializable]
    public sealed record PurchaseOrderQuickCreateItemDto
    {
        public required Guid ProductId { get; init; }
        public required decimal Quantity { get; init; }
        public decimal? UnitCost { get; init; }
        public Guid? WarehouseId { get; init; }
    }

    [Serializable]
    public sealed record PurchaseOrderQuickCreatePaymentDto
    {
        public required decimal PaidAmount { get; init; }
        public required PaymentMethod PaymentMethod { get; init; }
        public PaymentType PaymentType { get; init; } = PaymentType.VendorDebtPayment;
    }
}

[Serializable]
public sealed record PurchaseOrderQuickCreateResultDto
{
    public required Guid PurchaseOrderId { get; init; }
}

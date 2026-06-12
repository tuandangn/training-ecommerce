using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Commands.Models.PurchaseOrders;

[Serializable]
public sealed class RecordPurchaseOrderSettlementPaymentCommand : ICommand<CommonActionResultModel>
{
    public required Guid PurchaseOrderId { get; init; }
    public decimal Amount { get; init; }
    public int PaymentMethod { get; init; }
    public string? Note { get; init; }
    public DateTime PaidOn { get; init; } = DateTime.Now;
}

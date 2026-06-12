using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Commands.Models.Orders;

[Serializable]
public sealed class RecordOrderSettlementPaymentCommand : ICommand<CommonActionResultModel>
{
    public required Guid OrderId { get; init; }
    public decimal Amount { get; init; }
    public int PaymentMethod { get; init; }
    public string? Note { get; init; }
    public DateTime PaidOn { get; init; } = DateTime.Now;
}

[Serializable]
public sealed class RecordOrderSettlementQrPaymentCommand : ICommand<CommonActionResultModel>
{
    public required Guid OrderId { get; init; }
    public required Guid PaymentIntentId { get; init; }
    public string? Note { get; init; }
}

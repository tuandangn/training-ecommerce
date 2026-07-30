using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Commands.Models.Orders;

[Serializable]
public sealed class CompleteQuickCreateOrderPaymentCommand : ICommand<CommonActionResultModel>
{
    public required Guid OrderId { get; init; }
    public required decimal PaidAmount { get; init; }
    public required Guid? PaymentIntentId { get; init; }
}

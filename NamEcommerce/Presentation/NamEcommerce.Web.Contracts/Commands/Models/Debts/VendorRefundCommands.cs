using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Commands.Models.Debts;

[Serializable]
public sealed class CompleteVendorRefundCommand : ICommand<CommonActionResultModel>
{
    public required Guid Id { get; init; }
    public int PaymentMethod { get; init; }
    public Guid? BankAccountId { get; init; }
    public string? Note { get; init; }
}

[Serializable]
public sealed class CancelVendorRefundCommand : ICommand<CommonActionResultModel>
{
    public required Guid Id { get; init; }
}

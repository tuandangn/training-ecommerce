using MediatR;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Commands.Models.Debts;

[Serializable]
public sealed class CompleteCustomerRefundCommand : IRequest<CommonActionResultModel>
{
    public required Guid Id { get; init; }
    public int PaymentMethod { get; init; }
    public string? Note { get; init; }
}

[Serializable]
public sealed class CancelCustomerRefundCommand : IRequest<CommonActionResultModel>
{
    public required Guid Id { get; init; }
}

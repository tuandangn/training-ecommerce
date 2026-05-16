using MediatR;
using NamEcommerce.Web.Contracts.Models.Debts;

namespace NamEcommerce.Web.Contracts.Queries.Models.Debts;

[Serializable]
public sealed class GetCustomerRefundQuery : IRequest<CustomerRefundModel?>
{
    public required Guid Id { get; init; }
}

[Serializable]
public sealed class GetCustomerRefundListQuery : IRequest<CustomerRefundListModel>
{
    public Guid? CustomerId { get; init; }
    public int? Status { get; init; }
    public string? Keywords { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

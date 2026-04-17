using MediatR;
using NamEcommerce.Web.Contracts.Models.Debts;

namespace NamEcommerce.Web.Contracts.Queries.Models.Debts;

public sealed class GetCustomerDebtListQuery : IRequest<CustomerDebtListModel>
{
    public Guid? CustomerId { get; set; }
    public string? Keywords { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 15;
}

public sealed class GetCustomerDebtDetailsQuery : IRequest<CustomerDebtDetailsModel?>
{
    public required Guid Id { get; init; }
}

using MediatR;
using NamEcommerce.Web.Contracts.Models.Customers;

namespace NamEcommerce.Web.Contracts.Queries.Models.Customers;

[Serializable]
public sealed class GetCustomerListQuery : IRequest<CustomerListModel>
{
    public string? Keywords { get; init; }
    public int PageIndex { get; init; }
    public int PageSize { get; init; }
}

[Serializable]
public sealed class GetCustomerByIdQuery : IRequest<CustomerModel?>
{
    public required Guid Id { get; init; }
}

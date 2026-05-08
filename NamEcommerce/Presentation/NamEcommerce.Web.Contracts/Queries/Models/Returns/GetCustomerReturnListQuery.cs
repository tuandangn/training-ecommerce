using MediatR;
using NamEcommerce.Web.Contracts.Models.Returns;

namespace NamEcommerce.Web.Contracts.Queries.Models.Returns;

[Serializable]
public sealed class GetCustomerReturnListQuery : IRequest<CustomerReturnListModel>
{
    public Guid? CustomerId { get; init; }
    public Guid? OrderId { get; init; }
    public int? Status { get; init; }
    public int PageIndex { get; init; }
    public int PageSize { get; init; }
}

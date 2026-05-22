using MediatR;
using NamEcommerce.Web.Contracts.Models.Preparation;

namespace NamEcommerce.Web.Contracts.Queries.Models.Preparations;

[Serializable]
public sealed class GetCustomerPreparationListQuery : IRequest<PreparationListModel>
{
    public string? Keywords { get; init; }
    public int PageIndex { get; init; }
    public int PageSize { get; init; }
}

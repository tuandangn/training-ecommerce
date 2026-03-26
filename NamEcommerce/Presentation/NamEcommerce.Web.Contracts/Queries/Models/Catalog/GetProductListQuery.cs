using MediatR;
using NamEcommerce.Web.Contracts.Models.Catalog;

namespace NamEcommerce.Web.Contracts.Queries.Models.Catalog;

[Serializable]
public sealed class GetProductListQuery : IRequest<ProductListModel>
{
    public required string? Keywords { get; init; }

    public required int PageIndex { get; init; }
    public required int PageSize { get; init; }
}

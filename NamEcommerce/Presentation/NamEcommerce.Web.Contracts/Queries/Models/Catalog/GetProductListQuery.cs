using MediatR;
using NamEcommerce.Web.Contracts.Models.Catalog;

namespace NamEcommerce.Web.Contracts.Queries.Models.Catalog;

[Serializable]
public sealed class GetProductListQuery : IRequest<ProductListModel>
{
    public string? Keywords { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? VendorId { get; set; }

    public required int PageIndex { get; init; }
    public required int PageSize { get; init; }
}

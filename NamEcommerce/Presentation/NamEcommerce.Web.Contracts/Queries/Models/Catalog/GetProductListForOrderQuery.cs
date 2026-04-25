using MediatR;
using NamEcommerce.Web.Contracts.Models.Catalog;

namespace NamEcommerce.Web.Contracts.Queries.Models.Catalog;

[Serializable]
public sealed class GetProductListForOrderQuery : IRequest<ProductListForOrderModel>
{
    public required string? Keywords { get; set; }
    public Guid? VendorId { get; set; }
    public Guid? WarehouseId { get; set; }
    public Guid? CategoryId { get; set; }
}
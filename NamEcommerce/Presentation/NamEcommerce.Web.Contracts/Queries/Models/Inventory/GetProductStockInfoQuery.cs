using MediatR;
using NamEcommerce.Web.Contracts.Models.Inventory;

namespace NamEcommerce.Web.Contracts.Queries.Models.Inventory;

[Serializable]
public sealed record GetProductStockInfoQuery(Guid ProductId, Guid? WarehouseId) : IRequest<ProductStockInfoModel>;

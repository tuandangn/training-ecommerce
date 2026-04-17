using MediatR;
using NamEcommerce.Web.Contracts.Models.Catalog;

namespace NamEcommerce.Web.Contracts.Queries.Models.Catalog;

public sealed record GetProductPriceHistoryQuery(Guid ProductId) : IRequest<ProductPriceHistoryModel>;

using MediatR;
using NamEcommerce.Web.Contracts.Models.PurchaseOrders;

namespace NamEcommerce.Web.Contracts.Queries.Models.PurchaseOrders;

[Serializable]
public sealed record GetPurchaseOrderByCodeQuery(string Code) : IRequest<Guid?>;

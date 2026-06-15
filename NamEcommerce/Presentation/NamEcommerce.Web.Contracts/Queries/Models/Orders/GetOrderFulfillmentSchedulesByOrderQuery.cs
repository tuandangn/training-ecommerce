using MediatR;
using NamEcommerce.Web.Contracts.Models.Orders;

namespace NamEcommerce.Web.Contracts.Queries.Models.Orders;

[Serializable]
public sealed record GetOrderFulfillmentSchedulesByOrderQuery(Guid OrderId) : IRequest<IList<OrderFulfillmentScheduleModel>>;

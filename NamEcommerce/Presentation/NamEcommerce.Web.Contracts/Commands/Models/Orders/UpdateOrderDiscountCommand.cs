using MediatR;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Commands.Models.Orders;

public sealed record UpdateOrderDiscountCommand(Guid OrderId, decimal? Discount) : IRequest<CommonResultModel>;

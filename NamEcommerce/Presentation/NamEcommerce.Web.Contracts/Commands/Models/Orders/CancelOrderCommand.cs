using MediatR;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Commands.Models.Orders;

public sealed record CancelOrderCommand(Guid OrderId, string Reason) : IRequest<CommonResultModel>;

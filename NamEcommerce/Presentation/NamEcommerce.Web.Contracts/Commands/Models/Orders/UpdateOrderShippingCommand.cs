using MediatR;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Commands.Models.Orders;

[Serializable]
public sealed record UpdateOrderShippingCommand(Guid OrderId, DateTime? ExpectedShippingDate, string? Address) 
    : IRequest<CommonActionResultModel>;

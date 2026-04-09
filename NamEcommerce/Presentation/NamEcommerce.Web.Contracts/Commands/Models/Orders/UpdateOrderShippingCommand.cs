using MediatR;

namespace NamEcommerce.Web.Contracts.Commands.Models.Orders;

public sealed record UpdateOrderShippingResultModel(bool Success, string? ErrorMessage);

public sealed record UpdateOrderShippingCommand(Guid OrderId, int ShippingStatus, string? Address, string? Note) : IRequest<UpdateOrderShippingResultModel>;

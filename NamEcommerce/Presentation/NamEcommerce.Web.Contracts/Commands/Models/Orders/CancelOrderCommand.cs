using MediatR;

namespace NamEcommerce.Web.Contracts.Commands.Models.Orders;

public sealed record CancelOrderResultModel(bool Success, string? ErrorMessage);

public sealed record CancelOrderCommand(Guid OrderId, string Reason) : IRequest<CancelOrderResultModel>;

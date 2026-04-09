using MediatR;

namespace NamEcommerce.Web.Contracts.Commands.Models.Orders;

public sealed record MarkOrderAsPaidResultModel(bool Success, string? ErrorMessage);

public sealed record MarkOrderAsPaidCommand(Guid OrderId, int PaymentMethod, string? Note) : IRequest<MarkOrderAsPaidResultModel>;

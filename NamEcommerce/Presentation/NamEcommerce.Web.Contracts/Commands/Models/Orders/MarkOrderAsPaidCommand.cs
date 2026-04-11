using MediatR;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Commands.Models.Orders;

[Serializable]
public sealed record MarkOrderAsPaidCommand(Guid OrderId, int PaymentMethod, string? Note) : IRequest<CommonResultModel>;

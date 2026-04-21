using MediatR;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Commands.Models.Orders;

public sealed record UpdateOrderNoteCommand(Guid OrderId, string Note) : IRequest<CommonActionResultModel>;

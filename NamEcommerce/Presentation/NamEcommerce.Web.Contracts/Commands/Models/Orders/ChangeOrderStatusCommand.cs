using MediatR;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Commands.Models.Orders;

[Serializable]
public sealed class ChangeOrderStatusCommand : IRequest<CommonResultModel>
{
    public required Guid OrderId { get; init; }
    public required int Status { get; init; }
}
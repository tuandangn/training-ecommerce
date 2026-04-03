using MediatR;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Commands.Models.Orders;

[Serializable]
public sealed class UpdateOrderCommand : IRequest<CommonResultModel>
{
    public required Guid Id { get; init; }
    public string? Note { get; init; }
    public decimal? OrderDiscount { get; init; }
}

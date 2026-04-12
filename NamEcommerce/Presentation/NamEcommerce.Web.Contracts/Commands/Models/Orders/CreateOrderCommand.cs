using MediatR;
using NamEcommerce.Web.Contracts.Models.Orders;

namespace NamEcommerce.Web.Contracts.Commands.Models.Orders;

[Serializable]
public sealed class CreateOrderCommand : IRequest<CreateOrderResultModel>
{
    public required Guid CustomerId { get; init; }
    public string? Note { get; init; }
    public decimal? OrderDiscount { get; init; }
    public DateTime? ExpectedShippingDate { get; set; }
    public required string ShippingAddress { get; set; }
    public IList<OrderItemModel> Items { get; init; } = [];
}

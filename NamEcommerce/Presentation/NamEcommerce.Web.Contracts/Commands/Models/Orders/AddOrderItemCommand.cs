using MediatR;

namespace NamEcommerce.Web.Contracts.Commands.Models.Orders;

[Serializable]
public sealed class AddOrderItemCommand : IRequest<AddOrderItemResultModel>
{
    public required Guid OrderId { get; init; }
    public required Guid ProductId { get; init; }
    public required decimal Quantity { get; init; }
    public required decimal UnitPrice { get; init; }
}

[Serializable]
public sealed class AddOrderItemResultModel
{
    public required bool Success { get; init; }
    public string? ErrorMessage { get; init; }
}

using MediatR;
using NamEcommerce.Web.Contracts.Models.Returns;

namespace NamEcommerce.Web.Contracts.Commands.Models.Returns;

[Serializable]
public sealed class CreateCustomerReturnCommand : IRequest<CreateCustomerReturnResultModel>
{
    public required Guid OrderId { get; init; }
    public required Guid WarehouseId { get; init; }
    public string? Note { get; init; }
    public IList<CreateCustomerReturnItemCommand> Items { get; init; } = [];
}

[Serializable]
public sealed class CreateCustomerReturnItemCommand
{
    public required Guid ProductId { get; init; }
    public Guid? DeliveryNoteItemId { get; init; }
    public required decimal RequestedQuantity { get; init; }
    public required decimal AcceptedQuantity { get; init; }
    public required decimal UnitPrice { get; init; }
}

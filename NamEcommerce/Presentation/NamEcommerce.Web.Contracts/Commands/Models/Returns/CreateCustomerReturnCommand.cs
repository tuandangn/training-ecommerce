using MediatR;
using NamEcommerce.Web.Contracts.Models.Returns;

namespace NamEcommerce.Web.Contracts.Commands.Models.Returns;

[Serializable]
public sealed class CreateCustomerReturnCommand : IRequest<CreateCustomerReturnResultModel>
{
    public Guid? DeliveryNoteId { get; init; }
    public required Guid CustomerId { get; init; }
    public string? Note { get; init; }
    public decimal AdditionalCost { get; init; } = 0;
    public IList<CreateCustomerReturnItemCommand> Items { get; init; } = [];
}

[Serializable]
public sealed class CreateCustomerReturnItemCommand
{
    public required Guid ProductId { get; init; }
    public Guid? DeliveryNoteItemId { get; init; }
    public required decimal RequestedQuantity { get; init; }
    public required decimal AcceptedQuantity { get; init; }
    public decimal? OriginalUnitPrice { get; init; }
    public required decimal ReturnUnitPrice { get; init; }
}

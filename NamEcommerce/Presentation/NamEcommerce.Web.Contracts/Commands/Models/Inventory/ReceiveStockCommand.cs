using MediatR;
using NamEcommerce.Web.Contracts.Models.Inventory;

namespace NamEcommerce.Web.Contracts.Commands.Models.Inventory;

[Serializable]
public sealed class ReceiveStockCommand : IRequest<ReceiveStockResultModel>
{
    public required Guid ProductId { get; init; }
    public required Guid WarehouseId { get; init; }
    public required decimal Quantity { get; init; }
    public int ReferenceType { get; init; }
    public Guid? ReferenceId { get; init; }
    public required Guid UserId { get; init; }
    public string? Note { get; init; }
}

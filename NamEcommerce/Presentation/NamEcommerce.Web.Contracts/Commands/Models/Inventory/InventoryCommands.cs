using MediatR;
using NamEcommerce.Web.Contracts.Models.Inventory;

namespace NamEcommerce.Web.Contracts.Commands.Models.Inventory;

[Serializable]
public sealed class AdjustStockCommand : IRequest<AdjustStockResultModel>
{
    public required Guid ProductId { get; init; }
    public required Guid WarehouseId { get; init; }
    public required decimal NewQuantity { get; init; }
    public string? Note { get; init; }
    public required Guid ModifiedByUserId { get; init; }
}

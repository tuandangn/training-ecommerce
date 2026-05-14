using MediatR;
using NamEcommerce.Web.Contracts.Models.Inventory;

namespace NamEcommerce.Web.Contracts.Commands.Models.Inventory;

[Serializable]
public sealed class SetStockLevelsCommand : IRequest<SetStockLevelsResultModel>
{
    public required Guid Id { get; init; }
    public required decimal ReorderLevel { get; init; }
    public required decimal MaxStockLevel { get; init; }
}

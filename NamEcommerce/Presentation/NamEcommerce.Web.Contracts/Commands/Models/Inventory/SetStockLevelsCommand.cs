using NamEcommerce.Web.Contracts.Models.Inventory;

namespace NamEcommerce.Web.Contracts.Commands.Models.Inventory;

[Serializable]
public sealed class SetStockLevelsCommand : ICommand<SetStockLevelsResultModel>
{
    public required Guid Id { get; init; }
    public required decimal ReorderLevel { get; init; }
    public required decimal MaxStockLevel { get; init; }
}

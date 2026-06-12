using NamEcommerce.Web.Contracts.Models.Inventory;

namespace NamEcommerce.Web.Contracts.Commands.Models.Inventory;

[Serializable]
public sealed record DeleteWarehouseCommand(Guid Id) : ICommand<DeleteWarehouseResultModel>;

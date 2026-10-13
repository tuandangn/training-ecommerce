using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Commands.Models.Orders;

public sealed record CancelOrderCommand(Guid OrderId, IReadOnlyList<Guid> OrderItemIds, Guid? ReturnWarehouseId = null) 
    : ICommand<CommonActionResultModel>;

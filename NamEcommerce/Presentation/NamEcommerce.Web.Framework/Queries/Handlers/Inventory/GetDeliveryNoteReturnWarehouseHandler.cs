using MediatR;
using NamEcommerce.Application.Contracts.Inventory;
using NamEcommerce.Web.Contracts.Queries.Models.Inventory;

namespace NamEcommerce.Web.Framework.Queries.Handlers.Inventory;

public sealed class GetDeliveryNoteReturnWarehouseHandler(IInventoryAppService inventoryAppService)
    : IRequestHandler<GetDeliveryNoteReturnWarehouseQuery, string?>
{
    public Task<string?> Handle(GetDeliveryNoteReturnWarehouseQuery request, CancellationToken cancellationToken)
        => inventoryAppService.GetReturnWarehouseNameForDeliveryNoteAsync(request.DeliveryNoteId, request.DeliveryNoteWarehouseId);
}

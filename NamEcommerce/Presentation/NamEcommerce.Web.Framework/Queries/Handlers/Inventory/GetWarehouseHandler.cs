using MediatR;
using NamEcommerce.Application.Contracts.Inventory;
using NamEcommerce.Web.Contracts.Models.Inventory;
using NamEcommerce.Web.Contracts.Queries.Models.Inventory;

namespace NamEcommerce.Web.Framework.Queries.Handlers.Inventory;

public sealed class GetWarehouseHandler : IRequestHandler<GetWarehouseQuery, WarehouseModel?>
{
    private readonly IWarehouseAppService _warehouseAppService;

    public GetWarehouseHandler(IWarehouseAppService warehouseAppService)
    {
        _warehouseAppService = warehouseAppService;
    }

    public async Task<WarehouseModel?> Handle(GetWarehouseQuery request, CancellationToken cancellationToken)
    {
        var warehouse = await _warehouseAppService.GetWarehouseByIdAsync(request.Id).ConfigureAwait(false);
        if (warehouse is null)
            return null;

        return new WarehouseModel
        {
            Id = warehouse.Id,
            Code = warehouse.Code,
            Name = warehouse.Name,
            WarehouseType = warehouse.WarehouseType,
            WarehouseNameKey = warehouse.WarehouseNameKey,
            PhoneNumber = warehouse.PhoneNumber,
            Address = warehouse.Address,
            ManagerUserId = warehouse.ManagerUserId,
            IsActive = warehouse.IsActive
        };
    }
}

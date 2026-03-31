using MediatR;
using NamEcommerce.Application.Contracts.Inventory;
using NamEcommerce.Web.Contracts.Models.Common;
using NamEcommerce.Web.Contracts.Queries.Models.Inventory;

namespace NamEcommerce.Web.Framework.Queries.Handlers.Inventory;

public sealed class GetWarehouseOptionListHandler : IRequestHandler<GetWarehouseOptionListQuery, EntityOptionListModel>
{
    private readonly IWarehouseAppService _warehouseAppService;

    public GetWarehouseOptionListHandler(IWarehouseAppService warehouseAppService)
    {
        _warehouseAppService = warehouseAppService;
    }

    public async Task<EntityOptionListModel> Handle(GetWarehouseOptionListQuery request, CancellationToken cancellationToken)
    {
        var warehouses = await _warehouseAppService.GetWarehousesAsync(null, 0, int.MaxValue);

        return new EntityOptionListModel
        {
            Options = warehouses.Select(x => new EntityOptionListModel.EntityOptionModel
            {
                Id = x.Id,
                Name = x.Name
            })
        };
    }
}

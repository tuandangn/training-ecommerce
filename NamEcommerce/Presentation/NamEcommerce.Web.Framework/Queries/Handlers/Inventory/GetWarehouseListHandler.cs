using MediatR;
using NamEcommerce.Application.Contracts.Inventory;
using NamEcommerce.Web.Contracts.Models.Inventory;
using NamEcommerce.Web.Contracts.Queries.Models.Inventory;
using NamEcommerce.Web.Framework.Common;

namespace NamEcommerce.Web.Framework.Queries.Handlers.Inventory;

public sealed class GetWarehouseListHandler : IRequestHandler<GetWarehouseListQuery, WarehouseListModel>
{
    private readonly IWarehouseAppService _warehouseAppService;

    public GetWarehouseListHandler(IWarehouseAppService warehouseAppService)
    {
        _warehouseAppService = warehouseAppService;
    }

    public async Task<WarehouseListModel> Handle(GetWarehouseListQuery request, CancellationToken cancellationToken)
    {
        var pagedData = await _warehouseAppService.GetWarehousesAsync(request.Keywords, request.PageIndex, request.PageSize).ConfigureAwait(false);

        var model = new WarehouseListModel
        {
            Keywords = request.Keywords,
            Data = pagedData.MapToModel(item => new WarehouseListModel.WarehouseItemModel(item.Id)
            {
                Code = item.Code,
                Name = item.Name,
                WarehouseType = item.WarehouseNameKey,
                PhoneNumber = item.PhoneNumber,
                Address = item.Address,
                IsActive = item.IsActive
            })
        };

        return model;
    }
}

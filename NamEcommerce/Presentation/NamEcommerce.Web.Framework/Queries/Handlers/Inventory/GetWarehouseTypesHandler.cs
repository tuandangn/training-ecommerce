using MediatR;
using NamEcommerce.Application.Contracts.Inventory;
using NamEcommerce.Web.Contracts.Models.Common;
using NamEcommerce.Web.Contracts.Queries.Models.Inventory;

namespace NamEcommerce.Web.Framework.Queries.Handlers.Inventory;

public sealed class GetWarehouseTypeOptionsHandler : IRequestHandler<GetWarehouseTypeOptionsQuery, CommonOptionListModel>
{
    private readonly IWarehouseAppService _warehouseAppService;

    public GetWarehouseTypeOptionsHandler(IWarehouseAppService warehouseAppService)
    {
        _warehouseAppService = warehouseAppService;
    }

    public Task<CommonOptionListModel> Handle(GetWarehouseTypeOptionsQuery request, CancellationToken cancellationToken)
    {
        var optionList = _warehouseAppService.GetAvailableWarehouseTypes();

        return Task.FromResult(new CommonOptionListModel
        {
            Items = optionList.Select(option => new CommonOptionListModel.CommonOptionItemModel(option.Text, option.Value))
        });
    }
}

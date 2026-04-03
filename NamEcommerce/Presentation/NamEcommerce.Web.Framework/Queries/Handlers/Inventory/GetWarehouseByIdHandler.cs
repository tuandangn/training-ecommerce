using MediatR;
using NamEcommerce.Application.Contracts.Inventory;
using NamEcommerce.Web.Contracts.Models.Inventory;
using NamEcommerce.Web.Contracts.Queries.Models.Inventory;

namespace NamEcommerce.Web.Framework.Queries.Handlers.Inventory;

public sealed class GetWarehouseByIdHandler : IRequestHandler<GetWarehouseByIdQuery, WarehouseDetailModel?>
{
    private readonly IWarehouseAppService _warehouseAppService;

    public GetWarehouseByIdHandler(IWarehouseAppService warehouseAppService)
    {
        _warehouseAppService = warehouseAppService;
    }

    public async Task<WarehouseDetailModel?> Handle(GetWarehouseByIdQuery request, CancellationToken cancellationToken)
    {
        var warehouse = await _warehouseAppService.GetWarehouseByIdAsync(request.Id);

        if (warehouse == null)
            return null;

        return new WarehouseDetailModel
        {
            Id = warehouse.Id,
            Code = warehouse.Code,
            Name = warehouse.Name,
            WarehouseType = warehouse.WarehouseType,
            Address = warehouse.Address,
            PhoneNumber = warehouse.PhoneNumber,
            ManagerUserId = warehouse.ManagerUserId,
            IsActive = warehouse.IsActive
        };
    }
}

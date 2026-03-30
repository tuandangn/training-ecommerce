using MediatR;
using NamEcommerce.Application.Contracts.Dtos.Inventory;
using NamEcommerce.Application.Contracts.Inventory;
using NamEcommerce.Web.Contracts.Commands.Models.Inventory;
using NamEcommerce.Web.Contracts.Models.Inventory;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Inventory;

public sealed class UpdateWarehouseHandler : IRequestHandler<UpdateWarehouseCommand, UpdateWarehouseResultModel>
{
    private readonly IWarehouseAppService _warehouseAppService;

    public UpdateWarehouseHandler(IWarehouseAppService warehouseAppService)
    {
        _warehouseAppService = warehouseAppService;
    }

    public async Task<UpdateWarehouseResultModel> Handle(UpdateWarehouseCommand request, CancellationToken cancellationToken)
    {
        var result = await _warehouseAppService.UpdateWarehouseAsync(new UpdateWarehouseAppDto(request.Id)
        {
            Code = request.Code,
            Name = request.Name,
            WarehouseType = request.WarehouseType,
            Address = request.Address,
            PhoneNumber = request.PhoneNumber,
            ManagerUserId = request.ManagerUserId,
            IsActive = request.IsActive
        }).ConfigureAwait(false);

        return new UpdateWarehouseResultModel
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage,
            UpdatedId = result.UpdatedId
        };
    }
}

using MediatR;
using NamEcommerce.Application.Contracts.Dtos.Inventory;
using NamEcommerce.Application.Contracts.Inventory;
using NamEcommerce.Web.Contracts.Commands.Models.Inventory;
using NamEcommerce.Web.Contracts.Models.Inventory;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Inventory;

public sealed class CreateWarehouseHandler : IRequestHandler<CreateWarehouseCommand, CreateWarehouseResultModel>
{
    private readonly IWarehouseAppService _warehouseAppService;

    public CreateWarehouseHandler(IWarehouseAppService warehouseAppService)
    {
        _warehouseAppService = warehouseAppService;
    }

    public async Task<CreateWarehouseResultModel> Handle(CreateWarehouseCommand request, CancellationToken cancellationToken)
    {
        var result = await _warehouseAppService.CreateWarehouseAsync(new CreateWarehouseAppDto
        {
            Code = request.Code,
            Name = request.Name,
            WarehouseType = request.WarehouseType,
            Address = request.Address,
            PhoneNumber = request.PhoneNumber,
            ManagerUserId = request.ManagerUserId,
            IsActive = request.IsActive
        }).ConfigureAwait(false);

        return new CreateWarehouseResultModel
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage,
            CreatedId = result.CreatedId
        };
    }
}

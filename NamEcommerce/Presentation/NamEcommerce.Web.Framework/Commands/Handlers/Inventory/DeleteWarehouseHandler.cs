using MediatR;
using NamEcommerce.Application.Contracts.Dtos.Inventory;
using NamEcommerce.Application.Contracts.Inventory;
using NamEcommerce.Web.Contracts.Commands.Models.Inventory;
using NamEcommerce.Web.Contracts.Models.Inventory;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Inventory;

public sealed class DeleteWarehouseHandler : IRequestHandler<DeleteWarehouseCommand, DeleteWarehouseResultModel>
{
    private readonly IWarehouseAppService _warehouseAppService;

    public DeleteWarehouseHandler(IWarehouseAppService warehouseAppService)
    {
        _warehouseAppService = warehouseAppService;
    }

    public async Task<DeleteWarehouseResultModel> Handle(DeleteWarehouseCommand request, CancellationToken cancellationToken)
    {
        var deleteResultDto = await _warehouseAppService.DeleteWarehouseAsync(new DeleteWarehouseAppDto(request.Id)).ConfigureAwait(false);

        return new DeleteWarehouseResultModel
        {
            Success = deleteResultDto.Success,
            ErrorMessage = deleteResultDto.ErrorMessage
        };
    }
}

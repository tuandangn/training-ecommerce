using MediatR;
using NamEcommerce.Application.Contracts.Catalog;
using NamEcommerce.Application.Contracts.Dtos.Catalog;
using NamEcommerce.Web.Contracts.Commands.Models.Catalog;
using NamEcommerce.Web.Contracts.Models.UnitMeasurements;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Catalog;

public sealed class DeleteUnitMeasurementHandler : IRequestHandler<DeleteUnitMeasurementCommand, DeleteUnitMeasurementResultModel>
{
    private readonly IUnitMeasurementAppService _unitMeasurementAppService;

    public DeleteUnitMeasurementHandler(IUnitMeasurementAppService unitMeasurementAppService)
    {
        _unitMeasurementAppService = unitMeasurementAppService;
    }

    public async Task<DeleteUnitMeasurementResultModel> Handle(DeleteUnitMeasurementCommand request, CancellationToken cancellationToken)
    {
        var deleteResultDto = await _unitMeasurementAppService.DeleteUnitMeasurementAsync(
            new DeleteUnitMeasurementAppDto(request.Id)).ConfigureAwait(false);

        return new DeleteUnitMeasurementResultModel
        {
            Success = deleteResultDto.Success,
            ErrorMessage = deleteResultDto.ErrorMessage
        };
    }
}

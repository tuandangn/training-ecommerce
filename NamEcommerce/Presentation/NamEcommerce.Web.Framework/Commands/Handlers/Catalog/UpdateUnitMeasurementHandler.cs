using MediatR;
using NamEcommerce.Application.Contracts.Catalog;
using NamEcommerce.Application.Contracts.Dtos.Catalog;
using NamEcommerce.Web.Contracts.Commands.Models.Catalog;
using NamEcommerce.Web.Contracts.Models.UnitMeasurements;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Catalog;

public sealed class UpdateUnitMeasurementHandler : IRequestHandler<UpdateUnitMeasurementCommand, UpdateUnitMeasurementResultModel>
{
    private readonly IUnitMeasurementAppService _unitMeasurementAppService;

    public UpdateUnitMeasurementHandler(IUnitMeasurementAppService unitMeasurementAppService)
    {
        _unitMeasurementAppService = unitMeasurementAppService;
    }

    public async Task<UpdateUnitMeasurementResultModel> Handle(UpdateUnitMeasurementCommand request, CancellationToken cancellationToken)
    {
        var updateResult = await _unitMeasurementAppService.UpdateUnitMeasurementAsync(new UpdateUnitMeasurementAppDto(request.Id) {
            Name = request.Name,
            DisplayOrder = request.DisplayOrder
        });

        return new UpdateUnitMeasurementResultModel
        {
            Success = updateResult.Success,
            ErrorMessage = updateResult.ErrorMessage,
            UpdatedId = updateResult.UpdatedId
        };
    }
}

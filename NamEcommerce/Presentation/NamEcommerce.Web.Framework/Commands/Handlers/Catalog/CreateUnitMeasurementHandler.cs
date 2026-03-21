using MediatR;
using NamEcommerce.Application.Contracts.Catalog;
using NamEcommerce.Application.Contracts.Dtos.Catalog;
using NamEcommerce.Web.Contracts.Commands.Models.Catalog;
using NamEcommerce.Web.Contracts.Models.UnitMeasurements;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Catalog;

public sealed class CreateUnitMeasurementHandler : IRequestHandler<CreateUnitMeasurementCommand, CreateUnitMeasurementResultModel>
{
    private readonly IUnitMeasurementAppService _unitMeasurementAppService;

    public CreateUnitMeasurementHandler(IUnitMeasurementAppService unitMeasurementAppService)
    {
        _unitMeasurementAppService = unitMeasurementAppService;
    }

    public async Task<CreateUnitMeasurementResultModel> Handle(CreateUnitMeasurementCommand request, CancellationToken cancellationToken)
    {
        var result = await _unitMeasurementAppService.CreateUnitMeasurementAsync(new CreateUnitMeasurementAppDto
        {
            Name = request.Name,
            DisplayOrder = request.DisplayOrder
        }).ConfigureAwait(false);

        return new CreateUnitMeasurementResultModel
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage,
            CreatedId = result.CreatedId ?? default
        };
    }
}

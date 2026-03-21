using MediatR;
using NamEcommerce.Application.Contracts.Catalog;
using NamEcommerce.Web.Contracts.Models.UnitMeasurements;
using NamEcommerce.Web.Contracts.Queries.Models.Catalog;
using NamEcommerce.Web.Framework.Common;

namespace NamEcommerce.Web.Framework.Queries.Handlers.Catalog;

public sealed class GetUnitMeasurementListHandler : IRequestHandler<GetUnitMeasurementListQuery, UnitMeasurementListModel>
{
    private readonly IUnitMeasurementAppService _unitMeasurementAppService;

    public GetUnitMeasurementListHandler(IUnitMeasurementAppService unitMeasurementAppService)
    {
        _unitMeasurementAppService = unitMeasurementAppService;
    }

    public async Task<UnitMeasurementListModel> Handle(GetUnitMeasurementListQuery request, CancellationToken cancellationToken)
    {
        var pagedData = await _unitMeasurementAppService.GetUnitMeasurementsAsync(request.Keywords, request.PageIndex, request.PageSize);

        var model = new UnitMeasurementListModel
        {
            Keywords = request.Keywords,
            Data = pagedData.MapToModel(item => new UnitMeasurementListModel.ItemModel(item.Id, item.Name)
            {
                DisplayOrder = item.DisplayOrder
            })
        };

        return model;
    }
}

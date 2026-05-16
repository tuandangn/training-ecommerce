using MediatR;
using NamEcommerce.Application.Contracts.Dtos.Inventory;
using NamEcommerce.Application.Contracts.Inventory;
using NamEcommerce.Web.Contracts.Models.Inventory;
using NamEcommerce.Web.Contracts.Queries.Models.Inventory;
using NamEcommerce.Web.Framework.Extensions;

namespace NamEcommerce.Web.Framework.Queries.Handlers.Inventory;

public sealed class GetOrderShortageInfoHandler(IShortageAggregationAppService shortageAggregationAppService)
    : IRequestHandler<GetOrderShortageInfoQuery, ShortageInfoModel>
{
    public async Task<ShortageInfoModel> Handle(GetOrderShortageInfoQuery request, CancellationToken cancellationToken)
    {
        var aggregation = await shortageAggregationAppService.GetAggregatedShortagesAsync(new ShortageAggregationFilterAppDto
        {
            OrderId = request.OrderId
        }).ConfigureAwait(false);

        return aggregation.ToShortageInfoModel();
    }
}

public sealed class GetDeliveryNoteShortageInfoHandler(IShortageAggregationAppService shortageAggregationAppService)
    : IRequestHandler<GetDeliveryNoteShortageInfoQuery, ShortageInfoModel>
{
    public async Task<ShortageInfoModel> Handle(GetDeliveryNoteShortageInfoQuery request, CancellationToken cancellationToken)
    {
        var aggregation = await shortageAggregationAppService.GetAggregatedShortagesAsync(new ShortageAggregationFilterAppDto
        {
            DeliveryNoteId = request.DeliveryNoteId,
            IncludeSalesOrderScope = request.IncludeSalesOrderScope
        }).ConfigureAwait(false);

        return aggregation.ToShortageInfoModel();
    }
}


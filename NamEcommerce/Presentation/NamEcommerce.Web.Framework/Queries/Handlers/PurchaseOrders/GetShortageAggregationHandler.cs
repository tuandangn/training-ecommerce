using MediatR;
using NamEcommerce.Application.Contracts.Dtos.Inventory;
using NamEcommerce.Application.Contracts.Inventory;
using NamEcommerce.Web.Contracts.Models.PurchaseOrders;
using NamEcommerce.Web.Contracts.Queries.Models.PurchaseOrders;
using NamEcommerce.Web.Framework.Extensions;

namespace NamEcommerce.Web.Framework.Queries.Handlers.PurchaseOrders;

public sealed class GetShortageAggregationHandler(IShortageAggregationAppService shortageAggregationAppService)
    : IRequestHandler<GetShortageAggregationQuery, ShortageAggregationModel>
{
    public async Task<ShortageAggregationModel> Handle(GetShortageAggregationQuery request, CancellationToken cancellationToken)
    {
        var aggregation = await shortageAggregationAppService.GetAggregatedShortagesAsync(new ShortageAggregationFilterAppDto
        {
            OrderId = request.OrderId,
            DeliveryNoteId = request.DeliveryNoteId,
            ProductId = request.ProductId,
            IncludeSalesOrderScope = request.IncludeSalesOrderScope
        }).ConfigureAwait(false);

        return aggregation.ToModel();
    }
}


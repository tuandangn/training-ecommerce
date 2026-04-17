using MediatR;
using NamEcommerce.Application.Contracts.Catalog;
using NamEcommerce.Web.Contracts.Models.Catalog;
using NamEcommerce.Web.Contracts.Queries.Models.Catalog;

namespace NamEcommerce.Web.Framework.Queries.Handlers.Catalog;

public sealed class GetProductPriceHistoryHandler : IRequestHandler<GetProductPriceHistoryQuery, ProductPriceHistoryModel>
{
    private readonly IProductAppService _productAppService;

    public GetProductPriceHistoryHandler(IProductAppService productAppService)
    {
        _productAppService = productAppService;
    }

    public async Task<ProductPriceHistoryModel> Handle(GetProductPriceHistoryQuery request, CancellationToken cancellationToken)
    {
        var historyAppDtos = await _productAppService.GetProductPriceHistoryAsync(request.ProductId).ConfigureAwait(false);

        var model = new ProductPriceHistoryModel
        {
            Items = historyAppDtos.Select(h => new ProductPriceHistoryModel.PriceHistoryItemModel
            {
                OldPrice = h.OldPrice,
                NewPrice = h.NewPrice,
                OldCostPrice = h.OldCostPrice,
                NewCostPrice = h.NewCostPrice,
                Note = h.Note,
                CreatedOnUtc = h.CreatedOnUtc
            })
        };

        return model;
    }
}

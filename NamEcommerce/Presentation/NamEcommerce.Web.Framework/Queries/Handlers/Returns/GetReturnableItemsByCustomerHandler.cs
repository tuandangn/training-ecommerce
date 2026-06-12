using MediatR;
using NamEcommerce.Application.Contracts.Returns;
using NamEcommerce.Web.Contracts.Models.Returns;
using NamEcommerce.Web.Contracts.Queries.Models.Returns;

namespace NamEcommerce.Web.Framework.Queries.Handlers.Returns;

public sealed class GetReturnableItemsByCustomerHandler
    : IRequestHandler<GetReturnableItemsByCustomerQuery, List<ReturnableItemModel>>
{
    private readonly ICustomerReturnAppService _customerReturnAppService;

    public GetReturnableItemsByCustomerHandler(ICustomerReturnAppService customerReturnAppService)
    {
        _customerReturnAppService = customerReturnAppService;
    }

    public async Task<List<ReturnableItemModel>> Handle(
        GetReturnableItemsByCustomerQuery request, CancellationToken cancellationToken)
    {
        var dtos = await _customerReturnAppService
            .GetReturnableItemsByCustomerAsync(request.CustomerId, request.ExcludeReturnId)
            .ConfigureAwait(false);

        return dtos.Select(d => new ReturnableItemModel
        {
            ProductId = d.ProductId,
            ProductName = d.ProductName,
            Unit = d.Unit,
            OriginalQty = d.OriginalQty,
            AlreadyReturnedQty = d.AlreadyReturnedQty,
            UnitPrice = d.UnitPrice,
            SourceItemId = d.SourceItemId,
            QuantityDecimalPlaces = d.QuantityDecimalPlaces
        }).ToList();
    }
}

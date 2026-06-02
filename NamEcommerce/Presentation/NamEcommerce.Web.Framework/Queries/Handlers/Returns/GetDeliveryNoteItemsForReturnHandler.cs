using MediatR;
using NamEcommerce.Application.Contracts.Returns;
using NamEcommerce.Web.Contracts.Models.Returns;
using NamEcommerce.Web.Contracts.Queries.Models.Returns;

namespace NamEcommerce.Web.Framework.Queries.Handlers.Returns;

/// <summary>
/// Lấy danh sách items có thể trả của một phiếu xuất kho — bao gồm số lượng đã trả.
/// </summary>
public sealed class GetDeliveryNoteItemsForReturnHandler
    : IRequestHandler<GetDeliveryNoteItemsForReturnQuery, List<ReturnableItemModel>>
{
    private readonly ICustomerReturnAppService _customerReturnAppService;

    public GetDeliveryNoteItemsForReturnHandler(ICustomerReturnAppService customerReturnAppService)
    {
        _customerReturnAppService = customerReturnAppService;
    }

    public async Task<List<ReturnableItemModel>> Handle(
        GetDeliveryNoteItemsForReturnQuery request, CancellationToken cancellationToken)
    {
        var dtos = await _customerReturnAppService
            .GetDeliveryNoteItemsForReturnAsync(request.DeliveryNoteId, request.ExcludeReturnId)
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

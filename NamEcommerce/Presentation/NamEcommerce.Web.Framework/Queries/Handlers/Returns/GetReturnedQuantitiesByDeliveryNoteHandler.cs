using MediatR;
using NamEcommerce.Application.Contracts.Returns;
using NamEcommerce.Web.Contracts.Queries.Models.Returns;

namespace NamEcommerce.Web.Framework.Queries.Handlers.Returns;

public sealed class GetReturnedQuantitiesByDeliveryNoteHandler
    : IRequestHandler<GetReturnedQuantitiesByDeliveryNoteQuery, IReadOnlyDictionary<Guid, ReturnedQuantitySummary>>
{
    private const int FullPageSize = 500;

    private const int DraftStatus = 0;
    private const int InspectingStatus = 1;
    private const int ConfirmedStatus = 2;
    private const int CancelledStatus = 3;

    private readonly ICustomerReturnAppService _customerReturnAppService;

    public GetReturnedQuantitiesByDeliveryNoteHandler(ICustomerReturnAppService customerReturnAppService)
    {
        _customerReturnAppService = customerReturnAppService;
    }

    public async Task<IReadOnlyDictionary<Guid, ReturnedQuantitySummary>> Handle(GetReturnedQuantitiesByDeliveryNoteQuery request, CancellationToken cancellationToken)
    {
        var (_, items) = await _customerReturnAppService.GetListAsync(
            customerId: null,
            deliveryNoteId: request.DeliveryNoteId,
            status: null,
            pageIndex: 0,
            pageSize: FullPageSize).ConfigureAwait(false);

        var dict = items
            .Where(r => r.Status != CancelledStatus)
            .SelectMany(r => r.Items.Select(i => new { r.Status, Item = i }))
            .Where(x => x.Item.DeliveryNoteItemId.HasValue)
            .GroupBy(x => x.Item.DeliveryNoteItemId!.Value)
            .ToDictionary(
                g => g.Key,
                g => new ReturnedQuantitySummary(
                    ConfirmedQuantity: g.Where(x => x.Status == ConfirmedStatus).Sum(x => x.Item.AcceptedQuantity),
                    PendingQuantity: g.Where(x => x.Status == DraftStatus || x.Status == InspectingStatus).Sum(x => x.Item.RequestedQuantity)
                ));

        return dict;
    }
}

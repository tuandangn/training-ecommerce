using MediatR;
using NamEcommerce.Application.Contracts.CustomerPortal;
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
    private const int PortalPendingReviewStatus = 0;
    private const int PortalAcceptedStatus = 1;

    private readonly ICustomerReturnAppService _customerReturnAppService;
    private readonly ICustomerPortalAdminAppService _customerPortalAdminAppService;

    public GetReturnedQuantitiesByDeliveryNoteHandler(
        ICustomerReturnAppService customerReturnAppService,
        ICustomerPortalAdminAppService customerPortalAdminAppService)
    {
        _customerReturnAppService = customerReturnAppService;
        _customerPortalAdminAppService = customerPortalAdminAppService;
    }

    public async Task<IReadOnlyDictionary<Guid, ReturnedQuantitySummary>> Handle(GetReturnedQuantitiesByDeliveryNoteQuery request, CancellationToken cancellationToken)
    {
        var returnableItems = await _customerReturnAppService
            .GetDeliveryNoteItemsForReturnAsync(request.DeliveryNoteId)
            .ConfigureAwait(false);
        var deliveryNoteItemIds = returnableItems
            .Where(item => item.SourceItemId.HasValue)
            .Select(item => item.SourceItemId!.Value)
            .ToHashSet();

        var (_, items) = await _customerReturnAppService.GetListAsync(
            pageIndex: 0,
            pageSize: FullPageSize,
            customerId: null,
            deliveryNoteId: null,
            status: null).ConfigureAwait(false);

        var dict = items
            .Where(r => r.Status != CancelledStatus)
            .SelectMany(r => r.Items.Select(i => new { r.Status, r.CompensateInNextDelivery, Item = i }))
            .Where(x => x.Item.DeliveryNoteItemId.HasValue && deliveryNoteItemIds.Contains(x.Item.DeliveryNoteItemId.Value))
            .GroupBy(x => x.Item.DeliveryNoteItemId!.Value)
            .ToDictionary(
                g => g.Key,
                g => new ReturnedQuantitySummary(
                    ConfirmedQuantity: g.Where(x => x.Status == ConfirmedStatus).Sum(x => x.Item.AcceptedQuantity),
                    PendingQuantity: g.Where(x => x.Status == DraftStatus || x.Status == InspectingStatus).Sum(x => x.Item.RequestedQuantity),
                    ConfirmedCompensatedQuantity: g.Where(x => x.Status == ConfirmedStatus && x.CompensateInNextDelivery).Sum(x => x.Item.AcceptedQuantity),
                    ActiveCompensatedQuantity: g.Where(x => x.CompensateInNextDelivery && x.Status != DraftStatus).Sum(x => x.Item.AcceptedQuantity)
                ));

        var portalRequests = await _customerPortalAdminAppService.GetReturnRequestsAsync().ConfigureAwait(false);
        var portalPendingQuantities = portalRequests
            .Where(r => r.Status == PortalPendingReviewStatus || r.Status == PortalAcceptedStatus)
            .SelectMany(r => r.Items)
            .Where(i => deliveryNoteItemIds.Contains(i.DeliveryNoteItemId))
            .GroupBy(i => i.DeliveryNoteItemId)
            .ToDictionary(g => g.Key, g => g.Sum(i => i.RequestedQuantity));

        foreach (var (deliveryNoteItemId, pendingQuantity) in portalPendingQuantities)
        {
            dict.TryGetValue(deliveryNoteItemId, out var summary);
            dict[deliveryNoteItemId] = new ReturnedQuantitySummary(
                summary?.ConfirmedQuantity ?? 0m,
                (summary?.PendingQuantity ?? 0m) + pendingQuantity,
                summary?.ConfirmedCompensatedQuantity ?? 0m,
                summary?.ActiveCompensatedQuantity ?? 0m);
        }

        return dict;
    }
}

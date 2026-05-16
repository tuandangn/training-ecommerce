using MediatR;
using NamEcommerce.Application.Contracts.Dtos.Inventory;
using NamEcommerce.Application.Contracts.Inventory;
using NamEcommerce.Web.Contracts.Commands.Models.PurchaseOrders;
using NamEcommerce.Web.Contracts.Extensions;
using NamEcommerce.Web.Contracts.Models.PurchaseOrders;
using NamEcommerce.Web.Framework.Services;

namespace NamEcommerce.Web.Framework.Commands.Handlers.PurchaseOrders;

public sealed class CheckExistingDraftsHandler(IShortageAggregationAppService shortageAggregationAppService)
    : IRequestHandler<CheckExistingDraftsCommand, ExistingDraftPurchaseOrdersResultModel>
{
    public async Task<ExistingDraftPurchaseOrdersResultModel> Handle(CheckExistingDraftsCommand request, CancellationToken cancellationToken)
    {
        var drafts = await shortageAggregationAppService.CheckExistingDraftsAsync(request.VendorIds).ConfigureAwait(false);
        return new ExistingDraftPurchaseOrdersResultModel
        {
            Items = drafts.Select(draft => new ExistingDraftPurchaseOrderModel
            {
                Id = draft.Id,
                VendorId = draft.VendorId,
                Code = draft.Code,
                CreatedOn = draft.CreatedOnUtc.ToLocalTime(),
                ExpectedDeliveryDate = draft.ExpectedDeliveryDateUtc?.ToLocalTime()
            }).ToList()
        };
    }
}

public sealed class CheckRelatedPurchaseOrdersHandler(IShortageAggregationAppService shortageAggregationAppService)
    : IRequestHandler<CheckRelatedPurchaseOrdersCommand, RelatedPurchaseOrdersResultModel>
{
    private const int DraftStatus = 10;
    private const int SubmittedStatus = 20;
    private const int ApprovedStatus = 30;

    public async Task<RelatedPurchaseOrdersResultModel> Handle(CheckRelatedPurchaseOrdersCommand request, CancellationToken cancellationToken)
    {
        var purchaseOrders = await shortageAggregationAppService
            .CheckRelatedPurchaseOrdersAsync(new CheckRelatedPurchaseOrdersAppDto
            {
                VendorIds = request.VendorIds,
                ProductIds = request.ProductIds
            })
            .ConfigureAwait(false);

        return new RelatedPurchaseOrdersResultModel
        {
            Items = purchaseOrders.Select(purchaseOrder =>
            {
                return new RelatedPurchaseOrderModel
                {
                    Id = purchaseOrder.Id,
                    VendorId = purchaseOrder.VendorId,
                    VendorName = purchaseOrder.VendorName,
                    Code = purchaseOrder.Code,
                    Status = purchaseOrder.Status,
                    StatusName = GetStatusName(purchaseOrder.Status),
                    StatusClass = GetStatusClass(purchaseOrder.Status),
                    CreatedOn = purchaseOrder.CreatedOnUtc.ToLocalTime(),
                    ExpectedDeliveryDate = purchaseOrder.ExpectedDeliveryDateUtc?.ToLocalTime(),
                    TotalAmount = purchaseOrder.TotalAmount,
                    ItemCount = purchaseOrder.ItemCount,
                    CanAllocate = purchaseOrder.CanAllocate,
                    CanMerge = purchaseOrder.CanMerge,
                    Items = purchaseOrder.Items.Select(item => new RelatedPurchaseOrderItemModel
                    {
                        PurchaseOrderItemId = item.PurchaseOrderItemId,
                        ProductId = item.ProductId,
                        ProductName = item.ProductName,
                        QuantityOrdered = item.QuantityOrdered,
                        AllocatedQuantity = item.AllocatedQuantity,
                        ReceivedQuantity = item.ReceivedQuantity,
                        AvailableForAllocation = item.AvailableForAllocation,
                        UnitCost = item.UnitCost
                    }).ToList()
                };
            }).ToList()
        };
    }

    private static string GetStatusName(int status)
        => status switch
        {
            DraftStatus => "Bản nháp",
            SubmittedStatus => "Đã gửi",
            ApprovedStatus => "Đã duyệt",
            _ => status.ToString()
        };

    private static string GetStatusClass(int status)
        => status switch
        {
            DraftStatus => "secondary",
            SubmittedStatus => "primary",
            ApprovedStatus => "info",
            _ => "secondary"
        };
}

public sealed class CreatePurchaseOrdersFromShortageHandler(IShortageAggregationAppService shortageAggregationAppService)
    : IRequestHandler<CreatePurchaseOrdersFromShortageCommand, CreatePurchaseOrdersFromShortageResultModel>
{
    public async Task<CreatePurchaseOrdersFromShortageResultModel> Handle(CreatePurchaseOrdersFromShortageCommand request, CancellationToken cancellationToken)
    {
        var result = await shortageAggregationAppService.CreatePurchaseOrdersFromShortageAsync(new CreatePosFromShortageAppDto
        {
            Groups = request.Groups.Select(group => new CreatePoFromShortageGroupAppDto
            {
                VendorId = group.VendorId,
                WarehouseId = group.WarehouseId,
                MergeIntoExistingPoId = group.MergeIntoExistingPoId,
                ExpectedDeliveryDateUtc = DateTimeHelper.ToUniversalTime(group.ExpectedDeliveryDate.ToEndOfDate()),
                Note = group.Note,
                Items = group.Items.Select(item => new CreatePoFromShortageItemAppDto
                {
                    OrderItemId = item.OrderItemId,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitCost = item.UnitCost,
                    Note = item.Note,
                    Actions = item.Actions.Select(action => new CreatePoFromShortageActionAppDto
                    {
                        ActionType = action.ActionType,
                        PurchaseOrderId = action.PurchaseOrderId,
                        PurchaseOrderItemId = action.PurchaseOrderItemId,
                        Quantity = action.Quantity,
                        UnitCost = action.UnitCost
                    }).ToList()
                }).ToList()
            }).ToList()
        }).ConfigureAwait(false);

        return new CreatePurchaseOrdersFromShortageResultModel
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage,
            Items = result.Items.Select(item => new CreatedPurchaseOrderResultModel
            {
                PurchaseOrderId = item.PurchaseOrderId,
                PurchaseOrderCode = item.PurchaseOrderCode,
                CreatedNew = item.CreatedNew,
                VendorId = item.VendorId
            }).ToList()
        };
    }
}

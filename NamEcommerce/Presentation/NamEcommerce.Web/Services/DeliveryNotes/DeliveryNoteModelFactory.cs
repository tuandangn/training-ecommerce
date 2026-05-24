using MediatR;
using NamEcommerce.Application.Contracts.DeliveryNotes;
using NamEcommerce.Application.Contracts.Orders;
using NamEcommerce.Application.Contracts.PurchaseOrders;
using NamEcommerce.Domain.Shared.Enums.DeliveryNotes;
using NamEcommerce.Domain.Shared.Enums.PurchaseOrders;
using NamEcommerce.Web.Contracts.Models.DeliveryNotes;
using NamEcommerce.Web.Contracts.Queries.Models.Returns;
using NamEcommerce.Web.Contracts.Services;
using NamEcommerce.Application.Contracts.Media;
using NamEcommerce.Web.Contracts.Models.Common;
using NamEcommerce.Web.Contracts.Queries.Models.Inventory;

using NamEcommerce.Application.Contracts.Inventory;
using NamEcommerce.Web.Models.DeliveryNotes;
using NamEcommerce.Web.Framework.Services;

namespace NamEcommerce.Web.Services.DeliveryNotes;

public sealed class DeliveryNoteModelFactory : IDeliveryNoteModelFactory
{
    private readonly IDeliveryNoteAppService _deliveryNoteAppService;
    private readonly IOrderAppService _orderAppService;
    private readonly IDirectShipAppService _directShipAppService;
    private readonly IPictureAppService _pictureAppService;
    private readonly IWarehouseAppService _warehouseAppService;
    private readonly IWebHelper _webHelper;
    private readonly IMediator _mediator;

    public DeliveryNoteModelFactory(
        IDeliveryNoteAppService deliveryNoteAppService,
        IOrderAppService orderAppService,
        IDirectShipAppService directShipAppService,
        IPictureAppService pictureAppService,
        IWarehouseAppService warehouseAppService,
        IWebHelper webHelper,
        IMediator mediator)
    {
        _deliveryNoteAppService = deliveryNoteAppService;
        _orderAppService = orderAppService;
        _directShipAppService = directShipAppService;
        _pictureAppService = pictureAppService;
        _warehouseAppService = warehouseAppService;
        _webHelper = webHelper;
        _mediator = mediator;
    }

    public async Task<DeliveryNoteListModel> PrepareDeliveryNoteListModelAsync(DeliveryNoteSearchModel searchModel)
    {
        var pagedData = await _deliveryNoteAppService.GetListAsync(
            searchModel.Keywords,
            searchModel.PageIndex - 1,
            searchModel.PageSize).ConfigureAwait(false);

        var deliveryNotes = pagedData.Items.Select(deliveryNote => new DeliveryNoteListItemModel
        {
            Id = deliveryNote.Id,
            Code = deliveryNote.Code,
            CustomerName = deliveryNote.CustomerName,
            CustomerPhone = deliveryNote.CustomerPhone,
            ShippingAddress = deliveryNote.ShippingAddress,
            OrderId = deliveryNote.OrderId,
            OrderCode = deliveryNote.OrderCode ?? string.Empty,
            TotalAmount = deliveryNote.TotalAmount,
            Status = deliveryNote.Status,
            StatusName = GetStatusName((DeliveryNoteStatus)deliveryNote.Status),
            WarehouseId = deliveryNote.WarehouseId,
            CreatedOnUtc = deliveryNote.CreatedOnUtc,
            DeliveredOnUtc = deliveryNote.DeliveredOnUtc
        }).ToList();

        foreach (var deliveryNote in deliveryNotes)
        {
            var warehouse = await _warehouseAppService.GetWarehouseByIdAsync(deliveryNote.WarehouseId).ConfigureAwait(false);
            deliveryNote.WarehouseName = warehouse?.Name;
        }

        var data = PagedDataModel.Create(deliveryNotes, searchModel.PageIndex, searchModel.PageSize, pagedData.Pagination.TotalCount);

        var model = new DeliveryNoteListModel
        {
            Keywords = searchModel.Keywords,
            Data = data
        };

        return model;
    }

    public async Task<CreateDeliveryNoteModel> PrepareCreateDeliveryNoteModelAsync(Guid orderId, CreateDeliveryNoteModel? oldModel = null)
    {
        var order = await _orderAppService.GetOrderByIdAsync(orderId).ConfigureAwait(false);
        if (order == null)
            throw new ArgumentException("Order not found");

        var model = oldModel ?? new CreateDeliveryNoteModel
        {
            OrderId = order.Id,
            ShowPrice = false, // Default is hide price
            Items = []
        };
        model.OrderCode = order.Code;
        //*TODO*
        model.PlacedOn = DateTimeHelper.ToLocalTime(order.CreatedOnUtc);
        model.ExpectedShippingDate = DateTimeHelper.ToLocalTime(order.ExpectedShippingDateUtc);
        model.OrderNote = order.Note;
        model.CustomerName = order.CustomerName ?? string.Empty;
        model.CustomerAddress = order.CustomerAddress;
        model.CustomerPhoneNumber = order.CustomerPhone ?? string.Empty;
        model.ShippingAddress = order.ShippingAddress ?? string.Empty;

        var warehouses = await _warehouseAppService.GetWarehousesAsync().ConfigureAwait(false);
        model.AvailableWarehouses = new EntityOptionListModel
        {
            Options = warehouses.Items.Select(warehouse => new EntityOptionListModel.EntityOptionModel
            {
                Id = warehouse.Id,
                Name = warehouse.Name
            }).ToList()
        };

        var deliveryNotes = await _deliveryNoteAppService.GetByOrderIdAsync(orderId).ConfigureAwait(false);
        var orderItemIds = order.Items.Select(item => item.Id).ToList();
        var directShipOutstandingQuantities = (await _directShipAppService
                .GetDirectShipAllocationsForOrderAsync(orderItemIds)
                .ConfigureAwait(false))
            .Where(allocation => allocation.Status != (int)AllocationStatus.Cancelled)
            .GroupBy(allocation => allocation.OrderItemId)
            .ToDictionary(
                group => group.Key,
                group => group.Sum(allocation => Math.Max(0m, allocation.AllocatedQuantity - allocation.ReceivedQuantity)));

        foreach (var orderItem in order.Items)
        {
            var deliveredQty = deliveryNotes
                .Where(deliveryNote => deliveryNote.Status != (int)DeliveryNoteStatus.Cancelled)
                .SelectMany(n => n.Items)
                .Where(i => i.OrderItemId == orderItem.Id)
                .Sum(i => i.Quantity);
            var directShipOutstandingQty = directShipOutstandingQuantities.GetValueOrDefault(orderItem.Id);
            var remainingQty = orderItem.Quantity - deliveredQty - directShipOutstandingQty;
            if (remainingQty > 0)
            {
                var existingItem = model.Items.FirstOrDefault(item => item.OrderItemId == orderItem.Id);

                var itemModel = existingItem ?? new CreateDeliveryNoteItemModel
                {
                    OrderItemId = orderItem.Id,
                    Quantity = remainingQty,
                    Selected = true
                };
                itemModel.ProductName = orderItem.ProductName ?? string.Empty;
                itemModel.OrderedQuantity = orderItem.Quantity;
                itemModel.PreviouslyDeliveredQuantity = deliveredQty;
                itemModel.UnitPrice = orderItem.UnitPrice;

                if (existingItem is null)
                    model.Items.Add(itemModel);
            }
            else
            {
                var itemModel = model.Items.FirstOrDefault(item => item.OrderItemId == orderItem.Id);
                if (itemModel is not null)
                    model.Items.Remove(itemModel);
            }
        }

        return model;
    }

    public async Task<DeliveryNoteDetailsModel> PrepareDeliveryNoteDetailsModelAsync(Guid id)
    {
        var deliveryNote = await _deliveryNoteAppService.GetByIdAsync(id).ConfigureAwait(false);
        if (deliveryNote == null)
            throw new ArgumentException("Delivery note not found");

        var order = await _orderAppService.GetOrderByIdAsync(deliveryNote.OrderId).ConfigureAwait(false);

        // Tổng số lượng đã trả (Confirmed) — group theo DeliveryNoteItemId.
        var returnedQuantities = await _mediator.Send(new GetReturnedQuantitiesByDeliveryNoteQuery
        {
            DeliveryNoteId = id
        }).ConfigureAwait(false);

        var model = new DeliveryNoteDetailsModel
        {
            Id = deliveryNote.Id,
            Code = deliveryNote.Code,
            OrderId = deliveryNote.OrderId,
            OrderCode = order?.Code ?? string.Empty,
            CustomerId = deliveryNote.CustomerId,
            CustomerName = deliveryNote.CustomerName,
            CustomerPhone = deliveryNote.CustomerPhone,
            CustomerAddress = deliveryNote.CustomerAddress,
            ShippingAddress = deliveryNote.ShippingAddress,
            ShowPrice = deliveryNote.ShowPrice,
            Note = deliveryNote.Note,
            Status = deliveryNote.Status,
            SourceType = deliveryNote.SourceType,
            IsDirectShip = deliveryNote.IsDirectShip,
            DeliveryConfirmationStatus = deliveryNote.DeliveryConfirmationStatus,
            StatusName = GetStatusName((DeliveryNoteStatus)deliveryNote.Status),
            CreatedOnUtc = deliveryNote.CreatedOnUtc,
            DeliveredOnUtc = deliveryNote.DeliveredOnUtc,
            DeliveryProofPictureId = deliveryNote.DeliveryProofPictureId,
            DeliveryReceiverName = deliveryNote.DeliveryReceiverName,
            TotalAmount = deliveryNote.TotalAmount,
            Surcharge = deliveryNote.Surcharge,
            SurchargeReason = deliveryNote.SurchargeReason,
            AmountToCollect = deliveryNote.AmountToCollect,
            WarehouseId = deliveryNote.WarehouseId,
            Items = deliveryNote.Items.Select(i =>
            {
                returnedQuantities.TryGetValue(i.Id, out var summary);
                return new DeliveryNoteItemModel
                {
                    Id = i.Id,
                    ProductName = i.ProductName,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    SubTotal = i.SubTotal,
                    ReturnedQuantity = summary?.ConfirmedQuantity ?? 0m,
                    PendingReturnQuantity = summary?.PendingQuantity ?? 0m
                };
            }).ToList()
        };

        var warehouseDetails = await _warehouseAppService.GetWarehouseByIdAsync(deliveryNote.WarehouseId).ConfigureAwait(false);
        model.WarehouseName = warehouseDetails?.Name;

        if (deliveryNote.DeliveryProofPictureId.HasValue)
        {
            var picture = await _pictureAppService.GetBase64PictureByIdAsync(deliveryNote.DeliveryProofPictureId.Value).ConfigureAwait(false);
            if (picture != null)
            {
                model.DeliveryProofPictureUrl = picture.Base64Value;
            }
        }

        model.ShortageInfo = await _mediator.Send(new GetDeliveryNoteShortageInfoQuery
        {
            DeliveryNoteId = id
        }).ConfigureAwait(false);

        return model;
    }

    private string GetStatusName(DeliveryNoteStatus status)
    {
        return status switch
        {
            DeliveryNoteStatus.Draft => "Bản nháp",
            DeliveryNoteStatus.Confirmed => "Đã xác nhận",
            DeliveryNoteStatus.Delivering => "Đang giao",
            DeliveryNoteStatus.Delivered => "Đã giao",
            DeliveryNoteStatus.Cancelled => "Đã hủy",
            _ => status.ToString()
        };
    }
}

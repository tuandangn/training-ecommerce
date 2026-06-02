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
using NamEcommerce.Web.Contracts.Configurations;

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
    private readonly AppConfig _appConfig;

    public DeliveryNoteModelFactory(
        IDeliveryNoteAppService deliveryNoteAppService,
        IOrderAppService orderAppService,
        IDirectShipAppService directShipAppService,
        IPictureAppService pictureAppService,
        IWarehouseAppService warehouseAppService,
        IWebHelper webHelper,
        IMediator mediator,
        AppConfig appConfig)
    {
        _deliveryNoteAppService = deliveryNoteAppService;
        _orderAppService = orderAppService;
        _directShipAppService = directShipAppService;
        _pictureAppService = pictureAppService;
        _warehouseAppService = warehouseAppService;
        _webHelper = webHelper;
        _mediator = mediator;
        _appConfig = appConfig;
    }

    public async Task<DeliveryNoteListModel> PrepareDeliveryNoteListModelAsync(DeliveryNoteSearchModel searchModel)
    {
        var pageNumber = searchModel?.PageNumber ?? 1;
        var pageSize = searchModel?.PageSize ?? 0;
        if (pageNumber <= 0) pageNumber = 1;
        if (pageSize <= 0) pageSize = _appConfig.DefaultPageSize;
        if (_appConfig.PageSizeOptions.Contains(pageSize)) pageSize = _appConfig.DefaultPageSize;

        var pagedData = await _deliveryNoteAppService.GetListAsync(
            searchModel.Keywords,
            pageNumber - 1,
            pageSize).ConfigureAwait(false);

        var deliveryNotes = pagedData.Items.Select(deliveryNote => new DeliveryNoteListItemModel
        {
            Id = deliveryNote.Id,
            Code = deliveryNote.Code,
            CustomerName = deliveryNote.CustomerName,
            CustomerPhone = deliveryNote.CustomerPhone,
            ShippingAddress = deliveryNote.ShippingAddress,
            IsDirectShip = deliveryNote.IsDirectShip,
            OrderId = deliveryNote.OrderId,
            OrderCode = deliveryNote.OrderCode ?? string.Empty,
            TotalAmount = deliveryNote.TotalAmount,
            Status = deliveryNote.Status,
            StatusName = GetStatusName((DeliveryNoteStatus)deliveryNote.Status),
            WarehouseId = deliveryNote.WarehouseId,
            CreatedOnUtc = deliveryNote.CreatedOnUtc,
            DeliveredOnUtc = deliveryNote.DeliveredOnUtc,
            Items = deliveryNote.Items.Select(item => new DeliveryNoteListItemProductModel
            {
                Id = item.Id,
                ProductName = item.ProductName,
                Quantity = item.Quantity
            }).ToList()
        }).ToList();

        foreach (var deliveryNote in deliveryNotes)
        {
            var warehouse = await _warehouseAppService.GetWarehouseByIdAsync(deliveryNote.WarehouseId).ConfigureAwait(false);
            deliveryNote.WarehouseName = warehouse?.Name;
        }

        var data = PagedDataModel.Create(deliveryNotes, pagedData.Pagination.PageIndex, pagedData.Pagination.PageSize, pagedData.Pagination.TotalCount);

        var model = new DeliveryNoteListModel
        {
            Keywords = searchModel.Keywords,
            Data = data,
            AvailableWarehouses = await _mediator.Send(new GetWarehouseOptionListQuery()).ConfigureAwait(false)
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
        model.OrderId = order.Id;
        model.OrderCode = order.Code;
        //*TODO*
        model.PlacedOn = DateTimeHelper.ToLocalTime(order.CreatedOnUtc);
        model.ExpectedShippingDate = DateTimeHelper.ToLocalTime(order.ExpectedShippingDateUtc);
        model.OrderNote = order.Note;
        model.CustomerName = order.CustomerName ?? string.Empty;
        model.CustomerAddress = order.CustomerAddress;
        model.CustomerPhoneNumber = order.CustomerPhone ?? string.Empty;
        model.ShippingAddress = order.ShippingAddress ?? string.Empty;

        model.AvailableWarehouses = await _mediator.Send(new GetWarehouseOptionListQuery()).ConfigureAwait(false);

        var deliveryNotes = await _deliveryNoteAppService.GetByOrderIdAsync(orderId).ConfigureAwait(false);
        var activeDeliveryNotes = deliveryNotes
            .Where(note => note.Status != (int)DeliveryNoteStatus.Cancelled)
            .ToList();
        var returnedByDeliveryNoteItemId = new Dictionary<Guid, decimal>();
        foreach (var deliveryNote in activeDeliveryNotes)
        {
            var returnedByItem = await _mediator.Send(new GetReturnedQuantitiesByDeliveryNoteQuery
            {
                DeliveryNoteId = deliveryNote.Id
            }).ConfigureAwait(false);

            foreach (var noteItem in deliveryNote.Items.Where(item => item.OrderItemId != Guid.Empty))
            {
                if (!returnedByItem.TryGetValue(noteItem.Id, out var summary))
                    continue;

                var returnedQuantity = Math.Max(0m, summary.ActiveCompensatedQuantity);
                returnedByDeliveryNoteItemId[noteItem.Id] = Math.Min(noteItem.Quantity, returnedQuantity);
            }
        }

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
            var deliveredQuantity = activeDeliveryNotes
                .SelectMany(note => note.Items)
                .Where(item => item.OrderItemId == orderItem.Id)
                .Sum(item => item.Quantity);
            var returnedQuantity = activeDeliveryNotes
                .SelectMany(note => note.Items)
                .Where(item => item.OrderItemId == orderItem.Id)
                .Sum(item => returnedByDeliveryNoteItemId.GetValueOrDefault(item.Id));
            var netDeliveredQuantity = Math.Max(0m, deliveredQuantity - returnedQuantity);
            var deliveredQtyForRemaining = netDeliveredQuantity;
            var directShipOutstandingQty = directShipOutstandingQuantities.GetValueOrDefault(orderItem.Id);
            var remainingQty = orderItem.Quantity - deliveredQtyForRemaining - directShipOutstandingQty;
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
                itemModel.ProductId = orderItem.ProductId;
                itemModel.OrderedQuantity = orderItem.Quantity;
                itemModel.PreviouslyDeliveredQuantity = deliveredQuantity;
                itemModel.RemainingQuantity = remainingQty;
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
                    PendingReturnQuantity = summary?.PendingQuantity ?? 0m,
                    CompensatedReturnQuantity = summary?.ActiveCompensatedQuantity ?? 0m
                };
            }).ToList()
        };

        model.AvailableWarehouses = await _mediator.Send(new GetWarehouseOptionListQuery()).ConfigureAwait(false);
        model.WarehouseName = model.AvailableWarehouses?.Options.FirstOrDefault(warehouse => warehouse.Id == deliveryNote.WarehouseId)?.Name;

        if (deliveryNote.DeliveryProofPictureId.HasValue)
        {
            var picture = await _pictureAppService.GetBase64PictureByIdAsync(deliveryNote.DeliveryProofPictureId.Value).ConfigureAwait(false);
            if (picture != null)
            {
                model.DeliveryProofPictureUrl = picture.Base64Value;
            }
        }

        if (deliveryNote.DeliveryConfirmationStatus == (int)DeliveryConfirmationStatus.Rejected)
        {
            model.RejectionReason = deliveryNote.ConfirmedNote;
            model.ReturnedToWarehouseName = await _mediator.Send(new GetDeliveryNoteReturnWarehouseQuery
            {
                DeliveryNoteId = id,
                DeliveryNoteWarehouseId = deliveryNote.WarehouseId
            }).ConfigureAwait(false);
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

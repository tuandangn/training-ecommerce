using NamEcommerce.Application.Contracts.DeliveryNotes;
using NamEcommerce.Application.Contracts.Orders;
using NamEcommerce.Domain.Shared.Enums.DeliveryNotes;
using NamEcommerce.Web.Contracts.Models.DeliveryNotes;
using NamEcommerce.Web.Contracts.Services;
using NamEcommerce.Application.Contracts.Media;
using NamEcommerce.Web.Contracts.Models.Common;

using NamEcommerce.Application.Contracts.Inventory;
using NamEcommerce.Web.Models.DeliveryNotes;

namespace NamEcommerce.Web.Services.DeliveryNotes;

public sealed class DeliveryNoteModelFactory : IDeliveryNoteModelFactory
{
    private readonly IDeliveryNoteAppService _deliveryNoteAppService;
    private readonly IOrderAppService _orderAppService;
    private readonly IPictureAppService _pictureAppService;
    private readonly IWarehouseAppService _warehouseAppService;
    private readonly IWebHelper _webHelper;

    public DeliveryNoteModelFactory(
        IDeliveryNoteAppService deliveryNoteAppService,
        IOrderAppService orderAppService,
        IPictureAppService pictureAppService,
        IWarehouseAppService warehouseAppService,
        IWebHelper webHelper)
    {
        _deliveryNoteAppService = deliveryNoteAppService;
        _orderAppService = orderAppService;
        _pictureAppService = pictureAppService;
        _warehouseAppService = warehouseAppService;
        _webHelper = webHelper;
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
        model.OrderNote = order.Note;
        model.CustomerName = order.CustomerName ?? string.Empty;
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
        foreach (var orderItem in order.Items)
        {
            var deliveredQty = deliveryNotes
                .Where(deliveryNote => deliveryNote.Status != (int)DeliveryNoteStatus.Cancelled)
                .SelectMany(n => n.Items)
                .Where(i => i.OrderItemId == orderItem.Id)
                .Sum(i => i.Quantity);
            var remainingQty = orderItem.Quantity - deliveredQty;
            if (remainingQty > 0)
            {
                var itemModel = model.Items.FirstOrDefault(item => item.OrderItemId == orderItem.Id)
                    ?? new CreateDeliveryNoteItemModel
                    {
                        OrderItemId = orderItem.Id,
                        Quantity = remainingQty,
                        Selected = true
                    };
                itemModel.ProductName = orderItem.ProductName ?? string.Empty;
                itemModel.OrderedQuantity = orderItem.Quantity;
                itemModel.PreviouslyDeliveredQuantity = deliveredQty;
                itemModel.UnitPrice = orderItem.UnitPrice;

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

        var model = new DeliveryNoteDetailsModel
        {
            Id = deliveryNote.Id,
            Code = deliveryNote.Code,
            OrderId = deliveryNote.OrderId,
            OrderCode = order?.Code ?? string.Empty,
            CustomerName = deliveryNote.CustomerName,
            CustomerPhone = deliveryNote.CustomerPhone,
            CustomerAddress = deliveryNote.CustomerAddress,
            ShippingAddress = deliveryNote.ShippingAddress,
            ShowPrice = deliveryNote.ShowPrice,
            Note = deliveryNote.Note,
            Status = deliveryNote.Status,
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
            Items = deliveryNote.Items.Select(i => new DeliveryNoteItemModel
            {
                Id = i.Id,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                SubTotal = i.SubTotal
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

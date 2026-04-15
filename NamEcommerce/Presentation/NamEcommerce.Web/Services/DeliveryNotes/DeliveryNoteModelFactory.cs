using NamEcommerce.Application.Contracts.DeliveryNotes;
using NamEcommerce.Application.Contracts.Orders;
using NamEcommerce.Domain.Shared.Enums.DeliveryNotes;
using NamEcommerce.Web.Contracts.Models.DeliveryNotes;
using NamEcommerce.Web.Contracts.Services;
using NamEcommerce.Application.Contracts.Media;
using NamEcommerce.Web.Framework.Services;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Services.DeliveryNotes;

public sealed class DeliveryNoteModelFactory : IDeliveryNoteModelFactory
{
    private readonly IDeliveryNoteAppService _deliveryNoteAppService;
    private readonly IOrderAppService _orderAppService;
    private readonly IPictureAppService _pictureAppService;
    private readonly IWebHelper _webHelper;

    public DeliveryNoteModelFactory(
        IDeliveryNoteAppService deliveryNoteAppService,
        IOrderAppService orderAppService,
        IPictureAppService pictureAppService,
        IWebHelper webHelper)
    {
        _deliveryNoteAppService = deliveryNoteAppService;
        _orderAppService = orderAppService;
        _pictureAppService = pictureAppService;
        _webHelper = webHelper;
    }

    public async Task<DeliveryNoteListModel> PrepareDeliveryNoteListModelAsync(DeliveryNoteSearchModel searchModel)
    {
        var result = await _deliveryNoteAppService.GetListAsync(
            searchModel.Keywords,
            searchModel.PageIndex - 1,
            searchModel.PageSize).ConfigureAwait(false);

            var items = result.Items.Select(x => new DeliveryNoteListItemModel
            {
                Id = x.Id,
                Code = x.Code,
                CustomerName = x.CustomerName,
                CustomerPhone = x.CustomerPhone,
                ShippingAddress = x.ShippingAddress,
                TotalAmount = x.TotalAmount,
                Status = x.Status,
                StatusName = GetStatusName((DeliveryNoteStatus)x.Status),
                CreatedOnUtc = x.CreatedOnUtc,
                DeliveredOnUtc = x.DeliveredOnUtc
            }).ToList();

            var data = PagedDataModel.Create(items, searchModel.PageIndex, searchModel.PageSize, result.Pagination.TotalCount);

            var model = new DeliveryNoteListModel
            {
                Keywords = searchModel.Keywords,
                Data = data
            };

        return model;
    }

    public async Task<CreateDeliveryNoteModel> PrepareCreateDeliveryNoteModelAsync(Guid orderId)
    {
        var order = await _orderAppService.GetOrderByIdAsync(orderId).ConfigureAwait(false);
        if (order == null)
            throw new ArgumentException("Order not found");

        var model = new CreateDeliveryNoteModel
        {
            OrderId = order.Id,
            OrderCode = order.Code,
            CustomerName = order.CustomerName,
            ShippingAddress = order.CustomerAddress ?? string.Empty,
            ShowPrice = false, // Default is hide price
            Items = new List<CreateDeliveryNoteItemModel>()
        };

        // Here we should calculate how many items have already been delivered in previous notes
        var existingNotes = await _deliveryNoteAppService.GetByOrderIdAsync(orderId).ConfigureAwait(false);
        var activeNotes = existingNotes.Where(n => n.Status != (int)DeliveryNoteStatus.Cancelled).ToList();

        foreach (var orderItem in order.Items)
        {
            // Calculate previously delivered quantity
            var prevQuantity = activeNotes
                .SelectMany(n => n.Items)
                .Where(i => i.OrderItemId == orderItem.Id)
                .Sum(i => i.Quantity);

            var remainingQuantity = orderItem.Quantity - prevQuantity;
            
            if (remainingQuantity > 0)
            {
                model.Items.Add(new CreateDeliveryNoteItemModel
                {
                    OrderItemId = orderItem.Id,
                    ProductName = orderItem.ProductName,
                    OrderedQuantity = orderItem.Quantity,
                    PreviouslyDeliveredQuantity = prevQuantity,
                    Quantity = remainingQuantity, // Default to remaining
                    UnitPrice = orderItem.UnitPrice,
                    Selected = true
                });
            }
        }

        return model;
    }

    public async Task<DeliveryNoteDetailsModel> PrepareDeliveryNoteDetailsModelAsync(Guid id)
    {
        var note = await _deliveryNoteAppService.GetByIdAsync(id).ConfigureAwait(false);
        if (note == null)
            throw new ArgumentException("Delivery note not found");

        var order = await _orderAppService.GetOrderByIdAsync(note.OrderId).ConfigureAwait(false);

        var model = new DeliveryNoteDetailsModel
        {
            Id = note.Id,
            Code = note.Code,
            OrderId = note.OrderId,
            OrderCode = order?.Code ?? string.Empty,
            CustomerName = note.CustomerName,
            CustomerPhone = note.CustomerPhone,
            CustomerAddress = note.CustomerAddress,
            ShippingAddress = note.ShippingAddress,
            ShowPrice = note.ShowPrice,
            Note = note.Note,
            Status = note.Status,
            StatusName = GetStatusName((DeliveryNoteStatus)note.Status),
            CreatedOnUtc = note.CreatedOnUtc,
            DeliveredOnUtc = note.DeliveredOnUtc,
            DeliveryProofPictureId = note.DeliveryProofPictureId,
            DeliveryReceiverName = note.DeliveryReceiverName,
            TotalAmount = note.TotalAmount,
            Items = note.Items.Select(i => new DeliveryNoteItemModel
            {
                Id = i.Id,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                SubTotal = i.SubTotal
            }).ToList()
        };

        if (note.DeliveryProofPictureId.HasValue)
        {
            var picture = await _pictureAppService.GetBase64PictureByIdAsync(note.DeliveryProofPictureId.Value).ConfigureAwait(false);
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

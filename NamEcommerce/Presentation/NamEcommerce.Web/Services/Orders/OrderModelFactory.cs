using MediatR;
using NamEcommerce.Application.Contracts.DeliveryNotes;
using NamEcommerce.Web.Contracts.Configurations;
using NamEcommerce.Web.Contracts.Models.Orders;
using NamEcommerce.Web.Contracts.Queries.Models.Catalog;
using NamEcommerce.Web.Contracts.Queries.Models.Customers;
using NamEcommerce.Web.Contracts.Queries.Models.Orders;
using NamEcommerce.Web.Models.Orders;

namespace NamEcommerce.Web.Services.Orders;

public sealed class OrderModelFactory : IOrderModelFactory
{
    private readonly AppConfig _appConfig;
    private readonly IMediator _mediator;
    private readonly IDeliveryNoteAppService _deliveryNoteAppService;

    public OrderModelFactory(AppConfig appConfig, IMediator mediator, IDeliveryNoteAppService deliveryNoteAppService)
    {
        _appConfig = appConfig;
        _mediator = mediator;
        _deliveryNoteAppService = deliveryNoteAppService;
    }

    public async Task<CreateOrderModel> PrepareCreateOrderModel(CreateOrderModel? model = null)
    {
        model = model ?? new CreateOrderModel();

        if (model.CustomerId.HasValue)
        {
            var customer = await _mediator.Send(new GetCustomerByIdQuery { Id = model.CustomerId.Value }).ConfigureAwait(false);
            if (customer is null)
                model.CustomerId = null;
            else
            {
                model.CustomerDisplayName = customer.FullName;
                model.CustomerDisplayPhone = customer.PhoneNumber;
                model.CustomerDisplayAddress = customer.Address;
            }
        }

        if (model.Items.Count > 0)
        {
            var productIds = model.Items.Select(i => i.ProductId).OfType<Guid>().ToList();
            if (productIds.Count > 0)
            {
                var products = await _mediator.Send(new GetProductsByIdsForOrderQuery
                {
                    Ids = productIds
                }).ConfigureAwait(false);

                model.Items = model.Items.Where(i => products.Any(p => p.Id == i.ProductId)).ToList();

                foreach (var item in model.Items)
                {
                    var product = products.First(p => p.Id == item.ProductId);
                    item.ProductDisplayName = product.Name;
                    item.ProductDisplayQty = product.QuantityAvailable;
                    item.ProductDisplayPicture = product.PictureUrl;
                }
            }
        }

        return model;
    }

    public async Task<OrderDetailsModel?> PrepareOrderDetailsModel(Guid orderId)
    {
        var order = await _mediator.Send(new GetOrderByIdQuery { Id = orderId }).ConfigureAwait(false);
        if (order is null)
            return null;

        var model = new OrderDetailsModel
        {
            Id = order.Id,
            Code = order.Code,
            CustomerId = order.CustomerId,
            CustomerName = order.CustomerName,
            OrderSubTotal = order.OrderSubTotal,
            TotalAmount = order.TotalAmount,
            OrderDiscount = order.OrderDiscount,
            Status = order.Status,
            Note = order.Note,
            ExpectedShippingDate = order.ExpectedShippingDate,
            ShippingAddress = order.ShippingAddress,
            LockOrderReason = order.LockOrderReason,
            CustomerAddress = order.CustomerAddress,
            CustomerPhoneNumber = order.CustomerPhoneNumber,
            CanUpdateInfo = order.CanUpdateInfo,
            CanUpdateOrderItems = order.CanUpdateOrderItems,
            CanLockOrder = order.CanLockOrder
        };
        foreach (var it in order.Items)
        {
            model.Items.Add(new OrderDetailsModel.OrderItemModel(it.Id)
            {
                ProductId = it.ProductId,
                ProductName = it.ProductName,
                ProductPicture = it.ProductPicture,
                ProductAvailableQty = it.ProductAvailableQty,
                Quantity = it.Quantity,
                UnitPrice = it.UnitPrice
            });
        }

        // Fetch delivery notes for this order
        var deliveryNotes = await _deliveryNoteAppService.GetByOrderIdAsync(orderId).ConfigureAwait(false);
        foreach (var dn in deliveryNotes)
        {
            var dnModel = new OrderDetailsModel.DeliveryNoteBasicModel
            {
                Id = dn.Id,
                Code = dn.Code
            };

            // Add delivery note items for coverage calculation
            foreach (var item in dn.Items)
            {
                dnModel.Items.Add(new OrderDetailsModel.DeliveryNoteItemModel
                {
                    OrderItemId = item.OrderItemId,
                    Quantity = item.Quantity
                });
            }

            model.DeliveryNotes.Add(dnModel);
        }

        return model;
    }

    public async Task<OrderListModel> PrepareOrderListModel(OrderListSearchModel searchModel)
    {
        var pageNumber = searchModel?.PageNumber ?? 1;
        var pageSize = searchModel?.PageSize ?? 0;
        if (pageNumber <= 0) pageNumber = 1;
        if (pageSize <= 0) pageSize = _appConfig.DefaultPageSize;
        if (_appConfig.PageSizeOptions.Contains(pageSize)) pageSize = _appConfig.DefaultPageSize;

        var model = await _mediator.Send(new GetOrderListQuery
        {
            Keywords = searchModel?.Keywords,
            Status = searchModel?.Status,
            PageIndex = pageNumber - 1,
            PageSize = pageSize
        });

        return model;
    }
}

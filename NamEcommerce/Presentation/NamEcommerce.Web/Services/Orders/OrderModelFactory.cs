using MediatR;
using NamEcommerce.Application.Contracts.DeliveryNotes;
using NamEcommerce.Application.Contracts.Inventory;
using NamEcommerce.Application.Contracts.PurchaseOrders;
using NamEcommerce.Domain.Shared.Enums.DeliveryNotes;
using NamEcommerce.Domain.Shared.Enums.Inventory;
using NamEcommerce.Domain.Shared.Enums.Orders;
using NamEcommerce.Web.Contracts.Configurations;
using NamEcommerce.Web.Contracts.Models.Orders;
using NamEcommerce.Web.Contracts.Queries.Models.Catalog;
using NamEcommerce.Web.Contracts.Queries.Models.Customers;
using NamEcommerce.Web.Contracts.Queries.Models.Inventory;
using NamEcommerce.Web.Contracts.Queries.Models.Orders;
using NamEcommerce.Web.Contracts.Queries.Models.PurchaseOrders;
using NamEcommerce.Web.Models.Orders;

namespace NamEcommerce.Web.Services.Orders;

public sealed class OrderModelFactory : IOrderModelFactory
{
    private readonly AppConfig _appConfig;
    private readonly IMediator _mediator;
    private readonly IDeliveryNoteAppService _deliveryNoteAppService;
    private readonly IDirectShipAppService _directShipAppService;
    private readonly IWarehouseAppService _warehouseAppService;

    public OrderModelFactory(
        AppConfig appConfig,
        IMediator mediator,
        IDeliveryNoteAppService deliveryNoteAppService,
        IDirectShipAppService directShipAppService,
        IWarehouseAppService warehouseAppService)
    {
        _appConfig = appConfig;
        _mediator = mediator;
        _deliveryNoteAppService = deliveryNoteAppService;
        _directShipAppService = directShipAppService;
        _warehouseAppService = warehouseAppService;
    }

    public async Task<CreateOrderModel> PrepareCreateOrderModel(CreateOrderModel? oldModel = null)
    {
        var model = oldModel ?? new CreateOrderModel();

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
            else
            {
                model.Items.Clear();
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
            CanLockOrder = order.CanLockOrder,
            CreatedOn = order.CreatedOn
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
            if (dn.Status == (int)DeliveryNoteStatus.Cancelled)
                continue;
            var dnModel = new OrderDetailsModel.DeliveryNoteBasicModel
            {
                Id = dn.Id,
                Code = dn.Code,
                Status = dn.Status,
                SourceType = dn.SourceType,
                IsDirectShip = dn.IsDirectShip,
                WarehouseName = dn.WarehouseName,
                CreatedOn = dn.CreatedOnUtc.ToLocalTime(),
                DeliveredOn = dn.DeliveredOnUtc?.ToLocalTime()
            };

            // Add delivery note items for coverage calculation
            foreach (var item in dn.Items)
            {
                dnModel.Items.Add(new OrderDetailsModel.DeliveryNoteItemModel
                {
                    OrderItemId = item.OrderItemId,
                    ProductName = item.ProductName,
                    Quantity = item.Quantity
                });
            }

            model.DeliveryNotes.Add(dnModel);
        }

        model.ShortageInfo = await _mediator.Send(new GetOrderShortageInfoQuery
        {
            OrderId = orderId
        }).ConfigureAwait(false);

        model.AllocatedPurchaseOrders = await _mediator.Send(new GetAllocatedPurchaseOrdersForOrderQuery
        {
            OrderId = orderId
        }).ConfigureAwait(false);

        var orderItemIds = order.Items.Select(i => i.Id).ToList();
        if (orderItemIds.Count > 0)
        {
            var dsAllocations = await _directShipAppService
                .GetDirectShipAllocationsForOrderAsync(orderItemIds)
                .ConfigureAwait(false);

            foreach (var alloc in dsAllocations)
            {
                var orderItem = order.Items.FirstOrDefault(i => i.Id == alloc.OrderItemId);

                model.DirectShipAllocations.Add(new OrderDetailsModel.DirectShipAllocationModel
                {
                    AllocationId = alloc.AllocationId,
                    ProductName = orderItem?.ProductName ?? string.Empty,
                    Status = alloc.Status,
                    DeliveryStatus = alloc.DeliveryStatus,
                    AllocatedQuantity = alloc.AllocatedQuantity,
                    ReceivedQuantity = alloc.ReceivedQuantity,
                    DeliveryNoteId = alloc.DeliveryNoteId,
                    DeliveryNoteCode = alloc.DeliveryNoteCode
                });
            }
        }

        model.CanCancelOrder = order.Status != (int)OrderStatus.Locked
            && order.Status != (int)OrderStatus.Cancelled;
        model.CanDeleteOrder = order.Status is (int)OrderStatus.Pending or (int)OrderStatus.Cancelled;
        model.FullyReceivedDirectShipCount = model.DirectShipAllocations.Count(a =>
            a.ReceivedQuantity > 0 &&
            (!a.DeliveryStatus.HasValue || a.DeliveryStatus == (int)DeliveryNoteStatus.Confirmed));
        model.ReturnWarehouseOptions = await GetReturnWarehouseOptionsAsync().ConfigureAwait(false);

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

    private async Task<IList<OrderDetailsModel.ReturnWarehouseOptionModel>> GetReturnWarehouseOptionsAsync()
    {
        var warehouses = await _warehouseAppService.GetWarehousesAsync().ConfigureAwait(false);
        return warehouses
            .Where(w => w.IsActive && w.WarehouseType == (int)WarehouseType.Physical)
            .OrderBy(w => w.Name)
            .Select(w => new OrderDetailsModel.ReturnWarehouseOptionModel
            {
                Id = w.Id,
                Name = w.Name
            })
            .ToList();
    }
}

using MediatR;
using NamEcommerce.Application.Contracts.Customers;
using NamEcommerce.Application.Contracts.DeliveryNotes;
using NamEcommerce.Application.Contracts.Dtos.DeliveryNotes;
using NamEcommerce.Application.Contracts.Orders;
using NamEcommerce.Web.Contracts.Models.Common;
using NamEcommerce.Web.Contracts.Models.Orders;
using NamEcommerce.Web.Contracts.Queries.Models.Orders;
using NamEcommerce.Web.Framework.Services;

namespace NamEcommerce.Web.Framework.Queries.Handlers.Orders;

public sealed class GetOrderListHandler : IRequestHandler<GetOrderListQuery, OrderListModel>
{
    private const int CancelledDeliveryNoteStatus = 50;
    private const int DeliveredDeliveryNoteStatus = 40;
    private readonly IOrderAppService _orderAppService;
    private readonly ICustomerAppService _customerAppService;
    private readonly IDeliveryNoteAppService _deliveryNoteAppService;

    public GetOrderListHandler(
        IOrderAppService orderAppService,
        ICustomerAppService customerAppService,
        IDeliveryNoteAppService deliveryNoteAppService)
    {
        _orderAppService = orderAppService;
        _customerAppService = customerAppService;
        _deliveryNoteAppService = deliveryNoteAppService;
    }

    public async Task<OrderListModel> Handle(GetOrderListQuery request, CancellationToken cancellationToken)
    {
        var ordersData = await _orderAppService.GetOrdersAsync(request.PageIndex, request.PageSize, request.Keywords, request.Status).ConfigureAwait(false);
        var customers = await _customerAppService.GetCustomersByIdsAsync(ordersData.Select(o => o.CustomerId)).ConfigureAwait(false);
        var orderDeliveryNotesMap = new Dictionary<Guid, IList<DeliveryNoteAppDto>>();
        foreach(var order in ordersData)
        {
            var deliveryNotes = await _deliveryNoteAppService.GetByOrderIdAsync(order.Id).ConfigureAwait(false);
            orderDeliveryNotesMap.Add(order.Id, deliveryNotes);
        }

        var orderItemModels = new List<OrderListModel.OrderModel>();
        foreach (var order in ordersData.Items)
        {
            var customer = customers.FirstOrDefault(cust => cust.Id == order.CustomerId);
            var deliveryNotes = orderDeliveryNotesMap[order.Id];
            var activeDeliveryNotes = deliveryNotes
                .Where(deliveryNote => deliveryNote.Status != CancelledDeliveryNoteStatus)
                .ToList();
            var deliveredNotes = deliveryNotes
                .Where(deliveryNote => deliveryNote.Status == DeliveredDeliveryNoteStatus)
                .ToList();

            orderItemModels.Add(new OrderListModel.OrderModel
            {
                Id = order.Id,
                Code = order.Code,
                OrderStatus = order.Status,
                CustomerId = order.CustomerId,
                CustomerName = order.CanUpdateInfo ? customer?.FullName : order.CustomerName,
                CustomerPhone = order.CanUpdateInfo ? customer?.PhoneNumber : order.CustomerPhone,
                CustomerAddress = order.CanUpdateInfo ? customer?.Address : order.CustomerAddress,
                TotalAmount = order.TotalAmount,
                IsFinished = order.IsFinished,
                ExpectedShippingDate = DateTimeHelper.ToLocalTime(order.ExpectedShippingDateUtc),
                CreatedOn = DateTimeHelper.ToLocalTime(order.CreatedOnUtc),
                CanUpdateInfo = order.CanUpdateInfo,
                Items = order.Items.Select(item => new OrderListModel.OrderItemSummaryModel
                {
                    OrderItemId = item.Id,
                    ProductName = item.ProductName ?? string.Empty,
                    QuantityOrdered = item.Quantity,
                    QuantityInDeliveryNotes = activeDeliveryNotes
                        .SelectMany(deliveryNote => deliveryNote.Items)
                        .Where(deliveryNoteItem => deliveryNoteItem.OrderItemId == item.Id)
                        .Sum(deliveryNoteItem => deliveryNoteItem.Quantity),
                    QuantityDelivered = deliveredNotes
                        .SelectMany(deliveryNote => deliveryNote.Items)
                        .Where(deliveryNoteItem => deliveryNoteItem.OrderItemId == item.Id)
                        .Sum(deliveryNoteItem => deliveryNoteItem.Quantity)
                }).ToList()
            });
        }

        var model = new OrderListModel
        {
            Keywords = request.Keywords,
            Status = request.Status,
            Data = PagedDataModel.Create(orderItemModels, ordersData.Pagination.PageIndex, ordersData.Pagination.PageSize, ordersData.Pagination.TotalCount)
        };
        return model;
    }
}

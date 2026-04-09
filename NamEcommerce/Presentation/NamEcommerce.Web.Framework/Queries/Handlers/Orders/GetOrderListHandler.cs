using MediatR;
using NamEcommerce.Application.Contracts.Customers;
using NamEcommerce.Application.Contracts.Orders;
using NamEcommerce.Web.Contracts.Models.Common;
using NamEcommerce.Web.Contracts.Models.Orders;
using NamEcommerce.Web.Contracts.Queries.Models.Orders;

namespace NamEcommerce.Web.Framework.Queries.Handlers.Orders;

public sealed class GetOrderListHandler : IRequestHandler<GetOrderListQuery, OrderListModel>
{
    private readonly IOrderAppService _orderAppService;
    private readonly ICustomerAppService _customerAppService;

    public GetOrderListHandler(IOrderAppService orderAppService, ICustomerAppService customerAppService)
    {
        _orderAppService = orderAppService;
        _customerAppService = customerAppService;
    }

    public async Task<OrderListModel> Handle(GetOrderListQuery request, CancellationToken cancellationToken)
    {
        var pagedData = await _orderAppService.GetOrdersAsync(request.Keywords, request.Status, request.PageIndex, request.PageSize).ConfigureAwait(false);

        var customers = await _customerAppService.GetCustomersByIdsAsync(pagedData.Select(o => o.CustomerId)).ConfigureAwait(false);
        var orderItemModels = new List<OrderListModel.ItemModel>();
        foreach (var order in pagedData.Items)
        {
            var customer = customers.FirstOrDefault(cust => cust.Id == order.CustomerId);
            orderItemModels.Add(new OrderListModel.ItemModel
            {
                Id = order.Id,
                Code = order.Code,
                CustomerId = order.CustomerId,
                CustomerName = customer?.FullName,
                CustomerPhone = customer?.PhoneNumber,
                CustomerAddress = customer?.Address,
                TotalAmount = order.TotalAmount,
                Status = order.Status,
                PaymentStatus = order.PaymentStatus,
                ShippingStatus = order.ShippingStatus
            });
        }

        var model = new OrderListModel
        {
            Keywords = request.Keywords,
            Status = request.Status,
            Data = PagedDataModel.Create(orderItemModels, pagedData.Pagination.PageIndex, pagedData.Pagination.PageSize, pagedData.Pagination.TotalCount)
        };
        return model;
    }
}

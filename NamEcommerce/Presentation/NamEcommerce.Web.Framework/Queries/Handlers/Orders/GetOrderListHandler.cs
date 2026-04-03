using MediatR;
using NamEcommerce.Web.Contracts.Queries.Models.Orders;
using NamEcommerce.Application.Contracts.Orders;
using NamEcommerce.Web.Contracts.Models.Orders;

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
        var paged = await _orderAppService.GetOrdersAsync(request.Keywords, request.PageIndex, request.PageSize).ConfigureAwait(false);
        var model = new OrderListModel();
        foreach (var it in paged.Items)
        {
            var customer = await _customerAppService.GetCustomerByIdAsync(it.CustomerId).ConfigureAwait(false);
            model.Items.Add(new NamEcommerce.Web.Contracts.Models.Orders.OrderListItemModel 
            { 
                Id = it.Id, 
                CustomerId = it.CustomerId, 
                CustomerName = customer?.FullName ?? "Unknown Customer",
                TotalAmount = it.TotalAmount, 
                Status = it.Status 
            });
        }
        return model;
    }
}

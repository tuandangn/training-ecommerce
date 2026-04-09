using MediatR;
using NamEcommerce.Web.Contracts.Queries.Models.Orders;
using NamEcommerce.Application.Contracts.Orders;
using NamEcommerce.Web.Contracts.Models.Orders;
using NamEcommerce.Application.Contracts.Customers;
using NamEcommerce.Application.Contracts.Catalog;

namespace NamEcommerce.Web.Framework.Queries.Handlers.Orders;

public sealed class GetOrderByIdHandler : IRequestHandler<GetOrderByIdQuery, OrderDetailsModel?>
{
    private readonly IOrderAppService _orderAppService;
    private readonly ICustomerAppService _customerAppService;
    private readonly IProductAppService _productAppService;

    public GetOrderByIdHandler(IOrderAppService orderAppService, ICustomerAppService customerAppService, IProductAppService productAppService)
    {
        _orderAppService = orderAppService;
        _customerAppService = customerAppService;
        _productAppService = productAppService;
    }

    public async Task<OrderDetailsModel?> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await _orderAppService.GetOrderByIdAsync(request.Id).ConfigureAwait(false);
        if (order is null) return null;

        var customer = await _customerAppService.GetCustomerByIdAsync(order.CustomerId).ConfigureAwait(false);
        var model = new OrderDetailsModel
        {
            Id = order.Id,
            Code = order.Code,
            CustomerId = order.CustomerId,
            CustomerName = customer?.FullName ?? "N/A",
            CustomerAddress = customer?.Address,
            CustomerPhoneNumber = customer?.PhoneNumber,
            TotalAmount = order.TotalAmount,
            OrderDiscount = order.OrderDiscount ?? 0,
            Status = order.Status,
            Note = order.Note,
            PaymentStatus = order.PaymentStatus,
            PaymentMethod = order.PaymentMethod,
            PaidOnUtc = order.PaidOnUtc,
            PaymentNote = order.PaymentNote,
            ShippingStatus = order.ShippingStatus,
            ShippingAddress = order.ShippingAddress,
            ShippedOnUtc = order.ShippedOnUtc,
            ShippingNote = order.ShippingNote,
            CancellationReason = order.CancellationReason
        };
        var products = await _productAppService.GetProductsByIdsAsync(order.Items.Select(i => i.ProductId)).ConfigureAwait(false);
        foreach (var orderItem in order.Items)
        {
            var product = products.FirstOrDefault(p => p.Id == orderItem.ProductId);
            model.Items.Add(new OrderDetailsItemModel 
            { 
                ItemId = orderItem.Id, 
                ProductId = orderItem.ProductId, 
                ProductName = product?.Name,
                Quantity = orderItem.Quantity, 
                UnitPrice = orderItem.UnitPrice 
            });
        }
        return model;
    }
}

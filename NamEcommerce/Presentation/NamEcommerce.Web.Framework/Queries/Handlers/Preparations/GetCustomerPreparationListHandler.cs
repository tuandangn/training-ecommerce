using MediatR;
using NamEcommerce.Application.Contracts.Catalog;
using NamEcommerce.Application.Contracts.Customers;
using NamEcommerce.Application.Contracts.Preparation;
using NamEcommerce.Web.Contracts.Models.Common;
using NamEcommerce.Web.Contracts.Models.Preparation;
using NamEcommerce.Web.Contracts.Queries.Models.Preparations;

namespace NamEcommerce.Web.Framework.Queries.Handlers.Preparations;

public sealed class GetCustomerPreparationListHandler(IPreparationAppService preparationAppService, ICustomerAppService customerAppService, IProductAppService productAppService)
    : IRequestHandler<GetCustomerPreparationListQuery, PreparationListModel>
{
    public async Task<PreparationListModel> Handle(GetCustomerPreparationListQuery request, CancellationToken cancellationToken)
    {
        var model = new PreparationListModel
        {
            IsGrouped = false
        };

        var preparationItems = await preparationAppService.GetPreparationListAsync(request.PageIndex, request.PageSize, request.Keywords).ConfigureAwait(false);
        var customers = await customerAppService.GetCustomersByIdsAsync(preparationItems.Select(item => item.CustomerId).Distinct()).ConfigureAwait(false);
        var products = await productAppService.GetProductsByIdsAsync(preparationItems.Select(item => item.ProductId).Distinct()).ConfigureAwait(false);

        var itemModels = new List<PreparationItemModel>();
        foreach(var preparationItem in preparationItems)
        {
            var customer = customers.FirstOrDefault(customer => customer.Id == preparationItem.CustomerId);
            var product = products.FirstOrDefault(product => product.Id == preparationItem.ProductId);
            var itemModel = new PreparationItemModel
            {
                OrderItemId = preparationItem.OrderItemId,
                OrderId = preparationItem.OrderId,
                OrderCode = preparationItem.OrderCode,
                ProductId = preparationItem.ProductId,
                ProductName = product?.Name ?? string.Empty,
                Quantity = preparationItem.Quantity,
                UnitPrice = preparationItem.UnitPrice,
                CustomerId = preparationItem.CustomerId,
                CustomerName = customer?.FullName ?? string.Empty,
                CustomerPhone = customer?.PhoneNumber,
                ExpectedShippingDate = preparationItem.ExpectedShippingDateUtc?.ToLocalTime(),
                IsDelivered = preparationItem.IsDelivered
            };

            itemModels.Add(itemModel);
        }
        model.Items = PagedDataModel.Create(itemModels, preparationItems.Pagination.PageIndex, preparationItems.Pagination.PageSize, preparationItems.Pagination.TotalCount);

        return model;
    }
}

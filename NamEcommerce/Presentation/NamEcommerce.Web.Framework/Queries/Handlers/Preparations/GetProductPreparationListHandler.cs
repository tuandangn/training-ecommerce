using MediatR;
using NamEcommerce.Application.Contracts.Catalog;
using NamEcommerce.Application.Contracts.Customers;
using NamEcommerce.Application.Contracts.Preparation;
using NamEcommerce.Web.Contracts.Models.Common;
using NamEcommerce.Web.Contracts.Models.Preparation;
using NamEcommerce.Web.Contracts.Queries.Models.Preparations;

namespace NamEcommerce.Web.Framework.Queries.Handlers.Preparations;

public sealed class GetProductPreparationListHandler(
    IPreparationAppService preparationAppService, IProductAppService productAppService, ICustomerAppService customerAppService)
    : IRequestHandler<GetProductPreparationListQuery, PreparationListModel>
{
    public async Task<PreparationListModel> Handle(GetProductPreparationListQuery request, CancellationToken cancellationToken)
    {
        var model = new PreparationListModel
        {
            IsGrouped = true
        };

        var groups = await preparationAppService.GetPreparationGroupedListAsync(request.PageIndex, request.PageSize, request.Keywords).ConfigureAwait(false);
        var products = await productAppService.GetProductsByIdsAsync(groups.Select(g => g.ProductId).Distinct()).ConfigureAwait(false);
        var customers = await customerAppService.GetCustomersByIdsAsync(groups.SelectMany(g => g.CustomerDetails.Select(detail => detail.CustomerId))).ConfigureAwait(false);

        model.GroupedItems = PagedDataModel.Create(groups.Select(info => new PreparationGroupedItemModel
        {
            ProductId = info.ProductId,
            ProductName = products.FirstOrDefault(p => p.Id == info.ProductId)?.Name ?? string.Empty,
            TotalQuantity = info.TotalQuantity,
            EarliestShippingDate = info.EarliestShippingDate,
            CustomerDetails = info.CustomerDetails.Select(details =>
            {
                var customer = customers.FirstOrDefault(customer => customer.Id == details.CustomerId);
                return new PreparationGroupedCustomerDetailModel
                {
                    OrderItemId = details.OrderItemId,
                    OrderId = details.OrderId,
                    OrderCode = details.OrderCode,
                    CustomerId = details.CustomerId,
                    CustomerName = customer?.FullName ?? string.Empty,
                    CustomerPhone = customer?.PhoneNumber,
                    Quantity = details.Quantity,
                    ExpectedShippingDate = details.ExpectedShippingDateUtc?.ToLocalTime(),
                    IsDelivered = details.IsDelivered
                };
            }).ToList()
        }).ToList(), groups.Pagination.PageIndex, groups.Pagination.PageSize, groups.Pagination.TotalCount);

        return model;
    }
}

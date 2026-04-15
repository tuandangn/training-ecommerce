using MediatR;
using NamEcommerce.Web.Contracts.Configurations;
using NamEcommerce.Web.Contracts.Models.Common;
using NamEcommerce.Web.Contracts.Queries.Models.Preparations;
using NamEcommerce.Web.Models.Preparations;

namespace NamEcommerce.Web.Services.Preparations;

public sealed class PreparationModelFactory(IMediator mediator, AppConfig appConfig) : IPreparationModelFactory
{
    public async Task<CustomerPreparationListModel> PrepareCustomerPreparationListModelAsync(PreparationListSearchModel searchModel)
    {
        var pageNumber = searchModel?.PageNumber ?? 1;
        var pageSize = searchModel?.PageSize ?? 0;
        if (pageNumber <= 0) pageNumber = 1;
        if (pageSize <= 0) pageSize = appConfig.DefaultPageSize;
        if (appConfig.PageSizeOptions.Contains(pageSize)) pageSize = appConfig.DefaultPageSize;

        var preparationList = await mediator.Send(new GetCustomerPreparationListQuery
        {
            Keywords = searchModel?.Keywords,
            PageIndex = pageNumber - 1,
            PageSize = pageSize
        });

        return new CustomerPreparationListModel
        {
            Items = PagedDataModel.Create(preparationList.Items!.Select(item => new CustomerPreparationListModel.PreparationItemModel
            {
                CustomerId = item.CustomerId,
                CustomerName = item.CustomerName,
                OrderCode = item.OrderCode,
                OrderId = item.OrderId,
                OrderItemId = item.OrderId,
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                CustomerPhone = item.CustomerPhone,
                ExpectedShippingDate = item.ExpectedShippingDate,
                IsDelivered = item.IsDelivered
            }).ToList(), preparationList.Items!.Pagination.PageIndex, preparationList.Items!.Pagination.PageSize, preparationList.Items!.Pagination.TotalCount)
        };
    }

    public async Task<ProductPreparationListModel> PrepareProductPreparationListModelAsync(PreparationListSearchModel searchModel)
    {
        var pageNumber = searchModel?.PageNumber ?? 1;
        var pageSize = searchModel?.PageSize ?? 0;
        if (pageNumber <= 0) pageNumber = 1;
        if (pageSize <= 0) pageSize = appConfig.DefaultPageSize;
        if (appConfig.PageSizeOptions.Contains(pageSize)) pageSize = appConfig.DefaultPageSize;

        var preparationList = await mediator.Send(new GetProductPreparationListQuery
        {
            Keywords = searchModel?.Keywords,
            PageIndex = pageNumber - 1,
            PageSize = pageSize
        });

        return new ProductPreparationListModel
        {
            Items = PagedDataModel.Create(preparationList.GroupedItems!.Select(info => new ProductPreparationListModel.PreparationItemModel
            {
                ProductId = info.ProductId,
                ProductName = info.ProductName,
                TotalQuantity = info.TotalQuantity,
                EarliestShippingDate = info.EarliestShippingDate,
                CustomerDetails = info.CustomerDetails.Select(details => new ProductPreparationListModel.PreparationCustomerDetailModel
                {
                    CustomerId = details.CustomerId,
                    CustomerName = details.CustomerName,
                    CustomerPhone = details.CustomerPhone,
                    OrderCode = details.OrderCode,
                    OrderId = details.OrderId,
                    OrderItemId = details.OrderItemId,
                    Quantity = details.Quantity,
                    ExpectedShippingDate = details.ExpectedShippingDate,
                    IsDelivered = details.IsDelivered
                }).ToList()
            }), preparationList.GroupedItems!.Pagination.PageIndex, preparationList.GroupedItems!.Pagination.PageSize, preparationList.GroupedItems!.Pagination.TotalCount)
        };
    }
}

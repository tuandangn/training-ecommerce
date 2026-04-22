using MediatR;
using NamEcommerce.Web.Contracts.Configurations;
using NamEcommerce.Web.Contracts.Models.Common;
using NamEcommerce.Web.Contracts.Queries.Models.Preparations;
using NamEcommerce.Web.Models.Preparations;
using NamEcommerce.Application.Contracts.Inventory;

namespace NamEcommerce.Web.Services.Preparations;

public sealed class PreparationModelFactory(
    IMediator mediator,
    AppConfig appConfig,
    IWarehouseAppService warehouseAppService) : IPreparationModelFactory
{
    private async Task<EntityOptionListModel> GetAvailableWarehousesAsync()
    {
        var warehouses = await warehouseAppService.GetWarehousesAsync().ConfigureAwait(false);
        return new EntityOptionListModel
        {
            Options = warehouses.Items.Select(warehouse => new EntityOptionListModel.EntityOptionModel
            {
                Id = warehouse.Id,
                Name = warehouse.Name
            }).ToList()
        };
    }

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

        var availableWarehouses = await GetAvailableWarehousesAsync().ConfigureAwait(false);
        return new CustomerPreparationListModel
        {
            Items = PagedDataModel.Create(preparationList.Items!.Select(item => new CustomerPreparationListModel.PreparationItemModel
            {
                CustomerId = item.CustomerId,
                CustomerName = item.CustomerName,
                OrderCode = item.OrderCode,
                OrderId = item.OrderId,
                OrderItemId = item.OrderItemId,
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                Quantity = item.Quantity,
                DeliveredQuantity = item.DeliveredQuantity,
                UnitPrice = item.UnitPrice,
                CustomerPhone = item.CustomerPhone,
                ExpectedShippingDate = item.ExpectedShippingDate,
                IsDelivered = item.IsDelivered,
                StockQuantityAvailable = item.StockQuantityAvailable,
                DeliveryNoteLinks = item.DeliveryNoteLinks.Select(link => new NamEcommerce.Web.Contracts.Models.DeliveryNotes.DeliveryNoteLinkModel(link.Id, link.Code, link.Status, link.CreatedOnUtc)).ToList()
            }).ToList(), preparationList.Items!.Pagination.PageIndex, preparationList.Items!.Pagination.PageSize, preparationList.Items!.Pagination.TotalCount),
            AvailableWarehouses = availableWarehouses
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

        var availableWarehouses = await GetAvailableWarehousesAsync().ConfigureAwait(false);
        return new ProductPreparationListModel
        {
            Items = PagedDataModel.Create(preparationList.GroupedItems!.Select(info => new ProductPreparationListModel.PreparationItemModel
            {
                ProductId = info.ProductId,
                ProductName = info.ProductName,
                TotalQuantity = info.TotalQuantity,
                DeliveredQuantity = info.CustomerDetails.Sum(d => d.DeliveredQuantity),
                EarliestShippingDate = info.EarliestShippingDate,
                StockQuantityAvailable = info.StockQuantityAvailable,
                CustomerDetails = info.CustomerDetails.Select(details => new ProductPreparationListModel.PreparationCustomerDetailModel
                {
                    CustomerId = details.CustomerId,
                    CustomerName = details.CustomerName,
                    CustomerPhone = details.CustomerPhone,
                    OrderCode = details.OrderCode,
                    OrderId = details.OrderId,
                    OrderItemId = details.OrderItemId,
                    Quantity = details.Quantity,
                    UnitPrice = details.UnitPrice,
                    DeliveredQuantity = details.DeliveredQuantity,
                    ExpectedShippingDate = details.ExpectedShippingDate,
                    IsDelivered = details.IsDelivered,
                    DeliveryNoteLinks = details.DeliveryNoteLinks.Select(link => new NamEcommerce.Web.Contracts.Models.DeliveryNotes.DeliveryNoteLinkModel(link.Id, link.Code, link.Status, link.CreatedOnUtc)).ToList()
                }).ToList()
            }), preparationList.GroupedItems!.Pagination.PageIndex, preparationList.GroupedItems!.Pagination.PageSize, preparationList.GroupedItems!.Pagination.TotalCount),
            AvailableWarehouses = availableWarehouses
        };
    }
}

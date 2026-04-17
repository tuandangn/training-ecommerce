using NamEcommerce.Application.Contracts.Dtos.Common;
using NamEcommerce.Application.Contracts.Dtos.Preparation;
using NamEcommerce.Application.Contracts.Preparation;
using NamEcommerce.Domain.Shared.Enums.Orders;
using NamEcommerce.Domain.Shared.Services.Orders;

using NamEcommerce.Domain.Shared.Services.Inventory;
using NamEcommerce.Domain.Shared.Services.DeliveryNotes;

namespace NamEcommerce.Application.Services.Preparation;

public sealed class PreparationAppService(
    IOrderManager orderManager,
    IInventoryStockManager inventoryStockManager,
    IDeliveryNoteManager deliveryNoteManager) : IPreparationAppService
{
    public async Task<IPagedDataAppDto<PreparationItemAppDto>> GetPreparationListAsync(int pageIndex, int pageSize, string? keywords = null)
    {
        var status = Enum.GetValues<OrderStatus>().Where(status => status != OrderStatus.Locked).ToArray();
        var orders = await orderManager.GetOrdersAsync(null, status, 0, int.MaxValue).ConfigureAwait(false);

        var orderItems = orders.OrderBy(o => o.ExpectedShippingDateUtc).SelectMany(order => order.Items.Select(item => (order, item)));
        var totalCount = orderItems.Count();

        var selectedItems = orderItems.Skip(pageIndex * pageSize).Take(pageSize).ToList();
        var orderItemIds = selectedItems.Select(x => x.item.Id).ToList();
        var deliveredQuantities = await deliveryNoteManager.GetDeliveredQuantitiesAsync(orderItemIds).ConfigureAwait(false);
        var deliveryNoteLinks = await deliveryNoteManager.GetDeliveryNoteLinksAsync(orderItemIds).ConfigureAwait(false);

        var preparationItems = new List<PreparationItemAppDto>();

        foreach (var (order, item) in selectedItems)
        {
            var links = deliveryNoteLinks.TryGetValue(item.Id, out var dnLinks)
                ? dnLinks.Select(l => new DeliveryNoteLinkAppDto(l.Id, l.Code, (int)l.Status, l.CreatedOnUtc)).ToList()
                : [];

            preparationItems.Add(new PreparationItemAppDto
            {
                OrderItemId = item.Id,
                OrderId = order.Id,
                OrderCode = order.Code,
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                CustomerId = order.CustomerId,
                ExpectedShippingDateUtc = order.ExpectedShippingDateUtc,
                IsDelivered = item.IsDelivered,
                DeliveredQuantity = deliveredQuantities.TryGetValue(item.Id, out var qty) ? qty : 0,
                DeliveryNoteLinks = links
            });
        }

        foreach (var item in preparationItems)
        {
            var stocks = await inventoryStockManager.GetInventoryStocksForProductAsync(item.ProductId, null).ConfigureAwait(false);
            item.StockQuantityAvailable = stocks.Sum(s => s.QuantityAvailable);
        }

        return PagedDataAppDto.Create(preparationItems, pageIndex, pageSize, totalCount);
    }

    public async Task<IPagedDataAppDto<PreparationGroupedItemAppDto>> GetPreparationGroupedListAsync(int pageIndex, int pageSize, string? keywords = null)
    {
        var status = Enum.GetValues<OrderStatus>().Where(status => status != OrderStatus.Locked).ToArray();
        var orders = await orderManager.GetOrdersAsync(null, status, 0, int.MaxValue).ConfigureAwait(false);

        var groupedItems = orders.SelectMany(order => order.Items.Select(item => (order, item, expectedDate: order.ExpectedShippingDateUtc)))
            .GroupBy(info => info.item.ProductId)
            .Select(group =>
            {
                var earliestDate = group
                        .Where(info => info.order.ExpectedShippingDateUtc.HasValue)
                        .Select(info => info.order.ExpectedShippingDateUtc!.Value)
                        .OrderBy(date => date)
                        .FirstOrDefault();
                return (group, earliestDate);
            })
            .OrderBy(info => info.earliestDate)
            .ToList();

        var totalCount = groupedItems.Count;

        var selectedGroups = groupedItems.Skip(pageIndex * pageSize).Take(pageSize).ToList();
        
        // Collect all OrderItemIds for the selected groups to fetch delivered quantities and links in one go
        var allOrderItemIds = selectedGroups.SelectMany(g => g.group.Select(info => info.item.Id)).ToList();
        var deliveredQuantities = await deliveryNoteManager.GetDeliveredQuantitiesAsync(allOrderItemIds).ConfigureAwait(false);
        var deliveryNoteLinks = await deliveryNoteManager.GetDeliveryNoteLinksAsync(allOrderItemIds).ConfigureAwait(false);

        var preparationItems = selectedGroups
            .Select(groupInfo => new PreparationGroupedItemAppDto
            {
                ProductId = groupInfo.group.Key,
                TotalQuantity = groupInfo.group.Sum(info => info.item.Quantity),
                EarliestShippingDate = groupInfo.earliestDate,
                CustomerDetails = groupInfo.group.Select(info => new PreparationGroupedCustomerDetail
                {
                    OrderItemId = info.item.Id,
                    OrderId = info.order.Id,
                    CustomerId = info.order.CustomerId,
                    OrderCode = info.order.Code,
                    Quantity = info.item.Quantity,
                    UnitPrice = info.item.UnitPrice,
                    ExpectedShippingDateUtc = info.order.ExpectedShippingDateUtc,
                    IsDelivered = info.item.IsDelivered,
                    DeliveredQuantity = deliveredQuantities.TryGetValue(info.item.Id, out var qty) ? qty : 0,
                    DeliveryNoteLinks = deliveryNoteLinks.TryGetValue(info.item.Id, out var dnLinks)
                        ? dnLinks.Select(l => new DeliveryNoteLinkAppDto(l.Id, l.Code, (int)l.Status, l.CreatedOnUtc)).ToList()
                        : []
                }).ToList()
            }).ToList();

        foreach (var item in preparationItems)
        {
            var stocks = await inventoryStockManager.GetInventoryStocksForProductAsync(item.ProductId, null).ConfigureAwait(false);
            item.StockQuantityAvailable = stocks.Sum(s => s.QuantityAvailable);
        }

        return PagedDataAppDto.Create(preparationItems, pageIndex, pageSize, totalCount);
    }
}

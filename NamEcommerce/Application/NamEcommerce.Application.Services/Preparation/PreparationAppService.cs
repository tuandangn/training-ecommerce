using NamEcommerce.Application.Contracts.Dtos.Common;
using NamEcommerce.Application.Contracts.Dtos.Preparation;
using NamEcommerce.Application.Contracts.Preparation;
using NamEcommerce.Domain.Shared.Enums.Orders;
using NamEcommerce.Domain.Shared.Services.Orders;

namespace NamEcommerce.Application.Services.Preparation;

public sealed class PreparationAppService(IOrderManager orderManager) : IPreparationAppService
{
    public async Task<IPagedDataAppDto<PreparationItemAppDto>> GetPreparationListAsync(int pageIndex, int pageSize, string? keywords = null)
    {
        var status = Enum.GetValues<OrderStatus>().Where(status => status != OrderStatus.Locked).ToArray();
        var orders = await orderManager.GetOrdersAsync(null, status, 0, int.MaxValue).ConfigureAwait(false);

        var orderItems = orders.OrderBy(o => o.ExpectedShippingDateUtc).SelectMany(order => order.Items.Select(item => (order, item)));
        var totalCount = orderItems.Count();

        var preparationItems = new List<PreparationItemAppDto>();

        foreach (var (order, item) in orderItems.Skip(pageIndex * pageSize).Take(pageSize))
        {
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
                IsDelivered = item.IsDelivered
            });
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

        var preparationItems = groupedItems
            .Skip(pageIndex * pageSize).Take(pageSize)
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
                    ExpectedShippingDateUtc = info.order.ExpectedShippingDateUtc,
                    IsDelivered = info.item.IsDelivered
                }).ToList()
            });

        return PagedDataAppDto.Create(preparationItems, pageIndex, pageSize, totalCount);
    }
}

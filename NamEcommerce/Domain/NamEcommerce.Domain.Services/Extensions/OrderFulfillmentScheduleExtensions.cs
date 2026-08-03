using NamEcommerce.Domain.Entities.Orders;
using NamEcommerce.Domain.Shared.Dtos.Orders;

namespace NamEcommerce.Domain.Services.Extensions;

public static class OrderFulfillmentScheduleExtensions
{
    public static OrderFulfillmentScheduleDto ToDto(this OrderFulfillmentSchedule schedule)
        => new(schedule.Id)
        {
            OrderId = schedule.OrderId,
            OrderCode = schedule.OrderCode,
            ScheduledFromUtc = schedule.ScheduledFromUtc,
            ScheduledToUtc = schedule.ScheduledToUtc,
            Mode = schedule.Mode,
            Note = schedule.Note,
            IsActive = schedule.IsActive,
            CreatedByUserId = schedule.CreatedByUserId,
            CreatedOnUtc = schedule.CreatedOnUtc,
            UpdatedOnUtc = schedule.UpdatedOnUtc,
            InactivatedOnUtc = schedule.InactivatedOnUtc,
            InactivatedByUserId = schedule.InactivatedByUserId,
            Items = schedule.Items
                .Select(item => new OrderFulfillmentScheduleDto.OrderFulfillmentScheduleItemDto(item.Id)
                {
                    OrderFulfillmentScheduleId = item.OrderFulfillmentScheduleId,
                    OrderItemId = item.OrderItemId,
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    Quantity = item.Quantity,
                    CreatedOnUtc = item.CreatedOnUtc
                })
                .ToList()
        };
}

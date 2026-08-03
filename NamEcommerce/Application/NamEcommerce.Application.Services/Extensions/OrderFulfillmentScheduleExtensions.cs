using NamEcommerce.Application.Contracts.Dtos.Orders;
using NamEcommerce.Domain.Shared.Dtos.Orders;
using NamEcommerce.Domain.Shared.Enums.Orders;

namespace NamEcommerce.Application.Services.Extensions;

public static class OrderFulfillmentScheduleExtensions
{
    public static OrderFulfillmentScheduleAppDto ToDto(this OrderFulfillmentScheduleDto schedule)
        => new(schedule.Id)
        {
            OrderId = schedule.OrderId,
            OrderCode = schedule.OrderCode,
            ScheduledFromUtc = schedule.ScheduledFromUtc,
            ScheduledToUtc = schedule.ScheduledToUtc,
            Mode = (int)schedule.Mode,
            Note = schedule.Note,
            IsActive = schedule.IsActive,
            CreatedByUserId = schedule.CreatedByUserId,
            CreatedOnUtc = schedule.CreatedOnUtc,
            UpdatedOnUtc = schedule.UpdatedOnUtc,
            InactivatedOnUtc = schedule.InactivatedOnUtc,
            InactivatedByUserId = schedule.InactivatedByUserId,
            Items = schedule.Items
                .Select(item => new OrderFulfillmentScheduleItemAppDto(item.Id)
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

    public static CreateOrderFulfillmentScheduleDto ToDomainDto(this CreateOrderFulfillmentScheduleAppDto dto)
        => new()
        {
            OrderId = dto.OrderId,
            ScheduledFromUtc = dto.ScheduledFromUtc,
            ScheduledToUtc = dto.ScheduledToUtc,
            Mode = (OrderFulfillmentScheduleMode)dto.Mode,
            Note = dto.Note,
            Items = dto.Items.Select(ToDomainItemDto).ToList()
        };

    public static UpdateOrderFulfillmentScheduleDto ToDomainDto(this UpdateOrderFulfillmentScheduleAppDto dto)
        => new(dto.Id)
        {
            OrderId = dto.OrderId,
            ScheduledFromUtc = dto.ScheduledFromUtc,
            ScheduledToUtc = dto.ScheduledToUtc,
            Mode = (OrderFulfillmentScheduleMode)dto.Mode,
            Note = dto.Note,
            Items = dto.Items.Select(ToDomainItemDto).ToList(),
            IsActive = dto.IsActive
        };

    private static CreateOrderFulfillmentScheduleItemDto ToDomainItemDto(OrderFulfillmentScheduleItemInputAppDto item)
        => new()
        {
            OrderItemId = item.OrderItemId,
            ProductId = item.ProductId,
            ProductName = item.ProductName,
            Quantity = item.Quantity
        };
}

using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.Orders;
using NamEcommerce.Domain.Services.Extensions;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Orders;
using NamEcommerce.Domain.Shared.Enums.Orders;
using NamEcommerce.Domain.Shared.Exceptions.Orders;
using NamEcommerce.Domain.Shared.Services.Inventory;
using NamEcommerce.Domain.Shared.Services.Orders;
using NamEcommerce.Domain.Shared.Services.Users;

namespace NamEcommerce.Domain.Services.Orders;

public sealed class OrderFulfillmentScheduleManager(
    IRepository<OrderFulfillmentSchedule> scheduleRepository,
    IEntityDataReader<OrderFulfillmentSchedule> scheduleDataReader,
    IRepository<Order> orderRepository,
    IShortageQueryService shortageQueryService,
    ICurrentUserAccessor currentUserAccessor) : IOrderFulfillmentScheduleManager
{
    public async Task<OrderFulfillmentScheduleDto?> GetByIdAsync(Guid id)
        => (await scheduleDataReader.GetByIdAsync(id).ConfigureAwait(false))?.ToDto();

    public Task<IList<OrderFulfillmentScheduleDto>> GetByOrderIdAsync(Guid orderId)
    {
        var schedules = scheduleDataReader.DataSource
            .Where(schedule => schedule.OrderId == orderId)
            .OrderByDescending(schedule => schedule.IsActive)
            .ThenBy(schedule => schedule.ScheduledFromUtc ?? DateTime.MaxValue)
            .ThenBy(schedule => schedule.CreatedOnUtc)
            .ToList()
            .Select(schedule => schedule.ToDto())
            .ToList();

        return Task.FromResult<IList<OrderFulfillmentScheduleDto>>(schedules);
    }

    public Task<IList<OrderFulfillmentScheduleDto>> GetActiveByOrderIdsAsync(IReadOnlyCollection<Guid> orderIds)
    {
        if (orderIds.Count == 0)
            return Task.FromResult<IList<OrderFulfillmentScheduleDto>>([]);

        var schedules = scheduleDataReader.DataSource
            .Where(schedule => schedule.IsActive && orderIds.Contains(schedule.OrderId))
            .OrderBy(schedule => schedule.ScheduledFromUtc ?? DateTime.MaxValue)
            .ThenBy(schedule => schedule.CreatedOnUtc)
            .ToList()
            .Select(schedule => schedule.ToDto())
            .ToList();

        return Task.FromResult<IList<OrderFulfillmentScheduleDto>>(schedules);
    }

    public async Task<CreateOrderFulfillmentScheduleResultDto> CreateAsync(CreateOrderFulfillmentScheduleDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        dto.Verify();

        var order = await GetEditableOrderAsync(dto.OrderId).ConfigureAwait(false);
        var normalizedItems = NormalizeItems(order, dto.Items);
        await EnsureQuantitiesDoNotExceedRemainingAsync(order, normalizedItems, null).ConfigureAwait(false);

        var currentUser = await currentUserAccessor.GetCurrentUserAsync().ConfigureAwait(false);
        var schedule = new OrderFulfillmentSchedule(
            order.Id,
            order.Code,
            dto.Mode,
            dto.ScheduledFromUtc,
            dto.ScheduledToUtc,
            dto.Note,
            currentUser?.Id);
        schedule.ReplaceItems(normalizedItems);

        var inserted = await scheduleRepository.InsertAsync(schedule).ConfigureAwait(false);
        return new CreateOrderFulfillmentScheduleResultDto { CreatedId = inserted.Id };
    }

    public async Task<UpdateOrderFulfillmentScheduleResultDto> UpdateAsync(UpdateOrderFulfillmentScheduleDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        dto.Verify();

        var schedule = await scheduleDataReader.GetByIdAsync(dto.Id).ConfigureAwait(false)
            ?? throw new OrderFulfillmentScheduleIsNotFoundException(dto.Id);
        var order = await GetEditableOrderAsync(dto.OrderId).ConfigureAwait(false);
        if (schedule.OrderId != order.Id)
            throw new OrderFulfillmentScheduleDataIsInvalidException("Error.OrderFulfillmentScheduleOrderMismatch");

        var normalizedItems = NormalizeItems(order, dto.Items);
        if (schedule.IsActive)
            await EnsureQuantitiesDoNotExceedRemainingAsync(order, normalizedItems, schedule.Id).ConfigureAwait(false);

        schedule.SetMode(dto.Mode);
        schedule.SetWindow(dto.ScheduledFromUtc, dto.ScheduledToUtc);
        schedule.SetNote(dto.Note);
        schedule.ReplaceItems(normalizedItems);

        var updated = await scheduleRepository.UpdateAsync(schedule).ConfigureAwait(false);
        return new UpdateOrderFulfillmentScheduleResultDto { UpdatedId = updated.Id };
    }

    public async Task SetActiveAsync(SetOrderFulfillmentScheduleActiveDto dto)
    {
        var schedule = await scheduleDataReader.GetByIdAsync(dto.Id).ConfigureAwait(false)
            ?? throw new OrderFulfillmentScheduleIsNotFoundException(dto.Id);

        if (dto.IsActive)
        {
            var order = await GetEditableOrderAsync(schedule.OrderId).ConfigureAwait(false);
            var items = schedule.Items
                .Select(item => new CreateOrderFulfillmentScheduleItemDto
                {
                    OrderItemId = item.OrderItemId,
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    Quantity = item.Quantity
                })
                .ToList();
            await EnsureQuantitiesDoNotExceedRemainingAsync(order, items, schedule.Id).ConfigureAwait(false);
            schedule.Activate();
        }
        else
        {
            schedule.Inactivate();
        }

        await scheduleRepository.UpdateAsync(schedule).ConfigureAwait(false);
    }

    public async Task RefreshWhenStockAvailableAsync(IReadOnlyCollection<Guid> orderItemIds)
    {
        if (orderItemIds.Count == 0)
            return;

        var schedules = scheduleDataReader.DataSource
            .Where(schedule => schedule.IsActive
                && schedule.Mode == OrderFulfillmentScheduleMode.WhenStockAvailable
                && schedule.Items.Any(item => orderItemIds.Contains(item.OrderItemId)))
            .ToList();
        foreach (var schedule in schedules)
        {
            var states = await shortageQueryService.GetOrderItemFulfillmentStatesAsync(schedule.OrderId).ConfigureAwait(false);
            var statesByItem = states.ToDictionary(state => state.OrderItemId);
            var canDeliverAnyItem = schedule.Items.Any(item =>
                statesByItem.TryGetValue(item.OrderItemId, out var state)
                && state.AvailableQuantity > 0);

            if (!canDeliverAnyItem)
                continue;

            schedule.SetMode(OrderFulfillmentScheduleMode.AsSoonAsPossible);
            await scheduleRepository.UpdateAsync(schedule).ConfigureAwait(false);
        }
    }

    private async Task<Order> GetEditableOrderAsync(Guid orderId)
    {
        var order = await orderRepository.GetByIdAsync(orderId).ConfigureAwait(false)
            ?? throw new OrderIsNotFoundException(orderId);
        if (!order.CanUpdateInfo())
            throw new OrderCannotUpdateInfoException();

        return order;
    }

    private static IList<CreateOrderFulfillmentScheduleItemDto> NormalizeItems(
        Order order,
        IList<CreateOrderFulfillmentScheduleItemDto> items)
    {
        var orderItemsById = order.OrderItems.ToDictionary(item => item.Id);
        return items
            .GroupBy(item => item.OrderItemId)
            .Select(group =>
            {
                if (!orderItemsById.TryGetValue(group.Key, out var orderItem))
                    throw new OrderItemIsNotFoundException();

                return new CreateOrderFulfillmentScheduleItemDto
                {
                    OrderItemId = orderItem.Id,
                    ProductId = orderItem.ProductId,
                    ProductName = orderItem.ProductName ?? group.First().ProductName ?? string.Empty,
                    Quantity = group.Sum(item => item.Quantity)
                };
            })
            .ToList();
    }

    private async Task EnsureQuantitiesDoNotExceedRemainingAsync(
        Order order,
        IList<CreateOrderFulfillmentScheduleItemDto> requestedItems,
        Guid? excludedScheduleId)
    {
        var fulfillmentStates = await shortageQueryService.GetOrderItemFulfillmentStatesAsync(order.Id).ConfigureAwait(false);
        var remainingQuantities = fulfillmentStates.ToDictionary(
            state => state.OrderItemId,
            state => Math.Max(0, state.RequiredQuantity - state.ShippedQuantity));

        var activeScheduledQuantities = GetActiveScheduledQuantities(order.Id, excludedScheduleId);
        foreach (var item in requestedItems)
        {
            var remaining = remainingQuantities.GetValueOrDefault(item.OrderItemId);
            var alreadyScheduled = activeScheduledQuantities.GetValueOrDefault(item.OrderItemId);
            if (alreadyScheduled + item.Quantity > remaining)
                throw new OrderFulfillmentScheduleDataIsInvalidException("Error.OrderFulfillmentScheduleQuantityExceedsOrderItemQuantity");
        }
    }

    private Dictionary<Guid, decimal> GetActiveScheduledQuantities(Guid orderId, Guid? excludedScheduleId)
        => scheduleDataReader.DataSource
            .Where(schedule => schedule.OrderId == orderId
                && schedule.IsActive
                && (!excludedScheduleId.HasValue || schedule.Id != excludedScheduleId.Value))
            .ToList()
            .SelectMany(schedule => schedule.Items)
            .GroupBy(item => item.OrderItemId)
            .ToDictionary(group => group.Key, group => group.Sum(item => item.Quantity));
}

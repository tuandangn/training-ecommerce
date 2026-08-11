using Microsoft.EntityFrameworkCore;
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
    {
        var schedule = await scheduleRepository.GetByIdAsync(id).ConfigureAwait(false);
        return schedule?.ToDto();
    }

    public async Task<IList<OrderFulfillmentScheduleDto>> GetByOrderIdAsync(Guid orderId, bool includeInactive = false)
    {
        var schedules = await scheduleDataReader.DataSource
            .Where(schedule => schedule.OrderId == orderId && (includeInactive || schedule.IsActive))
            .OrderByDescending(schedule => schedule.IsActive)
            .ThenBy(schedule => schedule.ScheduledFromUtc ?? DateTime.MaxValue)
            .ThenBy(schedule => schedule.CreatedOnUtc)
            .ToListAsync().ConfigureAwait(false);

        return schedules.Select(schedule => schedule.ToDto()).ToList();
    }

    public async Task<IList<OrderFulfillmentScheduleDto>> GetActiveByOrderIdsAsync(IReadOnlyCollection<Guid> orderIds)
    {
        if (orderIds.Count == 0)
            return [];

        var schedules = await scheduleDataReader.DataSource
            .Where(schedule => schedule.IsActive && orderIds.Contains(schedule.OrderId))
            .OrderBy(schedule => schedule.ScheduledFromUtc ?? DateTime.MaxValue)
            .ThenBy(schedule => schedule.CreatedOnUtc)
            .ToListAsync().ConfigureAwait(false);

        return schedules.Select(schedule => schedule.ToDto()).ToList();
    }

    public async Task<CreateOrderFulfillmentScheduleResultDto> CreateAsync(CreateOrderFulfillmentScheduleDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        dto.Verify();

        var order = await GetEditableOrderAsync(dto.OrderId).ConfigureAwait(false);
        var normalizedItems = NormalizeItems(order, dto.Items);
        foreach (var item in normalizedItems)
        {
            if (item.OrderItemId == Guid.Empty)
                throw new OrderFulfillmentScheduleDataIsInvalidException("Error.OrderItemIsNotFound");
            if (item.ProductId == Guid.Empty)
                throw new OrderFulfillmentScheduleDataIsInvalidException("Error.ProductIsNotFound");
            if (item.Quantity <= 0)
                throw new OrderFulfillmentScheduleDataIsInvalidException("Error.OrderFulfillmentScheduleQuantityMustBePositive");
        }
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

        var schedule = await scheduleRepository.GetByIdAsync(dto.Id).ConfigureAwait(false);
        if (schedule is null)
            throw new OrderFulfillmentScheduleIsNotFoundException(dto.Id);

        var order = await GetEditableOrderAsync(dto.OrderId).ConfigureAwait(false);
        if (schedule.OrderId != order.Id)
            throw new OrderFulfillmentScheduleDataIsInvalidException("Error.OrderFulfillmentScheduleOrderMismatch");

        var normalizedItems = NormalizeItems(order, dto.Items);
        foreach (var item in normalizedItems)
        {
            if (item.OrderItemId == Guid.Empty)
                throw new OrderFulfillmentScheduleDataIsInvalidException("Error.OrderItemIsNotFound");
            if (item.ProductId == Guid.Empty)
                throw new OrderFulfillmentScheduleDataIsInvalidException("Error.ProductIsNotFound");
            if (item.Quantity <= 0)
                throw new OrderFulfillmentScheduleDataIsInvalidException("Error.OrderFulfillmentScheduleQuantityMustBePositive");
        }

        if (dto.IsActive ?? schedule.IsActive)
            await EnsureQuantitiesDoNotExceedRemainingAsync(order, normalizedItems, schedule.Id).ConfigureAwait(false);

        var existingItems = schedule.Items.ToList();
        schedule.SetMode(dto.Mode);
        schedule.SetWindow(dto.ScheduledFromUtc, dto.ScheduledToUtc);
        schedule.SetNote(dto.Note);
        schedule.ReplaceItems(normalizedItems);
        if (dto.IsActive.HasValue)
            await ActivateSchedule(schedule, dto.IsActive.Value).ConfigureAwait(false);

        var updated = await scheduleRepository.UpdateAsync(schedule).ConfigureAwait(false);
        return new UpdateOrderFulfillmentScheduleResultDto { UpdatedId = updated.Id };
    }

    public async Task SetActiveAsync(SetOrderFulfillmentScheduleActiveDto dto)
    {
        var schedule = await scheduleRepository.GetByIdAsync(dto.Id).ConfigureAwait(false)
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
        }
        var updated = await ActivateSchedule(schedule, dto.IsActive).ConfigureAwait(false);
        if (updated)
            await scheduleRepository.UpdateAsync(schedule).ConfigureAwait(false);
    }

    public async Task DeleteScheduleItemsOfOrderItemsAsync(Guid orderId, IList<Guid> orderItemIds)
    {
        var schedules = await scheduleDataReader.TrackingDataSource
            .Where(schedule => schedule.OrderId == orderId && schedule.Items.Any(item => orderItemIds.Contains(item.OrderItemId)))
            .ToListAsync().ConfigureAwait(false);

        foreach (var schedule in schedules)
        {
            var newItems = schedule.Items.Where(item => !orderItemIds.Contains(item.OrderItemId)).Select(item => new CreateOrderFulfillmentScheduleItemDto
            {
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                OrderItemId = item.OrderItemId,
                Quantity = item.Quantity
            }).ToList();
            if (newItems.Count == 0)
            {
                schedule.ReplaceItems([]);
                await DeleteAsync(schedule.Id).ConfigureAwait(false);
                continue;
            }
            await UpdateAsync(new UpdateOrderFulfillmentScheduleDto(schedule.Id)
            {
                IsActive = schedule.IsActive,
                Mode = schedule.Mode,
                Note = schedule.Note,
                OrderId = orderId,
                ScheduledFromUtc = schedule.ScheduledFromUtc,
                ScheduledToUtc = schedule.ScheduledToUtc,
                Items = newItems
            }).ConfigureAwait(false);
        }
    }

    private async Task<bool> ActivateSchedule(OrderFulfillmentSchedule schedule, bool isActive)
    {
        if (schedule.IsActive == isActive)
            return false;

        if (isActive)
            schedule.Activate();
        else
        {
            var currentUser = await currentUserAccessor.GetCurrentUserAsync().ConfigureAwait(false);
            schedule.Inactivate(currentUser?.Id);
        }

        return true;
    }

    public async Task DeleteAsync(Guid id)
    {
        var schedule = await scheduleRepository.GetByIdAsync(id).ConfigureAwait(false)
            ?? throw new OrderFulfillmentScheduleIsNotFoundException(id);

        await GetEditableOrderAsync(schedule.OrderId).ConfigureAwait(false);
        await scheduleRepository.DeleteAsync(schedule).ConfigureAwait(false);
    }

    public async Task RefreshWhenStockAvailableAsync(IReadOnlyCollection<SecondaryItemId> orderItemIds)
    {
        if (orderItemIds.Count == 0)
            return;

        var schedules = await scheduleDataReader.TrackingDataSource
            .Where(schedule => schedule.IsActive
                && schedule.Mode == OrderFulfillmentScheduleMode.WhenStockAvailable
                && schedule.Items.Any(item => orderItemIds.Any(orderItemId => orderItemId.SecondaryId == item.OrderItemId)))
            .ToListAsync().ConfigureAwait(false);
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
        Order order, IList<CreateOrderFulfillmentScheduleItemDto> items)
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

        var activeScheduledQuantities = await GetActiveScheduledQuantitiesAsync(order.Id, excludedScheduleId).ConfigureAwait(false);
        foreach (var item in requestedItems)
        {
            var remaining = remainingQuantities.GetValueOrDefault(item.OrderItemId);
            var alreadyScheduled = activeScheduledQuantities.GetValueOrDefault(item.OrderItemId);
            if (alreadyScheduled + item.Quantity > remaining)
                throw new OrderFulfillmentScheduleDataIsInvalidException("Error.OrderFulfillmentScheduleQuantityExceedsOrderItemQuantity");
        }
    }

    private Task<Dictionary<Guid, decimal>> GetActiveScheduledQuantitiesAsync(Guid orderId, Guid? excludedScheduleId)
        => scheduleDataReader.DataSource
            .Where(schedule => schedule.OrderId == orderId
                && schedule.IsActive
                && (!excludedScheduleId.HasValue || schedule.Id != excludedScheduleId.Value))
            .SelectMany(schedule => schedule.Items)
            .GroupBy(item => item.OrderItemId)
            .ToDictionaryAsync(group => group.Key, group => group.Sum(item => item.Quantity));

}

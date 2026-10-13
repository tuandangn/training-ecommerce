using MediatR;
using NamEcommerce.Application.Contracts.Dtos.Common;
using NamEcommerce.Application.Contracts.Dtos.Orders;
using NamEcommerce.Application.Contracts.Orders;
using NamEcommerce.Application.Services.Extensions;
using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.DeliveryNotes;
using NamEcommerce.Domain.Entities.Orders;
using NamEcommerce.Domain.Entities.PurchaseOrders;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Inventory;
using NamEcommerce.Domain.Shared.Enums.DeliveryNotes;
using NamEcommerce.Domain.Shared.Enums.Orders;
using NamEcommerce.Domain.Shared.Enums.PurchaseOrders;
using NamEcommerce.Domain.Shared.Services.Inventory;
using NamEcommerce.Domain.Shared.Services.Orders;
using NamEcommerce.Domain.Shared.Specifications;
using NamEcommerce.Domain.Specifications.DeliveryNotes;
using NamEcommerce.Domain.Specifications.Orders;
using NamEcommerce.Domain.Specifications.PurchaseOrders;

namespace NamEcommerce.Application.Services.Orders;

public sealed class OrderFulfillmentScheduleAppService(
    IOrderFulfillmentScheduleManager scheduleManager,
    IEntityDataReader<Order> orderDataReader,
    IEntityDataReader<DeliveryNote> deliveryNoteDataReader,
    IEntityDataReader<DeliveryRun> deliveryRunDataReader,
    IEntityDataReader<PurchaseOrderItemAllocation> allocationDataReader,
    IRepository<Order> orderRepository,
    IShortageQueryService shortageQueryService) : IOrderFulfillmentScheduleAppService
{
    public async Task<OrderFulfillmentScheduleAppDto?> GetByIdAsync(Guid id)
        => (await scheduleManager.GetByIdAsync(id).ConfigureAwait(false))?.ToDto();

    public async Task<IList<OrderFulfillmentScheduleAppDto>> GetByOrderIdAsync(Guid orderId, bool includeInactive = false)
        => (await scheduleManager.GetByOrderIdAsync(orderId, includeInactive).ConfigureAwait(false))
            .Select(schedule => schedule.ToDto())
            .ToList();

    public async Task<CreateOrderFulfillmentScheduleResultAppDto> CreateAsync(CreateOrderFulfillmentScheduleAppDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var (valid, errorMessage) = dto.Validate();
        if (!valid)
            return new CreateOrderFulfillmentScheduleResultAppDto { Success = false, ErrorMessage = errorMessage };

        var result = await scheduleManager.CreateAsync(dto.ToDomainDto()).ConfigureAwait(false);
        return new CreateOrderFulfillmentScheduleResultAppDto { Success = true, CreatedId = result.CreatedId };
    }

    public async Task<CommonActionResultDto> CreateDefaultSchedulesForOrderAsync(Guid orderId, IList<Guid>? limitedOrderItemIds)
    {
        var order = await orderRepository.GetByIdAsync(orderId).ConfigureAwait(false);
        if (order is null)
            return CommonActionResultDto.CreateSuccess();

        var states = await shortageQueryService.GetOrderItemFulfillmentStatesAsync(order.Id).ConfigureAwait(false);
        if (limitedOrderItemIds is not null)
            states = states.Where(state => limitedOrderItemIds.Contains(state.OrderItemId)).ToList();

        if (states.Count == 0)
            return CommonActionResultDto.CreateSuccess();

        //not before date
        if (order.ExpectedShippingDateUtc.HasValue && order.ExpectedShippingDateUtc > DateTime.UtcNow)
        {
            var fulfillmentItems = states.Select(state => new OrderFulfillmentScheduleItemInputAppDto
            {
                OrderItemId = state.OrderItemId,
                ProductId = state.ProductId,
                ProductName = state.ProductName,
                Quantity = Math.Max(0, state.RequiredQuantity - state.ShippedQuantity)
            }).Where(item => item.Quantity > 0).ToList();

            if (fulfillmentItems.Count == 0)
                return CommonActionResultDto.CreateSuccess();

            return await CreateAsync(new CreateOrderFulfillmentScheduleAppDto
            {
                OrderId = orderId,
                Mode = (int)OrderFulfillmentScheduleMode.NotBeforeDate,
                ScheduledFromUtc = order.ExpectedShippingDateUtc,
                Items = fulfillmentItems
            }).ConfigureAwait(false);
        }

        //as soon as possible
        var asapItems = states
            .Select(state => new OrderFulfillmentScheduleItemInputAppDto
            {
                OrderItemId = state.OrderItemId,
                ProductId = state.ProductId,
                ProductName = state.ProductName,
                Quantity = Math.Min(Math.Max(0, state.RequiredQuantity - state.ShippedQuantity), state.AvailableQuantity)
            })
            .Where(item => item.Quantity > 0)
            .ToList();

        if (asapItems.Count > 0)
        {
            var asapResult = await CreateAsync(new CreateOrderFulfillmentScheduleAppDto
            {
                OrderId = orderId,
                Mode = (int)OrderFulfillmentScheduleMode.AsSoonAsPossible,
                Items = asapItems
            }).ConfigureAwait(false);
            if (!asapResult.Success)
                return asapResult;
        }

        //waiting
        var waitingItems = states
            .Select(state =>
            {
                var remaining = Math.Max(0, state.RequiredQuantity - state.ShippedQuantity);
                var available = Math.Min(remaining, state.AvailableQuantity);
                return new OrderFulfillmentScheduleItemInputAppDto
                {
                    OrderItemId = state.OrderItemId,
                    ProductId = state.ProductId,
                    ProductName = state.ProductName,
                    Quantity = Math.Max(0, remaining - available)
                };
            })
            .Where(item => item.Quantity > 0)
            .ToList();
        if (waitingItems.Count == 0)
            return CommonActionResultDto.CreateSuccess();

        return await CreateAsync(new CreateOrderFulfillmentScheduleAppDto
        {
            OrderId = orderId,
            Mode = (int)OrderFulfillmentScheduleMode.WhenStockAvailable,
            Items = waitingItems
        }).ConfigureAwait(false);
    }

    public async Task<UpdateOrderFulfillmentScheduleResultAppDto> UpdateAsync(UpdateOrderFulfillmentScheduleAppDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var (valid, errorMessage) = dto.Validate();
        if (!valid)
        {
            return new UpdateOrderFulfillmentScheduleResultAppDto { Success = false, ErrorMessage = errorMessage };
        }

        var result = await scheduleManager.UpdateAsync(dto.ToDomainDto()).ConfigureAwait(false);
        return new UpdateOrderFulfillmentScheduleResultAppDto { Success = true, UpdatedId = result.UpdatedId };
    }

    public async Task<CommonActionResultDto> SetActiveAsync(SetOrderFulfillmentScheduleActiveAppDto dto)
    {
        await scheduleManager.SetActiveAsync(new(dto.Id, dto.IsActive)).ConfigureAwait(false);
        return CommonActionResultDto.CreateSuccess();
    }

    public async Task<CommonActionResultDto> DeleteAsync(Guid id)
    {
        await scheduleManager.DeleteAsync(id).ConfigureAwait(false);
        return CommonActionResultDto.CreateSuccess();
    }

    public async Task<CommonActionResultDto> RefreshWhenStockAvailableForPurchaseOrderItemsAsync(
        IReadOnlyCollection<(Guid purchaseOrderId, Guid purchaseOrderItemId)> purchaseOrderItemIds)
    {
        if (purchaseOrderItemIds.Count == 0)
            return CommonActionResultDto.CreateSuccess();

        var poItemIds = purchaseOrderItemIds.Select(id => id.purchaseOrderItemId).Distinct().ToList();
        var orderItemIds = (await allocationDataReader.GetListAsync(new ActivePurchaseOrderAllocationOfPurchaseOrderItemSpec(purchaseOrderItemIds.First().purchaseOrderId, poItemIds)))
            .Select(allocation => allocation.OrderItemId)
            .Distinct()
            .ToList();

        await scheduleManager.RefreshWhenStockAvailableAsync(orderItemIds).ConfigureAwait(false);
        return CommonActionResultDto.CreateSuccess();
    }

    public async Task<OrderFulfillmentBoardAppDto> GetBoardAsync(OrderFulfillmentBoardFilterAppDto filter)
    {
        ArgumentNullException.ThrowIfNull(filter);

        var baseDateUtc = filter.DateUtc ?? DateTime.UtcNow;
        var todayLocal = baseDateUtc.ToLocalTime().Date;

        var pendingOrderSpec = new CompositeSpecification<Order>(new HaveStatusOrderSpec([OrderStatus.Pending]));
        pendingOrderSpec.ApplyOrderBy(order => order.CreatedOnUtc);
        var orders = await orderDataReader.GetListAsync(pendingOrderSpec).ConfigureAwait(false);

        var pendingOrderIds = orders.Select(order => order.Id).ToList();
        var statesByOrders = await GetStatesByOrderAsync(pendingOrderIds).ConfigureAwait(false);
        var processingDeliveryOrderIds = await GetProcessingDeliveryOrderIdsAsync(pendingOrderIds).ConfigureAwait(false);

        orders = orders.Where(order => processingDeliveryOrderIds.Contains(order.Id)
            || statesByOrders.GetValueOrDefault(order.Id)?.Any(state => state.RequiredQuantity > state.ShippedQuantity) == true).ToList();

        if (!string.IsNullOrWhiteSpace(filter.Keywords))
        {
            var orderSearchSpec = new OrderKeywordSearchSpec(filter.Keywords.Trim());
            orders = orders.Where(orderSearchSpec.Criteria.Compile()).ToList();
        }

        pendingOrderIds = orders.Select(order => order.Id).ToList();

        var schedules = await GetSchedulesAsync(orders, filter.IncludeInactive).ConfigureAwait(false);

        var entries = new List<OrderFulfillmentBoardEntryAppDto>();
        entries.AddRange(BuildScheduleEntries(schedules, orders, statesByOrders));
        entries.AddRange(BuildUnscheduledOrderEntries(orders, schedules, statesByOrders, baseDateUtc));
        entries.AddRange(BuildDeliveryNoteEntries(pendingOrderIds));
        entries.AddRange(BuildDeliveryRunEntries());

        var filteredEntries = ApplyRiskFilter(entries, filter.Risk).ToList();
        return new OrderFulfillmentBoardAppDto
        {
            DateUtc = baseDateUtc,
            Overdue = BuildDays(filteredEntries, DateTime.MinValue.Date, todayLocal.AddDays(-1)),
            Today = BuildDays(filteredEntries, todayLocal, todayLocal),
            Next3Days = BuildDays(filteredEntries, todayLocal.AddDays(1), todayLocal.AddDays(3)),
            Next7Days = BuildDays(filteredEntries, todayLocal.AddDays(1), todayLocal.AddDays(7)),
            Next30Days = BuildDays(filteredEntries, todayLocal.AddDays(1), todayLocal.AddDays(30)),
            UnscheduledGroups = BuildUnscheduledGroups(filteredEntries),
            TotalEntries = filteredEntries.Count,
            OverdueCount = filteredEntries.Count(entry =>
                entry.ScheduledFromUtc.HasValue
                && entry.ScheduledFromUtc.Value.ToLocalTime().Date < todayLocal),
            DangerCount = filteredEntries.Count(entry => entry.Tone == "danger"),
            WarningCount = filteredEntries.Count(entry => entry.Tone == "warning")
        };
    }

    public async Task<decimal> GetActiveScheduledQuantityForOrderItemAsync(Guid orderId, Guid orderItemId)
    {
        var schedules = await GetByOrderIdAsync(orderId, false).ConfigureAwait(false);
        var hadOrderItemSchedules = schedules.Where(schedule => schedule.Items.Any(item => item.OrderItemId == orderItemId))
            .Select(schedule => (scheduleId: schedule.Id, items: schedule.Items.Where(item => item.OrderItemId == orderItemId)))
            .ToList();
        return hadOrderItemSchedules.Sum(info => info.items.Sum(item => item.Quantity));
    }

    public Task DeleteScheduleItemsOfOrderItemsAsync(Guid orderId, IList<Guid> orderItemIds)
        => scheduleManager.DeleteScheduleItemsOfOrderItemsAsync(orderId, orderItemIds);

    private async Task<HashSet<Guid>> GetProcessingDeliveryOrderIdsAsync(IList<Guid> orderIds)
    {
        if (orderIds.Count == 0)
            return [];

        var validStatus = new List<DeliveryNoteStatus>{
            DeliveryNoteStatus.Confirmed,
            DeliveryNoteStatus.Delivering,
            DeliveryNoteStatus.PendingConfirmation
        };
        var haveStatusDeliveryNoteOfOrdersSpec = new CompositeSpecification<DeliveryNote>(new HaveStatusDeliveryNoteSpec(validStatus));
        haveStatusDeliveryNoteOfOrdersSpec.And(new DeliveryNotesOfOrdersSpec(orderIds));
        return (await deliveryNoteDataReader.GetListAsync(haveStatusDeliveryNoteOfOrdersSpec).ConfigureAwait(false))
            .Select(note => note.OrderId)
            .ToHashSet();
    }

    private async Task<Dictionary<Guid, IList<OrderItemFulfillmentStateDto>>> GetStatesByOrderAsync(IEnumerable<Guid> orderIds)
    {
        var result = new Dictionary<Guid, IList<OrderItemFulfillmentStateDto>>();
        foreach (var orderId in orderIds)
            result[orderId] = await shortageQueryService.GetOrderItemFulfillmentStatesAsync(orderId).ConfigureAwait(false);

        return result;
    }

    private async Task<IList<OrderFulfillmentScheduleAppDto>> GetSchedulesAsync(IList<Order> orders, bool includeInactive)
    {
        if (orders.Count == 0)
            return [];

        if (!includeInactive)
        {
            var activeSchedules = await scheduleManager.GetActiveByOrderIdsAsync(orders.Select(order => order.Id).ToList()).ConfigureAwait(false);
            return activeSchedules.Select(schedule => schedule.ToDto()).ToList();
        }

        var schedules = new List<OrderFulfillmentScheduleAppDto>();
        foreach (var order in orders)
        {
            schedules.AddRange((await scheduleManager.GetByOrderIdAsync(order.Id, true).ConfigureAwait(false)).Select(schedule => schedule.ToDto()));
        }

        return schedules;
    }

    private static IEnumerable<OrderFulfillmentBoardEntryAppDto> BuildScheduleEntries(
        IList<OrderFulfillmentScheduleAppDto> schedules,
        IList<Order> orders,
        Dictionary<Guid, IList<OrderItemFulfillmentStateDto>> statesByOrder)
    {
        var ordersById = orders.ToDictionary(order => order.Id);
        foreach (var schedule in schedules)
        {
            if (!ordersById.TryGetValue(schedule.OrderId, out var order))
                continue;

            var statesByItem = statesByOrder.GetValueOrDefault(order.Id)?.ToDictionary(state => state.OrderItemId) ?? [];
            var firstState = statesByItem.Values.FirstOrDefault();
            var items = schedule.Items.Select(item => BuildEntryItem(item.OrderItemId, item.ProductId, item.ProductName, item.Quantity, statesByItem)).ToList();
            var dependencies = BuildDependencies(items, statesByItem);
            var tone = GetScheduleTone(schedule, items, dependencies);

            yield return new OrderFulfillmentBoardEntryAppDto
            {
                Id = schedule.Id,
                SourceType = "Schedule",
                SourceId = schedule.Id,
                SourceCode = schedule.OrderCode,
                OrderId = schedule.OrderId,
                OrderCode = schedule.OrderCode,
                CustomerName = firstState?.CustomerName,
                CustomerPhone = firstState?.CustomerPhone,
                ShippingAddress = order.ShippingAddress,
                ScheduledFromUtc = schedule.ScheduledFromUtc,
                ScheduledToUtc = schedule.ScheduledToUtc,
                Mode = schedule.Mode,
                Tone = tone,
                StatusText = GetScheduleStatusText(schedule, tone),
                IsActive = schedule.IsActive,
                Note = schedule.Note,
                Items = items,
                Dependencies = dependencies
            };
        }
    }

    private static IEnumerable<OrderFulfillmentBoardEntryAppDto> BuildUnscheduledOrderEntries(
        IList<Order> orders,
        IList<OrderFulfillmentScheduleAppDto> schedules,
        Dictionary<Guid, IList<OrderItemFulfillmentStateDto>> statesByOrder,
        DateTime fallbackScheduledFromUtc)
    {
        var scheduledOrderIds = schedules
            .Where(schedule => schedule.IsActive)
            .Select(schedule => schedule.OrderId)
            .ToHashSet();

        foreach (var order in orders.Where(order => !scheduledOrderIds.Contains(order.Id)))
        {
            var states = statesByOrder.GetValueOrDefault(order.Id) ?? [];
            var firstState = states.FirstOrDefault();
            var items = states
                .Where(state => state.RequiredQuantity > state.ShippedQuantity)
                .Select(state => BuildEntryItem(
                    state.OrderItemId,
                    state.ProductId,
                    state.ProductName,
                    Math.Max(0, state.RequiredQuantity - state.ShippedQuantity),
                    states.ToDictionary(s => s.OrderItemId)))
                .ToList();
            if (items.Count == 0)
                continue;

            var statesByItem = states.ToDictionary(state => state.OrderItemId);
            var dependencies = BuildDependencies(items, statesByItem);
            var tone = GetUnscheduledTone(items, dependencies);

            yield return new OrderFulfillmentBoardEntryAppDto
            {
                Id = order.Id,
                SourceType = "Order",
                SourceId = order.Id,
                SourceCode = order.Code,
                OrderId = order.Id,
                OrderCode = order.Code,
                CustomerName = firstState?.CustomerName,
                CustomerPhone = firstState?.CustomerPhone,
                ShippingAddress = order.ShippingAddress,
                ScheduledFromUtc = fallbackScheduledFromUtc,
                ScheduledToUtc = null,
                Mode = (int)OrderFulfillmentScheduleMode.AsSoonAsPossible,
                Tone = tone,
                StatusText = tone == "success" ? "Có thể giao" : tone == "warning" ? "Chờ hàng nhập" : "Chưa có nguồn hàng",
                IsActive = true,
                Items = items,
                Dependencies = dependencies
            };
        }
    }

    private IEnumerable<OrderFulfillmentBoardEntryAppDto> BuildDeliveryNoteEntries(IReadOnlyCollection<Guid> orderIds)
    {
        if (orderIds.Count == 0)
            return [];

        var statuses = new[] { DeliveryNoteStatus.Confirmed, DeliveryNoteStatus.Delivering, DeliveryNoteStatus.PendingConfirmation };
        return deliveryNoteDataReader.DataSource
            .Where(note => orderIds.Contains(note.OrderId) && statuses.Contains(note.Status))
            .OrderBy(note => note.UpdatedOnUtc ?? note.CreatedOnUtc)
            .ToList()
            .Select(note => new OrderFulfillmentBoardEntryAppDto
            {
                Id = note.Id,
                SourceType = "DeliveryNote",
                SourceId = note.Id,
                SourceCode = note.Code,
                OrderId = note.OrderId,
                OrderCode = note.OrderCode ?? string.Empty,
                CustomerName = note.CustomerInfo.FullName,
                CustomerPhone = note.CustomerInfo.PhoneNumber,
                ShippingAddress = note.ShippingAddress,
                ScheduledFromUtc = note.AssignedDeliveryOnUtc ?? note.UpdatedOnUtc ?? note.CreatedOnUtc,
                ScheduledToUtc = null,
                Mode = (int)OrderFulfillmentScheduleMode.AsSoonAsPossible,
                Tone = "info",
                StatusText = note.Status == DeliveryNoteStatus.PendingConfirmation
                    ? "Chờ đối soát"
                    : note.Status == DeliveryNoteStatus.Delivering ? "Đang giao" : "Phiếu đã xác nhận",
                IsActive = true,
                Note = note.Note,
                Items = note.Items.Select(item => new OrderFulfillmentBoardItemAppDto
                {
                    OrderItemId = item.OrderItemId,
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    ScheduledQuantity = item.Quantity,
                    ShippedQuantity = item.Quantity,
                    AvailableQuantity = item.Quantity,
                    MissingSourceQuantity = 0
                }).ToList()
            });
    }

    private IEnumerable<OrderFulfillmentBoardEntryAppDto> BuildDeliveryRunEntries()
    {
        var statuses = new[] { DeliveryRunStatus.ReadyForHandover, DeliveryRunStatus.HandedToDriver };
        return deliveryRunDataReader.DataSource
            .Where(run => statuses.Contains(run.Status))
            .OrderBy(run => run.HandedOverOnUtc ?? run.PreparedOnUtc ?? run.CreatedOnUtc)
            .ToList()
            .Select(run => new OrderFulfillmentBoardEntryAppDto
            {
                Id = run.Id,
                SourceType = "DeliveryRun",
                SourceId = run.Id,
                SourceCode = run.Code,
                OrderId = Guid.Empty,
                OrderCode = string.Join(", ", run.Items.Select(item => item.OrderCode).Where(code => !string.IsNullOrWhiteSpace(code)).Distinct()),
                CustomerName = run.AssignedDeliveryFullName,
                CustomerPhone = null,
                ShippingAddress = $"{run.Items.Count} phiếu giao",
                ScheduledFromUtc = run.HandedOverOnUtc ?? run.PreparedOnUtc ?? run.CreatedOnUtc,
                ScheduledToUtc = null,
                Mode = (int)OrderFulfillmentScheduleMode.AsSoonAsPossible,
                Tone = "info",
                StatusText = run.Status == DeliveryRunStatus.HandedToDriver ? "Đã bàn giao shipper" : "Chờ bàn giao",
                IsActive = true,
                Note = run.Note,
                Items = []
            });
    }

    private static OrderFulfillmentBoardItemAppDto BuildEntryItem(
        Guid orderItemId,
        Guid productId,
        string productName,
        decimal scheduledQuantity,
        Dictionary<Guid, OrderItemFulfillmentStateDto> statesByItem)
    {
        statesByItem.TryGetValue(orderItemId, out var state);
        return new OrderFulfillmentBoardItemAppDto
        {
            OrderItemId = orderItemId,
            ProductId = productId,
            ProductName = productName,
            ScheduledQuantity = scheduledQuantity,
            ShippedQuantity = state?.ShippedQuantity ?? 0,
            AvailableQuantity = state?.AvailableQuantity ?? 0,
            MissingSourceQuantity = state?.MissingSourceQuantity ?? scheduledQuantity
        };
    }

    private static IList<OrderFulfillmentBoardDependencyAppDto> BuildDependencies(
        IList<OrderFulfillmentBoardItemAppDto> items,
        Dictionary<Guid, OrderItemFulfillmentStateDto> statesByItem)
        => items
            .SelectMany(item => statesByItem.TryGetValue(item.OrderItemId, out var state)
                ? state.AllocatedFromPurchaseOrders
                : [])
            .GroupBy(allocation => allocation.POId)
            .Select(group => new OrderFulfillmentBoardDependencyAppDto
            {
                PurchaseOrderId = group.Key,
                PurchaseOrderCode = group.First().POCode,
                AllocatedQuantity = group.Sum(allocation => allocation.AllocatedQty),
                ReceivedQuantity = group.Sum(allocation => allocation.ReceivedQty),
                ExpectedReceiveDateUtc = group.Min(allocation => allocation.ExpectedReceiveDateUtc),
                IsDirectShip = group.Any(allocation => allocation.IsDirectShip)
            })
            .ToList();

    private static string GetScheduleTone(
        OrderFulfillmentScheduleAppDto schedule,
        IList<OrderFulfillmentBoardItemAppDto> items,
        IList<OrderFulfillmentBoardDependencyAppDto> dependencies)
    {
        if (!schedule.IsActive)
            return "muted";

        var now = DateTime.UtcNow;
        var hasInsufficientAvailable = items.Any(item => item.AvailableQuantity < item.ScheduledQuantity);
        var hasUndeliveredQuantity = items.Any(item => item.ScheduledQuantity > item.ShippedQuantity);
        if (schedule.ScheduledFromUtc.HasValue && schedule.ScheduledFromUtc.Value.Date < now.Date && hasUndeliveredQuantity)
            return "danger";
        if (schedule.ScheduledFromUtc.HasValue && schedule.ScheduledFromUtc.Value.Date == now.Date && hasInsufficientAvailable)
            return "danger";
        if (schedule.ScheduledFromUtc.HasValue
            && dependencies.Any(dependency => dependency.ExpectedReceiveDateUtc.HasValue
                && dependency.ExpectedReceiveDateUtc.Value > schedule.ScheduledFromUtc.Value))
            return "warning";
        if (schedule.ScheduledFromUtc.HasValue && schedule.ScheduledFromUtc.Value <= now.AddHours(24))
            return "warning";
        if (items.All(item => item.AvailableQuantity >= item.ScheduledQuantity))
            return "success";
        if (dependencies.Count > 0)
            return "warning";

        return "danger";
    }

    private static string GetUnscheduledTone(
        IList<OrderFulfillmentBoardItemAppDto> items,
        IList<OrderFulfillmentBoardDependencyAppDto> dependencies)
    {
        if (items.Any(item => item.AvailableQuantity > 0))
            return "success";
        if (dependencies.Count > 0)
            return "warning";

        return "danger";
    }

    private static string GetScheduleStatusText(OrderFulfillmentScheduleAppDto schedule, string tone)
    {
        if (!schedule.IsActive)
            return "Tạm tắt";
        if (tone == "danger")
            return "Cần xử lý";
        if (tone == "warning")
            return "Cần theo dõi";
        if ((OrderFulfillmentScheduleMode)schedule.Mode == OrderFulfillmentScheduleMode.WhenStockAvailable)
            return "Ngay khi có hàng";

        return "Có thể giao";
    }

    private static IEnumerable<OrderFulfillmentBoardEntryAppDto> ApplyRiskFilter(
        IEnumerable<OrderFulfillmentBoardEntryAppDto> entries,
        string? risk)
    {
        if (string.IsNullOrWhiteSpace(risk))
            return entries;

        var trimmed = risk.Trim();
        if (string.Equals(trimmed, "unscheduled", StringComparison.OrdinalIgnoreCase))
            return entries.Where(entry => entry.SourceType == "Order");

        return entries.Where(entry => string.Equals(entry.Tone, trimmed, StringComparison.OrdinalIgnoreCase));
    }

    private static IList<OrderFulfillmentBoardDayAppDto> BuildDays(
        IList<OrderFulfillmentBoardEntryAppDto> entries,
        DateTime fromLocalDate,
        DateTime toLocalDate)
        => entries
            .Where(entry => entry.ScheduledFromUtc.HasValue)
            .Where(entry =>
            {
                var localDate = entry.ScheduledFromUtc!.Value.ToLocalTime().Date;
                return localDate >= fromLocalDate && localDate <= toLocalDate;
            })
            .GroupBy(entry => entry.ScheduledFromUtc!.Value.ToLocalTime().Date)
            .OrderBy(group => group.Key)
            .Select(group => new OrderFulfillmentBoardDayAppDto
            {
                DateUtc = group.Key.ToUniversalTime(),
                Label = group.Key.ToString("dd/MM/yyyy"),
                Entries = group.OrderBy(entry => entry.ScheduledFromUtc).ToList()
            })
            .ToList();

    private static IList<OrderFulfillmentUnscheduledGroupAppDto> BuildUnscheduledGroups(IList<OrderFulfillmentBoardEntryAppDto> entries)
    {
        var unscheduled = entries.Where(entry => !entry.ScheduledFromUtc.HasValue).ToList();
        return
        [
            new()
            {
                Key = "ready",
                Title = "Có thể giao ngay",
                Tone = "success",
                Entries = unscheduled.Where(entry => entry.Tone == "success").ToList()
            },
            new()
            {
                Key = "waiting-po",
                Title = "Đang chờ hàng nhập",
                Tone = "warning",
                Entries = unscheduled.Where(entry => entry.Tone == "warning").ToList()
            },
            new()
            {
                Key = "no-source",
                Title = "Chưa có nguồn hàng",
                Tone = "danger",
                Entries = unscheduled.Where(entry => entry.Tone == "danger").ToList()
            }
        ];
    }

}

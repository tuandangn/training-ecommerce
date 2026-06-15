using NamEcommerce.Application.Contracts.Dtos.Common;
using NamEcommerce.Application.Contracts.Dtos.Orders;
using NamEcommerce.Application.Contracts.Orders;
using NamEcommerce.Application.Services.Extensions;
using NamEcommerce.Domain.Entities.DeliveryNotes;
using NamEcommerce.Domain.Entities.Orders;
using NamEcommerce.Domain.Entities.PurchaseOrders;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Inventory;
using NamEcommerce.Domain.Shared.Enums.DeliveryNotes;
using NamEcommerce.Domain.Shared.Enums.Orders;
using NamEcommerce.Domain.Shared.Exceptions;
using NamEcommerce.Domain.Shared.Services.Inventory;
using NamEcommerce.Domain.Shared.Services.Orders;

namespace NamEcommerce.Application.Services.Orders;

public sealed class OrderFulfillmentScheduleAppService(
    IOrderFulfillmentScheduleManager scheduleManager,
    IEntityDataReader<Order> orderDataReader,
    IEntityDataReader<DeliveryNote> deliveryNoteDataReader,
    IEntityDataReader<DeliveryRun> deliveryRunDataReader,
    IEntityDataReader<PurchaseOrderItemAllocation> allocationDataReader,
    IShortageQueryService shortageQueryService) : IOrderFulfillmentScheduleAppService
{
    public async Task<OrderFulfillmentScheduleAppDto?> GetByIdAsync(Guid id)
        => (await scheduleManager.GetByIdAsync(id).ConfigureAwait(false))?.ToDto();

    public async Task<IList<OrderFulfillmentScheduleAppDto>> GetByOrderIdAsync(Guid orderId)
        => (await scheduleManager.GetByOrderIdAsync(orderId).ConfigureAwait(false))
            .Select(schedule => schedule.ToDto())
            .ToList();

    public async Task<CreateOrderFulfillmentScheduleResultAppDto> CreateAsync(CreateOrderFulfillmentScheduleAppDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var (valid, errorMessage) = dto.Validate();
        if (!valid)
            return new CreateOrderFulfillmentScheduleResultAppDto { Success = false, ErrorMessage = errorMessage };

        try
        {
            var result = await scheduleManager.CreateAsync(dto.ToDomainDto()).ConfigureAwait(false);
            return new CreateOrderFulfillmentScheduleResultAppDto { Success = true, CreatedId = result.CreatedId };
        }
        catch (NamEcommerceDomainException ex)
        {
            return new CreateOrderFulfillmentScheduleResultAppDto { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<UpdateOrderFulfillmentScheduleResultAppDto> UpdateAsync(UpdateOrderFulfillmentScheduleAppDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var (valid, errorMessage) = dto.Validate();
        if (!valid)
            return new UpdateOrderFulfillmentScheduleResultAppDto { Success = false, ErrorMessage = errorMessage };

        try
        {
            var result = await scheduleManager.UpdateAsync(dto.ToDomainDto()).ConfigureAwait(false);
            return new UpdateOrderFulfillmentScheduleResultAppDto { Success = true, UpdatedId = result.UpdatedId };
        }
        catch (NamEcommerceDomainException ex)
        {
            return new UpdateOrderFulfillmentScheduleResultAppDto { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<CommonActionResultDto> SetActiveAsync(SetOrderFulfillmentScheduleActiveAppDto dto)
    {
        try
        {
            await scheduleManager.SetActiveAsync(new(dto.Id, dto.IsActive)).ConfigureAwait(false);
            return CommonActionResultDto.CreateSuccess();
        }
        catch (NamEcommerceDomainException ex)
        {
            return CommonActionResultDto.CreateError(ex.Message);
        }
    }

    public async Task<CommonActionResultDto> RefreshWhenStockAvailableForPurchaseOrderItemsAsync(IReadOnlyCollection<Guid> purchaseOrderItemIds)
    {
        if (purchaseOrderItemIds.Count == 0)
            return CommonActionResultDto.CreateSuccess();

        var orderItemIds = allocationDataReader.DataSource
            .Where(allocation => purchaseOrderItemIds.Contains(allocation.PurchaseOrderItemId))
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
        var orders = GetOpenOrders(filter.Keywords);
        var orderIds = orders.Select(order => order.Id).ToList();
        var statesByOrder = await GetStatesByOrderAsync(orderIds).ConfigureAwait(false);
        var schedules = await GetSchedulesAsync(orders, filter.IncludeInactive).ConfigureAwait(false);

        var entries = new List<OrderFulfillmentBoardEntryAppDto>();
        entries.AddRange(BuildScheduleEntries(schedules, orders, statesByOrder));
        entries.AddRange(BuildUnscheduledOrderEntries(orders, schedules, statesByOrder, baseDateUtc));
        entries.AddRange(BuildDeliveryNoteEntries(orderIds));
        entries.AddRange(BuildDeliveryRunEntries());

        var filteredEntries = ApplyRiskFilter(entries, filter.Risk).ToList();
        return new OrderFulfillmentBoardAppDto
        {
            DateUtc = baseDateUtc,
            Today = BuildDays(filteredEntries, todayLocal, todayLocal),
            Next3Days = BuildDays(filteredEntries, todayLocal.AddDays(1), todayLocal.AddDays(3)),
            Next7Days = BuildDays(filteredEntries, todayLocal.AddDays(1), todayLocal.AddDays(7)),
            Next30Days = BuildDays(filteredEntries, todayLocal.AddDays(1), todayLocal.AddDays(30)),
            UnscheduledGroups = BuildUnscheduledGroups(filteredEntries),
            TotalEntries = filteredEntries.Count,
            DangerCount = filteredEntries.Count(entry => entry.Tone == "danger"),
            WarningCount = filteredEntries.Count(entry => entry.Tone == "warning")
        };
    }

    private IList<Order> GetOpenOrders(string? keywords)
    {
        var query = orderDataReader.DataSource
            .Where(order => order.OrderStatus == OrderStatus.Pending && order.OrderItems.Any(item => !item.IsDelivered));

        var orders = query
            .OrderBy(order => order.CreatedOnUtc)
            .ToList();

        if (string.IsNullOrWhiteSpace(keywords))
            return orders;

        var normalizedKeywords = keywords.Trim();
        return orders
            .Where(order => order.Code.Contains(normalizedKeywords, StringComparison.OrdinalIgnoreCase)
                || order.ShippingAddress.Value.Contains(normalizedKeywords, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private async Task<Dictionary<Guid, IList<OrderItemFulfillmentStateDto>>> GetStatesByOrderAsync(IEnumerable<Guid> orderIds)
    {
        var result = new Dictionary<Guid, IList<OrderItemFulfillmentStateDto>>();
        foreach (var orderId in orderIds)
        {
            result[orderId] = await shortageQueryService.GetOrderItemFulfillmentStatesAsync(orderId).ConfigureAwait(false);
        }

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
            schedules.AddRange((await scheduleManager.GetByOrderIdAsync(order.Id).ConfigureAwait(false)).Select(schedule => schedule.ToDto()));
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

        var statuses = new[] { DeliveryNoteStatus.Confirmed, DeliveryNoteStatus.Delivering };
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
                StatusText = note.Status == DeliveryNoteStatus.Delivering ? "Đang giao" : "Phiếu đã xác nhận",
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

        return entries.Where(entry => string.Equals(entry.Tone, risk.Trim(), StringComparison.OrdinalIgnoreCase));
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

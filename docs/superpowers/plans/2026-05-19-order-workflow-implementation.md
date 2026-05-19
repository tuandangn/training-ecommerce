# Order Workflow Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Upgrade the existing order detail page into a clear 4-step workflow with computed active stage, settlement data, order expenses, timeline, and a real `Completed` order status.

**Architecture:** Keep the existing ASP.NET Core MVC/Razor architecture. Extend domain/application DTOs for order completion and order-linked expenses, prepare display-ready workflow data in `OrderModelFactory`, and render the order detail page through focused Razor partials plus small JavaScript for panel switching.

**Tech Stack:** ASP.NET Core MVC, Razor views, MediatR, EF Core mappings, Bootstrap, Bootstrap Icons, vanilla JavaScript modules.

---

## Scope Rules

- Do not add or edit any `*.Test` project.
- Do not run EF migration commands.
- Use existing MVC, app service, domain manager, and model factory patterns.
- Keep changes focused on order workflow/detail, order completion naming, and order-linked expenses.

## File Map

### Domain

- Modify `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Enums/Orders/OrderStatus.cs`: rename `Locked` to `Completed` with the same numeric value.
- Modify `NamEcommerce/Domain/NamEcommerce.Domain/Entities/Orders/Order.cs`: replace lock semantics with completion semantics, add `CompletedOnUtc`, remove new reads/writes of `LockOrderReason`.
- Modify `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Dtos/Orders/OrderDtos.cs`: expose `CompletedOnUtc` and `CanCompleteOrder`.
- Modify `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Events/Orders/OrderEvents.cs`: rename `OrderLocked` to `OrderCompleted`.
- Modify `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Services/Orders/IOrderManager.cs`: rename `LockOrderAsync` to `CompleteOrderAsync`.
- Modify `NamEcommerce/Domain/NamEcommerce.Domain.Services/Orders/OrderManager.cs`: implement `CompleteOrderAsync`, update delete/cancel/completion checks.
- Modify `NamEcommerce/Domain/NamEcommerce.Domain.Services/Extensions/OrderExtensions.cs`: map completion fields.

### Expense

- Modify `NamEcommerce/Domain/NamEcommerce.Domain/Entities/Finance/Expense.cs`: add nullable `SourceOrderId`.
- Modify `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Dtos/Finance/ExpenseDtos.cs`: add `SourceOrderId` to create/update/read DTOs if present.
- Modify `NamEcommerce/Domain/NamEcommerce.Domain.Services/Finance/ExpenseManager.cs`: persist `SourceOrderId`.
- Modify `NamEcommerce/Infrastructure/NamEcommerce.Data.SqlServer/Mappings/ExpenseMapping.cs`: map/index `SourceOrderId`.
- Modify `NamEcommerce/Application/NamEcommerce.Application.Contracts/Dtos/Finance/CreateExpenseAppDto.cs`: add `SourceOrderId`.
- Modify `NamEcommerce/Application/NamEcommerce.Application.Contracts/Dtos/Finance/ExpenseAppDto.cs`: add `SourceOrderId`.
- Modify `NamEcommerce/Application/NamEcommerce.Application.Contracts/Finance/IExpenseAppService.cs`: add `GetExpensesByOrderIdAsync(Guid orderId)`.
- Modify `NamEcommerce/Application/NamEcommerce.Application.Services/Finance/ExpenseAppService.cs`: filter expenses by `SourceOrderId`.
- Modify `NamEcommerce/Presentation/NamEcommerce.Web.Contracts/Commands/Models/Finance/ExpenseCommands.cs`: add `SourceOrderId` to create command.
- Modify `NamEcommerce/Presentation/NamEcommerce.Web.Framework/Commands/Handlers/Finance/ExpenseCommandHandlers.cs`: pass `SourceOrderId`.

### Application and Presentation Order Completion

- Modify `NamEcommerce/Application/NamEcommerce.Application.Contracts/Dtos/Orders/OrderAppDtos.cs`: replace lock DTO/result names with complete DTO/result names, add `CompletedOnUtc`.
- Modify `NamEcommerce/Application/NamEcommerce.Application.Contracts/Orders/IOrderAppService.cs`: replace `LockOrderAsync` with `CompleteOrderAsync`.
- Modify `NamEcommerce/Application/NamEcommerce.Application.Services/Orders/OrderAppService.cs`: complete order and return completion result.
- Modify `NamEcommerce/Presentation/NamEcommerce.Web.Contracts/Commands/Models/Orders/LockOrderCommand.cs`: replace with `CompleteOrderCommand`.
- Rename or replace `NamEcommerce/Presentation/NamEcommerce.Web.Framework/Commands/Handlers/Orders/LockOrderHandler.cs`: handle `CompleteOrderCommand`.
- Modify `NamEcommerce/Presentation/NamEcommerce.Web.Contracts/Models/Orders/OrderModel.cs`: replace lock properties.
- Modify `NamEcommerce/Presentation/NamEcommerce.Web.Framework/Queries/Handlers/Orders/GetOrderByIdHandler.cs`: map completion properties.
- Rename or replace `NamEcommerce/Presentation/NamEcommerce.Web/Models/Orders/LockOrderModel.cs`: use `CompleteOrderModel`.
- Rename or replace `NamEcommerce/Presentation/NamEcommerce.Web/Validators/Orders/LockOrderValidator.cs`: use `CompleteOrderValidator` or delete if no completion note is collected.
- Modify `NamEcommerce/Presentation/NamEcommerce.Web/Controllers/OrderController.cs`: replace `LockOrder` action with `CompleteOrder`, add `AddExpense` action for order-linked expenses.
- Modify `NamEcommerce/Presentation/NamEcommerce.Web/Extensions/OrderStatusExtensions.cs`: display `Pending`, `Completed`, `Cancelled`.

### Workflow Detail View

- Modify `NamEcommerce/Presentation/NamEcommerce.Web/Models/Orders/OrderDetailsModel.cs`: add workflow, preparation, delivery summary, settlement, and timeline nested records.
- Modify `NamEcommerce/Presentation/NamEcommerce.Web/Services/Orders/OrderModelFactory.cs`: inject expense/debt services, compute workflow data, compute settlement, compute timeline.
- Modify `NamEcommerce/Application/NamEcommerce.Application.Contracts/Dtos/DeliveryNotes/DeliveryNoteAppDtos.cs`: add `CostAtDispatch` to `DeliveryNoteItemAppDto`.
- Modify `NamEcommerce/Application/NamEcommerce.Application.Services/Extensions/DeliveryNoteExtensions.cs`: map `CostAtDispatch`.
- Modify `NamEcommerce/Presentation/NamEcommerce.Web/Views/Order/Details.cshtml`: reduce top-level markup and render workflow partials.
- Create `NamEcommerce/Presentation/NamEcommerce.Web/Views/Order/_OrderWorkflowBar.cshtml`.
- Create `NamEcommerce/Presentation/NamEcommerce.Web/Views/Order/_OrderWorkflowOrderPanel.cshtml`.
- Create `NamEcommerce/Presentation/NamEcommerce.Web/Views/Order/_OrderWorkflowPreparationPanel.cshtml`.
- Create `NamEcommerce/Presentation/NamEcommerce.Web/Views/Order/_OrderWorkflowDeliveryPanel.cshtml`.
- Create `NamEcommerce/Presentation/NamEcommerce.Web/Views/Order/_OrderWorkflowSettlementPanel.cshtml`.
- Create `NamEcommerce/Presentation/NamEcommerce.Web/Views/Order/_OrderWorkflowTimeline.cshtml`.
- Modify `NamEcommerce/Presentation/NamEcommerce.Web/wwwroot/modules/OrderDetails.js`: add workflow panel switching and order expense submission.
- Modify `NamEcommerce/Presentation/NamEcommerce.Web/wwwroot/css/site.css`: add workflow/timeline styles using the existing design system colors.

---

### Task 1: Replace Lock Semantics With Completed Semantics

**Files:**
- Modify: `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Enums/Orders/OrderStatus.cs`
- Modify: `NamEcommerce/Domain/NamEcommerce.Domain/Entities/Orders/Order.cs`
- Modify: `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Dtos/Orders/OrderDtos.cs`
- Modify: `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Events/Orders/OrderEvents.cs`
- Modify: `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Services/Orders/IOrderManager.cs`
- Modify: `NamEcommerce/Domain/NamEcommerce.Domain.Services/Orders/OrderManager.cs`
- Modify: `NamEcommerce/Domain/NamEcommerce.Domain.Services/Extensions/OrderExtensions.cs`

- [ ] **Step 1: Rename enum member while preserving stored value**

In `OrderStatus.cs`, keep numeric values stable:

```csharp
namespace NamEcommerce.Domain.Shared.Enums.Orders;

public enum OrderStatus
{
    Pending = 0,
    Completed = 1,
    Cancelled = 2
}
```

- [ ] **Step 2: Update `Order` completion fields and guards**

In `Order.cs`, replace lock-specific fields/methods with completion naming:

```csharp
public OrderStatus OrderStatus { get; private set; }
public DateTime? CompletedOnUtc { get; private set; }

internal bool CanUpdateInfo() => OrderStatus != OrderStatus.Completed && OrderStatus != OrderStatus.Cancelled;

internal bool CanCompleteOrder()
{
    if (OrderStatus != OrderStatus.Pending)
        return false;

    return true;
}

internal void Complete()
{
    if (!CanCompleteOrder())
        throw new OrderCannotChangeStatusException();

    ChangeStatus(OrderStatus.Completed);
    CompletedOnUtc = DateTime.UtcNow;
    RaiseDomainEvent(new OrderCompleted(Id));
}
```

Remove the `LockOrderReason` property from new code paths. If the old database column exists until Tuấn runs migration, do not read/write it.

- [ ] **Step 3: Replace `OrderLocked` event**

In `OrderEvents.cs`, replace:

```csharp
public sealed record OrderLocked(Guid OrderId, string? Reason) : DomainEvent;
```

with:

```csharp
public sealed record OrderCompleted(Guid OrderId) : DomainEvent;
```

- [ ] **Step 4: Update domain DTO**

In `OrderDtos.cs`, make `OrderDto` expose:

```csharp
public required OrderStatus Status { get; init; }
public DateTime? CompletedOnUtc { get; set; }
public bool CanCompleteOrder { get; init; }
```

Replace `LockOrderDto` with:

```csharp
[Serializable]
public sealed record CompleteOrderDto
{
    public required Guid OrderId { get; init; }
}
```

- [ ] **Step 5: Update manager interface**

In `IOrderManager.cs`, replace:

```csharp
Task LockOrderAsync(LockOrderDto dto);
```

with:

```csharp
Task CompleteOrderAsync(CompleteOrderDto dto);
```

- [ ] **Step 6: Implement `CompleteOrderAsync`**

In `OrderManager.cs`, replace `LockOrderAsync` with:

```csharp
public async Task CompleteOrderAsync(CompleteOrderDto dto)
{
    ArgumentNullException.ThrowIfNull(dto);

    var order = await orderDataReader.GetByIdAsync(dto.OrderId).ConfigureAwait(false);
    if (order is null)
        throw new OrderIsNotFoundException(dto.OrderId);

    var activeDeliveryNotes = deliveryNoteDataReader.DataSource
        .Where(d => d.OrderId == order.Id && d.Status != DeliveryNoteStatus.Cancelled)
        .ToList();

    var allDelivered = order.OrderItems.Count() > 0
        && order.OrderItems.All(orderItem =>
            activeDeliveryNotes
                .Where(d => d.Status == DeliveryNoteStatus.Delivered)
                .SelectMany(d => d.Items)
                .Where(i => i.OrderItemId == orderItem.Id)
                .Sum(i => i.Quantity) >= orderItem.Quantity);

    if (!allDelivered)
        throw new OrderCannotChangeStatusException();

    order.Complete();
    order.UpdatedOnUtc = DateTime.UtcNow;

    await orderRepository.UpdateAsync(order).ConfigureAwait(false);
}
```

- [ ] **Step 7: Remove auto-lock behavior**

In `MarkOrderItemDeliveredAsync`, remove the call to `TryAutoLock()`. Completion must be user-triggered.

- [ ] **Step 8: Update mapping**

In `OrderExtensions.cs`, map:

```csharp
CompletedOnUtc = order.CompletedOnUtc,
CanCompleteOrder = order.CanCompleteOrder()
```

- [ ] **Step 9: Search for stale domain references**

Run:

```powershell
rtk powershell -NoProfile -Command "Get-ChildItem -Path NamEcommerce\Domain -Recurse -File -Include *.cs | Select-String -Pattern 'Locked|LockOrder|LockOrderReason|OrderLocked|CanLockOrder'"
```

Expected: no remaining domain references except historical migration files if the search includes migrations.

### Task 2: Update Application and MVC Completion Flow

**Files:**
- Modify: `NamEcommerce/Application/NamEcommerce.Application.Contracts/Dtos/Orders/OrderAppDtos.cs`
- Modify: `NamEcommerce/Application/NamEcommerce.Application.Contracts/Orders/IOrderAppService.cs`
- Modify: `NamEcommerce/Application/NamEcommerce.Application.Services/Orders/OrderAppService.cs`
- Modify: `NamEcommerce/Presentation/NamEcommerce.Web.Contracts/Commands/Models/Orders/LockOrderCommand.cs`
- Modify or rename: `NamEcommerce/Presentation/NamEcommerce.Web.Framework/Commands/Handlers/Orders/LockOrderHandler.cs`
- Modify: `NamEcommerce/Presentation/NamEcommerce.Web.Contracts/Models/Orders/OrderModel.cs`
- Modify: `NamEcommerce/Presentation/NamEcommerce.Web.Framework/Queries/Handlers/Orders/GetOrderByIdHandler.cs`
- Modify or rename: `NamEcommerce/Presentation/NamEcommerce.Web/Models/Orders/LockOrderModel.cs`
- Modify or delete: `NamEcommerce/Presentation/NamEcommerce.Web/Validators/Orders/LockOrderValidator.cs`
- Modify: `NamEcommerce/Presentation/NamEcommerce.Web/Controllers/OrderController.cs`
- Modify: `NamEcommerce/Presentation/NamEcommerce.Web/Extensions/OrderStatusExtensions.cs`

- [ ] **Step 1: Update application DTO names**

In `OrderAppDtos.cs`, add completion fields:

```csharp
public DateTime? CompletedOnUtc { get; set; }
public bool CanCompleteOrder { get; init; }
```

Replace lock DTO/result with:

```csharp
[Serializable]
public sealed record CompleteOrderAppDto
{
    public required Guid OrderId { get; init; }
}

[Serializable]
public sealed record CompleteOrderResultAppDto
{
    public required bool Success { get; init; }
    public string? ErrorMessage { get; set; }
}
```

- [ ] **Step 2: Update app service interface**

In `IOrderAppService.cs`, replace:

```csharp
Task<LockOrderResultAppDto> LockOrderAsync(LockOrderAppDto dto);
```

with:

```csharp
Task<CompleteOrderResultAppDto> CompleteOrderAsync(CompleteOrderAppDto dto);
```

- [ ] **Step 3: Implement app service completion**

In `OrderAppService.cs`, replace `LockOrderAsync` with:

```csharp
public async Task<CompleteOrderResultAppDto> CompleteOrderAsync(CompleteOrderAppDto dto)
{
    ArgumentNullException.ThrowIfNull(dto);

    var order = await orderManager.GetOrderByIdAsync(dto.OrderId).ConfigureAwait(false);
    if (order is null)
        return new CompleteOrderResultAppDto { Success = false, ErrorMessage = "Error.OrderIsNotFound" };

    if (!order.CanCompleteOrder)
        return new CompleteOrderResultAppDto { Success = false, ErrorMessage = "Error.OrderCannotComplete" };

    try
    {
        await orderManager.CompleteOrderAsync(new CompleteOrderDto { OrderId = dto.OrderId }).ConfigureAwait(false);
        return new CompleteOrderResultAppDto { Success = true };
    }
    catch (Exception ex)
    {
        return new CompleteOrderResultAppDto { Success = false, ErrorMessage = ex.Message };
    }
}
```

- [ ] **Step 4: Replace MVC command**

Replace the lock command with:

```csharp
using MediatR;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Commands.Models.Orders;

public sealed record CompleteOrderCommand(Guid OrderId) : IRequest<CommonActionResultModel>;
```

- [ ] **Step 5: Replace command handler**

The handler should call `IOrderAppService.CompleteOrderAsync`:

```csharp
public sealed class CompleteOrderHandler : IRequestHandler<CompleteOrderCommand, CommonActionResultModel>
{
    private readonly IOrderAppService _orderAppService;

    public CompleteOrderHandler(IOrderAppService orderAppService)
    {
        _orderAppService = orderAppService;
    }

    public async Task<CommonActionResultModel> Handle(CompleteOrderCommand request, CancellationToken cancellationToken)
    {
        var result = await _orderAppService.CompleteOrderAsync(new CompleteOrderAppDto
        {
            OrderId = request.OrderId
        }).ConfigureAwait(false);

        return new CommonActionResultModel { Success = result.Success, ErrorMessage = result.ErrorMessage };
    }
}
```

- [ ] **Step 6: Update web models**

In `OrderModel.cs` and `OrderDetailsModel.cs`, use:

```csharp
public DateTime? CompletedOn { get; set; }
public bool CanCompleteOrder { get; init; }
```

Remove `LockOrderReason` and `CanLockOrder` from new view code.

- [ ] **Step 7: Update `OrderController` action**

Replace `LockOrder` with:

```csharp
[HttpPost]
public async Task<IActionResult> CompleteOrder(Guid orderId)
{
    var order = await _mediator.Send(new GetOrderByIdQuery { Id = orderId });
    if (order is null)
        return Json(new { success = false, message = LocalizeError("Error.OrderIsNotFound") });

    if (!order.CanCompleteOrder)
        return Json(new { success = false, message = LocalizeError("Error.OrderCannotComplete") });

    var result = await _mediator.Send(new CompleteOrderCommand(orderId));
    if (!result.Success)
        return Json(new { success = false, message = LocalizeError(result.ErrorMessage!) });

    return Json(new { success = true, message = string.Empty });
}
```

- [ ] **Step 8: Update status display**

In `OrderStatusExtensions.cs`:

```csharp
OrderStatus.Pending => "Đang xử lý",
OrderStatus.Completed => "Hoàn thành",
OrderStatus.Cancelled => "Đã hủy",
```

Use success color for `Completed`, neutral for `Pending`, danger for `Cancelled`.

- [ ] **Step 9: Search presentation/application stale references**

Run:

```powershell
rtk powershell -NoProfile -Command "Get-ChildItem -Path NamEcommerce\Application,NamEcommerce\Presentation -Recurse -File -Include *.cs,*.cshtml,*.js | Select-String -Pattern 'Locked|LockOrder|LockOrderReason|OrderLocked|CanLockOrder|Khóa đơn|khóa đơn'"
```

Expected: no active code references remain. Historical migration files are not part of this search.

### Task 3: Add Order-Linked Expenses

**Files:**
- Modify: `NamEcommerce/Domain/NamEcommerce.Domain/Entities/Finance/Expense.cs`
- Modify: `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Dtos/Finance/ExpenseDtos.cs`
- Modify: `NamEcommerce/Domain/NamEcommerce.Domain.Services/Finance/ExpenseManager.cs`
- Modify: `NamEcommerce/Infrastructure/NamEcommerce.Data.SqlServer/Mappings/ExpenseMapping.cs`
- Modify: `NamEcommerce/Application/NamEcommerce.Application.Contracts/Dtos/Finance/CreateExpenseAppDto.cs`
- Modify: `NamEcommerce/Application/NamEcommerce.Application.Contracts/Dtos/Finance/ExpenseAppDto.cs`
- Modify: `NamEcommerce/Application/NamEcommerce.Application.Contracts/Finance/IExpenseAppService.cs`
- Modify: `NamEcommerce/Application/NamEcommerce.Application.Services/Finance/ExpenseAppService.cs`
- Modify: `NamEcommerce/Presentation/NamEcommerce.Web.Contracts/Commands/Models/Finance/ExpenseCommands.cs`
- Modify: `NamEcommerce/Presentation/NamEcommerce.Web.Framework/Commands/Handlers/Finance/ExpenseCommandHandlers.cs`
- Modify: `NamEcommerce/Presentation/NamEcommerce.Web/Controllers/OrderController.cs`

- [ ] **Step 1: Add source order field to entity**

In `Expense.cs`:

```csharp
public Guid? SourceOrderId { get; internal set; }
```

- [ ] **Step 2: Add mapping**

In `ExpenseMapping.cs`:

```csharp
builder.Property(x => x.SourceOrderId).IsRequired(false);
builder.HasIndex(x => x.SourceOrderId);
```

- [ ] **Step 3: Add DTO fields**

Add nullable `SourceOrderId` to domain create/read DTOs and app create/read DTOs:

```csharp
public Guid? SourceOrderId { get; init; }
```

- [ ] **Step 4: Persist source order**

In `ExpenseManager.CreateExpenseAsync`, assign:

```csharp
SourceOrderId = dto.SourceOrderId,
```

Keep existing vendor/customer return idempotency unchanged.

- [ ] **Step 5: Add app service query**

In `IExpenseAppService.cs`:

```csharp
Task<IList<ExpenseAppDto>> GetExpensesByOrderIdAsync(Guid orderId);
```

In `ExpenseAppService.cs`:

```csharp
public Task<IList<ExpenseAppDto>> GetExpensesByOrderIdAsync(Guid orderId)
{
    var items = _expenseDataReader.DataSource
        .Where(x => x.SourceOrderId == orderId)
        .OrderByDescending(x => x.IncurredDate)
        .Select(x => new ExpenseAppDto
        {
            Id = x.Id,
            Title = x.Title,
            Description = x.Description,
            Amount = x.Amount,
            ExpenseType = (int)x.ExpenseType,
            IncurredDate = x.IncurredDate,
            SourceOrderId = x.SourceOrderId
        })
        .ToList();

    return Task.FromResult<IList<ExpenseAppDto>>(items);
}
```

- [ ] **Step 6: Add order expense command payload**

In `CreateExpenseCommand`, add:

```csharp
public Guid? SourceOrderId { get; init; }
```

Pass it through the handler to `CreateExpenseAppDto`.

- [ ] **Step 7: Add order controller action**

In `OrderController.cs`, add a JSON endpoint:

```csharp
[HttpPost]
public async Task<IActionResult> AddExpense(CreateExpenseCommand command)
{
    if (command.SourceOrderId is null || command.SourceOrderId == Guid.Empty)
        return Json(new { success = false, message = LocalizeError("Error.OrderIsNotFound") });

    var order = await _mediator.Send(new GetOrderByIdQuery { Id = command.SourceOrderId.Value });
    if (order is null)
        return Json(new { success = false, message = LocalizeError("Error.OrderIsNotFound") });

    var result = await _mediator.Send(command);
    if (!result.Success)
        return Json(new { success = false, message = LocalizeError(result.ErrorMessage!) });

    return Json(new { success = true, message = string.Empty });
}
```

### Task 4: Build Workflow Data in `OrderDetailsModel`

**Files:**
- Modify: `NamEcommerce/Presentation/NamEcommerce.Web/Models/Orders/OrderDetailsModel.cs`
- Modify: `NamEcommerce/Presentation/NamEcommerce.Web/Services/Orders/OrderModelFactory.cs`
- Modify: `NamEcommerce/Application/NamEcommerce.Application.Contracts/Dtos/DeliveryNotes/DeliveryNoteAppDtos.cs`
- Modify: `NamEcommerce/Application/NamEcommerce.Application.Services/Extensions/DeliveryNoteExtensions.cs`

- [ ] **Step 1: Add workflow enums**

In `OrderDetailsModel.cs`, add:

```csharp
public enum WorkflowStage
{
    Order = 1,
    Preparation = 2,
    Delivery = 3,
    Settlement = 4
}

public enum OrderDeliverySummaryStatus
{
    Pending = 0,
    Shipping = 1,
    PartialDelivered = 2,
    Delivered = 3
}
```

- [ ] **Step 2: Add workflow summary records**

Add records:

```csharp
public WorkflowModel Workflow { get; set; } = new();
public PreparationModel Preparation { get; set; } = new();
public DeliveryWorkflowModel DeliveryWorkflow { get; set; } = new();
public SettlementModel Settlement { get; set; } = new();
public IList<TimelineEventModel> Timeline { get; set; } = [];
```

Include nested records for:

- stage summaries
- preparation item rows
- related purchase order rows
- delivery progress rows
- debt rows
- expense rows
- cost rows
- timeline events

- [ ] **Step 3: Add cost snapshot to app DTO**

In `DeliveryNoteItemAppDto`, add:

```csharp
public decimal? CostAtDispatch { get; init; }
```

Map it in `DeliveryNoteExtensions.cs`:

```csharp
CostAtDispatch = i.CostAtDispatch
```

- [ ] **Step 4: Inject finance/debt services**

In `OrderModelFactory`, add constructor dependencies:

```csharp
ICustomerDebtAppService customerDebtAppService,
IExpenseAppService expenseAppService
```

Store them in private fields.

- [ ] **Step 5: Compute delivery status**

Add a private method:

```csharp
private static OrderDetailsModel.OrderDeliverySummaryStatus CalculateDeliveryStatus(OrderDetailsModel model)
{
    if (model.Items.Count == 0)
        return OrderDetailsModel.OrderDeliverySummaryStatus.Pending;

    var validNotes = model.DeliveryNotes
        .Where(dn => dn.Status != (int)DeliveryNoteStatus.Cancelled)
        .ToList();

    var hasNotes = validNotes.Count > 0 || model.DirectShipAllocations.Count > 0;
    var deliveredAll = model.Items.All(item => item.GetDeliveredToCustomerQuantity(validNotes) >= item.Quantity);
    if (deliveredAll)
        return OrderDetailsModel.OrderDeliverySummaryStatus.Delivered;

    var deliveredAny = model.Items.Any(item => item.GetDeliveredToCustomerQuantity(validNotes) > 0);
    if (deliveredAny)
        return OrderDetailsModel.OrderDeliverySummaryStatus.PartialDelivered;

    return hasNotes
        ? OrderDetailsModel.OrderDeliverySummaryStatus.Shipping
        : OrderDetailsModel.OrderDeliverySummaryStatus.Pending;
}
```

- [ ] **Step 6: Compute active workflow stage**

Add a private method:

```csharp
private static OrderDetailsModel.WorkflowStage CalculateActiveStage(
    OrderDetailsModel model,
    OrderDetailsModel.OrderDeliverySummaryStatus deliveryStatus)
{
    if (model.Status == (int)OrderStatus.Completed || deliveryStatus == OrderDetailsModel.OrderDeliverySummaryStatus.Delivered)
        return OrderDetailsModel.WorkflowStage.Settlement;

    if (deliveryStatus is OrderDetailsModel.OrderDeliverySummaryStatus.Shipping or OrderDetailsModel.OrderDeliverySummaryStatus.PartialDelivered)
        return OrderDetailsModel.WorkflowStage.Delivery;

    var hasPreparationWork = model.ShortageInfo.HasShortage
        || (model.AllocatedPurchaseOrders?.Items.Any(po => !po.IsFullyReceived) ?? false)
        || model.DirectShipAllocations.Any(a => a.ReceivedQuantity < a.AllocatedQuantity);

    return hasPreparationWork
        ? OrderDetailsModel.WorkflowStage.Preparation
        : OrderDetailsModel.WorkflowStage.Order;
}
```

- [ ] **Step 7: Fill preparation rows**

For each order item, compute:

- ordered quantity
- available quantity from `ProductAvailableQty`
- shortage quantity from existing shortage info
- issued quantity from all valid delivery notes
- delivered quantity from delivered notes
- direct ship quantity/status from `DirectShipAllocations`
- related purchase order rows from `AllocatedPurchaseOrders`

- [ ] **Step 8: Fill settlement**

Use:

- `CustomerDebtAppService.GetDebtsByCustomerIdAsync(model.CustomerId)` and filter `Debt.OrderId == model.Id`.
- `ExpenseAppService.GetExpensesByOrderIdAsync(model.Id)`.
- delivery note items with `CostAtDispatch`.

Profit calculation:

```csharp
Revenue = model.TotalAmount;
TotalCost = costRows.Sum(row => row.TotalCost ?? 0);
TotalExpenses = expenseRows.Sum(row => row.Amount);
Profit = Revenue - TotalCost - TotalExpenses;
IsProfitFinal = costRows.All(row => row.UnitCost.HasValue);
```

- [ ] **Step 9: Fill timeline**

Create timeline events for:

- order created
- related purchase order placed
- received purchase order progress when available
- delivery note created
- delivery note delivered
- customer debt created
- expense incurred
- order completed
- order cancelled

Sort ascending by event time.

### Task 5: Render Workflow UI

**Files:**
- Modify: `NamEcommerce/Presentation/NamEcommerce.Web/Views/Order/Details.cshtml`
- Create: `NamEcommerce/Presentation/NamEcommerce.Web/Views/Order/_OrderWorkflowBar.cshtml`
- Create: `NamEcommerce/Presentation/NamEcommerce.Web/Views/Order/_OrderWorkflowOrderPanel.cshtml`
- Create: `NamEcommerce/Presentation/NamEcommerce.Web/Views/Order/_OrderWorkflowPreparationPanel.cshtml`
- Create: `NamEcommerce/Presentation/NamEcommerce.Web/Views/Order/_OrderWorkflowDeliveryPanel.cshtml`
- Create: `NamEcommerce/Presentation/NamEcommerce.Web/Views/Order/_OrderWorkflowSettlementPanel.cshtml`
- Create: `NamEcommerce/Presentation/NamEcommerce.Web/Views/Order/_OrderWorkflowTimeline.cshtml`
- Modify: `NamEcommerce/Presentation/NamEcommerce.Web/wwwroot/modules/OrderDetails.js`
- Modify: `NamEcommerce/Presentation/NamEcommerce.Web/wwwroot/css/site.css`

- [ ] **Step 1: Add workflow shell to `Details.cshtml`**

Keep existing page title/breadcrumb and modals. Replace the main detail content with:

```cshtml
@await Html.PartialAsync("_OrderWorkflowBar", Model)

<div class="order-workflow-layout">
    <div class="order-workflow-main">
        @await Html.PartialAsync("_OrderWorkflowOrderPanel", Model)
        @await Html.PartialAsync("_OrderWorkflowPreparationPanel", Model)
        @await Html.PartialAsync("_OrderWorkflowDeliveryPanel", Model)
        @await Html.PartialAsync("_OrderWorkflowSettlementPanel", Model)
    </div>
    <aside class="order-workflow-timeline">
        @await Html.PartialAsync("_OrderWorkflowTimeline", Model)
    </aside>
</div>
```

Preserve existing modals for add/edit item, shipping, note, cancel, delete, and delivery note creation.

- [ ] **Step 2: Workflow bar markup**

`_OrderWorkflowBar.cshtml` renders four buttons:

```cshtml
<button type="button"
        class="workflow-step @(Model.Workflow.ActiveStage == OrderDetailsModel.WorkflowStage.Order ? "active" : "")"
        data-workflow-target="order">
    <i class="bi bi-receipt"></i>
    <span>Đặt hàng</span>
</button>
```

Repeat for `preparation`, `delivery`, and `settlement`.

- [ ] **Step 3: Panels**

Each panel uses:

```cshtml
<section class="workflow-panel @(Model.Workflow.ActiveStage == OrderDetailsModel.WorkflowStage.Order ? "active" : "")"
         data-workflow-panel="order">
    ...
</section>
```

Only one panel should be visible at a time.

- [ ] **Step 4: Settlement add expense modal**

In settlement panel, include a compact modal/form posting to `/Order/AddExpense` with:

```html
<input type="hidden" name="SourceOrderId" value="@Model.Id" />
<input name="Title" class="form-control" />
<textarea name="Description" class="form-control"></textarea>
<input name="Amount" class="form-control" data-decimal="currency" />
<input name="IncurredDate" type="date" class="form-control" />
```

- [ ] **Step 5: Complete order button**

Show button only when `Model.CanCompleteOrder`:

```cshtml
<button type="button" class="btn btn-success" id="btnCompleteOrder" data-order-id="@Model.Id">
    <i class="bi bi-check-circle me-1"></i>
    Hoàn thành đơn
</button>
```

- [ ] **Step 6: JavaScript panel switching**

In `OrderDetails.js`, add:

```javascript
document.querySelectorAll('[data-workflow-target]').forEach((button) => {
    button.addEventListener('click', () => {
        const target = button.dataset.workflowTarget;
        document.querySelectorAll('[data-workflow-target]').forEach(step => step.classList.toggle('active', step === button));
        document.querySelectorAll('[data-workflow-panel]').forEach(panel => {
            panel.classList.toggle('active', panel.dataset.workflowPanel === target);
        });
    });
});
```

- [ ] **Step 7: JavaScript complete order**

In `OrderDetails.js`, add:

```javascript
document.getElementById('btnCompleteOrder')?.addEventListener('click', async (event) => {
    const orderId = event.currentTarget.dataset.orderId;
    const confirmed = await confirm('Hoàn thành đơn', 'Xác nhận đã kiểm tra giao hàng, công nợ và chi phí?');
    if (!confirmed) return;

    showPageLoading();
    try {
        const formData = new FormData();
        formData.append('orderId', orderId);
        const result = await apiPost('/Order/CompleteOrder', formData);
        if (result.success) {
            location.reload();
        } else {
            hidePageLoading();
            toast('Lỗi', result.message || 'Không thể hoàn thành đơn.', 'error');
        }
    } catch {
        hidePageLoading();
        toast('Lỗi', 'Có lỗi xảy ra khi gửi yêu cầu.', 'error');
    }
});
```

- [ ] **Step 8: JavaScript add expense**

Submit the order expense form with `apiPost('/Order/AddExpense', formData)` and reload on success.

- [ ] **Step 9: CSS**

Add responsive styles:

```css
.order-workflow-layout {
    display: grid;
    grid-template-columns: minmax(0, 1fr) 320px;
    gap: 16px;
}

.workflow-panel {
    display: none;
}

.workflow-panel.active {
    display: block;
}

.order-workflow-timeline {
    position: sticky;
    top: 88px;
    align-self: start;
}

@media (max-width: 991.98px) {
    .order-workflow-layout {
        grid-template-columns: 1fr;
    }

    .order-workflow-timeline {
        position: static;
    }
}
```

Use existing `content-card`, `data-table`, `status-badge`, and Bootstrap spacing conventions.

### Task 6: Verify and Document Migration Note

**Files:**
- Modify: `docs/superpowers/plans/2026-05-19-order-workflow-implementation.md` only if a discovered implementation detail changes the plan.
- No migration files.
- No test files.

- [ ] **Step 1: Build**

Run:

```powershell
rtk dotnet build NamEcommerce\NamEcommerce.sln
```

Expected: build succeeds.

- [ ] **Step 2: Search stale naming**

Run:

```powershell
rtk powershell -NoProfile -Command "Get-ChildItem -Path NamEcommerce -Recurse -File -Include *.cs,*.cshtml,*.js | Select-String -Pattern 'OrderStatus.Locked|LockOrder|CanLockOrder|LockOrderReason|OrderLocked|Khóa đơn|Đã khóa'"
```

Expected: no active code references. Migration files may still contain old names if included by broader searches.

- [ ] **Step 3: Confirm migration note**

Final response must tell Tuấn to create/run EF migration for:

- `OrderStatus.Locked` business rename to `Completed` while preserving value `1`.
- `Order.CompletedOnUtc`.
- `Expense.SourceOrderId`.
- Retiring old `LockOrderReason` column if desired.

- [ ] **Step 4: Browser/UI check when possible**

If the app can be launched locally without new dependency installation, start it and inspect one order detail page. Confirm:

- workflow bar renders
- active stage opens by default
- step click changes panels only
- timeline stays on the right on desktop
- settlement panel renders empty states without crashing

If local app launch is blocked by environment/database, report that clearly.

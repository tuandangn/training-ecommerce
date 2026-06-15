# Order Fulfillment Schedule Implementation Plan

> **For agentic workers:** Implement task by task. Keep each checkbox updated. Use the existing Clean Architecture flow: Controller -> MediatR -> Handler -> AppService -> Manager -> Entity -> Repository.

**Goal:** Add a multi-stage order fulfillment schedule so admins can plan, view, and act on partial deliveries by date, item, and quantity.

**Architecture:** Persist customer/order delivery intentions as `OrderFulfillmentSchedule` records under the Orders module. Keep `DeliveryNote` and `DeliveryRun` as execution records, and use shortage/PO allocation data to derive board risk states. The first version is an operational board plus schedule CRUD, not route optimization.

**Tech Stack:** ASP.NET Core MVC, Razor, MediatR, EF Core, Bootstrap 5, existing `theme.css`/page CSS conventions.

---

## Assumptions

- Worktree: `D:\Learning\NamTraining\training-ecommerce\.worktrees\new-modules`.
- Feature scope is MVP direction 2: multi-stage fulfillment schedule with schedule items and quantities.
- Existing `Order.ExpectedShippingDateUtc` remains for backward compatibility and initial order entry. It is not removed in this slice.
- Schedule date/time is stored in UTC. UI displays local time using existing `ToLocalTime()` convention.
- Viewing the board uses `SystemPermissions.Orders.View`. Creating, updating, activating, and inactivating schedules uses `SystemPermissions.Orders.Edit`.
- Do not add new automated tests for this slice.
- Do not create EF migrations. The user will create migrations after reviewing the implementation.
- Build projects only when truly needed: after broad contract changes, before handoff, or when compile errors are likely. Do not build after every small edit.
- Add code comments and XML summaries only when they clarify non-obvious business rules.

## Out Of Scope

- Drag and drop calendar editing.
- Vehicle, driver capacity, route optimization, weight/volume capacity, and map routing.
- Removing or redesigning existing order, delivery note, or delivery run workflows.
- Replacing `DeliveryNote` with schedule records. Schedule is intention; delivery note/run is execution.

## Success Criteria

- Admin can open a fulfillment board showing Today, 3 days, 7 days, 1 month, and unscheduled buckets.
- Board includes active pending orders that are not fully delivered, active delivery notes, delivery runs, and PO dependencies for blocked items.
- Admin can add multiple schedules for one order.
- Each schedule can target specific order items and quantities.
- Schedule can be active/inactive with note.
- Order creation creates default schedules:
  - with promised date: `NotBeforeDate` for all ordered quantities;
  - without promised date: `AsSoonAsPossible` for currently available quantities and `WhenStockAvailable` for the remaining quantities.
- When allocated PO receiving makes waiting items deliverable, active `WhenStockAvailable` schedules can be refreshed to `AsSoonAsPossible`.
- Web project builds when final verification is required.
- UI lint passes for the new Razor/CSS files.

---

## Task 1: Add Domain Model And Enums

**Files:**
- Create: `NamEcommerce\Domain\NamEcommerce.Domain.Shared\Enums\Orders\OrderFulfillmentScheduleMode.cs`
- Create: `NamEcommerce\Domain\NamEcommerce.Domain\Entities\Orders\OrderFulfillmentSchedule.cs`
- Create: `NamEcommerce\Domain\NamEcommerce.Domain\Entities\Orders\OrderFulfillmentScheduleItem.cs`
- Create: `NamEcommerce\Domain\NamEcommerce.Domain.Shared\Exceptions\Orders\OrderFulfillmentScheduleDataIsInvalidException.cs`
- Create: `NamEcommerce\Domain\NamEcommerce.Domain.Shared\Exceptions\Orders\OrderFulfillmentScheduleIsNotFoundException.cs`

**Implementation:**

- [x] Create enum:

```csharp
namespace NamEcommerce.Domain.Shared.Enums.Orders;

public enum OrderFulfillmentScheduleMode
{
    AsSoonAsPossible = 10,
    NotBeforeDate = 20,
    WhenStockAvailable = 30
}
```

- [x] Create `OrderFulfillmentSchedule` as `sealed record : AppAggregateEntity` with:
  - `OrderId`
  - `OrderCode`
  - `ScheduledFromUtc`
  - `ScheduledToUtc`
  - `Mode`
  - `Note`
  - `IsActive`
  - `CreatedByUserId`
  - `CreatedOnUtc`
  - `UpdatedOnUtc`
  - `InactivatedOnUtc`
  - private list `_items`
  - `IReadOnlyCollection<OrderFulfillmentScheduleItem> Items`

- [x] Domain methods:
  - constructor validates `OrderId != Guid.Empty`;
  - `SetWindow(DateTime? scheduledFromUtc, DateTime? scheduledToUtc)` validates `scheduledToUtc >= scheduledFromUtc` when both exist;
  - `SetMode(OrderFulfillmentScheduleMode mode)` validates enum;
  - `SetNote(string? note)` trims empty to null;
  - `ReplaceItems(IEnumerable<CreateOrderFulfillmentScheduleItemDto> items)` validates at least one item and positive quantities;
  - `Activate()` and `Inactivate()`.

- [x] Create child `OrderFulfillmentScheduleItem : AppEntity` with:
  - `OrderFulfillmentScheduleId`
  - `OrderItemId`
  - `ProductId`
  - `ProductName`
  - `Quantity`
  - `CreatedOnUtc`

**Verify:** No build required for this task unless compiler errors are suspected after the edit.

---

## Task 2: Add Domain DTOs, Manager Contract, And Extensions

**Files:**
- Create: `NamEcommerce\Domain\NamEcommerce.Domain.Shared\Dtos\Orders\OrderFulfillmentScheduleDtos.cs`
- Create: `NamEcommerce\Domain\NamEcommerce.Domain.Shared\Services\Orders\IOrderFulfillmentScheduleManager.cs`
- Create: `NamEcommerce\Domain\NamEcommerce.Domain.Services\Orders\OrderFulfillmentScheduleManager.cs`
- Create: `NamEcommerce\Domain\NamEcommerce.Domain.Services\Extensions\OrderFulfillmentScheduleExtensions.cs`

**Implementation:**

- [x] DTOs:
  - `OrderFulfillmentScheduleDto(Guid Id)`
  - `OrderFulfillmentScheduleItemDto(Guid Id)`
  - `CreateOrderFulfillmentScheduleDto`
  - `CreateOrderFulfillmentScheduleItemDto`
  - `CreateOrderFulfillmentScheduleResultDto`
  - `UpdateOrderFulfillmentScheduleDto(Guid Id)`
  - `UpdateOrderFulfillmentScheduleResultDto`
  - `SetOrderFulfillmentScheduleActiveDto(Guid Id, bool IsActive)`

- [x] `Verify()` rules:
  - `OrderId` required;
  - `Mode` must be defined;
  - `NotBeforeDate` requires `ScheduledFromUtc`;
  - item list must not be empty;
  - every item requires `OrderItemId`, `ProductId`, and `Quantity > 0`;
  - `ScheduledToUtc` cannot be earlier than `ScheduledFromUtc`.

- [x] Manager contract methods:

```csharp
Task<OrderFulfillmentScheduleDto?> GetByIdAsync(Guid id);
Task<IList<OrderFulfillmentScheduleDto>> GetByOrderIdAsync(Guid orderId);
Task<IList<OrderFulfillmentScheduleDto>> GetActiveByOrderIdsAsync(IReadOnlyCollection<Guid> orderIds);
Task<CreateOrderFulfillmentScheduleResultDto> CreateAsync(CreateOrderFulfillmentScheduleDto dto);
Task<UpdateOrderFulfillmentScheduleResultDto> UpdateAsync(UpdateOrderFulfillmentScheduleDto dto);
Task SetActiveAsync(SetOrderFulfillmentScheduleActiveDto dto);
Task RefreshWhenStockAvailableAsync(IReadOnlyCollection<Guid> orderItemIds);
```

- [x] Manager dependencies:
  - `IRepository<OrderFulfillmentSchedule>`
  - `IEntityDataReader<OrderFulfillmentSchedule>`
  - `IEntityDataReader<Order>`
  - `IEntityDataReader<DeliveryNote>`
  - `IShortageQueryService`
  - `ICurrentUserAccessor`

- [x] Manager create/update validation:
  - order exists and is not completed/cancelled;
  - every schedule item belongs to the order;
  - quantity per schedule item cannot exceed remaining undelivered order item quantity across active schedules, excluding the current schedule when updating;
  - inactive schedules are ignored in the active quantity limit.

**Verify:** Build `NamEcommerce\Domain\NamEcommerce.Domain.Services\NamEcommerce.Domain.Services.csproj` only if the manager contract or implementation has unresolved compile risk.

---

## Task 3: Add EF Mapping And Leave Migration To User

**Files:**
- Create: `NamEcommerce\Infrastructure\NamEcommerce.Data.SqlServer\Mappings\OrderFulfillmentScheduleMapping.cs`
- Create: `NamEcommerce\Infrastructure\NamEcommerce.Data.SqlServer\Mappings\OrderFulfillmentScheduleItemMapping.cs`

**Implementation:**

- [x] Map `OrderFulfillmentSchedule` to table `OrderFulfillmentSchedule`.
- [x] Add indexes:
  - `OrderId`
  - `ScheduledFromUtc`
  - `Mode`
  - `IsActive`
- [x] Map `OrderFulfillmentScheduleItem` to table `OrderFulfillmentScheduleItem`.
- [x] Add indexes:
  - `OrderFulfillmentScheduleId`
  - `OrderItemId`
  - `ProductId`
- [x] Configure cascade from schedule to items.
- [x] Decimal quantities use `decimal(18,2)`.
- [x] String max lengths:
  - `OrderCode`: 50
  - `ProductName`: 500
  - `Note`: 1000
- [x] Do not run any EF migration creation command. Stop after mapping code and remind the user to create the migration.

**Verify:** Build `NamEcommerce\Infrastructure\NamEcommerce.Data.SqlServer\NamEcommerce.Data.SqlServer.csproj` only if the mapping does not clearly compile by inspection.

---

## Task 4: Extend Shortage/Fulfillment Read Data

**Files:**
- Modify: `NamEcommerce\Domain\NamEcommerce.Domain.Shared\Dtos\Inventory\ShortageDtos.cs`
- Modify: `NamEcommerce\Domain\NamEcommerce.Domain.Shared\Services\Inventory\IShortageQueryService.cs`
- Modify: `NamEcommerce\Domain\NamEcommerce.Domain.Services\Inventory\ShortageQueryService.cs`

**Implementation:**

- [x] Add DTO `OrderItemFulfillmentStateDto` with:
  - `OrderId`
  - `OrderCode`
  - `OrderItemId`
  - `ProductId`
  - `ProductName`
  - `RequiredQuantity`
  - `ShippedQuantity`
  - `AvailableQuantity`
  - `AllocatedIncomingQuantity`
  - `MissingSourceQuantity`
  - `AllocatedFromPurchaseOrders`

- [x] Add method:

```csharp
Task<IList<OrderItemFulfillmentStateDto>> GetOrderItemFulfillmentStatesAsync(Guid orderId);
```

- [x] Refactor `ShortageQueryService.BuildOrderItemShortagesAsync` so existing shortage methods keep current behavior, while the new method returns all order items with `MissingSourceQuantity = Math.Max(0, stillNeeded - availableQuantity - allocatedIncoming)`.

**Verify:** No build required unless the refactor touches public contracts in a way that is hard to validate by inspection.

---

## Task 5: Add Application Contracts And App Service

**Files:**
- Create: `NamEcommerce\Application\NamEcommerce.Application.Contracts\Dtos\Orders\OrderFulfillmentScheduleAppDtos.cs`
- Create: `NamEcommerce\Application\NamEcommerce.Application.Contracts\Orders\IOrderFulfillmentScheduleAppService.cs`
- Create: `NamEcommerce\Application\NamEcommerce.Application.Services\Orders\OrderFulfillmentScheduleAppService.cs`
- Create: `NamEcommerce\Application\NamEcommerce.Application.Services\Extensions\OrderFulfillmentScheduleExtensions.cs`

**Implementation:**

- [x] Add app DTOs mirroring domain schedule DTOs with `Validate()` returning `(bool, string?)`.
- [x] Add board DTOs:
  - `OrderFulfillmentBoardAppDto`
  - `OrderFulfillmentBoardDayAppDto`
  - `OrderFulfillmentBoardEntryAppDto`
  - `OrderFulfillmentBoardItemAppDto`
  - `OrderFulfillmentBoardDependencyAppDto`
  - `OrderFulfillmentUnscheduledGroupAppDto`
- [x] Add app service methods:

```csharp
Task<OrderFulfillmentScheduleAppDto?> GetByIdAsync(Guid id);
Task<IList<OrderFulfillmentScheduleAppDto>> GetByOrderIdAsync(Guid orderId);
Task<CreateOrderFulfillmentScheduleResultAppDto> CreateAsync(CreateOrderFulfillmentScheduleAppDto dto);
Task<UpdateOrderFulfillmentScheduleResultAppDto> UpdateAsync(UpdateOrderFulfillmentScheduleAppDto dto);
Task<CommonActionResultDto> SetActiveAsync(SetOrderFulfillmentScheduleActiveAppDto dto);
Task<OrderFulfillmentBoardAppDto> GetBoardAsync(OrderFulfillmentBoardFilterAppDto filter);
Task<CommonActionResultDto> RefreshWhenStockAvailableForPurchaseOrderItemsAsync(IReadOnlyCollection<Guid> purchaseOrderItemIds);
```

- [x] `GetBoardAsync` loads:
  - pending orders that are not fully delivered;
  - active schedules for those orders;
  - delivery notes with status `Confirmed` or `Delivering`;
  - delivery runs with status `ReadyForHandover` or `HandedToDriver`;
  - purchase order dependencies from fulfillment state allocations.

- [x] Board bucketing:
  - `Today`: entries whose scheduled local date is today, grouped by hour;
  - `Next3Days`: local date from tomorrow through day 3, grouped by day plus simple time;
  - `Next7Days`: local date through day 7, grouped by day;
  - `Next30Days`: local date through day 30, grouped by day;
  - `UnscheduledReady`: no date and has available quantity;
  - `UnscheduledWaitingPo`: no date, no available quantity, has allocated incoming quantity;
  - `UnscheduledNoSource`: no date, missing source quantity > 0.

- [x] Risk/tone rules:
  - `danger`: active schedule date has passed and undelivered quantity remains;
  - `danger`: due today and available quantity is less than scheduled quantity;
  - `warning`: due within 24 hours;
  - `warning`: waiting on PO expected after scheduled date;
  - `success`: available quantity covers scheduled quantity;
  - `info`: delivery note/run is currently executing;
  - `muted`: inactive schedule.

**Verify:** Build Application.Contracts and Application.Services only if DTO/service signatures changed enough that compile risk is high.

---

## Task 6: Create Default Schedules From Order Flows

**Files:**
- Modify: `NamEcommerce\Application\NamEcommerce.Application.Services\Orders\OrderAppService.cs`
- Modify: `NamEcommerce\Application\NamEcommerce.Application.Services\Events\PurchaseOrders\PurchaseOrderItemReceivedHandler.cs`
- Modify: `NamEcommerce\Application\NamEcommerce.Application.Services\Events\PurchaseOrders\PurchaseOrderBulkReceivedHandler.cs`

**Implementation:**

- [x] Inject `IOrderFulfillmentScheduleAppService` into `OrderAppService`.
- [x] After successful `CreateOrderAsync`, fetch the created order and fulfillment states.
- [x] If `ExpectedShippingDateUtc` has value, create one `NotBeforeDate` schedule containing all order items and their ordered quantities.
- [x] If no expected date:
  - create one `AsSoonAsPossible` schedule for available quantities;
  - create one `WhenStockAvailable` schedule for remaining quantities;
  - do not create an empty schedule.
- [x] When order items are added later, create or extend default schedules using the same rules.
- [x] When order item quantity is reduced, require active scheduled quantity not to exceed the new remaining undelivered quantity. Return `Error.OrderFulfillmentScheduleQuantityExceedsOrderItemQuantity` from app service instead of throwing in UI flow.
- [x] In PO received handlers, call `RefreshWhenStockAvailableForPurchaseOrderItemsAsync` with affected purchase order item ids. The app service resolves allocation order item ids, then calls the manager refresh method. This switches active `WhenStockAvailable` schedules to `AsSoonAsPossible` only when fulfillment state now has available quantity.

**Verify:** Build Application.Services only if constructor injection or event handler changes are likely to break compilation.

---

## Task 7: Add Web Contracts And MediatR Handlers

**Files:**
- Create: `NamEcommerce\Presentation\NamEcommerce.Web.Contracts\Models\Orders\OrderFulfillmentScheduleModels.cs`
- Create: `NamEcommerce\Presentation\NamEcommerce.Web.Contracts\Queries\Models\Orders\GetOrderFulfillmentBoardQuery.cs`
- Create: `NamEcommerce\Presentation\NamEcommerce.Web.Contracts\Queries\Models\Orders\GetOrderFulfillmentSchedulesByOrderQuery.cs`
- Create: `NamEcommerce\Presentation\NamEcommerce.Web.Contracts\Commands\Models\Orders\OrderFulfillmentScheduleCommands.cs`
- Create: `NamEcommerce\Presentation\NamEcommerce.Web.Framework\Queries\Handlers\Orders\OrderFulfillmentScheduleQueryHandlers.cs`
- Create: `NamEcommerce\Presentation\NamEcommerce.Web.Framework\Commands\Handlers\Orders\OrderFulfillmentScheduleCommandHandlers.cs`

**Implementation:**

- [x] Query models:
  - board query with `Range`, `Date`, `Keywords`, `Risk`, `IncludeInactive`;
  - schedules-by-order query with `OrderId`.
- [x] Command models:
  - `CreateOrderFulfillmentScheduleCommand`
  - `UpdateOrderFulfillmentScheduleCommand`
  - `SetOrderFulfillmentScheduleActiveCommand`
- [x] Result models use existing `CommonActionResultModel` where no created/updated id is required.
- [x] Handlers map web contracts to app DTOs and return app result values without business rules in handlers.

**Verify:** Build Web.Contracts and Web.Framework only if MediatR command/query signatures need a compile checkpoint.

---

## Task 8: Add Web Models, Model Factory, Controller, And Menu

**Files:**
- Create: `NamEcommerce\Presentation\NamEcommerce.Web\Models\OrderFulfillment\OrderFulfillmentBoardSearchModel.cs`
- Create: `NamEcommerce\Presentation\NamEcommerce.Web\Models\OrderFulfillment\OrderFulfillmentScheduleInputModel.cs`
- Create: `NamEcommerce\Presentation\NamEcommerce.Web\Models\OrderFulfillment\OrderFulfillmentScheduleInputValidator.cs`
- Create: `NamEcommerce\Presentation\NamEcommerce.Web\Services\OrderFulfillment\IOrderFulfillmentModelFactory.cs`
- Create: `NamEcommerce\Presentation\NamEcommerce.Web\Services\OrderFulfillment\OrderFulfillmentModelFactory.cs`
- Create: `NamEcommerce\Presentation\NamEcommerce.Web\Controllers\OrderFulfillmentController.cs`
- Modify: `NamEcommerce\Presentation\NamEcommerce.Web\Program.cs`
- Modify: `NamEcommerce\Presentation\NamEcommerce.Web\Models\Common\MenuNavigationModel.cs`
- Modify: `NamEcommerce\Presentation\NamEcommerce.Web\Components\MenuNavigationComponent.cs`
- Modify: `NamEcommerce\Presentation\NamEcommerce.Web\Views\Shared\Components\MenuNavigationComponent\Default.cshtml`
- Modify: `NamEcommerce\Presentation\NamEcommerce.Web\Views\_ViewImports.cshtml`

**Implementation:**

- [x] Controller actions:
  - `Index(OrderFulfillmentBoardSearchModel search)` returns board view;
  - `OrderSchedules(Guid orderId)` returns schedule partial for order details;
  - `CreateSchedule(OrderFulfillmentScheduleInputModel model)` returns JSON;
  - `UpdateSchedule(OrderFulfillmentScheduleInputModel model)` returns JSON;
  - `SetScheduleActive(Guid id, bool isActive)` returns JSON.
- [x] Controller injects only `IMediator` and `IOrderFulfillmentModelFactory`.
- [x] Model factory injects only `IMediator` and `AppConfig`.
- [x] Add DI:
  - `IOrderFulfillmentScheduleManager`
  - `IOrderFulfillmentScheduleAppService`
  - `IOrderFulfillmentModelFactory`
  - validator.
- [x] Menu:
  - add `CanViewOrderFulfillmentSchedule = Orders.View`;
  - add link under `Bán hàng`: `Lịch xử lý đơn hàng`;
  - active when controller is `OrderFulfillment`.

**Verify:** Build Web only if DI, controller, or model factory changes are difficult to validate by inspection.

---

## Task 9: Build Board UI

**Files:**
- Create: `NamEcommerce\Presentation\NamEcommerce.Web\Views\OrderFulfillment\Index.cshtml`
- Create: `NamEcommerce\Presentation\NamEcommerce.Web\Views\OrderFulfillment\_BoardFilters.cshtml`
- Create: `NamEcommerce\Presentation\NamEcommerce.Web\Views\OrderFulfillment\_TimelineDay.cshtml`
- Create: `NamEcommerce\Presentation\NamEcommerce.Web\Views\OrderFulfillment\_EntryCard.cshtml`
- Create: `NamEcommerce\Presentation\NamEcommerce.Web\Views\OrderFulfillment\_UnscheduledBuckets.cshtml`
- Create: `NamEcommerce\Presentation\NamEcommerce.Web\Views\OrderFulfillment\_ScheduleModal.cshtml`
- Create: `NamEcommerce\Presentation\NamEcommerce.Web\wwwroot\css\pages\order-fulfillment.css`

**Implementation:**

- [x] Use `@section Styles` to load `~/css/pages/order-fulfillment.css`.
- [x] Use Bootstrap icons already available in the project.
- [x] Use existing shared components for page header, filters, status badges, quantity display, empty state, and confirm modal when practical.
- [x] Board tabs:
  - `Hôm nay`
  - `3 ngày`
  - `7 ngày`
  - `1 tháng`
  - `Chưa hẹn`
- [x] Card content:
  - entity type icon;
  - order code or delivery note/run/PO code;
  - customer name and phone;
  - scheduled time/date;
  - item rows with quantity;
  - availability summary;
  - dependency PO rows with expected receive date;
  - action link to details page.
- [x] No inline `style=""` and no Razor `<style>` block.
- [x] CSS uses existing Bootstrap variables and `--app-*` tokens. Do not add new raw hex colors unless a token is added in `theme.css`.
- [x] Mobile layout stacks day sections and keeps action buttons tappable.

**Verify:** Run `powershell -ExecutionPolicy Bypass -File tools\ui-lint.ps1`. Build Web only if Razor/model changes create compile risk.

---

## Task 10: Add Schedule Management To Order Details

**Files:**
- Modify: `NamEcommerce\Presentation\NamEcommerce.Web.Contracts\Models\Orders\OrderModel.cs`
- Modify: `NamEcommerce\Presentation\NamEcommerce.Web\Models\Orders\OrderDetailsModel.cs`
- Modify: `NamEcommerce\Presentation\NamEcommerce.Web\Services\Orders\OrderModelFactory.cs`
- Modify: `NamEcommerce\Presentation\NamEcommerce.Web\Views\Order\Details.cshtml`
- Create: `NamEcommerce\Presentation\NamEcommerce.Web\Views\Order\_OrderFulfillmentSchedulePanel.cshtml`

**Implementation:**

- [x] Add schedule summary to order details model:
  - active schedule count;
  - next scheduled date;
  - risk tone;
  - item/quantity rows.
- [x] In `OrderModelFactory.PrepareOrderDetailsModel`, load schedules by order id through MediatR.
- [x] Render a new panel after preparation panel and before delivery panel.
- [x] Add buttons:
  - add schedule;
  - edit schedule;
  - activate/inactivate schedule;
  - create delivery note from a ready schedule links to existing delivery note creation flow.
- [x] Do not move existing delivery note, direct ship, return, debt, or settlement sections.

**Verify:** Build Web only if model/view changes are likely to break Razor compilation.

---

## Task 11: Navigation Links And Entity Details Links

**Files:**
- Modify: board and order detail partials from Tasks 9 and 10.

**Implementation:**

- [x] Order card links to `OrderController.Details`.
- [x] PO dependency links to `PurchaseOrderController.Details`.
- [x] Delivery note card links to `DeliveryNoteController.Details`.
- [x] Delivery run card links to `DeliveryRunController.Details`.
- [x] Use normal anchors for navigation. Buttons are reserved for commands.
- [x] Icon-only buttons have `title` and `aria-label`.

**Verify:**

Manual smoke in browser:

Pending until the user creates the EF migration and updates the database schema.

- open `/OrderFulfillment`;
- click an order card;
- click a PO dependency;
- click a delivery note/run card.

Expected: each opens the correct detail page.

---

## Task 14: InactivatedByUserId + Pre-fill Delivery Note From Schedule

**Files:**
- Modify: `NamEcommerce\Domain\NamEcommerce.Domain\Entities\Orders\OrderFulfillmentSchedule.cs`
- Modify: `NamEcommerce\Infrastructure\NamEcommerce.Data.SqlServer\Mappings\OrderFulfillmentScheduleMapping.cs`
- Modify: `NamEcommerce\Domain\NamEcommerce.Domain.Shared\Dtos\Orders\OrderFulfillmentScheduleDtos.cs`
- Modify: `NamEcommerce\Domain\NamEcommerce.Domain.Services\Extensions\OrderFulfillmentScheduleExtensions.cs`
- Modify: `NamEcommerce\Domain\NamEcommerce.Domain.Services\Orders\OrderFulfillmentScheduleManager.cs`
- Modify: `NamEcommerce\Application\NamEcommerce.Application.Contracts\Dtos\Orders\OrderFulfillmentScheduleAppDtos.cs`
- Modify: `NamEcommerce\Application\NamEcommerce.Application.Services\Extensions\OrderFulfillmentScheduleExtensions.cs`
- Modify: `NamEcommerce\Presentation\NamEcommerce.Web.Contracts\Models\Orders\OrderFulfillmentScheduleModels.cs`
- Modify: `NamEcommerce\Presentation\NamEcommerce.Web.Framework\Queries\Handlers\Orders\OrderFulfillmentScheduleQueryHandlers.cs`
- Modify: `NamEcommerce\Presentation\NamEcommerce.Web\Views\Order\_OrderFulfillmentSchedulePanel.cshtml`
- Modify: `NamEcommerce\Presentation\NamEcommerce.Web\Views\OrderFulfillment\_EntryCard.cshtml`

**Implementation:**

- [x] Add `InactivatedByUserId` property to `OrderFulfillmentSchedule` entity.
- [x] Update `Inactivate()` to `Inactivate(Guid? inactivatedByUserId)` — sets both `InactivatedOnUtc` and `InactivatedByUserId`.
- [x] Add `InactivatedByUserId` to EF mapping (nullable, no index needed).
- [x] Propagate `InactivatedByUserId` through domain DTO → domain extension → app DTO → app extension → web model → query handler.
- [x] In `OrderFulfillmentScheduleManager.SetActiveAsync`: when inactivating, call `currentUserAccessor.GetCurrentUserAsync()` and pass the id to `Inactivate()`.
- [x] Pre-fill delivery note from schedule panel: pass `asp-route-selected` with comma-joined `OrderItemId`s from schedule items so `DeliveryNote/Create` pre-selects those items.
- [x] Pre-fill delivery note from board entry card: when `SourceType == "Schedule"`, pass `selected` query param with item ids. "Order" entries get no pre-selection (unknown which items apply).

**Migration required:** `InactivatedByUserId` is a new column — user must create migration.

---

## Task 13: Board Improvements (Overdue Bucket, Customer Search, Unscheduled Tab)

**Files:**
- Modify: `NamEcommerce\Application\NamEcommerce.Application.Contracts\Dtos\Orders\OrderFulfillmentScheduleAppDtos.cs`
- Modify: `NamEcommerce\Application\NamEcommerce.Application.Services\Orders\OrderFulfillmentScheduleAppService.cs`
- Modify: `NamEcommerce\Presentation\NamEcommerce.Web.Contracts\Models\Orders\OrderFulfillmentScheduleModels.cs`
- Modify: `NamEcommerce\Presentation\NamEcommerce.Web.Framework\Queries\Handlers\Orders\OrderFulfillmentScheduleQueryHandlers.cs`
- Modify: `NamEcommerce\Presentation\NamEcommerce.Web\Views\OrderFulfillment\Index.cshtml`
- Modify: `NamEcommerce\Presentation\NamEcommerce.Web\Views\OrderFulfillment\_BoardFilters.cshtml`

**Implementation:**

- [x] Add `Overdue` bucket to board: entries where `ScheduledFromUtc < today` in local time, grouped by day (same `BuildDays` method with `[MinValue, yesterday]` range).
- [x] Add `OverdueCount` to `OrderFulfillmentBoardAppDto` and `OrderFulfillmentBoardModel`. Surface in metrics bar with red badge; show "Quá hạn" tab as the active default tab when count > 0.
- [x] Fix keyword search: restructure `GetBoardAsync` to apply keyword filter **after** states are loaded so customer name and phone (from `OrderItemFulfillmentStateDto`) can be included in the match. Remove keyword parameter from `GetOpenOrders`.
- [x] Fix "Chưa hẹn" (Unscheduled) tab: add missing nav-pill and `_UnscheduledBuckets` tab-pane to `Index.cshtml`.
- [x] Update filter placeholder to `"Mã đơn, địa chỉ, tên/SĐT khách"`.

---

## Task 12: Final Verification

**Commands:**

```powershell
git diff --check
powershell -ExecutionPolicy Bypass -File tools\ui-lint.ps1
```

Run `dotnet build NamEcommerce\Presentation\NamEcommerce.Web\NamEcommerce.Web.csproj -v:minimal` only when final changed code needs compile confirmation.

**Manual smoke checks:**

- [ ] Create order with expected shipping date and verify one `NotBeforeDate` schedule.
- [ ] Create order without expected shipping date and verify ASAP/WhenStockAvailable schedules.
- [ ] Add second schedule for a subset of order items and quantities.
- [ ] Inactivate schedule and verify it is muted/hidden from active board.
- [ ] Receive allocated PO and verify waiting schedule becomes deliverable or visible as ready.
- [ ] Open board Today, 3 days, 7 days, 1 month, and unscheduled buckets.
- [ ] Confirm overdue order appears danger.
- [ ] Confirm upcoming order appears warning.
- [ ] Confirm enough-stock order appears success.
- [ ] Confirm missing-stock due-today order appears danger.
- [ ] Confirm delivery note/run appears as execution entry.
- [x] Confirm no new `<style>` or `style=""` appears in new Razor files.

**Database note:**

- Do not create migrations.
- Do not update the database.
- Remind the user that they will create the migration after implementation.

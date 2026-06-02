# Purchase Order Detail Workflow Plan

## Assumptions

- Scope follows option 2: add edit-line support for purchase order items and record timeline notes for add, edit, and remove item actions.
- Item add, edit, and remove are allowed only while the purchase order can still update items. Once any goods have been received, item editing remains locked by the existing business rule.
- The timeline must show enough detail to audit a line change: product name, old quantity, new quantity, old unit cost, new unit cost, and note changes when applicable.
- Existing receive, bulk receive, direct-ship allocation, vendor return, close partial, cancel, and status actions keep their current behavior.
- The UI should mirror the sales order workflow layout, but this is not a visual redesign. Reuse existing `order-workflow-*` CSS classes where possible.
- This feature needs a small persisted activity table. Current purchase order state is not enough to reconstruct old quantity or old unit cost after an edit or delete.

## Success Criteria

- Purchase order detail page uses a workflow bar with these steps: `Dat hang`, `Nhan hang`, `Tra hang`, `Ket so`.
- A timeline column is visible on the purchase order detail page, matching the sales order detail layout pattern.
- `Dat hang` panel shows vendor/order info, item table, allocation rows, and add/edit/remove item controls when allowed.
- Editing an item can change ordered quantity, unit cost, and item note before receiving starts.
- Adding, editing, and removing a purchase order item writes a persisted activity note and displays it in Timeline.
- Timeline also shows derived events for order creation, status changes, goods receipts, vendor returns, closing, cancelling, and completion.
- `Nhan hang` panel shows item receive progress and related goods receipts.
- `Tra hang` panel shows vendor returns linked to the purchase order.
- `Ket so` panel shows order totals, received value, returned value, net purchase value, and vendor debt/payment information available from existing services.
- Build verification covers modified projects. Full solution build remains excluded because the solution currently fails on the `NamEcommerce.Customer.Client` website project requiring .NET Framework MSBuild.

## Non-goals

- Do not build a generic audit framework for every module.
- Do not change sales order behavior.
- Do not change receiving, allocation, return, debt, or status transition rules except where needed to support item edit before receiving.
- Do not add new payment workflows inside purchase order detail. Link or summarize existing vendor debt/payment data only.

## Proposed Design

### Persisted activity

Add a purchase-order-scoped activity entity, for example `PurchaseOrderActivity`, with:

- `PurchaseOrderId`
- `ActivityType`
- `Title`
- `Description`
- `Tone`
- `Icon`
- `OccurredOnUtc`

Use this only for user actions that cannot be reconstructed from final state, especially item add/edit/remove. Other timeline rows can still be derived from existing purchase order, goods receipt, vendor return, and debt data.

### Item edit flow

Add a narrow update path:

- Web model: `EditPurchaseOrderItemModel`
- Command: `UpdatePurchaseOrderItemCommand`
- Handler: `UpdatePurchaseOrderItemHandler`
- App DTO: `UpdatePurchaseOrderItemAppDto`
- Domain DTO: `UpdatePurchaseOrderItemDto`
- Domain method: update one item quantity, unit cost, and note after validating `CanUpdateItems()`

The update command captures old values before mutation and records one activity row after a successful update.

### Detail workflow view

Split the large purchase order detail view into partials:

- `_PurchaseOrderWorkflowBar.cshtml`
- `_PurchaseOrderWorkflowOrderingPanel.cshtml`
- `_PurchaseOrderWorkflowReceivingPanel.cshtml`
- `_PurchaseOrderWorkflowReturnsPanel.cshtml`
- `_PurchaseOrderWorkflowSettlementPanel.cshtml`
- `_PurchaseOrderWorkflowTimeline.cshtml`

Keep the existing modals for receive, bulk receive, allocation, direct ship, close partial, and cancel. Add only the edit item modal and the JavaScript needed to submit it.

## TodoList

- [ ] Add purchase order activity domain entity, enum, DTOs, EF mapping, and migration.
- [ ] Add app service methods to record and read purchase order activities.
- [ ] Add domain/app/web update item flow for purchase order items.
- [ ] Record activity notes when adding, editing, and removing purchase order items.
- [ ] Extend `PurchaseOrderDetailsModel` with workflow, section, settlement, and timeline models.
- [ ] Update `PurchaseOrderModelFactory` to build workflow steps, active stage, section data, settlement summary, and timeline rows.
- [ ] Split `Views/PurchaseOrder/Details.cshtml` into workflow partials using the sales order detail layout as the reference.
- [ ] Add edit item modal and JavaScript wiring on purchase order detail.
- [ ] Add minimal CSS only if existing workflow classes do not cover the purchase order page cleanly.
- [ ] Verify with targeted project builds and a manual UI smoke test.

## Verification Plan

- `rtk dotnet build NamEcommerce\Domain\NamEcommerce.Domain.Shared\NamEcommerce.Domain.Shared.csproj`
- `rtk dotnet build NamEcommerce\Domain\NamEcommerce.Domain\NamEcommerce.Domain.csproj`
- `rtk dotnet build NamEcommerce\Domain\NamEcommerce.Domain.Services\NamEcommerce.Domain.Services.csproj`
- `rtk dotnet build NamEcommerce\Application\NamEcommerce.Application.Contracts\NamEcommerce.Application.Contracts.csproj`
- `rtk dotnet build NamEcommerce\Application\NamEcommerce.Application.Services\NamEcommerce.Application.Services.csproj`
- `rtk dotnet build NamEcommerce\Presentation\NamEcommerce.Web.Contracts\NamEcommerce.Web.Contracts.csproj`
- `rtk dotnet build NamEcommerce\Presentation\NamEcommerce.Web.Framework\NamEcommerce.Web.Framework.csproj`
- `rtk dotnet build NamEcommerce\Presentation\NamEcommerce.Web\NamEcommerce.Web.csproj`


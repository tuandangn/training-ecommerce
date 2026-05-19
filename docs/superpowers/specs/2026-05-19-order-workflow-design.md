# Order Workflow Detail Design

Date: 2026-05-19

## Goal

Redesign the order detail page so users can understand an order as a clear 4-step workflow:

1. Dat hang: customer information and ordered products.
2. Chuan bi hang: stock check, shortages, purchase orders, direct ship information.
3. Giao hang: delivery notes and delivery progress.
4. Ket so: expenses, customer debt, cost of goods, profit, and order completion.

The page should make the current stage obvious, allow users to click any stage to inspect its details, and keep a vertical timeline visible for the full order history.

## Current Context

The system is ASP.NET Core MVC with Razor views, MediatR queries/commands, application services, and domain managers. The current `Order/Details` page already has order items, customer/shipping/note blocks, shortage notice, allocated purchase orders through an offcanvas, delivery notes through an offcanvas, and direct-ship allocation data.

The existing `OrderStatus` values are:

- `Pending`
- `Locked`
- `Cancelled`

`Locked` is currently being used as the business meaning "completed". This will be corrected.

The current delivery note status already exists at note level:

- `Draft`
- `Confirmed`
- `Delivering`
- `Delivered`
- `Cancelled`

`DeliveryNoteItem.CostAtDispatch` already stores cost snapshot at dispatch time and is the right source for cost/profit reporting per delivery.

## Decisions

- Upgrade the existing `Order/Details` page instead of creating a parallel workflow page.
- Rename the business meaning of `OrderStatus.Locked` to `OrderStatus.Completed`.
- Store real completion time on the order with `CompletedOnUtc`.
- Do not require a completion reason. Existing order notes remain in `Order.Note`.
- Keep `Cancelled` and delete actions separate from completion.
- Compute the active workflow stage from order data. Do not store a manual workflow stage.
- Compute overall delivery status from delivery notes. Do not store it separately on the order.
- Add `Expense.SourceOrderId` so expenses can be attached directly to an order.
- Keep the timeline always visible on desktop in a sticky right column; on mobile, show it below the workflow/summary content.
- Do not add or edit unit tests because project instructions forbid changes in `*.Test` projects.
- Do not run migrations. Tuấn will create/run migrations after code changes.

## Order Status

`OrderStatus` will become:

- `Pending = 0`: order is active and not yet completed or cancelled.
- `Completed = 1`: order has been reviewed and closed by the user.
- `Cancelled = 2`: order has been cancelled.

Existing code names should follow the new business language:

- `LockOrder` -> `CompleteOrder`
- `CanLockOrder` -> `CanCompleteOrder`
- `LockOrderModel` -> `CompleteOrderModel`
- UI text "Khóa đơn" -> "Hoàn thành đơn"

Completion rules:

- User can complete only when order is not cancelled and delivery is fully delivered.
- Completing sets status to `Completed` and records `CompletedOnUtc`.
- Completed orders are read-only for order item edits, shipping edits, discount edits, and note edits.
- Cancelled orders remain cancellable state, not completed.
- Keep the existing delete eligibility behavior. Do not expand delete eligibility as part of this work.

## Workflow Stage Calculation

The workflow bar has 4 fixed steps:

1. `Đặt hàng`
2. `Chuẩn bị hàng`
3. `Giao hàng`
4. `Kết sổ`

The active stage is computed in this priority order:

1. If order is `Completed`, active stage is `Kết sổ` and stage state is completed.
2. If order is `Cancelled`, active stage remains the latest meaningful stage from data and the workflow shows cancelled state.
3. If delivery status is `Delivered`, active stage is `Kết sổ`.
4. If there are valid delivery notes or delivered quantity is greater than zero, active stage is `Giao hàng`.
5. If the order has shortage, allocated purchase orders not fully received, or direct-ship allocations not fully resolved, active stage is `Chuẩn bị hàng`.
6. Otherwise active stage is `Đặt hàng`.

Clicking a workflow step only changes the visible details panel. It does not mutate data.

When the page loads, it opens the active stage panel by default.

## Overall Delivery Status

Delivery status is derived from non-cancelled delivery notes and quantities:

- `Pending`: no valid delivery note and no delivered quantity.
- `Shipping`: at least one valid delivery note exists, but no quantity has reached customer as delivered.
- `PartialDelivered`: delivered-to-customer quantity is greater than zero but less than ordered quantity.
- `Delivered`: delivered-to-customer quantity is greater than or equal to ordered quantity for every order item.

Use `DeliveryNoteStatus.Delivered` as the signal that goods reached the customer. Draft/confirmed/delivering notes count as shipping progress but not delivered-to-customer quantity.

## Page Layout

The updated order detail page will keep the clean admin dashboard style from `DESIGN.md`.

Top area:

- Page title with order code and order status badge.
- Workflow bar with 4 clickable steps.
- Each step shows icon, label, short status, and a compact metric.

Main area:

- Left/wide column: the selected workflow panel.
- Right/sticky desktop column: full vertical timeline.
- Mobile: workflow scrolls horizontally; selected panel appears first; timeline appears below the panel.

Footer/actions:

- Keep useful actions, but align them to workflow:
  - Back to list.
  - Print order.
  - Cancel order.
  - Delete order.
  - Complete order when eligible.

## Stage Panels

### 1. Đặt hàng

Shows the order as originally placed:

- Customer name, phone, address, and map link.
- Shipping expected date and shipping address.
- Order note.
- Ordered products with product name, quantity, unit price, line total.
- Order subtotal, discount, and total amount.

Existing edit capabilities remain available only when the order can still be updated:

- Add/edit/remove order items.
- Update shipping information.
- Update note.
- Update discount.

### 2. Chuẩn bị hàng

Shows stock readiness per order item:

- Product.
- Ordered quantity.
- Current available stock.
- Shortage quantity.
- Quantity already issued through delivery notes.
- Delivered-to-customer quantity.
- Direct-ship indicator and direct-ship status.
- Cost information when known.
- Related purchase orders.

Actions:

- If shortage exists, show "Nhập hàng thiếu" linking to the existing shortage aggregation flow.
- Related purchase orders appear inline in this panel, with links to purchase order details.
- Purchase order rows show placed date, status, expected delivery date if present, allocated quantity, received quantity, and pending quantity.
- If direct-ship allocations need user confirmation and existing flows require extra information, open the relevant modal instead of silently failing.

### 3. Giao hàng

Shows delivery execution:

- Overall delivery status: `Pending`, `Shipping`, `PartialDelivered`, `Delivered`.
- Delivery notes with code, status, source type, warehouse, direct-ship badge, created date, delivered date, and item quantities.
- Per-item delivered progress.

Actions:

- Create delivery note when remaining quantity exists and order is not completed/cancelled.
- Open delivery note details from each row.
- Use existing delivery-note status update flows where available.

### 4. Kết sổ

Shows order settlement:

- Revenue: subtotal, discount, total amount.
- Customer debts linked by `OrderId`: total debt, paid amount, remaining amount, status, due date, and delivery note link.
- Expenses linked by `Expense.SourceOrderId`: title, reason/description, amount, incurred date.
- Cost of goods by delivery note item using `DeliveryNoteItem.CostAtDispatch`.
- Profit summary:
  - Revenue after discount.
  - Total cost of goods.
  - Total order expenses.
  - Estimated/final profit.

Actions:

- Add order expense.
- Complete order when delivery status is `Delivered` and order is still `Pending`.

If some cost snapshots are missing, the panel must show "Chưa có giá vốn" for those lines and avoid pretending profit is final.

## Timeline

Timeline is a normalized list of order events. It is displayed top-to-bottom in chronological order.

Timeline event fields:

- Time.
- Category.
- Title.
- Description.
- Status/badge.
- Link when the source record has a detail page.

Timeline includes:

- Order created date.
- Purchase order created/placed events related to the order.
- Goods receipt or received quantity events related to allocated purchase orders.
- Direct-ship allocation/received/customer-confirmed events when present.
- Delivery note created events.
- Delivery note delivering/delivered events when dates are available.
- Customer debt created/payment events when available.
- Order expense created events.
- Order completed date.
- Order cancelled date if cancelled.

If an event date does not exist in current data, the timeline should omit that exact event rather than inventing a date.

## Data Model Additions

Order:

- Rename status enum member `Locked` to `Completed`.
- Add `CompletedOnUtc`.
- Remove "lock reason" from order completion UI and view models. If the old database column still exists until migration, new code must not read or write it.

Expense:

- Add nullable `SourceOrderId`.
- Existing return-related source fields remain unchanged.
- Expenses can be global or linked to an order. Only order-linked expenses appear in order settlement.

No migration commands will be run by Codex.

## Query/View Model Additions

Add workflow-focused models under the existing order detail view model boundary.

Suggested model groups:

- `OrderWorkflowModel`
  - current stage
  - stage summaries
  - overall delivery status
- `OrderPreparationModel`
  - per-item stock readiness
  - shortage data
  - related purchase orders
  - direct-ship data
- `OrderDeliveryWorkflowModel`
  - overall delivery status
  - delivery notes
  - per-item shipped/delivered quantities
- `OrderSettlementModel`
  - debts by order
  - expenses by order
  - cost breakdown by delivery note item
  - profit summary
- `OrderTimelineEventModel`
  - normalized timeline event list

Implementation may place these as nested records under `OrderDetailsModel` or as separate model files. The important rule is that Razor should receive prepared display data and avoid complex business calculations inside the view.

## UI Implementation Shape

Use partials to keep `Details.cshtml` readable:

- `_OrderWorkflowBar.cshtml`
- `_OrderWorkflowOrderPanel.cshtml`
- `_OrderWorkflowPreparationPanel.cshtml`
- `_OrderWorkflowDeliveryPanel.cshtml`
- `_OrderWorkflowSettlementPanel.cshtml`
- `_OrderWorkflowTimeline.cshtml`

Use the existing Bootstrap and Bootstrap Icons stack. Add page-specific CSS only where needed, preferably in the existing site CSS/module pattern.

Use JavaScript only for:

- Switching visible workflow panel on step click.
- Opening existing modals/offcanvas.
- Existing AJAX form submissions.

Do not create a new frontend framework.

## Error And Empty States

- Empty stage data should render a quiet empty state, not hide the stage.
- Missing purchase orders: show "Chưa có đơn nhập liên quan."
- Missing delivery notes: show "Chưa có phiếu giao."
- Missing debts: show "Chưa phát sinh công nợ."
- Missing expenses: show "Chưa có chi phí phát sinh."
- Missing cost snapshot: show "Chưa có giá vốn" and mark profit as provisional.

## Verification

After implementation, verify:

- `dotnet build` passes.
- Existing order detail page renders.
- Workflow defaults to the computed active stage.
- Clicking workflow steps switches panels without changing data.
- Completed orders display as completed instead of locked.
- Delivery status summary matches delivery note data.
- Settlement math uses order expenses and `CostAtDispatch`.

Do not add or edit unit tests because the project instructions explicitly forbid that.

## Out Of Scope

- Running EF migrations.
- Adding tests under `*.Test` projects.
- Rebuilding the order module as a new workflow subsystem.
- Creating a second order detail route.
- Replacing MVC/Razor with a frontend framework.

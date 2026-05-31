# Order Workflow Improvements Plan

## Assumptions

- Cancelled/rejected delivery notes remain visible in Delivery and Timeline for audit history.
- Cancelled/rejected delivery notes are not counted in issued, delivered, or settlement totals.
- Delivery quick actions reuse the existing `DeliveryNoteController` endpoints and modals from the delivery note detail page.
- This change is presentation-focused. No domain status transition rules or database schema changes are required.

## Success Criteria

- Preparation table only shows `Hang hoa`, `SL dat`, `Ton kho`, `SL thieu`.
- Delivery section shows an order-item delivery summary table with `Hang hoa`, `SL dat`, `Ton kho`, `Da xuat`, `Da giao`, `Giao thang`.
- Delivery section shows linked customer returns, additional return costs, refund amount, receiver, proof image, surcharge, and rejection/cancel reasons where available.
- Delivery note rows have quick controls for confirm/export, delivering, delivered, reject/cancel matching the delivery note detail workflow.
- Timeline includes delivery confirmed/exported, delivering, delivered, rejected/cancelled, and customer return events.
- Settlement shows return financials: returned goods value, additional return cost, net customer refund, and compensation quantity.
- Build verification covers the modified Web, Web.Contracts, and Web.Framework projects.

## TodoList

- [x] Extend `OrderDetailsModel` with delivery summary rows, return rows, delivery note detail fields, and settlement return totals.
- [x] Update `OrderModelFactory` to load customer returns by delivery note, proof pictures, delivery note statuses/details, and return financial aggregates.
- [x] Simplify `_OrderWorkflowPreparationPanel.cshtml`.
- [x] Expand `_OrderWorkflowDeliveryPanel.cshtml` with the delivery item table, returns table, quick actions, and delivery journey modal.
- [x] Include existing delivery/reject modal partials in `Details.cshtml`.
- [x] Extend `OrderDetails.js` to wire quick delivery actions using `DeliveryNoteController.js`.
- [x] Update `_OrderWorkflowTimeline.cshtml` data via factory-generated timeline events.
- [x] Update `_OrderWorkflowSettlementPanel.cshtml` with return financial metrics and tables.
- [x] Add narrowly scoped CSS only if existing utility classes cannot express the layout cleanly.
- [x] Verify with targeted project builds.

## Verification Plan

- `rtk dotnet build NamEcommerce\Presentation\NamEcommerce.Web.Contracts\NamEcommerce.Web.Contracts.csproj`
- `rtk dotnet build NamEcommerce\Presentation\NamEcommerce.Web.Framework\NamEcommerce.Web.Framework.csproj`
- `rtk dotnet build NamEcommerce\Presentation\NamEcommerce.Web\NamEcommerce.Web.csproj`

The full solution build is not the baseline command here because the solution currently fails on `NamEcommerce.Customer.Client` website project requiring the .NET Framework ASP.NET compiler.

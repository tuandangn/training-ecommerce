# Order Details Audit Plan

## Assumptions

- Order item changes need durable history, so the timeline will read from a new `OrderItemChangeAudit` table instead of inferring from current order state.
- The audit captures add, update, and remove actions, including product snapshot, old/new quantity, and old/new unit price.
- Database migration and database update are not run by the agent; the user will run them after reviewing the code.

## Steps

1. Add a domain audit entity and EF mapping for order item changes.
2. Extend order item domain events with product name and old unit price where needed.
3. Add a domain manager and application service to record/read audit entries.
4. Add event handlers for `OrderItemAdded`, `OrderItemUpdated`, and `OrderItemRemoved`.
5. Use audit entries in `Order/Details` timeline.
6. Update Order Details UI:
   - add `Giao đủ` in preparation;
   - hide stock unless the item is not fully delivered and still short;
   - remove goods value from delivery table;
   - add returned quantity in delivery notes table.
7. Update Delivery Note Details UI to show receiver and surcharge information clearly.
8. Verify with project build.

## Verification

- Build: `rtk dotnet build NamEcommerce\Presentation\NamEcommerce.Web\NamEcommerce.Web.csproj`
- Manual DB work after code: create EF migration and update database.

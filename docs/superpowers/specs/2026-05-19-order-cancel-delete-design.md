# Order Cancel and Delete

## Context

Order already has backend commands and controller actions for cancel and delete. The details page has cancel modals and JavaScript that expect a `btnCancelOrder` button, but that button is not rendered. Delete has a controller action, but the UI does not expose a confirmation flow.

## Behavior

- Show `Huy don` on the order details page when `CanCancelOrder` is true.
- Show `Xoa don` on the order details page when the order is `Pending` or `Cancelled`.
- Cancelling keeps the existing modal behavior, including the direct-ship return warehouse warning when needed.
- Deleting uses a confirmation modal and posts to `Order/Delete`.
- Backend delete must allow `Pending` and `Cancelled` orders, while still rejecting orders with active delivery notes or other processing constraints.
- Deleting an already `Cancelled` order must not release product reservations a second time.
- Locked orders are not deletable.

## Implementation Notes

- Add a delete availability property to the order details model.
- Populate the property in `OrderModelFactory` from order status.
- Adjust `DeleteOrderAsync` to allow `Cancelled` orders instead of relying only on `CanUpdateInfo`.
- Adjust order delete domain event payload so `Cancelled` orders do not double-release reservations.
- Add detail page buttons and a small confirmation form/modal following existing Bootstrap patterns.
- Keep changes scoped to Order files. Do not add or edit test projects.

## Verification

- Run `rtk dotnet build NamEcommerce/NamEcommerce.sln`.
- Manually inspect the details page conditions in Razor for Pending, Cancelled, and Locked orders.

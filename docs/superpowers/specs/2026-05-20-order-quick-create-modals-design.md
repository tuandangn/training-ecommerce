# Order Quick Create Modals Design

Date: 2026-05-20

## Goal

Let users create a new customer or product without leaving the order creation page. After a quick create succeeds, the new record is selected immediately so the user can continue building the order.

## Current Context

The order creation page is an ASP.NET Core MVC Razor view at `NamEcommerce/Presentation/NamEcommerce.Web/Views/Order/Create.cshtml`. It already uses JavaScript modules for `CustomerPicker`, `ProductPicker`, and `ProductBrowser`.

Customer and product creation already exist through MVC controllers and MediatR commands:

- `CreateCustomerCommand`
- `CreateProductCommand`

Search and pick endpoints already exist:

- `Customer/Search`
- `Customer/PickItem`
- `Product/Search`
- `Product/PickItem`

## Decisions

- Keep the user on `Order/Create`.
- Add Bootstrap quick-create modals instead of redirecting to full create pages.
- Add JSON endpoints on existing controllers:
  - `Customer/QuickCreate`
  - `Product/QuickCreate`
- Reuse existing commands and validators.
- Do not add or edit unit tests because project instructions forbid changes in `*.Test` projects.
- Do not run migrations.

## Customer Quick Create

The customer picker area gets a small `+` button next to the label. The modal collects:

- Full name
- Phone number
- Address
- Email
- Note

Required fields follow the existing customer validator: full name, phone number, and address.

On success, the server returns the created customer payload in the same shape as `Customer/PickItem`:

```json
{
  "success": true,
  "customer": {
    "id": "...",
    "name": "...",
    "phone": "...",
    "address": "..."
  }
}
```

The client selects the customer in `CustomerPicker`, closes the modal, clears the form, and copies the customer address to the shipping address when that field is still using the customer address.

## Product Quick Create

The product area gets a `+ Tạo hàng hóa` action near the product browser and the add-product modal.

The modal collects:

- Product name
- Unit measurement
- Category
- Vendors
- Default sale price

Unit, category, vendors, and default sale price are optional, but vendors are shown because order item selection currently accepts products with stock or at least one vendor.

On success, the server returns the created product payload in the same shape as `Product/PickItem`:

```json
{
  "success": true,
  "product": {
    "id": "...",
    "name": "...",
    "unitPrice": 0,
    "availableQty": 0,
    "vendorCount": 1
  }
}
```

The client adds the product to the order using the existing order item flow. If the quick-create was launched from the add-product modal, it also selects the product inside `ProductPicker`.

## UI Shape

Use existing Bootstrap modals, Bootstrap Icons, form controls, and `content-card` styling from `DESIGN.md`.

No new frontend framework is introduced. JavaScript remains in `wwwroot/modules/OrderController.js`.

## Error Handling

- Invalid form data returns JSON `{ success: false, message: "..." }`.
- Server command failures are localized using existing `LocalizeError`.
- Client shows existing `toast` notifications.
- Successful creation does not submit the order form.

## Verification

Verify with:

- `rtk dotnet build NamEcommerce\NamEcommerce.sln`
- Manual browser check if the app can run locally:
  - create a customer from `Order/Create`, verify it is selected and shipping address fills.
  - create a product from `Order/Create`, verify it is added to the order item table.
  - submit validation errors from empty quick-create forms.

## Out Of Scope

- Full product image upload inside the quick modal.
- Initial stock entry inside the quick modal.
- Creating categories, units, or vendors inside the quick modal.
- Unit tests or migration commands.

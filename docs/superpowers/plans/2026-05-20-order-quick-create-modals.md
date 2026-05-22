# Order Quick Create Modals Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add quick-create customer and product modals to the order creation page, selecting the new record immediately after creation.

**Architecture:** Keep the existing MVC/Razor and JavaScript module structure. Add small JSON endpoints to the existing customer and product controllers, extend the order create model with product option lists, render modals in `Order/Create.cshtml`, and wire submit/select behavior in `OrderController.js`.

**Tech Stack:** ASP.NET Core MVC, Razor, MediatR, Bootstrap, vanilla JavaScript modules.

---

## Scope Rules

- Do not add or edit files under any `*.Test` project.
- Do not run EF migration commands.
- Reuse existing `CreateCustomerCommand`, `CreateProductCommand`, `Customer/PickItem`, and `Product/PickItem` behavior.
- Keep quick modals compact and order-page specific.

## File Map

- Modify `NamEcommerce/Presentation/NamEcommerce.Web/Models/Orders/CreateOrderModel.cs`: add option lists for quick product fields.
- Modify `NamEcommerce/Presentation/NamEcommerce.Web/Services/Orders/OrderModelFactory.cs`: populate category, unit, and vendor options.
- Modify `NamEcommerce/Presentation/NamEcommerce.Web/Controllers/CustomerController.cs`: add `QuickCreate`.
- Modify `NamEcommerce/Presentation/NamEcommerce.Web/Controllers/ProductController.cs`: add `QuickCreate`.
- Modify `NamEcommerce/Presentation/NamEcommerce.Web.Contracts/Commands/Models/Catalog/CreateProductCommand.cs`: add optional `UnitPrice`.
- Modify `NamEcommerce/Presentation/NamEcommerce.Web.Framework/Commands/Handlers/Catalog/CreateProductHandler.cs`: pass optional initial product price into create flow.
- Modify `NamEcommerce/Presentation/NamEcommerce.Web/Views/Order/Create.cshtml`: render quick-create buttons and modals.
- Modify `NamEcommerce/Presentation/NamEcommerce.Web/wwwroot/modules/OrderController.js`: submit quick forms and select new records.

## Task 1: Prepare Order Create Data

- [ ] Add `AvailableCategories`, `AvailableUnitMeasurements`, and `AvailableVendors` to `CreateOrderModel`.
- [ ] Populate those lists in `OrderModelFactory.PrepareCreateOrderModel`.
- [ ] Verify `Order/Create` still renders with empty and posted models.

## Task 2: Add JSON Quick Create Endpoints

- [ ] Add `CustomerController.QuickCreate(CreateCustomerModel model)` returning JSON success plus created customer pick payload.
- [ ] Add `ProductController.QuickCreate(CreateProductModel model)` returning JSON success plus created product pick payload.
- [ ] Keep validation errors in JSON instead of returning full views.

## Task 3: Allow Initial Product Sale Price

- [ ] Add nullable `UnitPrice` to `CreateProductCommand`.
- [ ] Add initial price fields to the create product DTO flow and persist them during product creation.
- [ ] In `CreateProductHandler`, pass `UnitPrice` into `CreateProductAppDto` when it has a value.
- [ ] Ensure the response preserves `CreatedId`.

## Task 4: Render Quick Create UI

- [ ] Add customer quick-create button beside the customer label.
- [ ] Add product quick-create button near product browser and add-product modal.
- [ ] Add two Bootstrap modals with antiforgery-compatible forms.
- [ ] Keep fields compact and aligned with `DESIGN.md`.

## Task 5: Wire JavaScript

- [ ] Import `apiPost` in `OrderController.js`.
- [ ] Submit quick customer form with `FormData`.
- [ ] On customer success, call `CustomerPicker.displayCustomer`, update order state, close modal, and clear form.
- [ ] Submit quick product form with `FormData`.
- [ ] On product success, add the product to the order through the existing `#addOrIncrementItem` path, close modal, clear form, and refresh visible product browser results by search focus when possible.
- [ ] Show existing toast messages for errors and success.

## Task 6: Verify

- [ ] Run `rtk dotnet build NamEcommerce\NamEcommerce.sln`.
- [ ] Search touched files for stale typos or broken namespace paths.
- [ ] If local app launch is available, manually verify quick customer and product creation on `Order/Create`.

# Order Delivery Visibility Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make Order list and detail screens show ordered items, delivery-note quantities, and delivery-note status clearly.

**Architecture:** Reuse existing Order query/model factory flow. Compute delivery-note coverage from non-cancelled delivery notes, expose compact summary models, and render the details through an Order offcanvas partial.

**Tech Stack:** ASP.NET Core MVC, Razor views, MediatR query handler, Bootstrap popover/offcanvas.

---

### Task 1: Order List Summary

**Files:**
- Modify: `NamEcommerce/Presentation/NamEcommerce.Web.Contracts/Models/Orders/OrderListModel.cs`
- Modify: `NamEcommerce/Presentation/NamEcommerce.Web.Framework/Queries/Handlers/Orders/GetOrderListHandler.cs`
- Modify: `NamEcommerce/Presentation/NamEcommerce.Web/Views/Order/List.cshtml`

- [ ] Add item summary fields for ordered, delivery-note, and delivered quantities.
- [ ] Fetch delivery notes per order in the list query handler and map per-item quantities.
- [ ] Add a compact popover column based on the existing PurchaseOrder list pattern.
- [ ] Verify the web project builds.

### Task 2: Order Details And Offcanvas

**Files:**
- Modify: `NamEcommerce/Presentation/NamEcommerce.Web/Models/Orders/OrderDetailsModel.cs`
- Modify: `NamEcommerce/Presentation/NamEcommerce.Web/Services/Orders/OrderModelFactory.cs`
- Modify: `NamEcommerce/Presentation/NamEcommerce.Web/Controllers/OrderController.cs`
- Modify: `NamEcommerce/Presentation/NamEcommerce.Web/Views/Order/Details.cshtml`
- Create: `NamEcommerce/Presentation/NamEcommerce.Web/Views/Order/_DeliveryNotesOffcanvasBody.cshtml`

- [ ] Enrich order detail delivery-note data with status, source, dates, warehouse, and item product names.
- [ ] Replace the direct-ship table card with a `Phiếu giao` offcanvas button.
- [ ] Add a delivery progress column to the order item table.
- [ ] Render all delivery notes and direct-ship rows inside a single offcanvas.
- [ ] Verify the web project builds.

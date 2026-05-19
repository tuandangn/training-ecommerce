# Direct Ship Return Warehouse Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Let users choose the physical warehouse that receives stock when a direct-ship delivery is rejected or a sales order with received direct-ship stock is cancelled.

**Architecture:** Carry `ReturnWarehouseId` from MVC request models through MediatR commands, application DTOs, and `IDirectShipManager`. `DirectShipManager` continues to own the stock transfer, but uses the user-selected physical warehouse instead of resolving the original goods receipt warehouse. UI uses the existing warehouse app service to show active physical warehouses only.

**Tech Stack:** ASP.NET Core MVC, MediatR, application services, domain services, EF-backed repository/data reader, Bootstrap/Razor, vanilla JavaScript.

---

### Task 1: Extend Contracts

**Files:**
- Modify: `NamEcommerce/Application/NamEcommerce.Application.Contracts/Dtos/PurchaseOrders/DirectShipAppDtos.cs`
- Modify: `NamEcommerce/Application/NamEcommerce.Application.Contracts/PurchaseOrders/IDirectShipAppService.cs`
- Modify: `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Services/PurchaseOrders/IDirectShipManager.cs`
- Modify: `NamEcommerce/Application/NamEcommerce.Application.Contracts/Dtos/Orders/OrderAppDtos.cs`
- Modify: `NamEcommerce/Presentation/NamEcommerce.Web.Contracts/Commands/Models/PurchaseOrders/DirectShipDeliveryCommands.cs`
- Modify: `NamEcommerce/Presentation/NamEcommerce.Web.Contracts/Commands/Models/Orders/CancelOrderCommand.cs`

- [ ] Add `ReturnWarehouseId` to reject/cancel DTOs and commands.
- [ ] Require the field in direct-ship return paths.

### Task 2: Update Application and Domain Flow

**Files:**
- Modify: `NamEcommerce/Application/NamEcommerce.Application.Services/PurchaseOrders/DirectShipAppService.cs`
- Modify: `NamEcommerce/Application/NamEcommerce.Application.Services/Orders/OrderAppService.cs`
- Modify: `NamEcommerce/Presentation/NamEcommerce.Web.Framework/Commands/Handlers/PurchaseOrders/DirectShipDeliveryCommandHandlers.cs`
- Modify: `NamEcommerce/Presentation/NamEcommerce.Web.Framework/Commands/Handlers/Orders/CancelOrderHandler.cs`
- Modify: `NamEcommerce/Domain/NamEcommerce.Domain.Services/PurchaseOrders/DirectShipManager.cs`

- [ ] Pass `ReturnWarehouseId` through each layer.
- [ ] Validate selected warehouse exists, is active, and has `WarehouseType.Physical`.
- [ ] Transfer each direct-ship delivery item from Direct-Ship Transit to the selected warehouse.

### Task 3: Populate UI Warehouse Options

**Files:**
- Modify: `NamEcommerce/Presentation/NamEcommerce.Web/Controllers/DirectShipDeliveryController.cs`
- Modify: `NamEcommerce/Presentation/NamEcommerce.Web/Models/DirectShipDelivery/DirectShipDeliveryModels.cs`
- Modify: `NamEcommerce/Presentation/NamEcommerce.Web/Services/Orders/OrderModelFactory.cs`
- Modify: `NamEcommerce/Presentation/NamEcommerce.Web/Models/Orders/OrderDetailsModel.cs`

- [ ] Query warehouses via `IWarehouseAppService.GetWarehousesAsync`.
- [ ] Filter to `IsActive && WarehouseType == WarehouseType.Physical`.
- [ ] Add options to pending direct-ship list model and order details model.

### Task 4: Update Razor and JavaScript

**Files:**
- Modify: `NamEcommerce/Presentation/NamEcommerce.Web/Views/DirectShipDelivery/Pending.cshtml`
- Modify: `NamEcommerce/Presentation/NamEcommerce.Web/Views/Order/Details.cshtml`
- Modify: `NamEcommerce/Presentation/NamEcommerce.Web/Models/Orders/CancelOrderModel.cs`
- Modify: `NamEcommerce/Presentation/NamEcommerce.Web/Controllers/OrderController.cs`

- [ ] Add required warehouse dropdown before reason text in Reject modal.
- [ ] Add required warehouse dropdown in cancel warning modal.
- [ ] Block submit with inline validation if no warehouse is selected.
- [ ] Send `returnWarehouseId` in JSON payload.

### Task 5: Verify

**Files:**
- No test project edits, per `AGENTS.md`.

- [ ] Run `rtk dotnet build NamEcommerce/NamEcommerce.sln`.
- [ ] Manual smoke: reject direct-ship DN with selected warehouse, cancel SO with selected warehouse, and verify payload reaches domain transfer call.

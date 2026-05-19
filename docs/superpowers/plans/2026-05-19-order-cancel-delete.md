# Order Cancel Delete Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Expose cancel and delete actions for sales orders, with delete allowed for Pending and Cancelled orders.

**Architecture:** Keep existing MediatR command flow. Add UI buttons and confirmation modal in the order details Razor page, and update application-level delete eligibility so Cancelled orders can be deleted while active delivery-note constraints remain enforced.

**Tech Stack:** ASP.NET Core MVC/Razor, MediatR, Bootstrap, .NET solution build.

---

## File Structure

- Modify `NamEcommerce/Application/NamEcommerce.Application.Services/Orders/OrderAppService.cs`: allow delete when status is Pending or Cancelled, still reject active delivery notes.
- Modify `NamEcommerce/Domain/NamEcommerce.Domain/Entities/Orders/Order.cs`: avoid double reservation release when deleting a Cancelled order.
- Modify `NamEcommerce/Domain/NamEcommerce.Domain.Services/Orders/OrderManager.cs`: apply the same Pending or Cancelled delete rule at the domain layer.
- Modify `NamEcommerce/Presentation/NamEcommerce.Web/Models/Orders/OrderDetailsModel.cs`: add `CanDeleteOrder`.
- Modify `NamEcommerce/Presentation/NamEcommerce.Web/Services/Orders/OrderModelFactory.cs`: populate `CanDeleteOrder`.
- Modify `NamEcommerce/Presentation/NamEcommerce.Web/Views/Order/Details.cshtml`: add cancel button, delete button, and delete confirmation modal.
- Verify with `rtk dotnet build NamEcommerce/NamEcommerce.sln`.

### Task 1: Backend Delete Eligibility

**Files:**
- Modify: `NamEcommerce/Application/NamEcommerce.Application.Services/Orders/OrderAppService.cs`
- Modify: `NamEcommerce/Domain/NamEcommerce.Domain/Entities/Orders/Order.cs`
- Modify: `NamEcommerce/Domain/NamEcommerce.Domain.Services/Orders/OrderManager.cs`

- [ ] **Step 1: Inspect existing delete branch**

Run:

```powershell
rtk powershell -NoProfile -Command "Get-Content NamEcommerce/Application/NamEcommerce.Application.Services/Orders/OrderAppService.cs | Select-Object -Skip 560 -First 45"
```

Expected: Shows `DeleteOrderAsync` currently rejecting when `!order.CanUpdateInfo`.

- [ ] **Step 2: Replace status rule**

Change the `!order.CanUpdateInfo` check to reject only when the order status is neither `Pending` nor `Cancelled`:

```csharp
var canDeleteOrder = order.Status is OrderStatus.Pending or OrderStatus.Cancelled;
if (!canDeleteOrder)
{
    return new DeleteOrderResultAppDto
    {
        Success = false,
        ErrorMessage = "Error.OrderCannotDelete"
    };
}
```

Expected: Cancelled orders reach the existing active-delivery-note check.

- [ ] **Step 3: Update domain delete rule**

In `OrderManager.DeleteOrderAsync`, change the `!order.CanUpdateInfo()` guard to:

```csharp
var canDeleteOrder = order.OrderStatus is OrderStatus.Pending or OrderStatus.Cancelled;
if (!canDeleteOrder)
    throw new InvalidOperationException("Order cannot delete.");
```

Expected: Domain deletion no longer rejects Cancelled orders before checking active delivery notes.

- [ ] **Step 4: Avoid double reservation release for Cancelled delete**

In `Order.MarkDeleted`, use an empty reservation item collection when the order is already Cancelled:

```csharp
internal void MarkDeleted()
{
    // Cancelled orders already released reservation when the order was cancelled.
    IReadOnlyCollection<OrderReservationItem> reservationItems = OrderStatus == OrderStatus.Cancelled
        ? []
        : GetReservationItems();

    RaiseDomainEvent(new OrderDeleted(Id, Code, reservationItems));
}
```

Expected: Pending delete still releases reservation; Cancelled delete does not release the same reservation twice.

### Task 2: Details Model Flag

**Files:**
- Modify: `NamEcommerce/Presentation/NamEcommerce.Web/Models/Orders/OrderDetailsModel.cs`
- Modify: `NamEcommerce/Presentation/NamEcommerce.Web/Services/Orders/OrderModelFactory.cs`

- [ ] **Step 1: Add `CanDeleteOrder` to details model**

In `OrderDetailsModel`, add the property next to the other action flags:

```csharp
public bool CanDeleteOrder { get; set; }
```

- [ ] **Step 2: Populate the flag**

In `PrepareOrderDetailsModel`, after `CanCancelOrder` is assigned, add:

```csharp
model.CanDeleteOrder = order.Status is (int)OrderStatus.Pending or (int)OrderStatus.Cancelled;
```

Expected: Razor can show delete only for Pending and Cancelled.

### Task 3: Details Page Actions

**Files:**
- Modify: `NamEcommerce/Presentation/NamEcommerce.Web/Views/Order/Details.cshtml`

- [ ] **Step 1: Add cancel button**

In the top action group near Print and Lock, add this before the lock button:

```cshtml
@if (Model.CanCancelOrder)
{
    <button type="button" class="btn btn-outline-danger" id="btnCancelOrder">
        <i class="bi bi-x-circle me-1"></i>
        Huy don
    </button>
}
```

Expected: The existing cancel JavaScript finds `btnCancelOrder`.

- [ ] **Step 2: Add delete button**

Add this in the same action group:

```cshtml
@if (Model.CanDeleteOrder)
{
    <button type="button" class="btn btn-danger" data-bs-toggle="modal" data-bs-target="#deleteOrderModal">
        <i class="bi bi-trash me-1"></i>
        Xoa don
    </button>
}
```

Expected: Delete is visible for Pending and Cancelled orders.

- [ ] **Step 3: Add delete confirmation modal**

After the cancel modal block, add:

```cshtml
@if (Model.CanDeleteOrder)
{
    <div class="modal fade" id="deleteOrderModal" tabindex="-1" aria-hidden="true">
        <div class="modal-dialog modal-dialog-centered">
            <div class="modal-content border-0 shadow rounded-4">
                <div class="modal-header border-0 pb-0">
                    <h5 class="modal-title fw-bold text-danger">Xac nhan xoa don hang</h5>
                    <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                </div>
                <div class="modal-body py-3">
                    <p>Xac nhan xoa don hang <strong>@Model.Code</strong>? Thao tac nay khong the hoan tac.</p>
                </div>
                <form method="post" asp-action="Delete" asp-route-id="@Model.Id">
                    <div class="modal-footer border-0 pt-0 pb-3">
                        <button type="button" class="btn btn-light" data-bs-dismiss="modal">Giu nguyen</button>
                        <button type="submit" class="btn btn-danger">
                            <i class="bi bi-trash me-1"></i>
                            Xoa don
                        </button>
                    </div>
                </form>
            </div>
        </div>
    </div>
}
```

Expected: POSTs to `Order/Delete` with the route `id`.

### Task 4: Verification

**Files:**
- No file edits.

- [ ] **Step 1: Build solution**

Run:

```powershell
rtk dotnet build NamEcommerce/NamEcommerce.sln
```

Expected: Build succeeds.

- [ ] **Step 2: Inspect final diff**

Run:

```powershell
rtk git diff -- NamEcommerce/Application/NamEcommerce.Application.Services/Orders/OrderAppService.cs NamEcommerce/Presentation/NamEcommerce.Web/Models/Orders/OrderDetailsModel.cs NamEcommerce/Presentation/NamEcommerce.Web/Services/Orders/OrderModelFactory.cs NamEcommerce/Presentation/NamEcommerce.Web/Views/Order/Details.cshtml
```

Expected: Diff only contains scoped cancel/delete changes.

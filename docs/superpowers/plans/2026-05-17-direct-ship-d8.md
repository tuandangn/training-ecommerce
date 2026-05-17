# Direct-Ship D8 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Complete D8 of the Direct-Ship workflow — PO Details allocation address editing with audit trail and re-print banner (D8.1–D8.3), GR oversupply detection with 3-choice modal (D8.4), and backend acceptance of oversupply to main warehouse (D8.5).

**Architecture:** MVC + MediatR + domain event pattern. Domain events are raised on aggregate root via `RaiseDomainEvent(IDomainEvent)` inside entity methods — dispatched automatically after `SaveChanges`. No injected event publisher needed.

**Tech Stack:** ASP.NET Core MVC (C#), EF Core, Bootstrap 5, vanilla JS (no framework).

---

## Key Facts (verified from codebase)

- `PurchaseOrderItemAllocation` entity: `internal void UpdateDirectShipInfo(string address, string? contactName, string? contactPhone)` — already exists but does NOT raise a domain event; fix needed.
- `DirectShipAddressUpdated(Guid AllocationId, string OldAddress, string NewAddress, Guid EditedByUserId)` — domain event record already defined in `NamEcommerce.Domain.Shared.Events.PurchaseOrders.DirectShipEvents`.
- `VendorOversupplyAccepted(Guid PurchaseOrderItemId, Guid WarehouseId, decimal OversupplyQuantity, decimal UnitCost)` — domain event record already defined.
- `IDirectShipAppService.UpdateDirectShipAddressAsync(UpdateDirectShipAddressAppDto dto)` — already exists; `UpdateDirectShipAddressAppDto` has `AllocationId`, `NewAddress`, `NewContactName`, `NewContactPhone`, `Reason`.
- `ReceivePurchaseOrderItemCommand` in `NamEcommerce.Web.Contracts.Commands.Models.PurchaseOrders` — fields: `PurchaseOrderId`, `PurchaseOrderItemId`, `WarehouseId`, `ReceivedQuantity`, `SellingPrice`.
- `ReceivePurchaseOrderItemHandler` calls `_purchaseOrderAppService.ReceiveItemAsync(ReceivedGoodsForItemAppDto)`.
- `ReceivedGoodsForItemAppDto(Guid PurchaseOrderId, Guid PurchaseOrderItemId)` — has `ReceivedQuantity`, `WarehouseId`, `ReceivedByUserId`, `SellingPrice`.
- `PurchaseOrderAppService.ReceiveItemAsync` line 372: `if (purchaseOrderItem.QuantityReceived + dto.ReceivedQuantity > purchaseOrderItem.QuantityOrdered) return error` — this guard blocks oversupply; for D8.5 we need to bypass it when `OversupplyAction == AcceptToMainWarehouse`.
- `DirectShipDeliveryController` uses `return Json(new { success, message })` pattern — reuse same for the address update endpoint.
- `PurchaseOrderModelFactory.PreparePurchaseOrderDetailsModel` populates `PurchaseOrderDetailsModel`; injecting `IDirectShipAppService` there is the right place to fetch direct-ship allocations per PO item.
- `PurchaseOrderItemAllocation` has `PurchaseOrderItemId` property — enables querying allocations by PO item.
- `IDirectShipManager.GetDirectShipAllocationsForOrderItemsAsync(IReadOnlyList<Guid> orderItemIds)` filters by `a.OrderItemId` — **NOT** `PurchaseOrderItemId`. For D8 we need a separate method filtering by `PurchaseOrderItemId`.

---

## Task 1: D8.1–D8.3 — PO Details direct-ship allocation sub-rows + address edit modal

**Goal:** Show direct-ship allocation rows under each PO item that has `IsDirectShip` allocations; let staff edit address/contact; fix `UpdateDirectShipAddressAsync` to raise the domain event; show banner after save.

**Verify:** Build passes. On PO Details page with a direct-ship PO, each direct-ship item row has a sub-row with address text and "Sửa địa chỉ giao" button. Clicking opens the modal, submitting calls the AJAX endpoint, and a success banner appears.

### Step 1.1 — Add `UpdateDirectShipInfo` domain event to entity

File: `NamEcommerce/Domain/NamEcommerce.Domain/Entities/PurchaseOrders/PurchaseOrderItemAllocation.cs`

Replace the existing `UpdateDirectShipInfo` method to raise the event:

```csharp
internal void UpdateDirectShipInfo(string address, string? contactName, string? contactPhone)
{
    if (string.IsNullOrWhiteSpace(address))
        throw new PurchaseOrderItemDataIsInvalidException("Error.DirectShipAddressRequired");

    var oldAddress = DirectShipAddress ?? string.Empty;
    DirectShipAddress = address;
    DirectShipContactName = contactName;
    DirectShipContactPhone = contactPhone;

    RaiseDomainEvent(new DirectShipAddressUpdated(Id, oldAddress, address, Guid.Empty));
}
```

**Note:** `EditedByUserId` is not available on the entity at mutation time — the pattern used elsewhere (e.g., `DirectShipManager`) stores `editedByUserId` externally. The domain event will be raised with `Guid.Empty` here, then `DirectShipManager.UpdateDirectShipAddressAsync` will need to set the correct userId on the event. However, since the event is built inline in the entity, we instead pass `editedByUserId` into `UpdateDirectShipInfo`:

Change the method signature to accept `editedByUserId`:

```csharp
internal void UpdateDirectShipInfo(string address, string? contactName, string? contactPhone, Guid editedByUserId)
{
    if (string.IsNullOrWhiteSpace(address))
        throw new PurchaseOrderItemDataIsInvalidException("Error.DirectShipAddressRequired");

    var oldAddress = DirectShipAddress ?? string.Empty;
    DirectShipAddress = address;
    DirectShipContactName = contactName;
    DirectShipContactPhone = contactPhone;

    RaiseDomainEvent(new DirectShipAddressUpdated(Id, oldAddress, address, editedByUserId));
}
```

Required using:
```csharp
using NamEcommerce.Domain.Shared.Events.PurchaseOrders;
```

- [ ] Edit `PurchaseOrderItemAllocation.cs` — update `UpdateDirectShipInfo` signature to `(string address, string? contactName, string? contactPhone, Guid editedByUserId)`, read `oldAddress` before mutation, call `RaiseDomainEvent(new DirectShipAddressUpdated(Id, oldAddress, address, editedByUserId))`.

### Step 1.2 — Update `DirectShipManager.UpdateDirectShipAddressAsync` to use the new signature

File: `NamEcommerce/Domain/NamEcommerce.Domain.Services/PurchaseOrders/DirectShipManager.cs`

The current call at line 119:
```csharp
allocation.UpdateDirectShipInfo(newAddress, newContactName, newContactPhone);
```

Replace with:
```csharp
allocation.UpdateDirectShipInfo(newAddress, newContactName, newContactPhone, editedByUserId);
```

- [ ] Edit `DirectShipManager.cs` line 119 — pass `editedByUserId` as fourth argument.

### Step 1.3 — Add `GetDirectShipAllocationsForPoItemsAsync` to domain manager

The existing `GetDirectShipAllocationsForOrderItemsAsync` filters by `a.OrderItemId`. We need filtering by `a.PurchaseOrderItemId`.

**Add to `IDirectShipManager`** in `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Services/PurchaseOrders/IDirectShipManager.cs`:

```csharp
Task<IList<DirectShipAllocationForPoItemDto>> GetDirectShipAllocationsForPoItemsAsync(
    IReadOnlyList<Guid> purchaseOrderItemIds,
    CancellationToken ct = default);
```

**Add new DTO** in `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Dtos/PurchaseOrders/DirectShipAllocationDtos.cs` (append to existing file):

```csharp
[Serializable]
public sealed record DirectShipAllocationForPoItemDto
{
    public Guid AllocationId { get; init; }
    public Guid PurchaseOrderItemId { get; init; }
    public string DirectShipAddress { get; init; } = string.Empty;
    public string? DirectShipContactName { get; init; }
    public string? DirectShipContactPhone { get; init; }
    public decimal AllocatedQuantity { get; init; }
    public int Status { get; init; }
}
```

**Implement in `DirectShipManager`** in `NamEcommerce/Domain/NamEcommerce.Domain.Services/PurchaseOrders/DirectShipManager.cs` (append after the existing `GetDirectShipAllocationsForOrderItemsAsync` method):

```csharp
public Task<IList<DirectShipAllocationForPoItemDto>> GetDirectShipAllocationsForPoItemsAsync(
    IReadOnlyList<Guid> purchaseOrderItemIds, CancellationToken ct = default)
{
    IList<DirectShipAllocationForPoItemDto> results = allocationReader.DataSource
        .Where(a => a.IsDirectShip && purchaseOrderItemIds.Contains(a.PurchaseOrderItemId))
        .Select(a => new DirectShipAllocationForPoItemDto
        {
            AllocationId = a.Id,
            PurchaseOrderItemId = a.PurchaseOrderItemId,
            DirectShipAddress = a.DirectShipAddress ?? string.Empty,
            DirectShipContactName = a.DirectShipContactName,
            DirectShipContactPhone = a.DirectShipContactPhone,
            AllocatedQuantity = a.AllocatedQuantity,
            Status = (int)a.Status
        })
        .ToList();

    return Task.FromResult(results);
}
```

- [ ] Edit `IDirectShipManager.cs` — add the new method signature.
- [ ] Edit `DirectShipAllocationDtos.cs` — append `DirectShipAllocationForPoItemDto` record.
- [ ] Edit `DirectShipManager.cs` — implement `GetDirectShipAllocationsForPoItemsAsync`.

### Step 1.4 — Add `GetDirectShipAllocationsForPoItemsAsync` to app service layer

**Add to `IDirectShipAppService`** in `NamEcommerce/Application/NamEcommerce.Application.Contracts/PurchaseOrders/IDirectShipAppService.cs`:

```csharp
Task<IList<DirectShipAllocationForPoItemAppDto>> GetDirectShipAllocationsForPoItemsAsync(
    IReadOnlyList<Guid> purchaseOrderItemIds);
```

**Add DTO** in `NamEcommerce/Application/NamEcommerce.Application.Contracts/Dtos/PurchaseOrders/DirectShipAllocationAppDtos.cs` (append):

```csharp
[Serializable]
public sealed record DirectShipAllocationForPoItemAppDto
{
    public Guid AllocationId { get; init; }
    public Guid PurchaseOrderItemId { get; init; }
    public string DirectShipAddress { get; init; } = string.Empty;
    public string? DirectShipContactName { get; init; }
    public string? DirectShipContactPhone { get; init; }
    public decimal AllocatedQuantity { get; init; }
    public int Status { get; init; }
}
```

**Implement in `DirectShipAppService`** in `NamEcommerce/Application/NamEcommerce.Application.Services/PurchaseOrders/DirectShipAppService.cs` (append before closing brace):

```csharp
public async Task<IList<DirectShipAllocationForPoItemAppDto>> GetDirectShipAllocationsForPoItemsAsync(
    IReadOnlyList<Guid> purchaseOrderItemIds)
{
    var items = await directShipManager.GetDirectShipAllocationsForPoItemsAsync(purchaseOrderItemIds)
        .ConfigureAwait(false);
    return items.Select(a => new DirectShipAllocationForPoItemAppDto
    {
        AllocationId = a.AllocationId,
        PurchaseOrderItemId = a.PurchaseOrderItemId,
        DirectShipAddress = a.DirectShipAddress,
        DirectShipContactName = a.DirectShipContactName,
        DirectShipContactPhone = a.DirectShipContactPhone,
        AllocatedQuantity = a.AllocatedQuantity,
        Status = a.Status
    }).ToList();
}
```

- [ ] Edit `IDirectShipAppService.cs` — add the new method signature.
- [ ] Edit `DirectShipAllocationAppDtos.cs` — append `DirectShipAllocationForPoItemAppDto` record.
- [ ] Edit `DirectShipAppService.cs` — implement `GetDirectShipAllocationsForPoItemsAsync`.

### Step 1.5 — Add `DirectShipAllocations` dict to `PurchaseOrderDetailsModel`

File: `NamEcommerce/Presentation/NamEcommerce.Web/Models/PurchaseOrders/PurchaseOrderDetailsModel.cs`

Add a nested record and a dictionary property (append before closing brace):

```csharp
[ValidateNever]
public IDictionary<Guid, IList<DirectShipAllocationForPoModel>> DirectShipAllocationsPerItem { get; set; }
    = new Dictionary<Guid, IList<DirectShipAllocationForPoModel>>();

[Serializable]
public sealed record DirectShipAllocationForPoModel
{
    public Guid AllocationId { get; init; }
    public string DirectShipAddress { get; init; } = string.Empty;
    public string? DirectShipContactName { get; init; }
    public string? DirectShipContactPhone { get; init; }
    public decimal AllocatedQuantity { get; init; }
    public int Status { get; init; }
}
```

Required using at top of file:
```csharp
using NamEcommerce.Application.Contracts.Dtos.PurchaseOrders;
```

- [ ] Edit `PurchaseOrderDetailsModel.cs` — add `DirectShipAllocationsPerItem` property and `DirectShipAllocationForPoModel` nested record. Add `[ValidateNever]` attribute (already imported from `Microsoft.AspNetCore.Mvc.ModelBinding.Validation`).

### Step 1.6 — Populate `DirectShipAllocationsPerItem` in the model factory

File: `NamEcommerce/Presentation/NamEcommerce.Web/Services/PurchaseOrders/PurchaseOrderModelFactory.cs`

1. Inject `IDirectShipAppService` via constructor (add to constructor parameter and field):

```csharp
private readonly IDirectShipAppService _directShipAppService;

public PurchaseOrderModelFactory(IMediator mediator, AppConfig appConfig, IDirectShipAppService directShipAppService)
{
    _mediator = mediator;
    _appConfig = appConfig;
    _directShipAppService = directShipAppService;
}
```

2. In `PreparePurchaseOrderDetailsModel`, after building the model and BEFORE the `return model` line, add:

```csharp
var poItemIds = purchaseOrderInfo.Items.Select(i => i.Id).ToList();
if (poItemIds.Count > 0)
{
    var directShipAllocations = await _directShipAppService
        .GetDirectShipAllocationsForPoItemsAsync(poItemIds)
        .ConfigureAwait(false);

    foreach (var alloc in directShipAllocations)
    {
        if (!model.DirectShipAllocationsPerItem.TryGetValue(alloc.PurchaseOrderItemId, out var list))
        {
            list = new List<DirectShipAllocationForPoModel>();
            model.DirectShipAllocationsPerItem[alloc.PurchaseOrderItemId] = list;
        }
        list.Add(new DirectShipAllocationForPoModel
        {
            AllocationId = alloc.AllocationId,
            DirectShipAddress = alloc.DirectShipAddress,
            DirectShipContactName = alloc.DirectShipContactName,
            DirectShipContactPhone = alloc.DirectShipContactPhone,
            AllocatedQuantity = alloc.AllocatedQuantity,
            Status = alloc.Status
        });
    }
}
```

Required using:
```csharp
using NamEcommerce.Application.Contracts.PurchaseOrders;
using NamEcommerce.Web.Models.PurchaseOrders;
```

- [ ] Edit `PurchaseOrderModelFactory.cs` — inject `IDirectShipAppService`, populate `DirectShipAllocationsPerItem` before `return model`.

### Step 1.7 — Add AJAX endpoint `UpdateDirectShipAddress` to `DirectShipDeliveryController`

File: `NamEcommerce/Presentation/NamEcommerce.Web/Controllers/DirectShipDeliveryController.cs`

Add after `RejectDelivery` method:

```csharp
[HttpPost]
public async Task<IActionResult> UpdateAddress([FromBody] UpdateDirectShipAddressRequest request)
{
    if (request.AllocationId == Guid.Empty)
        return Json(new { success = false, message = "ID phân bổ không hợp lệ." });

    if (string.IsNullOrWhiteSpace(request.NewAddress))
        return Json(new { success = false, message = "Địa chỉ giao không được để trống." });

    var result = await directShipAppService.UpdateDirectShipAddressAsync(new UpdateDirectShipAddressAppDto
    {
        AllocationId = request.AllocationId,
        NewAddress = request.NewAddress,
        NewContactName = request.NewContactName,
        NewContactPhone = request.NewContactPhone,
        Reason = request.Reason
    }).ConfigureAwait(false);

    if (result.Success)
        return Json(new { success = true, message = LocalizeError("Msg.SaveSuccess") });

    return Json(new { success = false, message = LocalizeError(result.ErrorMessage!) });
}
```

Add the request class (after the existing request classes at the bottom of the file):

```csharp
public sealed class UpdateDirectShipAddressRequest
{
    public Guid AllocationId { get; set; }
    public required string NewAddress { get; set; }
    public string? NewContactName { get; set; }
    public string? NewContactPhone { get; set; }
    public string? Reason { get; set; }
}
```

- [ ] Edit `DirectShipDeliveryController.cs` — add `UpdateAddress` action and `UpdateDirectShipAddressRequest` class.

### Step 1.8 — Add direct-ship sub-rows and address edit modal to PO Details view

File: `NamEcommerce/Presentation/NamEcommerce.Web/Views/PurchaseOrder/Details.cshtml`

**Sub-row rendering:** Inside the `@foreach (var item in Model.Info.Items)` loop, after the closing `</tr>` of each item row (around line 209), add:

```cshtml
@if (Model.DirectShipAllocationsPerItem.TryGetValue(item.Id, out var dsAllocations) && dsAllocations.Any())
{
    foreach (var alloc in dsAllocations)
    {
        <tr class="table-light border-0">
            <td colspan="2" class="ps-5 py-1">
                <span class="text-muted small"><i class="bi bi-send me-1 text-primary"></i>Giao thẳng:</span>
                <span class="small fw-medium ms-1">@alloc.DirectShipAddress</span>
                @if (!string.IsNullOrEmpty(alloc.DirectShipContactName))
                {
                    <span class="text-muted small ms-2">— @alloc.DirectShipContactName</span>
                }
                @if (!string.IsNullOrEmpty(alloc.DirectShipContactPhone))
                {
                    <span class="text-muted small ms-1">(@alloc.DirectShipContactPhone)</span>
                }
            </td>
            <td class="text-end py-1">
                <span class="small text-muted">@alloc.AllocatedQuantity.DisplayQuantity()</span>
            </td>
            <td class="py-1"></td>
            <td class="py-1 d-none d-md-table-cell"></td>
            @if (Model.Info.CanReceiveGoods || Model.Info.CanAddItems)
            {
                <td class="text-center py-1">
                    <button class="btn btn-sm btn-outline-secondary py-0 px-1 btnEditDirectShipAddress"
                            data-allocation-id="@alloc.AllocationId"
                            data-address="@alloc.DirectShipAddress"
                            data-contact-name="@alloc.DirectShipContactName"
                            data-contact-phone="@alloc.DirectShipContactPhone">
                        <i class="bi bi-pencil-square"></i>
                        <span class="d-none d-lg-inline">Sửa địa chỉ giao</span>
                    </button>
                </td>
            }
        </tr>
    }
}
```

**Address edit modal:** Add before the closing `@section Scripts` or before the `receiveItemModal` block:

```cshtml
<div class="modal fade" id="editDirectShipAddressModal" tabindex="-1" aria-hidden="true">
    <div class="modal-dialog modal-dialog-centered">
        <div class="modal-content border-0 shadow rounded-4">
            <div class="modal-header border-0 pb-0">
                <h5 class="modal-title fw-bold">Sửa địa chỉ giao</h5>
                <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
            </div>
            <div class="modal-body py-4">
                <input type="hidden" id="editDsAllocationId" />

                <div class="mb-3">
                    <label class="form-label small fw-bold text-muted text-uppercase">Địa chỉ giao <span class="text-danger">*</span></label>
                    <textarea id="editDsAddress" class="form-control" rows="2" required></textarea>
                </div>
                <div class="mb-3">
                    <label class="form-label small fw-bold text-muted text-uppercase">Người nhận</label>
                    <input id="editDsContactName" type="text" class="form-control" />
                </div>
                <div class="mb-3">
                    <label class="form-label small fw-bold text-muted text-uppercase">Điện thoại</label>
                    <input id="editDsContactPhone" type="text" class="form-control" />
                </div>

                <div id="editDsSuccessBanner" class="alert alert-warning border-0 d-none" role="alert">
                    <i class="bi bi-exclamation-triangle-fill me-2"></i>
                    Địa chỉ đã cập nhật — vui lòng in lại phiếu giao cho NCC.
                </div>
                <div id="editDsErrorBanner" class="alert alert-danger border-0 small d-none" role="alert"></div>
            </div>
            <div class="modal-footer border-0 pt-0 pb-4">
                <button type="button" class="btn btn-outline-secondary" data-bs-dismiss="modal">Đóng</button>
                <button type="button" class="btn btn-primary" id="btnSaveDirectShipAddress">
                    <i class="bi bi-save me-1"></i> Lưu
                </button>
            </div>
        </div>
    </div>
</div>
```

**JavaScript:** Add inside the existing `<script>` block or a new `<script>` block at the bottom:

```javascript
// D8.1–D8.3: Edit direct-ship address
document.querySelectorAll('.btnEditDirectShipAddress').forEach(btn => {
    btn.addEventListener('click', function () {
        document.getElementById('editDsAllocationId').value = this.dataset.allocationId;
        document.getElementById('editDsAddress').value = this.dataset.address || '';
        document.getElementById('editDsContactName').value = this.dataset.contactName || '';
        document.getElementById('editDsContactPhone').value = this.dataset.contactPhone || '';
        document.getElementById('editDsSuccessBanner').classList.add('d-none');
        document.getElementById('editDsErrorBanner').classList.add('d-none');
        bootstrap.Modal.getOrCreate(document.getElementById('editDirectShipAddressModal')).show();
    });
});

document.getElementById('btnSaveDirectShipAddress')?.addEventListener('click', async function () {
    const allocationId = document.getElementById('editDsAllocationId').value;
    const newAddress = document.getElementById('editDsAddress').value.trim();
    const successBanner = document.getElementById('editDsSuccessBanner');
    const errorBanner = document.getElementById('editDsErrorBanner');

    successBanner.classList.add('d-none');
    errorBanner.classList.add('d-none');

    if (!newAddress) {
        errorBanner.textContent = 'Địa chỉ giao không được để trống.';
        errorBanner.classList.remove('d-none');
        return;
    }

    const payload = {
        allocationId,
        newAddress,
        newContactName: document.getElementById('editDsContactName').value.trim() || null,
        newContactPhone: document.getElementById('editDsContactPhone').value.trim() || null
    };

    try {
        const resp = await fetch('/DirectShipDelivery/UpdateAddress', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? '' },
            body: JSON.stringify(payload)
        });
        const data = await resp.json();
        if (data.success) {
            successBanner.classList.remove('d-none');
        } else {
            errorBanner.textContent = data.message || 'Có lỗi xảy ra.';
            errorBanner.classList.remove('d-none');
        }
    } catch {
        errorBanner.textContent = 'Lỗi kết nối.';
        errorBanner.classList.remove('d-none');
    }
});
```

- [ ] Edit `Details.cshtml` — add sub-rows after each item's `</tr>`, add `editDirectShipAddressModal`, add JavaScript handlers.

### Step 1.9 — Build verify

- [ ] Run `dotnet build NamEcommerce/NamEcommerce.sln` — confirm 0 errors.

---

## Task 2: D8.4 — GR receive modal oversupply detection (client-side)

**Goal:** When the user types a `ReceivedQuantity` larger than `RemainingQuantity` in `receiveItemModal`, reveal a warning panel with 3 choices: "Nhập kho chính phần thừa", "Từ chối phần thừa", and "Hủy". Pass the selected action as a hidden input with the form. Dismiss modal on "Hủy".

**Verify:** Build passes. In the receive modal, entering qty > remaining shows the 3-choice panel. Selecting "Hủy" closes the modal. Selecting one of the two other options and submitting sends `OversupplyAction` in the POST body.

### Step 2.1 — Add `OversupplyAction` to `ReceivePurchaseOrderItemModel`

File: `NamEcommerce/Presentation/NamEcommerce.Web/Models/PurchaseOrders/ReceivePurchaseOrderItemModel.cs`

Read the current file first, then add at the end of the class:

```csharp
public string? OversupplyAction { get; set; }
```

Oversupply action values (string constants, not enum, to keep the model serialization simple):
- `"AcceptToMainWarehouse"` — stock the excess into main warehouse
- `"RejectOversupply"` — only record up to the ordered qty, ignore excess

- [ ] Edit `ReceivePurchaseOrderItemModel.cs` — add `public string? OversupplyAction { get; set; }`.

### Step 2.2 — Add `OversupplyAction` to `ReceivePurchaseOrderItemCommand`

File: `NamEcommerce/Presentation/NamEcommerce.Web.Contracts/Commands/Models/PurchaseOrders/ReceivePurchaseOrderItemCommand.cs`

```csharp
public string? OversupplyAction { get; set; }
```

- [ ] Edit `ReceivePurchaseOrderItemCommand.cs` — add `public string? OversupplyAction { get; set; }`.

### Step 2.3 — Pass `OversupplyAction` from model to command in `PurchaseOrderController.Receive`

File: `NamEcommerce/Presentation/NamEcommerce.Web/Controllers/PurchaseOrderController.cs`

Find the `Receive` action that creates `ReceivePurchaseOrderItemCommand`. Add `OversupplyAction = model.OversupplyAction` to the command initializer.

Example (existing code pattern):
```csharp
var command = new ReceivePurchaseOrderItemCommand
{
    PurchaseOrderId = model.PurchaseOrderId,
    PurchaseOrderItemId = model.PurchaseOrderItemId,
    WarehouseId = model.WarehouseId,
    ReceivedQuantity = model.ReceivedQuantity,
    SellingPrice = model.SellingPrice,
    OversupplyAction = model.OversupplyAction  // ADD THIS
};
```

- [ ] Edit `PurchaseOrderController.cs` — pass `OversupplyAction` when constructing the command.

### Step 2.4 — Add oversupply UI to `receiveItemModal` in `Details.cshtml`

File: `NamEcommerce/Presentation/NamEcommerce.Web/Views/PurchaseOrder/Details.cshtml`

Inside the `receiveItemModal` form body, after the quantity input block (around line 670), add:

```cshtml
<input type="hidden" name="OversupplyAction" id="modalOversupplyAction" value="" />

<div id="oversupplyWarningPanel" class="alert alert-warning border-0 mt-3 d-none">
    <div class="fw-bold mb-2">
        <i class="bi bi-exclamation-triangle-fill me-1"></i>
        Số lượng nhận vượt quá số lượng đặt hàng. Chọn cách xử lý phần thừa:
    </div>
    <div class="d-flex flex-column gap-2">
        <div class="form-check">
            <input class="form-check-input" type="radio" name="oversupplyChoice" id="oversupplyAccept" value="AcceptToMainWarehouse" />
            <label class="form-check-label" for="oversupplyAccept">
                <span class="fw-semibold">Nhập kho chính phần thừa</span>
                <div class="small text-muted">Phần thừa sẽ được nhập vào kho chính, không giao khách.</div>
            </label>
        </div>
        <div class="form-check">
            <input class="form-check-input" type="radio" name="oversupplyChoice" id="oversupplyReject" value="RejectOversupply" />
            <label class="form-check-label" for="oversupplyReject">
                <span class="fw-semibold">Từ chối phần thừa</span>
                <div class="small text-muted">Chỉ ghi nhận đúng số lượng đặt, trả lại phần thừa cho NCC.</div>
            </label>
        </div>
        <div class="form-check">
            <input class="form-check-input" type="radio" name="oversupplyChoice" id="oversupplyCancel" value="Cancel" checked />
            <label class="form-check-label" for="oversupplyCancel">
                <span class="fw-semibold">Hủy</span>
                <div class="small text-muted">Đóng modal, không lưu.</div>
            </label>
        </div>
    </div>
</div>
```

**JavaScript** — add to the existing `receiveItemModal` event handler block where `remaining` is known:

```javascript
// Oversupply detection
const qtyInput = document.getElementById('modalReceivedQty');
const oversupplyPanel = document.getElementById('oversupplyWarningPanel');
const oversupplyActionInput = document.getElementById('modalOversupplyAction');
const submitBtn = receiveItemModal.querySelector('[type="submit"]');
let itemRemainingQty = 0;

receiveItemModal.addEventListener('show.bs.modal', function(event) {
    // existing code sets itemRemainingQty via data-item-remaining
    itemRemainingQty = parseFloat(event.relatedTarget.getAttribute('data-item-remaining')) || 0;
    oversupplyPanel.classList.add('d-none');
    oversupplyActionInput.value = '';
    document.getElementById('oversupplyCancel').checked = true;
});

qtyInput.addEventListener('input', function() {
    const enteredQty = parseFloat(this.value.replace(/,/g, '')) || 0;
    if (enteredQty > itemRemainingQty) {
        oversupplyPanel.classList.remove('d-none');
    } else {
        oversupplyPanel.classList.add('d-none');
        oversupplyActionInput.value = '';
    }
});

receiveItemModal.querySelector('form').addEventListener('submit', function(e) {
    const enteredQty = parseFloat(qtyInput.value.replace(/,/g, '')) || 0;
    if (enteredQty > itemRemainingQty) {
        const choice = document.querySelector('input[name="oversupplyChoice"]:checked')?.value;
        if (!choice || choice === 'Cancel') {
            e.preventDefault();
            bootstrap.Modal.getInstance(receiveItemModal)?.hide();
            return;
        }
        oversupplyActionInput.value = choice;
    }
});
```

- [ ] Edit `Details.cshtml` — add `#modalOversupplyAction` hidden input inside the `receiveItemModal` form, add `#oversupplyWarningPanel` with 3 radio options, add JS oversupply detection and submit intercept.

### Step 2.5 — Build verify

- [ ] Run `dotnet build NamEcommerce/NamEcommerce.sln` — confirm 0 errors.

---

## Task 3: D8.5 — Backend oversupply handling

**Goal:** When `OversupplyAction == "AcceptToMainWarehouse"`, bypass the quantity guard in `ReceiveItemAsync` and stock the excess qty into the main warehouse via `IPurchaseOrderManager.ReceiveItemsAsync`, then raise `VendorOversupplyAccepted` event. When `OversupplyAction == "RejectOversupply"`, cap the received qty at `RemainingQuantity` before recording.

**Verify:** Build passes. Submitting receive with qty > remaining and "AcceptToMainWarehouse" succeeds (no error), and the `VendorOversupplyAccepted` event handler in `PurchaseOrderItem` fires. Submitting with "RejectOversupply" records only up to ordered qty.

### Step 3.1 — Add `OversupplyAction` to `ReceivedGoodsForItemAppDto`

File: `NamEcommerce/Application/NamEcommerce.Application.Contracts/Dtos/PurchaseOrders/PurchaseOrderItemAppDtos.cs`

Append inside `ReceivedGoodsForItemAppDto` class:

```csharp
public string? OversupplyAction { get; init; }
```

- [ ] Edit `PurchaseOrderItemAppDtos.cs` — add `public string? OversupplyAction { get; init; }` to `ReceivedGoodsForItemAppDto`.

### Step 3.2 — Pass `OversupplyAction` from command handler to app dto

File: `NamEcommerce/Presentation/NamEcommerce.Web.Framework/Commands/Handlers/PurchaseOrders/ReceivePurchaseOrderItemHandler.cs`

In the `Handle` method, add `OversupplyAction` when building the dto:

```csharp
var result = await _purchaseOrderAppService.ReceiveItemAsync(new ReceivedGoodsForItemAppDto(request.PurchaseOrderId, request.PurchaseOrderItemId)
{
    ReceivedQuantity = request.ReceivedQuantity,
    WarehouseId = request.WarehouseId,
    ReceivedByUserId = currentUser?.Id,
    SellingPrice = request.SellingPrice,
    OversupplyAction = request.OversupplyAction  // ADD THIS
}).ConfigureAwait(false);
```

- [ ] Edit `ReceivePurchaseOrderItemHandler.cs` — add `OversupplyAction = request.OversupplyAction` to the dto constructor.

### Step 3.3 — Add `AcceptOversupplyToMainWarehouse` method to `PurchaseOrderItem` entity

The `VendorOversupplyAccepted` event must be raised on an aggregate root. `PurchaseOrderItem` is the aggregate that owns the allocation. Find the file:

File: `NamEcommerce/Domain/NamEcommerce.Domain/Entities/PurchaseOrders/PurchaseOrderItem.cs`

Add a method to raise the event:

```csharp
internal void RaiseOversupplyAccepted(Guid warehouseId, decimal oversupplyQuantity, decimal unitCost)
{
    RaiseDomainEvent(new VendorOversupplyAccepted(Id, warehouseId, oversupplyQuantity, unitCost));
}
```

Required using:
```csharp
using NamEcommerce.Domain.Shared.Events.PurchaseOrders;
```

- [ ] Read `PurchaseOrderItem.cs` to verify it extends `AppAggregateEntity`, then add `RaiseOversupplyAccepted` method.

### Step 3.4 — Add oversupply method to `IPurchaseOrderManager`

File: `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Services/PurchaseOrders/IPurchaseOrderManager.cs`

Add:

```csharp
Task AcceptOversupplyToMainWarehouseAsync(
    Guid purchaseOrderId,
    Guid purchaseOrderItemId,
    decimal oversupplyQuantity,
    Guid warehouseId,
    CancellationToken ct = default);
```

- [ ] Edit `IPurchaseOrderManager.cs` — add the new method signature.

### Step 3.5 — Implement `AcceptOversupplyToMainWarehouseAsync` in `PurchaseOrderManager`

Find the `PurchaseOrderManager` implementation file (likely `NamEcommerce/Domain/NamEcommerce.Domain.Services/PurchaseOrders/PurchaseOrderManager.cs`).

Add implementation:

```csharp
public async Task AcceptOversupplyToMainWarehouseAsync(
    Guid purchaseOrderId, Guid purchaseOrderItemId, decimal oversupplyQuantity, Guid warehouseId,
    CancellationToken ct = default)
{
    var purchaseOrder = await GetPurchaseOrderByIdAsync(purchaseOrderId).ConfigureAwait(false)
        ?? throw new PurchaseOrderNotFoundException(purchaseOrderId);

    var item = purchaseOrder.Items.FirstOrDefault(i => i.Id == purchaseOrderItemId)
        ?? throw new PurchaseOrderItemNotFoundException(purchaseOrderItemId);

    item.RaiseOversupplyAccepted(warehouseId, oversupplyQuantity, item.UnitCost);
    await purchaseOrderRepository.UpdateAsync(purchaseOrder, ct).ConfigureAwait(false);
}
```

Note: `GetPurchaseOrderByIdAsync` and `purchaseOrderRepository` already exist in `PurchaseOrderManager`. Verify exact field names by reading the class before editing.

- [ ] Read `PurchaseOrderManager.cs` to confirm method and field names.
- [ ] Edit `PurchaseOrderManager.cs` — implement `AcceptOversupplyToMainWarehouseAsync`.

### Step 3.6 — Update `PurchaseOrderAppService.ReceiveItemAsync` to handle oversupply

File: `NamEcommerce/Application/NamEcommerce.Application.Services/PurchaseOrders/PurchaseOrderAppService.cs`

Current line 372 (the guard that blocks oversupply):
```csharp
if (purchaseOrderItem.QuantityReceived + dto.ReceivedQuantity > purchaseOrderItem.QuantityOrdered)
    return CommonActionResultDto.CreateError("Error.PurchaseOrderReceiveQuantityExceedsOrdered");
```

Replace with:

```csharp
var maxReceivable = purchaseOrderItem.QuantityOrdered - purchaseOrderItem.QuantityReceived;
if (dto.ReceivedQuantity > maxReceivable)
{
    if (dto.OversupplyAction == "RejectOversupply")
    {
        // Caller chose to reject excess — cap at remaining quantity
        dto = dto with { ReceivedQuantity = maxReceivable };
    }
    else if (dto.OversupplyAction == "AcceptToMainWarehouse")
    {
        // Will handle excess after normal receive below
    }
    else
    {
        return CommonActionResultDto.CreateError("Error.PurchaseOrderReceiveQuantityExceedsOrdered");
    }
}
```

Then, after the existing `await _purchaseOrderManager.ReceiveItemsAsync(...)` call (line 398–404), add:

```csharp
if (originalReceivedQuantity > maxReceivable && dto.OversupplyAction == "AcceptToMainWarehouse")
{
    var oversupplyQty = originalReceivedQuantity - maxReceivable;
    await _purchaseOrderManager.AcceptOversupplyToMainWarehouseAsync(
        dto.PurchaseOrderId, dto.PurchaseOrderItemId, oversupplyQty, warehouseId!.Value)
        .ConfigureAwait(false);
}
```

Where `originalReceivedQuantity` is captured before any capping:
```csharp
var originalReceivedQuantity = dto.ReceivedQuantity;
var maxReceivable = purchaseOrderItem.QuantityOrdered - purchaseOrderItem.QuantityReceived;
```

Since `ReceivedGoodsForItemAppDto` is a `record`, using `with` expression to cap it requires the record to support `with`. Confirmed: it is declared as `record` so `dto = dto with { ReceivedQuantity = maxReceivable }` is valid.

Full edit to `ReceiveItemAsync` around the guard (replace the section from the quantity check to just before the `ReceiveItemsAsync` call):

```csharp
var originalReceivedQuantity = dto.ReceivedQuantity;
var maxReceivable = purchaseOrderItem.QuantityOrdered - purchaseOrderItem.QuantityReceived;

if (originalReceivedQuantity > maxReceivable)
{
    if (dto.OversupplyAction == "RejectOversupply")
    {
        dto = dto with { ReceivedQuantity = maxReceivable };
    }
    else if (dto.OversupplyAction != "AcceptToMainWarehouse")
    {
        return CommonActionResultDto.CreateError("Error.PurchaseOrderReceiveQuantityExceedsOrdered");
    }
    // AcceptToMainWarehouse: receive only up to maxReceivable in the normal flow,
    // then stock the rest separately via AcceptOversupplyToMainWarehouseAsync
    if (dto.OversupplyAction == "AcceptToMainWarehouse")
        dto = dto with { ReceivedQuantity = maxReceivable };
}
```

After `await _purchaseOrderManager.ReceiveItemsAsync(...)`:

```csharp
return CommonActionResultDto.CreateSuccess();
// becomes:
if (originalReceivedQuantity > maxReceivable && dto.OversupplyAction == "AcceptToMainWarehouse")
{
    var oversupplyQty = originalReceivedQuantity - maxReceivable;
    await _purchaseOrderManager.AcceptOversupplyToMainWarehouseAsync(
        dto.PurchaseOrderId, dto.PurchaseOrderItemId, oversupplyQty, warehouseId!.Value)
        .ConfigureAwait(false);
}
return CommonActionResultDto.CreateSuccess();
```

- [ ] Read `PurchaseOrderAppService.cs` lines 353–406 before editing to confirm exact structure.
- [ ] Edit `PurchaseOrderAppService.cs` — replace the quantity guard block and add post-receive oversupply call.

### Step 3.7 — Build verify

- [ ] Run `dotnet build NamEcommerce/NamEcommerce.sln` — confirm 0 errors.

---

## File Change Summary

| File | Action | Change |
|------|--------|--------|
| `Domain/NamEcommerce.Domain/Entities/PurchaseOrders/PurchaseOrderItemAllocation.cs` | Modify | `UpdateDirectShipInfo` accepts `editedByUserId`, raises `DirectShipAddressUpdated` |
| `Domain/NamEcommerce.Domain/Entities/PurchaseOrders/PurchaseOrderItem.cs` | Modify | Add `RaiseOversupplyAccepted` method |
| `Domain/NamEcommerce.Domain.Services/PurchaseOrders/DirectShipManager.cs` | Modify | Pass `editedByUserId` to `UpdateDirectShipInfo`; add `GetDirectShipAllocationsForPoItemsAsync` |
| `Domain/NamEcommerce.Domain.Shared/Services/PurchaseOrders/IDirectShipManager.cs` | Modify | Add `GetDirectShipAllocationsForPoItemsAsync` signature |
| `Domain/NamEcommerce.Domain.Shared/Services/PurchaseOrders/IPurchaseOrderManager.cs` | Modify | Add `AcceptOversupplyToMainWarehouseAsync` signature |
| `Domain/NamEcommerce.Domain.Shared/Dtos/PurchaseOrders/DirectShipAllocationDtos.cs` | Modify | Append `DirectShipAllocationForPoItemDto` |
| `Domain/NamEcommerce.Domain.Services/PurchaseOrders/PurchaseOrderManager.cs` | Modify | Implement `AcceptOversupplyToMainWarehouseAsync` |
| `Application/NamEcommerce.Application.Contracts/PurchaseOrders/IDirectShipAppService.cs` | Modify | Add `GetDirectShipAllocationsForPoItemsAsync` signature |
| `Application/NamEcommerce.Application.Contracts/Dtos/PurchaseOrders/DirectShipAllocationAppDtos.cs` | Modify | Append `DirectShipAllocationForPoItemAppDto` |
| `Application/NamEcommerce.Application.Contracts/Dtos/PurchaseOrders/PurchaseOrderItemAppDtos.cs` | Modify | Add `OversupplyAction` to `ReceivedGoodsForItemAppDto` |
| `Application/NamEcommerce.Application.Services/PurchaseOrders/DirectShipAppService.cs` | Modify | Implement `GetDirectShipAllocationsForPoItemsAsync` |
| `Application/NamEcommerce.Application.Services/PurchaseOrders/PurchaseOrderAppService.cs` | Modify | Handle oversupply branches in `ReceiveItemAsync` |
| `Presentation/NamEcommerce.Web.Contracts/Commands/Models/PurchaseOrders/ReceivePurchaseOrderItemCommand.cs` | Modify | Add `OversupplyAction` |
| `Presentation/NamEcommerce.Web.Framework/Commands/Handlers/PurchaseOrders/ReceivePurchaseOrderItemHandler.cs` | Modify | Pass `OversupplyAction` to dto |
| `Presentation/NamEcommerce.Web/Models/PurchaseOrders/PurchaseOrderDetailsModel.cs` | Modify | Add `DirectShipAllocationsPerItem` and `DirectShipAllocationForPoModel` |
| `Presentation/NamEcommerce.Web/Models/PurchaseOrders/ReceivePurchaseOrderItemModel.cs` | Modify | Add `OversupplyAction` |
| `Presentation/NamEcommerce.Web/Services/PurchaseOrders/PurchaseOrderModelFactory.cs` | Modify | Inject `IDirectShipAppService`, populate `DirectShipAllocationsPerItem` |
| `Presentation/NamEcommerce.Web/Controllers/DirectShipDeliveryController.cs` | Modify | Add `UpdateAddress` endpoint and `UpdateDirectShipAddressRequest` |
| `Presentation/NamEcommerce.Web/Controllers/PurchaseOrderController.cs` | Modify | Pass `OversupplyAction` when building command |
| `Presentation/NamEcommerce.Web/Views/PurchaseOrder/Details.cshtml` | Modify | Sub-rows for direct-ship allocations, address edit modal, oversupply panel + JS |

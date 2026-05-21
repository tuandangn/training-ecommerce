# Inventory Costing Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a durable inventory costing model that supports pending receipt costs, weighted-average COGS, later revaluation, future FIFO/LIFO, and costing policy settings.

**Architecture:** Keep `InventoryStockManager` responsible for physical quantity. Add a separate `InventoryCostingManager` plus cost ledger/layer/allocation/policy entities for inventory value. Integrate costing through existing domain/application event handlers so quantity and cost move from the same business events without making stale snapshot fields authoritative.

**Tech Stack:** ASP.NET Core MVC, MediatR notifications, EF Core mappings, generic repository/data reader, Clean Architecture projects in `NamEcommerce`.

---

## Scope Rules

- Do not add or edit files under any `*.Test` project.
- Do not run `Add-Migration`, `Update-Database`, or any EF migration command.
- Do not delete old columns in this implementation. Stop relying on stale fields first.
- Keep physical stock by `ProductId + WarehouseId`.
- Start costing by `ProductId`, but store `WarehouseId` on every cost movement.
- Implement weighted average first. Add enum/data shape for FIFO/LIFO, but do not implement FIFO/LIFO runtime in this pass.
- Keep `Product.UnitPrice` as a selling price/default sales price concept.

## File Map

### Domain Shared

- Create `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Enums/Inventory/InventoryCostingEnums.cs`: costing method, valuation scope, movement type, status, rebuild status, rebuild trigger.
- Create `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Dtos/Inventory/InventoryCostingDtos.cs`: command/query DTOs for costing manager and app service.
- Create `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Services/Inventory/IInventoryCostingManager.cs`: domain service contract.
- Create `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Exceptions/Inventory/InventoryCostingExceptions.cs`: focused domain exceptions.

### Domain Entities

- Create `NamEcommerce/Domain/NamEcommerce.Domain/Entities/Inventory/InventoryCostingPolicy.cs`: active costing policy.
- Create `NamEcommerce/Domain/NamEcommerce.Domain/Entities/Inventory/InventoryCostLedgerEntry.cs`: source of truth for value movement.
- Create `NamEcommerce/Domain/NamEcommerce.Domain/Entities/Inventory/InventoryCostLayer.cs`: inbound layer for future FIFO/LIFO and pending receipt cost.
- Create `NamEcommerce/Domain/NamEcommerce.Domain/Entities/Inventory/InventoryCostAllocation.cs`: outbound COGS allocation.
- Create `NamEcommerce/Domain/NamEcommerce.Domain/Entities/Inventory/InventoryCostRebuildRun.cs`: historical rebuild audit.

### Infrastructure

- Modify `NamEcommerce/Infrastructure/NamEcommerce.Data.SqlServer/Usings.cs`: add required namespaces if new mapping files need them.
- Create `NamEcommerce/Infrastructure/NamEcommerce.Data.SqlServer/Mappings/InventoryCostingPolicyMapping.cs`.
- Create `NamEcommerce/Infrastructure/NamEcommerce.Data.SqlServer/Mappings/InventoryCostLedgerEntryMapping.cs`.
- Create `NamEcommerce/Infrastructure/NamEcommerce.Data.SqlServer/Mappings/InventoryCostLayerMapping.cs`.
- Create `NamEcommerce/Infrastructure/NamEcommerce.Data.SqlServer/Mappings/InventoryCostAllocationMapping.cs`.
- Create `NamEcommerce/Infrastructure/NamEcommerce.Data.SqlServer/Mappings/InventoryCostRebuildRunMapping.cs`.
- Do not create or modify migration files. After build succeeds, ask Tuấn to create/apply the migration.

### Domain Services

- Create `NamEcommerce/Domain/NamEcommerce.Domain.Services/Inventory/InventoryCostingManager.cs`: weighted-average costing, pending layers, allocations, revaluation, rebuild.
- Modify `NamEcommerce/Domain/NamEcommerce.Domain.Services/Inventory/InventoryStockManager.cs`: keep old quantity behavior; only remove authoritative use of `AverageCost` where this plan replaces it with costing manager reads.
- Modify `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Services/Inventory/IInventoryStockManager.cs`: keep compatibility methods during transition.

### Application Services And Events

- Modify `NamEcommerce/Application/NamEcommerce.Application.Services/Events/GoodsReceipts/GoodsReceiptCreatedHandler.cs`: register inbound cost movement after physical receipt.
- Modify `NamEcommerce/Application/NamEcommerce.Application.Services/Events/GoodsReceipts/GoodsReceiptItemUnitCostSetHandler.cs`: replace old full average recalculation with receipt cost assignment + product revaluation.
- Modify `NamEcommerce/Domain/NamEcommerce.Domain.Services/GoodsReceipts/GoodsReceiptManager.cs`: make all paths that set `GoodsReceiptItem.UnitCost` raise `GoodsReceiptItemUnitCostSet`.
- Modify `NamEcommerce/Domain/NamEcommerce.Domain.Services/PurchaseOrders/PurchaseOrderManager.cs`: when linking/splitting goods receipts and setting unit cost, raise the existing item-cost event.
- Modify `NamEcommerce/Application/NamEcommerce.Application.Services/Events/DeliveryNotes/DeliveryNoteDeliveredStockHandler.cs`: register outbound COGS movement after physical dispatch.
- Modify `NamEcommerce/Domain/NamEcommerce.Domain.Services/DeliveryNotes/DeliveryNoteManager.cs`: stop treating `CostAtDispatch` as authoritative.
- Modify `NamEcommerce/Domain/NamEcommerce.Domain.Services/StockTransfer/StockTransferNoteManager.cs`: use costing manager for transfer value; keep `StockTransferNoteItem.UnitCost` display-only during transition.
- Modify `NamEcommerce/Application/NamEcommerce.Application.Services/Events/StockAdjustment/StockAdjustmentNoteApprovedEventHandler.cs`: register adjustment cost movement.
- Modify `NamEcommerce/Domain/NamEcommerce.Domain.Services/GoodsReceipts/GoodsReceiptManager.cs`: customer-return receipt cost should come from original sale allocation when available, not `ReturnUnitPrice`.
- Modify `NamEcommerce/Application/NamEcommerce.Application.Services/Events/Returns/VendorReturnConfirmedEventHandler.cs`: keep supplier-facing `ReturnUnitCost`, but let outbound inventory cost come from costing manager through delivery dispatch.

### Reports And UI

- Modify `NamEcommerce/Application/NamEcommerce.Application.Services/Report/FinancialReportAppService.cs`: read COGS from cost allocations/ledger instead of `DeliveryNoteItem.CostAtDispatch`.
- Modify `NamEcommerce/Domain/NamEcommerce.Domain.Shared/Dtos/Finance/FinanceDtos.cs`: add pending/revalued cost flags if the current report DTOs need them.
- Create `NamEcommerce/Application/NamEcommerce.Application.Contracts/Inventory/IInventoryCostingAppService.cs`.
- Create `NamEcommerce/Application/NamEcommerce.Application.Contracts/Dtos/Inventory/InventoryCostingAppDtos.cs`.
- Create `NamEcommerce/Application/NamEcommerce.Application.Services/Inventory/InventoryCostingAppService.cs`.
- Create `NamEcommerce/Presentation/NamEcommerce.Web.Contracts/Queries/Models/Inventory/GetInventoryCostingPolicyQuery.cs`.
- Create `NamEcommerce/Presentation/NamEcommerce.Web.Contracts/Commands/Models/Inventory/UpdateInventoryCostingPolicyCommand.cs`.
- Create `NamEcommerce/Presentation/NamEcommerce.Web.Contracts/Commands/Models/Inventory/RebuildInventoryCostingCommand.cs`.
- Create `NamEcommerce/Presentation/NamEcommerce.Web.Framework/Queries/Handlers/Inventory/GetInventoryCostingPolicyHandler.cs`.
- Create `NamEcommerce/Presentation/NamEcommerce.Web.Framework/Commands/Handlers/Inventory/UpdateInventoryCostingPolicyHandler.cs`.
- Create `NamEcommerce/Presentation/NamEcommerce.Web.Framework/Commands/Handlers/Inventory/RebuildInventoryCostingHandler.cs`.
- Modify `NamEcommerce/Presentation/NamEcommerce.Web/Controllers/InventoryController.cs`: add policy/rebuild actions.
- Create `NamEcommerce/Presentation/NamEcommerce.Web/Models/Inventory/InventoryCostingPolicyModel.cs`.
- Create `NamEcommerce/Presentation/NamEcommerce.Web/Views/Inventory/CostingPolicy.cshtml`.
- Modify `NamEcommerce/Presentation/NamEcommerce.Web/Program.cs`: register costing manager and costing app service.
- Modify `NamEcommerce/Presentation/Customer/NamEcommerce.Customer.Api/Infrastructure/CustomerApiServiceCollectionExtensions.cs`: register costing manager if customer portal flows can dispatch/return stock through shared services.

## Task 1: Add Costing Enums And DTO Contracts

- [ ] Create `InventoryCostingEnums.cs` with these enums:

```csharp
namespace NamEcommerce.Domain.Shared.Enums.Inventory;

public enum InventoryCostingMethod
{
    WeightedAverage = 0,
    FIFO = 1,
    LIFO = 2
}

public enum InventoryValuationScope
{
    Product = 0,
    ProductWarehouse = 1
}

public enum InventoryCostMovementType
{
    GoodsReceipt = 0,
    SaleDispatch = 1,
    CustomerReturn = 2,
    VendorReturn = 3,
    TransferOut = 4,
    TransferIn = 5,
    PositiveAdjustment = 6,
    NegativeAdjustment = 7,
    Revaluation = 8,
    RevertReceipt = 9
}

public enum InventoryCostingStatus
{
    Pending = 0,
    Final = 1,
    Revalued = 2,
    Superseded = 3
}

public enum InventoryCostRebuildStatus
{
    Queued = 0,
    Running = 1,
    Completed = 2,
    Failed = 3
}

public enum InventoryCostRebuildTrigger
{
    ReceiptCostAssigned = 0,
    PolicyRebuild = 1,
    ManualRepair = 2
}

public enum InventoryCostReferenceType
{
    None = 0,
    PurchaseOrder = 1,
    SalesOrder = 2,
    StockIssue = 3,
    StockTransfer = 4,
    Adjustment = 5,
    GoodsReceipt = 6,
    CustomerReturn = 7,
    VendorReturn = 8
}
```

- [ ] Create `InventoryCostingDtos.cs` with request/result records used by the manager:

```csharp
using NamEcommerce.Domain.Shared.Enums.Inventory;

namespace NamEcommerce.Domain.Shared.Dtos.Inventory;

public sealed record RegisterInventoryInboundCostDto
{
    public required Guid ProductId { get; init; }
    public required Guid WarehouseId { get; init; }
    public required decimal Quantity { get; init; }
    public required decimal? UnitCost { get; init; }
    public required InventoryCostMovementType MovementType { get; init; }
    public required InventoryCostReferenceType ReferenceType { get; init; }
    public required Guid ReferenceId { get; init; }
    public required Guid ReferenceItemId { get; init; }
    public DateTime? OccurredAtUtc { get; init; }
}

public sealed record RegisterInventoryOutboundCostDto
{
    public required Guid ProductId { get; init; }
    public required Guid WarehouseId { get; init; }
    public required decimal Quantity { get; init; }
    public required InventoryCostMovementType MovementType { get; init; }
    public required InventoryCostReferenceType ReferenceType { get; init; }
    public required Guid ReferenceId { get; init; }
    public required Guid ReferenceItemId { get; init; }
    public DateTime? OccurredAtUtc { get; init; }
}

public sealed record RegisterInventoryTransferInCostDto
{
    public required Guid ProductId { get; init; }
    public required Guid WarehouseId { get; init; }
    public required decimal Quantity { get; init; }
    public required decimal UnitCost { get; init; }
    public required InventoryCostingStatus SourceStatus { get; init; }
    public required InventoryCostReferenceType ReferenceType { get; init; }
    public required Guid ReferenceId { get; init; }
    public required Guid ReferenceItemId { get; init; }
    public DateTime? OccurredAtUtc { get; init; }
}

public sealed record AssignGoodsReceiptItemCostDto
{
    public required Guid GoodsReceiptId { get; init; }
    public required Guid GoodsReceiptItemId { get; init; }
    public required Guid ProductId { get; init; }
    public required Guid WarehouseId { get; init; }
    public required decimal UnitCost { get; init; }
}

public sealed record InventoryCostSummaryDto
{
    public required Guid ProductId { get; init; }
    public required decimal QuantityBalance { get; init; }
    public required decimal ValueBalance { get; init; }
    public required decimal AverageCost { get; init; }
    public required InventoryCostingStatus Status { get; init; }
}

public sealed record InventoryCostMovementResultDto
{
    public required Guid LedgerEntryId { get; init; }
    public required decimal UnitCost { get; init; }
    public required decimal TotalCost { get; init; }
    public required InventoryCostingStatus Status { get; init; }
}

public sealed record InventoryCogsSummaryDto
{
    public required decimal TotalCost { get; init; }
    public required bool HasPendingCost { get; init; }
    public required bool HasRevaluedCost { get; init; }
}
```

- [ ] Create `IInventoryCostingManager.cs` with this interface:

```csharp
using NamEcommerce.Domain.Shared.Dtos.Inventory;
using NamEcommerce.Domain.Shared.Enums.Inventory;

namespace NamEcommerce.Domain.Shared.Services.Inventory;

public interface IInventoryCostingManager
{
    Task<InventoryCostMovementResultDto> RegisterInboundAsync(RegisterInventoryInboundCostDto dto);
    Task<InventoryCostMovementResultDto> RegisterOutboundAsync(RegisterInventoryOutboundCostDto dto);
    Task<InventoryCostMovementResultDto> RegisterTransferInAsync(RegisterInventoryTransferInCostDto dto);
    Task AssignGoodsReceiptItemCostAsync(AssignGoodsReceiptItemCostDto dto);
    Task<InventoryCostSummaryDto> GetCurrentCostSummaryAsync(Guid productId);
    Task<InventoryCogsSummaryDto> GetCogsForReferencesAsync(InventoryCostReferenceType referenceType, IEnumerable<Guid> referenceIds);
    Task<Guid> RevalueProductFromAsync(Guid productId, DateTime fromUtc, InventoryCostRebuildTrigger trigger, Guid? requestedByUserId);
    Task<Guid> RebuildAllAsync(InventoryCostingMethod method, InventoryValuationScope scope, Guid? requestedByUserId);
}
```

- [ ] Create `InventoryCostingExceptions.cs` with specific exceptions for invalid quantity, missing policy, and unsupported runtime costing method.

- [ ] Run `rtk dotnet build NamEcommerce\NamEcommerce.sln`.

- [ ] Commit:

```bash
rtk git add NamEcommerce/Domain/NamEcommerce.Domain.Shared/Enums/Inventory/InventoryCostingEnums.cs NamEcommerce/Domain/NamEcommerce.Domain.Shared/Dtos/Inventory/InventoryCostingDtos.cs NamEcommerce/Domain/NamEcommerce.Domain.Shared/Services/Inventory/IInventoryCostingManager.cs NamEcommerce/Domain/NamEcommerce.Domain.Shared/Exceptions/Inventory/InventoryCostingExceptions.cs
rtk git commit -m "feat: add inventory costing contracts"
```

## Task 2: Add Costing Entities And EF Mappings

- [ ] Create `InventoryCostingPolicy.cs`:

```csharp
using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Enums.Inventory;

namespace NamEcommerce.Domain.Entities.Inventory;

[Serializable]
public sealed record InventoryCostingPolicy : AppAggregateEntity
{
    public InventoryCostingPolicy(Guid id, InventoryCostingMethod costingMethod, InventoryValuationScope valuationScope, DateTime effectiveFromUtc, Guid? createdByUserId, string? note) : base(id)
    {
        CostingMethod = costingMethod;
        ValuationScope = valuationScope;
        EffectiveFromUtc = effectiveFromUtc;
        IsActive = true;
        CreatedByUserId = createdByUserId;
        CreatedAtUtc = DateTime.UtcNow;
        Note = note;
    }

    public InventoryCostingMethod CostingMethod { get; private set; }
    public InventoryValuationScope ValuationScope { get; private set; }
    public DateTime EffectiveFromUtc { get; private set; }
    public bool IsActive { get; private set; }
    public Guid? CreatedByUserId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public string? Note { get; private set; }

    public void Deactivate() => IsActive = false;
}
```

- [ ] Create `InventoryCostLedgerEntry.cs` with constructor fields matching the spec and immutable public properties except status/run updates:

```csharp
using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Enums.Inventory;

namespace NamEcommerce.Domain.Entities.Inventory;

[Serializable]
public sealed record InventoryCostLedgerEntry : AppAggregateEntity
{
    public InventoryCostLedgerEntry(Guid id, Guid productId, Guid warehouseId, DateTime occurredAtUtc, long sequenceNumber,
        InventoryCostMovementType movementType, decimal quantityDelta, decimal? unitCost, decimal? totalCost,
        decimal quantityBalanceAfter, decimal valueBalanceAfter, decimal averageCostAfter,
        InventoryCostingStatus costingStatus, InventoryCostingMethod costingMethod, InventoryValuationScope valuationScope,
        InventoryCostReferenceType referenceType, Guid referenceId, Guid referenceItemId, Guid? costingRunId) : base(id)
    {
        ProductId = productId;
        WarehouseId = warehouseId;
        OccurredAtUtc = occurredAtUtc;
        SequenceNumber = sequenceNumber;
        MovementType = movementType;
        QuantityDelta = quantityDelta;
        UnitCost = unitCost;
        TotalCost = totalCost;
        QuantityBalanceAfter = quantityBalanceAfter;
        ValueBalanceAfter = valueBalanceAfter;
        AverageCostAfter = averageCostAfter;
        CostingStatus = costingStatus;
        CostingMethod = costingMethod;
        ValuationScope = valuationScope;
        ReferenceType = referenceType;
        ReferenceId = referenceId;
        ReferenceItemId = referenceItemId;
        CostingRunId = costingRunId;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid ProductId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }
    public long SequenceNumber { get; private set; }
    public InventoryCostMovementType MovementType { get; private set; }
    public decimal QuantityDelta { get; private set; }
    public decimal? UnitCost { get; private set; }
    public decimal? TotalCost { get; private set; }
    public decimal QuantityBalanceAfter { get; private set; }
    public decimal ValueBalanceAfter { get; private set; }
    public decimal AverageCostAfter { get; private set; }
    public InventoryCostingStatus CostingStatus { get; private set; }
    public InventoryCostingMethod CostingMethod { get; private set; }
    public InventoryValuationScope ValuationScope { get; private set; }
    public InventoryCostReferenceType ReferenceType { get; private set; }
    public Guid ReferenceId { get; private set; }
    public Guid ReferenceItemId { get; private set; }
    public Guid? CostingRunId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public void MarkSuperseded(Guid costingRunId)
    {
        CostingStatus = InventoryCostingStatus.Superseded;
        CostingRunId = costingRunId;
    }
}
```

- [ ] Create `InventoryCostLayer.cs`, `InventoryCostAllocation.cs`, and `InventoryCostRebuildRun.cs` using the fields from `docs/superpowers/specs/2026-05-21-inventory-costing-design.md`.

- [ ] Map every money/quantity column with decimal types:

```csharp
builder.Property(p => p.QuantityDelta).HasColumnType("decimal(18,4)");
builder.Property(p => p.UnitCost).HasColumnType("decimal(18,4)");
builder.Property(p => p.TotalCost).HasColumnType("decimal(18,4)");
builder.Property(p => p.ValueBalanceAfter).HasColumnType("decimal(18,4)");
builder.Property(p => p.AverageCostAfter).HasColumnType("decimal(18,4)");
```

- [ ] Add indexes:

```csharp
builder.HasIndex(p => new { p.ProductId, p.OccurredAtUtc, p.SequenceNumber });
builder.HasIndex(p => new { p.ReferenceType, p.ReferenceId, p.ReferenceItemId });
builder.HasIndex(p => p.CostingStatus);
```

- [ ] Map `InventoryCostingPolicy` with an index that makes active policy lookup cheap:

```csharp
builder.HasIndex(p => new { p.IsActive, p.EffectiveFromUtc });
builder.Property(p => p.Note).HasMaxLength(1000);
```

- [ ] Run `rtk dotnet build NamEcommerce\NamEcommerce.sln`.

- [ ] Tell Tuấn to create the EF migration manually after this task is merged. Do not run migration commands.

- [ ] Commit:

```bash
rtk git add NamEcommerce/Domain/NamEcommerce.Domain/Entities/Inventory NamEcommerce/Infrastructure/NamEcommerce.Data.SqlServer/Mappings NamEcommerce/Infrastructure/NamEcommerce.Data.SqlServer/Usings.cs
rtk git commit -m "feat: add inventory costing persistence model"
```

## Task 3: Implement Weighted-Average Costing Manager Foundation

- [ ] Create `InventoryCostingManager.cs` with constructor dependencies:

```csharp
public sealed class InventoryCostingManager : IInventoryCostingManager
{
    private readonly IRepository<InventoryCostingPolicy> _policyRepository;
    private readonly IRepository<InventoryCostLedgerEntry> _ledgerRepository;
    private readonly IRepository<InventoryCostLayer> _layerRepository;
    private readonly IRepository<InventoryCostAllocation> _allocationRepository;
    private readonly IRepository<InventoryCostRebuildRun> _rebuildRunRepository;
    private readonly IEntityDataReader<InventoryCostingPolicy> _policyReader;
    private readonly IEntityDataReader<InventoryCostLedgerEntry> _ledgerReader;
    private readonly IEntityDataReader<InventoryCostLayer> _layerReader;
    private readonly IEntityDataReader<InventoryCostAllocation> _allocationReader;
}
```

- [ ] Add `GetActivePolicyAsync()`:

```csharp
private async Task<InventoryCostingPolicy> GetActivePolicyAsync()
{
    var policy = _policyReader.DataSource
        .Where(p => p.IsActive)
        .OrderByDescending(p => p.EffectiveFromUtc)
        .FirstOrDefault();

    if (policy is not null)
        return policy;

    var defaultPolicy = new InventoryCostingPolicy(
        Guid.NewGuid(),
        InventoryCostingMethod.WeightedAverage,
        InventoryValuationScope.Product,
        DateTime.UtcNow,
        null,
        "Default inventory costing policy");

    return await _policyRepository.InsertAsync(defaultPolicy).ConfigureAwait(false);
}
```

- [ ] Add `GetNextSequenceNumber()` by reading max `SequenceNumber` from ledger and adding 1.

- [ ] Add `GetLastProductBalance(Guid productId)` from the latest non-superseded ledger entry for the product.

- [ ] Add `RegisterInboundAsync`:
  - validate `Quantity > 0`.
  - use active policy.
  - for `WeightedAverage`, if `UnitCost` has value, calculate inbound value and new product-wide average.
  - if `UnitCost` is null, create pending ledger/layer and keep previous value/average.
  - create one `InventoryCostLedgerEntry`.
  - create one `InventoryCostLayer`.

- [ ] Add `RegisterOutboundAsync`:
  - validate `Quantity > 0`.
  - for `WeightedAverage`, use latest product average.
  - if current product has pending layers before or at the outbound sequence, mark outbound ledger/allocation as pending.
  - create negative quantity ledger entry.
  - create one allocation with `InboundLayerId = null` for weighted average.

- [ ] Add `GetCurrentCostSummaryAsync(productId)` returning latest balance and pending/final status.

- [ ] Add `GetCogsForReferencesAsync(referenceType, referenceIds)` summing allocations by outbound reference.

- [ ] For `FIFO` or `LIFO`, throw `UnsupportedInventoryCostingMethodException` from runtime movement methods for now.

- [ ] Register the service in:
  - `NamEcommerce/Presentation/NamEcommerce.Web/Program.cs`
  - `NamEcommerce/Presentation/Customer/NamEcommerce.Customer.Api/Infrastructure/CustomerApiServiceCollectionExtensions.cs`

```csharp
services.AddScoped<IInventoryCostingManager, InventoryCostingManager>();
```

- [ ] Run `rtk dotnet build NamEcommerce\NamEcommerce.sln`.

- [ ] Commit:

```bash
rtk git add NamEcommerce/Domain/NamEcommerce.Domain.Services/Inventory/InventoryCostingManager.cs NamEcommerce/Presentation/NamEcommerce.Web/Program.cs NamEcommerce/Presentation/Customer/NamEcommerce.Customer.Api/Infrastructure/CustomerApiServiceCollectionExtensions.cs
rtk git commit -m "feat: implement weighted average costing manager"
```

## Task 4: Integrate Goods Receipts And Delayed Cost Assignment

- [ ] Modify `GoodsReceiptCreatedHandler` to inject `IInventoryCostingManager`.

- [ ] After each successful `ReceiveStockAsync`, call:

```csharp
await _inventoryCostingManager.RegisterInboundAsync(new RegisterInventoryInboundCostDto
{
    ProductId = item.ProductId,
    WarehouseId = item.WarehouseId.Value,
    Quantity = item.Quantity,
    UnitCost = item.UnitCost,
    MovementType = InventoryCostMovementType.GoodsReceipt,
    ReferenceType = InventoryCostReferenceType.GoodsReceipt,
    ReferenceId = goodsReceipt.Id,
    ReferenceItemId = item.Id,
    OccurredAtUtc = goodsReceipt.CreatedOnUtc
}).ConfigureAwait(false);
```

- [ ] Modify `GoodsReceiptItemUnitCostSetHandler`:
  - remove the call to old `RecalculateAverageCostAsync`.
  - inject/use `IInventoryCostingManager`.
  - call `AssignGoodsReceiptItemCostAsync`.
  - keep vendor debt creation logic.

- [ ] Implement `AssignGoodsReceiptItemCostAsync` in `InventoryCostingManager`:
  - find the pending layer/ledger by `ReferenceType = GoodsReceipt`, `ReferenceId`, `ReferenceItemId`.
  - set layer cost to the new unit cost.
  - call `RevalueProductFromAsync(productId, layer.OpenedAtUtc, ReceiptCostAssigned, null)`.

- [ ] Modify `GoodsReceiptManager.CreateGoodsReceiptAsync` so items that have `UnitCost` at creation eventually raise `MarkItemUnitCostSet` after `MarkCreated`.

- [ ] Modify `PurchaseOrderManager.ApplyCostAssignmentsAndSplit` path so every `SetUnitCost(unitCost)` also results in `goodsReceipt.MarkItemUnitCostSet(item.Id)` for the original or split item.

- [ ] Keep old `InventoryStock.AverageCost` update methods in place for compatibility, but do not call them from the new costing path.

- [ ] Run `rtk dotnet build NamEcommerce\NamEcommerce.sln`.

- [ ] Manual smoke after Tuấn runs migration:
  - create goods receipt without unit cost.
  - confirm quantity increases.
  - verify cost ledger/layer is pending.
  - set unit cost later.
  - verify ledger/layer becomes priced and revaluation run is created.

- [ ] Commit:

```bash
rtk git add NamEcommerce/Application/NamEcommerce.Application.Services/Events/GoodsReceipts NamEcommerce/Domain/NamEcommerce.Domain.Services/GoodsReceipts/GoodsReceiptManager.cs NamEcommerce/Domain/NamEcommerce.Domain.Services/PurchaseOrders/PurchaseOrderManager.cs NamEcommerce/Domain/NamEcommerce.Domain.Services/Inventory/InventoryCostingManager.cs
rtk git commit -m "feat: register goods receipt inventory cost"
```

## Task 5: Integrate Dispatch, Vendor Returns, And COGS Allocations

- [ ] Modify `DeliveryNoteDeliveredStockHandler` to inject `IInventoryCostingManager`.

- [ ] After each successful `DispatchStockAsync`, call:

```csharp
await _inventoryCostingManager.RegisterOutboundAsync(new RegisterInventoryOutboundCostDto
{
    ProductId = item.ProductId,
    WarehouseId = deliveryNote.WarehouseId,
    Quantity = item.Quantity,
    MovementType = deliveryNote.SourceType == (int)DeliveryNoteSourceType.ToVendorReturn
        ? InventoryCostMovementType.VendorReturn
        : InventoryCostMovementType.SaleDispatch,
    ReferenceType = deliveryNote.SourceType == (int)DeliveryNoteSourceType.ToVendorReturn
        ? InventoryCostReferenceType.VendorReturn
        : InventoryCostReferenceType.SalesOrder,
    ReferenceId = deliveryNote.Id,
    ReferenceItemId = item.Id,
    OccurredAtUtc = deliveryNote.DeliveredOnUtc ?? DateTime.UtcNow
}).ConfigureAwait(false);
```

- [ ] Use `DeliveryNoteAppDto.DeliveredOnUtc ?? DateTime.UtcNow` as the outbound costing timestamp.

- [ ] Modify `DeliveryNoteManager`:
  - stop using `GetAverageCostAsync` to decide authoritative COGS.
  - leave `CostAtDispatch` assignment only as a transition display value if removing it would break views.
  - prefer setting it from costing summary for display, not from `InventoryStock.AverageCost`.

- [ ] Ensure vendor return delivery still decreases physical stock through the existing delivery note path. Do not use `VendorReturnItem.ReturnUnitCost` as inventory cost.

- [ ] Run `rtk dotnet build NamEcommerce\NamEcommerce.sln`.

- [ ] Manual smoke after migration:
  - deliver a sale after priced receipt.
  - verify allocation total equals moving weighted average times quantity.
  - deliver a sale after pending receipt.
  - verify allocation is pending.
  - confirm vendor return creates outbound cost movement independent of supplier-facing return amount.

- [ ] Commit:

```bash
rtk git add NamEcommerce/Application/NamEcommerce.Application.Services/Events/DeliveryNotes/DeliveryNoteDeliveredStockHandler.cs NamEcommerce/Domain/NamEcommerce.Domain.Services/DeliveryNotes/DeliveryNoteManager.cs NamEcommerce/Domain/NamEcommerce.Domain.Services/Inventory/InventoryCostingManager.cs
rtk git commit -m "feat: allocate inventory cogs on dispatch"
```

## Task 6: Integrate Stock Transfers And Adjustments

- [ ] Modify `StockTransferNoteManager` to inject `IInventoryCostingManager`.

- [ ] Before or after the existing `TransferStockAsync`, register transfer out and transfer in cost movements:

```csharp
var transferOutCost = await inventoryCostingManager.RegisterOutboundAsync(new RegisterInventoryOutboundCostDto
{
    ProductId = item.ProductId,
    WarehouseId = note.FromWarehouseId,
    Quantity = item.Quantity,
    MovementType = InventoryCostMovementType.TransferOut,
    ReferenceType = InventoryCostReferenceType.StockTransfer,
    ReferenceId = note.Id,
    ReferenceItemId = item.Id,
    OccurredAtUtc = DateTime.UtcNow
}).ConfigureAwait(false);
```

- [ ] Register transfer in with the cost returned by transfer out:

```csharp
await inventoryCostingManager.RegisterTransferInAsync(new RegisterInventoryTransferInCostDto
{
    ProductId = item.ProductId,
    WarehouseId = note.ToWarehouseId,
    Quantity = item.Quantity,
    UnitCost = transferOutCost.UnitCost,
    SourceStatus = transferOutCost.Status,
    ReferenceType = InventoryCostReferenceType.StockTransfer,
    ReferenceId = note.Id,
    ReferenceItemId = item.Id,
    OccurredAtUtc = DateTime.UtcNow
}).ConfigureAwait(false);
```

- [ ] Implement `RegisterTransferInAsync` so it creates a transfer-in ledger entry and layer using the supplied unit cost/status. With product-level valuation, the transfer-out and transfer-in pair must return the product-wide quantity and value balances to their previous totals.

- [ ] Keep `item.UnitCost` as display-only by setting it from the transfer allocation/unit cost returned by costing manager.

- [ ] Modify `StockAdjustmentNoteApprovedEventHandler` to inject `IInventoryCostingManager`.

- [ ] For positive delta, call `RegisterInboundAsync` with `UnitCost = null` and `MovementType = PositiveAdjustment`.

- [ ] For negative delta, call `RegisterOutboundAsync` with `MovementType = NegativeAdjustment`.

- [ ] Run `rtk dotnet build NamEcommerce\NamEcommerce.sln`.

- [ ] Manual smoke after migration:
  - approve transfer and confirm two warehouse movements are recorded.
  - confirm product-level value is unchanged by transfer.
  - approve positive adjustment and confirm pending inbound cost.
  - approve negative adjustment and confirm outbound cost allocation.

- [ ] Commit:

```bash
rtk git add NamEcommerce/Domain/NamEcommerce.Domain.Services/StockTransfer/StockTransferNoteManager.cs NamEcommerce/Application/NamEcommerce.Application.Services/Events/StockAdjustment/StockAdjustmentNoteApprovedEventHandler.cs NamEcommerce/Domain/NamEcommerce.Domain.Services/Inventory/InventoryCostingManager.cs
rtk git commit -m "feat: record transfer and adjustment costs"
```

## Task 7: Revalue Product History When Late Cost Arrives

- [ ] Implement `RevalueProductFromAsync` in `InventoryCostingManager`.

- [ ] Create an `InventoryCostRebuildRun` with:
  - `Status = Running`
  - `Trigger = ReceiptCostAssigned`
  - `ProductId = productId`
  - `FromUtc = fromUtc`
  - active policy method/scope

- [ ] Load all non-superseded ledger entries for the product from `fromUtc` onward ordered by:

```csharp
.OrderBy(e => e.OccurredAtUtc)
.ThenBy(e => e.SequenceNumber)
```

- [ ] Mark affected ledger entries and allocations as `Superseded` with the run id.

- [ ] Replay movements using weighted average:
  - priced inbound updates quantity/value/average.
  - pending inbound updates quantity and keeps status pending.
  - outbound uses current average and remains pending if consumed balance includes pending cost.
  - transfer entries preserve value symmetry.

- [ ] Insert replacement ledger entries and allocations with `CostingStatus = Revalued` when replacing a previous finalized/pending movement, and `CostingStatus = Final` when replaying a movement that was created inside the current rebuild run for the first time.

- [ ] Complete the rebuild run. On exception, mark it failed and store the message.

- [ ] Run `rtk dotnet build NamEcommerce\NamEcommerce.sln`.

- [ ] Manual smoke after migration:
  - receipt 10 units without cost.
  - sell 4 units.
  - set receipt cost later.
  - verify the sale allocation changes from pending to final/revalued.

- [ ] Commit:

```bash
rtk git add NamEcommerce/Domain/NamEcommerce.Domain.Services/Inventory/InventoryCostingManager.cs
rtk git commit -m "feat: revalue inventory cost after late receipt pricing"
```

## Task 8: Move Financial Reports To Costing Allocations

- [ ] Modify `FinancialReportAppService` to inject `IInventoryCostingManager`.

- [ ] Replace COGS calculation from:

```csharp
dn.Items.Sum(i => (i.CostAtDispatch ?? 0m) * i.Quantity)
```

with costing-manager lookup for delivered note ids.

- [ ] Add pending/revalued flags to finance DTOs only if the report UI needs to display the status.

- [ ] Keep return amount logic using `CustomerReturnItem.ReturnUnitPrice`, because that is refund amount, not inventory cost.

- [ ] Run `rtk dotnet build NamEcommerce\NamEcommerce.sln`.

- [ ] Manual smoke after migration:
  - compare profit report before and after setting late receipt cost.
  - verify report can indicate pending COGS before cost is known.

- [ ] Commit:

```bash
rtk git add NamEcommerce/Application/NamEcommerce.Application.Services/Report/FinancialReportAppService.cs NamEcommerce/Domain/NamEcommerce.Domain.Shared/Dtos/Finance/FinanceDtos.cs
rtk git commit -m "feat: report cogs from inventory costing"
```

## Task 9: Add Costing Policy App Service And Admin UI

- [ ] Read `DESIGN.md` before touching Razor/UI files.

- [ ] Create `IInventoryCostingAppService` with methods:

```csharp
Task<InventoryCostingPolicyAppDto> GetActivePolicyAsync();
Task<InventoryCostingPolicyAppDto> UpdatePolicyAsync(UpdateInventoryCostingPolicyAppDto dto);
Task<Guid> RebuildAllAsync(RebuildInventoryCostingAppDto dto);
```

- [ ] Create app DTOs with `CostingMethod`, `ValuationScope`, `EffectiveFromUtc`, `Note`, and rebuild command fields.

- [ ] Implement `InventoryCostingAppService` using `IInventoryCostingManager` and repositories/readers for policy/rebuild run.

- [ ] Create MediatR query/command contracts and handlers listed in the file map.

- [ ] Add `InventoryController.CostingPolicy` GET.

- [ ] Add `InventoryController.CostingPolicy` POST to update active policy.

- [ ] Add `InventoryController.RebuildCosting` POST to trigger full rebuild.

- [ ] Create `CostingPolicy.cshtml` using existing Bootstrap/content-card patterns from `DESIGN.md`.

- [ ] Show:
  - active method.
  - valuation scope.
  - effective date.
  - note.
  - explicit rebuild button with confirmation text.

- [ ] Default update behavior must only affect new transactions from the effective date.

- [ ] Run `rtk dotnet build NamEcommerce\NamEcommerce.sln`.

- [ ] Manual smoke after migration:
  - open costing policy page.
  - change method effective for future transactions.
  - run rebuild action.
  - confirm rebuild run row is created.

- [ ] Commit:

```bash
rtk git add NamEcommerce/Application/NamEcommerce.Application.Contracts NamEcommerce/Application/NamEcommerce.Application.Services/Inventory NamEcommerce/Presentation/NamEcommerce.Web.Contracts NamEcommerce/Presentation/NamEcommerce.Web.Framework NamEcommerce/Presentation/NamEcommerce.Web/Controllers/InventoryController.cs NamEcommerce/Presentation/NamEcommerce.Web/Models/Inventory NamEcommerce/Presentation/NamEcommerce.Web/Views/Inventory NamEcommerce/Presentation/NamEcommerce.Web/Program.cs
rtk git commit -m "feat: add inventory costing policy settings"
```

## Task 10: Stop Using Stale Cost Fields As Authority

- [ ] Search for stale authoritative cost usage:

```bash
rtk powershell -NoProfile -Command "rg -n 'OrderItem\\.CostPrice|Product\\.CostPrice|InventoryStock\\.AverageCost|CostAtDispatch|StockTransferNoteItem\\.UnitCost|ReturnUnitPrice|ReturnUnitCost' NamEcommerce -g '*.cs' -g '!NamEcommerce/Migrations/**' -g '!NamEcommerce/Tests/**'"
```

- [ ] For each result:
  - leave sales-price fields alone.
  - leave `ReturnUnitPrice` and `ReturnUnitCost` for refund/debt only.
  - remove COGS/reporting dependency on `CostAtDispatch`.
  - remove inventory-cost dependency on `Product.CostPrice`.
  - keep `StockTransferNoteItem.UnitCost` display-only.

- [ ] Do not delete properties or columns yet.

- [ ] Update comments that explicitly say old fields are authoritative if those comments are now false.

- [ ] Run `rtk dotnet build NamEcommerce\NamEcommerce.sln`.

- [ ] Commit:

```bash
rtk git add NamEcommerce
rtk git commit -m "refactor: stop using stale inventory cost snapshots"
```

## Task 11: Final Verification And Migration Handoff

- [ ] Run:

```bash
rtk dotnet build NamEcommerce\NamEcommerce.sln
```

- [ ] Run the stale usage search from Task 10 again and classify any remaining results in the final notes.

- [ ] Check git status:

```bash
rtk git status --short
```

- [ ] Tell Tuấn to run the EF migration commands manually for the new costing tables.

- [ ] Manual smoke checklist after Tuấn applies migration:
  - goods receipt without cost increases quantity and creates pending cost.
  - sale while cost is pending is allowed and creates pending COGS.
  - setting receipt cost later revalues affected sale COGS.
  - report shows final or pending cost status.
  - stock transfer records both warehouses.
  - vendor return uses inventory valuation for stock-out and `ReturnUnitCost` only for supplier amount.
  - customer return restores cost from original allocation when available.
  - changing costing method only affects new transactions.
  - full rebuild creates a rebuild run and recalculates history.

## Known Deferred Cleanup

- Delete `OrderItem.CostPrice` after no code path reads it.
- Delete or rename `Product.CostPrice` after purchasing suggestions no longer need it.
- Keep or rename `Product.UnitPrice` as sales/default selling price.
- Delete or convert `InventoryStock.AverageCost` into a read model only after reports and UI no longer depend on it.
- Delete `DeliveryNoteItem.CostAtDispatch` after order/detail views read cost from allocations.
- Delete `StockTransferNoteItem.UnitCost` after transfer details read display cost from ledger/allocation.

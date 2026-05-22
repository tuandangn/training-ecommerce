# Inventory Costing Design

Date: 2026-05-21

## Goal

Replace the current inventory cost calculation with a reliable costing model that supports:

- receiving goods before the purchase cost is known.
- selling and dispatching goods while the cost is still pending.
- recalculating affected cost of goods sold when purchase cost is entered later.
- weighted average costing first, with a path to FIFO and LIFO.
- product-level costing now, while preserving warehouse-level movement data for future reporting.
- a business setting for the active costing method and a separate operation to rebuild history.

## Current Context

The current stock model is quantity-first:

- `InventoryStock` stores stock by product and warehouse, including `AverageCost`.
- `InventoryStockManager` receives, dispatches, transfers, and adjusts quantities.
- `StockMovementLog` records quantity movement but does not record inventory value or costing status.
- `GoodsReceiptItem.UnitCost` can be null, but cost recalculation depends on `GoodsReceiptItemUnitCostSet`.
- The current average cost recalculation uses historical priced receipt items for a product and warehouse, which is not a complete valuation model for remaining inventory or COGS.
- Delivery note items snapshot `CostAtDispatch`, and reports use that snapshot for COGS.
- Transfers and returns carry some cost-like fields, but those fields are not a consistent source of truth.

Known stale or risky cost fields:

- `OrderItem.CostPrice`
- `Product.CostPrice`
- `InventoryStock.AverageCost`
- `DeliveryNoteItem.CostAtDispatch`
- `StockTransferNoteItem.UnitCost`

`Product.UnitPrice` is a selling price concept, not an inventory cost concept. It should not be used for COGS. It can remain for product sales defaults unless the sales pricing model is redesigned separately.

## Decisions

- Keep physical quantity by `ProductId + WarehouseId`.
- Start inventory costing by `ProductId`.
- Store `WarehouseId` on every cost movement and allocation so future warehouse-level valuation can be added without losing source data.
- Weighted average is the initial active costing method.
- FIFO and LIFO must be supported by the data model, even if not implemented in the first release.
- Goods can be sold and dispatched while cost is pending.
- Reports must be able to show whether COGS/profit is final or pending.
- Changing the costing method applies to new transactions by default.
- Historical recalculation is a separate explicit operation.
- Existing stale cost fields are phased out instead of deleted immediately.
- Do not add or edit unit tests because project instructions forbid changes in `*.Test` projects.
- Do not run migrations from the AI workflow.

## Costing Scope

The first implementation uses product-level valuation:

```text
Costing key: ProductId
Quantity key: ProductId + WarehouseId
```

This means stock quantity remains accurate per warehouse. Inventory value is calculated for the product as a whole.

Every ledger entry still stores `WarehouseId`. This keeps the audit trail and allows a later move to:

```text
Costing key: ProductId + WarehouseId
```

The later move should be a policy change plus historical rebuild, not a schema rewrite.

## Core Concepts

### InventoryCostingPolicy

Stores the active business policy for inventory costing.

Suggested fields:

- `Id`
- `CostingMethod`: `WeightedAverage`, `FIFO`, `LIFO`
- `ValuationScope`: `Product`, `ProductWarehouse`
- `EffectiveFromUtc`
- `IsActive`
- `CreatedByUserId`
- `CreatedAtUtc`
- `Note`

Only one policy should be active for new transactions at a time. Each cost result stores the method and scope used to calculate it so historical numbers remain auditable after policy changes.

### InventoryCostLedgerEntry

The source of truth for inventory value movement.

Suggested fields:

- `Id`
- `ProductId`
- `WarehouseId`
- `OccurredAtUtc`
- `SequenceNumber`
- `MovementType`: receipt, sale dispatch, customer return, vendor return, transfer out, transfer in, adjustment, revaluation
- `QuantityDelta`
- `UnitCost`
- `TotalCost`
- `QuantityBalanceAfter`
- `ValueBalanceAfter`
- `AverageCostAfter`
- `CostingStatus`: `Pending`, `Final`, `Revalued`, `Superseded`
- `CostingMethod`
- `ValuationScope`
- `ReferenceType`
- `ReferenceId`
- `ReferenceItemId`
- `CostingRunId`
- `CreatedAtUtc`

`SequenceNumber` is important because recalculation must be deterministic when multiple movements have the same timestamp.

Balance fields are calculated at the active valuation scope. In the initial product-level scope, `QuantityBalanceAfter`, `ValueBalanceAfter`, and `AverageCostAfter` represent the product-wide costing balance, while `WarehouseId` records where the physical movement happened.

### InventoryCostLayer

Represents inbound stock that can later be consumed by FIFO/LIFO.

Suggested fields:

- `Id`
- `ProductId`
- `WarehouseId`
- `SourceLedgerEntryId`
- `SourceReferenceType`
- `SourceReferenceId`
- `SourceReferenceItemId`
- `OpenedAtUtc`
- `OriginalQuantity`
- `RemainingQuantity`
- `UnitCost`
- `TotalCost`
- `CostingStatus`
- `CostingMethod`
- `ValuationScope`
- `ClosedAtUtc`

Weighted average can use ledger balances directly. FIFO/LIFO consume from layers.

### InventoryCostAllocation

Links outbound movements to the inbound cost they consumed.

Suggested fields:

- `Id`
- `ProductId`
- `WarehouseId`
- `OutboundLedgerEntryId`
- `OutboundReferenceType`
- `OutboundReferenceId`
- `OutboundReferenceItemId`
- `InboundLayerId`
- `Quantity`
- `UnitCost`
- `TotalCost`
- `CostingStatus`
- `CostingMethod`
- `ValuationScope`
- `CostingRunId`
- `CreatedAtUtc`

Reports should use allocations or ledger entries for COGS, not `DeliveryNoteItem.CostAtDispatch`.

### InventoryCostRebuildRun

Tracks explicit recalculation work.

Suggested fields:

- `Id`
- `Status`: queued, running, completed, failed
- `Trigger`: cost assigned, policy rebuild, manual repair
- `CostingMethod`
- `ValuationScope`
- `FromUtc`
- `ToUtc`
- `ProductId`
- `RequestedByUserId`
- `StartedAtUtc`
- `CompletedAtUtc`
- `ErrorMessage`

The default policy change does not rewrite history. A rebuild run is required to recalculate historical transactions.

## Business Flows

### Goods Receipt Without Cost

When warehouse staff create a goods receipt without known cost:

- increase `InventoryStock.QuantityOnHand`.
- create a receipt ledger entry with positive quantity and `CostingStatus = Pending`.
- create a pending layer for the received quantity.
- do not update `InventoryStock.AverageCost` as a source of truth.
- allow later sale or dispatch of the product.

### Selling While Cost Is Pending

When goods are dispatched and the relevant cost source is pending:

- decrease physical stock as today.
- create an outbound ledger entry.
- create pending allocation records when exact cost cannot be finalized.
- mark affected COGS/profit as pending.
- keep the sale operationally valid.

Financial reports should surface pending status instead of pretending the profit is final.

### Admin Assigns Receipt Cost Later

When admin enters unit cost for a goods receipt item:

- update `GoodsReceiptItem.UnitCost`.
- mark the related pending receipt layer as priced.
- start a revaluation from that receipt movement for the affected product.
- recalculate later outbound COGS and remaining inventory value.
- update affected ledger entries and allocations.
- mark reports as final or revalued where possible.

This recalculation must include sales that happened after the receipt, even if those sales were already delivered.

### Weighted Average

Weighted average should be calculated as a moving balance:

```text
new average = (previous value + inbound value) / (previous quantity + inbound quantity)
outbound cost = current average * outbound quantity
```

Pending inbound cost means later outbound cost may remain pending until that inbound cost is known.

### FIFO and LIFO

FIFO and LIFO should consume `InventoryCostLayer` records:

- FIFO consumes oldest available layers.
- LIFO consumes newest available layers.
- pending layers can still be consumed, but the outbound allocation remains pending until the layer is priced.

The first release can implement weighted average only, as long as the entities and service boundaries do not block FIFO/LIFO later.

### Customer Returns

`CustomerReturnItem.ReturnUnitPrice` is a refund/sales amount, not inventory cost.

When returned goods are accepted back into stock:

- increase physical stock in the return warehouse.
- restore inventory cost from the original sale allocation where possible.
- if the original allocation is pending, the customer return cost is also pending.
- if the original allocation is unavailable, use a controlled fallback policy and flag the result for review.

### Vendor Returns

`VendorReturnItem.ReturnUnitCost` is the supplier-facing refund/debt amount, not the source of inventory valuation.

When goods are returned to vendor:

- decrease physical stock.
- create an outbound cost movement from current inventory valuation.
- record supplier refund/debt separately through the existing vendor return/debt flow.

### Stock Transfers

Transfers must create cost movements for both warehouses:

- transfer out from source warehouse.
- transfer in to destination warehouse.
- preserve the same cost value across both sides.

With product-level costing, transfer does not change total product value. It is still recorded with both warehouse IDs for future warehouse-level reporting.

`StockTransferNoteItem.UnitCost` can remain temporarily as a display snapshot, but it should not be the source of truth.

### Stock Adjustments

Adjustments need explicit cost behavior:

- positive adjustment creates inbound cost, either entered by admin or pending.
- negative adjustment creates outbound cost using the active costing method.
- all adjustments write ledger entries and preserve warehouse context.

## Costing Settings UI

Add an admin-facing setting area for inventory costing.

Initial fields:

- active costing method.
- valuation scope.
- effective date.
- note/reason.

Default behavior:

- changing method affects new transactions from the effective date.
- historical data is not changed automatically.

Add a separate action:

- rebuild all history.
- optionally rebuild for one product first if needed for operational safety.
- show rebuild status and failure details.

## Field Migration Strategy

Phase out stale fields in two steps: stop using them first, delete them later.

### Stop Using As Source Of Truth

- `OrderItem.CostPrice`: no longer used for COGS.
- `Product.CostPrice`: no longer used for inventory cost.
- `InventoryStock.AverageCost`: no longer authoritative.
- `DeliveryNoteItem.CostAtDispatch`: no longer authoritative.
- `StockTransferNoteItem.UnitCost`: display snapshot only.

### Keep Or Rename

- `Product.UnitPrice`: keep as selling price/default sales price for now.
- Later rename to `DefaultSellingPrice` if the sales pricing model is cleaned up.

### Delete Later

After reports and workflows read from the new costing model, create a separate cleanup migration for stale columns. The AI should not run that migration.

## Application Boundaries

Add a domain service such as `InventoryCostingManager`.

Responsibilities:

- register inbound cost movements.
- register outbound cost movements.
- assign cost to pending receipts.
- create and update layers.
- create allocations.
- calculate weighted average.
- expose current product cost status.
- run revaluation from a movement.
- run full rebuild.

Existing stock quantity operations remain in `InventoryStockManager`. Costing should be called from application/domain event handlers around the same business events that change stock quantity.

## Reporting

Financial reports should read COGS from the costing model:

- final COGS when allocations are final.
- pending COGS when one or more allocations are pending.
- revalued COGS when later cost assignment changed previously reported values.

Inventory reports can show:

- quantity by warehouse.
- total product inventory value.
- pending value count/amount.
- later, warehouse-level value after valuation scope changes.

## Implementation Phases

### Phase 1: Foundation

- Add costing enums and entities.
- Add EF mappings and repositories/readers.
- Add default costing policy.
- Do not remove old columns yet.

### Phase 2: Weighted Average Runtime

- Add `InventoryCostingManager`.
- Write ledger entries for receipts, dispatches, transfers, returns, and adjustments.
- Support pending receipt cost and delayed admin cost assignment.
- Route financial COGS reads to the new model.

### Phase 3: Revaluation And Rebuild

- Revalue affected product movements when receipt cost is entered later.
- Add full rebuild operation.
- Add admin UI for costing method and rebuild action.

### Phase 4: Cleanup

- Stop updating old cost snapshots.
- Remove stale usages.
- Prepare a cleanup migration for obsolete columns.

## Verification

Verify with:

- `rtk dotnet build NamEcommerce\NamEcommerce.sln`
- manual smoke checks for:
  - goods receipt without cost.
  - sale/dispatch while cost is pending.
  - admin later sets receipt cost and affected COGS updates.
  - costing method change applies only to new transactions.
  - full rebuild recalculates historical transactions.
  - customer return restores cost from original sale allocation.
  - vendor return uses inventory valuation for stock out and supplier amount separately.
  - stock transfer preserves value while recording both warehouses.

## Out Of Scope

- Running migrations.
- Adding or modifying unit tests.
- Replacing the sales pricing model.
- Deleting stale columns in the first implementation phase.
- Implementing FIFO/LIFO runtime behavior in the first release.

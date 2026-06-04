# Purchase Order Detail Workflow Implementation

## Goal

Make purchase order detail as clear as sales order detail and add persisted timeline notes for purchase order item add, edit, and remove actions.

## TodoList

### Task 1: Persist Purchase Order Activities

- [ ] Create `NamEcommerce\Domain\NamEcommerce.Domain.Shared\Enums\PurchaseOrders\PurchaseOrderActivityType.cs` with values for `ItemAdded`, `ItemUpdated`, `ItemRemoved`, `StatusChanged`, and `ManualNote`.
- [ ] Create `NamEcommerce\Domain\NamEcommerce.Domain\Entities\PurchaseOrders\PurchaseOrderActivity.cs` as a sealed record with internal constructor and private setters.
- [ ] Add mapping `NamEcommerce\Infrastructure\NamEcommerce.Data.SqlServer\Mappings\PurchaseOrderActivityMapping.cs`.
- [ ] Add EF migration in `NamEcommerce\Migrations\NamEcommerce.Data.SqlServerMigrations\Migrations`.
- [ ] Verify: build Domain, Data.SqlServer, and migration projects.

### Task 2: Add Activity DTOs and Services

- [ ] Create domain DTOs in `NamEcommerce\Domain\NamEcommerce.Domain.Shared\Dtos\PurchaseOrders\PurchaseOrderActivityDtos.cs`.
- [ ] Add manager methods to `IPurchaseOrderManager` and `PurchaseOrderManager`: record activity and get activities by purchase order id.
- [ ] Add app DTOs in `NamEcommerce\Application\NamEcommerce.Application.Contracts\Dtos\PurchaseOrders\PurchaseOrderActivityAppDtos.cs`.
- [ ] Add app service methods to `IPurchaseOrderAppService` and `PurchaseOrderAppService`.
- [ ] Verify: build Domain.Shared, Domain.Services, Application.Contracts, and Application.Services.

### Task 3: Add Purchase Order Item Edit

- [ ] Create `EditPurchaseOrderItemModel` in `NamEcommerce\Presentation\NamEcommerce.Web\Models\PurchaseOrders`.
- [ ] Create `UpdatePurchaseOrderItemCommand` in `NamEcommerce\Presentation\NamEcommerce.Web.Contracts\Commands\Models\PurchaseOrders`.
- [ ] Create `UpdatePurchaseOrderItemResultModel` if `CommonActionResultModel` is not enough.
- [ ] Create `UpdatePurchaseOrderItemHandler` in `NamEcommerce\Presentation\NamEcommerce.Web.Framework\Commands\Handlers\PurchaseOrders`.
- [ ] Add domain/app DTOs for updating quantity, unit cost, and note.
- [ ] Add `UpdatePurchaseOrderItem` POST action to `PurchaseOrderController`.
- [ ] Verify invalid cases: missing item, locked purchase order, quantity <= 0, unit cost < 0.
- [ ] Verify: build Web.Contracts, Web.Framework, and Web.

### Task 4: Record Item Timeline Notes

- [ ] In add item flow, record an activity note like `Them hang hoa: {ProductName} - SL {Quantity} - Don gia {UnitCost}`.
- [ ] In edit item flow, record an activity note like `Sua hang hoa: {ProductName} - SL {OldQuantity} -> {NewQuantity}; Don gia {OldUnitCost} -> {NewUnitCost}`.
- [ ] In remove item flow, record an activity note like `Xoa hang hoa: {ProductName} - SL {Quantity} - Don gia {UnitCost}` before deleting the line.
- [ ] Keep activity recording after successful mutation only.
- [ ] Verify by inspecting activity rows through app service in the model factory.

### Task 5: Extend Purchase Order Detail Models

- [ ] Extend `PurchaseOrderDetailsModel` with `WorkflowStage`, `WorkflowModel`, `WorkflowStepModel`, `OrderingModel`, `ReceivingModel`, `ReturnsModel`, `SettlementModel`, and `TimelineEventModel`.
- [ ] Keep existing properties used by current modals and actions.
- [ ] Ensure `Info`, `AddItemModel`, `ReceiveItemModels`, `RelatedGoodsReceipts`, `RelatedVendorReturns`, `AllocationsPerItem`, and `DirectShipAllocationsPerItem` remain populated.
- [ ] Verify: build Web project after model changes.

### Task 6: Build Workflow Data in ModelFactory

- [ ] Update `PurchaseOrderModelFactory.PreparePurchaseOrderDetailsModel` to load activities with receipts and vendor returns.
- [ ] Add private helper methods for active stage, workflow steps, ordering section, receiving section, returns section, settlement section, and timeline.
- [ ] Timeline ordering should use `OccurredOn`.
- [ ] Derived timeline rows should include creation, submit/approve/status changes where available, goods receipt, vendor return, close partial, cancel, and completion.
- [ ] Verify: build Web project.

### Task 7: Split Purchase Order Detail View

- [ ] Replace the top-level detail content with workflow bar, main panel column, and timeline aside.
- [ ] Create `_PurchaseOrderWorkflowBar.cshtml`.
- [ ] Create `_PurchaseOrderWorkflowOrderingPanel.cshtml`.
- [ ] Create `_PurchaseOrderWorkflowReceivingPanel.cshtml`.
- [ ] Create `_PurchaseOrderWorkflowReturnsPanel.cshtml`.
- [ ] Create `_PurchaseOrderWorkflowSettlementPanel.cshtml`.
- [ ] Create `_PurchaseOrderWorkflowTimeline.cshtml`.
- [ ] Preserve existing receive, bulk receive, allocation, direct ship, return, close partial, cancel, and info edit UI.
- [ ] Verify the page still posts to existing endpoints.

### Task 8: Add Edit Item UI Wiring

- [ ] Add edit item button in the ordering panel when `Info.CanAddItems` is true.
- [ ] Add edit item modal to `Views\PurchaseOrder\Details.cshtml`.
- [ ] Add JavaScript to prefill item id, product name, quantity, unit cost, and note.
- [ ] Submit edit through `PurchaseOrderController.UpdatePurchaseOrderItem`.
- [ ] Refresh back to details after success, matching current add/remove behavior.

### Task 9: Final Verification

- [ ] Run all targeted build commands from `purchase_order_detail_workflow_plan.md`.
- [ ] Start Web project and smoke test a purchase order detail page.
- [ ] Manually verify these UI paths: add item, edit item, remove item, receive item, bulk receive, create vendor return link, close partial, cancel.
- [ ] Confirm Timeline shows item notes with old/new values after add/edit/remove.


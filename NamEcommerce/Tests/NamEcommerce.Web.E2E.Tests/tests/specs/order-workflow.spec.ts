import { expect, test } from '@playwright/test';
import { E2EClient } from '../support/e2e-client';
import {
  DeliveryNoteWorkflowPage,
  DirectShipDeliveryPage,
  OrderCreatePage,
  OrderDetailsWorkflowPage,
  PurchaseOrderCreatePage,
  PurchaseOrderWorkflowPage,
} from '../pages/OrderWorkflowPage';
import { StockInventoryState } from '../states/StockInventoryState';

test.describe.serial('Order workflow', () => {
  test('creates one-product order, receives stock, delivers, then completes order', async ({ page, request }) => {
    const client = new E2EClient(request);
    const scenarioId = `order-stock-${Date.now()}`;
    const quantity = 3;
    const seed = await client.seedOrderWorkflow(scenarioId, quantity, false);
    const inventory = new StockInventoryState(client);

    const orderUrl = await new OrderCreatePage(page).create(seed);
    await inventory.checkInventoryState(scenarioId, {
      globalReservedQuantity: quantity
    });
    const poUrl = await new PurchaseOrderCreatePage(page).create(seed);

    await page.goto(poUrl);
    const purchaseOrder = new PurchaseOrderWorkflowPage(page);
    await purchaseOrder.submitAndApprove();
    await purchaseOrder.receive(seed, false);

    await page.goto(orderUrl);
    await new OrderDetailsWorkflowPage(page).createDelivery(seed);

    const deliveryNoteWorkflowPage = new DeliveryNoteWorkflowPage(page);
    await deliveryNoteWorkflowPage.confirm();
    await inventory.checkInventoryState(scenarioId, {
      globalReservedQuantity: 0,
      stockOnHandQuantity: quantity,
      stockReservedQuantity: quantity
    });
    await deliveryNoteWorkflowPage.processDelivery();

    await page.goto(orderUrl);
    await new OrderDetailsWorkflowPage(page).completeOrder();

    await expect.poll(async () => await client.getOrderWorkflowState(scenarioId)).toMatchObject({
      orderStatus: 'Completed',
      purchaseOrderStatus: 'Completed',
      deliveryStatus: 'Delivered',
      orderedQuantity: quantity,
      receivedQuantity: quantity,
      deliveredQuantity: quantity,
      stockInfo: {
        globalReservedQuantity: 0,
        stockAvailableQuantity: 0,
        stockOnHandQuantity: 0,
        stockReservedQuantity: 0
      }
    });
  });

  test('creates one-product order, receives direct ship purchase order, then confirms delivery', async ({ page, request }) => {
    const client = new E2EClient(request);
    const scenarioId = `order-direct-${Date.now()}`;
    const quantity = 2;
    const seed = await client.seedOrderWorkflow(scenarioId, quantity, true);

    const orderUrl = await new OrderCreatePage(page).create(seed);
    const poUrl = await new PurchaseOrderCreatePage(page).create(seed);

    await page.goto(poUrl);
    const purchaseOrder = new PurchaseOrderWorkflowPage(page);
    await purchaseOrder.submitAndApprove();
    await purchaseOrder.receive(seed, true);

    const stateAfterReceive = await client.getOrderWorkflowState(scenarioId);
    await new DirectShipDeliveryPage(page).confirm(stateAfterReceive.deliveryNoteCode);

    await page.goto(orderUrl);
    await new OrderDetailsWorkflowPage(page).completeOrder();

    await expect.poll(async () => await client.getOrderWorkflowState(scenarioId)).toMatchObject({
      orderStatus: 'Completed',
      purchaseOrderStatus: 'Completed',
      deliveryStatus: 'Delivered',
      orderedQuantity: quantity,
      receivedQuantity: quantity,
      deliveredQuantity: quantity,
    });
  });
});

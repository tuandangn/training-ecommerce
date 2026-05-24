import { expect, Page } from '@playwright/test';
import { Buffer } from 'buffer';
import { OrderWorkflowSeed } from '../support/e2e-client';
import { clickAndWait, selectPickerItem, submitAndWaitForUrl } from '../support/pickers';

export class OrderCreatePage {
  constructor(private readonly page: Page) {}

  async create(seed: OrderWorkflowSeed) {
    await this.page.goto('/Order/Create');

    await selectPickerItem(this.page, 'order-customer-picker', '.customerSearch', seed.customerName);
    await this.page.getByTestId('order-add-product-open').click();
    await selectPickerItem(this.page, 'order-product-picker', '.productSearch', seed.productName);
    await this.page.getByTestId('order-item-quantity').fill(String(seed.quantity));
    await this.page.getByTestId('order-item-unit-price').fill(String(seed.unitPrice));
    await this.page.getByTestId('order-add-item-submit').click();
    await expect(this.page.locator('#itemsTableBody tr')).toHaveCount(1);

    await submitAndWaitForUrl(this.page, this.page.getByTestId('order-submit'), /\/Order\/Details\//i);
    return this.page.url();
  }
}

export class PurchaseOrderCreatePage {
  constructor(private readonly page: Page) {}

  async create(seed: OrderWorkflowSeed) {
    await this.page.goto('/PurchaseOrder/Create');

    await selectPickerItem(this.page, 'po-vendor-picker', '.vendorSearch', seed.vendorName);
    await this.page.getByTestId('po-add-product-open').click();
    await selectPickerItem(this.page, 'po-product-picker', '.productSearch', seed.productName);
    await this.page.getByTestId('po-item-quantity').fill(String(seed.quantity));
    await this.page.getByTestId('po-item-unit-cost').fill(String(seed.unitCost));
    await this.page.getByTestId('po-add-item-submit').click();
    await expect(this.page.locator('#itemsTableBody tr')).toHaveCount(1);

    await submitAndWaitForUrl(this.page, this.page.getByTestId('po-submit'), /\/PurchaseOrder\/Details\//i);
    return this.page.url();
  }
}

export class PurchaseOrderWorkflowPage {
  constructor(private readonly page: Page) {}

  async submitAndApprove() {
    await submitAndWaitForUrl(this.page, this.page.getByTestId('po-submit-for-approval'), /\/PurchaseOrder\/Details\//i);
    await submitAndWaitForUrl(this.page, this.page.getByTestId('po-approve'), /\/PurchaseOrder\/Details\//i);
  }

  async receive(seed: OrderWorkflowSeed, directShip: boolean) {
    await this.page.getByTestId('po-item-actions').first().click();
    await this.page.getByTestId('po-receive-item-open').click();
    await this.page.getByTestId('po-receive-quantity').fill(String(seed.quantity));

    if (directShip) {
      await this.page.getByTestId('po-receive-direct-ship').check();
      const orderItem = this.page.locator('#modalDsOrderItems .list-group-item-action').first();
      await expect(orderItem).toBeVisible();
      await orderItem.click();
    } else {
      await this.page.getByTestId('po-receive-warehouse').selectOption({ label: seed.warehouseName });
    }

    await clickAndWait(this.page, () => this.page.getByTestId('po-receive-submit').click());
  }
}

export class OrderDetailsWorkflowPage {
  constructor(private readonly page: Page) {}

  async createDelivery(seed: OrderWorkflowSeed) {
    await this.activatePanel('delivery');
    await this.page.getByTestId('order-create-delivery').click();
    await this.page.locator('select[name="WarehouseId"]').selectOption({ label: seed.warehouseName });
    await submitAndWaitForUrl(this.page, this.page.getByTestId('delivery-create-submit'), /\/DeliveryNote\/Details\//i);
    return this.page.url();
  }

  async completeOrder() {
    await this.activatePanel('settlement');
    await this.page.getByTestId('order-complete').click();
    await this.page.locator('#confirmModal .btnConfirm').click();
    await this.page.waitForLoadState('domcontentloaded');
  }

  private async activatePanel(panel: string) {
    const tab = this.page.locator(`[data-workflow-target="${panel}"]`);
    if (await tab.count()) {
      await tab.click();
    }
  }
}

export class DeliveryNoteWorkflowPage {
  constructor(private readonly page: Page) {}

  async confirm() {
    this.page.once('dialog', dialog => dialog.accept());
    await clickAndWait(this.page, () => this.page.getByTestId('delivery-confirm').click());
  }

  async processDelivery() {
    await clickAndWait(this.page, () => this.page.getByTestId('delivery-mark-delivering').click());
    await this.page.getByTestId('delivery-mark-delivered-open').click();
    await this.page.getByTestId('delivery-receiver-name').fill('E2E Receiver');
    await this.page.getByTestId('delivery-proof-file').setInputFiles({
      name: 'delivery-proof.png',
      mimeType: 'image/png',
      buffer: Buffer.from(
        'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAFgwJ/lFK6LwAAAABJRU5ErkJggg==',
        'base64'
      ),
    });
    await clickAndWait(this.page, () => this.page.getByTestId('delivery-mark-delivered').click());
  }

  async confirmAndDeliver() {
    this.page.once('dialog', dialog => dialog.accept());
    await clickAndWait(this.page, () => this.page.getByTestId('delivery-confirm').click());
    await clickAndWait(this.page, () => this.page.getByTestId('delivery-mark-delivering').click());

    await this.page.getByTestId('delivery-mark-delivered-open').click();
    await this.page.getByTestId('delivery-receiver-name').fill('E2E Receiver');
    await this.page.getByTestId('delivery-proof-file').setInputFiles({
      name: 'delivery-proof.png',
      mimeType: 'image/png',
      buffer: Buffer.from(
        'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAFgwJ/lFK6LwAAAABJRU5ErkJggg==',
        'base64'
      ),
    });
    await clickAndWait(this.page, () => this.page.getByTestId('delivery-mark-delivered').click());
  }
}

export class DirectShipDeliveryPage {
  constructor(private readonly page: Page) {}

  async confirm(deliveryNoteCode?: string) {
    await this.page.goto('/DirectShipDelivery/Pending');

    const row = deliveryNoteCode
      ? this.page.locator('tbody tr', { hasText: deliveryNoteCode }).first()
      : this.page.locator('tbody tr').first();

    await expect(row).toBeVisible();
    await row.getByTestId('directship-confirm-open').click();
    await clickAndWait(this.page, () => this.page.getByTestId('directship-confirm-submit').click());
  }
}

export class InventoryPage {
  constructor(private readonly page: Page) {}

  async validQuantities() {
    await this.page.goto('/Inventory/StockList');

    await expect(this.page.locator('#itemsTableBody tr')).toHaveCount(1);
  }
}
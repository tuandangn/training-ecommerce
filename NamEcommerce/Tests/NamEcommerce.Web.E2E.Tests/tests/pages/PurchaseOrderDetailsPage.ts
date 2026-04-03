import { Page, expect } from '@playwright/test';

export class PurchaseOrderDetailsPage {
  readonly page: Page;

  // Alerts
  readonly successAlert: any;
  readonly errorAlert: any;

  // Status & Actions
  readonly statusBadge: any;
  readonly submitForApprovalButton: any;
  readonly approveButton: any;
  readonly cancelOrderButton: any;

  // Add Item Section
  readonly addItemForm: any;
  readonly productSelect: any;
  readonly quantityInput: any;
  readonly unitCostInput: any;
  readonly itemNoteInput: any;
  readonly addItemButton: any;

  // Items Table
  readonly itemRows: any;
  readonly totalAmountText: any;

  constructor(page: Page) {
    this.page = page;
    this.successAlert = this.page.locator('div.alert-success');
    this.errorAlert = this.page.locator('div.alert-danger');

    this.statusBadge = this.page.locator('section.content-title span.badge');
    this.submitForApprovalButton = this.page.locator('form[action*="SubmitsPurchaseOrder"] button[type="submit"]');
    this.approveButton = this.page.locator('form[action*="ChangeStatus"] button.btn-secondary:has-text("Phê duyệt")');
    this.cancelOrderButton = this.page.locator('form[action*="ChangeStatus"] button.btn-light:has-text("Hủy đơn")');

    this.addItemForm = this.page.locator('form[action*="AddPurchaseOrderItem"]');
    this.productSelect = this.page.locator('select[name="ProductId"]');
    this.quantityInput = this.page.locator('input[name="Quantity"]');
    this.unitCostInput = this.page.locator('input[name="UnitCost"]');
    this.itemNoteInput = this.page.locator('input[name="Note"]');
    this.addItemButton = this.page.locator('button:has-text("Thêm hàng hóa")');

    this.itemRows = this.page.locator('table.table-bordered tbody tr');
    this.totalAmountText = this.page.locator('p.fs-5 span.text-primary');
  }

  async verifySuccessMessage(message?: string) {
    await expect(this.successAlert).toBeVisible();
    if (message) {
      await expect(this.successAlert).toContainText(message);
    }
  }

  async addItem(productValue: string, quantity: string, unitCost: string, note?: string) {
    await this.productSelect.selectOption(productValue);
    await this.quantityInput.fill(quantity);
    await this.unitCostInput.fill(unitCost);
    if (note) await this.itemNoteInput.fill(note);
    await this.addItemButton.click();
  }

  async verifyItemInList(productName: string) {
    const productNames = await this.itemRows.locator('td.ps-3 span.d-block').allTextContents();
    expect(productNames).toContain(productName);
  }

  async submitOrder() {
    await this.submitForApprovalButton.click();
  }

  async verifyStatus(statusLabel: string) {
    await expect(this.statusBadge).toContainText(statusLabel);
  }

  async getTotalAmount(): Promise<string | null> {
    return await this.totalAmountText.textContent();
  }
}

import { Page, expect } from '@playwright/test';

export class PurchaseOrderPage {
  readonly page: Page;

  // List Page Elements
  readonly createButton: any;
  readonly searchInput: any;
  readonly searchButton: any;
  readonly tableRows: any;

  // Create Page Elements
  readonly vendorSelect: any;
  readonly warehouseSelect: any;
  readonly expectedDeliveryDateInput: any;
  readonly noteTextarea: any;
  readonly submitButton: any;

  constructor(page: Page) {
    this.page = page;
    this.createButton = this.page.locator('a[href*="/PurchaseOrder/Create"]');
    this.searchInput = this.page.locator('input[name="Keywords"]');
    this.searchButton = this.page.locator('button#btnSearch');
    this.tableRows = this.page.locator('table tbody tr');

    this.vendorSelect = this.page.locator('select#VendorId');
    this.warehouseSelect = this.page.locator('select#WarehouseId');
    this.expectedDeliveryDateInput = this.page.locator('input#ExpectedDeliveryDate');
    this.noteTextarea = this.page.locator('textarea#Note');
    this.submitButton = this.page.locator('button[type="submit"]');
  }

  async goto() {
    await this.page.goto('/PurchaseOrder/List');
  }

  async clickCreate() {
    await this.createButton.click();
  }

  async fillCreateForm(expectedDate: string, note: string) {
    if (this.vendorSelect) await this.vendorSelect.selectOption({ index: 1 });
    if (this.warehouseSelect) await this.warehouseSelect.selectOption({ index: 1 });
    if (expectedDate) await this.expectedDeliveryDateInput.fill(expectedDate);
    if (note) await this.noteTextarea.fill(note);
    await this.submitButton.click();
  }

  async search(keywords: string) {
    await this.searchInput.fill(keywords);
    await this.searchButton.click();
  }

  async verifyFirstRowCode(code: string) {
    const firstRowCode = await this.tableRows.first().locator('td').first().textContent();
    expect(firstRowCode?.trim()).toContain(code);
  }
}

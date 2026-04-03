import { Page, expect } from '@playwright/test';

export class ProductPage {
  readonly page: Page;

  // List Page Elements
  readonly createButton: any;
  readonly searchInput: any;
  readonly searchButton: any;
  readonly tableRows: any;
  readonly successAlert: any;
  readonly confirmModalButton: any;

  // Create/Edit Form Elements
  readonly infoTab: any;
  readonly settingsTab: any;
  readonly nameInput: any;
  readonly categorySelect: any;
  readonly unitSelect: any;
  readonly shortDescTextarea: any;
  readonly displayOrderInput: any;
  readonly trackInventorySwitch: any;
  readonly submitButton: any;

  constructor(page: Page) {
    this.page = page;
    
    // List selectors
    this.createButton = this.page.locator('a[href*="/Product/Create"]');
    this.searchInput = this.page.locator('input[name="Keywords"]');
    this.searchButton = this.page.locator('button#btnSearch');
    this.tableRows = this.page.locator('table tbody tr');
    this.successAlert = this.page.locator('div.alert-success');
    this.confirmModalButton = this.page.locator('#confirmModal .btnConfirm');

    // Form selectors
    this.infoTab = this.page.locator('button#infoTab');
    this.settingsTab = this.page.locator('button#additionalTab');
    this.nameInput = this.page.locator('input#Name');
    this.categorySelect = this.page.locator('select#CategoryId');
    this.unitSelect = this.page.locator('select#UnitMeasurementId');
    this.shortDescTextarea = this.page.locator('textarea#ShortDesc');
    this.displayOrderInput = this.page.locator('input#DisplayOrder');
    this.trackInventorySwitch = this.page.locator('input#TrackInventory');
    this.submitButton = this.page.locator('button[type="submit"]');
  }

  async goto() {
    await this.page.goto('/Product/List');
  }

  async clickCreate() {
    await this.createButton.click();
  }

  async fillForm(name: string, categoryIndex: number = 1, unitIndex: number = 1, shortDesc: string = '', displayOrder: string = '0', trackInventory: boolean = true) {
    // Fill Info tab
    await this.infoTab.click();
    await this.nameInput.fill(name);
    if (categoryIndex > 0) {
      await this.categorySelect.selectOption({ index: categoryIndex });
    }
    if (unitIndex > 0) {
      await this.unitSelect.selectOption({ index: unitIndex });
    }
    if (shortDesc) {
      await this.shortDescTextarea.fill(shortDesc);
    }

    // Fill Settings tab
    await this.settingsTab.click();
    await this.displayOrderInput.fill(displayOrder);
    
    const isChecked = await this.trackInventorySwitch.isChecked();
    if (isChecked !== trackInventory) {
      await this.trackInventorySwitch.click();
    }

    // Submit
    await this.submitButton.click();
  }

  async search(keywords: string) {
    await this.searchInput.fill(keywords);
    await this.searchButton.click();
  }

  async verifyProductInList(name: string) {
    const names = await this.tableRows.locator('td.ps-3').allTextContents();
    const match = names.some(n => n.includes(name));
    expect(match).toBe(true);
  }

  async deleteProduct(name: string) {
    const row = this.tableRows.filter({ hasText: name });
    await row.locator('button.btnDelete').click();
    await expect(this.confirmModalButton).toBeVisible();
    await this.confirmModalButton.click();
  }

  async verifySuccessMessage(message?: string) {
    await expect(this.successAlert).toBeVisible();
    if (message) {
      await expect(this.successAlert).toContainText(message);
    }
  }
}

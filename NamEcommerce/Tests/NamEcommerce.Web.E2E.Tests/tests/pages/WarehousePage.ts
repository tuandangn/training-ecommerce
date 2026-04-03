import { Page, expect } from '@playwright/test';

export class WarehousePage {
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
  readonly codeInput: any;
  readonly addressInput: any;
  readonly phoneNumberInput: any;
  readonly warehouseTypeSelect: any;
  readonly isActiveSwitch: any;
  readonly submitButton: any;

  constructor(page: Page) {
    this.page = page;
    
    // List selectors
    this.createButton = this.page.locator('a[href*="/Warehouse/Create"]');
    this.searchInput = this.page.locator('input[name="Keywords"]');
    this.searchButton = this.page.locator('button#btnSearch');
    this.tableRows = this.page.locator('table tbody tr');
    this.successAlert = this.page.locator('div.alert-success');
    this.confirmModalButton = this.page.locator('#confirmModal .btnConfirm');

    // Form selectors
    this.infoTab = this.page.locator('button#infoTab');
    this.settingsTab = this.page.locator('button#additionalTab');
    this.nameInput = this.page.locator('input#Name');
    this.codeInput = this.page.locator('input#Code');
    this.addressInput = this.page.locator('input#Address');
    this.phoneNumberInput = this.page.locator('input#PhoneNumber');
    this.warehouseTypeSelect = this.page.locator('select#WarehouseType');
    this.isActiveSwitch = this.page.locator('input#IsActive');
    this.submitButton = this.page.locator('button[type="submit"]');
  }

  async goto() {
    await this.page.goto('/Warehouse/List');
  }

  async clickCreate() {
    await this.createButton.click();
  }

  async fillForm(name: string, code: string, address: string, phoneNumber: string, warehouseTypeIndex: number = 0, isActive: boolean = true) {
    // Fill Info tab
    await this.infoTab.click();
    await this.nameInput.fill(name);
    await this.codeInput.fill(code);
    await this.addressInput.fill(address);
    await this.phoneNumberInput.fill(phoneNumber);
    if (warehouseTypeIndex >= 0) {
      await this.warehouseTypeSelect.selectOption({ index: warehouseTypeIndex });
    }

    // Fill Settings tab
    await this.settingsTab.click();
    const isChecked = await this.isActiveSwitch.isChecked();
    if (isChecked !== isActive) {
      await this.isActiveSwitch.click();
    }

    // Submit
    await this.submitButton.click();
  }

  async search(keywords: string) {
    await this.searchInput.fill(keywords);
    await this.searchButton.click();
  }

  async verifyWarehouseInList(name: string) {
    const names = await this.tableRows.locator('td.ps-3').allTextContents();
    const match = names.some(n => n.includes(name));
    expect(match).toBe(true);
  }

  async deleteWarehouse(name: string) {
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

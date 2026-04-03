import { Page, expect } from '@playwright/test';

export class UnitMeasurementPage {
  readonly page: Page;

  // List Page Elements
  readonly createButton: any;
  readonly searchInput: any;
  readonly searchButton: any;
  readonly tableRows: any;
  readonly successAlert: any;
  readonly confirmModalButton: any;

  // Create/Edit Form Elements
  readonly nameInput: any;
  readonly descriptionTextarea: any;
  readonly submitButton: any;

  constructor(page: Page) {
    this.page = page;
    
    // List selectors
    this.createButton = this.page.locator('a[href*="/UnitMeasurement/Create"]');
    this.searchInput = this.page.locator('input[name="Keywords"]');
    this.searchButton = this.page.locator('button#btnSearch');
    this.tableRows = this.page.locator('table tbody tr');
    this.successAlert = this.page.locator('div.alert-success');
    this.confirmModalButton = this.page.locator('#confirmModal .btnConfirm');

    // Form selectors
    this.nameInput = this.page.locator('input#Name');
    this.descriptionTextarea = this.page.locator('textarea#Description');
    this.submitButton = this.page.locator('button[type="submit"]');
  }

  async goto() {
    await this.page.goto('/UnitMeasurement/List');
  }

  async clickCreate() {
    await this.createButton.click();
  }

  async fillForm(name: string, description: string = '') {
    await this.nameInput.fill(name);
    if (description) {
      await this.descriptionTextarea.fill(description);
    }
    await this.submitButton.click();
  }

  async search(keywords: string) {
    await this.searchInput.fill(keywords);
    await this.searchButton.click();
  }

  async verifyUnitInList(name: string) {
    const names = await this.tableRows.locator('td.ps-3').allTextContents();
    const match = names.some(n => n.includes(name));
    expect(match).toBe(true);
  }

  async deleteUnit(name: string) {
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

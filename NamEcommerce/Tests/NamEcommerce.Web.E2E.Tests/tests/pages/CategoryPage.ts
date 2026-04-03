import { Page, expect } from '@playwright/test';

export class CategoryPage {
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
  readonly parentSelect: any;
  readonly displayOrderInput: any;
  readonly submitButton: any;

  constructor(page: Page) {
    this.page = page;
    
    // List selectors
    this.createButton = this.page.locator('a[href*="/Category/Create"]');
    this.searchInput = this.page.locator('input[name="Keywords"]');
    this.searchButton = this.page.locator('button#btnSearch');
    this.tableRows = this.page.locator('table tbody tr');
    this.successAlert = this.page.locator('div.alert-success');
    this.confirmModalButton = this.page.locator('#confirmModal .btnConfirm');

    // Form selectors
    this.infoTab = this.page.locator('button#infoTab');
    this.settingsTab = this.page.locator('button#additionalTab');
    this.nameInput = this.page.locator('input#Name');
    this.parentSelect = this.page.locator('select#ParentId');
    this.displayOrderInput = this.page.locator('input#DisplayOrder');
    this.submitButton = this.page.locator('button[type="submit"]');
  }

  async goto() {
    await this.page.goto('/Category/List');
  }

  async clickCreate() {
    await this.createButton.click();
  }

  async fillForm(name: string, parentIndex: number = 0, displayOrder: string = '0') {
    // Fill Info tab
    await this.infoTab.click();
    await this.nameInput.fill(name);
    if (parentIndex > 0) {
      await this.parentSelect.selectOption({ index: parentIndex });
    }

    // Fill Settings tab
    await this.settingsTab.click();
    await this.displayOrderInput.fill(displayOrder);

    // Submit
    await this.submitButton.click();
  }

  async search(keywords: string) {
    await this.searchInput.fill(keywords);
    await this.searchButton.click();
  }

  async verifyCategoryInList(name: string) {
    const names = await this.tableRows.locator('td.ps-3').allTextContents();
    const match = names.some(n => n.includes(name));
    expect(match).toBe(true);
  }

  async deleteCategory(name: string) {
    // Find the row with the category name and click the delete button in that row
    const row = this.tableRows.filter({ hasText: name });
    await row.locator('button.btnDelete').click();

    // Handle the custom confirmation modal
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

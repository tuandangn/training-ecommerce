import { Page, expect } from '@playwright/test';

export class VendorPage {
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
  readonly contactPersonInput: any;
  readonly emailInput: any;
  readonly phoneNumberInput: any;
  readonly addressInput: any;
  readonly submitButton: any;

  constructor(page: Page) {
    this.page = page;
    
    // List selectors
    this.createButton = this.page.locator('a[href*="/Vendor/Create"]');
    this.searchInput = this.page.locator('input[name="Keywords"]');
    this.searchButton = this.page.locator('button#btnSearch');
    this.tableRows = this.page.locator('table tbody tr');
    this.successAlert = this.page.locator('div.alert-success');
    this.confirmModalButton = this.page.locator('#confirmModal .btnConfirm');

    // Form selectors
    this.nameInput = this.page.locator('input#Name');
    this.contactPersonInput = this.page.locator('input#ContactPerson');
    this.emailInput = this.page.locator('input#Email');
    this.phoneNumberInput = this.page.locator('input#PhoneNumber');
    this.addressInput = this.page.locator('input#Address');
    this.submitButton = this.page.locator('button[type="submit"]');
  }

  async goto() {
    await this.page.goto('/Vendor/List');
  }

  async clickCreate() {
    await this.createButton.click();
  }

  async fillForm(name: string, contactPerson: string = '', email: string = '', phoneNumber: string = '', address: string = '') {
    await this.nameInput.fill(name);
    if (contactPerson) await this.contactPersonInput.fill(contactPerson);
    if (email) await this.emailInput.fill(email);
    if (phoneNumber) await this.phoneNumberInput.fill(phoneNumber);
    if (address) await this.addressInput.fill(address);
    await this.submitButton.click();
  }

  async search(keywords: string) {
    await this.searchInput.fill(keywords);
    await this.searchButton.click();
  }

  async verifyVendorInList(name: string) {
    const names = await this.tableRows.locator('td.ps-3').allTextContents();
    const match = names.some(n => n.includes(name));
    expect(match).toBe(true);
  }

  async deleteVendor(name: string) {
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

import { expect, Page } from '@playwright/test';

export class LoginPage {
  constructor(private readonly page: Page) {}

  async goto() {
    await this.page.goto('/User/Login');
  }

  async login(username: string, password: string) {
    await this.page.locator('[data-testid="login-username"], input[name="Username"]').fill(username);
    await this.page.locator('[data-testid="login-password"], input[name="Password"]').fill(password);
    await this.page.locator('[data-testid="login-submit"], button[type="submit"]').click();
    await expect(this.page).not.toHaveURL(/\/User\/Login/i);
  }
}

import { Page, expect } from '@playwright/test';

export class HomePage {
  readonly page: Page;
  readonly bannerCarousel = this.page.locator('[data-test-id="banner-carousel"]');
  readonly productCards = this.page.locator('[data-test-id="product-card"]');

  constructor(page: Page) {
    this.page = page;
  }

  async goto() {
    await this.page.goto('/');
  }

  async verifyBannerVisible() {
    await expect(this.bannerCarousel).toBeVisible();
  }

  async verifyProductsLoaded(minCount: number = 1) {
    await expect(this.productCards).toHaveCount(minCount);
  }

  async takeHeroSnapshot() {
    // Capture screenshot of the hero section for visual regression
    await expect(this.page).toHaveScreenshot('hero.png');
  }
}

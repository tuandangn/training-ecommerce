import { test, expect } from '@playwright/test';
import { HomePage } from '../pages/HomePage';

test.describe('Home Page', () => {
  test('should display banner and products', async ({ page }) => {
    const home = new HomePage(page);
    await home.goto();
    await home.verifyBannerVisible();
    await home.verifyProductsLoaded();
    await home.takeHeroSnapshot();
  });
});

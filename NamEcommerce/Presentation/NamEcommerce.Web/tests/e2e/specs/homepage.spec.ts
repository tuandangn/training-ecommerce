import { test, expect } from '@playwright/test';
import { HomePage } from '../pages/HomePage';

test('Home page loads and displays banner and products', async ({ page }) => {
  const home = new HomePage(page);
  await home.goto();
  await home.verifyBannerVisible();
  await home.verifyProductsLoaded();
  await home.takeHeroSnapshot();
});

import { expect, Locator, Page } from '@playwright/test';

export async function selectPickerItem(page: Page, testId: string, inputSelector: string, text: string) {
  const root = page.getByTestId(testId);
  await root.locator(inputSelector).fill(text);

  const item = root.locator('.list-group-item-action', { hasText: text }).first();
  await expect(item).toBeVisible();
  await item.click();
}

export async function clickAndWait(page: Page, action: () => Promise<unknown>) {
  await action();
  await page.waitForLoadState('domcontentloaded');
}

export async function submitAndWaitForUrl(page: Page, button: Locator, url: RegExp) {
  await Promise.all([
    page.waitForURL(url),
    button.click(),
  ]);
}

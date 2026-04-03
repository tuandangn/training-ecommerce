import { test, expect } from '@playwright/test';
import { ProductPage } from '../pages/ProductPage';

test.describe('Product Management', () => {
  const productName = `E2E Product ${Date.now()}`;
  const updatedProductName = `${productName} Updated`;

  test('should create, edit, search, and delete a product', async ({ page }) => {
    const productPage = new ProductPage(page);

    // 1. Create
    await productPage.goto();
    await productPage.clickCreate();
    await productPage.fillForm(productName, 1, 1, 'Short description for E2E product', '10', true);

    // 2. Verify
    await productPage.verifySuccessMessage('Thêm mới hàng hóa thành công!');
    await productPage.verifyProductInList(productName);

    // 3. Edit
    await page.locator('tr', { hasText: productName }).locator('i.bi-pencil').click();
    await productPage.fillForm(updatedProductName, 1, 1, 'Updated short description', '20', false);
    await productPage.verifySuccessMessage('Chỉnh sửa hàng hóa thành công!');
    await productPage.verifyProductInList(updatedProductName);

    // 4. Search
    await productPage.search(updatedProductName);
    await productPage.verifyProductInList(updatedProductName);

    // 5. Delete
    await productPage.deleteProduct(updatedProductName);
    await productPage.verifySuccessMessage('Xóa hàng hóa thành công!');
    
    // 6. Verify gone
    await productPage.search(updatedProductName);
    await expect(productPage.tableRows.first()).toContainText('Chưa có dữ liệu');
  });
});

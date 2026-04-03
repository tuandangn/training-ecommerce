import { test, expect } from '@playwright/test';
import { VendorPage } from '../pages/VendorPage';

test.describe('Vendor Management', () => {
  const vendorName = `E2E Vendor ${Date.now()}`;
  const updatedVendorName = `${vendorName} Updated`;

  test('should create, edit, search, and delete a vendor', async ({ page }) => {
    const vendorPage = new VendorPage(page);

    // 1. Create
    await vendorPage.goto();
    await vendorPage.clickCreate();
    await vendorPage.fillForm(vendorName, 'E2E Contact', 'e2e@vendor.com', '0123456789', 'Vendor Address 123');

    // 2. Verify
    await vendorPage.verifySuccessMessage('Thêm mới nhà cung cấp thành công!');
    await vendorPage.verifyVendorInList(vendorName);

    // 3. Edit
    await page.locator('tr', { hasText: vendorName }).locator('i.bi-pencil').click();
    await vendorPage.fillForm(updatedVendorName, 'Updated Contact', 'updated@vendor.com', '0987654321', 'Updated Address 456');
    await vendorPage.verifySuccessMessage('Chỉnh sửa nhà cung cấp thành công!');
    await vendorPage.verifyVendorInList(updatedVendorName);

    // 4. Search
    await vendorPage.search(updatedVendorName);
    await vendorPage.verifyVendorInList(updatedVendorName);

    // 5. Delete
    await vendorPage.deleteVendor(updatedVendorName);
    await vendorPage.verifySuccessMessage('Xóa nhà cung cấp thành công!');
    
    // 6. Verify gone
    await vendorPage.search(updatedVendorName);
    await expect(vendorPage.tableRows.first()).toContainText('Chưa có dữ liệu');
  });
});

import { test, expect } from '@playwright/test';
import { WarehousePage } from '../pages/WarehousePage';

test.describe('Warehouse Management', () => {
  const warehouseName = `E2E Test Warehouse ${Date.now()}`;
  const updatedWarehouseName = `${warehouseName} Updated`;

  test('should create, edit, search, and delete a warehouse', async ({ page }) => {
    const warehousePage = new WarehousePage(page);

    // 1. Create
    await warehousePage.goto();
    await warehousePage.clickCreate();
    await warehousePage.fillForm(warehouseName, 'WH-E2E', '123 E2E Street', '0123456789', 0, true);

    // 2. Verify creation
    await warehousePage.verifySuccessMessage('Thêm mới nhà kho thành công!');
    await warehousePage.verifyWarehouseInList(warehouseName);

    // 3. Edit
    await page.locator('tr', { hasText: warehouseName }).locator('i.bi-pencil').click();
    await warehousePage.fillForm(updatedWarehouseName, 'WH-E2E-U', '456 Updated Ave', '0987654321', 0, false);
    await warehousePage.verifySuccessMessage('Chỉnh sửa nhà kho thành công!');
    await warehousePage.verifyWarehouseInList(updatedWarehouseName);

    // 4. Search
    await warehousePage.search(updatedWarehouseName);
    await warehousePage.verifyWarehouseInList(updatedWarehouseName);

    // 5. Delete
    await warehousePage.deleteWarehouse(updatedWarehouseName);
    await warehousePage.verifySuccessMessage('Xóa nhà kho thành công!');
    
    // 6. Verify gone
    await warehousePage.search(updatedWarehouseName);
    await expect(warehousePage.tableRows.first()).toContainText('Chưa có dữ liệu');
  });
});

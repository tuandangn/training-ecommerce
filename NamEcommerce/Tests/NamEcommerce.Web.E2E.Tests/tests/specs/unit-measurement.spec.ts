import { test, expect } from '@playwright/test';
import { UnitMeasurementPage } from '../pages/UnitMeasurementPage';

test.describe('Unit Measurement Management', () => {
  const unitName = `E2E Unit ${Date.now()}`;
  const updatedUnitName = `${unitName} Updated`;

  test('should create, edit, search, and delete a unit measurement', async ({ page }) => {
    const unitPage = new UnitMeasurementPage(page);

    // 1. Create
    await unitPage.goto();
    await unitPage.clickCreate();
    await unitPage.fillForm(unitName, 'Description for E2E unit');

    // 2. Verify
    await unitPage.verifySuccessMessage('Thêm mới đơn vị tính thành công!');
    await unitPage.verifyUnitInList(unitName);

    // 3. Edit
    await page.locator('tr', { hasText: unitName }).locator('i.bi-pencil').click();
    await unitPage.fillForm(updatedUnitName, 'Updated description');
    await unitPage.verifySuccessMessage('Chỉnh sửa đơn vị tính thành công!');
    await unitPage.verifyUnitInList(updatedUnitName);

    // 4. Search
    await unitPage.search(updatedUnitName);
    await unitPage.verifyUnitInList(updatedUnitName);

    // 5. Delete
    await unitPage.deleteUnit(updatedUnitName);
    await unitPage.verifySuccessMessage('Xóa đơn vị tính thành công!');
    
    // 6. Verify gone
    await unitPage.search(updatedUnitName);
    await expect(unitPage.tableRows.first()).toContainText('Chưa có dữ liệu');
  });
});

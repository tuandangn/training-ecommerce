import { test, expect } from '@playwright/test';
import { CategoryPage } from '../pages/CategoryPage';

test.describe('Category Management', () => {
  const categoryName = `E2E Test Category ${Date.now()}`;
  const updatedCategoryName = `${categoryName} Updated`;

  test('should create, edit, and delete a category', async ({ page }) => {
    const categoryPage = new CategoryPage(page);

    // 1. Create a new category
    await categoryPage.goto();
    await categoryPage.clickCreate();
    await categoryPage.fillForm(categoryName, 0, '10');

    // 2. Verify success and presence in list
    await categoryPage.verifySuccessMessage('Thêm mới danh mục thành công!');
    await categoryPage.verifyCategoryInList(categoryName);

    // 3. Edit the category
    const row = categoryPage.tableRows.filter({ hasText: categoryName });
    await row.locator('a.btn-pencil').click(); // Using pencil icon btn-link class name or index

    // Note: I'll use the proper selector for the edit link
    await categoryPage.page.locator(`a[href*="/Category/Edit/"]:has-text("Edit")`).filter({ has: categoryPage.page.locator('xpath=..//..', { hasText: categoryName }) }).click();
    
    // Wait, the edit link does not have text "Edit", it's a pencil icon.
    // Let's refine the selector in the spec.
    await page.locator('tr', { hasText: categoryName }).locator('i.bi-pencil').click();
    
    await categoryPage.fillForm(updatedCategoryName, 0, '20');
    await categoryPage.verifySuccessMessage('Chỉnh sửa danh mục thành công!');
    await categoryPage.verifyCategoryInList(updatedCategoryName);

    // 4. Search for the category
    await categoryPage.search(updatedCategoryName);
    await categoryPage.verifyCategoryInList(updatedCategoryName);

    // 5. Delete the category
    await categoryPage.deleteCategory(updatedCategoryName);
    await categoryPage.verifySuccessMessage('Xóa danh mục thành công!');
    
    // 6. Verify it's gone
    await categoryPage.search(updatedCategoryName);
    const noDataText = await categoryPage.tableRows.first().textContent();
    expect(noDataText?.trim()).toContain('Chứa có dữ liệu'); // From List.cshtml: "Chưa có dữ liệu"
  });
});

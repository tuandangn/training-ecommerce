import { test, expect } from '@playwright/test';
import { PurchaseOrderPage } from '../pages/PurchaseOrderPage';
import { PurchaseOrderDetailsPage } from '../pages/PurchaseOrderDetailsPage';

test.describe('Purchase Order Management', () => {
    
    test('should create a new purchase order and add an item', async ({ page }) => {
        const poPage = new PurchaseOrderPage(page);
        const poDetailsPage = new PurchaseOrderDetailsPage(page);

        // 1. Navigate to PO List and click Create
        await poPage.goto();
        await poPage.clickCreate();

        // 2. Fill Create Form (using first available vendor/warehouse)
        const expectedDate = new Date();
        expectedDate.setDate(expectedDate.getDate() + 7);
        const dateString = expectedDate.toISOString().split('T')[0];
        
        await poPage.fillCreateForm(dateString, 'E2E Test Purchase Order');

        // 3. Verify redirection to Details and success message
        await poDetailsPage.verifySuccessMessage('Tạo đơn nhập hàng thành công!');
        await poDetailsPage.verifyStatus('Nháp');

        // 4. Add an Item to the PO
        await poDetailsPage.addItem('1', '10', '150000', 'Test item note');

        // 5. Verify item added and success message
        await poDetailsPage.verifySuccessMessage('Thêm sản phẩm thành công!');
        
        // Note: verifyItemInList might vary based on the actual product name of index 1
        // For now, we just check if any row exists
        await expect(poDetailsPage.itemRows).toHaveCount(1);

        // 6. Verify Total Amount is updated (not 0 đ)
        const totalAmount = await poDetailsPage.getTotalAmount();
        expect(totalAmount).not.toBe('0 đ');
    });

    test('should search for an existing purchase order', async ({ page }) => {
        const poPage = new PurchaseOrderPage(page);

        await poPage.goto();
        
        // Get the code of the first PO in the list
        const firstRowCode = await poPage.tableRows.first().locator('td').first().textContent();
        if (firstRowCode) {
            const code = firstRowCode.trim();
            await poPage.search(code);
            await poPage.verifyFirstRowCode(code);
        }
    });
});

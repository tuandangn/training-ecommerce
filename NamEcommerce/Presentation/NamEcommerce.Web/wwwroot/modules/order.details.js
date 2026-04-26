import { confirm, toast } from "/modules/modals.js";
import { apiPost } from "/modules/ajax-helper.js";
import PromiseModal from "/modules/PromiseModal.js";
import ProductPicker from "/modules/ProductPicker.js";

/**
 * Submit a form via apiPost.
 * Sends body as FormData (server vẫn nhận form-binding như trước),
 * auto attach antiforgery token, auto-render notification nếu server
 * trả `JsonNotificationResult`.
 *
 * @param {HTMLFormElement} form
 * @returns {Promise<{success:boolean, message?:string}>}
 */
async function submitFormAsync(form) {
    const formData = new FormData(form);
    return apiPost(form.action, formData);
}

(function () {
    // ─── Add item via modal (matching Create Order pattern) ───────────────────

    const addProductModalEl = document.getElementById('addProductModal');
    if (addProductModalEl) {
        const productPickerEl = document.getElementById('productPicker');
        const productPicker = new ProductPicker(productPickerEl);
        const addProductModal = new PromiseModal('#addProductModal');

        const addProductId = document.getElementById('addProductId');
        const addProductQuantity = document.getElementById('addProductQuantity');
        const addProductUnitPrice = document.getElementById('addProductUnitPrice');
        const modalProductInfo = document.getElementById('modalProductInfo');
        const addItemBtn = document.getElementById('addItemToTable');

        // Product selected → show quantity/price fields
        productPickerEl.addEventListener('select', (e) => {
            const product = e.detail?.product;
            if (product) {
                console.log(product);
                addProductId.value = product.id;
                addProductQuantity.value = 1;
                addProductUnitPrice.value = product.price || 0;
                modalProductInfo.classList.remove('d-none');
                addItemBtn.classList.remove('d-none');
            }
        });

        // Product removed → hide fields
        productPickerEl.addEventListener('remove', () => {
            addProductId.value = '';
            modalProductInfo.classList.add('d-none');
            addItemBtn.classList.add('d-none');
        });

        // Reset modal state when hidden
        addProductModalEl.addEventListener('show.bs.modal', () => {
            productPicker.clear();
            modalProductInfo.classList.add('d-none');
            addItemBtn.classList.add('d-none');
            addProductId.value = '';
            addProductQuantity.value = 1;
            addProductUnitPrice.value = '';
        });

        // Submit add item via AJAX
        const addProductForm = document.getElementById('addProductForm');
        addProductForm?.addEventListener('submit', async function (e) {
            e.preventDefault();
            if (!$(addProductForm).valid()) return;

            await addProductModal.hide();

            try {
                const result = await apiPost('/Order/AddOrderItem', new FormData(this));

                if (result.success) {
                    await toast('Thành công', 'Đã thêm hàng hóa vào đơn hàng.', 'success');
                    location.reload();
                } else {
                    toast('Lỗi', result.message || 'Không thể thêm hàng hóa vào đơn hàng.', 'error');
                }
            } catch (err) {
                toast('Lỗi', 'Có lỗi xảy ra khi gửi yêu cầu.', 'error');
            }
        });
    }

    // ─── Edit item ────────────────────────────────────────────────────────────

    const editItemModal = document.getElementById('editItemModal');
    if (editItemModal) {
        const editModal = new PromiseModal('#editItemModal');

        $('.btnEditItem').on('click', function () {
            const data = $(this).data();
            $('#editItemId').val(data.id);
            $('#editProductName').text(data.product);
            $('#editQuantity').val(data.qty);
            $('#editUnitPrice').val(data.price);
            $('#editProductStock').text(data.availableqty);

            var $img = $(this).closest('tr').find('.item-product-img');
            var pictureUrl = $img.attr('src');
            if (pictureUrl) {
                $('#editProductHasPicture').attr('src', pictureUrl).removeClass('d-none');
                $('#editProductNoPicture').addClass('d-none')
            } else {
                $('#editProductHasPicture').attr('src', '').addClass('d-none');
                $('#editProductNoPicture').removeClass('d-none')
            }

            editModal.show();
        });

        $('#editItemForm').on('submit', async function (e) {
            e.preventDefault();

            if (!$(this).valid())
                return;

            await editModal.hide();

            try {
                const result = await submitFormAsync(this);

                if (result.success) {
                    await toast('Thành công', 'Đã cập nhật hàng hóa của đơn hàng.', 'success');
                    location.reload();
                } else {
                    toast('Lỗi', result.message || 'Không thể cập nhật hàng hóa của đơn hàng.', 'error');
                }
            } catch (err) {
                toast('Lỗi', 'Có lỗi xảy ra khi gửi yêu cầu.', 'error');
            }
        });
    }

    // ─── Remove item ──────────────────────────────────────────────────────────

    $('.btnRemoveItem').on('click', async function () {
        const id = $(this).data('id');
        const name = $(this).data('name');
        const orderId = $(this).data('order');

        const confirmed = await confirm('Xác nhận xóa', `Bạn có chắc chắn muốn xóa <strong>${name}</strong> khỏi đơn hàng không?`);
        if (confirmed) {
            try {
                // Gửi qua FormData để server tiếp tục dùng default model-binding
                // (controller không dùng [FromBody]).
                const formData = new FormData();
                formData.append('OrderId', orderId);
                formData.append('ItemId', id);
                const result = await apiPost('/Order/RemoveOrderItem', formData);
                if (result.success) {
                    location.reload();
                } else {
                    toast('Lỗi', result.message || 'Không thể xóa hàng hóa khỏi đơn hàng.', 'error');
                }
            } catch (err) {
                toast('Lỗi', 'Có lỗi xảy ra khi gửi yêu cầu.', 'error');
            }
        }
    });

    // ─── Lock ─────────────────────────────────────────────────────────────────

    $('#lockForm').on('submit', async function (e) {
        e.preventDefault();

        if (!$(this).valid())
            return;

        const lockModal = new PromiseModal('#lockModal');
        await lockModal.hide();

        try {
            const result = await submitFormAsync(this);

            if (result.success) {
                await toast('Thành công', 'Đã khóa đơn hàng này.', 'success');
                location.reload();
            } else {
                toast('Lỗi', result.message || 'Không thể khóa đơn hàng.', 'error');
            }
        } catch (err) {
            toast('Lỗi', 'Có lỗi xảy ra khi gửi yêu cầu.', 'error');
        }
    });

    // ─── Discount ─────────────────────────────────────────────────────────────

    const editDiscountModalElement = document.getElementById('editDiscountModal');
    if (editDiscountModalElement) {
        $('#editDiscountForm').on('submit', async function (e) {
            e.preventDefault();

            if (!$(this).valid())
                return;

            const editDiscountModal = new PromiseModal('#editDiscountModal');
            await editDiscountModal.hide();

            try {
                const result = await submitFormAsync(this);

                if (result.success) {
                    await toast('Thành công', 'Đã cập nhật giảm giá cho đơn hàng.', 'success');
                    location.reload();
                } else {
                    toast('Lỗi', result.message || 'Không thể cập nhật giảm giá cho đơn hàng.', 'error');
                }
            } catch (err) {
                toast('Lỗi', 'Có lỗi xảy ra khi gửi yêu cầu.', 'error');
            }
        });
    }

    // ─── Edit note ────────────────────────────────────────────────────────────

    const changeNoteModalElement = document.getElementById('changeNoteModal');
    if (changeNoteModalElement) {
        changeNoteModalElement.addEventListener('shown.bs.modal', () => $('#orderNote').focusEnd());

        $('#changeNoteForm').on('submit', async function (e) {
            e.preventDefault();

            if (!$(this).valid())
                return;

            const changeNoteModal = new PromiseModal('#changeNoteModal');
            await changeNoteModal.hide();

            var note = $(this).find('[name="Note"]');

            try {
                const result = await submitFormAsync(this);

                if (result.success) {
                    if (note)
                        await toast('Thành công', 'Đã cập nhật ghi chú đơn hàng.', 'success');
                    else
                        await toast('Thành công', 'Đã xóa ghi chú đơn hàng.', 'success');
                    location.reload();
                } else {
                    toast('Lỗi', result.message || 'Không thể cập nhật ghi chú đơn hàng.', 'error');
                }
            } catch (err) {
                toast('Lỗi', 'Có lỗi xảy ra khi gửi yêu cầu.', 'error');
            }
        });
    }

    // ─── Shipping ─────────────────────────────────────────────────────────────

    const shippingModalElement = document.getElementById('shippingModal');
    if (shippingModalElement) {
        $('#shippingForm').on('submit', async function (e) {
            e.preventDefault();

            if (!$(this).valid())
                return;

            const shippingModal = new PromiseModal('#shippingModal');
            await shippingModal.hide();

            try {
                const result = await submitFormAsync(this);

                if (result.success) {
                    await toast('Thành công', 'Đã cập nhật thông tin giao hàng.', 'success');
                    location.reload();
                } else {
                    toast('Lỗi', result.message || 'Không thể cập nhật thông tin giao hàng.', 'error');
                }
            } catch (err) {
                toast('Lỗi', 'Có lỗi xảy ra khi gửi yêu cầu.', 'error');
            }
        });
    }

    // ─── Create delivery note ──────────────────────────────────────────────────

    const createDeliveryNoteModalEl = document.getElementById('createDeliveryNoteModal');
    if (createDeliveryNoteModalEl) {
        const selectAllCheckbox = document.getElementById('selectAllItems');
        const itemCheckboxes = document.querySelectorAll('.item-checkbox');
        const createDeliveryNoteBtn = document.getElementById('createDeliveryNoteBtn');

        // Select all functionality
        selectAllCheckbox?.addEventListener('change', (e) => {
            itemCheckboxes.forEach(checkbox => {
                // Don't check disabled checkboxes
                if (!checkbox.disabled) {
                    checkbox.checked = e.target.checked;
                    // Enable/disable the quantity input
                    const row = checkbox.closest('tr');
                    const qtyInput = row?.querySelector('.item-qty');
                    if (qtyInput) {
                        qtyInput.disabled = !e.target.checked;
                    }
                }
            });
        });

        // Individual checkbox change
        itemCheckboxes.forEach(checkbox => {
            checkbox.addEventListener('change', (e) => {
                const row = e.target.closest('tr');
                const qtyInput = row?.querySelector('.item-qty');
                if (qtyInput) {
                    qtyInput.disabled = !e.target.checked;
                    if (e.target.checked) {
                        const remainingQty = parseFloat(row.dataset.remainingQty || 0);
                        qtyInput.value = remainingQty.toString();
                        qtyInput.focus();
                    }
                }

                // Update select all checkbox state
                const enabledCheckboxes = Array.from(itemCheckboxes).filter(cb => !cb.disabled);
                const allChecked = enabledCheckboxes.length > 0 && enabledCheckboxes.every(cb => cb.checked);
                const someChecked = enabledCheckboxes.some(cb => cb.checked);
                if (selectAllCheckbox) {
                    selectAllCheckbox.checked = allChecked;
                    selectAllCheckbox.indeterminate = someChecked && !allChecked;
                }
            });

            // Validate quantity input
            checkbox.closest('tr')?.querySelector('.item-qty')?.addEventListener('change', (e) => {
                const row = e.target.closest('tr');
                const maxQty = parseFloat(e.target.max || 0);
                const value = parseFloat(e.target.value || 0);
                const errorEl = row?.querySelector('.qty-error');

                if (value > maxQty) {
                    e.target.value = maxQty.toString();
                    if (errorEl) {
                        errorEl.textContent = `Số lượng tối đa là ${maxQty}`;
                        errorEl.classList.remove('d-none');
                    }
                } else if (value > 0) {
                    if (errorEl) {
                        errorEl.classList.add('d-none');
                    }
                }
            });
        });

        // Create delivery note
        createDeliveryNoteBtn?.addEventListener('click', async () => {
            const selectedItems = Array.from(itemCheckboxes)
                .filter(cb => cb.checked)
                .map(cb => {
                    const row = cb.closest('tr');
                    const qtyInput = row?.querySelector('.item-qty');
                    const qty = parseFloat(qtyInput?.value || 0);
                    const maxQty = parseFloat(qtyInput?.max || 0);

                    return {
                        orderItemId: cb.dataset.itemId,
                        quantity: qty,
                        maxAvailable: maxQty
                    };
                });

            if (selectedItems.length === 0) {
                await toast('Cảnh báo', 'Vui lòng chọn ít nhất một hàng hóa để tạo phiếu xuất.', 'warning');
                return;
            }

            // Validate quantities
            for (const item of selectedItems) {
                if (item.quantity <= 0) {
                    await toast('Lỗi', 'Số lượng xuất phải lớn hơn 0.', 'error');
                    return;
                }

                if (item.quantity > item.maxAvailable) {
                    await toast('Lỗi', `Số lượng xuất không được vượt quá ${item.maxAvailable} cho item này.`, 'error');
                    return;
                }
            }

            try {
                const orderId = document.querySelector('[name="OrderId"]')?.value;
                const result = await apiPost('/DeliveryNote/CreateFromPreparation', {
                    orderId: orderId,
                    selectedItems: selectedItems.map(item => ({
                        orderItemId: item.orderItemId,
                        quantity: item.quantity
                    }))
                });

                if (result.success) {
                    const createModal = new PromiseModal('#createDeliveryNoteModal');
                    await createModal.hide();
                    await toast('Thành công', 'Đã tạo phiếu xuất kho thành công.', 'success');
                    location.reload();
                } else {
                    await toast('Lỗi', result.message || 'Không thể tạo phiếu xuất kho.', 'error');
                }
            } catch (err) {
                await toast('Lỗi', 'Có lỗi xảy ra khi gửi yêu cầu.', 'error');
            }
        });

        // Reset modal state when shown
        createDeliveryNoteModalEl.addEventListener('show.bs.modal', () => {
            itemCheckboxes.forEach(checkbox => {
                checkbox.checked = false;
                const row = checkbox.closest('tr');
                const qtyInput = row?.querySelector('.item-qty');
                if (qtyInput) {
                    qtyInput.disabled = true;
                }
            });
            if (selectAllCheckbox) {
                selectAllCheckbox.checked = false;
                selectAllCheckbox.indeterminate = false;
            }
        });
    }
})();

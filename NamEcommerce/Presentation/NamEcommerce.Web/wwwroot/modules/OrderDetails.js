import { confirm, toast } from "/modules/modals.js";
import { apiGet, apiPost } from "/modules/ajax-helper.js";
import DeliveryNoteController from "/modules/DeliveryNoteController.js";
import PromiseModal from "/modules/PromiseModal.js";
import ProductPicker from "/modules/ProductPicker.js";

async function submitFormAsync(form) {
    const formData = new FormData(form);

    const decimalFields = form.querySelectorAll('.decimal-input');
    for (const decimalField of decimalFields) {
        const decimals = Number(decimalField.dataset.decimals) ?? 0;
        formData.set(decimalField.name, DecimalFields.stripFormatting(decimalField.value, decimals))
    }

    return await apiPost(form.action, formData);
}

function parseDecimalInput(input) {
    if (!input) return 0;
    return DecimalFields.getValue(input);
}

(function () {
    document.querySelectorAll('[data-workflow-target]').forEach((button) => {
        button.addEventListener('click', () => {
            const target = button.dataset.workflowTarget;
            document.querySelectorAll('[data-workflow-target]').forEach((step) => {
                const isActive = step === button;
                step.classList.toggle('active', isActive);
                step.setAttribute('aria-selected', isActive ? 'true' : 'false');
            });
            document.querySelectorAll('[data-workflow-panel]').forEach((panel) => {
                panel.classList.toggle('active', panel.dataset.workflowPanel === target);
            });
        });
    });

    const deliveryNoteController = new DeliveryNoteController();
    document.querySelectorAll('[data-delivery-action]').forEach((button) => {
        button.addEventListener('click', async () => {
            const id = button.dataset.deliveryId;
            const action = button.dataset.deliveryAction;
            if (!id || !action) return;

            try {
                let result;
                if (action === 'confirm') {
                    result = await deliveryNoteController.confirm(id);
                } else if (action === 'delivering') {
                    result = await deliveryNoteController.delivering(id);
                } else if (action === 'cancel') {
                    result = await deliveryNoteController.cancel(id);
                } else if (action === 'delivered') {
                    await deliveryNoteController.beginDeliveredWorkflow('markDeliveredModal').setId(id);
                    return;
                } else if (action === 'reject') {
                    await deliveryNoteController
                        .beginRejectWorkflow('rejectDeliveryNoteModal')
                        .setData(id, button.dataset.deliveryCode || '', button.dataset.deliveryWarehouseId || '');
                    return;
                }
                if (result?.success) {
                    showPageLoading();
                    location.reload();
                }
            } catch (err) {
                hidePageLoading();
                toast('Lỗi', 'Không thể cập nhật phiếu giao.', 'error');
            }
        });
    });

    const addProductModalEl = document.getElementById('addProductModal');
    if (addProductModalEl) {
        const productPickerEl = document.getElementById('productPicker');
        const productPicker = new ProductPicker(productPickerEl, {
            checkProduct(product) {
                return product.availableQty > 0 || product.vendorCount > 0
            }
        });
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
                addProductId.value = product.id;
                const dpInput = document.getElementById('addProductDecimalPlaces');
                if (dpInput) dpInput.value = product.quantityDecimalPlaces ?? 0;
                addProductQuantity.value = 1;
                addProductQuantity.dataset.decimals = String(product.quantityDecimalPlaces ?? 0);
                addProductQuantity.dataset.decimalBound = '';
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
            window.DecimalFields?.wrapExistingInput?.(addProductQuantity);
        });

        // Submit add item via AJAX
        const addProductForm = document.getElementById('addProductForm');
        addProductForm?.addEventListener('submit', async function (e) {
            e.preventDefault();
            if (!$(addProductForm).valid()) return;

            showPageLoading();
            addProductModal.hide();

            try {
                const result = await apiPost('/Order/AddOrderItem', DecimalFields.getFormData(this));
                if (result.success) {
                    location.reload();
                } else {
                    hidePageLoading();
                    toast('Lỗi', result.message || 'Không thể thêm hàng hóa vào đơn hàng.', 'error');
                }
            } catch (err) {
                hidePageLoading();
                toast('Lỗi', 'Có lỗi xảy ra khi gửi yêu cầu.', 'error');
            }
        });
    }

    const editItemModal = document.getElementById('editItemModal');
    if (editItemModal) {
        const editModal = new PromiseModal('#editItemModal');

        $('.btnEditItem').on('click', function () {
            const data = $(this).data();
            $('#editItemId').val(data.id);
            $('#editProductName').text(data.product);
            const editQtyEl = document.getElementById('editQuantity');
            const dp = data.decimalplaces;
            if (editQtyEl) {
                editQtyEl.dataset.decimals = String(dp);
                window.DecimalFields?.wrapExistingInput?.(editQtyEl);
            }
            const editDpInput = document.getElementById('editDecimalPlaces');
            if (editDpInput) editDpInput.value = dp;
            $('#editQuantity').val(this.dataset.qty);
            $('#editUnitPrice').val(this.dataset.price);
            if (data.availableqty > 0) {
                $('#editProductStock').find('.stock-field').text('Tồn kho: ' + DecimalFields.formatQuantity(Math.max(0, data.availableqty), data.decimalplaces ?? 0));
            } else {
                $('#editProductStock').find('.stock-field').html('<span class="text-danger">Hết hàng</span>');
            }

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

            editModal.hide();
            showPageLoading();

            try {
                const result = await submitFormAsync(this);
                if (result.success) {
                    location.reload();
                } else {
                    hidePageLoading();
                    toast('Lỗi', result.message || 'Không thể cập nhật hàng hóa của đơn hàng.', 'error');
                }
            } catch (err) {
                hidePageLoading();
                toast('Lỗi', 'Có lỗi xảy ra khi gửi yêu cầu.', 'error');
            }
        });
    }

    $('.btnRemoveItem').on('click', async function () {
        const id = $(this).data('id');
        const name = $(this).data('name');
        const orderId = $(this).data('order');

        const confirmed = await confirm('Xác nhận xóa', `Bạn có chắc chắn muốn xóa <strong>${name}</strong> khỏi đơn hàng không?`);
        if (confirmed) {
            showPageLoading();
            try {
                const formData = new FormData();
                formData.append('OrderId', orderId);
                formData.append('ItemId', id);
                const result = await apiPost('/Order/RemoveOrderItem', formData);
                if (result.success) {
                    location.reload();
                } else {
                    hidePageLoading();
                    toast('Lỗi', result.message || 'Không thể xóa hàng hóa khỏi đơn hàng.', 'error');
                }
            } catch (err) {
                hidePageLoading();
                toast('Lỗi', 'Có lỗi xảy ra khi gửi yêu cầu.', 'error');
            }
        }
    });

    document.getElementById('btnCompleteOrder')?.addEventListener('click', async (event) => {
        const orderId = event.currentTarget.dataset.orderId;
        const confirmed = await confirm('Hoàn thành đơn', 'Xác nhận đã kiểm tra giao hàng, công nợ và chi phí?');
        if (!confirmed) return;

        showPageLoading();
        try {
            const formData = new FormData();
            formData.append('OrderId', orderId);
            const result = await apiPost('/Order/CompleteOrder', formData);
            if (result.success) {
                location.reload();
            } else {
                hidePageLoading();
                toast('Lỗi', result.message || 'Không thể hoàn thành đơn.', 'error');
            }
        } catch (err) {
            hidePageLoading();
            toast('Lỗi', 'Có lỗi xảy ra khi gửi yêu cầu.', 'error');
        }
    });

    const orderSettlementPaymentForm = document.getElementById('orderSettlementPaymentForm');
    if (orderSettlementPaymentForm) {
        const settlementModal = new PromiseModal('#orderSettlementPaymentModal');
        const amountInput = document.getElementById('orderSettlementPaymentAmount');
        const paidOnInput = document.getElementById('orderSettlementPaymentPaidOn');
        const noteInput = document.getElementById('orderSettlementPaymentNote');
        const submitButton = document.getElementById('orderSettlementPaymentSubmit');
        const qrPanel = document.getElementById('orderSettlementQrPanel');
        const qrImage = document.getElementById('orderSettlementQrImage');
        const qrReference = document.getElementById('orderSettlementQrReference');
        const qrStatus = document.getElementById('orderSettlementQrStatus');
        const qrExpires = document.getElementById('orderSettlementQrExpires');
        const qrManualConfirm = document.getElementById('orderSettlementQrManualConfirm');
        let paymentIntent = null;
        let statusTimer = null;

        const orderId = orderSettlementPaymentForm.querySelector('[name="OrderId"]')?.value;
        const customerId = orderSettlementPaymentForm.querySelector('[name="CustomerId"]')?.value;
        const isQrMode = () => orderSettlementPaymentForm.querySelector('[name="PaymentMode"]:checked')?.value === 'qr';
        const isIntentPending = (intent) => intent?.status === 10;
        const isIntentConfirmed = (intent) => intent?.status === 20 || intent?.status === 30;
        const getStatusText = (status) => ({
            10: 'Đang chờ chuyển khoản',
            20: 'Đã xác nhận',
            30: 'Đã xác nhận thủ công',
            40: 'Đã hết hạn',
            50: 'Đã hủy',
            60: 'Đã ghi nhận'
        })[status] || 'Không xác định';

        const stopPolling = () => {
            if (!statusTimer) return;

            window.clearTimeout(statusTimer);
            statusTimer = null;
        };

        const renderQrIntent = () => {
            if (!qrPanel) return;

            qrPanel.classList.toggle('d-none', !paymentIntent);
            if (!paymentIntent) {
                if (qrImage) qrImage.removeAttribute('src');
                if (qrReference) qrReference.textContent = '';
                if (qrStatus) qrStatus.textContent = '';
                if (qrExpires) qrExpires.textContent = '';
                if (qrManualConfirm) qrManualConfirm.disabled = true;
                return;
            }

            if (qrImage) qrImage.src = paymentIntent.qrImageUrl || '';
            if (qrReference) qrReference.textContent = paymentIntent.referenceCode || '';
            if (qrStatus) qrStatus.textContent = `Trạng thái: ${getStatusText(paymentIntent.status)}`;
            if (qrExpires) {
                qrExpires.textContent = paymentIntent.expiresAtUtc
                    ? `Hết hạn: ${new Date(paymentIntent.expiresAtUtc).toLocaleString('vi-VN')}`
                    : '';
            }
            if (qrManualConfirm) qrManualConfirm.disabled = !isIntentPending(paymentIntent);
        };

        const resetQrIntent = () => {
            paymentIntent = null;
            stopPolling();
            renderQrIntent();
            if (submitButton) {
                submitButton.classList.remove('d-none');
                submitButton.disabled = false;
            }
        };

        const refreshIntentStatus = async () => {
            if (!paymentIntent) {
                stopPolling();
                return;
            }

            try {
                const params = new URLSearchParams({ intentId: paymentIntent.id });
                const result = await apiGet(`${orderSettlementPaymentForm.dataset.statusUrl}?${params.toString()}`);
                if (!result.success || result.intent?.id !== paymentIntent.id) {
                    stopPolling();
                    return;
                }

                paymentIntent = result.intent;
                renderQrIntent();
                if (isIntentConfirmed(paymentIntent)) {
                    stopPolling();
                    await consumesPaymentIntent();
                }
            } catch {
                stopPolling();
                toast('Lỗi', 'Không thể cập nhật trạng thái QR.', 'error');
            }
        };

        const startPolling = () => {
            stopPolling();
            if (!isIntentPending(paymentIntent))
                return;

            handlePolling();

            async function handlePolling() {
                await refreshIntentStatus();
                statusTimer = window.setTimeout(handlePolling, 4000);
            }
        };

        settlementModal.element?.addEventListener('hidden.bs.modal', () => {
            $('[name="PaymentMode"][value="cash"]').prop('checked', true);
            paymentModeChanged();
            isPaidOnValid();
        });
        settlementModal.element?.addEventListener('shown.bs.modal', () => {
            amountInput.focus();
            amountInput.select();
        });
        orderSettlementPaymentForm.querySelectorAll('[name="PaymentMode"]').forEach((checkbox) => {
            checkbox.addEventListener('change', () => {
                paymentModeChanged();
            });
        });
        paidOnInput.addEventListener('change', () => {
            isPaidOnValid();
        });

        orderSettlementPaymentForm.addEventListener('submit', async (e) => {
            e.preventDefault();
            if (!isFormValid())
                return;

            const amount = parseDecimalInput(amountInput);
            if (!amount || amount <= 0) {
                toast('Lỗi', 'Số tiền thanh toán phải lớn hơn 0.', 'error');
                return;
            }

            if (isQrMode()) {
                if (amount !== Math.trunc(amount)) {
                    toast('Lỗi', 'Số tiền QR phải là VND nguyên.', 'error');
                    return;
                }

                if (submitButton) submitButton.disabled = true;
                try {
                    const result = await apiPost(orderSettlementPaymentForm.dataset.createIntentUrl, {
                        customerId,
                        amount,
                        note: noteInput?.value || null
                    });
                    if (!result.success) {
                        if (submitButton)
                            submitButton.disabled = false;
                        return;
                    }

                    submitButton?.classList.add('d-none');
                    qrManualConfirm?.classList.remove('d-none');
                    paymentIntent = result.intent;
                    renderQrIntent();
                    startPolling();
                } catch {
                    toast('Lỗi', 'Không thể tạo QR chuyển khoản.', 'error');
                }
                return;
            }

            settlementModal.element?.classList.add('d-none');
            const confirmed = await confirm('Đã nhận tiền', `Bạn xác nhận đã nhận đủ ${DecimalFields.formatCurrencyWithSymbol(amount)}`);
            if (!confirmed) {
                settlementModal.element?.classList.remove('d-none');
                return;
            }

            showPageLoading();
            settlementModal.hide();
            try {
                const result = await apiPost(orderSettlementPaymentForm.dataset.cashUrl, {
                    orderId,
                    amount,
                    paymentMethod: 0,
                    note: noteInput?.value || null,
                    paidOn: paidOnInput?.value
                });

                if (result.success) {
                    location.reload();
                } else {
                    hidePageLoading();
                    toast('Lỗi', result.message || 'Không thể ghi nhận thanh toán.', 'error');
                }
            } catch {
                hidePageLoading();
                toast('Lỗi', 'Có lỗi xảy ra khi gửi yêu cầu.', 'error');
            }
        });

        qrManualConfirm?.addEventListener('click', async () => {
            if (!paymentIntent) return;

            settlementModal.element?.classList.add('d-none');
            const confirmed = await confirm('Đã nhận tiền', `Bạn xác nhận đã nhận đủ ${DecimalFields.formatCurrencyWithSymbol(paymentIntent.amount)}`);
            if (!confirmed) {
                settlementModal.element?.classList.remove('d-none');
                return;
            }

            showPageLoading();
            try {
                const result = await apiPost(orderSettlementPaymentForm.dataset.confirmUrl, {
                    intentId: paymentIntent.id,
                    note: noteInput?.value || null
                });
                if (!result.success) {
                    hidePageLoading();
                    settlementModal.element?.classList.remove('d-none');
                    return;
                }

                paymentIntent = result.intent;
                renderQrIntent();
                stopPolling();
                settlementModal.element?.classList.remove('d-none');
                settlementModal.hide();
                await consumesPaymentIntent();
            } catch {
                toast('Lỗi', 'Không thể xác nhận QR.', 'error');
            }
        });

        async function consumesPaymentIntent() {
            if (!paymentIntent || !isIntentConfirmed(paymentIntent)) return;

            toast('Thành công', 'Đã nhận tiền vào tài khoản.', 'success');

            showPageLoading();
            try {
                const result = await apiPost(orderSettlementPaymentForm.dataset.qrRecordUrl, {
                    orderId,
                    paymentIntentId: paymentIntent.id,
                    note: noteInput?.value || null
                });
                if (result.success) {
                    location.reload();
                } else {
                    hidePageLoading();
                }
            } catch {
                hidePageLoading();
                toast('Lỗi', 'Có lỗi xảy ra khi gửi yêu cầu.', 'error');
            }
        }

        function paymentModeChanged() {
            resetQrIntent();
            if (submitButton) {
                submitButton.innerHTML = isQrMode()
                    ? '<i class="bi bi-qr-code me-1"></i>Tạo QR'
                    : '<i class="bi bi-floppy me-1"></i>Ghi nhận';
            }
            qrManualConfirm?.classList.toggle('d-none', !isQrMode() || paymentIntent == null);
            submitButton?.classList.toggle('d-none', isQrMode() && paymentIntent != null);
        }

        function isFormValid() {
            if (!$(orderSettlementPaymentForm).valid())
                return false;
            return isPaidOnValid();
        }
        function isPaidOnValid() {
            const paidOnStr = paidOnInput.value;
            const paidOnValidator = document.querySelector(`[data-valmsg-for="${paidOnInput.name}"]`);
            if (!paidOnStr) {
                paidOnValidator.textContent = 'Vui lòng chọn ngày thu';
                return false;
            }
            const paidOn = new Date(paidOnStr);
            if (!paidOn.getTime()) {
                paidOnValidator.textContent = 'Ngày thu không đúng';
                return false;
            }
            const minDateStr = paidOnInput.getAttribute('data-min-date-s');
            if (minDateStr) {
                const minDate = new Date(minDateStr);
                if (minDate.getTime()) {
                    if (paidOn.getTime() < minDate.getTime()) {
                        paidOnValidator.textContent = 'Ngày thu phải lớn hơn ' + formatDate(minDate);
                        return false;
                    }
                }
            }
            const maxDateStr = paidOnInput.getAttribute('data-max-date-s');
            if (maxDateStr) {
                const maxDate = new Date(maxDateStr);
                if (maxDate.getTime()) {
                    if (paidOn.getTime() > maxDate.getTime()) {
                        paidOnValidator.textContent = 'Ngày thu phải nhỏ hơn ' + formatDate(maxDate);
                        return false;
                    }
                }
            }
            paidOnValidator.textContent = '';
            return true;
        }
        function formatDate(date) {
            const year = date.getFullYear();
            // getMonth() trả về 0-11 nên phải +1
            const month = String(date.getMonth() + 1).padStart(2, '0');
            const day = String(date.getDate()).padStart(2, '0');

            return `${day}/${month}/${year}`; // Hoặc `${day}/${month}/${year}`
        }
    }

    const orderExpenseForm = document.getElementById('orderExpenseForm');
    if (orderExpenseForm) {
        $('#orderExpenseForm').on('submit', async function (e) {
            e.preventDefault();
            if (!$(orderExpenseForm).valid())
                return;

            showPageLoading();
            const expenseModal = new PromiseModal('#orderExpenseModal');
            expenseModal.hide();

            const result = await submitFormAsync(this);
            if (result.success) {
                location.reload();
            } else {
                hidePageLoading();
            }
        });
    }

    const editDiscountModalElement = document.getElementById('editDiscountModal');
    if (editDiscountModalElement) {
        $('#editDiscountForm').on('submit', async function (e) {
            e.preventDefault();

            if (!$(this).valid())
                return;

            showPageLoading();
            const editDiscountModal = new PromiseModal('#editDiscountModal');
            editDiscountModal.hide();

            try {
                const result = await submitFormAsync(this);
                if (result.success) {
                    location.reload();
                } else {
                    hidePageLoading();
                    toast('Lỗi', result.message || 'Không thể cập nhật giảm giá cho đơn hàng.', 'error');
                }
            } catch (err) {
                hidePageLoading();
                toast('Lỗi', 'Có lỗi xảy ra khi gửi yêu cầu.', 'error');
            }
        });
    }

    const changeNoteModalElement = document.getElementById('changeNoteModal');
    if (changeNoteModalElement) {
        changeNoteModalElement.addEventListener('shown.bs.modal', () => $('#orderNote').focusEnd());

        $('#changeNoteForm').on('submit', async function (e) {
            e.preventDefault();

            if (!$(this).valid())
                return;

            showPageLoading();
            const changeNoteModal = new PromiseModal('#changeNoteModal');
            changeNoteModal.hide();

            var note = $(this).find('[name="Note"]');

            try {
                const result = await submitFormAsync(this);
                if (result.success) {
                    location.reload();
                } else {
                    hidePageLoading();
                    toast('Lỗi', result.message || 'Không thể cập nhật ghi chú đơn hàng.', 'error');
                }
            } catch (err) {
                hidePageLoading();
                toast('Lỗi', 'Có lỗi xảy ra khi gửi yêu cầu.', 'error');
            }
        });
    }

    const shippingModalElement = document.getElementById('shippingModal');
    if (shippingModalElement) {
        $('#shippingForm').on('submit', async function (e) {
            e.preventDefault();

            if (!$(this).valid())
                return;

            showPageLoading();
            const shippingModal = new PromiseModal('#shippingModal');
            shippingModal.hide();

            try {
                const result = await submitFormAsync(this);

                if (result.success) {
                    location.reload();
                } else {
                    hidePageLoading();
                    toast('Lỗi', result.message || 'Không thể cập nhật thông tin giao hàng.', 'error');
                }
            } catch (err) {
                hidePageLoading();
                toast('Lỗi', 'Có lỗi xảy ra khi gửi yêu cầu.', 'error');
            }
        });
    }

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

            showPageLoading();
            const createModal = new PromiseModal('#createDeliveryNoteModal');
            createModal.hide();

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
                    location.reload();
                } else {
                    hidePageLoading();
                    toast('Lỗi', result.message || 'Không thể tạo phiếu xuất kho.', 'error');
                }
            } catch (err) {
                hidePageLoading();
                toast('Lỗi', 'Có lỗi xảy ra khi gửi yêu cầu.', 'error');
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

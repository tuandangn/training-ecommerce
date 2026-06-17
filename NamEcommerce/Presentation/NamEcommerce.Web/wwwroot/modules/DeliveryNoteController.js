import { confirm } from '/modules/modals.js';
import { apiPost, apiGet } from '/modules/ajax-helper.js';

export default class DeliveryNoteController {
    #CONFIRM_URL = '/DeliveryNote/Confirm/';
    #DELIVERING_URL = '/DeliveryNote/MarkDelivering/';
    #CANCEL_URL = '/DeliveryNote/Cancel/';

    #options;

    constructor(options) {
        this.#options = Object.assign({
            hidePageLoadOnRequestEnd: true
        }, options);
    }

    async confirm(id) {
        if (!id) throw new Error('Id is required');
        if (!await confirm('Xác nhận phiếu giao', 'Xác nhận phiếu giao hàng và chuẩn bị giao hàng.'))
            return;

        showPageLoading();
        const result = await apiPost(this.#CONFIRM_URL + id);
        if (this.#options.hidePageLoadOnRequestEnd)
            hidePageLoading();
        return { success: result.success, data: result.data, error: result.error };
    }

    async delivering(id) {
        if (!id) throw new Error('Id is required');
        if (!await confirm('Đang giao', 'Xác nhận hàng hóa đang đi giao.'))
            return;
        showPageLoading();
        const result = await apiPost(this.#DELIVERING_URL + id);
        if (this.#options.hidePageLoadOnRequestEnd)
            hidePageLoading();
        return { success: result.success, data: result.data, error: result.error };
    }

    async cancel(id) {
        if (!id) throw new Error('Id is required');
        if (!await confirm('Hủy phiếu xuất', 'Bạn có chắc chắn muốn hủy phiếu xuất này?'))
            return;
        showPageLoading();
        const result = await apiPost(this.#CANCEL_URL + id);
        if (this.#options.hidePageLoadOnRequestEnd)
            hidePageLoading();
        return { success: result.success, data: result.data, error: result.error };
    }

    //Delivered workflow
    #deliveredContainer;
    #deliveredModal;
    #deliveredForm;

    #acceptant_items_info_url = '/DeliveryNote/GetDeliveryNoteAcceptantItemsInfo/';

    #deliveredState = {};

    beginDeliveredWorkflow(modalId) {
        if (!modalId)
            throw new Error('Modal Id is required');

        const modalContainer = document.getElementById(modalId);
        if (!modalContainer)
            throw new Error('Delivered modal not found');
        this.#deliveredContainer = modalContainer;
        this.#deliveredForm = modalContainer.querySelector('form');

        const modal = bootstrap.Modal.getOrCreateInstance(modalContainer);
        this.#deliveredModal = modal;

        if (!this.#deliveredContainer.initialized)
            this.#deliveredModalEvents();

        this.#deliveredContainer.initialized = true;

        return {
            setId: async (id) => {
                if (!id) throw new Error('Id is required');
                this.#resetDeliveredControls();
                const { acceptantItems: items, amountToCollect } = await this.#loadAcceptantItemsInfo(id);
                if (!Array.isArray(items) || items.length == 0) {
                    window.NotificationCenter.error('Không thể tải dữ liệu.');
                    return;
                }
                this.#setDeliveredState({ id, items, amountToCollect });
                this.#deliveredModal.show();
            }
        }
    }

    #setDeliveredState(patch) {
        this.#deliveredState = Object.assign(this.#deliveredState, patch);
        this.#renderDelivered();
    }

    #renderDelivered() {
        this.#deliveredContainer.querySelector('#deliveryNoteId').value = this.#deliveredState.id;
        this.#renderAcceptantItems();

        const cashCollectedAmount = document.getElementById('cashCollectedAmount');
        cashCollectedAmount.value = DecimalFields.formatCurrency(this.#deliveredState.amountToCollect);
        cashCollectedAmount.closest('div').classList.toggle('d-none', this.#deliveredState.amountToCollect == 0)

        this.#deliveryTableEvents();

        $(this.#deliveredForm).removeData('validator').removeData('unobtrusiveValidation');
        $.validator.unobtrusive.parse(this.#deliveredForm);
    }

    #renderAcceptantItems() {
        const rowsHtml = this.#deliveredState.items?.map(item => {
            const deliveredQty = Number(item.quantity || 0);
            return `<tr data-item-id="${item.id}" data-delivered-qty="${deliveredQty}">
                            <td class="pe-3">${this.#escapeHtml(item.productName)}</td>
                            <td class="text-end pe-3">${DecimalFields.formatQuantity(deliveredQty, item.quantityDecimalPlaces)}</td>
                            <td class="pe-3">
                                <input class="form-control form-control-sm text-end returned-qty" name="returnedQty_${item.id}"
                                    value="0"
                    data-decimal="quantity" data-decimals="${item.quantityDecimalPlaces ?? 0}" data-val="true"
                    data-val-required="Vui lòng nhập số lượng."
                    data-val-range="Số lượng trả về phải nhỏ hơn hoặc bằng ${DecimalFields.formatQuantity(deliveredQty)}." data-val-range-max="${deliveredQty}"
                    data-val-range-min="0"
                    data-val-number="Số lượng không đúng." />
                <span class="small text-danger field-validation-valid"
                    data-valmsg-for="returnedQty_${item.id}"
                    data-valmsg-replace="true"></span>
                            </td>
                            <td class="text-end accepted-qty-text fw-semibold pe-3">${DecimalFields.formatQuantity(deliveredQty, item.quantityDecimalPlaces)}</td>
                        </tr>`;
        }).join('');
        const container = document.getElementById('acceptanceTableBody');
        container.innerHTML = rowsHtml;
        DecimalFields.autoWrap(container);
    }

    async #loadAcceptantItemsInfo(id) {
        const result = await apiGet(this.#acceptant_items_info_url + id);
        if (!result.success)
            return;
        return result.data;
    }

    async #handleDelivered() {
        showPageLoading();

        const formData = DecimalFields.getFormData(this.#deliveredForm);

        const btn = $('#btnSubmitDelivery');
        const originalText = btn.html();
        btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Đang tải ảnh...');

        const result = await apiPost(this.#deliveredForm.action, formData);
        if (result.success) {
            location.reload();
            return;
        }
        hidePageLoading();
        btn.prop('disabled', false).html(originalText);
    }

    #buildAcceptancePayload() {
        const self = this;
        const items = [];
        let hasRejectedItems = false;
        const rejectReason = String($('#rejectReason').val() || '').trim();
        $('#acceptanceTableBody tr').each(function () {
            const $row = $(this);
            const itemId = String($row.data('item-id'));
            const deliveredQty = Number($row.data('delivered-qty') || 0);
            const returnedQty = parseFloat(DecimalFields.stripFormatting($row.find('.returned-qty').val(), 2)) || 0;
            const rejectedQty = Math.max(0, Math.min(deliveredQty, returnedQty));
            const normalizedAcceptedQty = Math.max(0, deliveredQty - rejectedQty);
            if (rejectedQty > 0) hasRejectedItems = true;

            items.push({
                deliveryNoteId: self.#deliveredState.id,
                deliveryNoteItemId: itemId,
                acceptedQuantity: normalizedAcceptedQty,
                rejectedQuantity: rejectedQty,
                rejectReason: rejectedQty > 0 ? rejectReason : null
            });
        });

        if (hasRejectedItems && !rejectReason) {
            window.NotificationCenter.warning('Vui lòng nhập lý do trả hàng.');
            return null;
        }
        if (!hasRejectedItems) {
            $('#compensateInNextDelivery').prop('checked', false);
        }

        return items;
    }

    #deliveredModalEvents() {
        this.#deliveredContainer.addEventListener('hidden.bs.modal', () => {
            // Destroy uploader so it re-inits fresh on next open
            const uploaderEl = this.#deliveredContainer.querySelector('[data-image-uploader]');
            if (uploaderEl?._imageUploader) {
                uploaderEl._imageUploader.destroy();
                delete uploaderEl._imageUploader;
            }
            this.#resetDeliveredControls();
        });

        this.#deliveredContainer.addEventListener('shown.bs.modal', () => {
            // Re-init image uploader each time modal opens
            import('/modules/image-uploader.js').then(({ initImageUploaders }) => {
                initImageUploaders(this.#deliveredContainer);
            });
        });

        // Form Submit for Delivery Proof
        $(this.#deliveredForm).submit(e => {
            e.preventDefault();

            if (!$(this.#deliveredForm).valid()) {
                return;
            }

            var acceptanceItems = this.#buildAcceptancePayload();
            if (!acceptanceItems) {
                return;
            }

            $('#acceptanceItemsJson').val(JSON.stringify(acceptanceItems));
            this.#handleDelivered();
        });
    }
    #deliveryTableEvents() {
        const onInput = debounce((e) => {
            updateAcceptedQuantity($(e.target).closest('tr'));
        }, 700);
        $('#acceptanceTableBody')
            .off('input', '.returned-qty')
            .on('input', '.returned-qty', onInput);

        function updateAcceptedQuantity($row) {
            const deliveredQty = Number($row.data('delivered-qty') || 0);
            let returnedQty = parseFloat(DecimalFields.stripFormatting($row.find('.returned-qty').val(), 2));
            if (!returnedQty) returnedQty = 0;
            if (returnedQty < 0) returnedQty = 0;
            if (returnedQty > deliveredQty) returnedQty = deliveredQty;

            const acceptedQty = Math.max(0, deliveredQty - returnedQty);
            $row.find('.accepted-qty-text').text(DecimalFields.formatQuantity(acceptedQty));
            updateReturnControls();
        }

        function updateReturnControls() {
            let hasRejectedItems = false;
            $('#acceptanceTableBody tr').each(function () {
                const $row = $(this);
                const deliveredQty = Number($row.data('delivered-qty') || 0);
                let returnedQty = parseFloat(DecimalFields.stripFormatting($row.find('.returned-qty').val(), 2));
                if (!returnedQty) returnedQty = 0;
                if (Math.min(deliveredQty, returnedQty) > 0) hasRejectedItems = true;
            });

            $('#rejectReason').closest('div').toggleClass('d-none', !hasRejectedItems);
            $('#compensateInNextDeliveryContainer').toggleClass('d-none', !hasRejectedItems);
            if (!hasRejectedItems) {
                $('#compensateInNextDelivery').prop('checked', false);
            }
        }
    }

    #resetDeliveredControls() {
        $('#receiverName').val('');
        $('#rejectReason').val('');
        $('#rejectReason').closest('div').addClass('d-none');
        $('#compensateInNextDelivery').prop('checked', false);
        $('#compensateInNextDeliveryContainer').addClass('d-none');
        $('#agreedCustomerCharge').val('0');
        $('#agreedCustomerChargeReason').val('');
        $('#cashCollectedAmount').val('0');
        $('#acceptanceTableBody').html('');
        $('#acceptanceItemsJson').val('');
    }

    //end workflow

    #escapeHtml(value) {
        return String(value ?? '').replace(/[&<>"']/g, char => ({
            '&': '&amp;',
            '<': '&lt;',
            '>': '&gt;',
            '"': '&quot;',
            "'": '&#039;'
        }[char]));
    }

    //Reject workflow
    #rejectContainer;
    #rejectModal;
    #rejectForm;

    #rejectState = {};

    beginRejectWorkflow(modalId) {
        if (!modalId)
            throw new Error('Modal Id is required');

        const modalContainer = document.getElementById(modalId);
        if (!modalContainer)
            throw new Error('Reject modal not found');
        this.#rejectContainer = modalContainer;
        this.#rejectForm = modalContainer.querySelector('form');

        const modal = bootstrap.Modal.getOrCreateInstance(modalContainer);
        this.#rejectModal = modal;

        if (!this.#rejectContainer.initialized)
            this.#rejectModalEvents();

        this.#rejectContainer.initialized = true;

        return {
            setData: async (id, code, warehouse) => {
                if (!id) throw new Error('Id is required');
                const { acceptantItems: items } = await this.#loadAcceptantItemsInfo(id);
                if (!Array.isArray(items) || items.length == 0) {
                    window.NotificationCenter.error('Không thể tải dữ liệu.');
                    return;
                }
                this.#setRejectState({ id, code, warehouse, items });
                this.#rejectModal.show();
            }
        }
    }

    #setRejectState(patch) {
        this.#rejectState = Object.assign(this.#rejectState, patch);
        this.#renderReject();
    }

    #renderReject() {
        this.#rejectContainer.querySelector('#deliveryNoteId').value = this.#rejectState.id;
        this.#resetRejectControls();
        this.#renderItems();

        $(this.#rejectForm).removeData('validator').removeData('unobtrusiveValidation');
        $.validator.unobtrusive.parse(this.#rejectForm);
    }

    #renderItems() {
        const rowsHtml = this.#rejectState.items?.map(item => {
            const deliveredQty = Number(item.quantity || 0);
            return `<tr>
                            <td class="pe-3">${this.#escapeHtml(item.productName)}</td>
                            <td class="text-end pe-3">${DecimalFields.formatQuantity(deliveredQty, item.quantityDecimalPlaces)}</td>
                        </tr>`;
        }).join('');
        const container = document.getElementById('rejectTableBody');
        container.innerHTML = rowsHtml;
    }

    #rejectModalEvents() {
        const self = this;
        this.#rejectContainer.addEventListener('hidden.bs.modal', () => this.#setRejectState({ id: '', code: '', warehouse: '', items:[] }));

        // Form Submit for Delivery Proof
        $(this.#rejectForm).submit(e => {
            e.preventDefault();

            if (!$(this.#rejectForm).valid()) {
                return;
            }

            this.#handleReject();
        });
    }

    async #handleReject() {
        showPageLoading();

        const formData = DecimalFields.getFormData(this.#rejectForm);

        const btn = $('#btnRejectSubmit');
        const originalText = btn.html();
        btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Đang xử lý...');

        const result = await apiPost(this.#rejectForm.action, formData);
        if (result.success) {
            location.reload();
            return;
        }
        hidePageLoading();
        btn.prop('disabled', false).html(originalText);
    }

    #resetRejectControls() {
        this.#rejectContainer.querySelector('#rejectDnCode').textContent = this.#rejectState.code ? 'Phiếu: ' + this.#rejectState.code : '';
        const option = this.#rejectContainer.querySelector(`#rejectReturnWarehouseId option[value="${this.#rejectState.warehouse}"]`);
        if(option)
            this.#rejectContainer.querySelector('#rejectReturnWarehouseId').value = this.#rejectState.warehouse;
        else
            this.#rejectContainer.querySelector('#rejectReturnWarehouseId').value = '';
        const container = document.getElementById('rejectTableBody');
        container.innerHTML = '';
    }
}

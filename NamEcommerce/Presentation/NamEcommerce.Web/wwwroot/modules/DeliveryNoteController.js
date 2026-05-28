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
        if (!await confirm('Xuất hàng', 'Xác nhận hàng hóa đã xuất kho'))
            return;

        showPageLoading();
        const result = await apiPost(this.#CONFIRM_URL + id);
        if (this.#options.hidePageLoadOnRequestEnd)
            hidePageLoading();
        return { success: result.success, data: result.data, error: result.error };
    }

    async delivering(id) {
        if (!id) throw new Error('Id is required');
        if (!await confirm('Đang giao', 'Xác nhận hàng hóa đang đi giao'))
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
    #ACCEPTANT_ITEMS_URL = '/DeliveryNote/GetAcceptantItems/';

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
                const items = await this.#loadAcceptantItems(id);
                if (!Array.isArray(items) || items.length == 0) {
                    window.NotificationCenter.error('Không thể tải dữ liệu.');
                    return;
                }
                this.#setDeliveredState({ id, items });
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

        this.#deliveryTableEvents();

        $(this.#deliveredForm).removeData('validator').removeData('unobtrusiveValidation');
        $.validator.unobtrusive.parse(this.#deliveredForm);
    }

    #renderAcceptantItems() {
        const rowsHtml = this.#deliveredState.items?.map(item => {
            const deliveredQty = Number(item.quantity || 0);
            return `<tr data-item-id="${item.id}" data-delivered-qty="${deliveredQty}">
                            <td class="pe-3">${this.#escapeHtml(item.productName)}</td>
                            <td class="text-end pe-3">${DecimalFields.formatQuantity(deliveredQty)}</td>
                            <td class="pe-3">
                                <input class="form-control form-control-sm text-end accepted-qty" name="acceptedQty_${item.id}"
                                    value="${DecimalFields.formatQuantity(deliveredQty)}"
                    data-decimal="quantity" data-val="true"
                    data-val-required="Vui lòng nhập số lượng."
                    data-val-range="Số lượng phải nhỏ hơn ${DecimalFields.formatQuantity(deliveredQty)}." data-val-range-max="${deliveredQty}"
                    data-val-range-min="0"
                    data-val-number="Số lượng không đúng." />
                <span class="small text-danger field-validation-valid"
                    data-valmsg-for="acceptedQty_${item.id}"
                    data-valmsg-replace="true"></span>
                            </td>
                            <td class="text-end rejected-qty text-danger fw-semibold pe-3">0</td>
                        </tr>`;
        }).join('');
        const container = document.getElementById('acceptanceTableBody');
        container.innerHTML = rowsHtml;
        DecimalFields.autoWrap(container);
    }

    async #loadAcceptantItems(id) {
        const result = await apiGet(this.#ACCEPTANT_ITEMS_URL + id);
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
            const acceptedQty = parseFloat(DecimalFields.stripFormatting($row.find('.accepted-qty').val(), 2)) || 0;
            const normalizedAcceptedQty = Math.max(0, Math.min(deliveredQty, acceptedQty));
            const rejectedQty = Math.max(0, deliveredQty - normalizedAcceptedQty);
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

        return items;
    }

    #deliveredModalEvents() {
        const self = this;
        this.#deliveredContainer.addEventListener('hidden.bs.modal', () => this.#resetDeliveredControls());

        // Image Preview inside Modal
        $("#deliveryProofPicture").change(function () {
            var input = this;
            if (input.files && input.files[0]) {
                var reader = new FileReader();

                reader.onload = function (e) {
                    $('#previewContainer').removeClass('d-none');
                    $('#imagePreview').attr('src', e.target.result).show();
                }

                reader.readAsDataURL(input.files[0]);
                self.#deliveredContainer.querySelector('[data-valmsg-for="pictureFile"]').textContent = '';
            } else {
                $('#previewContainer').addClass('d-none');
                $('#imagePreview').hide();
            }
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
            updateRejectedQuantity($(e.target).closest('tr'));
        }, 700);
        $('#acceptanceTableBody').on('input', '.accepted-qty', onInput);

        function updateRejectedQuantity($row) {
            const deliveredQty = Number($row.data('delivered-qty') || 0);
            let acceptedQty = parseFloat(DecimalFields.stripFormatting($row.find('.accepted-qty').val(), 2));
            if (!acceptedQty) acceptedQty = 0;
            if (acceptedQty < 0) acceptedQty = 0;
            if (acceptedQty > deliveredQty) acceptedQty = deliveredQty;

            const rejectedQty = Math.max(0, deliveredQty - acceptedQty);
            $row.find('.rejected-qty').text(DecimalFields.formatQuantity(rejectedQty));
        }
    }

    #resetDeliveredControls() {
        $('#receiverName').val('');
        $('#rejectReason').val('');
        $('#agreedCustomerCharge').val('0');
        $('#agreedCustomerChargeReason').val('');
        $('#deliveryProofPicture').val('');
        $('#previewContainer').addClass('d-none');
        $('#imagePreview').hide();
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
                const items = await this.#loadAcceptantItems(id);
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
                            <td class="text-end pe-3">${DecimalFields.formatQuantity(deliveredQty)}</td>
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
        const option = this.#rejectContainer.querySelector(`#rejectWarehouseId option[value="${this.#rejectState.warehouse}"]`);
        if(option)
            this.#rejectContainer.querySelector('#rejectReturnWarehouseId').value = this.#rejectState.warehouse;
        else
            this.#rejectContainer.querySelector('#rejectReturnWarehouseId').value = '';
        const container = document.getElementById('rejectTableBody');
        container.innerHTML = '';
    }
}
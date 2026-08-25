import { apiGet } from '/modules/ajax-helper.js';
import { initImageUploaders } from '/modules/image-uploader.js';

export default class BulkReceiveController {
    #purchaseOrderId;

    #modalEl;
    #form;
    #tbody;
    #totalSummary;
    #addRowBtn;
    #submitBtn;
    #itemsError;

    #receivedOnEl;
    #shippingInput;
    #taxRate;
    #taxHint;

    #subtotalEl;
    #shippingDisplayEl;
    #taxDisplayEl;
    #grandTotalEl;

    #items = [];
    #itemsById = new Map();
    #warehouses = [];
    #defaultWarehouseId = '';
    #rowSeq = 0;
    #taxMode = 'amount';
    #dsAbortControllers = new Map();

    constructor(purchaseOrderId) {
        this.#purchaseOrderId = purchaseOrderId;
    }

    init(modalEl) {
        if (!modalEl) return;
        this.#modalEl = modalEl;

        const dataEl = document.getElementById('bulkReceiveData');
        if (!dataEl) return;
        try {
            const data = JSON.parse(dataEl.textContent ?? '{}');
            this.#items = Array.isArray(data.items) ? data.items : [];
            this.#warehouses = Array.isArray(data.warehouses) ? data.warehouses : [];
            this.#defaultWarehouseId = data.defaultWarehouseId ?? '';
        } catch {
            return;
        }
        this.#items.forEach(it => this.#itemsById.set(it.id, it));

        this.#form = document.getElementById('bulkReceiveForm');
        this.#tbody = document.getElementById('bulkReceiveTableBody');
        this.#totalSummary = this.#form.querySelector('.order-summary');
        this.#addRowBtn = document.getElementById('bulkReceiveAddRow');
        this.#submitBtn = document.getElementById('bulkReceiveSubmit');
        this.#itemsError = document.getElementById('bulkReceiveItemsError');

        this.#shippingInput = document.getElementById('bulkAdditionalShipping');
        this.#shippingDisplayEl = document.getElementById('bulkShippingDisplay');

        this.#taxHint = document.getElementById('bulkAdditionalTaxHint');
        this.#taxRate = document.getElementById('bulkTaxRate');
        this.#taxDisplayEl = document.getElementById('bulkTaxDisplay');

        this.#subtotalEl = document.getElementById('bulkSubtotal');
        this.#grandTotalEl = document.getElementById('bulkGrandTotal');

        this.#receivedOnEl = document.getElementById('bulkReceiveReceivedOn')

        this.#bindEvents();

        DecimalFields.autoWrap(this.#modalEl);
        initFlatPickrDateTime(this.#receivedOnEl);

        // Khi modal mở: reset và prefill 1 dòng cho mỗi item còn lại
        this.#modalEl.addEventListener('show.bs.modal', () => {
            this.#prefillRows();
            reparseForm(this.#form);
        });

        initImageUploaders(this.#form);
    }

    #bindEvents() {
        const onTotalChanged = debounce(() => this.#calculateTotals(), 500);

        this.#addRowBtn.addEventListener('click', () => {
            this.#addRow(null, this.#items.length);
            this.#items.push({});
            this.#calculateTotals();
        });

        this.#tbody.addEventListener('click', e => {
            const removeBtn = e.target.closest('.bulk-row-remove');
            if (removeBtn) {
                const tr = removeBtn.closest('tr');
                const idx = tr.dataset.rowIndex;
                if (idx) this.#tbody.querySelector(`[data-ds-row-for="${idx}"]`)?.remove();
                tr.remove();
                if (this.#tbody.rows.length > 0) {
                    this.#reOrderRows();
                    onTotalChanged.flush();
                } else {
                    this.#totalSummary.classList.add('d-none');
                    this.#showItemsError('Vui lòng thêm ít nhất một dòng nhận hàng hợp lệ.');
                }
                return;
            }

            const directShipToggle = e.target.closest('.bulk-row-ds-toggle');
            if (directShipToggle) {
                this.#toggleDirectShipRow(directShipToggle.closest('tr[data-row-index]'));
                return;
            }

            const directShipCloseBtn = e.target.closest('.bulk-ds-row-close');
            if (directShipCloseBtn) {
                const directShipTr = directShipCloseBtn.closest('tr.bulk-ds-row');
                const mainRowIndex = directShipTr.dataset.dsRowFor;
                if (mainRowIndex !== undefined) {
                    const mainTr = this.#tbody.querySelector(`tr[data-row-index="${mainRowIndex}"]`);
                    if (mainTr) this.#toggleDirectShipRow(mainTr);
                }
                return;
            }

            const dsOrderItemBtn = e.target.closest('.bulk-ds-order-item-btn');
            if (dsOrderItemBtn && !dsOrderItemBtn.classList.contains('active')) {
                const directShipTr = dsOrderItemBtn.closest('tr');
                directShipTr.querySelectorAll('.bulk-ds-order-item-btn, .bulk-ds-existing-alloc-btn').forEach(b => b.classList.remove('active'));
                dsOrderItemBtn.classList.add('active');
                directShipTr.querySelector('.bulk-ds-order-item-id').value = dsOrderItemBtn.dataset.orderItemId;
                directShipTr.querySelector('.bulk-ds-order-id').value = dsOrderItemBtn.dataset.orderId;
                directShipTr.querySelector('.bulk-ds-existing-allocation-id').value = '';
                const label = directShipTr.querySelector('.bulk-ds-selected-label');
                if (label) label.textContent = `Đơn: ${dsOrderItemBtn.dataset.orderCode} · Khách: ${dsOrderItemBtn.dataset.customerName} · Có thể nhận: ${dsOrderItemBtn.dataset.availableToAllocate}`;
                setFields(directShipTr, dsOrderItemBtn);
                return;
            }

            const directShipExistingAllocationBtn = e.target.closest('.bulk-ds-existing-alloc-btn');
            if (directShipExistingAllocationBtn && !directShipExistingAllocationBtn.classList.contains('active')) {
                const directShipTr = directShipExistingAllocationBtn.closest('tr');
                directShipTr.querySelectorAll('.bulk-ds-order-item-btn, .bulk-ds-existing-alloc-btn').forEach(b => b.classList.remove('active'));
                directShipExistingAllocationBtn.classList.add('active');
                directShipTr.querySelector('.bulk-ds-existing-allocation-id').value = directShipExistingAllocationBtn.dataset.allocationId;
                directShipTr.querySelector('.bulk-ds-order-item-id').value = '';
                directShipTr.querySelector('.bulk-ds-order-id').value = '';
                const label = directShipTr.querySelector('.bulk-ds-selected-label');
                if (label) label.textContent = `Nâng cấp: ${directShipExistingAllocationBtn.dataset.orderCode} · Khách: ${directShipExistingAllocationBtn.dataset.customerName} · Còn chờ: ${directShipExistingAllocationBtn.dataset.remainingQty}`;
                setFields(directShipTr, directShipExistingAllocationBtn);
                return;
            }

            function setFields(tr, btn) {
                const fields = tr.querySelector('.bulk-ds-fields');
                fields?.classList.remove('d-none');
                const addrInput = tr.querySelector('.bulk-ds-address-input');
                if (addrInput) addrInput.value = btn.dataset.shippingAddress || '';
                const phoneInput = tr.querySelector('.bulk-ds-contact-phone');
                if (phoneInput) phoneInput.value = btn.dataset.shippingPhoneNumber || btn.dataset.customerPhone || '';
                const nameInput = tr.querySelector('.bulk-ds-contact-name');
                if (nameInput) nameInput.value = btn.dataset.shippingContactName || '';
            }
        });
        this.#tbody.addEventListener('change', (e) => {
            if (!e.target.matches('.bulk-row-item')) {
                this.#calculateTotals();
                return;
            }
            const tr = e.target.closest('tr');
            let item = this.#itemsById.get(e.target.value);
            if (!item) item = { id: e.target.value, decimalPlaces: 0, unitCost: 0 }

            const decimalPlaces = String(item.decimalPlaces ?? 0);
            tr.dataset.decimalPlaces = decimalPlaces;

            const inputQuantity = tr.querySelector('.bulk-row-qty');
            const oldDecimalPlaces = inputQuantity.dataset.decimals;
            inputQuantity.dataset.decimals = decimalPlaces;
            inputQuantity.dataset.decimalBound = '';
            if (!item.id || (oldDecimalPlaces != decimalPlaces))
                inputQuantity.value = '';
            else
                DecimalFields.bindInput(inputQuantity);
            const remainingQty = item.remaining || 0;
            inputQuantity.dataset.valRangeMax = remainingQty;
            if (remainingQty > 0)
                inputQuantity.dataset.valRange = `Số lượng phải lớn hơn 0 và nhỏ hơn ${this.#formatQty(remainingQty, decimalPlaces)}`;
            else
                inputQuantity.dataset.valRange = 'Số lượng phải lớn hơn 0';

            const inputDecimalPlaces = tr.querySelector('.bulk-row-decimal-places');
            inputDecimalPlaces.value = decimalPlaces;

            const inputUnitCost = tr.querySelector('.bulk-row-cost');
            inputUnitCost.value = this.#formatCurrency(item.unitCost);

            this.#refreshRowHint(tr);
            this.#syncRowWarehouse(tr);

            inputQuantity.disabled = !item.id;
            inputUnitCost.disabled = !item.id;
            inputDecimalPlaces.disabled = !item.id;
            tr.querySelector('.bulk-row-warehouse').disabled = !item.id;
            tr.querySelector('.bulk-row-ds-toggle').disabled = !item.id;

            const rowIndex = tr.dataset.rowIndex;
            const directShipTr = this.#tbody?.querySelector(`[data-ds-row-for="${rowIndex}"]`);
            if (directShipTr) {
                if (!item.id)
                    directShipTr.classList.add('d-none');
                if (!directShipTr.classList.contains('d-none')) {
                    this.#loadDirectShipItems(tr, directShipTr);
                }
            }

            reparseForm(this.#form);
            validateElement(e.target);
            validateElement(inputQuantity);
            validateElement(inputUnitCost);
        });

        const onQtyAndCostChanged = debounce((e) => {
            if (e.target.matches('.bulk-row-qty') || e.target.matches('.bulk-row-cost'))
                onTotalChanged.flush();
            if (e.target.matches('.bulk-row-qty'))
                this.#syncRowWarehouse(e.target.closest('tr'));
        }, 500);
        this.#tbody.addEventListener('input', onQtyAndCostChanged);
        this.#tbody.addEventListener('change', (e) => {
            if (e.target.matches('.bulk-row-qty'))
                this.#syncRowWarehouse(e.target.closest('tr'));
        });

        this.#shippingInput.addEventListener('input', onTotalChanged);
        this.#shippingInput.addEventListener('change', onTotalChanged.flush);
        this.#taxRate.addEventListener('change', () => {
            onTotalChanged.flush();
        })

        this.#form.addEventListener('formdata', e => {
            DecimalFields.processFormData(this.#form, e.formData);
        });
        this.#form.addEventListener('submit', e => this.#onSubmit(e));
    }

    #prefillRows(items) {
        if (!this.#tbody) return;
        this.#tbody.innerHTML = '';
        if (!items) items = this.#items;
        items.forEach((item, index) => this.#addRow(item, index, true));
        this.#calculateTotals();
    }
    #reOrderRows() {
        this.#tbody.querySelectorAll('tr[data-row-index]').forEach((tr, index) => {
            const oldIndex = tr.dataset.rowIndex;
            {
                tr.dataset.rowIndex = index;
                for (let input of tr.querySelectorAll('[name]')) {
                    setIndex(input, 'name', index);
                }
                for (let input of tr.querySelectorAll('[data-valmsg-for]')) {
                    setIndex(input.dataset, 'valmsgFor', index);
                }
            }
            const directShipTr = this.#tbody.querySelector(`[data-ds-row-for="${oldIndex}"]`);
            if (directShipTr) {
                directShipTr.dataset.dsRowFor = index;
                for (let input of directShipTr.querySelectorAll('[name]')) {
                    setIndex(input, 'name', index);
                }
                for (let input of directShipTr.querySelectorAll('[data-valmsg-for]')) {
                    setIndex(input.dataset, 'valmsgFor', index);
                }
            }
        });
        reparseForm(this.#form);

        function setIndex(element, attrName, index) {
            if (!element[attrName])
                return;
            if (element[attrName].indexOf('[') == -1 || element[attrName].indexOf(']') == -1)
                return;
            element[attrName] = element[attrName].replace(/\[\d+\]/, `[${index}]`);
        }
    }
    #addRow(item, index, initital) {
        if (!this.#tbody) return;
        const presetItemId = item?.id ?? '';
        const presetQty = item?.remaining ?? 0;

        const tr = document.createElement('tr');
        tr.classList.add('align-top');
        tr.dataset.rowIndex = String(index);
        tr.dataset.decimalPlaces = item?.decimalPlaces ?? 0;
        tr.dataset.unitMeasurement = item?.unitMeasurement ?? '';

        const itemOptions = Array.from(this.#itemsById.values()).map(item =>
            `<option value="${escapeHtml(item.id)}" ${item.id === presetItemId ? 'selected' : ''}>${escapeHtml(item.name)}</option>`
        ).join('');

        const warehouseOptions = this.#warehouses.map(w => {
            const selected = w.id === this.#defaultWarehouseId ? 'selected' : '';
            return `<option value="${escapeHtml(w.id)}" ${selected}>${escapeHtml(w.name)}</option>`;
        }).join('');

        const qtyValue = presetQty !== '' && presetQty != null
            ? (window.DecimalFields?.formatQuantity ? DecimalFields.formatQuantity(presetQty) : presetQty)
            : '';

        tr.innerHTML = `
            <td class="ps-2">
                <button type="button" class="btn btn-link btn-sm text-danger px-0 bulk-row-remove" title="Xóa dòng">
                    <i class="bi bi-trash"></i>
                </button>
            </td>
            <td class="ps-2">
                <select name="Items[${index}].ItemId" data-val="true" data-val-required="Vui lòng chọn hàng hóa"
                        class="form-select form-select-sm bulk-row-item">
                    <option value="">-- Chọn hàng hóa --</option>
                    ${itemOptions}
                </select>
                <span class="small text-danger field-validation-valid" data-valmsg-for="Items[${index}].ItemId" data-valmsg-replace="true"></span>
            </td>
            <td class="text-end pe-2 d-none d-xl-table-cell">
                <span class="fw-medium text-muted bulk-row-remaining">${item == null ? '' : this.#formatQty(item.remaining, item.decimalPlaces)}</span>
            </td>
            <td class="text-end pe-2">
                <input name="Items[${index}].Quantity" inputmode="decimal" placeholder="Số lượng" 
                       class="form-control form-control-sm text-end bulk-row-qty" ${item == null ? 'disabled' : ''}
                       data-val="true" data-val-required="Vui lòng nhập số lượng" data-val-number="Số lượng phải là số"
                       data-val-range="Số lượng phải lớn hơn 0 và nhỏ hơn ${item == null ? '' : this.#formatQty(item.remaining, item.decimalPlaces)}" data-val-range-min="0.001" data-val-range-max="${item?.remaining}"
                       data-decimal="quantity" data-decimals="${item?.decimalPlaces ?? 0}" value="${escapeHtml(qtyValue)}" />
                <input type="hidden" name="Items[${index}].QuantityDecimalPlaces" class="bulk-row-decimal-places" value="${item?.decimalPlaces ?? 0}" />
                <span class="small text-danger field-validation-valid" data-valmsg-for="Items[${index}].Quantity" data-valmsg-replace="true"></span>
            </td>
            <td class="text-end pe-2">
                <input name="Items[${index}].ActualUnitCost" inputmode="numeric"
                       class="form-control form-control-sm text-end bulk-row-cost"
                       data-val="true" lòng nhập số lượng" data-val-number="Giá vốn phải là số"
                       data-val-range="Giá vốn phải lớn hơn 0" data-val-range-min="0.001"
                       data-decimal="currency" value="${this.#formatCurrency(item?.unitCost ?? 0)}" 
                       placeholder="Giá vốn" ${item == null ? 'disabled' : ''}/>
                <span class="small text-danger field-validation-valid" data-valmsg-for="Items[${index}].ActualUnitCost" data-valmsg-replace="true"></span>
            </td>
            <td class="ps-2">
                <select name="Items[${index}].WarehouseId" ${item == null ? 'disabled' : ''} data-val="true" data-val-required="Vui lòng chọn kho hàng" class="form-select form-select-sm bulk-row-warehouse">
                    ${warehouseOptions}
                </select>
                <span class="small text-danger field-validation-valid" data-valmsg-for="Items[${index}].WarehouseId" data-valmsg-replace="true"></span>
            </td>
            <td class="pe-2 text-nowrap text-end">
                <button type="button" class="btn btn-sm btn-outline-secondary bulk-row-ds-toggle" ${item == null ? 'disabled' : ''} title="Thiết lập giao thẳng cho dòng này">
                    <i class="bi bi-send"></i> 
                    <span class="d-none d-xl-inline">Giao thẳng</span>
                </button>
            </td>`;

        this.#tbody.appendChild(tr);
        this.#totalSummary.classList.remove('d-none');
        DecimalFields.autoWrap(tr);
        this.#refreshRowHint(tr);
        this.#syncRowWarehouse(tr);

        const directShipTr = document.createElement('tr');
        directShipTr.className = 'bulk-ds-row d-none';
        directShipTr.dataset.dsRowFor = String(index);
        directShipTr.innerHTML = `
            <td colspan="7" class="border-top-0 pt-0 pb-2 px-2">
                <div class="mt-2">
                    <div class="d-flex justify-content-between align-items-center mb-2">
                        <span class="small fw-semibold text-primary"><i class="bi bi-send me-1"></i>Giao thẳng cho đơn hàng</span>
                        <button type="button" class="btn-close btn-sm bulk-ds-row-close" title="Hủy giao thẳng"></button>
                    </div>
                    <input type="hidden" name="Items[${index}].DirectShipOrderItemId" class="bulk-ds-order-item-id" value="" />
                    <input type="hidden" name="Items[${index}].DirectShipOrderId" class="bulk-ds-order-id" value="" />
                    <input type="hidden" name="Items[${index}].DirectShipExistingAllocationId" class="bulk-ds-existing-allocation-id" value="" />
                    <div class="bulk-ds-loading d-none text-muted small py-1">
                        <span class="spinner-border spinner-border-sm me-1"></span> Đang tải đơn hàng...
                    </div>
                    <div class="bulk-ds-empty d-none alert alert-info py-1 small mb-2">
                        <i class="bi bi-exclamation-circle me-1"></i> Không có đơn hàng phù hợp cho sản phẩm này.
                    </div>
                    <div class="bulk-ds-order-list d-none">
                        <div class="bulk-ds-order-items list-group list-group-flush border rounded mb-2" style="max-height:130px;overflow-y:auto"></div>
                    </div>
                    <div class="bulk-ds-fields d-none">
                        <p class="small fw-semibold mb-2 bulk-ds-selected-label text-primary"></p>
                        <div class="row g-2">
                            <div class="col-xl-4 col-lg-12">
                                <label class="form-label form-label-sm text-muted mb-1">Địa chỉ giao hàng <span class="text-danger">*</span></label>
                                <input type="text" name="Items[${index}].DirectShipAddress" class="form-control form-control-sm bulk-ds-address-input" placeholder="Địa chỉ giao"
                                    data-val="true" data-val-maxlength="Địa chỉ tối đa 500 ký tự" data-val-maxlength-max="500"  />
                                <span class="small text-danger field-validation-valid" data-valmsg-for="Items[${index}].DirectShipAddress" data-valmsg-replace="true"></span>
                            </div>
                            <div class="col-xl-4 col-lg-6">
                                <label class="form-label form-label-sm text-muted mb-1">Số điện thoại <span class="text-danger">*</span></label>
                                <input type="text" name="Items[${index}].DirectShipContactPhone" class="form-control form-control-sm bulk-ds-contact-phone" placeholder="Số điện thoại" inputmode="tel"
                                       data-val-regex="Số điện thoại không hợp lệ." data-val-regex-pattern="0\\d{9,10}"
                                       data-val="true" data-val-required="Vui lòng nhập số điện thoại" />
                                <span class="small text-danger field-validation-valid" data-valmsg-for="Items[${index}].DirectShipContactPhone" data-valmsg-replace="true"></span>
                            </div>
                            <div class="col-xl-4 col-lg-6">
                                <label class="form-label form-label-sm text-muted mb-1">Tên người nhận</label>
                                <input type="text" name="Items[${index}].DirectShipContactName" class="form-control form-control-sm bulk-ds-contact-name" placeholder="Người nhận" 
                                    data-val="true" data-val-maxlength="Tên người nhận tối đa 200 ký tự" data-val-maxlength-max="200"/>
                                <span class="small text-danger field-validation-valid" data-valmsg-for="Items[${index}].DirectShipContactName" data-valmsg-replace="true"></span>
                            </div>
                        </div>
                    </div>
                    <div class="bulk-ds-error d-none alert alert-danger py-1 small mb-0"></div>
                </div>
            </td>`;
        this.#tbody.appendChild(directShipTr);

        this.#itemsError.classList.add('d-none');
        if (!initital) reparseForm(this.#form);
    }

    #toggleDirectShipRow(tr) {
        if (!tr) return;
        const idx = tr.dataset.rowIndex;
        const dsTr = this.#tbody?.querySelector(`[data-ds-row-for="${idx}"]`);
        if (!dsTr) return;
        const btn = tr.querySelector('.bulk-row-ds-toggle');

        if (dsTr.classList.contains('d-none')) {
            dsTr.classList.remove('d-none');
            btn?.classList.replace('btn-outline-secondary', 'btn-outline-primary');
            const itemId = tr.querySelector('.bulk-row-item')?.value;
            if (!itemId || dsTr.dataset.dsLoadedFor !== itemId) {
                this.#loadDirectShipItems(tr, dsTr);
            }
        } else {
            dsTr.classList.add('d-none');
            dsTr.querySelector('.bulk-ds-order-item-id').value = '';
            dsTr.querySelector('.bulk-ds-order-id').value = '';
            dsTr.querySelector('.bulk-ds-existing-allocation-id').value = '';
            btn?.classList.replace('btn-outline-primary', 'btn-outline-secondary');
        }
    }

    async #loadDirectShipItems(tr, directShipTr) {
        const rowIdx = directShipTr.dataset.dsRowFor;
        this.#dsAbortControllers.get(rowIdx)?.abort();
        const controller = new AbortController();
        this.#dsAbortControllers.set(rowIdx, controller);

        const itemId = tr.querySelector('.bulk-row-item')?.value;
        const loading = directShipTr.querySelector('.bulk-ds-loading');
        const empty = directShipTr.querySelector('.bulk-ds-empty');
        const orderList = directShipTr.querySelector('.bulk-ds-order-list');
        const orderItems = directShipTr.querySelector('.bulk-ds-order-items');
        const fields = directShipTr.querySelector('.bulk-ds-fields');
        const errorBox = directShipTr.querySelector('.bulk-ds-error');

        loading.classList.remove('d-none');
        empty.classList.add('d-none');
        orderList.classList.add('d-none');
        fields.classList.add('d-none');
        errorBox.classList.add('d-none');
        directShipTr.querySelector('.bulk-ds-order-item-id').value = '';
        directShipTr.querySelector('.bulk-ds-order-id').value = '';

        if (!itemId) {
            loading.classList.add('d-none');
            empty.classList.remove('d-none');
            return;
        }

        try {
            const [eligibleResp, nonDsResp] = await Promise.all([
                apiGet(`/PurchaseOrder/EligibleOrderItems?purchaseOrderId=${this.#purchaseOrderId}&purchaseOrderItemId=${itemId}`, { signal: controller.signal }),
                apiGet(`/PurchaseOrder/NonDirectShipAllocations?purchaseOrderId=${this.#purchaseOrderId}&purchaseOrderItemId=${itemId}`, { signal: controller.signal })
            ]);
            if (!eligibleResp.success || !nonDsResp.success)
                return;
            const [eligibleData, nonDsData] = [eligibleResp.data, nonDsResp.data];
            loading.classList.add('d-none');
            directShipTr.dataset.dsLoadedFor = itemId;

            if (!eligibleData?.length && !nonDsData?.length) {
                empty.classList.remove('d-none');
                return;
            }

            orderItems.innerHTML = '';

            if (eligibleData?.length) {
                const header = document.createElement('div');
                header.className = 'px-3 py-1 text-muted small fw-semibold bg-light border-bottom';
                header.textContent = 'Chọn đơn bán';
                orderItems.appendChild(header);
                eligibleData.forEach(order => {
                    const btn = document.createElement('button');
                    btn.type = 'button';
                    btn.className = 'list-group-item list-group-item-action py-2 px-3 bulk-ds-order-item-btn';
                    btn.dataset.orderItemId = order.orderItemId;
                    btn.dataset.availableToAllocate = order.availableToAllocate;
                    setDataSet(btn, order);
                    btn.innerHTML = `<div class="d-flex justify-content-between align-items-center">
                        <span><strong>${escapeHtml(order.orderCode)}</strong> <span class="text-muted small">${escapeHtml(order.customerName)}</span></span>
                        <span class="badge bg-success ms-2">Còn ${order.availableToAllocate}</span>
                    </div>`;
                    orderItems.appendChild(btn);
                });
            }

            if (nonDsData?.length) {
                const header = document.createElement('div');
                header.className = 'px-3 py-1 text-muted small fw-semibold bg-light border-bottom' + (eligibleData?.length ? ' border-top mt-1' : '');
                header.textContent = 'Chuyển lên giao thẳng';
                orderItems.appendChild(header);
                nonDsData.forEach(alloc => {
                    const btn = document.createElement('button');
                    btn.type = 'button';
                    btn.className = 'list-group-item list-group-item-action py-2 px-3 bulk-ds-existing-alloc-btn';
                    btn.dataset.allocationId = alloc.allocationId;
                    btn.dataset.remainingQty = alloc.remainingQty;
                    setDataSet(btn, alloc);
                    btn.innerHTML = `<div class="d-flex justify-content-between align-items-center">
                        <span><strong>${escapeHtml(alloc.orderCode)}</strong> <span class="text-muted small">${escapeHtml(alloc.customerName)}</span></span>
                        <span class="badge bg-warning text-light ms-2">Còn ${alloc.remainingQty}</span>
                    </div>`;
                    orderItems.appendChild(btn);
                });
            }

            function setDataSet(btn, data) {
                btn.dataset.orderId = data.orderId;
                btn.dataset.orderCode = data.orderCode;
                btn.dataset.customerName = data.customerName;
                btn.dataset.shippingContactName = data.shippingContactName || data.customerName;
                btn.dataset.shippingAddress = data.shippingAddress || '';
                btn.dataset.shippingPhoneNumber = data.shippingPhoneNumber || '';
                btn.dataset.customerPhone = data.customerPhone || '';
            }

            orderList.classList.remove('d-none');
        } catch (err) {
            if (err.name === 'AbortError') return;
            loading.classList.add('d-none');
            errorBox.textContent = err.message || 'Không thể tải danh sách đơn hàng.';
            errorBox.classList.remove('d-none');
        }
    }

    #refreshRowHint(tr) {
        if (!tr) return;

        const productSelect = tr.querySelector('.bulk-row-item');
        if (!productSelect) return;

        const item = this.#itemsById.get(productSelect.value);
        const displayHintRemaining = tr.querySelector('.bulk-row-remaining');
        if (!item) {
            displayHintRemaining.innerHTML = '';
            return;
        }

        const remainingQty = this.#formatQty(item.remaining, item.decimalPlaces);
        let hintQtyHtml = `${remainingQty}`;
        const directRemainingQty = item.dsRemaining ?? 0;
        if (directRemainingQty > 0) {
            hintQtyHtml += ` · <span class="text-info"><i class="bi bi-send me-1"></i>${this.#formatQty(String(directRemainingQty, item.decimalPlaces))} giao thẳng</span>`;
        }
        displayHintRemaining.innerHTML = hintQtyHtml;
    }
    #syncRowWarehouse(tr) {
        if (!tr) return;
        const warehouseTd = tr.querySelector('.bulk-row-warehouse');
        if (!warehouseTd) return;
        // Always show warehouse — user explicitly toggles DS panel to choose direct ship
        warehouseTd.classList.remove('d-none');
    }

    #parseQty(value) {
        if (!value) return 0;
        const stripped = window.DecimalFields?.stripFormatting
            ? DecimalFields.stripFormatting(String(value), 2)
            : String(value).replace(/[^0-9.,]/g, '').replace(',', '.');
        const n = parseFloat(stripped);
        return isNaN(n) ? 0 : n;
    }
    #parseCurrency(value) {
        if (!value) return 0;
        const stripped = window.DecimalFields?.stripFormatting
            ? DecimalFields.stripFormatting(String(value))
            : String(value).replace(/[^0-9]/g, '');
        const n = parseFloat(stripped);
        return isNaN(n) ? 0 : n;
    }
    #formatCurrency(n, includeSymbol) {
        if (includeSymbol)
            return DecimalFields.formatCurrencyWithSymbol(String(Math.trunc(n)));
        return DecimalFields.formatCurrency(String(Math.trunc(n)));
    }
    #formatCurrencyWithSymbol(n) {
        return DecimalFields.formatCurrencyWithSymbol(String(Math.trunc(n)));
    }
    #formatQty(n, d) {
        return DecimalFields.formatQuantity(n, d);
    }

    #calculateTotals() {
        let subtotal = 0;
        this.#tbody.querySelectorAll('tr[data-row-index]').forEach(tr => {
            const itemId = tr.querySelector('.bulk-row-item')?.value;
            const qty = this.#parseQty(tr.querySelector('.bulk-row-qty')?.value);
            if (!itemId || qty <= 0) return;
            const item = this.#itemsById.get(itemId);
            if (!item) return;
            const overrideCost = this.#parseCurrency(tr.querySelector('.bulk-row-cost')?.value);
            const effectiveCost = overrideCost > 0 ? overrideCost : (item.unitCost ?? 0);
            subtotal += qty * effectiveCost;
        });

        const shipping = this.#parseCurrency(this.#shippingInput?.value);
        this.#shippingDisplayEl.textContent = this.#formatCurrencyWithSymbol(shipping);
        this.#shippingDisplayEl.closest('div').classList.toggle('d-none', shipping == 0);

        const taxRate = this.#parseCurrency(this.#taxRate.value);
        let tax = Math.round(subtotal * taxRate / 100);
        this.#taxHint.textContent = tax > 0
            ? `(${this.#formatCurrencyWithSymbol(tax)})`
            : '';
        this.#taxHint.classList.toggle('d-none', tax <= 0);
        this.#taxDisplayEl.textContent = this.#formatCurrencyWithSymbol(tax);
        this.#taxDisplayEl.closest('div').classList.toggle('d-none', tax == 0);

        this.#subtotalEl.textContent = this.#formatCurrencyWithSymbol(subtotal);
        this.#grandTotalEl.textContent = this.#formatCurrencyWithSymbol(subtotal + tax);
    }

    #onSubmit(e) {
        e.preventDefault();
        if (!isFormValid(this.#form))
            return;

        showPageLoading();
        this.#submitBtn.disabled = true;

        const fieldset = this.#form.querySelector('fieldset');

        const rows = Array.from(this.#tbody?.querySelectorAll('tr[data-row-index]') ?? []);
        const validLines = [];
        let firstInvalidRow = null;

        // Tổng qty theo item để chặn vượt remaining
        const totalsByItem = new Map();
        rows.forEach(tr => {
            const itemSel = tr.querySelector('.bulk-row-item');
            const qtyInput = tr.querySelector('.bulk-row-qty');
            const decimalPlaces = Number(tr.dataset.decimalPlaces) || 0;
            tr.classList.remove('table-danger');

            const itemId = itemSel?.value;
            const qty = this.#parseQty(qtyInput?.value);
            if (!itemId || qty <= 0) {
                if (itemId && qty <= 0) firstInvalidRow ??= tr;
                if (!itemId && qty > 0) firstInvalidRow ??= tr;
                return;
            }
            totalsByItem.set(itemId, (totalsByItem.get(itemId) ?? 0) + qty);
            validLines.push({ tr, itemId, qty, decimalPlaces });
        });

        // Kiểm tra vượt remaining
        for (const [itemId, total] of totalsByItem) {
            const item = this.#itemsById.get(itemId);
            if (!item) continue;
            if (total > item.remaining + 1e-9) {
                this.#showItemsError(`Tổng số lượng nhận của "${item.name}" (${this.#formatQty(total, item.decimalPlaces)}) vượt số còn lại (${this.#formatQty(item.remaining, item.decimalPlaces)}).`);
                validLines.filter(l => l.itemId === itemId).forEach(l => l.tr.classList.add('table-danger'));

                hidePageLoading();
                this.#submitBtn.disabled = false;
                return;
            }
        }

        if (validLines.length === 0) {
            this.#showItemsError('Vui lòng thêm ít nhất một dòng nhận hàng hợp lệ.');
            if (firstInvalidRow) firstInvalidRow.classList.add('table-danger');
            hidePageLoading();
            this.#submitBtn.disabled = false;
            return;
        }

        // Validate DS rows
        let dsValid = true;
        this.#tbody?.querySelectorAll('.bulk-ds-row:not(.d-none)').forEach(dsTr => {
            const orderItemId = dsTr.querySelector('.bulk-ds-order-item-id')?.value;
            const existingAllocationId = dsTr.querySelector('.bulk-ds-existing-allocation-id')?.value;
            const errorBox = dsTr.querySelector('.bulk-ds-error');
            if (!orderItemId && !existingAllocationId) {
                errorBox.textContent = 'Vui lòng chọn đơn hàng để giao thẳng.';
                errorBox.classList.remove('d-none');
                dsValid = false;
            } else {
                errorBox.classList.add('d-none');
            }
        });
        if (!dsValid) {
            hidePageLoading();
            this.#submitBtn.disabled = false;
            return;
        }

        this.#showItemsError()
        this.#form.submit();
        fieldset.disabled = true;
    }

    #showItemsError(errorMessage) {
        if (!errorMessage){
            this.#itemsError.classList.add('d-none');
            return;
        }
        this.#itemsError.textContent = errorMessage;
        this.#itemsError.classList.remove('d-none');
    }
}

function escapeHtml(str) {
    return String(str ?? '')
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;');
}

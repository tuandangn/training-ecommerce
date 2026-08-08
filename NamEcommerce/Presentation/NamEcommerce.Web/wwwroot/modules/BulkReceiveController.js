export default class BulkReceiveController {
    #purchaseOrderId;

    #modalEl;
    #form;
    #tbody;
    #addRowBtn;
    #submitBtn;
    #itemsError;

    #shippingInput;
    #taxInput;
    #taxHidden;
    #taxSuffix;
    #taxHint;
    #taxModeRadios;

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
        this.#addRowBtn = document.getElementById('bulkReceiveAddRow');
        this.#submitBtn = document.getElementById('bulkReceiveSubmit');
        this.#itemsError = document.getElementById('bulkReceiveItemsError');

        this.#shippingInput = document.getElementById('bulkAdditionalShipping');
        this.#taxInput = document.getElementById('bulkAdditionalTaxInput');
        this.#taxHidden = document.getElementById('bulkAdditionalTax');
        this.#taxSuffix = document.getElementById('bulkAdditionalTaxSuffix');
        this.#taxHint = document.getElementById('bulkAdditionalTaxHint');
        this.#taxModeRadios = document.querySelectorAll('input[name="bulkTaxMode"]');

        this.#subtotalEl = document.getElementById('bulkSubtotal');
        this.#shippingDisplayEl = document.getElementById('bulkShippingDisplay');
        this.#taxDisplayEl = document.getElementById('bulkTaxDisplay');
        this.#grandTotalEl = document.getElementById('bulkGrandTotal');

        this.#bindEvents();

        // Khi modal mở: reset và prefill 1 dòng cho mỗi item còn lại
        DecimalFields.autoWrap(this.#modalEl);
        this.#modalEl.addEventListener('show.bs.modal', () => this.#prefillRows());
    }

    #bindEvents() {
        this.#addRowBtn?.addEventListener('click', () => {
            this.#addRow();
            this.#recompute();
        });

        this.#tbody?.addEventListener('click', (e) => {
            const removeBtn = e.target.closest('.bulk-row-remove');
            if (removeBtn) {
                const tr = removeBtn.closest('tr');
                const idx = tr?.dataset.rowIndex;
                if (idx) this.#tbody?.querySelector(`[data-ds-row-for="${idx}"]`)?.remove();
                tr?.remove();
                this.#recompute();
                return;
            }

            const dsToggle = e.target.closest('.bulk-row-ds-toggle');
            if (dsToggle) {
                this.#toggleDsRow(dsToggle.closest('tr[data-row-index]'));
                return;
            }

            const dsCloseBtn = e.target.closest('.bulk-ds-row-close');
            if (dsCloseBtn) {
                const dsTr = dsCloseBtn.closest('tr.bulk-ds-row');
                const mainRowIdx = dsTr?.dataset.dsRowFor;
                if (mainRowIdx !== undefined) {
                    const mainTr = this.#tbody?.querySelector(`tr[data-row-index="${mainRowIdx}"]`);
                    if (mainTr) this.#toggleDsRow(mainTr);
                }
                return;
            }

            const dsOrderItemBtn = e.target.closest('.bulk-ds-order-item-btn');
            if (dsOrderItemBtn) {
                const dsTr = dsOrderItemBtn.closest('tr');
                dsTr.querySelectorAll('.bulk-ds-order-item-btn, .bulk-ds-existing-alloc-btn').forEach(b => b.classList.remove('active'));
                dsOrderItemBtn.classList.add('active');
                dsTr.querySelector('.bulk-ds-order-item-id').value = dsOrderItemBtn.dataset.orderItemId;
                dsTr.querySelector('.bulk-ds-order-id').value = dsOrderItemBtn.dataset.orderId;
                dsTr.querySelector('.bulk-ds-existing-allocation-id').value = '';
                const label = dsTr.querySelector('.bulk-ds-selected-label');
                if (label) label.textContent = `Đơn: ${dsOrderItemBtn.dataset.orderCode} · Khách: ${dsOrderItemBtn.dataset.customerName} · Có thể nhận: ${dsOrderItemBtn.dataset.availableToAllocate}`;
                const fields = dsTr.querySelector('.bulk-ds-fields');
                fields?.classList.remove('d-none');
                const addrInput = dsTr.querySelector('.bulk-ds-address-input');
                if (addrInput && !addrInput.value) addrInput.value = dsOrderItemBtn.dataset.shippingAddress || '';
                const phoneInput = dsTr.querySelector('.bulk-ds-contact-phone');
                if (phoneInput && !phoneInput.value) phoneInput.value = dsOrderItemBtn.dataset.customerPhone || '';
                const nameInput = dsTr.querySelector('.bulk-ds-contact-name');
                if (nameInput && !nameInput.value) nameInput.value = dsOrderItemBtn.dataset.customerName || '';
                return;
            }

            const dsExistingAllocBtn = e.target.closest('.bulk-ds-existing-alloc-btn');
            if (dsExistingAllocBtn) {
                const dsTr = dsExistingAllocBtn.closest('tr');
                dsTr.querySelectorAll('.bulk-ds-order-item-btn, .bulk-ds-existing-alloc-btn').forEach(b => b.classList.remove('active'));
                dsExistingAllocBtn.classList.add('active');
                dsTr.querySelector('.bulk-ds-existing-allocation-id').value = dsExistingAllocBtn.dataset.allocationId;
                dsTr.querySelector('.bulk-ds-order-item-id').value = '';
                dsTr.querySelector('.bulk-ds-order-id').value = '';
                const label = dsTr.querySelector('.bulk-ds-selected-label');
                if (label) label.textContent = `Nâng cấp: ${dsExistingAllocBtn.dataset.orderCode} · Khách: ${dsExistingAllocBtn.dataset.customerName} · Còn chờ: ${dsExistingAllocBtn.dataset.remainingQty}`;
                const fields = dsTr.querySelector('.bulk-ds-fields');
                fields?.classList.remove('d-none');
                const addrInput = dsTr.querySelector('.bulk-ds-address-input');
                if (addrInput && !addrInput.value) addrInput.value = dsExistingAllocBtn.dataset.shippingAddress || '';
                const phoneInput = dsTr.querySelector('.bulk-ds-contact-phone');
                if (phoneInput && !phoneInput.value) phoneInput.value = dsExistingAllocBtn.dataset.customerPhone || '';
                const nameInput = dsTr.querySelector('.bulk-ds-contact-name');
                if (nameInput && !nameInput.value) nameInput.value = dsExistingAllocBtn.dataset.customerName || '';
                return;
            }
        });

        this.#tbody?.addEventListener('change', (e) => {
            if (e.target.matches('.bulk-row-item')) {
                const tr = e.target.closest('tr');
                const item = this.#itemsById.get(e.target.value);
                const qtyInput = tr.querySelector('.bulk-row-qty');
                if (qtyInput && item) {
                    const oldDecimalPlaces = qtyInput.dataset.decimals;
                    const dp = String(item.decimalPlaces ?? 0);
                    qtyInput.dataset.decimals = dp;
                    qtyInput.dataset.decimalBound = '';
                    window.DecimalFields?.bindInput?.(qtyInput);
                    const dpInput = tr.querySelector('.bulk-row-decimal-places');
                    if (dpInput) dpInput.value = dp;
                    tr.dataset.decimalPlaces = dp;
                    if (oldDecimalPlaces != dp) {
                        qtyInput.value = '';
                    }
                }
                this.#refreshRowHint(tr);
                this.#syncRowWarehouse(tr);
                // Reload DS items if DS panel is open
                const idx = tr.dataset.rowIndex;
                const dsTr = this.#tbody?.querySelector(`[data-ds-row-for="${idx}"]`);
                if (dsTr && !dsTr.classList.contains('d-none')) {
                    this.#loadDsItems(tr, dsTr);
                }
            }
            this.#recompute();
        });
        this.#tbody?.addEventListener('input', (e) => {
            if (e.target.matches('.bulk-row-qty') || e.target.matches('.bulk-row-cost')) this.#recompute();
            if (e.target.matches('.bulk-row-qty')) this.#syncRowWarehouse(e.target.closest('tr'));
        });
        this.#tbody?.addEventListener('change', (e) => {
            if (e.target.matches('.bulk-row-qty')) this.#syncRowWarehouse(e.target.closest('tr'));
        });

        this.#shippingInput?.addEventListener('input', () => this.#recompute());
        this.#shippingInput?.addEventListener('change', () => this.#recompute());
        this.#taxInput?.addEventListener('input', () => this.#recompute());
        this.#taxInput?.addEventListener('change', () => this.#recompute());

        this.#taxModeRadios.forEach(radio => {
            radio.addEventListener('change', () => {
                if (radio.checked) {
                    this.#taxMode = radio.value;
                    this.#taxSuffix?.classList.toggle('d-none', this.#taxMode !== 'percent');
                    if (this.#taxInput) this.#taxInput.value = '';
                    this.#recompute();
                }
            });
        });

        this.#form?.addEventListener('submit', (e) => this.#onSubmit(e));
    }

    #prefillRows() {
        if (!this.#tbody) return;
        this.#tbody.innerHTML = '';
        this.#rowSeq = 0;
        this.#items.forEach(item => this.#addRow(item));
        this.#recompute();
    }

    #addRow(item) {
        if (!this.#tbody) return;
        const presetItemId = item?.id ?? '';
        const presetQty = item?.remaining ?? 0;

        const idx = this.#rowSeq++;
        const tr = document.createElement('tr');
        tr.classList.add('align-top');
        tr.dataset.rowIndex = String(idx);
        tr.dataset.decimalPlaces = item?.decimalPlaces ?? 0;
        tr.dataset.unitMeasurement = item?.unitMeasurement ?? '';

        const itemOptions = this.#items.map(it =>
            `<option value="${escapeHtml(it.id)}" ${it.id === presetItemId ? 'selected' : ''}>${escapeHtml(it.name)}</option>`
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
                <select name="Items[${idx}].ItemId"
                        class="form-select form-select-sm bulk-row-item">
                    <option value="">-- Chọn hàng hóa --</option>
                    ${itemOptions}
                </select>
            </td>
            <td class="text-end pe-2">
                <input name="Items[${idx}].Quantity" inputmode="decimal"
                       class="form-control form-control-sm text-end bulk-row-qty"
                       data-decimal="quantity" data-decimals="${item?.decimalPlaces ?? 0}" value="${escapeHtml(qtyValue)}" placeholder="0" />
                <input type="hidden" name="Items[${idx}].QuantityDecimalPlaces" class="bulk-row-decimal-places" value="${item?.decimalPlaces ?? 0}" />
                <div class="small text-muted mt-1 bulk-row-hint-qty"></div>
            </td>
            <td class="text-end pe-2">
                <input name="Items[${idx}].ActualUnitCost" inputmode="numeric"
                       class="form-control form-control-sm text-end bulk-row-cost"
                       data-decimal="currency" value="${this.#formatCurrency(item?.unitCost ?? 0)}" 
                       placeholder="Giá vốn" />
                <div class="small text-muted mt-1 bulk-row-hint-cost"></div>
            </td>
            <td class="ps-2">
                <select name="Items[${idx}].WarehouseId"
                        class="form-select form-select-sm bulk-row-warehouse">
                    ${warehouseOptions}
                </select>
            </td>
            <td class="pe-2 text-nowrap text-end">
                <button type="button" class="btn btn-sm btn-outline-secondary bulk-row-ds-toggle" title="Thiết lập giao thẳng cho dòng này">
                    <i class="bi bi-send me-1"></i>GT
                </button>
                <button type="button" class="btn btn-link btn-sm text-danger p-0 ms-1 bulk-row-remove" title="Xóa dòng">
                    <i class="bi bi-trash"></i>
                </button>
            </td>`;

        this.#tbody.appendChild(tr);
        window.DecimalFields?.autoWrap?.(tr);
        this.#refreshRowHint(tr);
        this.#syncRowWarehouse(tr);

        // DS sub-row (hidden by default)
        const dsTr = document.createElement('tr');
        dsTr.className = 'bulk-ds-row d-none';
        dsTr.dataset.dsRowFor = String(idx);
        dsTr.innerHTML = `
            <td colspan="5" class="border-top-0 pt-0 pb-2 px-2">
                <div class="p-2 bg-primary bg-opacity-10 rounded-2 border border-primary border-opacity-25">
                    <div class="d-flex justify-content-between align-items-center mb-2">
                        <span class="small fw-semibold text-primary"><i class="bi bi-send me-1"></i>Giao thẳng cho đơn hàng</span>
                        <button type="button" class="btn-close btn-sm bulk-ds-row-close" title="Hủy giao thẳng"></button>
                    </div>
                    <input type="hidden" name="Items[${idx}].DirectShipOrderItemId" class="bulk-ds-order-item-id" value="" />
                    <input type="hidden" name="Items[${idx}].DirectShipOrderId" class="bulk-ds-order-id" value="" />
                    <input type="hidden" name="Items[${idx}].DirectShipExistingAllocationId" class="bulk-ds-existing-allocation-id" value="" />
                    <div class="bulk-ds-loading d-none text-muted small py-1">
                        <span class="spinner-border spinner-border-sm me-1"></span> Đang tải đơn hàng...
                    </div>
                    <div class="bulk-ds-empty d-none alert alert-warning py-1 small mb-2">
                        <i class="bi bi-exclamation-circle me-1"></i> Không có đơn hàng phù hợp cho sản phẩm này.
                    </div>
                    <div class="bulk-ds-order-list d-none">
                        <p class="small fw-semibold text-muted text-uppercase mb-1">Chọn đơn hàng bán hàng</p>
                        <div class="bulk-ds-order-items list-group list-group-flush border rounded mb-2" style="max-height:130px;overflow-y:auto"></div>
                    </div>
                    <div class="bulk-ds-fields d-none">
                        <p class="small fw-semibold mb-2 bulk-ds-selected-label text-primary"></p>
                        <div class="mb-2">
                            <label class="form-label form-label-sm text-muted mb-1">Địa chỉ giao hàng <span class="text-danger">*</span></label>
                            <input type="text" name="Items[${idx}].DirectShipAddress" class="form-control form-control-sm bulk-ds-address-input" placeholder="Nhập địa chỉ giao" />
                        </div>
                        <div class="row g-2">
                            <div class="col-6">
                                <label class="form-label form-label-sm text-muted mb-1">Tên người nhận</label>
                                <input type="text" name="Items[${idx}].DirectShipContactName" class="form-control form-control-sm bulk-ds-contact-name" placeholder="Tùy chọn" />
                            </div>
                            <div class="col-6">
                                <label class="form-label form-label-sm text-muted mb-1">Số điện thoại <span class="text-danger">*</span></label>
                                <input type="text" name="Items[${idx}].DirectShipContactPhone" class="form-control form-control-sm bulk-ds-contact-phone"
                                    placeholder="Bắt buộc" inputmode="tel"/>
                            </div>
                        </div>
                    </div>
                    <div class="bulk-ds-error d-none alert alert-danger py-1 small mb-0"></div>
                </div>
            </td>`;
        this.#tbody.appendChild(dsTr);
    }

    #toggleDsRow(tr) {
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
                this.#loadDsItems(tr, dsTr);
            }
        } else {
            dsTr.classList.add('d-none');
            dsTr.querySelector('.bulk-ds-order-item-id').value = '';
            dsTr.querySelector('.bulk-ds-order-id').value = '';
            dsTr.querySelector('.bulk-ds-existing-allocation-id').value = '';
            btn?.classList.replace('btn-outline-primary', 'btn-outline-secondary');
        }
    }

    async #loadDsItems(tr, dsTr) {
        const rowIdx = dsTr.dataset.dsRowFor;
        this.#dsAbortControllers.get(rowIdx)?.abort();
        const controller = new AbortController();
        this.#dsAbortControllers.set(rowIdx, controller);

        const itemId = tr.querySelector('.bulk-row-item')?.value;
        const loading = dsTr.querySelector('.bulk-ds-loading');
        const empty = dsTr.querySelector('.bulk-ds-empty');
        const orderList = dsTr.querySelector('.bulk-ds-order-list');
        const orderItems = dsTr.querySelector('.bulk-ds-order-items');
        const fields = dsTr.querySelector('.bulk-ds-fields');
        const errorBox = dsTr.querySelector('.bulk-ds-error');

        loading.classList.remove('d-none');
        empty.classList.add('d-none');
        orderList.classList.add('d-none');
        fields.classList.add('d-none');
        errorBox.classList.add('d-none');
        dsTr.querySelector('.bulk-ds-order-item-id').value = '';
        dsTr.querySelector('.bulk-ds-order-id').value = '';

        if (!itemId) {
            loading.classList.add('d-none');
            empty.classList.remove('d-none');
            return;
        }

        try {
            const [eligibleResp, nonDsResp] = await Promise.all([
                fetch(`/PurchaseOrder/EligibleOrderItems?purchaseOrderId=${this.#purchaseOrderId}&purchaseOrderItemId=${itemId}`, { signal: controller.signal }),
                fetch(`/PurchaseOrder/NonDirectShipAllocations?purchaseOrderId=${this.#purchaseOrderId}&purchaseOrderItemId=${itemId}`, { signal: controller.signal })
            ]);
            if (!eligibleResp.ok || !nonDsResp.ok) throw new Error('Lỗi tải dữ liệu');
            const [eligibleData, nonDsData] = await Promise.all([eligibleResp.json(), nonDsResp.json()]);
            loading.classList.add('d-none');
            dsTr.dataset.dsLoadedFor = itemId;

            if (!eligibleData?.length && !nonDsData?.length) {
                empty.classList.remove('d-none');
                return;
            }

            orderItems.innerHTML = '';

            if (eligibleData?.length) {
                const header = document.createElement('div');
                header.className = 'px-3 py-1 text-muted small fw-semibold bg-light border-bottom';
                header.textContent = 'Tạo liên kết & giao thẳng';
                orderItems.appendChild(header);
                eligibleData.forEach(order => {
                    const btn = document.createElement('button');
                    btn.type = 'button';
                    btn.className = 'list-group-item list-group-item-action py-2 px-3 bulk-ds-order-item-btn';
                    btn.dataset.orderItemId = order.orderItemId;
                    btn.dataset.orderId = order.orderId;
                    btn.dataset.orderCode = order.orderCode;
                    btn.dataset.customerName = order.customerName;
                    btn.dataset.availableToAllocate = order.availableToAllocate;
                    btn.dataset.shippingAddress = order.shippingAddress || '';
                    btn.dataset.customerPhone = order.customerPhone || '';
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
                header.textContent = 'Nâng cấp phân bổ hiện có lên giao thẳng';
                orderItems.appendChild(header);
                nonDsData.forEach(alloc => {
                    const btn = document.createElement('button');
                    btn.type = 'button';
                    btn.className = 'list-group-item list-group-item-action py-2 px-3 bulk-ds-existing-alloc-btn';
                    btn.dataset.allocationId = alloc.allocationId;
                    btn.dataset.orderCode = alloc.orderCode;
                    btn.dataset.customerName = alloc.customerName;
                    btn.dataset.remainingQty = alloc.remainingQty;
                    btn.dataset.shippingAddress = alloc.shippingAddress || '';
                    btn.dataset.customerPhone = alloc.customerPhone || '';
                    btn.innerHTML = `<div class="d-flex justify-content-between align-items-center">
                        <span><strong>${escapeHtml(alloc.orderCode)}</strong> <span class="text-muted small">${escapeHtml(alloc.customerName)}</span></span>
                        <span class="badge bg-warning text-dark ms-2">Còn ${alloc.remainingQty}</span>
                    </div>`;
                    orderItems.appendChild(btn);
                });
            }

            orderList.classList.remove('d-none');
        } catch (err) {
            if (err.name === 'AbortError') return;
            loading.classList.add('d-none');
            errorBox.textContent = err.message || 'Không thể tải danh sách đơn hàng.';
            errorBox.classList.remove('d-none');
        }
    }

    #syncRowWarehouse(tr) {
        if (!tr) return;
        const warehouseTd = tr.querySelector('.bulk-row-warehouse');
        if (!warehouseTd) return;
        // Always show warehouse — user explicitly toggles DS panel to choose direct ship
        warehouseTd.classList.remove('d-none');
    }

    #refreshRowHint(tr) {
        if (!tr) return;
        const productSelect = tr.querySelector('.bulk-row-item');
        if (!productSelect) return;
        const item = this.#itemsById.get(productSelect.value);
        const displayHintQty = tr.querySelector('.bulk-row-hint-qty');
        const displayHintCost = tr.querySelector('.bulk-row-hint-cost');
        if (!item) {
            displayHintQty.innerHTML = '';
            displayHintCost.innerHTML = '';
            return;
        }

        if (displayHintQty) {
            const itemQtyUnique = `hintQty${item.id}`;
            displayHintQty.dataset.id = itemQtyUnique;
            const decimalPlaces = Number(tr.dataset.decimalPlaces) || 0;
            const remainingQty = this.#formatQty(item.remaining, decimalPlaces);
            let hintQtyHtml = `Còn ${remainingQty}`;
            const directRemainingQty = item.dsRemaining ?? 0;
            if (directRemainingQty > 0) {
                hintQtHtml += ` · <span class="text-info"><i class="bi bi-send me-1"></i>${this.#formatQty(String(directRemainingQty, decimalPlaces))} giao thẳng</span>`;
            }
            displayHintQty.innerHTML = hintQtyHtml;
        }

        if (displayHintCost) {
            const itemCostUnique = `hintCost${item.id}`;
            displayHintCost.dataset.id = itemCostUnique;
            displayHintCost.innerHTML = `Giá đặt ${this.#formatCurrencyWithSymbol(item.unitCost)}`;
        }
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

    #recompute() {
        let subtotal = 0;
        this.#tbody?.querySelectorAll('tr[data-row-index]').forEach(tr => {
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
        const taxRaw = this.#parseCurrency(this.#taxInput?.value);
        let tax = 0;
        if (this.#taxMode === 'percent') {
            tax = Math.round(subtotal * taxRaw / 100);
            if (this.#taxHint) {
                this.#taxHint.textContent = taxRaw > 0
                    ? `= ${this.#formatCurrencyWithSymbol(tax)} (${taxRaw}% × tạm tính ${this.#formatCurrencyWithSymbol(subtotal)})`
                    : '';
                this.#taxHint.classList.toggle('d-none', taxRaw <= 0);
            }
        } else {
            tax = taxRaw;
            if (this.#taxHint) {
                this.#taxHint.textContent = '';
                this.#taxHint.classList.add('d-none');
            }
        }
        if (this.#taxHidden) this.#taxHidden.value = String(tax);

        if (this.#subtotalEl) this.#subtotalEl.textContent = this.#formatCurrencyWithSymbol(subtotal);
        if (this.#shippingDisplayEl) {
            this.#shippingDisplayEl.textContent = this.#formatCurrencyWithSymbol(shipping);
            this.#shippingDisplayEl.closest('div').classList.toggle('d-none', shipping == 0);
        }
        if (this.#taxDisplayEl) {
            this.#taxDisplayEl.textContent = this.#formatCurrencyWithSymbol(tax);
            this.#taxDisplayEl.closest('div').classList.toggle('d-none', tax == 0);
        }
        if (this.#grandTotalEl) this.#grandTotalEl.textContent = this.#formatCurrencyWithSymbol(subtotal + tax);
    }

    #onSubmit(e) {
        e.preventDefault();
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
                this.#itemsError.textContent = `Tổng số lượng nhận của "${item.name}" (${this.#formatQty(total, item.decimalPlaces)}) vượt số còn lại (${this.#formatQty(item.remaining, item.decimalPlaces)}).`;
                this.#itemsError.classList.remove('d-none');
                validLines.filter(l => l.itemId === itemId).forEach(l => l.tr.classList.add('table-danger'));

                hidePageLoading();
                this.#submitBtn.disabled = false;
                return;
            }
        }

        if (validLines.length === 0) {
            this.#itemsError.textContent = 'Vui lòng thêm ít nhất một dòng nhận hàng hợp lệ.';
            this.#itemsError.classList.remove('d-none');
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
            const phone = dsTr.querySelector('.bulk-ds-contact-phone')?.value?.trim();
            const address = dsTr.querySelector('.bulk-ds-address-input')?.value?.trim();
            const errorBox = dsTr.querySelector('.bulk-ds-error');
            if (!orderItemId && !existingAllocationId) {
                errorBox.textContent = 'Vui lòng chọn đơn hàng để giao thẳng.';
                errorBox.classList.remove('d-none');
                dsValid = false;
            } else if (!phone) {
                errorBox.textContent = 'Vui lòng nhập số điện thoại nhận hàng giao thẳng.';
                errorBox.classList.remove('d-none');
                dsValid = false;
            } else if (!address) {
                errorBox.textContent = 'Vui lòng nhập địa chỉ giao hàng.';
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

        this.#itemsError.classList.add('d-none');
        this.#form.submit();
        fieldset.disabled = true;
    }
}

function escapeHtml(str) {
    return String(str ?? '')
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;');
}

/**
 * BulkReceiveController
 * Quản lý modal "Nhận nhiều hàng" trong trang PurchaseOrder/Details.
 *
 * Đọc dữ liệu khởi tạo từ <script type="application/json" id="bulkReceiveData">.
 * Mỗi dòng (row) = 1 lần nhận: chọn item, kho và số lượng.
 * Cùng 1 item có thể có nhiều dòng vào các kho khác nhau — kiểm tra aggregate
 * qty không vượt RemainingQuantity.
 */
export default class BulkReceiveController {
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
                this.#toggleDsRow(dsToggle.closest('tr'));
                return;
            }

            const dsOrderItemBtn = e.target.closest('.bulk-ds-order-item-btn');
            if (dsOrderItemBtn) {
                const dsTr = dsOrderItemBtn.closest('tr');
                dsTr.querySelectorAll('.bulk-ds-order-item-btn').forEach(b => b.classList.remove('active'));
                dsOrderItemBtn.classList.add('active');
                dsTr.querySelector('.bulk-ds-order-item-id').value = dsOrderItemBtn.dataset.orderItemId;
                const label = dsTr.querySelector('.bulk-ds-selected-label');
                if (label) label.textContent = `${dsOrderItemBtn.dataset.orderCode} · ${dsOrderItemBtn.dataset.customerName} · Còn: ${dsOrderItemBtn.dataset.availableToAllocate}`;
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
        });

        this.#tbody?.addEventListener('change', (e) => {
            if (e.target.matches('.bulk-row-item')) {
                const tr = e.target.closest('tr');
                this.#refreshRowHint(tr);
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
        this.#items.forEach(item => this.#addRow(item.id, item.remaining));
        this.#recompute();
    }

    #addRow(presetItemId = '', presetQty = '') {
        if (!this.#tbody) return;
        const idx = this.#rowSeq++;
        const tr = document.createElement('tr');
        tr.dataset.rowIndex = String(idx);

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
            <td class="ps-0">
                <select name="Items[${idx}].ItemId"
                        class="form-select form-select-sm bulk-row-item">
                    <option value="">-- Chọn hàng hóa --</option>
                    ${itemOptions}
                </select>
                <div class="small text-muted mt-1 bulk-row-hint"></div>
            </td>
            <td>
                <select name="Items[${idx}].WarehouseId"
                        class="form-select form-select-sm bulk-row-warehouse">
                    ${warehouseOptions}
                </select>
            </td>
            <td class="text-end">
                <input name="Items[${idx}].Quantity"
                       class="form-control form-control-sm text-end bulk-row-qty no-additional-element no-hint"
                       data-decimal="quantity" value="${escapeHtml(qtyValue)}" placeholder="0" />
            </td>
            <td class="text-end">
                <input name="Items[${idx}].ActualUnitCost"
                       class="form-control form-control-sm text-end bulk-row-cost no-additional-element no-hint"
                       data-decimal="currency" value="" placeholder="giá PO" title="Để trống = dùng giá PO" />
            </td>
            <td class="text-end pe-0" style="white-space:nowrap">
                <button type="button" class="btn btn-link btn-sm text-secondary p-0 me-1 bulk-row-ds-toggle" title="Giao thẳng cho khách hàng">
                    <i class="bi bi-send"></i>
                </button>
                <button type="button" class="btn btn-link btn-sm text-danger p-0 bulk-row-remove" title="Xóa dòng">
                    <i class="bi bi-trash"></i>
                </button>
            </td>`;

        this.#tbody.appendChild(tr);
        window.DecimalFields?.autoWrap?.(tr);
        this.#refreshRowHint(tr);

        // DS sub-row (hidden by default)
        const dsTr = document.createElement('tr');
        dsTr.className = 'bulk-ds-row d-none';
        dsTr.dataset.dsRowFor = String(idx);
        dsTr.innerHTML = `
            <td colspan="5" class="border-top-0 pt-0 pb-2 px-2">
                <div class="p-2 bg-light rounded-2 border">
                    <input type="hidden" name="Items[${idx}].DirectShipOrderItemId" class="bulk-ds-order-item-id" value="" />
                    <div class="bulk-ds-loading d-none text-muted small py-1">
                        <span class="spinner-border spinner-border-sm me-1"></span> Đang tải đơn hàng...
                    </div>
                    <div class="bulk-ds-empty d-none alert alert-warning py-1 small mb-2">
                        <i class="bi bi-exclamation-circle me-1"></i> Không có đơn hàng phù hợp cho sản phẩm này.
                    </div>
                    <div class="bulk-ds-order-list d-none">
                        <p class="small fw-semibold text-muted text-uppercase mb-1">Chọn đơn hàng</p>
                        <div class="bulk-ds-order-items list-group list-group-flush border rounded mb-2" style="max-height:130px;overflow-y:auto"></div>
                    </div>
                    <div class="bulk-ds-fields d-none">
                        <p class="small fw-semibold text-muted mb-1 bulk-ds-selected-label text-primary"></p>
                        <div class="mb-2">
                            <input type="text" name="Items[${idx}].DirectShipAddress" class="form-control form-control-sm bulk-ds-address-input" placeholder="Địa chỉ giao hàng *" />
                        </div>
                        <div class="row g-2">
                            <div class="col-6">
                                <input type="text" name="Items[${idx}].DirectShipContactName" class="form-control form-control-sm bulk-ds-contact-name" placeholder="Tên người nhận" />
                            </div>
                            <div class="col-6">
                                <input type="text" name="Items[${idx}].DirectShipContactPhone" class="form-control form-control-sm bulk-ds-contact-phone" placeholder="Số điện thoại" />
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
        const icon = tr.querySelector('.bulk-row-ds-toggle i');

        const warehouseCell = tr.querySelector('.bulk-row-warehouse');
        if (dsTr.classList.contains('d-none')) {
            dsTr.classList.remove('d-none');
            icon?.classList.replace('text-secondary', 'text-primary');
            warehouseCell?.closest('td')?.classList.add('d-none');
            const itemId = tr.querySelector('.bulk-row-item')?.value;
            if (!itemId || dsTr.dataset.dsLoadedFor !== itemId) {
                this.#loadDsItems(tr, dsTr);
            }
        } else {
            dsTr.classList.add('d-none');
            dsTr.querySelector('.bulk-ds-order-item-id').value = '';
            icon?.classList.replace('text-primary', 'text-secondary');
            warehouseCell?.closest('td')?.classList.remove('d-none');
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

        if (!itemId) {
            loading.classList.add('d-none');
            empty.classList.remove('d-none');
            return;
        }

        try {
            const resp = await fetch(`/PurchaseOrder/EligibleOrderItems?purchaseOrderItemId=${itemId}`, { signal: controller.signal });
            if (!resp.ok) throw new Error('Lỗi tải dữ liệu');
            const data = await resp.json();
            loading.classList.add('d-none');
            dsTr.dataset.dsLoadedFor = itemId;

            if (!data || !data.length) {
                empty.classList.remove('d-none');
                return;
            }

            orderItems.innerHTML = '';
            data.forEach(order => {
                const btn = document.createElement('button');
                btn.type = 'button';
                btn.className = 'list-group-item list-group-item-action py-2 px-3 bulk-ds-order-item-btn';
                btn.dataset.orderItemId = order.orderItemId;
                btn.dataset.orderCode = order.orderCode;
                btn.dataset.customerName = order.customerName;
                btn.dataset.availableToAllocate = order.availableToAllocate;
                btn.dataset.shippingAddress = order.shippingAddress || '';
                btn.dataset.customerPhone = order.customerPhone || '';
                btn.innerHTML = `<div class="d-flex justify-content-between align-items-center">
                    <span><strong>${escapeHtml(order.orderCode)}</strong> <span class="text-muted small">${escapeHtml(order.customerName)}</span></span>
                    <span class="badge bg-secondary ms-2">Còn ${order.availableToAllocate}</span>
                </div>`;
                orderItems.appendChild(btn);
            });
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
        const select = tr.querySelector('.bulk-row-item');
        const hint = tr.querySelector('.bulk-row-hint');
        if (!select || !hint) return;
        const item = this.#itemsById.get(select.value);
        if (!item) {
            hint.textContent = '';
            return;
        }
        const remaining = this.#formatQty(item.remaining);
        const unitCost = this.#formatCurrency(item.unitCost);
        hint.textContent = `Còn lại: ${remaining} • Giá vốn: ${unitCost}`;
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
            ? DecimalFields.stripFormatting(String(value), false)
            : String(value).replace(/[^0-9]/g, '');
        const n = parseFloat(stripped);
        return isNaN(n) ? 0 : n;
    }
    #formatCurrency(n) {
        if (window.DecimalFields?.formatCurrency)
            return DecimalFields.formatCurrency(String(Math.trunc(n)));
        return String(Math.trunc(n));
    }
    #formatQty(n) {
        if (window.DecimalFields?.formatQuantity)
            return DecimalFields.formatQuantity(n);
        return String(n);
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
                    ? `= ${this.#formatCurrency(tax)} (${taxRaw}% × tạm tính ${this.#formatCurrency(subtotal)})`
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

        if (this.#subtotalEl) this.#subtotalEl.textContent = this.#formatCurrency(subtotal);
        if (this.#shippingDisplayEl) this.#shippingDisplayEl.textContent = this.#formatCurrency(shipping);
        if (this.#taxDisplayEl) this.#taxDisplayEl.textContent = this.#formatCurrency(tax);
        if (this.#grandTotalEl) this.#grandTotalEl.textContent = this.#formatCurrency(subtotal + shipping + tax);
    }

    #onSubmit(e) {
        const rows = Array.from(this.#tbody?.querySelectorAll('tr[data-row-index]') ?? []);
        const validLines = [];
        let firstInvalidRow = null;

        // Tổng qty theo item để chặn vượt remaining
        const totalsByItem = new Map();
        rows.forEach(tr => {
            const itemSel = tr.querySelector('.bulk-row-item');
            const qtyInput = tr.querySelector('.bulk-row-qty');
            tr.classList.remove('table-danger');

            const itemId = itemSel?.value;
            const qty = this.#parseQty(qtyInput?.value);
            if (!itemId || qty <= 0) {
                if (itemId && qty <= 0) firstInvalidRow ??= tr;
                if (!itemId && qty > 0) firstInvalidRow ??= tr;
                return;
            }
            totalsByItem.set(itemId, (totalsByItem.get(itemId) ?? 0) + qty);
            validLines.push({ tr, itemId, qty });
        });

        // Kiểm tra vượt remaining
        for (const [itemId, total] of totalsByItem) {
            const item = this.#itemsById.get(itemId);
            if (!item) continue;
            if (total > item.remaining + 1e-9) {
                e.preventDefault();
                this.#itemsError.textContent = `Tổng số lượng nhận của "${item.name}" (${this.#formatQty(total)}) vượt số còn lại (${this.#formatQty(item.remaining)}).`;
                this.#itemsError.classList.remove('d-none');
                validLines.filter(l => l.itemId === itemId).forEach(l => l.tr.classList.add('table-danger'));
                return;
            }
        }

        if (validLines.length === 0) {
            e.preventDefault();
            this.#itemsError.textContent = 'Vui lòng thêm ít nhất một dòng nhận hàng hợp lệ.';
            this.#itemsError.classList.remove('d-none');
            if (firstInvalidRow) firstInvalidRow.classList.add('table-danger');
            return;
        }

        // Validate DS rows
        let dsValid = true;
        this.#tbody?.querySelectorAll('.bulk-ds-row:not(.d-none)').forEach(dsTr => {
            const orderItemId = dsTr.querySelector('.bulk-ds-order-item-id')?.value;
            const address = dsTr.querySelector('.bulk-ds-address-input')?.value?.trim();
            const errorBox = dsTr.querySelector('.bulk-ds-error');
            if (!orderItemId) {
                errorBox.textContent = 'Vui lòng chọn đơn hàng để giao thẳng.';
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
            e.preventDefault();
            return;
        }

        this.#itemsError.classList.add('d-none');
    }
}

function escapeHtml(str) {
    return String(str ?? '')
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;');
}

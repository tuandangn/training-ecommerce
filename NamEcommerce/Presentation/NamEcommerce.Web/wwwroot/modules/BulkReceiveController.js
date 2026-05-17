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
            console.log('vo');
            this.#addRow();
            this.#recompute();
        });

        this.#tbody?.addEventListener('click', (e) => {
            const removeBtn = e.target.closest('.bulk-row-remove');
            if (removeBtn) {
                removeBtn.closest('tr')?.remove();
                this.#recompute();
            }
        });

        this.#tbody?.addEventListener('change', (e) => {
            if (e.target.matches('.bulk-row-item')) {
                this.#refreshRowHint(e.target.closest('tr'));
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
                    // Khi đổi mode: clear input để tránh người dùng nhầm 10% = 10đ
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
            <td class="text-end pe-0">
                <button type="button" class="btn btn-link btn-sm text-danger p-0 bulk-row-remove" title="Xóa dòng">
                    <i class="bi bi-trash"></i>
                </button>
            </td>`;

        this.#tbody.appendChild(tr);
        window.DecimalFields?.autoWrap?.(tr);
        this.#refreshRowHint(tr);
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
        const dsPart = item.directShipQty > 0 ? ` • Giao thẳng: ${this.#formatQty(item.directShipQty)} đv` : '';
        hint.textContent = `Còn lại: ${remaining} • Giá vốn: ${unitCost}${dsPart}`;
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
        this.#tbody?.querySelectorAll('tr').forEach(tr => {
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
        const rows = Array.from(this.#tbody?.querySelectorAll('tr') ?? []);
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
                // Bỏ qua dòng rỗng (cho phép user xóa nhanh mà không cần remove)
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

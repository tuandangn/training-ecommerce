import ProductPicker from "/modules/ProductPicker.js";
import ProductBrowser from "/modules/ProductBrowser.js";
import { getWarehouseSettings } from "/modules/Settings.js";

export default class GoodsReceiptCreateController {
    #state = {};
    #productPicker = null;
    #browser = null;
    #warehouseSettings;
    #warehouseOptions;

    constructor() {
        this.#warehouseSettings = getWarehouseSettings();
        this.#bindModal();

        const warehouseSelect = document.getElementById('WarehouseId');
        warehouseSelect.addEventListener('change', (e) => {
            this.#setState({ warehouse: e.target.value });
        });

        const warehouse = warehouseSelect.value;
        this.#bindTableEvents();

        const browserEl = document.getElementById('productBrowser');
        if (browserEl) {
            this.#browser = new ProductBrowser(
                browserEl,
                (product) => this.#addOrIncrementItem(product),
                { purchase: true }
            );
            this.#browser.init();
        }

        this.#setState({ warehouse });

        this.#warehouseOptions = Array.from(warehouseSelect.querySelectorAll('option'))
            .filter(option => option.value)
            .map(option => ({ value: option.value, label: option.label }));
    }

    #setState(patch) {
        this.#state = Object.assign({}, this.#state, patch);
    }

    #bindModal() {
        const pickerEl = document.getElementById('productPicker');
        if (!pickerEl) return;

        this.#productPicker = new ProductPicker(pickerEl, { purchase: true });

        pickerEl.addEventListener('select', (e) => {
            const product = e.detail?.product;
            if (!product) return;

            document.getElementById('modalProductId').value = product.id;
            pickerEl.dataset.selectedName = product.name;
            pickerEl.dataset.selectedPicture = product.picture ?? '';
            pickerEl.dataset.selectedDecimalPlaces = product.quantityDecimalPlaces ?? 0;

            const modalQtyInput = document.getElementById('modalQty');
            if (modalQtyInput) {
                modalQtyInput.dataset.decimals = String(product.quantityDecimalPlaces ?? 0);
                modalQtyInput.dataset.decimalBound = '';
                window.DecimalFields?.bindInput?.(modalQtyInput);
            }

            document.getElementById('modalProductDetails')?.classList.remove('d-none');
            document.getElementById('btnAddItemConfirm')?.classList.remove('d-none');
            document.getElementById('productPickerError')?.classList.add('d-none');
        });

        pickerEl.addEventListener('remove', () => {
            document.getElementById('modalProductId').value = '';
            document.getElementById('modalProductDetails')?.classList.add('d-none');
            document.getElementById('btnAddItemConfirm')?.classList.add('d-none');
        });

        document.getElementById('modalWarehouseId')?.addEventListener('change', (e) => {
            if (!this.#warehouseSettings.AllowNonWarehouse && !e.target.value) {
                document.getElementById('modalWarehouseError')?.classList.remove('d-none');
                return;
            }
            document.getElementById('modalWarehouseError')?.classList.add('d-none');
        });

        document.getElementById('btnAddItemConfirm')?.addEventListener('click', () => {
            const productId = document.getElementById('modalProductId')?.value;
            const productName = pickerEl.dataset.selectedName ?? '—';
            const productPicture = pickerEl.dataset.selectedPicture ?? '';
            const warehouseId = document.getElementById('modalWarehouseId')?.value ?? '';
            const qtyRaw = document.getElementById('modalQty')?.value ?? '0';
            const costRaw = document.getElementById('modalUnitCost')?.value ?? '';

            if (!productId) {
                document.getElementById('productPickerError')?.classList.remove('d-none');
                return;
            }

            const qty = parseFloat(DecimalFields.stripFormatting(qtyRaw, 2));
            if (!qty || qty <= 0) {
                document.getElementById('modalQtyError')?.classList.remove('d-none');
                return;
            }

            if (!this.#warehouseSettings.AllowNonWarehouse && !warehouseId) {
                document.getElementById('modalWarehouseError')?.classList.remove('d-none');
                return;
            }

            const cost = costRaw ? (parseFloat(DecimalFields.stripFormatting(costRaw)) || null) : null;
            if (cost < 0) {
                document.getElementById('modalUnitCostError')?.classList.remove('d-none');
                return;
            }

            const decimalPlaces = parseInt(pickerEl.dataset.selectedDecimalPlaces || '0', 10);
            this.#addItemToTable(
                { id: productId, name: productName, picture: productPicture, quantityDecimalPlaces: decimalPlaces },
                qty, cost, warehouseId
            );

            bootstrap.Modal.getOrCreateInstance(document.getElementById('addItemModal')).hide();
            this.#resetModal();
        });

        document.getElementById('addItemModal')?.addEventListener('show.bs.modal', () => {
            if (this.#state.warehouse) {
                document.getElementById('modalWarehouseId').disabled = true;
                document.getElementById('modalWarehouseId').value = this.#state.warehouse;
            }
        });

        document.getElementById('addItemModal')?.addEventListener('hidden.bs.modal', () => this.#resetModal());
    }

    #bindTableEvents() {
        document.getElementById('itemsTableBody')?.addEventListener('click', (e) => {
            const btn = e.target.closest('.btn-remove-item');
            if (!btn) return;
            btn.closest('tr').remove();
            this.#updateNoItemsVisibility();
            this.#reindexTableRows();
        });
    }

    #updateNoItemsVisibility() {
        const tbody = document.getElementById('itemsTableBody');
        const msg = document.getElementById('noItemsMessage');
        if (!tbody || !msg) return;
        msg.style.display = tbody.querySelectorAll('tr').length > 0 ? 'none' : 'block';
    }

    #reindexTableRows() {
        const tbody = document.getElementById('itemsTableBody');
        if (!tbody) return;

        tbody.querySelectorAll('tr').forEach((row, newIndex) => {
            row.id = `item-row-${newIndex}`;

            row.querySelectorAll('input, select, textarea').forEach(input => {
                if (input.name) {
                    input.name = input.name.replace(/Items\[\d+\]/, `Items[${newIndex}]`);
                }
            });

            row.querySelectorAll('.field-validation-valid, .field-validation-error').forEach(span => {
                const attr = span.getAttribute('data-valmsg-for');
                if (attr) {
                    span.setAttribute('data-valmsg-for', attr.replace(/Items\[\d+\]/, `Items[${newIndex}]`));
                }
            });
        });

        this.#reinitValidation();
    }

    #addOrIncrementItem(product) {
        const tbody = document.getElementById('itemsTableBody');
        if (!tbody) return;

        const existingRow = Array.from(tbody.querySelectorAll('tr')).find(row => {
            const idInput = row.querySelector('.item-product-id');
            return idInput && idInput.value === product.id;
        });

        if (existingRow) {
            const qtyInput = existingRow.querySelector('.item-qty');
            if (qtyInput) {
                const current = parseFloat(DecimalFields.stripFormatting(qtyInput.value, 2)) || 0;
                const newQty = current + 1;
                qtyInput.value = DecimalFields.formatQuantity ? DecimalFields.formatQuantity(newQty) : newQty;
                qtyInput.dispatchEvent(new Event('change', { bubbles: true }));
            }
            existingRow.classList.add('table-success');
            setTimeout(() => existingRow.classList.remove('table-success'), 700);
            return;
        }

        this.#addItemToTable(product, 1, null, this.#state.warehouse);

        const newRow = tbody.lastElementChild;
        if (newRow) {
            newRow.classList.add('table-success');
            setTimeout(() => newRow.classList.remove('table-success'), 700);
        }
    }

    #addItemToTable(product, quantity, unitCost, warehouseId = '') {
        const tbody = document.getElementById('itemsTableBody');
        if (!tbody) return;

        const existingInputs = tbody.querySelectorAll('input[name^="Items["]');
        let maxIndex = -1;
        existingInputs.forEach(inp => {
            const m = inp.name.match(/Items\[(\d+)\]/);
            if (m) maxIndex = Math.max(maxIndex, parseInt(m[1]));
        });
        const i = maxIndex + 1;

        const qtyFormatted = DecimalFields.formatQuantity ? DecimalFields.formatQuantity(quantity) : quantity;
        const costFormatted = unitCost != null
            ? (DecimalFields.formatCurrency ? DecimalFields.formatCurrency(unitCost) : unitCost)
            : '';

        const pictureHtml = product.picture
            ? `<img src="${product.picture}" class="rounded me-2 product-picture"
                    style="width:36px;height:36px;object-fit:cover;flex-shrink:0;" alt="${escapeHtml(product.name)}" />`
            : `<div class="d-flex align-items-center justify-content-center rounded bg-light me-2"
                    style="width:36px;height:36px;flex-shrink:0;">
                    <i class="bi bi-image text-muted small"></i>
               </div>`;

        const warehouseOptions = this.#warehouseSettings.AllowNonWarehouse
            ? `<option value="" ${warehouseId ? '' : 'selected'}>(Không chọn)</option>`
            : '';
        const warehouseOptionsHtml = warehouseOptions + this.#warehouseOptions
            .map(opt => `<option value="${opt.value}" ${opt.value == warehouseId ? 'selected' : ''}>${opt.label}</option>`)
            .join('');

        const row = document.createElement('tr');
        row.id = `item-row-${i}`;
        row.innerHTML = `
            <td class="ps-3">
                <div class="d-flex align-items-center">
                    ${pictureHtml}
                    <div>
                        <div class="fw-medium item-product-name">${escapeHtml(product.name)}</div>
                    </div>
                </div>
                <input type="hidden" name="Items[${i}].ProductId" value="${escapeHtml(product.id)}" class="item-product-id" />
                <input type="hidden" name="Items[${i}].QuantityDecimalPlaces" value="${product.quantityDecimalPlaces ?? 0}" />
            </td>
            <td class="text-center">
                <input name="Items[${i}].Quantity" value="${qtyFormatted}"
                       class="form-control form-control-sm text-end item-qty no-additional-element"
                       data-decimal="quantity" data-decimals="${product.quantityDecimalPlaces ?? 0}" min="0.001" placeholder="0"
                       data-val="true"
                       data-val-required="Vui lòng nhập số lượng."
                       data-val-range="Số lượng phải lớn hơn 0."
                       data-val-range-min="0.001"
                       data-val-number="Số lượng không đúng." />
                <span class="small text-danger field-validation-valid"
                      data-valmsg-for="Items[${i}].Quantity" data-valmsg-replace="true"></span>
            </td>
            <td>
                <select name="Items[${i}].WarehouseId" class="form-select item-warehouse"
                        ${this.#state.warehouse ? 'disabled' : ''}
                        data-val="${!this.#warehouseSettings.AllowNonWarehouse}"
                        data-required="Vui lòng chọn kho hàng">
                    ${warehouseOptionsHtml}
                </select>
                <span class="small text-danger field-validation-valid"
                      data-valmsg-for="Items[${i}].WarehouseId" data-valmsg-replace="true"></span>
            </td>
            <td class="text-end">
                <input name="Items[${i}].UnitCost" value="${costFormatted}"
                       class="form-control form-control-sm text-end item-unit-cost no-additional-element no-hint"
                       data-decimal="currency" placeholder="(Không rõ)" min="0"
                       data-val-range="Đơn giá không được âm."
                       data-val-range-min="0"
                       data-val-number="Đơn giá không đúng." />
                <span class="small text-danger field-validation-valid"
                      data-valmsg-for="Items[${i}].UnitCost" data-valmsg-replace="true"></span>
            </td>
            <td class="text-end pe-3">
                <button type="button" class="btn-table-action danger border-0 bg-transparent shadow-none btn-remove-item">
                    <i class="bi bi-trash"></i>
                </button>
            </td>`;

        tbody.appendChild(row);
        DecimalFields.autoWrap?.(row);
        this.#updateNoItemsVisibility();
        this.#reinitValidation();
    }

    #reinitValidation() {
        const form = document.getElementById('createGoodsReceiptForm');
        $(form).removeData('validator').removeData('unobtrusiveValidation');
        $.validator.unobtrusive.parse(form);
    }

    #resetModal() {
        document.getElementById('modalProductId').value = '';
        if (this.#warehouseSettings.AllowNonWarehouse) {
            document.getElementById('modalWarehouseId').value = '';
        }
        document.getElementById('modalQty').value = '1';
        const costInput = document.getElementById('modalUnitCost');
        if (costInput) costInput.value = '';
        document.getElementById('productPickerError')?.classList.add('d-none');
        document.getElementById('modalQtyError')?.classList.add('d-none');
        document.getElementById('modalUnitCostError')?.classList.add('d-none');
        document.getElementById('modalWarehouseError')?.classList.add('d-none');
        document.getElementById('modalProductDetails')?.classList.add('d-none');
        document.getElementById('btnAddItemConfirm')?.classList.add('d-none');
        this.#productPicker?.clear();
    }
}

function escapeHtml(str) {
    return String(str ?? '')
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;');
}

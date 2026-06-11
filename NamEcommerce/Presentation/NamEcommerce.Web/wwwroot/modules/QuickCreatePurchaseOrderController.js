import { apiGet, apiPost } from "/modules/ajax-helper.js";
import { toast, confirm } from "/modules/modals.js";
import VendorPicker from "/modules/VendorPicker.js";
import ProductBrowser from "/modules/ProductBrowser.js";
import ItemEditor from "/modules/ItemEditor.js";

function escapeHtml(str) {
    const d = document.createElement('div');
    d.appendChild(document.createTextNode(str ?? ''));
    return d.innerHTML;
}

function getEl(id) { return document.getElementById(id); }

export default class QuickCreatePurchaseOrderController {
    #vendorId = null;
    #items = [];
    #itemEditor;
    #browser;

    constructor() {
        const offcanvasEl = getEl('itemEditOffcanvas');
        const modalEl = getEl('itemEditModal');
        this.#itemEditor = new ItemEditor(offcanvasEl, modalEl, { priceLabel: 'Đơn giá nhập' });

        this.#bindVendorPicker();
        this.#bindBrowser();
        this.#bindToggles();
        this.#bindSubmit();
        this.#renderItems();
        this.#renderSummary();
    }

    #bindVendorPicker() {
        const el = getEl('vendorPicker');
        if (!el) return;

        new VendorPicker(el, {
            onSelect: (vendor) => {
                this.#vendorId = vendor?.id ?? null;
                getEl('VendorId').value = this.#vendorId ?? '';
                if (vendor) this.#refreshPricesForVendor(vendor.id);
            },
            onRemove: () => {
                this.#vendorId = null;
                getEl('VendorId').value = '';
            }
        });
    }

    #bindBrowser() {
        const productBrowserEl = getEl('productBrowser');
        if (productBrowserEl) {
            const browser = new ProductBrowser(
                productBrowserEl,
                (product) => this.#addOrIncrementProduct(product),
                { purchase: true, colClass: productBrowserEl.dataset.colClass ?? 'col-12', initialShow: true }
            );
            browser.init();
        }

        const mobileProductBrowserEl = getEl('productBrowserMobile');
        if (mobileProductBrowserEl) {
            const browser = new ProductBrowser(
                mobileProductBrowserEl,
                (product) => {
                    const offCanvas = getEl('productBrowserOffcanvas');
                    bootstrap.Offcanvas.getOrCreateInstance(offCanvas)?.hide();
                    this.#addOrIncrementProduct(product);
                },
                { purchase: true, colClass: productBrowserEl.dataset.colClass ?? 'col-12', notCollapsed: true }
            );
            browser.init();
        }
    }

    #bindToggles() {
        const receiveToggle = getEl('receiveImmediately');
        const paymentSection = getEl('paymentSection');
        const paidToggle = getEl('isPaid');

        receiveToggle?.addEventListener('change', () => {
            const on = receiveToggle.checked;
            paymentSection?.classList.toggle('d-none', !on);
            if (!on) {
                if (paidToggle) paidToggle.checked = false;
                getEl('paymentFields')?.classList.add('d-none');
            }
            this.#renderSummary();
        });

        paidToggle?.addEventListener('change', () => {
            const show = paidToggle.checked;
            getEl('paymentFields')?.classList.toggle('d-none', !show);
            if (show) this.#syncPaymentAmount();
        });
    }

    #bindSubmit() {
        getEl('btnSubmit')?.addEventListener('click', () => this.#submit());
    }

    async #addOrIncrementProduct(product) {
        const idx = this.#items.findIndex(i => i.productId === product.id);
        if (idx >= 0) {
            this.#items[idx] = { ...this.#items[idx], quantity: this.#items[idx].quantity };
            this.#renderItems();
            this.#renderSummary();
            this.#openEditor(idx);
            return;
        }

        let unitCost = 0;
        try {
            const params = new URLSearchParams({ ProductId: product.id });
            if (this.#vendorId) params.set('VendorId', this.#vendorId);
            const res = await apiGet(`/Product/PurchasePriceReference?${params}`);
            const payload = res.data ?? res;
            unitCost = payload.suggestedCost ?? 0;
        } catch { /* keep 0 */ }

        this.#items.push({
            productId: product.id,
            productName: product.name ?? '',
            productPicture: product.picture ?? '',
            quantity: 1,
            unitCost,
            warehouseId: null,
            quantityDecimalPlaces: product.quantityDecimalPlaces ?? 0,
            manualCostSet: false
        });

        const newIdx = this.#items.length - 1;
        this.#renderItems();
        this.#renderSummary();

        this.#openEditor(newIdx, { canRemove: false });
    }

    async #refreshPricesForVendor(vendorId) {
        for (let i = 0; i < this.#items.length; i++) {
            if (this.#items[i].manualCostSet) continue;
            try {
                const params = new URLSearchParams({ ProductId: this.#items[i].productId, VendorId: vendorId });
                const res = await apiGet(`/Product/PurchasePriceReference?${params}`);
                const payload = res.data ?? res;
                const suggested = payload.suggestedCost;
                if (suggested != null) {
                    this.#items[i] = { ...this.#items[i], unitCost: suggested };
                }
            } catch { /* keep existing */ }
        }
        this.#renderItems();
        this.#renderSummary();
    }

    #openEditor(index, openOptions) {
        const item = this.#items[index];
        this.#itemEditor.open({
            name: item.productName,
            picture: item.productPicture,
            quantity: item.quantity,
            unitPrice: item.unitCost,
            quantityDecimalPlaces: item.quantityDecimalPlaces,
            priceLabel: 'Đơn giá nhập'
        }, {
            onApply: (qty, price) => {
                this.#items[index] = {
                    ...this.#items[index],
                    quantity: qty,
                    unitCost: price,
                    manualCostSet: price !== this.#items[index].unitCost || this.#items[index].manualCostSet
                };
                this.#renderItems();
                this.#renderSummary();
            },
            onDelete: () => {
                this.#items.splice(index, 1);
                this.#renderItems();
                this.#renderSummary();
            }
        }, openOptions);
    }

    #renderItems() {
        const tbody = getEl('itemsBody');
        if (!tbody) return;

        if (this.#items.length === 0) {
            tbody.innerHTML = `<tr id="emptyRow"><td colspan="5" class="text-center text-muted py-3 small">Chưa có hàng hóa nào</td></tr>`;
            return;
        }

        tbody.innerHTML = this.#items.map((item, i) => this.#buildRow(item, i)).join('');

        tbody.querySelectorAll('[data-del-idx]').forEach(btn => {
            btn.addEventListener('click', async () => {
                const ok = await confirm('Xóa sản phẩm này khỏi đơn?');
                if (ok) {
                    this.#items.splice(+btn.dataset.delIdx, 1);
                    this.#renderItems();
                    this.#renderSummary();
                }
            });
        });
        tbody.querySelectorAll('tr').forEach((row, i) => {
            row.style.cursor = 'pointer';
            row.addEventListener('click', (e) => {
                if (e.target.closest('button')) return;
                this.#openEditor(i);
            });
        });
    }

    #buildRow(item, i) {
        return `<tr data-row-idx="${i}">
            <td class="ps-3 py-2">
                <div class="fw-medium">${escapeHtml(item.productName)}</div>
                <div class="text-muted small d-md-none">${DecimalFields.formatQuantity(item.quantity, item.quantityDecimalPlaces)} × ${DecimalFields.formatCurrencyWithSymbol(item.unitCost)}</div>
            </td>
            <td class="py-2 text-center d-none d-md-table-cell">
                <span class="fw-medium">${DecimalFields.formatQuantity(item.quantity, item.quantityDecimalPlaces)}</span>
            </td>
            <td class="py-2 text-end">
                <span class="text-muted">${DecimalFields.formatCurrency(item.unitCost)} đ</span>
            </td>
            <td class="py-2 text-end text-nowrap d-none d-md-table-cell">
                <span class="fw-bold text-primary">${DecimalFields.formatCurrencyWithSymbol(item.quantity * item.unitCost)}</span>
            </td>
            <td class="py-2 text-center">
                <button type="button" class="btn btn-sm btn-link text-danger p-0" data-del-idx="${i}"><i class="bi bi-trash"></i></button>
            </td>
        </tr>`;
    }

    #renderSummary() {
        const total = this.#items.reduce((s, i) => s + i.quantity * i.unitCost, 0);
        const totalEl = getEl('orderTotal');
        if (totalEl) totalEl.textContent = DecimalFields.formatCurrencyWithSymbol(total);

        this.#syncPaymentAmount();
    }

    #syncPaymentAmount() {
        const paidToggle = getEl('isPaid');
        if (!paidToggle?.checked) return;
        const amountInput = getEl('paymentAmount');
        if (!amountInput || document.activeElement === amountInput) return;
        const total = this.#items.reduce((s, i) => s + i.quantity * i.unitCost, 0);
        amountInput.value = DecimalFields.formatCurrency(total);
    }

    async #submit() {
        if (this.#items.length === 0) {
            window.NotificationCenter.warning('Vui lòng thêm ít nhất một sản phẩm');
            return;
        }

        const vendorInput = getEl('VendorId');
        if (!vendorInput?.value) {
            window.NotificationCenter.warning('Vui lòng chọn nhà cung cấp');
            return;
        }

        const warehouseId = getEl('DefaultWarehouseId')?.value;
        if (!warehouseId) {
            window.NotificationCenter.warning('Vui lòng chọn kho nhập hàng');
            return;
        }

        const receiveImmediately = getEl('receiveImmediately')?.checked ?? true;
        const isPaid = receiveImmediately && (getEl('isPaid')?.checked ?? false);

        let payment = null;
        if (isPaid) {
            const amountInput = getEl('paymentAmount');
            const amount = DecimalFields.stripFormatting(amountInput?.value ?? '0');
            if (amount <= 0) {
                window.NotificationCenter.warning('Số tiền thanh toán phải lớn hơn 0');
                return;
            }
            payment = {
                amount,
                paymentMethod: parseInt(getEl('paymentMethod')?.value ?? '0')
            };
        }

        const receivedOnInput = getEl('ReceivedOn');
        const receivedOn = receivedOnInput?.value ? new Date(receivedOnInput.value).toISOString() : new Date().toISOString();

        const command = {
            vendorId: vendorInput.value,
            defaultWarehouseId: warehouseId,
            receivedOn,
            note: getEl('Note')?.value || null,
            receiveImmediately,
            pictureIds: [],
            items: this.#items.map(i => ({
                productId: i.productId,
                quantity: i.quantity,
                unitCost: i.unitCost || null,
                warehouseId: i.warehouseId || null,
                quantityDecimalPlaces: i.quantityDecimalPlaces
            })),
            payment
        };

        const btn = getEl('btnSubmit');
        if (btn) { btn.disabled = true; btn.textContent = 'Đang xử lý...'; }

        try {
            const result = await apiPost('/PurchaseOrder/QuickCreate', command);
            if (result.success) {
                window.location.href = `/PurchaseOrder/Details/${result.data?.purchaseOrderId}`;
            }
        } finally {
            if (btn) { btn.disabled = false; btn.textContent = 'Lưu đơn nhập'; }
        }
    }
}

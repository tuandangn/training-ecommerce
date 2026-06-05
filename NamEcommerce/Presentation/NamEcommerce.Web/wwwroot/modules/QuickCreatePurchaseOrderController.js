import { apiGet, apiPost } from "/modules/ajax-helper.js";
import { toast, confirm } from "/modules/modals.js";
import VendorPicker from "/modules/VendorPicker.js";
import ProductBrowser from "/modules/ProductBrowser.js";
import ItemEditOffcanvas from "/modules/ItemEditOffcanvas.js";
import DecimalFields from "/modules/DecimalFields.js";

function escapeHtml(str) {
    const d = document.createElement('div');
    d.appendChild(document.createTextNode(str ?? ''));
    return d.innerHTML;
}

function getEl(id) { return document.getElementById(id); }

export default class QuickCreatePurchaseOrderController {
    #vendorId = null;
    #items = [];
    #offcanvas;
    #browser;

    constructor() {
        const offcanvasEl = getEl('itemEditOffcanvas');
        if (offcanvasEl) this.#offcanvas = new ItemEditOffcanvas(offcanvasEl);

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
                if (vendor) this.#refreshPricesForVendor(vendor.id);
            },
            onRemove: () => { this.#vendorId = null; }
        });
    }

    #bindBrowser() {
        const bindEl = (elId) => {
            const el = getEl(elId);
            if (!el) return;
            const browser = new ProductBrowser(
                el,
                (product) => this.#addOrIncrementProduct(product),
                { purchase: true, colClass: el.dataset.colClass ?? 'col-12', initialShow: true }
            );
            browser.init();
        };
        bindEl('productBrowser');
        bindEl('productBrowserMobile');
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
            this.#items[idx] = { ...this.#items[idx], quantity: this.#items[idx].quantity + 1 };
            this.#renderItems();
            this.#renderSummary();
            if (this.#offcanvas && window.innerWidth < 768) this.#openOffcanvas(idx);
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

        if (this.#offcanvas && window.innerWidth < 768) this.#openOffcanvas(newIdx);
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

    #openOffcanvas(index) {
        if (!this.#offcanvas) return;
        const item = this.#items[index];
        this.#offcanvas.open({
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
        });
    }

    #renderItems() {
        const tbody = getEl('itemsBody');
        if (!tbody) return;

        if (this.#items.length === 0) {
            tbody.innerHTML = `<tr id="emptyRow"><td colspan="5" class="text-center text-muted py-3 small">Chưa có hàng hóa nào</td></tr>`;
            return;
        }

        tbody.innerHTML = this.#items.map((item, i) => this.#buildRow(item, i)).join('');

        tbody.querySelectorAll('[data-edit-idx]').forEach(btn => {
            btn.addEventListener('click', () => this.#openOffcanvas(+btn.dataset.editIdx));
        });
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
        tbody.querySelectorAll('.item-qty-input').forEach((input, i) => {
            DecimalFields.wrapExistingInput(input);
            input.addEventListener('change', () => {
                this.#items[i] = { ...this.#items[i], quantity: DecimalFields.stripFormatting(input.value) };
                this.#renderSummary();
            });
        });
        tbody.querySelectorAll('.item-cost-input').forEach((input, i) => {
            DecimalFields.wrapExistingInput(input, { isCurrency: true });
            input.addEventListener('change', () => {
                this.#items[i] = { ...this.#items[i], unitCost: DecimalFields.stripFormatting(input.value), manualCostSet: true };
                this.#renderSummary();
            });
        });
    }

    #buildRow(item, i) {
        const isMobile = window.innerWidth < 768;
        if (isMobile) {
            return `<tr data-row-idx="${i}">
                <td class="ps-3 py-2">
                    <div class="fw-medium">${escapeHtml(item.productName)}</div>
                    <div class="text-muted small">${DecimalFields.formatQuantity(item.quantity, item.quantityDecimalPlaces)} x ${DecimalFields.formatCurrency(item.unitCost)}</div>
                </td>
                <td class="text-end pe-2 py-2 text-nowrap align-middle">
                    <span class="fw-bold text-primary">${DecimalFields.formatCurrency(item.quantity * item.unitCost)}</span> đ
                    <button type="button" class="btn btn-link p-0 ms-2" data-edit-idx="${i}"><i class="bi bi-pencil-square text-muted"></i></button>
                </td>
            </tr>`;
        }

        return `<tr data-row-idx="${i}">
            <td class="ps-3 py-2">
                <div class="fw-medium">${escapeHtml(item.productName)}</div>
            </td>
            <td class="py-2">
                <input type="text" class="form-control form-control-sm item-qty-input" value="${DecimalFields.formatQuantity(item.quantity, item.quantityDecimalPlaces)}" style="width:90px">
            </td>
            <td class="py-2">
                <input type="text" class="form-control form-control-sm item-cost-input" value="${DecimalFields.formatCurrency(item.unitCost)}" style="width:120px">
            </td>
            <td class="py-2 text-end text-nowrap">
                ${DecimalFields.formatCurrency(item.quantity * item.unitCost)} đ
            </td>
            <td class="py-2 text-center">
                <button type="button" class="btn btn-sm btn-link text-danger p-0" data-del-idx="${i}"><i class="bi bi-trash"></i></button>
            </td>
        </tr>`;
    }

    #renderSummary() {
        const total = this.#items.reduce((s, i) => s + i.quantity * i.unitCost, 0);
        const totalEl = getEl('orderTotal');
        if (totalEl) totalEl.textContent = DecimalFields.formatCurrency(total) + ' đ';

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
            toast.warning('Vui lòng thêm ít nhất một sản phẩm');
            return;
        }

        const vendorInput = getEl('VendorId');
        if (!vendorInput?.value) {
            toast.warning('Vui lòng chọn nhà cung cấp');
            return;
        }

        const warehouseId = getEl('DefaultWarehouseId')?.value;
        if (!warehouseId) {
            toast.warning('Vui lòng chọn kho nhập hàng');
            return;
        }

        const receiveImmediately = getEl('receiveImmediately')?.checked ?? true;
        const isPaid = receiveImmediately && (getEl('isPaid')?.checked ?? false);

        let payment = null;
        if (isPaid) {
            const amountInput = getEl('paymentAmount');
            const amount = DecimalFields.stripFormatting(amountInput?.value ?? '0');
            if (amount <= 0) {
                toast.warning('Số tiền thanh toán phải lớn hơn 0');
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

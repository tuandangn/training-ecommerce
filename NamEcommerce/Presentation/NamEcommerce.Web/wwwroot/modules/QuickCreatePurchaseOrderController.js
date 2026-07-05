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
    #mobileBrowser;

    #vendorPicker;

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

        this.#vendorPicker = new VendorPicker(el, {
            onSelect: async (vendor) => {
                const valid = vendor == null || await this.#checkCommonVendor(vendor.id);

                if (!valid) return;

                this.#vendorId = vendor?.id ?? null;
                getEl('VendorId').value = this.#vendorId ?? '';
                if (vendor) this.#refreshPricesForVendor(vendor.id);
                this.#browser?.setVendor(vendor?.id ?? null);
                this.#mobileBrowser?.setVendor(vendor?.id ?? null);
            },
            onRemove: () => {
                this.#vendorId = null;
                getEl('VendorId').value = '';
                this.#browser?.setVendor(null);
                this.#mobileBrowser?.setVendor(null);
            }
        });
    }

    #bindBrowser() {
        const productBrowserEl = getEl('productBrowser');
        if (productBrowserEl) {
            const browser = new ProductBrowser(
                productBrowserEl,
                (product) => this.#addOrIncrementProduct(product),
                {
                    purchase: true,
                    colClass: productBrowserEl.dataset.colClass ?? 'col-12',
                    initialShow: true,
                    checkProduct: this.#isValidProduct
                }
            );
            browser.init();
            this.#browser = browser;
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
                {
                    purchase: true,
                    colClass: productBrowserEl.dataset.colClass ?? 'col-12',
                    notCollapsed: true,
                    checkProduct: this.#isValidProduct
                }
            );
            browser.init();
            this.#mobileBrowser = browser;
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

    #isValidProduct(product) {
        return product.vendorCount > 0;
    }

    #getCommonVendorOfItems(items) {
        const vendors = items.flatMap(item => item.appropriateVendors);
        const vendorIds = [...new Set(items.flatMap(item => item.appropriateVendorIds))];
        return vendorIds.filter(id => items.every(item => item.appropriateVendorIds.includes(id)))
            .map(id => vendors.find(v => v.id == id));
    }

    async #checkCommonVendor(vendorId) {
        const commonVendors = this.#getCommonVendorOfItems(this.#items);
        if (vendorId == null || this.#items.length == 0 || (vendorId != null && commonVendors.find(v => v.id === vendorId))) {
            return true;
        }

        const isConfirmed = await confirm('Xác nhận', `Nhà cung cấp này không phù hợp với một số mặt hàng đã chọn. Bạn có muốn chuyển sang nhà cung cấp này và bỏ các mặt hàng không phù hợp không?`, 'warning');
        if (!isConfirmed) {
            this.#vendorPicker.removeVendor();
            return false;
        }

        const items = this.#items.filter(item => item.appropriateVendorIds.includes(vendorId));
        this.#items = items;
        this.#renderItems();
        return true;
    }

    async #addOrIncrementProduct(product) {
        if (!this.#isValidProduct(product)) {
            toast('Hàng hóa không phù hợp', 'Vui lòng chọn hàng hóa khác.', 'warning');
            return;
        }

        const idx = this.#items.findIndex(i => i.productId === product.id);
        if (idx >= 0) {
            this.#items[idx] = { ...this.#items[idx], quantity: this.#items[idx].quantity + 1 };
            this.#renderItems();
            this.#renderSummary();
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

        const newItem = {
            productId: product.id,
            productName: product.name ?? '',
            productPicture: product.picture ?? '',
            quantity: 1,
            unitCost,
            warehouseId: null,
            quantityDecimalPlaces: product.quantityDecimalPlaces ?? 0,
            manualCostSet: false,
            appropriateVendors: product.availableVendors?.map(v => ({ id: v.key, name: v.value })) ?? [],
            appropriateVendorIds: product.availableVendors?.map(v => v.key) ?? [],
        };
        const items = [...this.#items, newItem];
        const commonVendors = this.#getCommonVendorOfItems(items);
        if (commonVendors.length == 0) {
            const isConfirmed = await confirm('Xác nhận', `Không tìm thấy nhà cung cấp phù hợp khi bạn thêm mặt hàng này. Bạn có muốn tiếp tục không?`, 'warning');
            if (!isConfirmed) return;
        }

        this.#openEditor(newItem, { canRemove: false });
    }

    async #refreshPricesForVendor(vendorId) {
        for (let i = 0; i < this.#items.length; i++) {
            if (this.#items[i].manualCostSet) continue;
            try {
                const params = new URLSearchParams({ ProductId: this.#items[i].productId, VendorId: vendorId });
                const res = await apiGet(`/Product/PurchasePriceReference?${params}`);
                const payload = res.data ?? res;
                const suggested = payload.suggestedCost;
                if (suggested != null && suggested > 0) {
                    this.#items[i] = { ...this.#items[i], unitCost: suggested };
                }
            } catch { /* keep existing */ }
        }
        this.#renderItems();
        this.#renderSummary();
    }

    #openEditor(item, openOptions) {
        this.#closeOffcanvas();

        const data = { ...item };

        this.#itemEditor.open({
            name: item.productName,
            picture: item.productPicture,
            quantity: item.quantity,
            unitPrice: item.unitCost,
            quantityDecimalPlaces: item.quantityDecimalPlaces,
            priceLabel: 'Đơn giá nhập'
        }, {
            onApply: (qty, price) => {
                const idx = this.#items.findIndex(i => i.productId === data.productId);
                if (idx == -1) {
                    data.quantity = qty;
                    data.unitCost = price;
                    data.manualCostSet = price != data.unitCost;
                    this.#items.push(data);
                } else {
                    const currentItem = this.#items[idx];
                    this.#items[idx] = {
                        ...this.#items[idx],
                        quantity: qty,
                        unitCost: price,
                        manualCostSet: currentItem.manualCostSet || price != currentItem.unitCost
                    };
                }
                this.#renderItems();
                this.#renderSummary();
            },
            onDelete: () => {
                const idx = this.#items.findIndex(i => i.productId === data.productId);
                if (idx === -1) return;
                this.#items.splice(idx, 1);
                this.#renderItems();
                this.#renderSummary();
            }
        }, openOptions);
    }

    #closeOffcanvas() {
        const offcanvas = document.getElementById('productBrowserOffcanvas');
        bootstrap.Offcanvas.getOrCreateInstance(offcanvas)?.hide();
    }

    #renderItems() {
        const tbody = getEl('itemsBody');
        if (!tbody) return;

        if (this.#items.length === 0) {
            tbody.innerHTML = `<tr id="emptyRow"><td colspan="5" class="text-center text-muted py-3 small">Chưa có hàng hóa</td></tr>`;
            return;
        }

        tbody.innerHTML = this.#items.map((item, i) => this.#buildRow(item, i)).join('');

        tbody.querySelectorAll('[data-del-idx]').forEach(btn => {
            btn.addEventListener('click', async () => {
                const index = Number(btn.getAttribute('data-del-idx'));
                if (Number.isNaN(index) || index < 0) throw new Error('Invalid index');
                const item = this.#items[index];
                const result = await Swal.fire({
                    title: 'Xóa hàng hóa?',
                    text: `"${item.productName}" sẽ bị xóa khỏi đơn.`,
                    icon: 'warning',
                    showCancelButton: true,
                    confirmButtonText: 'Xóa',
                    cancelButtonText: 'Hủy',
                    confirmButtonColor: '#dc3545',
                    reverseButtons: true
                });

                if (result.isConfirmed) {
                    this.#items.splice(index, 1);
                    this.#renderItems();
                    this.#renderSummary();
                }
            });
        });
        tbody.querySelectorAll('tr').forEach((row, i) => {
            row.style.cursor = 'pointer';
            row.addEventListener('click', (e) => {
                if (e.target.closest('button')) return;
                this.#openEditor(this.#items[i]);
            });
        });

        const commonVendors = this.#getCommonVendorOfItems(this.#items);
        if (this.#items.length > 0) {
            this.#vendorPicker.setLimitVendorIds(commonVendors.map(v => v.id));
            document.querySelector('.notHasAppropriatedVendorWarning')?.classList.toggle('d-none', commonVendors.length > 0);
        }
        else {
            this.#vendorPicker.setLimitVendorIds(null);
        }
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

        showPageLoading();

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
                return;
            }
            hidePageLoading();
        }
        catch {
            hidePageLoading();
        }
        finally {
            if (btn) { btn.disabled = false; btn.textContent = 'Lưu đơn nhập'; }
        }
    }
}

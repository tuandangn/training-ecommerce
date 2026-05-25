import { toast, confirm } from "/modules/modals.js";
import { apiGet } from "/modules/ajax-helper.js";
import { ProductPriceController } from "/modules/ProductPricePicker.js";
import VendorPicker from "/modules/VendorPicker.js";
import ProductPicker from "/modules/ProductPicker.js";
import ProductBrowser from "/modules/ProductBrowser.js";

class Vendor {
    constructor({ id, name, phone }) {
        this.id = id;
        this.name = name ?? '';
        this.phone = phone ?? '';
    }
}

class ProductInfo {
    constructor({ id, name, availableQty, picture, unitPrice, vendorCount, firstVendorId, availableVendors }) {
        this.id = id;
        this.name = name ?? '';
        this.availableQty = availableQty ?? 0;
        this.picture = picture ?? '';
        this.unitPrice = unitPrice ?? 0;
        this.vendorCount = vendorCount ?? 0;
        this.firstVendorId = firstVendorId;
        this.availableVendors = availableVendors ?? [];
    }
}

class PurchaseOrderItem {
    constructor(productInfo, quantity, unitCost) {
        this.productInfo = productInfo;
        this.quantity = quantity;
        this.unitCost = unitCost;
    }

    get appropriateVendorIds() {
        return this.appropriateVendors.map(v => v.id);
    }
    get appropriateVendors() {
        return this.productInfo?.availableVendors?.map(v => ({ id: v.key, name: v.value })) ?? [];
    }

    get lineTotal() {
        return this.quantity * this.unitCost;
    }
}

class PurchaseOrderState {
    constructor() {
        this.items = [];
        this.vendor = null;
        this.warehouse = null;
        this.expectedDate = null;
        this.placedDate = null;
    }

    get subTotal() {
        return this.items.reduce((sum, item) => sum + item.lineTotal, 0);
    }

    get total() {
        return this.subTotal;
    }
}

export default class CreatePurchaseOrderController {
    #initialized = false;
    #activeRowIndex = null;

    #state;

    #addItemController;
    #priceController;

    #browser;
    #productPicker;
    #vendorPicker;

    #validator;

    constructor() {
        this.#priceController = new ProductPriceController('/PurchaseOrder/RecentPurchasePrices?productId=');
        this.#addItemController = new AddItemController();

        this.#bindProductPicker();
        this.#bindAddItemForm();

        const initialVendor = this.#bindVendorPicker();
        const initialItems = this.#getItems();
        const initialExpectedDate = this.#bindExpectedDate();
        const initialPlacedDate = this.#bindPlacedDate();
        const initialWarehouse = this.#bindWarehouse();

        const browserEl = document.getElementById('productBrowser');
        if (browserEl) {
            this.#browser = new ProductBrowser(
                browserEl,
                (product) => this.#addOrIncrementItem(product),
                { purchase: true, colClass: browserEl.dataset.colClass, initialShow: true }
            );
            this.#browser.init();
        }

        this.#bindEvents();

        if (initialVendor)
            this.#productPicker.setVendor(initialVendor.id, initialVendor.name);

        this.#state = Object.assign(new PurchaseOrderState());
        this.#setState({
            vendor: initialVendor,
            items: initialItems,
            warehouse: initialWarehouse,
            expectedDate: initialExpectedDate,
            placedDate: initialPlacedDate
        });

        this.#initialized = true;
    }

    #bindEvents() {
        const el = getEl('vendorPicker');
        el.addEventListener('select', (e) => {
            const vendor = e.detail?.vendor ? new Vendor(e.detail.vendor) : null;
            if (this.#browser)
                this.#browser.setVendor(vendor?.id);

            this.#updateUnitCostOfItemsWithVendor(vendor);
        });
        el.addEventListener('remove', () => {
            if (this.#browser) {
                this.#browser.setVendor(null);
            }
        });

        const form = document.getElementById('createPurchaseOrderForm');
        form.addEventListener('submit', e => {
            if (!this.#state.items.length) {
                e.preventDefault();
                toast('Chưa có hàng hóa nào', 'Vui lòng thêm hàng hóa', 'warning');
            }
        });
    }

    async #updateUnitCostOfItemsWithVendor(vendor) {
        if (!vendor) return;
        if (!this.#state.items.length)
            return;
        if (this.#state.items.every(item => item.unitCost))
            return;
        const itemPriceMap = new Map();
        const items = Array.from(this.#state.items);
        for (const item of items) {
            if (item.unitCost) continue;
            if (!item.appropriateVendorIds.includes(vendor.id))
                continue;
            const price = await this.#priceController.getProductCostOfVendor(item.productInfo.id, vendor.id);
            if (price)
                itemPriceMap.set(item, price);
        }
        if (!itemPriceMap.size) return;
        const html = `
                <span class="text-dark">Bạn có muốn lấy giá nhật gần nhất?</span>
                <div class="alert alert-info">
                    Nhà cung cấp <span class="fw-bold">${vendor.name}</span>
                </div>
                <div>
                    <table class="table">
                        <thead>
                            <tr>
                                <th>Sản phẩm</th><th>Giá nhập</th>
                            </tr>
                        </thead>
                        <tbody>
                            ${Array.from(itemPriceMap).map(([item, price]) => `<tr><td>${item.productInfo.name}</td><td>${DecimalFields.formatCurrencyWithSymbol(price)}</td></tr>`).join('')}
                        </tbody>
                    </table>
                </div>
            `;
        const allow = await confirm('Giá nhập gần nhất', html, 'info');
        if (!allow) return;
        for (const [item, price] of itemPriceMap) {
            item.unitCost = price;
        }
        this.#setState({ items });
    }

    #setState(patch) {
        Object.assign(this.#state, patch);
        if (this.#shouldRemoveVendor()) {
            this.#state.vendor = null;
        }
        this.#render();
    }

    #render() {
        this.#renderSummary();
        this.#renderVendor();
        this.#renderItems();
        this.#renderWarnings();
        this.#highlightRow();
        this.#validateForm();
    }

    #renderSummary() {
        getEl('subTotal').textContent = DecimalFields.formatCurrencyWithSymbol(this.#state.subTotal);
        getEl('grandTotal').textContent = DecimalFields.formatCurrencyWithSymbol(this.#state.total);

        const hasItems = this.#state.items.length > 0;
        getEl('noItemsMessage').style.display = hasItems ? 'none' : 'block';
        //getEl('tableFooter').classList.toggle('d-none', !hasItems);
        getEl('grandTotalHint').textContent = this.#state.total > 0 ? SoBangChu.docSoTien(this.#state.total) : '';
    }

    #renderVendor() {
        const { vendor } = this.#state;

        const vendorElement = getEl('VendorId');
        const oldValue = vendorElement.value;

        vendorElement.value = vendor?.id ?? '';
        if (oldValue != vendorElement.value) {
            vendorElement.dispatchEvent(new CustomEvent('change'));
        }

        if (this.#vendorPicker) {
            const isServerLocked = getEl('vendorPicker')?.dataset.locked === 'true';
            this.#vendorPicker.setLocked(isServerLocked);

            const commonVendors = this.#getCommonVendorOfItems(this.#state.items);
            if (this.#state.items.length > 0)
                this.#vendorPicker.setLimitVendorIds(commonVendors.map(v => v.id));
            else
                this.#vendorPicker.setLimitVendorIds(null);

            const currentVendor = this.#vendorPicker.value;
            if (currentVendor?.id != vendor?.id) {
                this.#vendorPicker.updateValue(vendor);
            }
        }
    }

    #renderWarnings() {
        const commonVendors = this.#getCommonVendorOfItems(this.#state.items);
        const warningEl = document.querySelector('.notHasAppropriatedVendorWarning');
        if (warningEl) {
            warningEl.classList.toggle('d-none', this.#state.items.length == 0 || commonVendors.length > 0);
        }
    }

    #renderItems() {
        const container = getEl('itemsTableBody');
        container.innerHTML = '';

        this.#state.items.forEach((item, index) => {
            this.#buildItemRow(container, item, index);
        });
    }

    #getItems() {
        const container = getEl('itemsTableBody');
        const rows = Array.from(container.querySelectorAll('tr'));
        return rows.map(row => {
            let availableVendors = [];
            const availableVendorEl = row.querySelector('.available-vendors');
            if (availableVendorEl) {
                availableVendors = JSON.parse(availableVendorEl.textContent).map(v => ({ key: v.id, value: v.name }));
            }
            const productInfo = {
                id: row.querySelector('.product-id').value,
                name: row.querySelector('.product-name').textContent,
                picture: row.querySelector('.product-picture')?.src,
                availableVendors
            };
            const quantity = parseNumber(DecimalFields.stripFormatting(row.querySelector('.row-qty').value, 2), 0);
            const unitCost = parseNumber(DecimalFields.stripFormatting(row.querySelector('.row-price').value, 0), 0);
            const purchaseOrderItem = new PurchaseOrderItem(new ProductInfo(productInfo), quantity, unitCost);

            return purchaseOrderItem;
        });
    }

    #buildItemRow(container, item, index) {
        const { productInfo: p, quantity, unitCost } = item;
        const row = document.createElement('tr');
        row.id = `row-${index}`;
        row.className = 'align-middle';
        row.innerHTML = `
            <td class="ps-3">
                <div class="d-flex align-items-center gap-2">
                    ${p.picture
                ? `<img src="${p.picture}" class="rounded product-picture" style="width:40px; height:40px; object-fit:cover;" alt="${p.name}" />`
                : '<div class="d-flex align-items-center justify-content-center rounded bg-light" style="width: 40px; height: 40px;"><i class="bi bi-image text-muted"></i></div>'
            }
                    <div class="fw-medium product-name">${p.name}</div>
                </div>
                <input type="text" class="visually-hidden product-id" name="Items[${index}].ProductId" value="${p.id}"
                    data-val="true" data-val-required="Vui lòng chọn hàng hóa." />
                <span class="small text-danger field-validation-valid" data-valmsg-for="Items[${index}].ProductId" data-valmsg-replace="true"></span>
            </td>
            <td class="text-end">
                <input name="Items[${index}].Quantity" data-decimal="quantity"
                    class="row-qty no-additional-element" value="${quantity}" autocomplete="off"
                    data-val="true" data-val-required="Vui lòng nhập số lượng." 
                    data-val-range="Số lượng phải lớn hơn 0" data-val-range-min="0.0001" 
                    data-val-number="Số lượng phải là số" />
                <span class="small text-danger field-validation-valid"
                    data-valmsg-for="Items[${index}].Quantity"
                    data-valmsg-replace="true"></span>
            </td>
            <td class="text-end">
                <input name="Items[${index}].UnitCost" data-decimal="currency"
                    class="row-price no-additional-element" value="${unitCost}"
                    data-val="true" autocomplete="off"
                    data-val-required="Vui lòng nhập đơn giá"
                    data-val-range="Đơn giá phải lớn hơn 0" data-val-range-min="0.1" 
                    data-val-number="Đơn giá phải là số" />
                <span class="small text-danger field-validation-valid"
                    data-valmsg-for="Items[${index}].UnitCost"
                    data-valmsg-replace="true"></span>
            </td>
            <td class="text-end fw-bold text-primary px-3 row-total text-nowrap d-none d-lg-table-cell">
                ${DecimalFields.formatCurrency(item.lineTotal)}
            </td>
            <td class="text-end pe-3 w-auto">
                <button type="button" class="btn-table-action danger border-0 bg-transparent shadow-none"
                    aria-label="Xóa hàng hóa">
                    <i class="bi bi-trash"></i>
                </button>
            </td>`;

        // Events
        row.querySelector('button').addEventListener('click', () => {
            const items = this.#state.items.filter((_, i) => i !== index);
            this.#setState({ items });
            this.#dispatch('purchaseOrder:itemRemoved');
        });

        container.appendChild(row);
        DecimalFields.autoWrap(row);

        const inputQuantity = row.querySelector('.row-qty');
        const inputUnitCost = row.querySelector('.row-price');

        var inputQtyChangeDebounced = debounce((e) => {
            const newQuantity = parseNumber(DecimalFields.stripFormatting(inputQuantity.value, 2), 0);
            const newUnitCost = parseNumber(DecimalFields.stripFormatting(inputUnitCost.value, 0), 0);
            this.#updateItem(index, { quantity: newQuantity, unitCost: newUnitCost });
        }, 1000, null, () => inputUnitCostChangeDebounced.cancel());
        var inputUnitCostChangeDebounced = debounce((e) => {
            const newQuantity = parseNumber(DecimalFields.stripFormatting(inputQuantity.value, 2), 0);
            const newUnitCost = parseNumber(DecimalFields.stripFormatting(inputUnitCost.value, 0), 0);
            this.#updateItem(index, { quantity: newQuantity, unitCost: newUnitCost });
        }, 1000, null, () => inputQtyChangeDebounced.cancel());

        inputQuantity.addEventListener('input', inputQtyChangeDebounced);
        inputQuantity.addEventListener('changed', inputQtyChangeDebounced);

        inputUnitCost.addEventListener('input', inputUnitCostChangeDebounced);
        inputUnitCost.addEventListener('changed', inputUnitCostChangeDebounced);
    }

    #updateItem(index, patch) {
        const items = this.#state.items.map((item, i) =>
            i === index ? Object.assign(new PurchaseOrderItem(item.productInfo, item.quantity, item.unitCost), patch) : item
        );
        this.#setState({ items });
    }

    async #addOrIncrementItem(product) {
        if (!this.#isValidProduct(product)) {
            toast('Hàng hóa không phù hợp', 'Vui lòng chọn hàng hóa khác.', 'warning');
            return;
        }
        const items = Array.from(this.#state.items);
        const existingIndex = items.findIndex(item => item.productInfo.id === product.id);
        if (existingIndex !== -1) {
            const existingItem = items[existingIndex];
            existingItem.quantity += 1;
            this.#activeRowIndex = existingIndex;
            this.#setState({ items });
        } else {
            let unitCost = 0;
            if (this.#state.vendor)
                unitCost = await this.#priceController.getProductCostOfVendor(product.id, this.#state.vendor.id) ?? 0;
            items.push(new PurchaseOrderItem(new ProductInfo(product), 1, unitCost));
            this.#activeRowIndex = items.length - 1;
            this.#setState({ items });
        }
    }

    #validateForm(triggers) {
        const form = document.getElementById('createPurchaseOrderForm');
        if (!form) return;

        $(form).removeData('validator').removeData('unobtrusiveValidation');
        $.validator.unobtrusive.parse(form);
        this.#validator = $(form).data('validator');

        if (!Array.isArray(triggers)) {
            if (this.#initialized)
                $(form).valid();
            return;
        }
        for (const trigger of triggers) {
            if (typeof trigger != 'string' && !(trigger instanceof HTMLElement))
                continue;
            this.#validator.element(trigger);
        }
    }

    #isValidProduct(product) {
        return product.vendorCount > 0;
    }

    #shouldRemoveVendor() {
        if (this.#state.items.length == 0)
            return false;

        if (!this.#state.vendor)
            return false;

        const commonVendors = this.#getCommonVendorOfItems(this.#state.items);
        if (commonVendors.length == 0)
            return true;
        if (commonVendors.length == 1 && commonVendors[0].id != this.#state.vendor.id)
            return true;
        if (commonVendors.length > 1 && commonVendors.findIndex(v => v.id === this.#state.vendor.id) == -1)
            return true;

        return false;
    }

    #getCommonVendorOfItems(items) {
        const vendors = items.flatMap(item => item.appropriateVendors);
        const vendorIds = [...new Set(items.flatMap(item => item.appropriateVendorIds))];
        return vendorIds.filter(id => items.every(item => item.appropriateVendorIds.includes(id)))
            .map(id => vendors.find(v => v.id == id));
    }

    #bindPlacedDate() {
        const el = getEl('PlacedOn');
        el.addEventListener('change', (e) => {
            this.#setState({
                placedDate: e.target.value ? new Date(e.target.value) : null,
            });
        });

        const initialPlacedDate = el.value ? new Date(el.value) : null;
        return initialPlacedDate;
    }

    #bindExpectedDate() {
        const el = document.getElementById('ExpectedDeliveryDate');
        if (!el) return;
        el.addEventListener('change', (e) => {
            this.#setState({
                expectedDate: e.target.value ? new Date(e.target.value) : null,
            });
        });

        const initialExpectedDate = el.value ? new Date(el.value) : null;
        return initialExpectedDate;
    }

    #bindWarehouse() {
        const el = document.getElementById('WarehouseId');
        if (!el) return;
        el.addEventListener('change', (e) => {
            this.#setState({
                warehouse: e.target.value,
            });
        });

        const initialWarehouse = el.value;
        return initialWarehouse;
    }

    #bindVendorPicker() {
        const el = getEl('vendorPicker');

        this.#vendorPicker = new VendorPicker(el);

        el.addEventListener('select', (e) => {
            const commonVendors = this.#getCommonVendorOfItems(this.#state.items);
            const vendor = e.detail?.vendor ? new Vendor(e.detail.vendor) : null;
            if (vendor == null || this.#state.items.length == 0 || (vendor != null && commonVendors.find(v => v.id === vendor.id))) {
                this.#setState({ vendor });
                if (this.#productPicker) this.#productPicker.setVendor(vendor?.id, vendor?.name);
                this.#addItemController.setVendor(vendor?.id ?? null);
                return;
            }

            confirm('Xác nhận', `Nhà cung cấp này không phù hợp với một số mặt hàng đã chọn. Bạn có muốn chuyển sang nhà cung cấp này và bỏ các mặt hàng không phù hợp không?`, 'warning')
                .then(isConfirmed => {
                    const oldVendor = e.detail?.oldVendor;
                    if (!isConfirmed) {
                        if (oldVendor)
                            this.#vendorPicker.selectVendor(oldVendor);
                        else
                            this.#vendorPicker.removeVendor();
                    } else {
                        const items = this.#state.items.filter(item => item.appropriateVendorIds.includes(vendor.id));
                        this.#setState({ items });
                    }
                });
        });
        el.addEventListener('remove', () => {
            this.#setState({ vendor: null });
            if (this.#productPicker) this.#productPicker.setVendor(null, null);
            this.#addItemController.setVendor(null);
        });

        var vendor = this.#vendorPicker.value;
        return vendor ?? null;
    }

    #bindProductPicker() {
        const el = getEl('productPicker');
        this.#productPicker = new ProductPicker(el, { purchase: true, checkProduct: this.#isValidProduct });

        el.addEventListener('select', (e) => {
            const product = e.detail?.product ? new ProductInfo(e.detail.product) : null;
            this.#addItemController.setProduct(product, this.#state.vendor?.id ?? null);
        });
        el.addEventListener('remove', () => {
            this.#addItemController.setProduct(null, null);
        });
    }

    #bindAddItemForm() {
        const form = document.getElementById('addProductForm');
        if (!form) return;
        form.addEventListener('addItem', e => {
            if (!e.detail?.item || !(e.detail.item instanceof PurchaseOrderItem))
                return;

            const items = [...this.#state.items, e.detail.item];
            this.#setState({ items });

            this.#productPicker.clear();
            this.#dispatch('purchaseOrder:itemAdded');
        });
    }

    #dispatch(name, detail = {}) {
        document.dispatchEvent(new CustomEvent(name, { bubbles: true, detail }));
    }

    #highlightRow() {
        if (this.#activeRowIndex < 0) return;
        const tableBody = getEl('itemsTableBody');
        const row = tableBody.rows[this.#activeRowIndex];
        this.#activeRowIndex = null;
        if (!row) return;
        row.classList.add('table-success');
        setTimeout(() => row.classList.remove('table-success'), 700);
    }

    #checkElement(element) {
        if (!this.#validator)
            throw new Error('validator is not found');
        return this.#validator.check(element);
    }
}

export class AddItemController {
    #form = null;

    state = {
        productInfo: null,
        vendorId: null,
        quantity: 1,
        unitCost: 0,
        recentPrices: [],
    };

    constructor() {
        getEl('itemQuantity').addEventListener('input', (e) => {
            this.state.quantity = parseNumber(DecimalFields.stripFormatting(e.target.value, 2), 0);
        });
        getEl('itemUnitPrice').addEventListener('input', (e) => {
            this.state.unitCost = parseNumber(DecimalFields.stripFormatting(e.target.value, 0), 0);
        });

        this.#form = document.getElementById('addProductForm');
        if (!this.#form)
            return;

        this.#form.addEventListener('submit', e => {
            e.preventDefault();
            if (!$(e.target).valid()) return;

            const { productInfo, quantity, unitCost } = this.state;

            if (!productInfo) return;

            if (quantity <= 0) {
                toast('Lỗi', 'Số lượng không hợp lệ', 'error');
                return;
            }

            this.#dispatchAddItem({ item: new PurchaseOrderItem(productInfo, quantity, unitCost) });
            this.reset();
        });
    }

    #dispatchAddItem(detail) {
        this.#form.dispatchEvent(new CustomEvent('addItem', { bubbles: true, detail }));
    }

    #setState(patch) {
        this.state = Object.assign({}, this.state, patch);
        this.#render();
    }

    setVendor(vendorId) {
        const unitCost = this.#getPriceOfVendor(vendorId, this.state.recentPrices);
        if (unitCost)
            this.#setState({ vendorId, unitCost });
        else
            this.#setState({ vendorId });
    }

    async setProduct(productInfo, currentVendorId) {
        let recentPrices = [];
        let unitCost = 0;
        if (productInfo) {
            try {
                const result = await apiGet(`/PurchaseOrder/RecentPurchasePrices?productId=${productInfo.id}`);
                recentPrices = Array.isArray(result) ? result : (result.data ?? []);
            } catch {
                recentPrices = [];
            }
            unitCost = this.#getPriceOfVendor(currentVendorId, recentPrices) ?? 0;
        }

        this.#setState({
            productInfo,
            recentPrices,
            unitCost
        });
    }

    reset() {
        this.#setState({
            vendorId: null,
            productInfo: null,
            quantity: 1,
            unitCost: 0,
            recentPrices: []
        });
    }

    #getPriceOfVendor(vendorId, recentPrices) {
        if (recentPrices.length === 0)
            return;

        if (vendorId) {
            const match = recentPrices.find(p => p.vendorId === vendorId);
            if (match) {
                return match.unitCost;
            }
            // Nếu NCC chưa có lịch sử → không thay đổi giá (giữ nguyên 0 hoặc giá cũ)
        } else {
            // Không chọn NCC → không tự động điền
        }
    }

    #render() {
        const { productInfo, quantity, unitCost, vendorId } = this.state;

        getEl('itemQuantity').value = DecimalFields.formatQuantity(quantity);
        getEl('itemUnitPrice').value = DecimalFields.formatCurrency(unitCost);

        const currencyHint = getEl('modalProductInfo').querySelector('.currency-hint');
        if (currencyHint) currencyHint.textContent = '';

        const hasProduct = Boolean(productInfo);
        getEl('modalProductInfo').classList.toggle('d-none', !hasProduct);
        getEl('addItemToTable').classList.toggle('d-none', !hasProduct);

        if (hasProduct) {
            this.#renderPriceTable(vendorId);
            this.#renderAutoFillInfo(vendorId);
        } else {
            this.#hidePriceHint();
        }

        DecimalFields.autoWrap(getEl('itemUnitPrice').closest('div') ?? document.body);
    }

    #renderPriceTable(currentVendorId) {
        const hintEl = document.getElementById('recentPricesHint');
        const emptyEl = document.getElementById('recentPricesEmpty');
        const tableWrapper = document.getElementById('recentPricesTableWrapper');
        const tbody = document.getElementById('recentPricesTbody');

        if (!hintEl || !tbody) return;

        const prices = this.state.recentPrices;

        if (prices.length === 0) {
            hintEl.classList.remove('d-none');
            tableWrapper?.classList.add('d-none');
            emptyEl?.classList.remove('d-none');
            return;
        }

        hintEl.classList.remove('d-none');
        tableWrapper?.classList.remove('d-none');
        emptyEl?.classList.add('d-none');

        tbody.innerHTML = '';
        prices.forEach(p => {
            const isCurrentVendor = currentVendorId && p.vendorId === currentVendorId;
            const tr = document.createElement('tr');
            tr.className = isCurrentVendor ? 'table-success' : '';
            tr.style.cursor = 'pointer';
            tr.title = `Đơn: ${p.purchaseOrderCode}`;

            tr.innerHTML = `
                <td class="ps-0 py-1 text-nowrap">
                    ${isCurrentVendor ? '<i class="bi bi-arrow-right-short text-success vendor-selected"></i>' : ''}
                    <span class="fw-medium">${escapeHtml(p.vendorName ?? 'Không rõ')}</span>
                    <div class="text-muted" style="font-size:0.72rem">${escapeHtml(p.purchaseOrderCode)}</div>
                </td>
                <td class="text-end py-1 fw-semibold text-nowrap">
                    ${DecimalFields.formatCurrency(p.unitCost)}
                </td>
                <td class="text-end py-1 text-muted text-nowrap" style="font-size:0.75rem">${escapeHtml(p.purchaseDate)}</td>`;

            // Click vào hàng → điền giá
            tr.addEventListener('click', () => {
                tr.closest('table').querySelectorAll('tbody tr').forEach(row => {
                    row.classList.remove('table-success')
                });
                tr.classList.add('table-success');
                this.state.unitCost = p.unitCost;
                const itemUnitPrice = getEl('itemUnitPrice');
                itemUnitPrice.value = DecimalFields.formatCurrency(p.unitCost);
                itemUnitPrice.dispatchEvent(new Event('blur'));
                this.#renderAutoFillInfo(currentVendorId);
            });

            tbody.appendChild(tr);
        });

        this.#renderAutoFillInfo(currentVendorId);
    }

    #renderAutoFillInfo(currentVendorId) {
        const infoEl = document.getElementById('recentPricesAutoFillInfo');
        const textEl = document.getElementById('recentPricesAutoFillText');
        if (!infoEl || !textEl) return;

        if (!this.state.productInfo || this.state.recentPrices.length === 0) {
            infoEl.classList.add('d-none');
            return;
        }

        if (currentVendorId) {
            const match = this.state.recentPrices.find(p => p.vendorId === currentVendorId);
            if (match) {
                textEl.innerHTML = `Giá nhập gần nhất của nhà cung cấp <span>${match.vendorName}</span> là <span class="fw-bold">${DecimalFields.formatCurrency(String(match.unitCost))}</span>.`;
                infoEl.classList.remove('d-none');
            } else {
                textEl.textContent = 'Nhà cung cấp này chưa có lịch sử nhập hàng cho sản phẩm — vui lòng nhập giá thủ công.';
                infoEl.classList.remove('d-none');
                infoEl.className = infoEl.className.replace('text-success', 'text-warning');
            }
        } else {
            // Chưa chọn NCC → gợi ý chọn hàng để điền giá
            textEl.textContent = 'Chọn nhà cung cấp để tự động điền giá, hoặc nhấn vào hàng trong bảng bên dưới.';
            infoEl.classList.remove('d-none');
            infoEl.className = 'small text-info mb-1';
        }
    }

    #hidePriceHint() {
        document.getElementById('recentPricesHint')?.classList.add('d-none');
        document.getElementById('recentPricesAutoFillInfo')?.classList.add('d-none');
    }
}

function escapeHtml(str) {
    return String(str ?? '')
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;');
}

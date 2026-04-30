import { toast } from "/modules/modals.js";
import { apiGet } from "/modules/ajax-helper.js";
import VendorPicker from "/modules/VendorPicker.js";
import ProductPicker from "/modules/ProductPicker.js";
import ProductBrowser from "/modules/ProductBrowser.js";

// ─── Models ───────────────────────────────────────────────────────────────────

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
    #state;
    #addItemController = new AddItemController();

    #browser = null;
    #productPicker;
    #vendorPicker;

    #activeRowIndex = null;

    constructor() {

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
                (product) => this.#addOrIncrementItem(product)
            );
            this.#browser.init();
        }

        this.#state = Object.assign(new PurchaseOrderState(), {
            vendor: initialVendor,
            items: initialItems,
            warehouse: initialWarehouse,
            expectedDate: initialExpectedDate,
            placedDate: initialPlacedDate
        });
    }

    // ─── State ────────────────────────────────────────────────────────────────
    #setState(patch) {
        Object.assign(this.#state, patch);
        this.#render();
    }

    // ─── Render ───────────────────────────────────────────────────────────────

    #render() {
        this.#renderSummary();
        this.#renderVendor();
        this.#renderItems();
        this.#highlightRow();
        this.#validateForm();
    }

    #renderSummary() {
        getEl('subTotal').textContent = DecimalFields.formatCurrency(this.#state.subTotal);
        getEl('grandTotal').textContent = DecimalFields.formatCurrency(this.#state.total);

        const hasItems = this.#state.items.length > 0;
        getEl('noItemsMessage').style.display = hasItems ? 'none' : 'block';
        getEl('tableFooter').classList.toggle('d-none', !hasItems);
    }

    #renderVendor() {
        const { vendor } = this.#state;
        const vendorId = getEl('VendorId');

        vendorId.value = vendor?.id ?? '';

        const hasItems = this.#state.items.length > 0;
        const isServerLocked = getEl('vendorPicker')?.dataset.locked === 'true';
        if (this.#vendorPicker) {
            this.#vendorPicker.setLocked(hasItems || isServerLocked);
        }
    }

    #renderItems() {
        const container = getEl('itemsTableBody');
        container.innerHTML = '';

        this.#state.items.forEach((item, index) => {
            this.#buildItemRow(container, item, index);
        });
    }

    #highlightRow() {
        if (this.#activeRowIndex < 0) return;
        const tableBody = getEl('itemsTableBody');
        const row = tableBody.rows[this.#activeRowIndex];
        console.log(row);
        this.#activeRowIndex = null;
        row.classList.add('table-success');
        setTimeout(() => row.classList.remove('table-success'), 700);
    }

    #getItems() {
        const container = getEl('itemsTableBody');
        const rows = Array.from(container.querySelectorAll('tr'));
        return rows.map(row => {
            const purchaseOrderItem = new PurchaseOrderItem({}, 0, 0);

            purchaseOrderItem.productInfo.id = row.querySelector('.product-id').value;
            purchaseOrderItem.productInfo.name = row.querySelector('.product-name').textContent;
            purchaseOrderItem.productInfo.picture = row.querySelector('.product-picture')?.src;
            purchaseOrderItem.quantity = parseNumber(DecimalFields.stripFormatting(row.querySelector('.row-qty').value, 2), 0);
            purchaseOrderItem.unitCost = parseNumber(DecimalFields.stripFormatting(row.querySelector('.row-price').value, 0), 0);

            return purchaseOrderItem;
        });
    }

    // ─── Build item row ───────────────────────────────────────────────────────

    #buildItemRow(container, item, index) {
        const { productInfo: p, quantity, unitCost } = item;
        const row = document.createElement('tr');
        row.id = `row-${index}`;
        row.className = 'align-top';
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
                    data-val-number="Đơn giá phải là số" />
                <span class="small text-danger field-validation-valid"
                    data-valmsg-for="Items[${index}].UnitCost"
                    data-valmsg-replace="true"></span>
            </td>
            <td class="text-end fw-bold text-danger px-3 row-total text-nowrap d-none d-lg-table-cell">
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
            this.#setState({
                items: this.#state.items.filter((_, i) => i !== index),
            });
            this.#dispatch('purchaseOrder:itemRemoved');
        });

        container.appendChild(row);
        DecimalFields.autoWrap(row);

        const inputQuantity = row.querySelector('.row-qty');
        const inputQtyChangeDebounced = debounce((e) => {
            const newQuantity = parseNumber(DecimalFields.stripFormatting(inputQuantity.value, 2), 0);
            this.#updateItem(index, { quantity: newQuantity });
        }, 2000, () => {
            if (inputQuantity._decimalFormatting)
                return false;

            var quantityRaw = DecimalFields.stripFormatting(inputQuantity.value, 2)
            return DecimalFields.isValidDecimal(inputQuantity, quantityRaw);
        });
        //inputQuantity.addEventListener('input', inputQtyChangeDebounced);
        inputQuantity.addEventListener('change', inputQtyChangeDebounced.flush);

        const inputUnitCost = row.querySelector('.row-price');
        const inputUnitCostChangeDebounced = debounce((e) => {
            const newUnitCost = parseNumber(DecimalFields.stripFormatting(inputUnitCost.value, 0), 0);
            this.#updateItem(index, { unitCost: newUnitCost });
        }, 3000, () => {
            if (inputUnitCost._decimalFormatting)
                return false;

            var unitCostRaw = DecimalFields.stripFormatting(inputUnitCost.value)
            return DecimalFields.isValidDecimal(inputUnitCost, unitCostRaw);
        });
        //inputUnitCost.addEventListener('input', inputUnitCostChangeDebounced);
        inputUnitCost.addEventListener('change', inputUnitCostChangeDebounced.flush);
    }

    #updateItem(index, patch) {
        const items = this.#state.items.map((item, i) =>
            i === index ? Object.assign(new PurchaseOrderItem(item.productInfo, item.quantity, item.unitCost), patch) : item
        );
        this.#setState({ items });
    }

    #addOrIncrementItem(product) {
        const items = Array.from(this.#state.items);
        const existingIndex = items.findIndex(item => item.productInfo.id === product.id);
        if (existingIndex !== -1) {
            const existingItem = items[existingIndex];
            existingItem.quantity += 1;
            this.#activeRowIndex = existingIndex;
            this.#setState({ items });
        } else {
            items.push(new PurchaseOrderItem(new ProductInfo(product), 1, 0)); //*TODO* recent unit cost of vendor
            this.#activeRowIndex = items.length - 1;
            this.#setState({ items });
        }
    }

    // ─── Validation ───────────────────────────────────────────────────────────

    #validateForm() {
        const form = document.getElementById('createPurchaseOrderForm');
        if (!form) return;

        // Re-parse unobtrusive validation
        $(form).removeData('validator').removeData('unobtrusiveValidation');
        $.validator.unobtrusive.parse(form);

        const vendorValid = this.#validateVendor();
        const warehouseValid = this.#validateWarehouse();
        const placedDateValid = this.#validatePlacedDate();
        const expectedDateValid = this.#validateExpectedDate();
        const canSubmit = Boolean(
            this.#state.items.length > 0
            && vendorValid && warehouseValid
            && expectedDateValid && placedDateValid
        );

        form.querySelector('[type="submit"]').disabled = !canSubmit;
    }

    #validateVendor() {
        const { vendor } = this.#state;

        const vendorValidator = document.querySelector('[data-valmsg-for="VendorId"]');
        if (vendorValidator) {
            vendorValidator.textContent = vendor ? '' : 'Vui lòng chọn nhà cung cấp.';
        }

        return !!vendor;
    }

    #validateWarehouse() {
        return true;
        //    const { warehouse } = this.#state;

        //    const warehouseValidator = document.querySelector('[data-valmsg-for="WarehouseId"]');
        //    if (warehouseValidator) {
        //        warehouseValidator.textContent = warehouse ? '' : 'Vui lòng chọn kho nhập hàng.';
        //    }

        //    return !!warehouse;
    }

    #validatePlacedDate() {
        const msgEl = document.querySelector('[data-valmsg-for="PlacedOn"]');
        if (!msgEl) return true;

        const today = new Date();

        if (this.#state.placedDate > today) {
            msgEl.textContent = 'Ngày đặt hàng không lớn hơn hiện tại.';
            return false;
        }

        msgEl.textContent = '';
        return true;
    }

    #validateExpectedDate() {
        //const msgEl = document.querySelector('[data-valmsg-for="ExpectedDeliveryDate"]');
        //if (!msgEl) return true;

        //if (this.#state.expectedDate) {
        //    const expected = new Date(this.#state.expectedDate);
        //    expected.setHours(23, 59, 59, 999);

        //    if (expected < this.#state.placedDate) {
        //        msgEl.textContent = 'Ngày giao dự kiến phải lớn hơn ngày đặt hàng.';
        //        return false;
        //    }
        //}

        //msgEl.textContent = '';
        return true;
    }

    // ─── Event bindings ───────────────────────────────────────────────────────

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
            const vendor = e.detail?.vendor ? new Vendor(e.detail.vendor) : null;
            this.#setState({ vendor });
            if (this.#productPicker) this.#productPicker.vendorId = vendor?.id ?? null;
            // Khi đổi NCC → tự động điền giá của NCC đó (nếu có dữ liệu)
            this.#addItemController.applyVendorPrice(vendor?.id ?? null);
        });
        el.addEventListener('remove', () => {
            this.#setState({ vendor: null });
            if (this.#productPicker) this.#productPicker.vendorId = null;
            this.#addItemController.applyVendorPrice(null);
        });

        const initialVendor = el.dataset;
        if (initialVendor.id && initialVendor.name) {
            var vendor = new Vendor(initialVendor);
            this.#vendorPicker.displayVendor(vendor);
            if (this.#productPicker) this.#productPicker.vendorId = vendor.id;
            return vendor;
        }
        return null;
    }

    #bindProductPicker() {
        const el = getEl('productPicker');
        this.#productPicker = new ProductPicker(el);

        el.addEventListener('select', (e) => {
            const product = e.detail?.product ? new ProductInfo(e.detail.product) : null;

            if (product && !this.#state.vendor) {
                if (product.vendorCount === 1) {
                    const vendorId = product.firstVendorId;
                    const vendorName = product.availableVendors.find(v => v.key === vendorId)?.value;
                    const vendor = new Vendor({ id: vendorId, name: vendorName });

                    this.#vendorPicker.selectVendor(vendor);
                } else if (product.vendorCount > 1) {
                    toast('Thông báo', 'Sản phẩm có nhiều nhà cung cấp. Vui lòng chọn nhà cung cấp ở mục trên trước!', 'info');
                    this.#productPicker.clear();
                    return;
                } else if (product.vendorCount === 0) {
                    toast('Lỗi', 'Sản phẩm này chưa được liên kết với nhà cung cấp nào. Vui lòng cập nhật sản phẩm.', 'error');
                    this.#productPicker.clear();
                    return;
                }
            }

            this.#addItemController.setProduct(product, this.#state.vendor?.id ?? null);
        });
        el.addEventListener('remove', () => {
            this.#addItemController.setProduct(null, null);
        });
    }

    #bindAddItemForm() {
        document.getElementById('addProductForm')?.addEventListener('submit', (e) => {
            e.preventDefault();
            if (!$(e.target).valid()) return;

            const { productInfo, quantity, unitCost } = this.#addItemController.state;

            if (!productInfo) return;

            if (quantity <= 0) {
                toast('Lỗi', 'Số lượng không hợp lệ', 'error');
                return;
            }

            this.#setState({
                items: [...this.#state.items, new PurchaseOrderItem(productInfo, quantity, unitCost)],
            });

            this.#productPicker.clear();
            this.#addItemController.reset();

            this.#dispatch('purchaseOrder:itemAdded');
        });
    }

    #dispatch(name, detail = {}) {
        document.dispatchEvent(new CustomEvent(name, { bubbles: true, detail }));
    }
}

// ─── AddItemController ────────────────────────────────────────────────────────

export class AddItemController {
    state = {
        productInfo: null,
        quantity: 1,
        unitCost: 0,
        /** @type {Array<{vendorId: string|null, vendorName: string, unitCost: number, purchaseOrderCode: string, purchaseDate: string}>} */
        recentPrices: [],
    };

    constructor() {
        getEl('itemQuantity').addEventListener('input', (e) => {
            this.state.quantity = parseNumber(DecimalFields.stripFormatting(e.target.value, 2), 0);
        });
        getEl('itemUnitPrice').addEventListener('input', (e) => {
            this.state.unitCost = parseNumber(DecimalFields.stripFormatting(e.target.value, 0), 0);
        });
    }

    /**
     * Khi chọn sản phẩm: fetch giá gần nhất rồi render.
     * @param {ProductInfo|null} productInfo
     * @param {string|null} currentVendorId - ID nhà cung cấp đang chọn (có thể null)
     */
    async setProduct(productInfo, currentVendorId) {
        this.state.productInfo = productInfo;
        this.state.recentPrices = [];

        if (productInfo) {
            // Fetch giá gần nhất qua apiGet (auto antiforgery + CSRF + chuẩn hoá lỗi mạng)
            try {
                const result = await apiGet(`/PurchaseOrder/RecentPurchasePrices?productId=${productInfo.id}`);
                // apiGet trả về parsed JSON nếu shape có `success`; ngược lại trả `{ success, data }`.
                // Endpoint này trả mảng thuần → dữ liệu nằm ở `result.data`.
                this.state.recentPrices = Array.isArray(result) ? result : (result.data ?? []);
            } catch {
                this.state.recentPrices = [];
            }

            // Tự động điền giá theo NCC đang chọn
            this.#autoFillPrice(currentVendorId);
        } else {
            this.state.unitCost = 0;
        }

        this.#render();
    }

    /**
     * Khi thay đổi NCC: tự động điền lại giá phù hợp (nếu có dữ liệu).
     * @param {string|null} vendorId
     */
    applyVendorPrice(vendorId) {
        this.#autoFillPrice(vendorId);
        this.#renderPriceTable(vendorId);
        // Cập nhật giá trị input
        getEl('itemUnitPrice').value = DecimalFields.formatCurrency(this.state.unitCost);
        DecimalFields.autoWrap(getEl('itemUnitPrice').closest('div') ?? document.body);
    }

    reset() {
        this.state = { productInfo: null, quantity: 1, unitCost: 0, recentPrices: [] };
        this.#render();
    }

    // ─── Private ──────────────────────────────────────────────────────────────

    /**
     * Tự động điền giá unitCost trong state từ recentPrices theo vendorId.
     * @param {string|null} vendorId
     */
    #autoFillPrice(vendorId) {
        if (this.state.recentPrices.length === 0) return;

        if (vendorId) {
            const match = this.state.recentPrices.find(p => p.vendorId === vendorId);
            if (match) {
                this.state.unitCost = match.unitCost;
            }
            // Nếu NCC chưa có lịch sử → không thay đổi giá (giữ nguyên 0 hoặc giá cũ)
        } else {
            // Không chọn NCC → không tự động điền
        }
    }

    #render() {
        const { productInfo, quantity, unitCost } = this.state;

        getEl('itemQuantity').value = quantity;
        getEl('itemUnitPrice').value = unitCost;

        const currencyHint = getEl('modalProductInfo').querySelector('.currency-hint');
        if (currencyHint) currencyHint.textContent = '';

        const hasProduct = Boolean(productInfo);
        getEl('modalProductInfo').classList.toggle('d-none', !hasProduct);
        getEl('addItemToTable').classList.toggle('d-none', !hasProduct);

        if (hasProduct) {
            // Đọc vendorId hiện tại từ hidden input (nếu có)
            const vendorIdEl = document.getElementById('VendorId');
            const currentVendorId = vendorIdEl?.value || null;
            this.#renderPriceTable(currentVendorId);
            this.#renderAutoFillInfo(currentVendorId);
        } else {
            this.#hidePriceHint();
        }
    }

    /**
     * Render bảng giá nhập gần nhất trong modal.
     * @param {string|null} currentVendorId
     */
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

    /**
     * Hiển thị thông báo đã tự động điền giá / gợi ý.
     * @param {string|null} currentVendorId
     */
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

// ─── Helpers ──────────────────────────────────────────────────────────────────

function escapeHtml(str) {
    return String(str ?? '')
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;');
}

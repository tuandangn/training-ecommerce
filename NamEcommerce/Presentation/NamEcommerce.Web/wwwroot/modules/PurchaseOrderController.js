import { toast } from "/modules/modals.js";
import VendorPicker from "/modules/VendorPicker.js";
import ProductPicker from "/modules/ProductPicker.js";

// ─── Models ───────────────────────────────────────────────────────────────────

class Vendor {
    constructor({ id, name, phone }) {
        this.id = id;
        this.name = name ?? '';
        this.phone = phone ?? '';
    }
}

class ProductInfo {
    constructor({ id, name, availableQty, picture, unitPrice }) {
        this.id = id;
        this.name = name ?? '';
        this.availableQty = availableQty ?? 0;
        this.picture = picture ?? '';
        this.unitPrice = unitPrice ?? 0;
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
    }

    get subTotal() {
        return this.items.reduce((sum, item) => sum + item.lineTotal, 0);
    }

    get total() {
        return this.subTotal;
    }
}


// ─── PurchaseOrderController ──────────────────────────────────────────────────

export default class PurchaseOrderController {
    #state;
    #addItemController = new AddItemController();

    #productPicker;
    #vendorPicker;

    constructor() {

        this.#bindProductPicker();
        this.#bindAddItemForm();

        const initialVendor = this.#bindVendorPicker();
        const initialItems = this.#getItems();
        const initialExpectedDate = this.#bindExpectedDate();
        const initialWarehouse = this.#bindWarehouse();

        this.#state = Object.assign(new PurchaseOrderState(), {
            vendor: initialVendor,
            items: initialItems,
            warehouse: initialWarehouse,
            expectedDate: initialExpectedDate,
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
        console.log(item);
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
                <span class="small text-danger field-validation-valid"
                    data-valmsg-for="Items[${index}].ProductId"
                    data-valmsg-replace="true"></span>
            </td>
            <td class="text-center">
                <input name="Items[${index}].Quantity"
                    class="row-qty" value="${quantity}"
                    data-val="true"
                    data-val-required="Vui lòng nhập số lượng."
                    data-val-range="Số lượng phải lớn hơn 0."
                    data-val-range-min="0.000000000000000001"
                    data-val-number="Số lượng không đúng." />
                <span class="small text-danger field-validation-valid"
                    data-valmsg-for="Items[${index}].Quantity"
                    data-valmsg-replace="true"></span>
            </td>
            <td class="text-end">
                <input name="Items[${index}].UnitCost"
                    class="row-price" value="${unitCost}"
                    data-val="true"
                    data-val-required="Vui lòng nhập đơn giá."
                    data-val-range="Đơn giá phải lớn hơn hoặc bằng 0."
                    data-val-range-min="0"
                    data-val-number="Đơn giá không đúng." />
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

        container.appendChild(row);

        // Events
        row.querySelector('button').addEventListener('click', () => {
            this.#setState({
                items: this.#state.items.filter((_, i) => i !== index),
            });
            this.#dispatch('purchaseOrder:itemRemoved');
        });

        const inputQuantity = row.querySelector('.row-qty');
        DecimalFields.wrapExistingInput(inputQuantity, 'quantity', { noAdditionalElement : true});

        //const inputQuantityAttrs = inputQuantity.attributes;
        //const inputQuantityAttrMap = {};
        //for (let i = 0; i < inputQuantityAttrs.length; i++) {
        //    const attrName = inputQuantityAttrs[i].name;
        //    if (attrName.startsWith('data-val'))
        //        inputQuantityAttrMap[attrName] = inputQuantityAttrs[i].value;
        //}
        //const { wrapper: qtyWrapper, input: qtyInput } = DecimalFields.createQuantityInput({
        //    name: inputQuantity.name,
        //    value: inputQuantity.value,
        //    id: inputQuantity.id,
        //    placeholder: inputQuantity.placeholder,
        //    noSuffix: true
        //});
        //$(inputQuantity).replaceWith(qtyWrapper);
        //$(qtyInput).attr(inputQuantityAttrMap);
        inputQuantity.addEventListener('change', debounce((e) => {
            this.#updateItem(index, { quantity: parseNumber(DecimalFields.stripFormatting(e.target.value, 2), 0) });
            e.target.focus();
        }, 500));

        var inputUnitCost = row.querySelector('.row-price');
        DecimalFields.wrapExistingInput(inputUnitCost, 'currency', { showHint: false, noAdditionalElement: true });
        //const inputUnitCostAttrs = inputUnitCost.attributes;
        //const inputUnitCostAttrMap = {};
        //for (let i = 0; i < inputUnitCostAttrs.length; i++) {
        //    const attrName = inputUnitCostAttrs[i].name;
        //    if (attrName.startsWith('data-val'))
        //        inputUnitCostAttrMap[attrName] = inputUnitCostAttrs[i].value;
        //}
        //const { wrapper: unitCostWrapper, input: unitCostInput } = DecimalFields.createCurrencyInput({
        //    name: inputUnitCost.name,
        //    value: inputUnitCost.value,
        //    id: inputUnitCost.id,
        //    placeholder: inputUnitCost.placeholder,
        //    noPrefix: true
        //});
        //$(unitCostInput).attr(inputUnitCostAttrMap);
        inputUnitCost.addEventListener('change', debounce((e) => {
            this.#updateItem(index, { unitCost: parseNumber(DecimalFields.stripFormatting(e.target.value, 0), 0) });
        }, 700));
        //$(inputUnitCost).replaceWith(unitCostWrapper);
    }

    #updateItem(index, patch) {
        const items = this.#state.items.map((item, i) =>
            i === index ? Object.assign(new PurchaseOrderItem(item.productInfo, item.quantity, item.unitCost), patch) : item
        );
        this.#setState({ items });
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
        const expectedDateValid = this.#validateExpectedDate();
        const canSubmit = Boolean(
            this.#state.items.length > 0 &&
            vendorValid && warehouseValid && expectedDateValid
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
        const { warehouse } = this.#state;

        const warehouseValidator = document.querySelector('[data-valmsg-for="WarehouseId"]');
        if (warehouseValidator) {
            warehouseValidator.textContent = warehouse ? '' : 'Vui lòng chọn kho nhập hàng.';
        }

        return !!warehouse;
    }

    #validateExpectedDate() {
        const msgEl = document.querySelector('[data-valmsg-for="ExpectedDeliveryDate"]');
        if (!msgEl) return true;

        if (this.#state.expectedDate) {
            const today = new Date();
            today.setHours(0, 0, 0, 0);

            if (this.#state.expectedDate < today) {
                msgEl.textContent = 'Ngày giao dự kiến phải lớn hơn hiện tại.';
                return false;
            }
        }

        msgEl.textContent = '';
        return true;
    }

    // ─── Event bindings ───────────────────────────────────────────────────────

    #bindExpectedDate() {
        const el = getEl('ExpectedDeliveryDate');
        el.addEventListener('change', (e) => {
            this.#setState({
                expectedDate: e.target.value ? new Date(e.target.value) : null,
            });
        });

        const initialExpectedDate = el.value ? new Date(el.value) : null;
        return initialExpectedDate;
    }

    #bindWarehouse() {
        const el = getEl('WarehouseId');
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
            this.#setState({ vendor: e.detail?.vendor ? new Vendor(e.detail.vendor) : null });
        });
        el.addEventListener('remove', () => {
            this.#setState({ vendor: null });
        });

        const initialVendor = el.dataset;
        if (initialVendor.id) {
            var vendor = new Vendor(initialVendor);
            this.#vendorPicker.displayVendor(vendor);
            return vendor;
        }
        return null;
    }

    #bindProductPicker() {
        const el = getEl('productPicker');
        this.#productPicker = new ProductPicker(el);

        el.addEventListener('select', (e) => {
            this.#addItemController.setProduct(e.detail?.product ? new ProductInfo(e.detail.product) : null);
        });
        el.addEventListener('remove', () => {
            this.#addItemController.setProduct(null);
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
    };

    constructor() {
        getEl('itemQuantity').addEventListener('input', (e) => {
            this.state.quantity = parseNumber(DecimalFields.stripFormatting(e.target.value, 2), 0);
        });
        getEl('itemUnitPrice').addEventListener('input', (e) => {
            this.state.unitCost = parseNumber(DecimalFields.stripFormatting(e.target.value, 0), 0);
        });
    }

    setProduct(productInfo) {
        this.state.productInfo = productInfo;
        if (productInfo) {
            this.state.unitCost = productInfo.unitPrice;
        } else {
            this.state.unitCost = 0;
        }
        this.#render();
    }

    reset() {
        this.state = { productInfo: null, quantity: 1, unitCost: 0 };
        this.#render();
    }

    #render() {
        const { productInfo, quantity, unitCost } = this.state;

        getEl('itemQuantity').value = quantity;
        getEl('itemUnitPrice').value = unitCost;

        const hasProduct = Boolean(productInfo);
        getEl('modalProductInfo').classList.toggle('d-none', !hasProduct);
        getEl('addItemToTable').classList.toggle('d-none', !hasProduct);
    }
}

import { toast } from "/modules/modals.js";
import CustomerPicker from "/modules/CustomerPicker.js";
import ProductPicker from "/modules/ProductPicker.js";

// ─── Models ───────────────────────────────────────────────────────────────────

class Customer {
    constructor({ id, name, phone, address }) {
        this.id = id;
        this.name = name ?? '';
        this.phone = phone ?? '';
        this.address = address ?? '';
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

class OrderItem {
    constructor(productInfo, quantity, unitPrice) {
        this.productInfo = productInfo;
        this.quantity = quantity;
        this.unitPrice = unitPrice;
    }

    get lineTotal() {
        return this.quantity * this.unitPrice;
    }
}

class OrderState {
    constructor() {
        this.items = [];
        this.discount = 0;
        this.customer = null;
        this.expectedDate = null;
    }

    get subTotal() {
        return this.items.reduce((sum, item) => sum + item.lineTotal, 0);
    }

    get total() {
        return Math.max(0, this.subTotal - this.discount);
    }
}


// ─── OrderController ──────────────────────────────────────────────────────────

export default class OrderController {
    #state;
    #addItemController = new AddItemController();

    #productPicker;
    #customerPicker;

    constructor() {

        this.#bindProductPicker();
        this.#bindAddItemForm();
        this.#bindShippingAddressEdit();

        const initialCustomer = this.#bindCustomerPicker();
        const initialDiscount = this.#bindDiscount();
        const initialExpectedDate = this.#bindExpectedDate();
        const initialItems = this.#getItems();

        this.#state = Object.assign(new OrderState(),{
            customer: initialCustomer,
            discount: initialDiscount,
            expectedDate: initialExpectedDate,
            items: initialItems
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
        this.#renderCustomer();
        this.#renderItems();
        this.#renderDiscount();
        this.#validateForm();
    }

    #renderSummary() {
        getEl('subTotal').textContent = this.#state.subTotal.toLocaleString() + ' đ';
        getEl('discountDisplay').textContent = '- ' + this.#state.discount.toLocaleString() + ' đ';
        getEl('grandTotal').textContent = this.#state.total.toLocaleString() + ' đ';

        const hasItems = this.#state.items.length > 0;
        getEl('noItemsMessage').style.display = hasItems ? 'none' : 'block';
        getEl('tableFooter').classList.toggle('d-none', !hasItems);
    }

    #renderCustomer() {
        const { customer } = this.#state;
        const customerId = getEl('CustomerId');
        const shippingAddr = getEl('ShippingAddress');

        customerId.value = customer?.id ?? '';

        if (shippingAddr.hasAttribute('readonly') || !shippingAddr.value)
            shippingAddr.value = customer?.address ?? '';
    }

    #renderItems() {
        const container = getEl('itemsTableBody');
        container.innerHTML = '';

        this.#state.items.forEach((item, index) => {
            const row = this.#buildItemRow(item, index);
            container.appendChild(row);
        });
    }

    #getItems() {
        const container = getEl('itemsTableBody');
        const rows = Array.from(container.querySelectorAll('tr'));
        return rows.map(row => {
            const orderItem = new OrderItem({}, 0, 0);

            orderItem.productInfo.id = row.querySelector('.product-id').value;
            orderItem.productInfo.name = row.querySelector('.product-name').textContent;
            orderItem.productInfo.picture = row.querySelector('.product-picture')?.src;
            orderItem.quantity = parseNumber(row.querySelector('.row-qty').value);
            orderItem.unitPrice = parseNumber(row.querySelector('.row-price').value);

            return orderItem;
        });
    }

    #renderDiscount() {
        const discountInput = getEl('OrderDiscount');
        const hasItems = this.#state.items.length > 0;

        discountInput.disabled = !hasItems;

        if (hasItems) {
            const max = this.#state.subTotal;
            discountInput.dataset.valRange = `Giảm giá tối đa không quá ${max.toLocaleString()}.`;
            discountInput.dataset.valRangeMax = max;
        } else {
            delete discountInput.dataset.valRange;
            delete discountInput.dataset.valRangeMax;
        }
    }

    // ─── Build item row ───────────────────────────────────────────────────────

    #buildItemRow(item, index) {
        const { productInfo: p, quantity, unitPrice } = item;
        const row = document.createElement('tr');
        row.id = `row-${index}`;
        row.className = 'align-top';
        row.innerHTML = `
            <td class="ps-4">
                <div class="d-flex gap-3">
                    <div class="text-center d-none d-lg-block" style="min-width:45px;">
                        ${p.picture
                ? `<img src="${p.picture}" class="img-fluid img-thumbnail product-picture" style="width:45px;" alt="${p.name}" />`
                : '<i class="bi bi-image fs-4 text-muted"></i>'
            }
                    </div>
                    <div>
                        <div class="fw-bold text-dark text-nowrap product-name">${p.name}</div>
                    </div>
                </div>
                <input type="text" class="visually-hidden product-id" name="Items[${index}].ProductId" value="${p.id}"
                    data-val="true" data-val-required="Vui lòng chọn hàng hóa." />
                <span class="small text-danger field-validation-valid"
                    data-valmsg-for="Items[${index}].ProductId"
                    data-valmsg-replace="true"></span>
            </td>
            <td class="text-center">
                <input type="number" name="Items[${index}].Quantity" value="${quantity}"
                    class="form-control form-control-sm text-center row-qty"
                    step="any" min="0.00000001"
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
                <input type="number" name="Items[${index}].UnitPrice" value="${unitPrice}"
                    class="form-control form-control-sm text-end row-price"
                    min="0" style="min-width:80px;"
                    data-val="true"
                    data-val-required="Vui lòng nhập đơn giá."
                    data-val-range="Đơn giá phải lớn hơn hoặc bằng 0."
                    data-val-range-min="0"
                    data-val-number="Đơn giá không đúng." />
                <span class="small text-danger field-validation-valid"
                    data-valmsg-for="Items[${index}].UnitPrice"
                    data-valmsg-replace="true"></span>
            </td>
            <td class="text-end fw-bold text-primary px-3 row-total text-nowrap d-none d-lg-table-cell">
                ${item.lineTotal.toLocaleString()} đ
            </td>
            <td class="text-end pe-4 w-auto">
                <button type="button" class="btn btn-link link-danger p-0 border-0 orderItemRemove"
                    aria-label="Xóa hàng hóa">
                    <i class="bi bi-trash"></i>
                </button>
            </td>`;

        // Events
        row.querySelector('.orderItemRemove').addEventListener('click', () => {
            this.#setState({
                items: this.#state.items.filter((_, i) => i !== index),
            });
        });

        const onInputQuantityChange = debounce((e) => {
            this.#updateItem(index, { quantity: parseNumber(e.target.value) });
        }, 500);
        row.querySelector('.row-qty').addEventListener('input', onInputQuantityChange);

        const onInputUnitPriceChange = debounce((e) => {
            this.#updateItem(index, { unitPrice: parseNumber(e.target.value) });
        }, 700);
        row.querySelector('.row-price').addEventListener('input', onInputUnitPriceChange);

        return row;
    }

    #updateItem(index, patch) {
        const items = this.#state.items.map((item, i) =>
            i === index ? Object.assign(new OrderItem(item.productInfo, item.quantity, item.unitPrice), patch) : item
        );
        this.#setState({ items });
    }

    // ─── Validation ───────────────────────────────────────────────────────────

    #validateForm() {
        const form = document.getElementById('createOrderForm');
        if (!form) return;

        // Re-parse unobtrusive validation
        $(form).removeData('validator').removeData('unobtrusiveValidation');
        $.validator.unobtrusive.parse(form);

        const customerValid = this.#validateCustomer();
        const dateValid = this.#validateExpectedDate();
        const canSubmit = Boolean(
            this.#state.items.length > 0 &&
            customerValid &&
            dateValid
        );

        form.querySelector('[type="submit"]').disabled = !canSubmit;
    }

    #validateCustomer() {
        const { customer } = this.#state;

        const customerValidator = document.querySelector('[data-valmsg-for="CustomerId"]');
        customerValidator.textContent = customer ? '' : 'Vui lòng chọn khách hàng.';

        return !!customer;
    }

    #validateExpectedDate() {
        const msgEl = document.querySelector('[data-valmsg-for="ExpectedShippingDate"]');
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

    #bindCustomerPicker() {
        const el = getEl('customerPicker');
        this.#customerPicker = new CustomerPicker(el);

        el.addEventListener('select', (e) => {
            this.#setState({ customer: e.detail?.customer ? new Customer(e.detail.customer) : null });
        });
        el.addEventListener('remove', () => {
            this.#setState({ customer: null });
        });

        const initialCustomer = el.dataset;
        if (initialCustomer.id) {
            var customer = new Customer(initialCustomer);
            this.#customerPicker.displayCustomer(customer);
            return customer;
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

    #bindDiscount() {
        const el = getEl('OrderDiscount');

        const onDiscountChange = debounce((e) => {
            const raw = parseNumber(e.target.value);
            const discount = Math.min(this.#state.subTotal, raw);
            this.#setState({ discount });
        }, 300);
        el.addEventListener('input', onDiscountChange);

        return parseNumber(DecimalFields.stripFormatting(el.value), 0);
    }

    #bindExpectedDate() {
        const el = getEl('ExpectedShippingDate');
        el.addEventListener('change', (e) => {
            this.#setState({
                expectedDate: e.target.value ? new Date(e.target.value) : null,
            });
        });

        const initialExpectedDate = el.value ? new Date(el.value) : null;
        return initialExpectedDate;
    }

    #bindAddItemForm() {
        document.getElementById('addProductForm')?.addEventListener('submit', (e) => {
            e.preventDefault();
            if (!$(e.target).valid()) return;

            const { productInfo, quantity, unitPrice } = this.#addItemController.state;

            if (!productInfo) return;

            if (quantity <= 0) {
                toast('Lỗi', 'Số lượng không hợp lệ', 'error');
                return;
            }

            this.#setState({
                items: [...this.#state.items, new OrderItem(productInfo, quantity, unitPrice)],
            });

            this.#productPicker.clear();
            this.#addItemController.reset();

            this.#dispatch('order:itemAdded');
        });
    }

    #bindShippingAddressEdit() {
        document.getElementById('btnEditShippingAddress')?.addEventListener('click', function () {
            const addr = getEl('ShippingAddress');
            addr.removeAttribute('readonly');
            addr.placeholder = 'Nhập địa chỉ giao hàng';
            addr.classList.remove('border-end-0');
            addr.focus();
            this.remove();
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
        unitPrice: 0,
    };

    constructor() {
        getEl('itemQuantity').addEventListener('input', (e) => {
            this.state.quantity = parseNumber(e.target.value, 1);
        });
        getEl('itemUnitPrice').addEventListener('input', (e) => {
            this.state.unitPrice = parseNumber(e.target.value);
        });
    }

    async setProduct(productInfo) {
        this.state.productInfo = productInfo;
        if (productInfo) {
            this.state.unitPrice = productInfo.unitPrice;
            // Luôn ẩn container trước, fetch ngầm rồi mới hiện nút toggle
            this.#hidePriceHistory();
            await this.#fetchPriceHistory(productInfo.id);
        } else {
            this.state.unitPrice = 0;
            this.#hidePriceHistory();
        }
        this.#render();
    }

    async #fetchPriceHistory(productId) {
        const container = getEl('priceHistoryContainer');
        const loading = getEl('priceHistoryLoading');
        const empty = getEl('priceHistoryEmpty');
        const tableBody = getEl('priceHistoryTableBodyContent');
        const toggleBtn = getEl('togglePriceHistoryBtn');
        const closeBtn = getEl('closePriceHistoryBtn');
        const toggleCostBtn = getEl('toggleCostColLabel');

        // Reset nội dung, ẩn container, hiện loading BÊN TRONG (container vẫn ẩn)
        tableBody.innerHTML = '';
        loading.classList.remove('d-none');
        empty.classList.add('d-none');
        toggleBtn?.classList.add('d-none');

        try {
            const res = await fetch(`/Product/PriceHistory?ProductId=${productId}`);
            if (!res.ok) throw new Error('Failed to fetch price history');
            const data = await res.json();

            loading.classList.add('d-none');

            if (!data.items || data.items.length === 0) {
                // Không có lịch sử → không hiện nút toggle
                return;
            }

            // Render các hàng, mỗi hàng có thể click để chọn giá
            data.items.forEach(item => {
                const row = document.createElement('tr');
                row.style.cursor = 'pointer';
                row.title = 'Nhấn để chọn giá bán này';
                const date = new Date(item.createdOnUtc).toLocaleDateString('vi-VN');
                row.innerHTML = `
                    <td class="ps-2 py-2">${date}</td>
                    <td class="text-end fw-bold text-success py-2">${item.newPrice.toLocaleString()}đ</td>
                    <td class="text-end text-muted pe-2 py-2 d-none cost-cell">${item.newCostPrice.toLocaleString()}đ</td>
                `;
                // Click → điền giá bán vào input và đóng bảng
                row.addEventListener('click', () => {
                    const priceInput = getEl('itemUnitPrice');
                    if (priceInput) {
                        priceInput.value = item.newPrice;
                        priceInput.dispatchEvent(new Event('input', { bubbles: true }));
                    }
                    this.#togglePriceHistory(false); // đóng sau khi chọn
                });
                row.addEventListener('mouseenter', () => row.classList.add('table-active'));
                row.addEventListener('mouseleave', () => row.classList.remove('table-active'));
                tableBody.appendChild(row);
            });

            // Hiện nút toggle (chỉ khi có dữ liệu)
            if (toggleBtn) {
                toggleBtn.classList.remove('d-none');
                // Đảm bảo chỉ bind 1 lần
                toggleBtn.onclick = () => this.#togglePriceHistory();
            }
            if (closeBtn) {
                closeBtn.onclick = () => this.#togglePriceHistory(false);
            }
            if (toggleCostBtn)
                toggleCostBtn.onclick = () => this.#toggleCost();
        } catch (error) {
            console.error(error);
            loading.classList.add('d-none');
            empty.innerHTML = '<small class="text-danger">Lỗi tải lịch sử.</small>';
            empty.classList.remove('d-none');
        }
    }

    /** Toggle hoặc ép trạng thái bảng lịch sử giá.
     * @param {boolean|undefined} forceShow - true: luôn hiện, false: luôn ẩn, undefined: toggle
     */
    #togglePriceHistory(forceShow) {
        const container = getEl('priceHistoryContainer');
        const toggleBtn = getEl('togglePriceHistoryBtn');
        const labelEl = getEl('togglePriceHistoryLabel');
        if (!container) return;

        const willShow = forceShow !== undefined ? forceShow : container.classList.contains('d-none');
        container.classList.toggle('d-none', !willShow);

        if (labelEl) {
            labelEl.textContent = willShow ? 'Ẩn lịch sử giá' : 'Xem lịch sử giá';
        }
    }

    #toggleCost(forceClose) {
        const container = getEl('priceHistoryContainer');
        const priceHistoryTable = container.querySelector('table');
        const costColumnHeader = priceHistoryTable.querySelector('.price-history-cost-col');

        const willHide = forceClose !== undefined ? forceClose : !costColumnHeader.classList.contains('d-none');
        costColumnHeader.classList.toggle('d-none', willHide)

        const priceHistoryBody = getEl('priceHistoryTableBodyContent');
        const cells = priceHistoryBody.querySelectorAll('.cost-cell');
        cells.forEach(cell => cell.classList.toggle('d-none', willHide));
    }

    #hidePriceHistory() {
        const container = getEl('priceHistoryContainer');
        const toggleBtn = getEl('togglePriceHistoryBtn');
        const labelEl = getEl('togglePriceHistoryLabel');
        if (container) container.classList.add('d-none');
        if (toggleBtn) toggleBtn.classList.add('d-none');
        if (labelEl) labelEl.textContent = 'Xem lịch sử giá';
        this.#toggleCost(true);
    }

    reset() {
        this.state = { productInfo: null, quantity: 1, unitPrice: 0 };
        this.#hidePriceHistory();
        this.#render();
    }

    #render() {
        const { productInfo, quantity, unitPrice } = this.state;

        getEl('itemQuantity').value = quantity;
        getEl('itemUnitPrice').value = unitPrice;

        const hasProduct = Boolean(productInfo);
        getEl('modalProductInfo').classList.toggle('d-none', !hasProduct);
        getEl('addItemToTable').classList.toggle('d-none', !hasProduct);
    }
}
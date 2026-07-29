import { toast } from "/modules/modals.js";
import { apiGet, apiPost } from "/modules/ajax-helper.js";
import { customerSettings } from "/modules/Settings.js";
import CustomerPicker from "/modules/CustomerPicker.js";
import ProductPicker from "/modules/ProductPicker.js";
import ProductBrowser from "/modules/ProductBrowser.js";
import ItemEditor from "/modules/ItemEditor.js";

class Customer {
    constructor({ id, name, phone, address, kind, isSystem }) {
        this.id = id;
        this.name = name ?? '';
        this.phone = phone ?? '';
        this.address = address ?? '';
        this.kind = Number(kind ?? 0);
        this.isSystem = isSystem === true || isSystem === 'true';
    }
    get isDefaultCustomer() {
        return this.id == customerSettings.defaultCustomerId;
    }
}

class ProductInfo {
    constructor({ id, name, availableQty, picture, unitPrice, quantityDecimalPlaces }) {
        this.id = id;
        this.name = name ?? '';
        this.availableQty = availableQty ?? 0;
        this.picture = picture ?? '';
        this.unitPrice = unitPrice ?? 0;
        this.quantityDecimalPlaces = quantityDecimalPlaces ?? 0;
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

export default class OrderController {
    #initialized = false;
    #activeRowIndex = null;

    #state;

    #addItemController;
    #itemEditor;

    #productBrowser;
    #productPicker;
    #customerPicker;

    #validator;

    constructor() {
        this.#addItemController = new AddItemController(() => getEl('CustomerId')?.value ?? '');

        const offcanvasEl = document.getElementById('itemEditOffcanvas');
        const modalEl = document.getElementById('itemEditModal');
        if (offcanvasEl || modalEl) {
            this.#itemEditor = new ItemEditor(offcanvasEl, modalEl);
        }

        this.#bindProductPicker();
        this.#bindAddItemForm();

        const initialCustomer = this.#bindCustomerPicker();
        const initialDiscount = this.#bindDiscount();
        const initialExpectedDate = this.#bindExpectedDate();
        const initialItems = this.#getItems();

        const browserEl = document.getElementById('productBrowser');
        if (browserEl) {
            this.#productBrowser = new ProductBrowser(
                browserEl,
                (product) => this.#addOrIncrementItem(product),
                {
                    colClass: browserEl.dataset.colClass,
                    initialShow: true,
                    allowCreateNew: true,
                    checkProduct: this.#isValidProduct
                }
            );
            this.#productBrowser.init();
        }

        this.#bindQuickCreateForms();
        this.#bindEvents();

        this.#state = new OrderState();
        this.#setState({
            customer: initialCustomer,
            discount: initialDiscount,
            expectedDate: initialExpectedDate,
            items: initialItems,
            shippingAddress: getEl('ShippingAddress').value,
            shippingPhoneNumber: getEl('ShippingPhoneNumber').value
        });

        this.#initialized = true;
    }

    #isValidProduct(product) {
        return product.availableQty > 0 || product.vendorCount > 0;
    }

    #setState(patch) {
        Object.assign(this.#state, patch);
        this.#render();
    }

    #render() {
        this.#renderSummary();
        this.#renderCustomer();
        this.#renderItems();
        this.#renderDiscount();
        this.#validateForm();
    }

    #bindEvents() {
        const form = document.getElementById('createOrderForm');
        form.addEventListener('submit', e => {
            if (!this.#state.items.length) {
                e.preventDefault();
                toast('Chưa có hàng hóa nào', 'Vui lòng thêm hàng hóa', 'warning');
            }
        });

        getEl('ShippingAddress').addEventListener('change', (e) => this.#setState({ shippingAddress: e.target.value }));
        getEl('ShippingPhoneNumber').addEventListener('change', (e) => this.#setState({ shippingPhoneNumber: e.target.value }));
    }

    #renderSummary() {
        getEl('subTotal').textContent = DecimalFields.formatCurrency(this.#state.subTotal) + ' đ';
        if (this.#state.discount > 0) {
            getEl('discountDisplay').textContent = '- ' + DecimalFields.formatCurrency(this.#state.discount) + ' đ';
        } else {
            getEl('discountDisplay').textContent = '0 đ';
        }
        getEl('grandTotal').textContent = DecimalFields.formatCurrency(this.#state.total) + ' đ';
        getEl('grandTotalHint').textContent = window.SoBangChu.docSoTien(this.#state.total);

        const hasItems = this.#state.items.length > 0;
        getEl('noItemsMessage').classList.toggle('d-none', hasItems);
        //getEl('tableFooter').classList.toggle('d-none', !hasItems);
    }

    #renderCustomer() {
        const { customer, shippingAddress, shippingPhoneNumber } = this.#state;
        const customerId = getEl('CustomerId');
        const shippingAddr = getEl('ShippingAddress');
        const shippingPhone = getEl('ShippingPhoneNumber');

        customerId.value = customer?.id ?? '';

        shippingAddr.value = shippingAddress;
        shippingPhone.value = shippingPhoneNumber;

        if (customer) {
            shippingAddr.removeAttribute('readonly');
            shippingPhone.removeAttribute('readonly');
        } else {
            shippingAddr.setAttribute('readonly', true);
            shippingPhone.setAttribute('readonly', true);
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
            const orderItem = new OrderItem({}, 0, 0);

            orderItem.productInfo.id = row.querySelector('.product-id').value;
            orderItem.productInfo.name = row.querySelector('.product-name').textContent;
            orderItem.productInfo.picture = row.querySelector('.product-picture')?.src;
            orderItem.quantity = parseNumber(DecimalFields.stripFormatting(row.querySelector('.row-qty').value, 2));
            orderItem.unitPrice = parseNumber(DecimalFields.stripFormatting(row.querySelector('.row-price').value));

            return orderItem;
        });
    }

    #renderDiscount() {
        const discountInput = getEl('OrderDiscount');
        const hasItems = this.#state.items.length > 0;

        discountInput.disabled = !hasItems;

        if (hasItems) {
            const max = this.#state.subTotal;
            discountInput.dataset.valRange = `Giảm giá tối đa không quá ${DecimalFields.formatCurrency(max)}.`;
            discountInput.dataset.valRangeMax = max;
        } else {
            delete discountInput.dataset.valRange;
            delete discountInput.dataset.valRangeMax;
        }
    }

    #buildItemRow(container, item, index) {
        const { productInfo: p, quantity, unitPrice } = item;
        const row = document.createElement('tr');
        row.id = `row-${index}`;
        row.classList.add('order-item-row');
        row.innerHTML = `
            <td class="ps-4 align-middle">
                <div class="d-flex align-items-center gap-3">
                    ${p.picture
                ? `<img src="${p.picture}" class="rounded product-picture order-item-thumb d-none d-lg-block" alt="" />`
                : ''
            }
                    <div>
                        <div class="fw-medium product-name">${escapeHtml(p.name)}</div>
                        <div class="text-muted small d-md-none">
                            ${DecimalFields.formatQuantity(quantity, p.quantityDecimalPlaces ?? 0)} × ${DecimalFields.formatCurrencyWithSymbol(unitPrice)}
                        </div>
                    </div>
                </div>
                <input type="text" class="visually-hidden product-id" name="Items[${index}].ProductId" value="${p.id}"
                    data-val="true" data-val-required="Vui lòng chọn hàng hóa." />
                <input type="hidden" name="Items[${index}].QuantityDecimalPlaces" value="${p.quantityDecimalPlaces ?? 0}" />
                <input type="hidden" class="row-qty" name="Items[${index}].Quantity" value="${quantity}"
                    data-val="true" data-val-required="Vui lòng nhập số lượng."
                    data-val-range="Số lượng phải lớn hơn 0."
                    data-val-range-min="${(p.quantityDecimalPlaces ?? 0) > 0 ? '0.001' : '1'}"
                    data-val-number="Số lượng không đúng." />
                <input type="hidden" class="row-price" name="Items[${index}].UnitPrice" value="${unitPrice}"
                    data-val="true" data-val-required="Vui lòng nhập đơn giá"
                    data-val-range="Đơn giá phải lớn hơn 0" data-val-range-min="0.1"
                    data-val-number="Đơn giá phải là số" />
                <span class="small text-danger field-validation-valid"
                    data-valmsg-for="Items[${index}].ProductId"
                    data-valmsg-replace="true"></span>
            </td>
            <td class="text-center d-none d-md-table-cell align-middle">
                <span class="fw-medium">${DecimalFields.formatQuantity(quantity, p.quantityDecimalPlaces ?? 0)}</span>
            </td>
            <td class="text-end align-middle">
                <span class="text-muted">${DecimalFields.formatCurrencyWithSymbol(unitPrice)}</span>
            </td>
            <td class="text-end fw-bold text-primary px-3 row-total text-nowrap d-none d-lg-table-cell align-middle">
                ${DecimalFields.formatCurrencyWithSymbol(item.lineTotal)}
            </td>
            <td class="text-end pe-4 w-auto align-middle">
                <button type="button" class="btn-table-action danger border-0 bg-transparent shadow-none orderItemRemove"
                    aria-label="Xóa hàng hóa">
                    <i class="bi bi-trash"></i>
                </button>
            </td>`;

        container.appendChild(row);

        row.addEventListener('click', (e) => {
            if (e.target.closest('.orderItemRemove')) return;
            this.#openEditorForIndex(index);
        });

        row.querySelector('.orderItemRemove').addEventListener('click', () => {
            this.#setState({
                items: this.#state.items.filter((_, i) => i !== index),
            });
        });

        return row;
    }

    #updateItem(index, patch) {
        const items = this.#state.items.map((item, i) =>
            i === index ? Object.assign(new OrderItem(item.productInfo, item.quantity, item.unitPrice), patch) : item
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
            items[existingIndex].quantity += 1;
            this.#activeRowIndex = existingIndex;
            this.#setState({ items });
        } else {
            const unitPrice = await this.#addItemController.getSuggestedUnitPrice(product.id, product.unitPrice);
            items.push(new OrderItem(new ProductInfo(product), 1, unitPrice));
            this.#activeRowIndex = items.length - 1;
            this.#setState({ items });
            this.#openEditorForIndex(items.length - 1, { canRemove: false });
        }
    }

    #openEditorForIndex(index, openOptions) {
        const item = this.#state.items[index];
        if (!item || !this.#itemEditor) return;
        this.#itemEditor.open(
            {
                name: item.productInfo.name,
                picture: item.productInfo.picture,
                quantity: item.quantity,
                unitPrice: item.unitPrice,
                quantityDecimalPlaces: item.productInfo.quantityDecimalPlaces ?? 0,
                priceLabel: 'Đơn giá',
            },
            {
                onApply: (qty, price) => {
                    this.#updateItem(index, { quantity: qty, unitPrice: price });
                },
                onDelete: () => {
                    this.#setState({
                        items: this.#state.items.filter((_, i) => i !== index),
                    });
                },
            }, openOptions
        );
    }

    #validateForm(triggers) {
        const form = document.getElementById('createOrderForm');
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

    #bindCustomerPicker() {
        const el = getEl('customerPicker');
        this.#customerPicker = new CustomerPicker(el, {
            allowCreateNew: true
        });

        el.addEventListener('select', (e) => {
            const customer = e.detail?.customer ? new Customer(e.detail.customer) : null;
            this.#setState({
                customer,
                shippingAddress: customer?.isDefaultCustomer ? '' : customer?.address,
                shippingPhoneNumber: customer?.isDefaultCustomer ? '' : customer?.phone
            });
            this.#addItemController.refreshPriceReference();
        });
        el.addEventListener('remove', () => {
            this.#setState({ customer: null, shippingAddress: '', shippingPhoneNumber: '' });
            this.#addItemController.refreshPriceReference();
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
        this.#productPicker = new ProductPicker(el, { checkProduct: this.#isValidProduct, allowCreateNew: true });

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
            const raw = parseNumber(DecimalFields.stripFormatting(e.target.value));
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

    #bindQuickCreateForms() {
        const quickCustomerModalEl = document.getElementById('quickCustomerModal');
        const quickProductModalEl = document.getElementById('quickProductModal');
        const quickCustomerModal = quickCustomerModalEl ? bootstrap.Modal.getOrCreateInstance(quickCustomerModalEl) : null;
        const quickProductModal = quickProductModalEl ? bootstrap.Modal.getOrCreateInstance(quickProductModalEl) : null;

        document.querySelectorAll('[data-open-quick-customer]').forEach(button => {
            button.addEventListener('click', () => quickCustomerModal?.show());
        });

        document.querySelectorAll('[data-open-quick-product]').forEach(button => {
            button.addEventListener('click', () => {
                const addProductModalEl = document.getElementById('addProductModal');
                if (addProductModalEl?.classList.contains('show')) {
                    bootstrap.Modal.getOrCreateInstance(addProductModalEl).hide();
                }
                quickProductModal?.show();
            });
        });

        document.getElementById('quickCreateCustomerForm')?.addEventListener('submit', async (e) => {
            e.preventDefault();
            const form = e.currentTarget;

            if (!$(form).valid())
                return;

            const submitButton = form.querySelector('button[type="submit"]');
            submitButton.disabled = true;

            try {
                const result = await apiPost(form.action, new FormData(form));
                if (!result.success) {
                    if (result.message) {
                        toast('Lỗi', result.message || 'Không thể tạo khách hàng.', 'error');
                    }
                    return;
                }

                this.#customerPicker.selectCustomer(new Customer(result.customer));
                form.reset();
                quickCustomerModal?.hide();
                toast('Thành công', result.message || 'Đã tạo khách hàng.', 'success');
            } catch {
                toast('Lỗi', 'Có lỗi xảy ra khi tạo khách hàng.', 'error');
            } finally {
                submitButton.disabled = false;
            }
        });

        document.getElementById('quickCreateProductForm')?.addEventListener('submit', async (e) => {
            e.preventDefault();
            const form = e.currentTarget;

            if (!$(form).valid())
                return;

            const submitButton = form.querySelector('button[type="submit"]');
            submitButton.disabled = true;

            try {
                const result = await apiPost(form.action, new FormData(form));
                if (!result.success) {
                    if (result.message) {
                        toast('Lỗi', result.message || 'Không thể tạo hàng hóa.', 'error');
                    }
                    return;
                }

                this.#productPicker?.clear();
                this.#addItemController.reset();
                form.reset();
                quickProductModal?.hide();
                this.#productBrowser.reload();

                const items = Array.from(this.#state.items);
                items.push(new OrderItem(new ProductInfo(result.product), 1, result.unitPrice || 0));
                this.#activeRowIndex = items.length - 1;
                this.#setState({ items });
                this.#openEditorForIndex(items.length - 1, { canRemove: false });
            } catch {
                toast('Lỗi', 'Có lỗi xảy ra khi tạo hàng hóa.', 'error');
            } finally {
                submitButton.disabled = false;
            }
        });
    }

    #dispatch(name, detail = {}) {
        document.dispatchEvent(new CustomEvent(name, { bubbles: true, detail }));
    }
}

export class AddItemController {
    #getCustomerId;

    state = {
        productInfo: null,
        quantity: 1,
        unitPrice: 0,
    };

    constructor(getCustomerId = () => '') {
        this.#getCustomerId = getCustomerId;

        getEl('itemQuantity').addEventListener('input', (e) => {
            this.state.quantity = parseNumber(DecimalFields.stripInputFormatting(e.target), 1);
        });
        getEl('itemUnitPrice').addEventListener('input', (e) => {
            this.state.unitPrice = parseNumber(DecimalFields.stripFormatting(e.target.value));
        });
    }

    async setProduct(productInfo) {
        this.state.productInfo = productInfo;
        if (productInfo) {
            this.state.unitPrice = productInfo.unitPrice;
            this.#hidePriceHistory();
            const reference = await this.#fetchSalePriceReference(productInfo.id);
            if (reference)
                this.state.unitPrice = reference.suggestedPrice ?? productInfo.unitPrice ?? 0;
        } else {
            this.state.unitPrice = 0;
            this.#hidePriceHistory();
        }
        this.#render();
    }

    async refreshPriceReference() {
        if (!this.state.productInfo)
            return;

        this.state.unitPrice = this.state.productInfo.unitPrice ?? 0;
        this.#hidePriceHistory();
        const reference = await this.#fetchSalePriceReference(this.state.productInfo.id);
        if (reference)
            this.state.unitPrice = reference.suggestedPrice ?? this.state.unitPrice;
        this.#render();
    }

    async getSuggestedUnitPrice(productId, fallbackPrice = 0) {
        const reference = await this.#requestSalePriceReference(productId);
        return reference?.suggestedPrice ?? fallbackPrice ?? 0;
    }

    async #fetchSalePriceReference(productId) {
        const loading = getEl('priceHistoryLoading');
        const empty = getEl('priceHistoryEmpty');
        const tableBody = getEl('priceHistoryTableBodyContent');
        const toggleBtn = getEl('togglePriceHistoryBtn');
        const closeBtn = getEl('closePriceHistoryBtn');

        tableBody.innerHTML = '';
        loading.classList.remove('d-none');
        empty.classList.add('d-none');
        toggleBtn?.classList.add('d-none');

        try {
            const data = await this.#requestSalePriceReference(productId);

            loading.classList.add('d-none');

            if (!data.items || data.items.length === 0) {
                return data;
            }

            data.items.forEach(item => {
                const row = document.createElement('tr');
                row.classList.add('order-price-history-row');
                row.title = 'Nhấn để chọn giá bán này';
                const date = item.orderDateText || new Date(item.orderDate).toLocaleDateString('vi-VN');
                row.innerHTML = `
                    <td class="ps-3 py-2 text-nowrap">${escapeHtml(date)}</td>
                    <td class="py-2">
                        <span class="fw-medium">${escapeHtml(item.orderCode)}</span>
                        <div class="text-muted order-price-history-source">${escapeHtml(item.sourceText)}</div>
                    </td>
                    <td class="text-end fw-bold text-success pe-3 py-2 text-nowrap">${DecimalFields.formatCurrency(item.unitPrice)}</td>
                `;
                row.addEventListener('click', () => {
                    tableBody.querySelectorAll('tr').forEach(r => r.classList.remove('table-info'));
                    row.classList.add('table-info');
                    const priceInput = getEl('itemUnitPrice');
                    if (priceInput) {
                        priceInput.value = DecimalFields.formatCurrency(item.unitPrice);
                        priceInput.dispatchEvent(new Event('input', { bubbles: true }));
                        priceInput.dispatchEvent(new Event('blur', { bubbles: true }));
                    }
                });
                row.addEventListener('mouseenter', () => row.classList.add('table-active'));
                row.addEventListener('mouseleave', () => row.classList.remove('table-active'));
                tableBody.appendChild(row);
            });

            if (toggleBtn) {
                toggleBtn.classList.remove('d-none');
                toggleBtn.onclick = () => this.#togglePriceHistory();
            }
            if (closeBtn) {
                closeBtn.onclick = () => this.#togglePriceHistory(false);
            }

            this.#togglePriceHistory(true);
            return data;
        } catch (error) {
            console.error(error);
            loading.classList.add('d-none');
            empty.innerHTML = '<small class="text-danger">Lỗi tải lịch sử.</small>';
            empty.classList.remove('d-none');
            return null;
        }
    }

    async #requestSalePriceReference(productId) {
        if (!productId)
            return null;

        const params = new URLSearchParams({ ProductId: productId });
        const customerId = this.#getCustomerId?.();
        if (customerId)
            params.set('CustomerId', customerId);

        const result = await apiGet(`/Product/SalePriceReference?${params.toString()}`);
        if (!result.success)
            throw new Error('Failed to fetch sale price reference');

        return result.data ?? result;
    }

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

    #hidePriceHistory() {
        const container = getEl('priceHistoryContainer');
        const toggleBtn = getEl('togglePriceHistoryBtn');
        const labelEl = getEl('togglePriceHistoryLabel');
        if (container) container.classList.add('d-none');
        if (toggleBtn) toggleBtn.classList.add('d-none');
        if (labelEl) labelEl.textContent = 'Xem lịch sử giá';
    }

    reset() {
        this.state = { productInfo: null, quantity: 1, unitPrice: 0 };
        this.#hidePriceHistory();
        this.#render();
    }

    #render() {
        const { productInfo, quantity, unitPrice } = this.state;

        const itemQuantity = getEl('itemQuantity');
        itemQuantity.value = DecimalFields.formatQuantity(quantity, productInfo?.quantityDecimalPlaces);
        var currentDecimals = parseInt(itemQuantity.dataset.decimals ?? '0', 10);
        if (currentDecimals !== productInfo?.quantityDecimalPlaces) {
            itemQuantity.dataset.decimals = productInfo?.quantityDecimalPlaces ?? 0;
            if (productInfo)
                DecimalFields.wrapExistingInput(itemQuantity, 'quantity');
        }

        getEl('itemUnitPrice').value = DecimalFields.formatCurrency(unitPrice);

        const modalProductInfo = getEl('modalProductInfo');
        const currencyHint = modalProductInfo.querySelector('.currency-hint');
        if (currencyHint) currencyHint.textContent = '';

        const hasProduct = Boolean(productInfo);
        modalProductInfo.classList.toggle('d-none', !hasProduct);
        getEl('addItemToTable').classList.toggle('d-none', !hasProduct);
    }
}

function escapeHtml(str) {
    return String(str ?? '')
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;');
}

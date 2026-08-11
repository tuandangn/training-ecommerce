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

        this.#bindElements();
        this.#bindVendorPicker();
        this.#bindBrowser();
        this.#bindEvents();

        const initialItems = this.#getItems();
        this.#items = initialItems;

        this.#render();
    }

    #bindElements() {
        this.inputVendor = document.getElementById('VendorId');

        this.inputPlacedDate = document.getElementById('PlacedOn');
        this.inputReceivedDate = document.getElementById('ReceivedOn');

        this.btnNotDelivered = document.getElementById('quickCreateNotDelivered');
        this.chkboxNotReceived = document.getElementById('IsReceived_notReceived');
        this.btnDelivered = document.getElementById('quickCreateDelivered');
        this.chkboxReceived = document.getElementById('IsReceived_received');

        this.inputReceivedOn = document.getElementById('ReceivedOn');
        this.inputReceivedOn.setAttribute('data-val-required', 'Vui lòng nhập Ngày nhận.')
        this.inputReceivedOn.setAttribute('data-val-range', 'Ngày nhận hàng phải lớn hơn ngày đặt và nhỏ hơn ngày hiện tại')
        this.inputExpectedDeliveryOn = document.getElementById('ExpectedDeliveryDate');

        this.inputWarehouse = document.getElementById('DefaultWarehouseId');
        this.inputWarehouse.setAttribute('data-val-required', 'Vui lòng nhập Kho hàng.')

        this.btnIsPaid = document.getElementById('quickCreatePaid');
        this.chkboxIsPaid = document.getElementById('IsPaid_paid');
        this.btnUnpaid = document.getElementById('quickCreateUnpaid');
        this.chkboxUnpaid = document.getElementById('IsPaid_unpaid');

        this.emptyItems = document.getElementById('emptyItems');
        this.tableBody = document.getElementById('itemsBody');

        this.orderSubTotal = document.getElementById('orderSubTotal');
        this.orderTaxRate = document.getElementById('orderTaxRate');
        this.orderTaxAmount = document.getElementById('orderTaxAmount');
        this.orderTotal = document.getElementById('orderTotal');
        this.orderShippingAmount = document.getElementById('orderShippingAmount');
        this.orderShippingAmountHint = document.getElementById('orderShippingAmountHint');

        this.inputShippingAmount = document.getElementById('ShippingAmount');
        this.inputTaxRate = document.getElementById('TaxRate');
        this.inputPaidAmount = document.getElementById('paymentAmount');
        this.inputPaymentMethod = document.getElementById('paymentMethod');
        this.inputBankAccount = document.getElementById('BankAccountId');

        this.btnSubmit = document.getElementById('btnSubmit');
        this.form = document.getElementById('purchaseOrderQuickCreateForm');
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
                validateElement(this.inputVendor);
            },
            onRemove: () => {
                this.#vendorId = null;
                getEl('VendorId').value = '';
                this.#browser?.setVendor(null);
                this.#mobileBrowser?.setVendor(null);
                validateElement(this.inputVendor);
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

    #bindEvents() {
        this.inputPlacedDate.addEventListener('change', e => {
            this.#render();
            reValidateDates();
        });

        this.form.addEventListener('change', e => {
            if (e.target.name == 'IsReceived') {
                this.#render();
                reValidateDates();
            }
            if (e.target.name == 'IsPaid') {
                this.#render();
            }
        });


        const totalChanged = debounce(() => {
            this.#renderSummary();
        }, 500);
        this.inputShippingAmount.addEventListener('input', totalChanged);
        this.inputShippingAmount.addEventListener('change', totalChanged.flush);
        this.inputTaxRate.addEventListener('change', totalChanged.flush);

        this.inputPaymentMethod.addEventListener('change', () => this.#render());

        this.form.addEventListener('submit', e => {
            e.preventDefault();
            if (!isFormValid(this.form))
                return;
            this.#handleFormSubmit();
        });

        var reValidateDates = () => {
            if (this.#isDelivered()) {
                validateElement(this.inputReceivedDate);
                reparseForm(this.form);
                initFlatPickrDateTime(this.inputReceivedDate);
            } else {
                validateElement(this.inputExpectedDeliveryOn);
                reparseForm(this.form);
                initFlatPickrDateTime(this.inputExpectedDeliveryOn);
            }
        }
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
        this.#render();
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
            this.#render();
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
        this.#render();
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
                this.#render();
            },
            onDelete: () => {
                const idx = this.#items.findIndex(i => i.productId === data.productId);
                if (idx === -1) return;
                this.#items.splice(idx, 1);
                this.#render();
            }
        }, openOptions);
    }

    #closeOffcanvas() {
        const offcanvas = document.getElementById('productBrowserOffcanvas');
        bootstrap.Offcanvas.getOrCreateInstance(offcanvas)?.hide();
    }

    #render() {
        const subTotal = this.#calculateSubTotal();
        const total = this.#calculateTotal();

        this.#renderItems();
        this.#renderSummary();

        const commonVendors = this.#getCommonVendorOfItems(this.#items);
        if (this.#items.length > 0) {
            this.#vendorPicker.setLimitVendorIds(commonVendors.map(v => v.id));
            document.querySelector('.notHasAppropriatedVendorWarning')?.classList.toggle('d-none', commonVendors.length > 0);
        }
        else {
            this.#vendorPicker.setLimitVendorIds(null);
        }

        this.btnNotDelivered.classList.toggle('disabled', this.#items.length === 0);
        this.btnDelivered.classList.toggle('disabled', this.#items.length === 0);
        this.#toggleDeliveryTabs();
        if (this.#isDelivered()) {
            this.inputWarehouse.setAttribute('data-val', 'true');
            this.inputReceivedOn.setAttribute('data-val', 'true');
            const placedDate = new Date(this.inputPlacedDate.value);
            if (placedDate.getTime()) {
                this.inputReceivedDate.setAttribute('data-val-range-min', placedDate.toISOString());
            } else {
                this.inputReceivedDate.setAttribute('data-val-range-min', '');
            }
            this.inputExpectedDeliveryOn.setAttribute('data-val', 'false');
        } else {
            this.inputReceivedOn.setAttribute('data-val', 'false');
            this.inputWarehouse.setAttribute('data-val', 'false');
            this.inputExpectedDeliveryOn.setAttribute('data-val', 'true');
        }

        this.btnIsPaid.classList.toggle('disabled', subTotal == 0 && total == 0);
        this.btnUnpaid.classList.toggle('disabled', subTotal == 0);
        this.#togglePaymentTabs();
        if (this.#isPaid()) {
            this.inputPaidAmount.setAttribute('data-val', 'true');
            this.inputPaidAmount.setAttribute('data-val-range-max', total);
            this.inputPaidAmount.setAttribute('data-val-range', 'Số tiền thanh toán phải lớn hơn 0 và nhỏ hơn hoặc bằng ' + DecimalFields.formatCurrencyWithSymbol(total));

            const paymentSelectedOption = Array.from(this.inputPaymentMethod.options).find(option => option.selected);
            const requiresBankAccount = paymentSelectedOption.dataset.requireBankAccount == 'true';
            this.inputBankAccount.disabled = !requiresBankAccount;
            this.inputBankAccount.closest('div').classList.toggle('d-none', !requiresBankAccount);
            this.inputBankAccount.setAttribute('data-val', requiresBankAccount);
        } else {
            this.inputPaidAmount.setAttribute('data-val', 'false');
            this.inputPaidAmount.setAttribute('data-val-range-max', '');
            this.inputPaidAmount.setAttribute('data-val-range', '');
            this.inputBankAccount.setAttribute('data-val', 'false');
            this.inputBankAccount.disabled = true;
        }
        reparseForm(this.form);
    }
    #getItems() {
        const container = this.tableBody;
        const rows = Array.from(container.querySelectorAll('tr'));
        return rows.map(row => {
            let availableVendors = [];
            const availableVendorEl = row.querySelector('.available-vendors');
            if (availableVendorEl) {
                availableVendors = JSON.parse(availableVendorEl.textContent).map(v => ({ key: v.id, value: v.name }));
            }
            const quantity = parseNumber(DecimalFields.stripFormatting(row.querySelector('.row-qty').value, 2), 0);
            const unitCost = parseNumber(DecimalFields.stripFormatting(row.querySelector('.row-price').value, 0), 0);
            const quantityDecimalPlaces = parseNumber(DecimalFields.stripFormatting(row.querySelector('.quantityDecimalPlaces').value, 0), 0);
            const purchaseOrderItem = {
                productId: row.querySelector('.product-id').value,
                productName: row.querySelector('.product-name').textContent.trim(),
                productPicture: row.querySelector('.product-picture')?.src,
                quantity,
                unitCost,
                warehouseId: null,
                quantityDecimalPlaces,
                manualCostSet: false,
                appropriateVendors: availableVendors.map(v => ({ id: v.key, name: v.value })) ?? [],
                appropriateVendorIds: availableVendors.map(v => v.key) ?? [],
            };

            return purchaseOrderItem;
        });
    }
    #renderItems() {
        this.tableBody.innerHTML = '';
        this.emptyItems.style.display = this.#items.length > 0 ? 'none' : 'block';

        if (this.#items.length === 0)
            return;

        this.tableBody.innerHTML = this.#items.map((item, i) => this.#buildRow(item, i)).join('');

        this.tableBody.querySelectorAll('[data-del-idx]').forEach(btn => {
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
                    this.#render();
                }
            });
        });
        this.tableBody.querySelectorAll('tr').forEach((row, i) => {
            row.style.cursor = 'pointer';
            row.addEventListener('click', (e) => {
                if (e.target.closest('button')) return;
                this.#openEditor(this.#items[i]);
            });
        });
    }
    #buildRow(item, i) {
        return `<tr data-row-idx="${i}">
            <td class="ps-3 align-middle">
                <div class="d-flex align-items-center gap-3">
                    ${item.productPicture ? `<img src="${item.productPicture}" class="rounded product-picture order-item-thumb d-none d-lg-block" alt="" />` : ''}
                    <div>
                        <div class="fw-medium product-name">${escapeHtml(item.productName)}</div>
                        <div class="d-xl-none">
                            <div class="text-muted small d-xl-none">
                                ${DecimalFields.formatQuantity(item.quantity, item.quantityDecimalPlaces ?? 0)} × ${DecimalFields.formatCurrencyWithSymbol(item.unitCost)}
                            </div>
                            <span class="small text-danger field-validation-valid"
                                data-valmsg-for="Items[${i}].Quantity" data-valmsg-replace="true"></span>
                            <span class="small text-danger field-validation-valid"
                                data-valmsg-for="Items[${i}].UnitCost" data-valmsg-replace="true"></span>
                        </div>
                    </div>
                </div>
                <input type="text" class="visually-hidden product-id" name="Items[${i}].ProductId" value="${item.productId}"
                    data-val="true" data-val-required="Vui lòng chọn hàng hóa." />
                <input type="hidden" name="Items[${i}].QuantityDecimalPlaces" value="${item.quantityDecimalPlaces ?? 0}" />
                <input type="hidden" class="row-qty" name="Items[${i}].Quantity" value="${item.quantity}"
                    data-val="true" data-val-required="Vui lòng nhập số lượng."
                    data-val-range="Số lượng phải lớn hơn 0."
                    data-val-range-min="${(item.quantityDecimalPlaces ?? 0) > 0 ? '0.001' : '1'}"
                    data-val-number="Số lượng không đúng." />
                <input type="hidden" class="row-price" name="Items[${i}].UnitCost" value="${item.unitCost}"
                    data-val="true" data-val-required="Vui lòng nhập đơn giá"
                    data-val-range="Đơn giá phải lớn hơn 0" data-val-range-min="0.1"
                    data-val-number="Đơn giá phải là số" />
                <span class="small text-danger field-validation-valid"
                    data-valmsg-for="Items[${i}].ProductId"
                    data-valmsg-replace="true"></span>
            </td>
            <td class="text-center align-middle d-none d-lg-table-cell">
                <span class="fw-medium">${DecimalFields.formatQuantity(item.quantity, item.quantityDecimalPlaces)}</span>
                <span class="small text-danger field-validation-valid"
                    data-valmsg-for="Items[${i}].Quantity" data-valmsg-replace="true"></span>
            </td>
            <td class="text-end align-middle d-none d-lg-table-cell">
                <span class="text-muted">${DecimalFields.formatCurrencyWithSymbol(item.unitCost)}</span>
                <span class="small text-danger field-validation-valid"
                    data-valmsg-for="Items[${i}].UnitCost" data-valmsg-replace="true"></span>
            </td>
            <td class="text-end align-middle d-table-cell d-lg-none d-xl-table-cell">
                <span class="fw-bold text-primary text-nowrap">${DecimalFields.formatCurrencyWithSymbol(item.quantity * item.unitCost)}</span>
            </td>
            <td class="text-center align-middle">
                <button type="button" class="btn btn-sm btn-link text-danger p-0" data-del-idx="${i}"><i class="bi bi-trash"></i></button>
            </td>
        </tr>`;
    }
    #renderSummary() {
        const subTotal = this.#calculateSubTotal();
        const [taxAmount, taxRate] = this.#calculateTaxAmount();
        const total = this.#calculateTotal();
        const shippingAmount = this.#isDelivered() ? DecimalFields.getValue(this.inputShippingAmount) : 0;

        this.orderSubTotal.textContent = DecimalFields.formatCurrencyWithSymbol(subTotal);
        this.orderTaxAmount.textContent = DecimalFields.formatCurrencyWithSymbol(taxAmount);
        this.orderTaxRate.textContent = taxRate === null ? '' : `${taxRate}%`;
        this.orderTaxRate.classList.toggle('d-none', taxRate === null);
        this.orderTaxAmount.closest('.order-summary-row').classList.toggle('d-none', taxAmount == 0);
        this.orderTotal.textContent = DecimalFields.formatCurrencyWithSymbol(total);
        this.orderShippingAmount.textContent = DecimalFields.formatCurrencyWithSymbol(shippingAmount);
        this.orderShippingAmount.closest('.order-summary-row').classList.toggle('d-none', shippingAmount == 0);
        this.orderShippingAmountHint.classList.toggle('d-none', shippingAmount == 0);
    }
    #calculateSubTotal() {
        return this.#items.reduce((s, i) => s + i.quantity * i.unitCost, 0);
    }
    #calculateTotal() {
        const subTotal = this.#calculateSubTotal();
        const [taxAmount] = this.#calculateTaxAmount();
        return subTotal + taxAmount;
    }
    #calculateTaxAmount() {
        if (!this.#isDelivered())
            return [0, null];
        if (!this.inputTaxRate.value)
            return [0, null];
        const taxRate = Number(this.inputTaxRate.value);
        const subTotal = this.#calculateSubTotal();
        return [Math.round(subTotal * taxRate / 100), taxRate];
    }

    #toggleDeliveryTabs() {
        if (this.#isDelivered()) {
            if (this.btnDelivered.classList.contains('disabled')) {
                this.btnDelivered.classList.remove('active');
                document.querySelector(this.btnDelivered.getAttribute('data-bs-target'))?.classList.remove('active', 'show');
            }
            else {
                bootstrap.Tab.getOrCreateInstance(this.btnDelivered).show();
            }
            return;
        }
        if (this.btnNotDelivered.classList.contains('disabled')) {
            this.btnNotDelivered.classList.remove('active');
            document.querySelector(this.btnNotDelivered.getAttribute('data-bs-target'))?.classList.remove('active', 'show');
        }
        else {
            bootstrap.Tab.getOrCreateInstance(this.btnNotDelivered).show();
        }
    }
    #togglePaymentTabs() {
        if (this.#isPaid()) {
            if (this.btnIsPaid.classList.contains('disabled')) {
                this.btnIsPaid.classList.remove('active');
                document.querySelector(this.btnIsPaid.getAttribute('data-bs-target'))?.classList.remove('active', 'show');
            }
            else {
                bootstrap.Tab.getOrCreateInstance(this.btnIsPaid).show();
            }
            return;
        }
        if (this.btnUnpaid.classList.contains('disabled')) {
            this.btnUnpaid.classList.remove('active');
            document.querySelector(this.btnUnpaid.getAttribute('data-bs-target'))?.classList.remove('active', 'show');
        }
        else {
            bootstrap.Tab.getOrCreateInstance(this.btnUnpaid).show();
        }
    }

    #isDelivered() {
        return this.chkboxReceived?.checked ?? false;
    }
    #isPaid() {
        return this.chkboxIsPaid?.checked ?? false;
    }

    async #handleFormSubmit() {
        if (this.#items.length === 0) {
            window.NotificationCenter.warning('Vui lòng thêm ít nhất một sản phẩm');
            return;
        }

        this.btnSubmit.disabled = true;
        showPageLoading();
        this.form.submit();
    }
}

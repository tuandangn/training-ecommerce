import { apiPost } from "/modules/ajax-helper.js";
import { confirm } from "/modules/modals.js";
import { customerInfo } from "/modules/CustomerInfo.js";
import CustomerPicker from "/modules/CustomerPicker.js";
import ProductBrowser from "/modules/ProductBrowser.js";
import ItemEditor from "/modules/ItemEditor.js";
import DecimalFields from "/modules/DecimalFields.js";

const IntentStatus = {
    Pending: 10,
    Confirmed: 20,
    ManuallyConfirmed: 30,
    Expired: 40,
    Cancelled: 50,
    Consumed: 60
};
const FulfillmentMode = {
    DeliverNow: 10,
    NotDelivered: 20
};
const PaymentTiming = {
    PayNow: 10,
    Unpaid: 20
};
const StatusPollingIntervalMs = 3000;
const EmptyGuid = '00000000-0000-0000-0000-000000000000';

class FastSale {
    constructor(root) {
        this.root = root;
        this.urls = {
            createIntent: root.dataset.createIntentUrl,
            statusIntent: root.dataset.statusUrl,
            confirmIntent: root.dataset.confirmIntentUrl,
            createCashSale: root.dataset.createCashSaleUrl,
            createBankSale: root.dataset.createBankSaleUrl,
            createUnpaidSale: root.dataset.createUnpaidSaleUrl,
            schedule: root.dataset.scheduleUrl
        };
        this.bankTransferEnabled = root.dataset.bankTransferEnabled === 'true';
        this.manualConfirmEnabled = root.dataset.manualConfirmEnabled === 'true';
        this.cart = [];
        this.selectedCustomer = null;
        this.fulfillmentMode = 'notDelivered';
        this.paymentTiming = 'unpaid';
        this.paymentMethod = 'cash';
        this.paymentIntent = null;
        this.paymentIntentConfirmed = false;
        this.statusTimer = null;
        this.saleInputVersion = 0;
        this.pollRequestSeq = 0;
        this.customerPicker = null;
        this.productBrowser = null;
        this.productBrowserMobile = null;
        this.itemEditor = null;

        this.bindElements();

        const offcanvasEl = document.getElementById('itemEditOffcanvas');
        const modalEl = document.getElementById('itemEditModal');
        if (offcanvasEl || modalEl) {
            this.itemEditor = new ItemEditor(offcanvasEl, modalEl);
        }

        this.bindPickers();
        this.bindEvents();
        this.render();
    }

    bindElements() {
        this.alert = document.getElementById('fastSaleAlert');
        this.warehouse = document.getElementById('fastSaleWarehouse');
        this.customerPickerEl = document.getElementById('fastSaleCustomerPicker');
        this.productBrowserEl = document.getElementById('fastSaleProductBrowser');
        this.productBrowserMobileEl = document.getElementById('fastSaleProductBrowserMobile');
        this.cartBody = document.getElementById('fastSaleCartBody');
        this.emptyCart = document.getElementById('fastSaleEmptyCart');
        this.discount = document.getElementById('fastSaleDiscount');
        this.discountDisplay = document.getElementById('fastSaleDiscountDisplay');
        this.note = document.getElementById('fastSaleNote');
        this.subtotal = document.getElementById('fastSaleSubtotal');
        this.total = document.getElementById('fastSaleTotal');
        this.totalHint = document.getElementById('fastSaleTotalHint');
        this.deliverNow = document.getElementById('fastSaleDeliverNow');
        this.notDelivered = document.getElementById('fastSaleNotDelivered');
        this.payNow = document.getElementById('fastSalePayNow');
        this.unpaid = document.getElementById('fastSaleUnpaid');
        this.cashMethod = document.getElementById('fastSaleCashMethod');
        this.bankMethod = document.getElementById('fastSaleBankMethod');
        this.qrPanel = document.getElementById('fastSaleQrPanel');
        this.qrImage = document.getElementById('fastSaleQrImage');
        this.reference = document.getElementById('fastSaleReference');
        this.qrAmount = document.getElementById('fastSaleQrAmount');
        this.qrStatus = document.getElementById('fastSaleQrStatus');
        this.qrExpires = document.getElementById('fastSaleQrExpires');
        this.createQr = document.getElementById('fastSaleCreateQr');
        this.confirmQr = document.getElementById('fastSaleConfirmQr');
        this.complete = document.getElementById('fastSaleComplete');
        this.customer = document.getElementById('CustomerId');
        this.shippingPhoneNumber = document.getElementById('ShippingPhoneNumber');
        this.shippingAddress = document.getElementById('ShippingAddress');
        this.paidAmount = document.createElement('input');
    }

    bindPickers() {
        if (this.customerPickerEl) {
            this.customerPicker = new CustomerPicker(this.customerPickerEl, {
                allowCreateNew: true
            });
            this.customerPickerEl.addEventListener('select', (event) => {
                this.selectedCustomer = event.detail?.customer || null;
                this.applyCustomerShippingDefaults();
                this.resetPaymentIntent();
                this.render();
            });
            this.customerPickerEl.addEventListener('remove', () => {
                this.selectedCustomer = null;
                this.applyCustomerShippingDefaults();
                this.resetPaymentIntent();
                this.render();
            });

            const initialCustomer = this.customerPickerEl.dataset;
            if (initialCustomer.id) {
                this.customerPicker.selectCustomer({
                    id: initialCustomer.id,
                    name: initialCustomer.name,
                    phone: initialCustomer.phone,
                    address: initialCustomer.address,
                    kind: initialCustomer.kind,
                    isSystem: initialCustomer.isSystem
                });
            }
        }

        if (this.productBrowserEl) {
            this.productBrowser = new ProductBrowser(
                this.productBrowserEl,
                (product) => this.addItem(product),
                {
                    colClass: this.productBrowserEl.dataset.colClass,
                    initialShow: true,
                    checkProduct: (product) => this.isProductSelectable(product)
                }
            );
            this.productBrowser.init();
        }

        if (this.productBrowserMobileEl) {
            this.productBrowserMobile = new ProductBrowser(
                this.productBrowserMobileEl,
                (product) => this.addItem(product),
                {
                    colClass: this.productBrowserMobileEl.dataset.colClass,
                    initialShow: true,
                    checkProduct: (product) => this.isProductSelectable(product)
                }
            );
            this.productBrowserMobile.init();
        }

        this.bindQuickCustomerForm();
    }

    applyCustomerShippingDefaults() {
        const isRetailWalkInSystem = this.selectedCustomer?.isSystem && Number(this.selectedCustomer?.kind) === 20;
        if (!this.selectedCustomer || isRetailWalkInSystem) {
            this.shippingAddress.value = '';
            this.shippingPhoneNumber.value = '';
            return;
        }

        this.shippingAddress.value = this.selectedCustomer.address ?? '';
        this.shippingPhoneNumber.value = this.selectedCustomer.phone ?? '';
    }

    bindQuickCustomerForm() {
        const quickCustomerModalEl = document.getElementById('quickCustomerModal');
        const quickCustomerModal = quickCustomerModalEl
            ? bootstrap.Modal.getOrCreateInstance(quickCustomerModalEl)
            : null;

        document.querySelectorAll('[data-open-quick-customer]').forEach(button => {
            button.addEventListener('click', () => quickCustomerModal?.show());
        });

        const form = document.getElementById('quickCreateCustomerForm');
        form?.addEventListener('submit', async (event) => {
            event.preventDefault();

            if (window.$ && typeof $(form).valid === 'function' && !$(form).valid()) {
                return;
            }

            const submitButton = form.querySelector('button[type="submit"]');
            submitButton.disabled = true;

            try {
                const result = await apiPost(form.action, new FormData(form));
                if (!result.success) {
                    this.showAlert('error', result.message || 'Không thể tạo khách hàng.');
                    return;
                }

                this.customerPicker?.selectCustomer(result.customer);
                form.reset();
                quickCustomerModal?.hide();
                this.showAlert('success', result.message || 'Đã tạo khách hàng.');
            } catch {
                this.showAlert('error', 'Có lỗi xảy ra khi tạo khách hàng.');
            } finally {
                submitButton.disabled = false;
            }
        });
    }

    bindEvents() {
        this.warehouse.addEventListener('change', () => {
            this.resetPaymentIntent();
            if (this.fulfillmentMode === 'deliverNow') {
                this.cart.forEach(item => {
                    if (!item.warehouseId) item.warehouseId = this.resolveInitialWarehouseId(item);
                });
            }
            this.render();
        });

        // const totalAmountChanged = debounce(() => {
        //     this.resetPaymentIntent();
        //     this.render();
        // }, 700)
        // this.discount.addEventListener('input', totalAmountChanged);
        // this.paidAmount.addEventListener('input', totalAmountChanged);

        this.deliverNow.addEventListener('click', () => this.setFulfillmentMode('deliverNow'));
        this.notDelivered.addEventListener('click', () => this.setFulfillmentMode('notDelivered'));
        this.payNow.addEventListener('click', () => this.setPaymentTiming('payNow'));
        this.unpaid.addEventListener('click', () => this.setPaymentTiming('unpaid'));
        this.complete.addEventListener('click', () => this.completeSale());

        const needResetPaymentIntent = debounce(() => this.resetPaymentIntent(), 700);
        this.shippingAddress?.addEventListener('input', needResetPaymentIntent);
        this.shippingPhoneNumber?.addEventListener('input', needResetPaymentIntent);
    }

    setFulfillmentMode(mode) {
        this.fulfillmentMode = mode;
        this.cart.forEach(item => {
            item.warehouseId = mode === 'deliverNow' ? (item.warehouseId || this.resolveInitialWarehouseId(item)) : '';
        });
        this.resetPaymentIntent();
        this.productBrowser?.reload();
        this.productBrowserMobile?.reload();
        this.render();
    }

    setPaymentTiming(timing) {
        this.paymentTiming = timing;
        this.resetPaymentIntent();
        this.render();
    }

    setPaymentMethod(method) {
        if (method === 'bank' && !this.bankTransferEnabled) return;
        this.paymentMethod = method;
        this.resetPaymentIntent();
        this.cashMethod.classList.toggle('active', method === 'cash');
        this.bankMethod.classList.toggle('active', method === 'bank');
        this.render();
    }

    resetPaymentIntent() {
        this.saleInputVersion += 1;
        this.pollRequestSeq += 1;
        this.stopIntentPolling();
        this.paymentIntent = null;
        this.paymentIntentConfirmed = false;
    }

    addItem(product) {
        product = this.normalizeProduct(product);
        const warehouseId = this.resolveInitialWarehouseId(product);
        const existing = this.cart.find(item => item.productId === product.id && item.warehouseId === warehouseId);
        let cartItem;
        if (existing) {
            existing.quantity += 1;
        } else {
            cartItem = {
                productId: product.id,
                name: product.name,
                warehouseId,
                unitMeasurement: product.unitMeasurement,
                availableWarehouses: product.availableWarehouses || [],
                quantity: 1,
                quantityAvailable: product.quantityAvailable,
                unitPrice: Number(product.unitPrice || 0),
                quantityDecimalPlaces: Number(product.quantityDecimalPlaces || 0),
                pictureUrl: product.pictureUrl
            };
            this.cart.push(cartItem);
        }

        this.resetPaymentIntent();
        this.render();
        this.#closeOffcanvas();

        if (this.itemEditor && cartItem) {
            this.itemEditor.open({
                name: cartItem.name,
                quantity: cartItem.quantity,
                unitPrice: cartItem.unitPrice,
                quantityDecimalPlaces: cartItem.quantityDecimalPlaces
            }, {
                onApply: (qty, price) => {
                    cartItem.quantity = qty;
                    cartItem.unitPrice = price;
                    this.resetPaymentIntent();
                    this.render();
                },
                onDelete: () => {
                    const i = this.cart.indexOf(cartItem);
                    if (i >= 0) this.cart.splice(i, 1);
                    this.resetPaymentIntent();
                    this.render();
                }
            }, { canRemove: false });
        }
    }

    #closeOffcanvas() {
        const offcanvas = document.getElementById('productBrowserOffcanvas');
        bootstrap.Offcanvas.getOrCreateInstance(offcanvas)?.hide();
    }

    isProductSelectable(product) {
        if (this.fulfillmentMode !== 'deliverNow' || this.deliverNow.disabled) return true;

        return Number(product?.availableQty ?? product?.quantityAvailable ?? 0) > 0;
    }

    normalizeProduct(product) {
        return {
            id: product.id,
            name: product.name,
            unitPrice: Number(product.unitPrice || 0),
            pictureUrl: product.pictureUrl || product.picture || '',
            quantityAvailable: Number(product.quantityAvailable ?? product.availableQty ?? 0),
            quantityDecimalPlaces: Number(product.quantityDecimalPlaces || 0),
            unitMeasurement: product.unitMeasurement || '',
            availableWarehouses: (product.availableWarehouses || []).map(warehouse => ({
                id: warehouse.id || warehouse.key || '',
                name: warehouse.name || warehouse.value || '',
                quantityOnHand: Number(warehouse.quantityOnHand || 0),
                quantityReserved: Number(warehouse.quantityReserved || 0),
                quantityAvailable: Number(warehouse.quantityAvailable || 0)
            })).filter(warehouse => warehouse.id)
        };
    }

    isRetailWalkInCustomer() {
        return this.selectedCustomer != null && customerInfo.isRetailWalkInCustomer(this.selectedCustomer.kind);
    }

    render() {
        const subtotal = this.calculateSubtotal();
        let discount = this.cart.length > 0 ? this.getDiscount() : 0;

        if (discount > subtotal) {
            discount = subtotal;
        }
        this.discount.value = discount;

        const total = Math.max(0, subtotal - discount);

        // if (total == 0 && this.paymentTiming == 'payNow') {
        //     this.setPaymentTiming('unpaid');
        //     return;
        // }

        if (total > 0 && this.isRetailWalkInCustomer() && this.paymentTiming == 'unpaid') {
            this.setPaymentTiming('payNow');
            return;
        }
        if (this.cart.some(item => item.quantity > item.quantityAvailable) && this.fulfillmentMode == 'deliverNow') {
            this.setFulfillmentMode('notDelivered');
            return;
        }

        const isPayNow = this.paymentTiming === 'payNow' && !this.payNow.disabled;
        const usesBankTransfer = isPayNow && this.paymentMethod === 'bank';

        this.payNow.disabled = total == 0 && subtotal == 0;
        this.unpaid.disabled = total == 0 || this.isRetailWalkInCustomer();
        this.paidAmount.disabled = !isPayNow || total == 0;
        this.#togglePaymentTabs();

        this.notDelivered.disabled = this.cart.length === 0;
        this.deliverNow.disabled = this.cart.length === 0 || this.cart.some(item => item.quantity > item.quantityAvailable);
        this.#toggleDeliveryTabs();

        this.renderCart();

        this.subtotal.textContent = this.formatMoneyWithSymbol(subtotal);
        this.total.textContent = this.formatMoneyWithSymbol(total);
        if (isPayNow) {
            const paidAmountValue = this.paidAmount.value ? DecimalFields.getValue(this.paidAmount) : 0;
            if (this.paidAmount.value == '' || paidAmountValue > total)
                this.paidAmount.value = total;
        } else {
            this.paidAmount.value = '';
        }
        this.discountDisplay.textContent = this.formatMoneyWithSymbol(discount);
        this.qrPanel.classList.toggle('d-none', !usesBankTransfer);
        this.createQr.disabled = !usesBankTransfer || total <= 0 || this.cart.length === 0;
        this.confirmQr.disabled = !this.paymentIntent || this.paymentIntentConfirmed || !this.manualConfirmEnabled || !this.isIntentPending(this.paymentIntent);
        this.complete.disabled = this.cart.length === 0 || subtotal <= 0 || (usesBankTransfer && !this.paymentIntentConfirmed);
        this.complete.innerHTML = this.getCompleteButtonHtml();
        if (total > 0)
            this.totalHint.textContent = window.SoBangChu?.docSoTien(total) ?? '';
        else
            this.totalHint.textContent = '';
        this.customer.value = this.selectedCustomer?.id ?? '';
        const showShippingInfo = this.cart.length > 0 && this.fulfillmentMode === 'notDelivered';
        this.shippingAddress.disabled = !showShippingInfo;
        this.shippingPhoneNumber.disabled = !showShippingInfo;
        this.shippingAddress.closest('.ship-info').classList.toggle('d-none', !showShippingInfo);

        this.discount.disabled = !isPayNow || this.cart.length === 0 || subtotal == 0;
        if (isPayNow) {
            if (subtotal > 0) {
                this.discount.setAttribute('data-val-range', `Giảm giá phải nhỏ hơn ${this.formatMoneyWithSymbol(subtotal)}`);
                this.discount.setAttribute('data-val-range-min', 0);
                this.discount.setAttribute('data-val-range-max', subtotal);
            } else {
                this.discount.removeAttribute('data-val-range');
                this.discount.removeAttribute('data-val-range-min');
                this.discount.removeAttribute('data-val-range-max');
            }
            if (total > 0) {
                this.paidAmount.setAttribute('data-val-range', `Số tiền thanh toán phải lớn hơn 0 và nhỏ hơn ${this.formatMoneyWithSymbol(total)}`);
                this.paidAmount.setAttribute('data-val-range-max', total);
                if (this.isRetailWalkInCustomer() || subtotal > 0) {
                    this.paidAmount.setAttribute('data-val-range-min', 0.001);
                }
            } else {
                this.paidAmount.removeAttribute('data-val-range');
                this.paidAmount.removeAttribute('data-val-range-max');
                this.paidAmount.removeAttribute('data-val-range-min');
            }
        }

        reparseForm(this.root);
        document.querySelectorAll('.retailWalkinPaymentWarning')?.forEach(warning => warning.classList.toggle('d-none', !this.isRetailWalkInCustomer() || total == 0));

        if (!this.paymentIntent || !usesBankTransfer) {
            this.qrImage.removeAttribute('src');
            this.reference.textContent = '';
            this.qrAmount.textContent = '';
            this.qrStatus.textContent = '';
            this.qrExpires.textContent = '';
            return;
        }

        this.qrImage.src = this.paymentIntent.qrImageUrl || '';
        this.reference.textContent = this.paymentIntent.referenceCode || '';
        this.qrAmount.textContent = this.formatMoneyWithSymbol(this.paymentIntent.amount);
        this.qrStatus.textContent = `${this.getIntentStatusText(this.paymentIntent.status)}`;
        this.qrExpires.textContent = this.paymentIntent.expiresAtUtc
            ? `${this.formatDateTime(this.paymentIntent.expiresAtUtc)}`
            : '';
    }
    #toggleDeliveryTabs() {
        if (this.fulfillmentMode == 'notDelivered') {
            if (this.notDelivered.disabled) {
                this.notDelivered.classList.remove('active');
                document.querySelector(this.notDelivered.getAttribute('data-bs-target'))?.classList.remove('active', 'show');
            }
            else
                bootstrap.Tab.getOrCreateInstance(this.notDelivered).show();
        }
        if (this.fulfillmentMode == 'deliverNow')
            if (this.deliverNow.disabled) {
                this.deliverNow.classList.remove('active');
                document.querySelector(this.deliverNow.getAttribute('data-bs-target'))?.classList.remove('active', 'show');
            }
            else
                bootstrap.Tab.getOrCreateInstance(this.deliverNow).show();
    }
    #togglePaymentTabs() {
        if (this.paymentTiming == 'unpaid') {
            if (this.unpaid.disabled) {
                this.unpaid.classList.remove('active');
                document.querySelector(this.unpaid.getAttribute('data-bs-target'))?.classList.remove('active', 'show');
            }
            else
                bootstrap.Tab.getOrCreateInstance(this.unpaid).show();
        }
        if (this.paymentTiming == 'payNow')
            if (this.payNow.disabled) {
                this.payNow.classList.remove('active');
                document.querySelector(this.payNow.getAttribute('data-bs-target'))?.classList.remove('active', 'show');
            }
            else
                bootstrap.Tab.getOrCreateInstance(this.payNow).show();
    }

    renderCart() {
        this.cartBody.innerHTML = '';
        this.emptyCart.style.display = this.cart.length === 0 ? 'block' : 'none';
        this.cart.forEach((item, index) => {
            const row = document.createElement('tr');
            row.innerHTML = `
                <td class="ps-3 fw-medium align-middle">
                    <div class="d-flex align-items-center gap-3">
                        ${item.pictureUrl ? `<img src="${item.pictureUrl}" class="rounded product-picture order-item-thumb d-none d-lg-block" alt="" />` : ''}
                        <div>
                            <div class="fw-medium product-name">${this.escape(item.name)}</div>
                            <div class="text-muted small d-md-none">
                                ${DecimalFields.formatQuantity(item.quantity, item.quantityDecimalPlaces ?? 0)} × ${DecimalFields.formatCurrencyWithSymbol(item.unitPrice)}
                            </div>
                        </div>
                    </div>
                    <input type="text" class="visually-hidden product-id" name="Items[${index}].ProductId" value="${item.productId}"
                        data-val="true" data-val-required="Vui lòng chọn hàng hóa." />
                    <input type="hidden" name="Items[${index}].QuantityDecimalPlaces" value="${item.quantityDecimalPlaces ?? 0}" />
                    <input type="hidden" class="row-qty" name="Items[${index}].Quantity" value="${item.quantity}"
                        data-val="true" data-val-required="Vui lòng nhập số lượng."
                        data-val-range="Số lượng phải lớn hơn 0."
                        data-val-range-min="${(item.quantityDecimalPlaces ?? 0) > 0 ? '0.001' : '1'}"
                        data-val-number="Số lượng không đúng." />
                    <input type="hidden" class="row-price" name="Items[${index}].UnitPrice" value="${item.unitPrice}"
                        data-val="true" data-val-required="Vui lòng nhập đơn giá"
                        data-val-range="Đơn giá phải lớn hơn 0" data-val-range-min="0.1"
                        data-val-number="Đơn giá phải là số" />
                    <span class="small text-danger field-validation-valid"
                        data-valmsg-for="Items[${index}].ProductId"
                        data-valmsg-replace="true"></span>
                </td>
                <td class="align-middle">
                    ${this.renderWarehouseSelect(item)}
                </td>
                <td class="text-end d-none d-md-table-cell align-middle">
                    <span class="fw-medium">${DecimalFields.formatQuantity(item.quantity, item.quantityDecimalPlaces ?? 0)}</span>
                </td>
                <td class="text-end align-middle">
                    <span class="text-muted">${DecimalFields.formatCurrencyWithSymbol(item.unitPrice)}</span>
                </td>
                <td class="text-end fw-semibold text-nowrap d-none d-md-table-cell align-middle">
                    ${this.formatMoneyWithSymbol(item.quantity * item.unitPrice)}
                </td>
                <td class="text-center align-middle">
                    <button type="button" class="btn-table-action danger border-0 bg-transparent shadow-none" aria-label="Xóa hàng hóa">
                        <i class="bi bi-trash"></i>
                    </button>
                </td>`;

            const quantityInput = row.querySelectorAll('input')[0];
            const priceInput = row.querySelectorAll('input')[1];
            const warehouseSelect = row.querySelector('select[data-role="warehouse"]');

            const quantityChanged = debounce(() => {
                item.quantity = DecimalFields.getValue(quantityInput);
                this.paymentIntent = null;
                this.paymentIntentConfirmed = false;
                this.render();
            }, 1000);
            const unitPriceChanged = debounce(() => {
                item.unitPrice = DecimalFields.getValue(priceInput);
                this.paymentIntent = null;
                this.paymentIntentConfirmed = false;
                this.render();
            }, 1000);

            quantityInput.addEventListener('input', quantityChanged);
            priceInput.addEventListener('input', unitPriceChanged);
            if (warehouseSelect) {
                warehouseSelect.addEventListener('change', () => {
                    item.warehouseId = warehouseSelect.value;
                    this.resetPaymentIntent();
                    this.render();
                });
            }

            row.querySelector('button').addEventListener('click', () => {
                this.cart.splice(index, 1);
                this.resetPaymentIntent();
                this.render();
                this.productBrowser?.refresh();
                this.productBrowserMobile?.refresh();
            });

            if (this.itemEditor) {
                row.style.cursor = 'pointer';
                row.addEventListener('click', (e) => {
                    if (e.target.closest('button') || e.target.closest('select')) return;
                    const cartItem = this.cart[index];
                    if (!cartItem) return;
                    this.itemEditor.open({
                        name: cartItem.name,
                        quantity: cartItem.quantity,
                        unitPrice: cartItem.unitPrice,
                        quantityDecimalPlaces: cartItem.quantityDecimalPlaces
                    }, {
                        onApply: (qty, price) => {
                            cartItem.quantity = qty;
                            cartItem.unitPrice = price;
                            this.resetPaymentIntent();
                            this.render();
                        },
                        onDelete: () => {
                            this.cart.splice(index, 1);
                            this.resetPaymentIntent();
                            this.render();
                        }
                    });
                });
            }

            this.cartBody.appendChild(row);
        });
        DecimalFields.autoWrap(this.cartBody);
    }

    renderWarehouseSelect(item) {
        if (this.fulfillmentMode !== 'deliverNow') {
            return '';
        }
        const warehouses = item.availableWarehouses || [];
        const options = ['<option value="">Chọn kho</option>'];
        for (const warehouse of warehouses) {
            const selected = warehouse.id === item.warehouseId ? 'selected' : '';
            const quantity = this.formatQuantity(warehouse.quantityAvailable, item.quantityDecimalPlaces);
            options.push(`<option value="${this.escape(warehouse.id)}" ${selected}>${this.escape(warehouse.name)} - ${quantity} ${this.escape(item.unitMeasurement)}</option>`);
        }

        return `<select class="form-select form-select-sm" data-role="warehouse">${options.join('')}</select>`;
    }

    async createPaymentIntent(amount) {
        if (this.paymentTiming !== 'payNow') return;

        const validation = this.validateSaleInput();
        if (validation) {
            this.showAlert('warning', validation);
            return;
        }

        const saleInputVersion = this.saleInputVersion;
        const response = await this.postJson(this.urls.createIntent, {
            customerId: this.getSelectedCustomerId(),
            amount,
            note: this.note.value
        });
        if (this.saleInputVersion !== saleInputVersion)
            return;

        if (!response.success) {
            return;
        }

        this.paymentIntent = response.intent;
        this.paymentIntentConfirmed = false;
        this.startIntentPolling();
    }

    async confirmPaymentIntent() {
        if (!this.paymentIntent) return;

        const intentId = this.paymentIntent.id;
        const saleInputVersion = this.saleInputVersion;
        const response = await this.postJson(this.urls.confirmIntent, {
            intentId,
            note: this.note.value
        });
        if (!this.paymentIntent || this.paymentIntent.id !== intentId || this.saleInputVersion !== saleInputVersion) return;

        if (!response.success) {
            return;
        }
        if (response.intent?.id !== intentId) return;

        this.paymentIntent = response.intent;
        this.paymentIntentConfirmed = this.isIntentConfirmed(this.paymentIntent);
        this.pollRequestSeq += 1;
        this.stopIntentPolling();
        this.showAlert('success', 'Đã xác nhận tiền vào tài khoản.');
    }

    startIntentPolling() {
        this.stopIntentPolling();
        if (!this.paymentIntent || !this.isIntentPending(this.paymentIntent)) return;

        this.statusTimer = window.setInterval(() => this.refreshIntentStatus(), StatusPollingIntervalMs);
        this.refreshIntentStatus();
    }

    stopIntentPolling() {
        if (!this.statusTimer) return;

        window.clearInterval(this.statusTimer);
        this.statusTimer = null;
    }

    async refreshIntentStatus() {
        if (!this.paymentIntent) {
            this.stopIntentPolling();
            return;
        }

        const intentId = this.paymentIntent.id;
        const saleInputVersion = this.saleInputVersion;
        const requestSeq = this.pollRequestSeq + 1;
        this.pollRequestSeq = requestSeq;
        const params = new URLSearchParams({ intentId });
        let data;

        try {
            const response = await fetch(`${this.urls.statusIntent}?${params.toString()}`);
            data = await response.json();
        } catch {
            if (!this.isCurrentPollingIntent(intentId, saleInputVersion, requestSeq)) return;

            this.showAlert('error', 'Không thể cập nhật trạng thái QR.');
            this.resetPaymentIntent();
            this.render();
            return;
        }

        if (!this.isCurrentPollingIntent(intentId, saleInputVersion, requestSeq)) return;

        if (!data?.success) {
            this.showAlert('error', data?.message);
            this.resetPaymentIntent();
            this.render();
            return;
        }
        if (data.intent?.id !== intentId) return;

        this.paymentIntent = data.intent;
        this.paymentIntentConfirmed = this.isIntentConfirmed(this.paymentIntent);
        if (this.paymentIntentConfirmed) {
            this.showAlert('success', 'Đã nhận tiền vào tài khoản.');
            this.stopIntentPolling();
        }

        if (this.isIntentExpiredOrCancelled(this.paymentIntent)) {
            this.showAlert('warning', 'QR đã hết hạn hoặc đã hủy. Vui lòng tạo QR mới.');
            this.stopIntentPolling();
        }

        if (!this.isIntentPending(this.paymentIntent)) this.stopIntentPolling();

        this.render();
    }

    async completeSale() {
        if (!$(this.root).valid())
            return;

        const validation = this.validateSaleInput();
        if (validation) {
            this.showAlert('warning', validation);
            return;
        }

        if (this.paymentTiming == 'unpaid' && this.isRetailWalkInCustomer()) {
            this.showAlert('warning', 'Khách bán lẻ cần thanh toán hoặc đặt cọc.');
            return;
        }

        const subTotal = this.calculateSubtotal();
        let paymentAmount = 0;
        if (this.paymentTiming == 'payNow' && subTotal > 0) {
            const paymentProcess = new PaymentProcess(subTotal, !this.isRetailWalkInCustomer() || this.fulfillmentMode != 'deliverNow', this);
            const paymentResult = await paymentProcess.startPayment();
            if (!paymentResult.success)
                return;
            paymentAmount = paymentResult.amount;
        }

        if (this.paymentTiming === 'payNow' && this.paymentMethod === 'bank' && !this.paymentIntentConfirmed) {
            this.showAlert('warning', 'Chuyển khoản chưa được xác nhận.');
            return;
        }

        const payload = this.buildSalePayload(paymentAmount);
        const url = this.resolveSaleUrl();
        if (this.paymentTiming === 'payNow' && this.paymentMethod === 'bank') payload.paymentIntentId = this.paymentIntent.id;

        this.complete.disabled = true;
        showPageLoading();
        const response = await this.postJson(url, payload);
        this.complete.disabled = false;

        if (!response.success) {
            hidePageLoading();
            return;
        }

        if (this.fulfillmentMode === 'notDelivered' && response.orderItems?.length > 0 && this.urls.schedule) {
            hidePageLoading();
            this.showScheduleModal(response.orderId, response.orderItems, response.orderUrl);
            return;
        }

        window.setTimeout(() => { window.location.href = response.orderUrl || '/Order/List'; }, 500);
    }

    validateSaleInput() {
        if (!this.getSelectedCustomerId()) return 'Vui lòng chọn khách hàng.';
        if (this.fulfillmentMode === 'deliverNow' && this.cart.some(item => !item.warehouseId)) return 'Vui lòng chọn kho cho từng mặt hàng.';
        if (this.cart.length === 0) return 'Vui lòng thêm hàng hóa.';
        if (this.cart.some(item => item.quantity <= 0)) return 'Số lượng phải lớn hơn 0.';
        if (this.calculateTotal() <= 0) return 'Tổng tiền phải lớn hơn 0.';
        if (this.fulfillmentMode === 'notDelivered') {
            if (!this.shippingPhoneNumber.value.trim()) return 'Vui lòng nhập số điện thoại giao hàng.';
            if (!this.shippingAddress.value.trim()) return 'Vui lòng nhập địa chỉ giao hàng.';
        }
        return null;
    }

    buildSalePayload(paymentAmount) {
        return {
            customerId: this.getSelectedCustomerId(),
            warehouseId: this.getHeaderWarehouseId(),
            items: this.cart.map(item => ({
                productId: item.productId,
                warehouseId: this.fulfillmentMode === 'deliverNow' ? item.warehouseId : EmptyGuid,
                quantity: item.quantity,
                unitPrice: item.unitPrice
            })),
            shippingAddress: this.shippingAddress.value,
            shippingPhoneNumber: this.shippingPhoneNumber.value,
            orderDiscount: this.getDiscount(),
            note: this.note.value,
            fulfillmentMode: this.fulfillmentMode === 'deliverNow' ? FulfillmentMode.DeliverNow : FulfillmentMode.NotDelivered,
            paymentTiming: this.paymentTiming === 'payNow' ? PaymentTiming.PayNow : PaymentTiming.Unpaid,
            paidAmount: this.paymentTiming === 'payNow' ? paymentAmount : 0
        };
    }

    resolveSaleUrl() {
        if (this.paymentTiming === 'unpaid') return this.urls.createUnpaidSale;
        return this.paymentMethod === 'bank' ? this.urls.createBankSale : this.urls.createCashSale;
    }

    getSelectedCustomerId() {
        return this.selectedCustomer?.id || '';
    }

    resolveInitialWarehouseId(product) {
        if (this.fulfillmentMode !== 'deliverNow') return '';

        const warehouses = product.availableWarehouses || [];
        const selectedWarehouse = warehouses.find(warehouse => warehouse.id === this.warehouse.value);
        if (selectedWarehouse) return selectedWarehouse.id;

        return warehouses[0]?.id || '';
    }

    getHeaderWarehouseId() {
        if (this.fulfillmentMode !== 'deliverNow') return EmptyGuid;
        return this.warehouse.value || this.cart.find(item => item.warehouseId)?.warehouseId || EmptyGuid;
    }

    async postJson(url, payload) {
        const result = await apiPost(url, payload);
        return result;
    }

    calculateSubtotal() {
        return this.cart.reduce((sum, item) => sum + item.quantity * item.unitPrice, 0);
    }

    getDiscount() {
        return DecimalFields.getValue(this.discount);
    }

    calculateTotal() {
        return Math.max(0, this.calculateSubtotal());
    }

    isIntentPending(intent) {
        return Number(intent?.status) === IntentStatus.Pending;
    }

    isIntentConfirmed(intent) {
        const status = Number(intent?.status);
        return status === IntentStatus.Confirmed || status === IntentStatus.ManuallyConfirmed;
    }

    isIntentExpiredOrCancelled(intent) {
        const status = Number(intent?.status);
        return status === IntentStatus.Expired || status === IntentStatus.Cancelled;
    }

    isCurrentPollingIntent(intentId, saleInputVersion, requestSeq) {
        return this.paymentIntent
            && this.paymentIntent.id === intentId
            && this.saleInputVersion === saleInputVersion
            && this.pollRequestSeq === requestSeq;
    }

    getIntentStatusText(status) {
        switch (Number(status)) {
            case IntentStatus.Pending:
                return 'Đang chờ thanh toán';
            case IntentStatus.Confirmed:
                return 'Đã nhận tiền';
            case IntentStatus.ManuallyConfirmed:
                return 'Đã nhận tiền';
            case IntentStatus.Expired:
                return 'Hết hạn';
            case IntentStatus.Cancelled:
                return 'Đã hủy';
            case IntentStatus.Consumed:
                return 'Đã sử dụng';
            default:
                return 'Không xác định';
        }
    }

    getCompleteButtonHtml() {
        // if (this.cart.length == 0)
        //     return '<i class="bi bi-check2-square me-1"></i> Hoàn tất bán hàng';

        // if (this.paymentTiming === 'unpaid') {
        //     return '<i class="bi bi-receipt me-1"></i> Tạo đơn chưa thanh toán';
        // }

        // if (this.fulfillmentMode === 'notDelivered') {
        //     return '<i class="bi bi-check2-square me-1"></i> Tạo đơn và ghi nhận tiền cọc';
        // }

        // return '<i class="bi bi-check2-square me-1"></i> Hoàn tất bán hàng';
        const total = this.calculateTotal();
        const paidAmount = DecimalFields.getValue(this.paidAmount);
        if (total == 0 || paidAmount == 0 || this.paymentTiming == 'unpaid')
            return '<i class="bi bi-floppy me-1"></i> Tạo đơn hàng';
        return 'Bắt đầu thanh toán <i class="bi bi-arrow-right"></i>';
    }

    formatDateTime(value) {
        const date = new Date(value);
        if (Number.isNaN(date.getTime())) return '';

        return new Intl.DateTimeFormat('vi-VN', {
            dateStyle: 'short',
            timeStyle: 'short'
        }).format(date);
    }

    formatMoney(value) {
        return DecimalFields.formatCurrency(value || 0);
    }

    formatMoneyWithSymbol(value) {
        return DecimalFields.formatCurrencyWithSymbol(value || 0);
    }

    formatQuantity(value, decimals) {
        return DecimalFields.formatQuantity(value, decimals);
    }

    showAlert(type, message) {
        window.NotificationCenter[type](message, 'warning');
    }

    escape(value) {
        const div = document.createElement('div');
        div.textContent = value ?? '';
        return div.innerHTML;
    }

    showScheduleModal(orderId, orderItems, orderUrl) {
        const modalEl = document.getElementById('fastSaleScheduleModal');
        if (!modalEl) {
            if (orderUrl) window.location.href = orderUrl;
            return;
        }

        const notBeforeDateValue = '20';
        const modeSelect = document.getElementById('fastSaleScheduleMode');
        const dateSection = document.getElementById('fastSaleScheduleDateSection');
        const fromInput = document.getElementById('fastSaleScheduleFrom');
        const noteInput = document.getElementById('fastSaleScheduleNote');
        const orderIdInput = document.getElementById('fastSaleScheduleOrderId');
        const itemsPayload = document.getElementById('fastSaleScheduleItemsPayload');
        const itemsTable = document.getElementById('fastSaleScheduleItemsTable');
        const form = document.getElementById('fastSaleScheduleForm');
        const skipBtn = document.getElementById('fastSaleScheduleSkip');

        orderIdInput.value = orderId ?? '';
        noteInput.value = '';
        fromInput.value = '';
        modeSelect.value = '10';
        dateSection.classList.add('d-none');
        fromInput.removeAttribute('required');

        modeSelect.onchange = () => {
            const needsDate = modeSelect.value === notBeforeDateValue;
            dateSection.classList.toggle('d-none', !needsDate);
            fromInput.toggleAttribute('required', needsDate);
            if (!needsDate) fromInput.value = '';
        };

        itemsTable.innerHTML = '';
        const cartItems = this.cart;
        const rows = orderItems.map((item, index) => {
            const cartItem = cartItems[index];
            const decimalPlaces = cartItem?.quantityDecimalPlaces ?? 0;
            const div = document.createElement('div');
            div.className = 'd-flex align-items-center justify-content-between gap-3 py-2 border-bottom';
            div.innerHTML = `
                <span class="fw-medium">${this.escape(item.productName)}</span>
                <div class="d-flex align-items-end flex-column">
                    <input type="text" class="form-control form-control-sm text-end schedule-qty-input"
                           style="max-width:120px" data-val="true" name="item${index}_quantity"
                           value="${this.formatQuantity(item.quantity, decimalPlaces)}"
                           data-decimal="quantity" data-decimals="${decimalPlaces}"
                           data-val-range="Số lượng phải nhỏ hơn ${DecimalFields.formatQuantity(cartItem.quantity, cartItem.quantityDecimalPlaces)}" 
                           data-val-range-max="${cartItem.quantity}"
                           data-val-number="Số lượng không đúng"
                           placeholder="Nhập số lượng" />
                    <span class="small text-danger text-end field-validation-valid"
                          data-valmsg-for="item${index}_quantity" data-valmsg-replace="true"></span>
               </div>
               `;
            itemsTable.appendChild(div);
            return { item, input: div.querySelector('input') };
        });
        DecimalFields.autoWrap(itemsTable);

        reparseForm(form);

        skipBtn.onclick = () => {
            showPageLoading();
            bootstrap.Modal.getInstance(modalEl)?.hide();
            if (orderUrl) window.location.href = orderUrl;
            else location.reload();
        };

        form.onsubmit = async (e) => {
            e.preventDefault();
            if (!$(form).valid())
                return;

            itemsPayload.replaceChildren();
            let index = 0;
            rows.forEach(({ item, input }) => {
                const qty = DecimalFields.getValue(input);
                if (!qty) return;
                const append = (name, value) => {
                    const el = document.createElement('input');
                    el.type = 'hidden'; el.name = name; el.value = value ?? '';
                    itemsPayload.appendChild(el);
                };
                append(`Items[${index}].OrderItemId`, item.orderItemId);
                append(`Items[${index}].ProductId`, item.productId);
                append(`Items[${index}].ProductName`, item.productName);
                append(`Items[${index}].Quantity`, qty);
                index++;
            });

            if (index === 0) {
                this.showAlert('warning', 'Vui lòng nhập số lượng cho ít nhất một hàng hóa.');
                return;
            }

            showPageLoading();
            const submitBtn = form.querySelector('button[type="submit"]');
            submitBtn.disabled = true;
            try {
                const result = await apiPost(this.urls.schedule, DecimalFields.getFormData(form));
                if (!result.success) {
                    hidePageLoading();
                    if (result.message)
                        this.showAlert('error', result.message || 'Không thể lưu lịch.');
                    return;
                }
            } finally {
                submitBtn.disabled = false;
            }

            bootstrap.Modal.getInstance(modalEl)?.hide();
            if (orderUrl) window.location.href = orderUrl;
            else location.reload();
        };

        bootstrap.Modal.getOrCreateInstance(modalEl).show();
    }
}

class PaymentProcess {
    #subTotal;
    #reference;
    #amount;
    #canChangePaidAmount;

    constructor(subTotal, canChangePaidAmount, reference) {
        this.#reference = reference;
        this.#canChangePaidAmount = canChangePaidAmount;
        this.root = document.getElementById('paymentModal');
        this.modal = bootstrap.Modal.getOrCreateInstance(this.root);

        this.#subTotal = subTotal;

        this.form = document.getElementById('paymentForm');
        this.paymentSubTotal = this.form.querySelector('#paymentSubTotal');
        this.paymentTotal = this.form.querySelector('#paymentTotal');
        this.paymentTotalHint = this.form.querySelector('#paymentTotalHint');
        this.skipPaymentBtn = this.form.querySelector('#paymentSkip');
        this.discountInput = this.form.querySelector('#fastSaleDiscount');
        this.paidAmountInput = this.form.querySelector('#paidAmount');
        this.debtAmount = this.form.querySelector('#debtAmount');
        this.paymentProcessContainer = this.form.querySelector('#paymentProcessContainer');
        this.submitBtn = this.form.querySelector('[type=submit]');
        this.moneyReceivedBtn = this.form.querySelector('#moneyReceived');
        this.createQr = this.form.querySelector('#fastSaleCreateQr');
        this.cashMethod = this.form.querySelector('#fastSaleCashMethod');
        this.bankMethod = this.form.querySelector('#fastSaleBankMethod');
    }

    async startPayment() {
        const self = this;
        let promiseResolve;
        let promiseReject;

        //event listeners
        const onDiscountChanged = debounce(async () => {
            this.render();
            if (this.#reference.paymentMethod == 'bank')
                await this.#createPaymentQrCode();
            else
                this.#reference.resetPaymentIntent();
        }, 700);

        const onAmountChanged = this.#canChangePaidAmount ? debounce(async () => {
            const total = this.#calculateTotal();
            const amount = DecimalFields.getValue(this.paidAmountInput);
            const debt = Math.max(0, total - amount);
            this.debtAmount.classList.toggle('d-none', debt == 0);
            if (debt > 0)
                this.debtAmount.textContent = `Còn nợ ${DecimalFields.formatCurrencyWithSymbol(debt)}`;
            if (this.#reference.paymentMethod == 'bank')
                await this.#createPaymentQrCode();
            else
                this.#reference.resetPaymentIntent();
            this.#amount = amount;
        }, 700) : Function.prototype;

        const onPaymentSubmit = e => {
            e.preventDefault();
            if (!$(this.form).valid())
                return;
            promiseResolve({ success: true, amount: 0});
            this.modal.hide();
        }

        const createQr = async () => {
            await this.#createPaymentQrCode();
        }

        const confirmMoneyReceived = async () => {
            if (!$(this.form).valid())
                return;
            this.root.classList.add('d-none');
            const confirmed = await confirm('Đã nhận tiền', `Bạn xác nhận đã nhận đủ ${DecimalFields.formatCurrencyWithSymbol(this.#amount)}`);
            if (!confirmed) {
                this.root.classList.remove('d-none');
                return;
            }
            await this.#reference.confirmPaymentIntent();
            promiseResolve({ success: true, amount: this.#amount });
            this.modal.hide();
        }

        const setCashPayment = () => this.#setPaymentMethod('cash');
        const setBankPayment = () => this.#setPaymentMethod('bank');

        const promise = new Promise((resolve, reject) => {
            promiseResolve = resolve;
            promiseReject = reject;

            this.createQr.addEventListener('click', createQr);
            this.moneyReceivedBtn.addEventListener('click', confirmMoneyReceived);
            this.discountInput.addEventListener('input', onDiscountChanged);
            this.paidAmountInput.addEventListener('input', onAmountChanged);
            this.form.addEventListener('submit', onPaymentSubmit);
            this.cashMethod.addEventListener('click', setCashPayment);
            this.bankMethod.addEventListener('click', setBankPayment);

            this.root.addEventListener('show.bs.modal', function onShow() {
                self.root.removeEventListener('show.bs.modal', onShow);
                self.skipPaymentBtn.addEventListener('click', function onClick() {
                    self.skipPaymentBtn.removeEventListener('click', onClick);
                    self.modal.hide();
                });

                self.render();
            });

            this.root.addEventListener('hidden.bs.modal', function onHidden(e) {
                self.root.removeEventListener('hidden.bs.modal', onHidden);
                self.discountInput.value = 0;
                self.discountInput.removeEventListener('input', onDiscountChanged);
                self.paidAmountInput.value = 0;
                self.paidAmountInput.removeEventListener('input', onAmountChanged);
                self.createQr.removeEventListener('click', createQr);
                self.moneyReceivedBtn.removeEventListener('click', confirmMoneyReceived);
                self.form.removeEventListener('submit', this.onPaymentSubmit);
                self.cashMethod.removeEventListener('click', setCashPayment);
                self.bankMethod.removeEventListener('click', setBankPayment);
                self.#reference.resetPaymentIntent();
                self.root.classList.remove('d-none');
                resolve({ success: false });
                self.render();
            });
        });

        this.modal.show();

        return promise;
    }

    async render() {
        let discount = DecimalFields.getValue(this.discountInput);
        if (discount > this.#subTotal)
            discount = this.#subTotal;
        this.discountInput.value = discount;
        const total = this.#calculateTotal();
        if (total == 0) {
            if (this.#reference.paymentMethod != 'cash') {
                await this.#setPaymentMethod('cash');
                this.render();
                return;
            }
        }
        this.#reference.bankMethod.disabled = total == 0;

        this.paymentSubTotal.textContent = DecimalFields.formatCurrency(this.#subTotal);
        this.paymentTotal.textContent = DecimalFields.formatCurrencyWithSymbol(total);
        this.paymentTotalHint.classList.toggle('d-none', total <= 0);
        if (total > 0) {
            this.paymentTotalHint.textContent = window.SoBangChu?.docSoTien(total) ?? '';
        }

        this.paidAmountInput.value = DecimalFields.formatCurrency(total);
        this.paidAmountInput.disabled = total == 0 || !this.#canChangePaidAmount;
        this.paidAmountInput.closest('div.d-flex').classList.toggle('d-none', !this.#canChangePaidAmount);

        this.paymentProcessContainer.classList.toggle('d-none', total == 0);

        this.discountInput.setAttribute('data-val-range', `Giảm giá phải nhỏ hơn ${DecimalFields.formatCurrencyWithSymbol(this.#subTotal)}`);
        this.discountInput.setAttribute('data-val-range-min', 0);
        this.discountInput.setAttribute('data-val-range-max', this.#subTotal);

        this.#amount = total;

        if (total > 0 && this.#canChangePaidAmount) {
            this.paidAmountInput.setAttribute('data-val-range', `Số tiền thanh toán phải lớn hơn 0 và nhỏ hơn ${DecimalFields.formatCurrencyWithSymbol(total)}`);
            this.paidAmountInput.setAttribute('data-val-range-max', total);
            if (this.#reference.isRetailWalkInCustomer()) {
                this.paidAmountInput.setAttribute('data-val-range-min', 0.001);
            }
        } else {
            this.paidAmountInput.removeAttribute('data-val-range');
            this.paidAmountInput.removeAttribute('data-val-range-max');
            this.paidAmountInput.removeAttribute('data-val-range-min');
        }

        reparseForm(this.form);
        this.submitBtn.disabled = !$(this.form).valid();
        this.submitBtn.classList.toggle('d-none', total > 0);
        this.moneyReceivedBtn.classList.toggle('d-none', total == 0);
    }

    #calculateTotal() {
        return Math.max(this.#subTotal - DecimalFields.getValue(this.discountInput));
    }

    async #setPaymentMethod(method) {
        if (method === 'bank' && !this.#reference.bankTransferEnabled) return;
        this.#reference.paymentMethod = method;
        this.#reference.resetPaymentIntent();
        this.#reference.cashMethod.classList.toggle('active', method === 'cash');
        this.#reference.bankMethod.classList.toggle('active', method === 'bank');
        this.#reference.qrPanel.classList.toggle('d-none', this.#reference.paymentMethod != 'bank')
        if (this.#reference.paymentMethod == 'bank')
            await this.#createPaymentQrCode();
        else
            this.#reference.resetPaymentIntent();
    }

    async #createPaymentQrCode() {
        if (!$(this.form).valid())
            return;
        const amount = DecimalFields.getValue(this.paidAmountInput);
        if (amount !== Math.trunc(amount)) {
            this.#reference.showAlert('warning', 'Số tiền phải là số nguyên.');
            return;
        }
        this.#reference.resetPaymentIntent();
        await this.#reference.createPaymentIntent(amount);
    }

}

const root = document.getElementById('fastSaleApp');
if (root) new FastSale(root);

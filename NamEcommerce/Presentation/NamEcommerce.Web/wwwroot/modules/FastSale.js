import { apiPost } from "/modules/ajax-helper.js";
import { confirm } from "/modules/modals.js";
import CustomerPicker from "/modules/CustomerPicker.js";
import ProductBrowser from "/modules/ProductBrowser.js";
import ItemEditor from "/modules/ItemEditor.js";

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
            createUnpaidSale: root.dataset.createUnpaidSaleUrl
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
        this.itemEditor = null;
        this.fromCommand = 'add';

        this.defaultCustomer = document.getElementById('CustomerId').dataset.default;

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
        this.paymentMethodSection = document.getElementById('fastSalePaymentMethodSection');
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
        this.clearCart = document.getElementById('fastSaleClearCart');
        this.customer = document.getElementById('CustomerId');
        this.shippingPhoneNumber = document.getElementById('ShippingPhoneNumber');
        this.shippingAddress = document.getElementById('ShippingAddress');
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
        this.discount.addEventListener('input', () => {
            this.resetPaymentIntent();
            this.render();
        });
        this.deliverNow.addEventListener('click', () => this.setFulfillmentMode('deliverNow'));
        this.notDelivered.addEventListener('click', () => this.setFulfillmentMode('notDelivered'));
        this.payNow.addEventListener('click', () => this.setPaymentTiming('payNow'));
        this.unpaid.addEventListener('click', () => this.setPaymentTiming('unpaid'));
        this.cashMethod.addEventListener('click', () => this.setPaymentMethod('cash'));
        this.bankMethod.addEventListener('click', () => this.setPaymentMethod('bank'));
        this.createQr.addEventListener('click', () => this.createPaymentIntent());
        this.confirmQr.addEventListener('click', () => this.confirmPaymentIntent());
        this.complete.addEventListener('click', () => this.completeSale());
        this.clearCart.addEventListener('click', () => {
            this.cart = [];
            this.resetPaymentIntent();
            this.render();
        });
        this.shippingAddress?.addEventListener('input', () => this.resetPaymentIntent());
        this.shippingPhoneNumber?.addEventListener('input', () => this.resetPaymentIntent());
    }

    setFulfillmentMode(mode) {
        this.fulfillmentMode = mode;
        this.deliverNow.classList.toggle('active', mode === 'deliverNow');
        this.notDelivered.classList.toggle('active', mode === 'notDelivered');
        this.cart.forEach(item => {
            item.warehouseId = mode === 'deliverNow' ? (item.warehouseId || this.resolveInitialWarehouseId(item)) : '';
        });
        this.resetPaymentIntent();
        this.productBrowser?.reload();
        this.render();
    }

    setPaymentTiming(timing) {
        this.paymentTiming = timing;
        this.payNow.classList.toggle('active', timing === 'payNow');
        this.unpaid.classList.toggle('active', timing === 'unpaid');
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
        if (existing) {
            existing.quantity += 1;
        } else {
            this.cart.push({
                productId: product.id,
                name: product.name,
                warehouseId,
                unitMeasurement: product.unitMeasurement,
                availableWarehouses: product.availableWarehouses || [],
                quantity: 1,
                quantityAvailable: product.quantityAvailable,
                unitPrice: Number(product.unitPrice || 0),
                quantityDecimalPlaces: Number(product.quantityDecimalPlaces || 0)
            });
        }
        if (product.quantityAvailable)
            this.fromCommand = 'add-available';
        else
            this.fromCommand = 'add';

        this.resetPaymentIntent();
        this.render();
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

    render() {
        const subtotal = this.calculateSubtotal();
        let discount = this.cart.length > 0 ? this.getDiscount() : 0;

        if (discount > subtotal) {
            discount = subtotal;
            this.discount.value = this.formatMoney(discount);
        }

        const total = Math.max(0, subtotal - discount);
        const isPayNow = this.paymentTiming === 'payNow';
        const usesBankTransfer = isPayNow && this.paymentMethod === 'bank';

        if (total == 0 && this.paymentTiming == 'payNow') {
            this.setPaymentTiming('unpaid');
            return;
        }
        if (total > 0 && this.selectedCustomer?.id == this.defaultCustomer && this.paymentTiming == 'unpaid') {
            this.setPaymentTiming('payNow');
            return;
        }
        if (this.fromCommand === 'add-available' && this.cart.length == 1 && this.cart[0].quantity <= this.cart[0].quantityAvailable && this.fulfillmentMode == 'notDelivered') {
            this.fromCommand = '';
            this.setFulfillmentMode('deliverNow');
            return;
        }
        if (this.cart.some(item => item.quantity > item.quantityAvailable) && this.fulfillmentMode == 'deliverNow') {
            this.setFulfillmentMode('notDelivered');
            return;
        }

        this.payNow.disabled = total == 0;
        this.unpaid.disabled = total == 0 || this.selectedCustomer?.id == this.defaultCustomer;

        this.notDelivered.disabled = this.cart.length === 0;
        this.deliverNow.disabled = this.cart.length === 0 || this.cart.some(item => item.quantity > item.quantityAvailable);

        this.renderCart();

        this.subtotal.textContent = this.formatMoneyWithSymbol(subtotal);
        this.total.textContent = this.formatMoneyWithSymbol(total);
        this.discountDisplay.textContent = this.formatMoneyWithSymbol(discount);
        this.paymentMethodSection.classList.toggle('d-none', !isPayNow);
        this.qrPanel.classList.toggle('d-none', !usesBankTransfer);
        this.createQr.disabled = !usesBankTransfer || total <= 0 || this.cart.length === 0;
        this.confirmQr.disabled = !this.paymentIntent || this.paymentIntentConfirmed || !this.manualConfirmEnabled || !this.isIntentPending(this.paymentIntent);
        this.complete.disabled = this.cart.length === 0 || total <= 0 || (usesBankTransfer && !this.paymentIntentConfirmed);
        this.complete.innerHTML = this.getCompleteButtonHtml();

        if (total > 0)
            this.totalHint.textContent = window.SoBangChu?.docSoTien(total) ?? '';

        this.customer.value = this.selectedCustomer?.id ?? '';
        const showShippingInfo = this.cart.length > 0 && this.fulfillmentMode === 'notDelivered';
        this.shippingAddress.disabled = !showShippingInfo;
        this.shippingPhoneNumber.disabled = !showShippingInfo;
        this.shippingAddress.closest('.ship-info').classList.toggle('d-none', !showShippingInfo);

        this.discount.disabled = this.cart.length === 0;

        this.fromCommand = '';

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
        this.qrStatus.textContent = `Trang thai: ${this.getIntentStatusText(this.paymentIntent.status)}`;
        this.qrExpires.textContent = this.paymentIntent.expiresAtUtc
            ? `Het han: ${this.formatDateTime(this.paymentIntent.expiresAtUtc)}`
            : '';
    }

    renderCart() {
        this.cartBody.innerHTML = '';
        this.emptyCart.style.display = this.cart.length === 0 ? 'block' : 'none';
        this.clearCart.classList.toggle('d-none', this.cart.length === 0);

        this.cart.forEach((item, index) => {
            const row = document.createElement('tr');
            row.innerHTML = `
                <td class="ps-3 fw-medium">${this.escape(item.name)}</td>
                <td style="width: 190px;">
                    ${this.renderWarehouseSelect(item)}
                </td>
                <td class="text-center" style="width: 110px;">
                    <input class="form-control form-control-sm text-end" value="${this.formatQuantity(item.quantity, item.quantityDecimalPlaces)}" data-decimal="quantity" data-decimals="${item.quantityDecimalPlaces}"/>
                </td>
                <td class="text-end" style="width: 140px;">
                    <input class="form-control form-control-sm text-end" value="${this.formatMoney(item.unitPrice)}" data-decimal="currency" />
                </td>
                <td class="text-end fw-semibold text-nowrap">${this.formatMoneyWithSymbol(item.quantity * item.unitPrice)}</td>
                <td class="text-end pe-3" style="width: 48px;">
                    <button type="button" class="btn btn-link link-danger p-0 border-0" title="Xóa"><i class="bi bi-trash"></i></button>
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
                this.fromCommand = 'remove';
                this.cart.splice(index, 1);
                this.resetPaymentIntent();
                this.render();
                this.productBrowser.refresh();
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

    async createPaymentIntent() {
        if (this.paymentTiming !== 'payNow') return;

        const validation = this.validateSaleInput();
        if (validation) {
            this.showAlert('warning', validation);
            return;
        }

        const total = this.calculateTotal();
        if (total !== Math.trunc(total)) {
            this.showAlert('warning', 'Số tiền VietQR phải là VND nguyên.');
            return;
        }

        const saleInputVersion = this.saleInputVersion;
        const response = await this.postJson(this.urls.createIntent, {
            customerId: this.getSelectedCustomerId(),
            amount: total,
            note: this.note.value
        });
        if (this.saleInputVersion !== saleInputVersion) return;

        if (!response.success) {
            return;
        }

        this.paymentIntent = response.intent;
        this.paymentIntentConfirmed = false;
        this.startIntentPolling();
        this.showAlert('success', 'Đã tạo QR chuyển khoản.');
        this.render();
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
        this.render();
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

            this.showAlert('error', 'Khong the cap nhat trang thai QR.');
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
            this.showAlert('success', 'Da xac nhan tien vao tai khoan.');
            this.stopIntentPolling();
        }

        if (this.isIntentExpiredOrCancelled(this.paymentIntent)) {
            this.showAlert('warning', 'QR da het han hoac da bi huy. Vui long tao QR moi.');
            this.stopIntentPolling();
        }

        if (!this.isIntentPending(this.paymentIntent)) this.stopIntentPolling();

        this.render();
    }

    async completeSale() {
        const validation = this.validateSaleInput();
        if (validation) {
            this.showAlert('warning', validation);
            return;
        }

        if (this.paymentTiming === 'payNow' && this.paymentMethod === 'bank' && !this.paymentIntentConfirmed) {
            this.showAlert('warning', 'Chuyển khoản chưa được xác nhận.');
            return;
        }

        if (this.paymentTiming == 'unpaid' && this.selectedCustomer?.id == this.defaultCustomer) {
            if (! await confirm('Chưa thanh toán', 'Khách hàng hiện tại là khách lẻ. Bạn có chắc chắn muốn tạo đơn chưa thanh toán cho khách này không?')) {
                return;
            }
        }

        const payload = this.buildSalePayload();
        const url = this.resolveSaleUrl();
        if (this.paymentTiming === 'payNow' && this.paymentMethod === 'bank') payload.paymentIntentId = this.paymentIntent.id;

        this.complete.disabled = true;
        const response = await this.postJson(url, payload);
        this.complete.disabled = false;

        if (!response.success) {
            return;
        }

        this.showAlert('success', 'Đã hoàn tất bán hàng.');
        if (response.orderUrl) {
            window.setTimeout(() => { window.location.href = response.orderUrl; }, 500);
        }
    }

    validateSaleInput() {
        if (!this.getSelectedCustomerId()) return 'Vui lòng chọn khách hàng.';
        if (this.fulfillmentMode === 'deliverNow' && this.cart.some(item => !item.warehouseId)) return 'Vui lòng chọn kho cho từng dòng hàng.';
        if (this.cart.length === 0) return 'Vui lòng thêm hàng hóa.';
        if (this.cart.some(item => item.quantity <= 0)) return 'Số lượng phải lớn hơn 0.';
        if (this.calculateTotal() <= 0) return 'Tổng tiền phải lớn hơn 0.';
        if (this.fulfillmentMode === 'notDelivered') {
            if (!this.shippingPhoneNumber.value.trim()) return 'Vui lòng nhập số điện thoại giao hàng.';
            if (!this.shippingAddress.value.trim()) return 'Vui lòng nhập địa chỉ giao hàng.';
        }
        return null;
    }

    buildSalePayload() {
        const total = this.calculateTotal();
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
            paidAmount: this.paymentTiming === 'payNow' ? total : 0
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
        return Math.max(0, this.calculateSubtotal() - this.getDiscount());
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
                return 'Dang cho chuyen khoan';
            case IntentStatus.Confirmed:
                return 'Da nhan tien';
            case IntentStatus.ManuallyConfirmed:
                return 'Da xac nhan thu cong';
            case IntentStatus.Expired:
                return 'Da het han';
            case IntentStatus.Cancelled:
                return 'Da huy';
            case IntentStatus.Consumed:
                return 'Da su dung';
            default:
                return 'Khong xac dinh';
        }
    }

    getCompleteButtonHtml() {
        if (this.paymentTiming === 'unpaid') {
            return '<i class="bi bi-receipt me-1"></i> Tạo đơn chưa thanh toán';
        }

        if (this.fulfillmentMode === 'notDelivered') {
            return '<i class="bi bi-check2-square me-1"></i> Tạo đơn và ghi nhận tiền cọc';
        }

        return '<i class="bi bi-check2-square me-1"></i> Hoàn tất bán hàng';
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
}

const root = document.getElementById('fastSaleApp');
if (root) new FastSale(root);

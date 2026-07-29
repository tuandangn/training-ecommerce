import { apiPost } from "/modules/ajax-helper.js";
import { confirm } from "/modules/modals.js";
import { customerInfo } from "/modules/CustomerInfo.js";
import CustomerPicker from "/modules/CustomerPicker.js";
import ProductBrowser from "/modules/ProductBrowser.js";
import ItemEditor from "/modules/ItemEditor.js";
import DecimalFields from "/modules/DecimalFields.js";

class FastSale {
    #deliveryNow = Symbol('deliveryNow');
    #notDelivered = Symbol('notDelivered');
    #payNow = Symbol('payNow');
    #unpaid = Symbol('unpaid');

    #paymentProcess;

    constructor(root) {
        this.root = root;
        this.urls = {
            createCashSale: root.dataset.createCashSaleUrl,
            createBankSale: root.dataset.createBankSaleUrl,
            createUnpaidSale: root.dataset.createUnpaidSaleUrl,
            schedule: root.dataset.scheduleUrl
        };
        this.selectedCustomer = null;
        this.customerPicker = null;
        this.fulfillmentMode = this.root.classList.contains('deliveryNow') ? this.#deliveryNow : this.#notDelivered;
        this.paymentTiming = this.root.classList.contains('payNow') ? this.#payNow : this.#unpaid;

        this.productBrowser = null;
        this.productBrowserMobile = null;

        this.itemEditor = null;
        const offcanvasEl = document.getElementById('itemEditOffcanvas');
        const modalEl = document.getElementById('itemEditModal');
        if (offcanvasEl || modalEl) {
            this.itemEditor = new ItemEditor(offcanvasEl, modalEl);
        }

        this.#paymentProcess = new PaymentProcess(this, {
            createIntent: root.dataset.createIntentUrl,
            statusIntent: root.dataset.statusUrl,
            confirmIntent: root.dataset.confirmIntentUrl,
        }, {
            bankTransferEnabled: root.dataset.bankTransferEnabled === 'true',
            manualConfirmEnabled: root.dataset.manualConfirmEnabled === 'true'
        });

        this.bindElements();
        const initialValues = this.bindPickers();
        this.bindEvents();

        this.cart = this.#getItems(this.fulfillmentMode == this.#deliveryNow);
        if (initialValues.customer) {
            this.customerPicker.selectCustomer({
                id: initialValues.customer.id,
                name: initialValues.customer.name,
                phone: initialValues.customer.phone,
                address: initialValues.customer.address,
                kind: initialValues.customer.kind,
                isSystem: initialValues.customer.isSystem
            });
            this.validateQuickCreateOrder();
        } else {
            this.render();
            if (this.cart.length > 0)
                this.validateQuickCreateOrder();
        }
    }

    bindElements() {
        this.alert = document.getElementById('fastSaleAlert');
        this.customerPickerEl = document.getElementById('fastSaleCustomerPicker');
        this.productBrowserEl = document.getElementById('fastSaleProductBrowser');
        this.productBrowserMobileEl = document.getElementById('fastSaleProductBrowserMobile');
        this.cartBody = document.getElementById('fastSaleCartBody');
        this.emptyCart = document.getElementById('fastSaleEmptyCart');
        this.note = document.getElementById('fastSaleNote');
        this.subTotal = document.getElementById('fastSaleSubtotal');
        this.total = document.getElementById('fastSaleTotal');
        this.totalHint = document.getElementById('fastSaleTotalHint');
        this.notDelivered = document.getElementById('fastSaleNotDelivered');
        this.unpaid = document.getElementById('fastSaleUnpaid');
        this.complete = document.getElementById('fastSaleComplete');
        this.customer = document.getElementById('CustomerId');
        this.shippingPhoneNumber = document.getElementById('ShippingPhoneNumber');
        this.shippingAddress = document.getElementById('ShippingAddress');
        this.deliveryNowBtn = document.getElementById('fastSaleDeliverNow');
        this.payNowBtn = document.getElementById('fastSalePayNow');
        this.discountInput = document.getElementById('orderDiscount');
    }
    bindPickers() {
        const initialValues = {};
        if (this.customerPickerEl) {
            this.customerPicker = new CustomerPicker(this.customerPickerEl, {
                allowCreateNew: true
            });
            this.customerPickerEl.addEventListener('select', (event) => {
                this.oldSelectedCustomer = this.selectedCustomer;
                this.selectedCustomer = event.detail?.customer || null;
                this.applyCustomerShippingDefaults();
                this.render();
                this.#validateCustomer();
            });
            this.customerPickerEl.addEventListener('remove', () => {
                this.oldSelectedCustomer = this.selectedCustomer;
                this.selectedCustomer = null;
                this.applyCustomerShippingDefaults();
                this.render();
                this.#validateCustomer();
            });

            const initialCustomer = this.customerPickerEl.dataset;
            if (initialCustomer.id) {
                initialValues.customer = initialCustomer;
            }
        }

        if (this.productBrowserEl) {
            this.productBrowser = new ProductBrowser(
                this.productBrowserEl,
                (product) => this.addOrIncrementProduct(product),
                {
                    colClass: this.productBrowserEl.dataset.colClass,
                    initialShow: true,
                    checkProduct: this.#isValidProduct
                }
            );
            this.productBrowser.init();
        }

        if (this.productBrowserMobileEl) {
            this.productBrowserMobile = new ProductBrowser(
                this.productBrowserMobileEl,
                (product) => this.addOrIncrementProduct(product),
                {
                    colClass: this.productBrowserMobileEl.dataset.colClass,
                    initialShow: true,
                    checkProduct: this.#isValidProduct
                }
            );
            this.productBrowserMobile.init();
        }

        this.bindQuickCustomerForm();

        return initialValues;
    }
    applyCustomerShippingDefaults() {
        if (this.selectedCustomer && !this.isRetailWalkInCustomer()) {
            this.shippingAddress.value = this.selectedCustomer.address ?? '';
            this.shippingPhoneNumber.value = this.selectedCustomer.phone ?? '';
            return;
        }

        if (this.oldSelectedCustomer != null) {
            this.shippingAddress.value = '';
            this.shippingPhoneNumber.value = '';
        }
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
        this.deliveryNowBtn.addEventListener('click', () => this.setFulfillmentMode(this.#deliveryNow));
        this.notDelivered.addEventListener('click', () => this.setFulfillmentMode(this.#notDelivered));
        this.payNowBtn.addEventListener('click', () => this.setPaymentTiming(this.#payNow));
        this.unpaid.addEventListener('click', () => this.setPaymentTiming(this.#unpaid));
        this.complete.addEventListener('click', () => this.completeSale());
        const discountChanged = debounce(() => {
            const discount = DecimalFields.getValue(this.discountInput);
            const subTotal = this.calculateSubtotal();
            if (discount > subTotal) {
                this.showAlert('warning', 'Giảm giá tối đa là ' + DecimalFields.formatCurrencyWithSymbol(subTotal));
                this.discountInput.value = subTotal;
            }
            this.render();
        }, 700);
        this.discountInput.addEventListener('input', discountChanged);
    }

    setFulfillmentMode(mode) {
        this.fulfillmentMode = mode;
        this.cart.forEach(item => {
            item.warehouseId = mode === this.#deliveryNow ? (item.warehouseId || this.resolveInitialWarehouseId(item)) : '';
        });
        this.productBrowser?.reload();
        this.productBrowserMobile?.reload();
        this.render();
    }
    setPaymentTiming(timing) {
        this.paymentTiming = timing;
        this.render();
    }

    addOrIncrementProduct(product) {
        if (!this.#isValidProduct(product)) {
            toast('Hàng hóa không phù hợp', 'Vui lòng chọn hàng hóa khác.', 'warning');
            return;
        }
        product = this.normalizeProduct(product);
        const idx = this.cart.findIndex(item => item.productId === product.id && item.unitPrice == product.unitPrice);
        if (idx >= 0) {
            this.cart[idx] = { ...this.cart[idx], quantity: this.cart[idx].quantity + 1 };
            this.render();
            this.#closeOffcanvas();
            return;
        }

        const warehouseId = this.resolveInitialWarehouseId(product);
        const existing = this.cart.find(item => item.productId === product.id && item.warehouseId === warehouseId);
        const cartItem = {
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
                    this.cart.push(cartItem);
                    this.render();
                },
                onDelete: () => {
                    const i = this.cart.indexOf(cartItem);
                    if (i >= 0) this.cart.splice(i, 1);
                    this.render();
                }
            }, { canRemove: false });
        }
    }

    #closeOffcanvas() {
        const offcanvas = document.getElementById('productBrowserOffcanvas');
        bootstrap.Offcanvas.getOrCreateInstance(offcanvas)?.hide();
    }
    #isValidProduct(product, checkQty = 1) {
        if (!product) return false;

        if (Number(product.availableQty ?? product.quantityAvailable ?? 0) >= checkQty)
            return true;

        return product.availableVendors?.length > 0;
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
        const subTotal = this.calculateSubtotal();
        const total = this.calculateTotal();

        if (subTotal > 0 && this.isRetailWalkInCustomer() && this.paymentTiming == this.#unpaid) {
            this.setPaymentTiming(this.#payNow);
            return;
        }
        if (this.cart.length && total == 0 && this.paymentTiming == this.#unpaid) {
            this.setPaymentTiming(this.#payNow);
            return;
        }
        if (this.cart.some(item => item.quantity > item.quantityAvailable) && this.fulfillmentMode == this.#deliveryNow) {
            this.setFulfillmentMode(this.#notDelivered);
            return;
        }

        this.payNowBtn.disabled = subTotal == 0 && total == 0;
        this.unpaid.disabled = subTotal == 0 || this.isRetailWalkInCustomer();
        this.#togglePaymentTabs();

        this.notDelivered.disabled = this.cart.length === 0;
        this.deliveryNowBtn.disabled = this.cart.length === 0 || this.cart.some(item => item.quantity > item.quantityAvailable);
        this.#toggleDeliveryTabs();

        this.renderCart();

        this.subTotal.textContent = this.formatMoneyWithSymbol(subTotal);
        this.total.textContent = this.formatMoneyWithSymbol(total);
        this.complete.innerHTML = this.getCompleteButtonHtml();
        if (total > 0)
            this.totalHint.textContent = window.SoBangChu?.docSoTien(total) ?? '';
        else
            this.totalHint.textContent = '';

        this.customer.value = this.selectedCustomer?.id ?? '';

        const showShippingInfo = this.cart.length > 0 && this.fulfillmentMode === this.#notDelivered;
        this.shippingAddress.closest('.ship-info').classList.toggle('d-none', !showShippingInfo);

        reparseForm(this.root);
        document.querySelectorAll('.retailWalkinPaymentWarning')?.forEach(warning =>
            warning.classList.toggle('d-none', !this.isRetailWalkInCustomer() || total == 0));
    }
    #toggleDeliveryTabs() {
        if (this.fulfillmentMode == this.#notDelivered) {
            if (this.notDelivered.disabled) {
                this.notDelivered.classList.remove('active');
                document.querySelector(this.notDelivered.getAttribute('data-bs-target'))?.classList.remove('active', 'show');
            }
            else
                bootstrap.Tab.getOrCreateInstance(this.notDelivered).show();
        }
        if (this.fulfillmentMode == this.#deliveryNow)
            if (this.deliveryNowBtn.disabled) {
                this.deliveryNowBtn.classList.remove('active');
                document.querySelector(this.deliveryNowBtn.getAttribute('data-bs-target'))?.classList.remove('active', 'show');
            }
            else
                bootstrap.Tab.getOrCreateInstance(this.deliveryNowBtn).show();
    }
    #togglePaymentTabs() {
        if (this.paymentTiming == this.#unpaid) {
            if (this.unpaid.disabled) {
                this.unpaid.classList.remove('active');
                document.querySelector(this.unpaid.getAttribute('data-bs-target'))?.classList.remove('active', 'show');
            }
            else
                bootstrap.Tab.getOrCreateInstance(this.unpaid).show();
        }
        if (this.paymentTiming == this.#payNow) {
            if (this.payNowBtn.disabled) {
                this.payNowBtn.classList.remove('active');
                document.querySelector(this.payNowBtn.getAttribute('data-bs-target'))?.classList.remove('active', 'show');
            }
            else
                bootstrap.Tab.getOrCreateInstance(this.payNowBtn).show();
        }
    }
    renderCart() {
        this.cartBody.innerHTML = '';
        this.emptyCart.style.display = this.cart.length === 0 ? 'block' : 'none';
        document.querySelector('.warehouse-col')?.classList.toggle('d-none', this.fulfillmentMode != this.#deliveryNow);
        this.cart.forEach((item, index) => {
            const row = document.createElement('tr');
            row.setAttribute('data-unit-measurement', item.unitMeasurement);
            row.setAttribute('data-qty-available', item.quantityAvailable);
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
                <td class="align-middle ${this.fulfillmentMode == this.#deliveryNow ? '' : 'd-none'}">
                    ${this.renderWarehouseSelect(item, index)}
                </td>
                <td class="text-end d-none d-xl-table-cell align-middle">
                    <span class="fw-medium">${DecimalFields.formatQuantity(item.quantity, item.quantityDecimalPlaces ?? 0)}</span>
                </td>
                <td class="text-end align-middle">
                    <span class="text-muted">${DecimalFields.formatCurrencyWithSymbol(item.unitPrice)}</span>
                </td>
                <td class="text-end fw-semibold text-nowrap d-none d-xl-table-cell align-middle">
                    ${this.formatMoneyWithSymbol(item.quantity * item.unitPrice)}
                </td>
                <td class="text-center align-middle">
                    <button type="button" class="btn-table-action danger border-0 bg-transparent shadow-none deleteItem" aria-label="Xóa hàng hóa">
                        <i class="bi bi-trash"></i>
                    </button>
                </td>`;

            //de
            row.querySelector('.deleteItem').addEventListener('click', () => {
                this.cart.splice(index, 1);
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
                            this.render();
                        },
                        onDelete: () => {
                            this.cart.splice(index, 1);
                            this.render();
                        }
                    });
                });
            }

            this.cartBody.appendChild(row);
        });
        DecimalFields.autoWrap(this.cartBody);
    }
    renderWarehouseSelect(item, index) {
        if (this.fulfillmentMode !== this.#deliveryNow) {
            return '';
        }
        const warehouses = item.availableWarehouses || [];
        const options = ['<option value="">Chọn kho</option>'];
        for (const warehouse of warehouses) {
            const selected = warehouse.id === item.warehouseId ? 'selected' : '';
            const quantity = this.formatQuantity(warehouse.quantityOnHand, item.quantityDecimalPlaces);
            options.push(`<option value="${this.escape(warehouse.id)}" ${selected} 
                data-name="${warehouse.name} data-qty-onhand="${warehouse.quantityOnHand}"
                data-qty-available="${warehouse.quantityAvailable}">
                ${this.escape(warehouse.name)} - ${quantity} ${this.escape(item.unitMeasurement)}
            </option>`);
        }

        return `<select class="form-select form-select-sm warehouse-id" data-role="warehouse" name="Items[${index}].WarehouseId" data-val="true" data-val-required="Vui lòng chọn kho hàng">${options.join('')}</select>
        <span class="small text-danger field-validation-valid" data-valmsg-for="Items[${index}].WarehouseId" data-valmsg-replace="true"></span>`;
    }
    #getItems(deliveryNow) {
        const rows = Array.from(this.cartBody.querySelectorAll('tr'));
        return rows.map(row => {
            const quantityDecimalPlaces = Number(row.querySelector('.quantityDecimalPlaces').value) || 0;
            const warehouseSelect = row.querySelector('.warehouse-id');
            const orderItem = {
                productId: row.querySelector('.product-id').value,
                name: row.querySelector('.product-name').textContent,
                pictureUrl: row.querySelector('.product-picture')?.src,
                quantity: parseNumber(DecimalFields.stripFormatting(row.querySelector('.row-qty').value, quantityDecimalPlaces)),
                unitPrice: parseNumber(DecimalFields.stripFormatting(row.querySelector('.row-price').value)),
                quantityDecimalPlaces,
                warehouseId: deliveryNow ? warehouseSelect.value : null,
                unitMeasurement: row.getAttribute('data-unit-measurement'),
                quantityAvailable: Number(row.getAttribute('data-qty-available')),
                availableWarehouses: Array.from(warehouseSelect.options).filter(option => !!option.value).map(option => ({
                    id: option.value,
                    name: option.getAttribute('data-name'),
                    quantityOnHand: Number(option.getAttribute('data-qty-onhand')),
                    quantityAvailable: Number(option.getAttribute('data-qty-available'))
                }))
            }

            return orderItem;
        });
    }

    async completeSale() {
        if (!$(this.root).valid())
            return;

        const isValid = this.validateQuickCreateOrder();
        if (!isValid) return;

        const self = this;

        //payment
        const subTotal = this.calculateSubtotal();
        const discount = DecimalFields.getValue(this.discountInput);
        const total = this.calculateTotal();

        if (this.paymentTiming != this.#payNow || total == 0) {
            if (!await confirm('Tạo đơn hàng', 'Xác nhận tạo đơn hàng'))
                return;

            showPageLoading();
            this.root.addEventListener('formdata', function onFormData(e) {
                self.root.removeEventListener('formdata', onFormData);
                prepareSubmitFormData(e.formData);
            });

            this.complete.disabled = true;
            this.root.submit();
            return;
        }

        if (!await confirm('Tạo đơn hàng', 'Xác nhận tạo đơn hàng và thanh toán'))
            return;

        showPageLoading();

        const formData = new FormData(this.root);
        prepareSubmitFormData(formData);
        formData.set('returnJson', true);

        const createOrderResult = await this.postJson(this.root.action, formData);
        if (!createOrderResult.success) {
            hidePageLoading();
            return;
        }

        const orderInfo = createOrderResult.data;

        if (orderInfo.subTotal != subTotal || orderInfo.total != total || orderInfo.discount != discount) {
            this.showAlert('warning', 'Thông tin đơn hàng không trùng khớp');
            location = `/Order/Details/${orderInfo.orderId}`;
            return;
        }

        hidePageLoading();
        const paymentResult = await this.#paymentProcess.startPayment({
            orderId: orderInfo.orderId,
            orderCode: orderInfo.orderCode,
            subTotal: orderInfo.subTotal,
            discount: orderInfo.discount,
            total: orderInfo.orderTotal,
            canChangePaidAmount: !this.isRetailWalkInCustomer() || this.fulfillmentMode != this.#deliveryNow,
            customer: {
                id: this.selectedCustomer.id,
                isRetailWalkIn: this.isRetailWalkInCustomer()
            }
        });
        if (!paymentResult.success) {
            // modal is closed by user
            showPageLoading();
            this.showAlert('warning', 'Đơn hàng chưa hoàn thành do chưa thanh toán')
            this.#redirectToOrderPage(orderInfo.orderId);
            return;
        }

        showPageLoading();
        const completePaymentPayload = {
            orderId: orderInfo.orderId,
            paidAmount: paymentResult.amount,
            paymentIntentId: paymentResult.paymentIntentId
        };
        const completeResult = await this.postJson('/Order/CompleteQuickCreateOrderPayment', completePaymentPayload);
        if (!completeResult.success) {
            this.showAlert('warning', 'Phát sinh lỗi khi hoàn thanh đơn');
            this.#redirectToOrderPage(orderInfo.orderId);
            return;
        }
        this.showAlert('success', this.fulfillmentMode == this.#deliveryNow ? 'Hoàn tất đơn hàng' : "Đơn hàng đã được tạo và thanh toán");
        this.#redirectToOrderPage(orderInfo.orderId);

        function prepareSubmitFormData(formData) {
            if (self.fulfillmentMode == self.#deliveryNow) {
                formData.set('deliveryNow', true);
                formData.set(self.shippingAddress.name, self.selectedCustomer.address);
                formData.set(self.shippingPhoneNumber.name, self.selectedCustomer.phone);
            }
        }
    }
    #redirectToOrderPage(orderId) {
        location = `/Order/Details/${orderId}`
    }

    validateQuickCreateOrder() {
        const validation = this.validateSaleInput();
        if (validation) {
            this.showAlert('warning', validation);
            return false;
        }

        if (this.paymentTiming == this.#unpaid && this.isRetailWalkInCustomer()) {
            this.showAlert('warning', 'Khách bán lẻ cần thanh toán hoặc đặt cọc.');
            return false;
        }

        return true;
    }
    #validateCustomer() {
        const customer = this.selectedCustomer;
        const customerValidator = document.querySelector('[data-valmsg-for="CustomerId"]');
        const shippingPhoneNumberValidator = document.querySelector('[data-valmsg-for="ShippingPhoneNumber"]');
        const shippingAddressValidator = document.querySelector('[data-valmsg-for="ShippingAddress"]');

        customerValidator.textContent = customer ? '' : 'Vui lòng chọn khách hàng.';
        shippingPhoneNumberValidator.textContent = this.shippingPhoneNumber.value ? '' : this.shippingPhoneNumber.getAttribute('data-val-required');
        shippingAddressValidator.textContent = this.shippingAddress.value ? '' : this.shippingAddress.getAttribute('data-val-required');

        return !!customer;
    }
    validateSaleInput() {
        if (!this.getSelectedCustomerId()) return 'Vui lòng chọn khách hàng.';
        if (this.fulfillmentMode === 'deliverNow' && this.cart.some(item => !item.warehouseId)) return 'Vui lòng chọn kho cho từng mặt hàng.';
        if (this.cart.length === 0) return 'Vui lòng thêm hàng hóa.';
        if (this.cart.some(item => item.quantity <= 0)) return 'Số lượng phải lớn hơn 0.';
        if (this.calculateTotal() <= 0) return 'Tổng tiền phải lớn hơn 0.';
        if (this.fulfillmentMode === this.#notDelivered) {
            if (!this.shippingPhoneNumber.value.trim()) return 'Vui lòng nhập số điện thoại giao hàng.';
            if (!this.shippingAddress.value.trim()) return 'Vui lòng nhập địa chỉ giao hàng.';
        }
        return null;
    }

    buildSalePayload(paymentAmount, discount, paymentIntentId) {
        return {
            customerId: this.getSelectedCustomerId(),
            items: this.cart.map(item => ({
                productId: item.productId,
                warehouseId: this.fulfillmentMode === this.#deliveryNow ? item.warehouseId : null,
                quantity: item.quantity,
                unitPrice: item.unitPrice
            })),
            shippingAddress: this.shippingAddress.value,
            shippingPhoneNumber: this.shippingPhoneNumber.value,
            orderDiscount: discount || 0,
            note: this.note.value,
            deliveryNow: this.fulfillmentMode === this.#deliveryNow,
            payNow: this.paymentTiming === this.#payNow,
            paidAmount: paymentAmount,
            paymentIntentId
        };
    }

    resolveSaleUrl() {
        if (this.paymentTiming === this.#unpaid)
            return this.urls.createUnpaidSale;

        return this.#paymentProcess.isBank()
            ? this.urls.createBankSale
            : this.urls.createCashSale;
    }

    getSelectedCustomerId() {
        return this.selectedCustomer?.id || '';
    }

    resolveInitialWarehouseId(product) {
        if (this.fulfillmentMode !== this.#deliveryNow) return '';

        const warehouses = product.availableWarehouses || [];
        for (let warehouseId of warehouses) {
            if (this.cart.some(item => item.warehouseId == warehouseId))
                return warehouseId;
        }

        return warehouses[0]?.id || '';
    }

    getHeaderWarehouseId() {
        if (this.fulfillmentMode !== this.#deliveryNow)
            return;
        return this.cart.find(item => item.warehouseId)?.warehouseId;
    }

    async postJson(url, payload) {
        const result = await apiPost(url, payload);
        return result;
    }

    calculateSubtotal() {
        return this.cart.reduce((sum, item) => sum + item.quantity * item.unitPrice, 0);
    }

    calculateTotal() {
        return Math.max(0, this.calculateSubtotal() - DecimalFields.getValue(this.discountInput));
    }

    getCompleteButtonHtml() {
        const total = this.calculateTotal();
        if (total == 0 || this.paymentTiming == this.#unpaid)
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
    #bankMethod = Symbol('bank');
    #cashMethod = Symbol('cash');

    #subTotal;
    #discount;
    #total;
    #amount;
    #canChangePaidAmount;

    #orderId;
    #orderCode;
    #customer;

    #paymentMethod;
    #paymentIntent;
    #paymentIntentConfirmed;

    #reference;
    #config;

    //intent validity
    #saleInputVersion;
    #pollRequestSeq = 0;
    #statusTimer;
    #StatusPollingIntervalMs = 3000;

    #urls;

    #_promiseResolve;
    #_promiseReject;

    constructor(reference, urls, config) {
        this.#reference = reference;
        this.#config = Object.assign({
            bankTransferEnabled: true,
            manualConfirmEnabled: false
        }, config);
        this.#urls = urls;

        this.root = document.getElementById('paymentModal');
        this.modal = bootstrap.Modal.getOrCreateInstance(this.root);

        this.form = document.getElementById('paymentForm');
        this.orderId = this.form.querySelector('#OrderId');
        this.deliveryWaiting = this.form.querySelector('#DeliveryWaiting');
        this.paymentIntentId = this.form.querySelector('#paymentIntentId');
        this.paymentSubTotal = this.form.querySelector('#paymentSubTotal');
        this.paymentDiscount = this.form.querySelector('#paymentDiscount');
        this.paymentTotal = this.form.querySelector('#paymentTotal');
        this.paymentTotalHint = this.form.querySelector('#paymentTotalHint');
        this.skipPaymentBtn = this.form.querySelector('#paymentSkip');
        this.paidAmountInput = this.form.querySelector('#paidAmount');
        this.debtAmount = this.form.querySelector('#debtAmount');
        this.paymentProcessContainer = this.form.querySelector('#paymentProcessContainer');
        this.paymentMethodContainer = this.form.querySelector('#paymentMethodContainer');
        this.submitBtn = this.form.querySelector('[type=submit]');
        this.moneyReceivedBtn = this.form.querySelector('#moneyReceived');
        this.qrPanel = this.form.querySelector('#fastSaleQrPanel');
        this.createQr = this.form.querySelector('#fastSaleCreateQr');
        this.cashMethod = this.form.querySelector('#fastSaleCashMethod');
        this.bankMethod = this.form.querySelector('#fastSaleBankMethod');
        this.qrImage = document.getElementById('fastSaleQrImage');
        this.reference = document.getElementById('fastSaleReference');
        this.qrAmount = document.getElementById('fastSaleQrAmount');
        this.qrStatus = document.getElementById('fastSaleQrStatus');
        this.qrExpires = document.getElementById('fastSaleQrExpires');

        if (!this.#config.bankTransferEnabled) {
            this.bankMethod.disabled = true;
            this.cashMethod.disabled = false;
            this.paymentMethodContainer.classList.add('d-none');

            this.createQr.classList.add('d-none');
            this.createQr.disabled = true;
        }

        this.#paymentIntent = null;
        this.#paymentIntentConfirmed = false;
        this.#saleInputVersion = 0;
        this.#pollRequestSeq = 0;
    }

    isBank() {
        return this.#paymentMethod == this.#bankMethod;
    }
    isPending() {
        return this.isBank() && !this.#paymentIntentConfirmed;
    }
    isCash() {
        return this.#paymentMethod == this.#cashMethod;
    }

    async startPayment({ orderId, orderCode,
        subTotal, discount, total,
        canChangePaidAmount, customer,
        deliveryWaiting
    }) {
        if (subTotal <= 0)
            throw new Error('Số tiền thanh toán không đúng');

        this.#orderId = orderId;
        this.#orderCode = orderCode;
        this.#customer = customer;

        this.#subTotal = subTotal || 0;
        this.#discount = discount || 0;
        this.#total = total || 0;

        this.#canChangePaidAmount = canChangePaidAmount;

        this.#resetPaymentIntent();
        this.#saleInputVersion = 0;
        this.#pollRequestSeq = 0;
        this.#amount = 0;
        this.#setPaymentMethod(this.#cashMethod);
        this.paidAmountInput.value = 0;

        const self = this;

        //event listeners
        const onAmountChanged = this.#canChangePaidAmount ? debounce(async () => {
            const total = this.#calculateTotal();
            const amount = DecimalFields.getValue(this.paidAmountInput);
            const debt = Math.max(0, total - amount);
            this.debtAmount.classList.toggle('d-none', debt == 0);
            if (debt > 0)
                this.debtAmount.textContent = `Còn nợ ${DecimalFields.formatCurrencyWithSymbol(debt)}`;
            if (this.isBank())
                await this.#createPaymentQrCode();
            else
                this.#resetPaymentIntent();
            this.#amount = amount;
        }, 700) : Function.prototype;

        const onPaymentSubmit = e => {
            e.preventDefault();
            if (!this.#isPaymentFormValid())
                return;

            const isValid = this.#reference.validateQuickCreateOrder();
            if (!isValid) return;

            this.#endPayment(true, 0);
        }

        const createQr = async () => {
            if (!this.#config.bankTransferEnabled)
                return;
            await this.#createPaymentQrCode();
        }

        const confirmMoneyReceived = async () => {
            if (!this.#isPaymentFormValid())
                return;

            //hide modal
            this.root.classList.add('d-none');

            const confirmed = await confirm('Đã nhận tiền', `Bạn xác nhận đã nhận đủ ${DecimalFields.formatCurrencyWithSymbol(this.#amount)}`);
            if (!confirmed) {
                this.root.classList.remove('d-none');
                return;
            }

            const isValid = this.#reference.validateQuickCreateOrder();
            if (!isValid) return;

            if (this.isBank()) {
                const success = await this.#confirmPaymentIntent();
                if (!success) {
                    this.#endPayment(false);
                    return;
                }
            }

            this.#endPayment(true, this.#amount);
        }

        const onCloseModal = async () => {
            this.root.classList.add('d-none');
            let confirmed;
            if (this.#reference.isRetailWalkInCustomer()) {
                confirmed = await confirm('Bỏ qua thanh toán', 'Đơn hàng chưa thể hoàn thành nếu bạn chưa thanh toán, bạn có muốn tiếp tục?');
            } else {
                confirmed = await confirm('Bỏ qua thanh toán', 'Xác nhận sẽ thanh toán đơn hàng sau?');
            }
            if (!confirmed) {
                this.root.classList.remove('d-none');
                return;
            }
            this.modal.hide();
        };

        const setCashPayment = () => this.#setPaymentMethod(this.#cashMethod);
        const setBankPayment = () => this.#setPaymentMethod(this.#bankMethod);

        const promise = new Promise((resolve, reject) => {
            this.#_promiseResolve = resolve;
            this.#_promiseReject = reject;

            this.createQr.addEventListener('click', createQr);
            this.moneyReceivedBtn.addEventListener('click', confirmMoneyReceived);
            this.paidAmountInput.addEventListener('input', onAmountChanged);
            this.form.addEventListener('submit', onPaymentSubmit);
            this.cashMethod.addEventListener('click', setCashPayment);
            this.bankMethod.addEventListener('click', setBankPayment);
            this.skipPaymentBtn.addEventListener('click', onCloseModal);

            this.root.addEventListener('show.bs.modal', function onShow() {
                self.root.removeEventListener('show.bs.modal', onShow);
                self.render();
            });

            this.root.addEventListener('hidden.bs.modal', function onHidden(e) {
                self.root.removeEventListener('hidden.bs.modal', onHidden);
                self.paidAmountInput.removeEventListener('input', onAmountChanged);
                self.createQr.removeEventListener('click', createQr);
                self.moneyReceivedBtn.removeEventListener('click', confirmMoneyReceived);
                self.form.removeEventListener('submit', this.onPaymentSubmit);
                self.cashMethod.removeEventListener('click', setCashPayment);
                self.bankMethod.removeEventListener('click', setBankPayment);
                self.skipPaymentBtn.removeEventListener('click', onCloseModal);
                self.root.classList.remove('d-none');
                self.debtAmount.classList.add('d-none');

                self.#stopIntentPolling();

                resolve({ success: false });

                self.#_promiseResolve = null;
                self.#_promiseReject = null;
            });
        });

        this.modal.show();

        return promise;
    }
    #endPayment(success, amount) {
        if (!this.#_promiseResolve) {
            throw new Error('Thao tác không hợp lệ');
        }
        if (success) {
            this.#_promiseResolve({
                success,
                amount: amount !== null ? amount : this.#amount,
                discount: this.#discount,
                paymentIntentId: this.#paymentIntent?.id
            });
        } else {
            this.#_promiseResolve({
                success: false
            });
        }

        this.modal.hide();
    }

    async render() {
        let discount = this.#discount;
        if (discount > this.#subTotal)
            discount = this.#subTotal;
        const total = this.#calculateTotal();
        if (total == 0) {
            if (!this.isCash()) {
                await this.#setPaymentMethod(this.#cashMethod);
                this.render();
                return;
            }
        }
        this.bankMethod.disabled = total == 0;

        this.paymentSubTotal.textContent = DecimalFields.formatCurrency(this.#subTotal);
        this.paymentDiscount.textContent = DecimalFields.formatCurrency(this.#discount);
        this.paymentTotal.textContent = DecimalFields.formatCurrencyWithSymbol(total);
        this.paymentTotalHint.classList.toggle('d-none', total <= 0);
        if (total > 0) {
            this.paymentTotalHint.textContent = window.SoBangChu?.docSoTien(total) ?? '';
        }

        this.paidAmountInput.value = DecimalFields.formatCurrency(total);
        this.paidAmountInput.disabled = total == 0 || !this.#canChangePaidAmount;
        this.paidAmountInput.closest('div.d-flex').classList.toggle('d-none', !this.#canChangePaidAmount);

        this.paymentProcessContainer.classList.toggle('d-none', total == 0);

        this.#amount = total;

        if (total > 0 && this.#canChangePaidAmount) {
            this.paidAmountInput.setAttribute('data-val-range', `Số tiền thanh toán phải lớn hơn 0 và nhỏ hơn ${DecimalFields.formatCurrencyWithSymbol(total)}`);
            this.paidAmountInput.setAttribute('data-val-range-max', total);
            if (this.#customer.isRetailWalkIn) {
                this.paidAmountInput.setAttribute('data-val-range-min', 0.001);
            }
        } else {
            this.paidAmountInput.removeAttribute('data-val-range');
            this.paidAmountInput.removeAttribute('data-val-range-max');
            this.paidAmountInput.removeAttribute('data-val-range-min');
        }

        reparseForm(this.form);

        this.qrPanel.classList.toggle('d-none', !this.isBank());

        this.submitBtn.disabled = !this.#isPaymentFormValid();
        this.submitBtn.classList.toggle('d-none', total > 0);
        this.moneyReceivedBtn.classList.toggle('d-none', total == 0);
        this.moneyReceivedBtn.disabled = this.isBank() && (!this.#paymentIntent || this.#paymentIntentConfirmed
            || !this.#config.manualConfirmEnabled || !this.#isIntentPending(this.#paymentIntent));
    }

    #calculateTotal() {
        return Math.max(this.#subTotal - this.#discount);
    }

    async #setPaymentMethod(method) {
        if (method == this.#bankMethod && !this.#config.bankTransferEnabled)
            return;

        this.#paymentMethod = method;
        this.#resetPaymentIntent();
        this.cashMethod.classList.toggle('active', this.isCash());
        this.bankMethod.classList.toggle('active', this.isBank());
        this.qrPanel.classList.toggle('d-none', !this.isBank())
        if (this.isBank())
            await this.#createPaymentQrCode();
        else {
            this.qrImage.removeAttribute('src');
            this.reference.textContent = '';
            this.qrAmount.textContent = '';
            this.qrStatus.textContent = '';
            this.qrExpires.textContent = '';

            this.#resetPaymentIntent();
        }
    }
    async #createPaymentQrCode() {
        if (!this.#isPaymentFormValid())
            return;
        const amount = DecimalFields.getValue(this.paidAmountInput);
        if (amount !== Math.trunc(amount)) {
            this.#reference.showAlert('warning', 'Số tiền phải là số nguyên.');
            return;
        }
        this.#resetPaymentIntent();
        await this.#createPaymentIntent(amount);

        this.moneyReceivedBtn.disabled = !this.#paymentIntent || this.#paymentIntentConfirmed
            || !this.#config.manualConfirmEnabled || !this.#isIntentPending(this.#paymentIntent);

        this.#displayPaymentIntentStatus();
    }
    #displayPaymentIntentStatus(status, expiredAt) {
        if (!this.isBank() || !this.#paymentIntent)
            return;
        this.qrImage.src = this.#paymentIntent.qrImageUrl || '';
        this.reference.textContent = this.#paymentIntent.referenceCode || '';
        this.qrAmount.textContent = DecimalFields.formatCurrencyWithSymbol(this.#paymentIntent.amount);
        this.qrStatus.textContent = status ? status : '...';
        this.qrExpires.textContent = expiredAt ? expiredAt : '';
    }
    #isPaymentFormValid() {
        return $(this.form).valid();
    }
    async #createPaymentIntent(amount) {
        if (!this.#isPaymentFormValid())
            return;

        const saleInputVersion = this.#saleInputVersion;
        const response = await this.#reference.postJson(this.#urls.createIntent, {
            customerId: this.#customer.id,
            amount,
            note: this.#getPaymentNote()
        });
        if (this.#saleInputVersion !== saleInputVersion)
            return;

        if (!response.success) {
            return;
        }

        this.#paymentIntent = response.intent;
        this.#paymentIntentConfirmed = false;
        this.#startIntentPolling();
    }
    #getPaymentNote() {
        return `Thanh toán đơn hàng ${this.#orderCode}`;
    }
    async #confirmPaymentIntent() {
        if (!this.#paymentIntent) return;

        const intentId = this.#paymentIntent.id;
        const saleInputVersion = this.#saleInputVersion;
        const response = await this.#reference.postJson(this.#urls.confirmIntent, {
            intentId
        });
        if (!this.#paymentIntent || this.#paymentIntent.id !== intentId || this.#saleInputVersion !== saleInputVersion) return;

        if (!response.success) {
            return;
        }
        if (response.intent?.id !== intentId) return;

        this.#paymentIntent = response.intent;
        this.#displayPaymentIntentStatus(response.status, response.expiresAt);

        this.#paymentIntentConfirmed = this.#paymentIntent.isConfirmed;
        this.#pollRequestSeq += 1;
        this.#stopIntentPolling();
        this.#reference.showAlert('success', 'Đã xác nhận tiền vào tài khoản.');
        return true;
    }
    #startIntentPolling() {
        this.#stopIntentPolling();
        if (!this.#paymentIntent || !this.#isIntentPending(this.#paymentIntent)) return;

        const self = this;
        const interval = this.#StatusPollingIntervalMs;
        checkIntentStatus();
        async function checkIntentStatus() {
            await self.#refreshIntentStatus();
            self.#statusTimer = setTimeout(checkIntentStatus, interval);
        }
    }
    #stopIntentPolling() {
        if (!this.#statusTimer) return;

        clearTimeout(this.#statusTimer);
        this.#statusTimer = null;
    }
    async #refreshIntentStatus() {
        if (!this.#paymentIntent) {
            this.#stopIntentPolling();
            return;
        }

        const intentId = this.#paymentIntent.id;
        const saleInputVersion = this.#saleInputVersion;
        const requestSeq = this.#pollRequestSeq + 1;
        this.#pollRequestSeq = requestSeq;
        const params = new URLSearchParams({ intentId });
        let data;

        try {
            const response = await fetch(`${this.#urls.statusIntent}?${params.toString()}`);
            data = await response.json();
        } catch {
            if (!this.#isCurrentPollingIntent(intentId, saleInputVersion, requestSeq)) return;

            this.#reference.showAlert('error', 'Không thể cập nhật trạng thái QR.');
            this.#resetPaymentIntent();
            return;
        }

        if (!this.#isCurrentPollingIntent(intentId, saleInputVersion, requestSeq)) return;

        if (!data?.success) {
            this.#reference.showAlert('error', data?.message);
            this.#resetPaymentIntent();
            return;
        }
        if (data.intent?.id !== intentId) return;

        this.#paymentIntent = data.intent;
        this.#displayPaymentIntentStatus(data.status, data.expiresAt);
        this.#paymentIntentConfirmed = this.#paymentIntent.isConfirmed;
        if (this.#paymentIntentConfirmed) {
            this.#reference.showAlert('success', 'Đã nhận tiền vào tài khoản.');
            this.#stopIntentPolling();

            this.#endPayment(true, this.#amount);
        }

        if (this.#isIntentExpiredOrCancelled(this.#paymentIntent)) {
            this.#reference.showAlert('warning', 'QR đã hết hạn hoặc đã hủy. Vui lòng tạo QR mới.');
            this.#stopIntentPolling();
        }

        if (!this.#isIntentPending(this.#paymentIntent)) this.#stopIntentPolling();
    }
    #resetPaymentIntent() {
        this.#saleInputVersion += 1;
        this.#pollRequestSeq += 1;
        this.#stopIntentPolling();
        this.#paymentIntent = null;
        this.#paymentIntentConfirmed = false;
    }
    #isIntentPending(intent) {
        return intent.isPending;
    }
    #isCurrentPollingIntent(intentId, saleInputVersion, requestSeq) {
        return this.#paymentIntent
            && this.#paymentIntent.id === intentId
            && this.#saleInputVersion === saleInputVersion
            && this.#pollRequestSeq === requestSeq;
    }
    #isIntentExpiredOrCancelled(intent) {
        return intent.isCancelled || intent.isExpired;
    }
}

const root = document.getElementById('fastSaleApp');
if (root) new FastSale(root);

class FastSale {
    constructor(root) {
        this.root = root;
        this.urls = {
            searchProducts: root.dataset.searchProductsUrl,
            createIntent: root.dataset.createIntentUrl,
            confirmIntent: root.dataset.confirmIntentUrl,
            createCashSale: root.dataset.createCashSaleUrl,
            createBankSale: root.dataset.createBankSaleUrl
        };
        this.bankTransferEnabled = root.dataset.bankTransferEnabled === 'true';
        this.manualConfirmEnabled = root.dataset.manualConfirmEnabled === 'true';
        this.cart = [];
        this.paymentMethod = 'cash';
        this.paymentIntent = null;
        this.paymentIntentConfirmed = false;
        this.searchTimer = null;

        this.bindElements();
        this.bindEvents();
        this.searchProducts();
        this.render();
    }

    bindElements() {
        this.alert = document.getElementById('fastSaleAlert');
        this.warehouse = document.getElementById('fastSaleWarehouse');
        this.customer = document.getElementById('fastSaleCustomer');
        this.searchInput = document.getElementById('fastSaleProductSearch');
        this.products = document.getElementById('fastSaleProducts');
        this.cartBody = document.getElementById('fastSaleCartBody');
        this.emptyCart = document.getElementById('fastSaleEmptyCart');
        this.discount = document.getElementById('fastSaleDiscount');
        this.note = document.getElementById('fastSaleNote');
        this.subtotal = document.getElementById('fastSaleSubtotal');
        this.total = document.getElementById('fastSaleTotal');
        this.cashMethod = document.getElementById('fastSaleCashMethod');
        this.bankMethod = document.getElementById('fastSaleBankMethod');
        this.qrPanel = document.getElementById('fastSaleQrPanel');
        this.qrImage = document.getElementById('fastSaleQrImage');
        this.reference = document.getElementById('fastSaleReference');
        this.qrAmount = document.getElementById('fastSaleQrAmount');
        this.createQr = document.getElementById('fastSaleCreateQr');
        this.confirmQr = document.getElementById('fastSaleConfirmQr');
        this.complete = document.getElementById('fastSaleComplete');
        this.clearCart = document.getElementById('fastSaleClearCart');
    }

    bindEvents() {
        this.searchInput.addEventListener('input', () => {
            window.clearTimeout(this.searchTimer);
            this.searchTimer = window.setTimeout(() => this.searchProducts(), 250);
        });
        this.warehouse.addEventListener('change', () => {
            this.paymentIntent = null;
            this.paymentIntentConfirmed = false;
            this.searchProducts();
            this.render();
        });
        this.discount.addEventListener('input', () => {
            this.paymentIntent = null;
            this.paymentIntentConfirmed = false;
            this.render();
        });
        this.cashMethod.addEventListener('click', () => this.setPaymentMethod('cash'));
        this.bankMethod.addEventListener('click', () => this.setPaymentMethod('bank'));
        this.createQr.addEventListener('click', () => this.createPaymentIntent());
        this.confirmQr.addEventListener('click', () => this.confirmPaymentIntent());
        this.complete.addEventListener('click', () => this.completeSale());
        this.clearCart.addEventListener('click', () => {
            this.cart = [];
            this.paymentIntent = null;
            this.paymentIntentConfirmed = false;
            this.render();
        });
    }

    setPaymentMethod(method) {
        if (method === 'bank' && !this.bankTransferEnabled) return;
        this.paymentMethod = method;
        this.paymentIntent = null;
        this.paymentIntentConfirmed = false;
        this.cashMethod.classList.toggle('active', method === 'cash');
        this.bankMethod.classList.toggle('active', method === 'bank');
        this.render();
    }

    async searchProducts() {
        const params = new URLSearchParams();
        if (this.searchInput.value.trim()) params.set('keywords', this.searchInput.value.trim());
        if (this.warehouse.value) params.set('warehouseId', this.warehouse.value);

        const response = await fetch(`${this.urls.searchProducts}?${params.toString()}`);
        const data = await response.json();
        this.renderProducts(data.items || []);
    }

    renderProducts(items) {
        this.products.innerHTML = '';
        if (items.length === 0) {
            this.products.innerHTML = '<div class="text-muted text-center py-3">Không có hàng hóa.</div>';
            return;
        }

        for (const item of items.slice(0, 30)) {
            const row = document.createElement('div');
            row.className = 'fast-sale-product';
            row.innerHTML = `
                ${item.pictureUrl ? `<img class="fast-sale-product-image" src="${this.escape(item.pictureUrl)}" alt="">` : '<div class="fast-sale-product-image d-flex align-items-center justify-content-center"><i class="bi bi-image text-muted"></i></div>'}
                <div class="min-w-0">
                    <div class="fw-semibold text-truncate">${this.escape(item.name)}</div>
                    <div class="small text-muted">${this.formatMoneyWithSymbol(item.unitPrice)} · Còn ${this.formatQuantity(item.quantityAvailable, item.quantityDecimalPlaces)}</div>
                </div>
                <button type="button" class="btn btn-sm btn-primary" title="Thêm">
                    <i class="bi bi-plus-lg"></i>
                </button>`;
            row.querySelector('button').addEventListener('click', () => this.addItem(item));
            this.products.appendChild(row);
        }
    }

    addItem(product) {
        if (!this.warehouse.value) {
            this.showAlert('warning', 'Vui lòng chọn kho.');
            return;
        }

        const existing = this.cart.find(item => item.productId === product.id);
        if (existing) {
            existing.quantity += 1;
        } else {
            this.cart.push({
                productId: product.id,
                name: product.name,
                quantity: 1,
                unitPrice: Number(product.unitPrice || 0),
                quantityDecimalPlaces: Number(product.quantityDecimalPlaces || 0)
            });
        }

        this.paymentIntent = null;
        this.paymentIntentConfirmed = false;
        this.render();
    }

    render() {
        this.renderCart();
        const subtotal = this.calculateSubtotal();
        const discount = this.getDiscount();
        const total = Math.max(0, subtotal - discount);

        this.subtotal.textContent = this.formatMoneyWithSymbol(subtotal);
        this.total.textContent = this.formatMoneyWithSymbol(total);
        this.qrPanel.classList.toggle('visible', this.paymentMethod === 'bank');
        this.createQr.disabled = this.paymentMethod !== 'bank' || total <= 0 || this.cart.length === 0;
        this.confirmQr.disabled = !this.paymentIntent || this.paymentIntentConfirmed || !this.manualConfirmEnabled;
        this.complete.disabled = this.cart.length === 0 || total <= 0 || (this.paymentMethod === 'bank' && !this.paymentIntentConfirmed);

        if (!this.paymentIntent) {
            this.qrImage.removeAttribute('src');
            this.reference.textContent = '';
            this.qrAmount.textContent = '';
        }
    }

    renderCart() {
        this.cartBody.innerHTML = '';
        this.emptyCart.style.display = this.cart.length === 0 ? 'block' : 'none';

        this.cart.forEach((item, index) => {
            const row = document.createElement('tr');
            row.innerHTML = `
                <td class="ps-3 fw-medium">${this.escape(item.name)}</td>
                <td class="text-center" style="width: 110px;">
                    <input class="form-control form-control-sm text-end" value="${this.formatQuantity(item.quantity, item.quantityDecimalPlaces)}" data-decimal="quantity" data-decimals="${item.quantityDecimalPlaces}"/>
                </td>
                <td class="text-end" style="width: 140px;">
                    <input class="form-control form-control-sm text-end" value="${this.formatMoney(item.unitPrice)}" data-decimal="currency" />
                </td>
                <td class="text-end fw-semibold text-nowrap">${this.formatMoneyWithSymbol(item.quantity * item.unitPrice)}</td>
                <td class="text-end pe-3" style="width: 48px;">
                    <button type="button" class="btn btn-sm btn-light" title="Xóa"><i class="bi bi-x-lg"></i></button>
                </td>`;


            const quantityInput = row.querySelectorAll('input')[0];
            const priceInput = row.querySelectorAll('input')[1];

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

            row.querySelector('button').addEventListener('click', () => {
                this.cart.splice(index, 1);
                this.paymentIntent = null;
                this.paymentIntentConfirmed = false;
                this.render();
            });

            this.cartBody.appendChild(row);
        });
        DecimalFields.autoWrap(this.cartBody);
    }

    async createPaymentIntent() {
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

        const response = await this.postJson(this.urls.createIntent, {
            customerId: this.customer.value,
            amount: total,
            note: this.note.value
        });
        if (!response.success) {
            this.showAlert('danger', response.message);
            return;
        }

        this.paymentIntent = response.intent;
        this.paymentIntentConfirmed = false;
        this.qrImage.src = this.paymentIntent.qrImageUrl;
        this.reference.textContent = this.paymentIntent.referenceCode;
        this.qrAmount.textContent = this.formatMoneyWithSymbol(this.paymentIntent.amount);
        this.showAlert('success', 'Đã tạo QR chuyển khoản.');
        this.render();
    }

    async confirmPaymentIntent() {
        if (!this.paymentIntent) return;

        const response = await this.postJson(this.urls.confirmIntent, {
            intentId: this.paymentIntent.id,
            note: this.note.value
        });
        if (!response.success) {
            this.showAlert('danger', response.message);
            return;
        }

        this.paymentIntent = response.intent;
        this.paymentIntentConfirmed = true;
        this.showAlert('success', 'Đã xác nhận tiền vào tài khoản.');
        this.render();
    }

    async completeSale() {
        const validation = this.validateSaleInput();
        if (validation) {
            this.showAlert('warning', validation);
            return;
        }

        if (this.paymentMethod === 'bank' && !this.paymentIntentConfirmed) {
            this.showAlert('warning', 'Chuyển khoản chưa được xác nhận.');
            return;
        }

        const payload = this.buildSalePayload();
        const url = this.paymentMethod === 'bank' ? this.urls.createBankSale : this.urls.createCashSale;
        if (this.paymentMethod === 'bank') payload.paymentIntentId = this.paymentIntent.id;

        this.complete.disabled = true;
        const response = await this.postJson(url, payload);
        this.complete.disabled = false;

        if (!response.success) {
            this.showAlert('danger', response.message);
            return;
        }

        this.showAlert('success', 'Đã hoàn tất bán hàng.');
        if (response.orderUrl) {
            window.setTimeout(() => { window.location.href = response.orderUrl; }, 500);
        }
    }

    validateSaleInput() {
        if (!this.customer.value) return 'Vui lòng chọn khách hàng.';
        if (!this.warehouse.value) return 'Vui lòng chọn kho.';
        if (this.cart.length === 0) return 'Vui lòng thêm hàng hóa.';
        if (this.cart.some(item => item.quantity <= 0)) return 'Số lượng phải lớn hơn 0.';
        if (this.calculateTotal() <= 0) return 'Tổng thu phải lớn hơn 0.';
        return null;
    }

    buildSalePayload() {
        const total = this.calculateTotal();
        return {
            customerId: this.customer.value,
            warehouseId: this.warehouse.value,
            items: this.cart.map(item => ({
                productId: item.productId,
                quantity: item.quantity,
                unitPrice: item.unitPrice
            })),
            orderDiscount: this.getDiscount(),
            note: this.note.value,
            paidAmount: total
        };
    }

    async postJson(url, payload) {
        const response = await fetch(url, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });
        return await response.json();
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

    parseNumber(value) {
        const normalized = String(value || '').replace(/\./g, '').replace(',', '.').replace(/[^\d.]/g, '');
        const parsed = Number(normalized);
        return Number.isFinite(parsed) ? parsed : 0;
    }

    formatMoney(value) {
        return DecimalFields.formatCurrency(value || 0);
    }
    formatMoneyWithSymbol(value) {
        return DecimalFields.formatCurrencyWithSymbol(value || 0);
    }

    formatQuantity(value, decimals) {
        return new Intl.NumberFormat('vi-VN', {
            minimumFractionDigits: 0,
            maximumFractionDigits: decimals || 0
        }).format(value || 0);
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

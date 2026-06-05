export default class ItemEditOffcanvas {
    #offcanvasEl;
    #bsOffcanvas;
    #callbacks = {};
    #currentItem = null;

    #nameEl;
    #pictureEl;
    #priceLabelEl;
    #qtyInput;
    #priceInput;
    #totalEl;
    #decrementBtn;
    #incrementBtn;
    #applyBtn;
    #deleteBtn;

    constructor(offcanvasEl) {
        this.#offcanvasEl = offcanvasEl;
        this.#bsOffcanvas = bootstrap.Offcanvas.getOrCreateInstance(offcanvasEl);

        this.#nameEl = offcanvasEl.querySelector('[data-oe-name]');
        this.#pictureEl = offcanvasEl.querySelector('[data-oe-picture]');
        this.#priceLabelEl = offcanvasEl.querySelector('[data-oe-price-label]');
        this.#qtyInput = offcanvasEl.querySelector('[data-oe-qty]');
        this.#priceInput = offcanvasEl.querySelector('[data-oe-price]');
        this.#totalEl = offcanvasEl.querySelector('[data-oe-total]');
        this.#decrementBtn = offcanvasEl.querySelector('[data-oe-decrement]');
        this.#incrementBtn = offcanvasEl.querySelector('[data-oe-increment]');
        this.#applyBtn = offcanvasEl.querySelector('[data-oe-apply]');
        this.#deleteBtn = offcanvasEl.querySelector('[data-oe-delete]');

        DecimalFields.wrapExistingInput(this.#qtyInput, 'quantity');
        DecimalFields.wrapExistingInput(this.#priceInput, 'currency');

        this.#bindEvents();
    }

    // item: { name, picture?, quantity, unitPrice, quantityDecimalPlaces, priceLabel? }
    // callbacks: { onApply(qty, price), onDelete() }
    open(item, callbacks = {}) {
        this.#currentItem = item;
        this.#callbacks = callbacks;
        this.#populate(item);
        this.#bsOffcanvas.show();
        this.#offcanvasEl.addEventListener('shown.bs.offcanvas', () => {
            this.#qtyInput.focus();
            this.#qtyInput.select();
        }, { once: true });
    }

    close() {
        this.#bsOffcanvas.hide();
    }

    #populate(item) {
        this.#nameEl.textContent = item.name;

        if (this.#pictureEl) {
            if (item.picture) {
                this.#pictureEl.src = item.picture;
                this.#pictureEl.classList.remove('d-none');
            } else {
                this.#pictureEl.classList.add('d-none');
            }
        }

        if (this.#priceLabelEl) {
            this.#priceLabelEl.textContent = item.priceLabel ?? 'Đơn giá';
        }

        const decimals = item.quantityDecimalPlaces ?? 0;
        const prevDecimals = parseInt(this.#qtyInput.dataset.decimals ?? '0', 10);
        if (prevDecimals !== decimals) {
            this.#qtyInput.dataset.decimals = decimals;
            DecimalFields.wrapExistingInput(this.#qtyInput, 'quantity');
        }

        this.#qtyInput.value = DecimalFields.formatQuantity(item.quantity, decimals);
        this.#priceInput.value = DecimalFields.formatCurrency(item.unitPrice);
        this.#refreshTotal();
    }

    #getQty() {
        const decimals = parseInt(this.#qtyInput.dataset.decimals ?? '0', 10);
        return parseFloat(DecimalFields.stripFormatting(this.#qtyInput.value, decimals > 0 ? decimals : undefined)) || 0;
    }

    #getPrice() {
        return parseFloat(DecimalFields.stripFormatting(this.#priceInput.value)) || 0;
    }

    #refreshTotal() {
        const total = this.#getQty() * this.#getPrice();
        if (this.#totalEl) {
            this.#totalEl.textContent = DecimalFields.formatCurrency(total) + ' đ';
        }
    }

    #adjustQty(delta) {
        const decimals = parseInt(this.#qtyInput.dataset.decimals ?? '0', 10);
        const current = this.#getQty();
        const next = Math.max(decimals > 0 ? Math.pow(10, -decimals) : 1, current + delta);
        this.#qtyInput.value = DecimalFields.formatQuantity(next, decimals);
        this.#refreshTotal();
    }

    #apply() {
        const qty = this.#getQty();
        const price = this.#getPrice();
        if (qty <= 0) return;
        this.#callbacks.onApply?.(qty, price);
        this.close();
    }

    async #confirmDelete() {
        const result = await Swal.fire({
            title: 'Xóa hàng hóa?',
            text: `"${this.#currentItem?.name}" sẽ bị xóa khỏi đơn.`,
            icon: 'warning',
            showCancelButton: true,
            confirmButtonText: 'Xóa',
            cancelButtonText: 'Hủy',
            confirmButtonColor: '#dc3545',
            reverseButtons: true
        });

        if (result.isConfirmed) {
            this.close();
            this.#callbacks.onDelete?.();
        }
    }

    #bindEvents() {
        this.#qtyInput.addEventListener('input', () => this.#refreshTotal());
        this.#priceInput.addEventListener('input', () => this.#refreshTotal());
        this.#decrementBtn?.addEventListener('click', () => this.#adjustQty(-1));
        this.#incrementBtn?.addEventListener('click', () => this.#adjustQty(1));
        this.#applyBtn?.addEventListener('click', () => this.#apply());
        this.#deleteBtn?.addEventListener('click', () => this.#confirmDelete());
    }
}

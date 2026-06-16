export default class ItemEditOffcanvas {
    #offcanvasEl;
    #bsOffcanvas;
    #callbacks = {};
    #currentItem = null;
    #showPrice = true;

    #nameEl;
    #pictureEl;
    #qtyLabelEl;
    #priceLabelEl;
    #priceSectionEl;
    #totalSectionEl;
    #qtyInput;
    #priceInput;
    #totalEl;
    #totalHintEl;
    #decrementBtn;
    #incrementBtn;
    #applyBtn;
    #deleteBtn;
    #form;

    #openOptions;

    // opts: { showPrice?: boolean, qtyLabel?: string }
    constructor(offcanvasEl, opts = {}) {
        this.#offcanvasEl = offcanvasEl;
        this.#bsOffcanvas = bootstrap.Offcanvas.getOrCreateInstance(offcanvasEl);
        this.#form = offcanvasEl.querySelector('form');
        this.#showPrice = opts.showPrice !== false;

        this.#nameEl = offcanvasEl.querySelector('[data-oe-name]');
        this.#pictureEl = offcanvasEl.querySelector('[data-oe-picture]');
        this.#qtyLabelEl = offcanvasEl.querySelector('[data-oe-qty-label]');
        this.#priceLabelEl = offcanvasEl.querySelector('[data-oe-price-label]');
        this.#priceSectionEl = offcanvasEl.querySelector('[data-oe-price-section]');
        this.#totalSectionEl = offcanvasEl.querySelector('[data-oe-total-section]');
        this.#qtyInput = offcanvasEl.querySelector('[data-oe-qty]');
        this.#priceInput = offcanvasEl.querySelector('[data-oe-price]');
        this.#totalEl = offcanvasEl.querySelector('[data-oe-total]');
        this.#totalHintEl = offcanvasEl.querySelector('[data-oe-total-hint]');
        this.#decrementBtn = offcanvasEl.querySelector('[data-oe-decrement]');
        this.#incrementBtn = offcanvasEl.querySelector('[data-oe-increment]');
        this.#applyBtn = offcanvasEl.querySelector('[data-oe-apply]');
        this.#deleteBtn = offcanvasEl.querySelector('[data-oe-delete]');

        if (opts.qtyLabel && this.#qtyLabelEl) {
            this.#qtyLabelEl.textContent = opts.qtyLabel;
        }

        if (!this.#showPrice) {
            this.#priceSectionEl?.classList.add('d-none');
            this.#totalSectionEl?.classList.add('d-none');
        }

        DecimalFields.wrapExistingInput(this.#qtyInput, 'quantity');
        if (this.#showPrice) {
            DecimalFields.wrapExistingInput(this.#priceInput, 'currency');
        }

        this.#bindEvents();
    }

    // item: { name, picture?, quantity, unitPrice, quantityDecimalPlaces, priceLabel? }
    // callbacks: { onApply(qty, price), onDelete() }
    open(item, callbacks = {}, opts = {}) {
        this.#currentItem = item;
        this.#callbacks = callbacks;
        this.#openOptions = Object.assign({ canRemove: true }, opts);
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

        if (this.#priceLabelEl && item.priceLabel) {
            this.#priceLabelEl.textContent = item.priceLabel;
        }

        const decimals = item.quantityDecimalPlaces ?? 0;
        const prevDecimals = parseInt(this.#qtyInput.dataset.decimals ?? '0', 10);
        if (prevDecimals !== decimals) {
            this.#qtyInput.dataset.decimals = decimals;
            DecimalFields.wrapExistingInput(this.#qtyInput, 'quantity');
        }

        this.#qtyInput.value = DecimalFields.formatQuantity(item.quantity, decimals);
        if (this.#showPrice) {
            this.#priceInput.value = DecimalFields.formatCurrency(item.unitPrice);
            this.#refreshTotal();
        }

        this.#deleteBtn.classList.toggle('invisible', !this.#openOptions.canRemove);

        this.#form.querySelectorAll('.field-validation-error').forEach(element => element.style.display = 'none');

        $(this.#form).removeData('validator').removeData('unobtrusiveValidation');
        $.validator.unobtrusive.parse(this.#form);
    }

    #getQty() {
        return DecimalFields.getValue(this.#qtyInput);
    }

    #getPrice() {
        if (!this.#showPrice) return 0;
        return DecimalFields.getValue(this.#priceInput);
    }

    #refreshTotal() {
        if (!this.#showPrice) return;
        const total = this.#getQty() * this.#getPrice();
        if (this.#totalEl) {
            this.#totalEl.textContent = DecimalFields.formatCurrencyWithSymbol(total);
        }
        if (this.#totalHintEl) {
            if (total > 0)
                this.#totalHintEl.textContent = window.SoBangChu.docSoTien(total);
            else
                this.#totalHintEl.textContent = '';
        }
    }

    #adjustQty(delta) {
        const decimals = parseInt(this.#qtyInput.dataset.decimals ?? '0', 10);
        const current = this.#getQty();
        const next = Math.max(decimals > 0 ? Math.pow(10, -decimals) : 1, current + delta);
        this.#qtyInput.value = DecimalFields.formatQuantity(next, decimals);
        if (this.#showPrice) this.#refreshTotal();
    }

    #apply() {
        const qty = this.#getQty();
        const price = this.#getPrice();
        if (qty <= 0) return;
        this.#callbacks.onApply?.(qty, price);
        this.close();
    }

    async #confirmDelete() {
        if (!this.#openOptions.canRemove)
            return;

        this.close();

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
        this.#qtyInput.addEventListener('input', () => { if (this.#showPrice) this.#refreshTotal(); });
        if (this.#showPrice) {
            this.#priceInput.addEventListener('input', () => this.#refreshTotal());
        }
        this.#decrementBtn?.addEventListener('click', () => this.#adjustQty(-1));
        this.#incrementBtn?.addEventListener('click', () => this.#adjustQty(1));
        this.#form?.addEventListener('submit', (e) => {
            e.preventDefault();
            if (!$(this.#form).valid())
                return;
            this.#apply();
        });
        this.#deleteBtn?.addEventListener('click', () => this.#confirmDelete());
    }
}

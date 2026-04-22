class Customer {
    constructor({ id, name, phone, address }) {
        this.id = id;
        this.name = name ?? '';
        this.phone = phone ?? '';
        this.address = address ?? '';
    }
}

export default class CustomerPicker {
    #customers = [];
    #selectedCustomer = null;
    #debounceTimer = null;
    #abortController = null;

    static #DEBOUNCE_DELAY = 500;
    static #MIN_QUERY_LENGTH = 0;

    constructor(target) {
        if (!(target instanceof HTMLElement)) {
            throw new TypeError('Target phải là một HTMLElement hợp lệ');
        }

        this.target = target;
        this.#init();
    }

    // ─── Khởi tạo ───────────────────────────────────────────────────────────────

    #init() {
        this.target.innerHTML = this.#template();
        this.target.classList.add('position-relative');
        this.#mapElements();
        this.#bindEvents();
    }

    #mapElements() {
        const q = (sel) => this.target.querySelector(sel);
        this.input = q('.customerSearch');
        this.inputGroup = q('.input-group-container');
        this.suggestion = q('.customerSuggestion');
        this.displayInfo = q('.selectedCustomerInfo');
        this.clearBtn = q('.clearCustomer');
        this.searchIcon = q('.searchIcon');
    }

    // ─── Sự kiện ────────────────────────────────────────────────────────────────

    #bindEvents() {
        this.input.addEventListener('input', (e) => this.#onInput(e));
        this.input.addEventListener('focus', (e) => this.#onInput(e));
        this.input.addEventListener('keydown', (e) => this.#onKeydown(e));
        this.clearBtn.addEventListener('click', () => this.removeCustomer());

        // Đóng suggestion khi click ra ngoài
        document.addEventListener('click', (e) => {
            if (!this.target.contains(e.target)) this.#hideSuggestion();
        });
    }

    #onInput(e) {
        const query = e.target.value.trim();
        this.#cancelPendingRequest();
        clearTimeout(this.#debounceTimer);

        if (query.length < CustomerPicker.#MIN_QUERY_LENGTH) {
            this.#hideSuggestion();
            return;
        }

        this.#debounceTimer = setTimeout(() => this.#search(query), CustomerPicker.#DEBOUNCE_DELAY);
    }

    #onKeydown(e) {
        if (e.key === 'Escape') {
            this.#hideSuggestion();
            this.input.blur();
        }

        if (e.key === 'ArrowDown') {
            e.preventDefault();
            this.suggestion.querySelector('.list-group-item-action')?.focus();
        }
    }

    // ─── Tìm kiếm ───────────────────────────────────────────────────────────────

    async #search(query) {
        this.#setLoading(true);

        const data = await this.#fetchCustomers(query);

        this.#setLoading(false);

        if (data === null) return; // Bị abort — bỏ qua

        this.#customers = data;
        this.#renderSuggestion(query);
    }

    async #fetchCustomers(query) {
        this.#abortController = new AbortController();

        try {
            const url = `/Customer/Search?q=${encodeURIComponent(query)}`;
            const res = await fetch(url, { signal: this.#abortController.signal });

            if (!res.ok) throw new Error(`HTTP ${res.status}`);

            return await res.json();
        } catch (err) {
            if (err.name === 'AbortError') return null;

            console.error('[CustomerPicker] Fetch error:', err);
            this.#showError('Không thể tải dữ liệu. Vui lòng thử lại.');
            return [];
        }
    }

    #cancelPendingRequest() {
        this.#abortController?.abort();
        this.#abortController = null;
    }

    // ─── Render ─────────────────────────────────────────────────────────────────

    #renderSuggestion(query = '') {
        this.suggestion.innerHTML = '';

        if (!this.#customers.length) {
            this.suggestion.innerHTML = `
                <div class="list-group-item p-3 text-center text-muted small">
                     Không tìm thấy kết quả
                </div>`;
            this.suggestion.style.display = 'block';
            return;
        }

        const fragment = document.createDocumentFragment();

        this.#customers.forEach((data, index) => {
            const customer = new Customer(data);
            const btn = document.createElement('button');
            btn.type = 'button';
            btn.className = 'list-group-item list-group-item-action py-2';
            btn.dataset.index = index;
            btn.innerHTML = `
                <div><span class="fw-bold">${this.#highlight(customer.name, query)}</span> <span class="text-muted">- ${customer.phone}</span></div>
                <div class="text-muted">
                    ${customer.address ? `<div>${customer.address}</div>` : ''}
                </div>`;

            btn.addEventListener('click', () => this.selectCustomer(customer));

            // Điều hướng bằng bàn phím trong danh sách
            btn.addEventListener('keydown', (e) => {
                if (e.key === 'ArrowDown') { e.preventDefault(); btn.nextElementSibling?.focus(); }
                if (e.key === 'ArrowUp') { e.preventDefault(); btn.previousElementSibling?.focus() ?? this.input.focus(); }
                if (e.key === 'Escape') { this.#hideSuggestion(); this.input.focus(); }
            });

            fragment.appendChild(btn);
        });

        this.suggestion.appendChild(fragment);
        this.suggestion.style.display = 'block';
    }

    /** Highlight từ khóa tìm kiếm trong kết quả */
    #highlight(text, query) {
        if (!query) return text;
        const escaped = query.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
        return text.replace(new RegExp(`(${escaped})`, 'gi'), '<mark class="bg-warning px-0">$1</mark>');
    }

    #showError(message) {
        this.suggestion.innerHTML = `
            <div class="list-group-item p-3 text-center text-danger small">
                ${message}
            </div>`;
        this.suggestion.style.display = 'block';
    }

    #hideSuggestion() {
        this.suggestion.style.display = 'none';
        this.suggestion.innerHTML = '';
    }

    #setLoading(isLoading) {
        this.input.readonly = isLoading;
    }

    // ─── Chọn / Xóa khách hàng ──────────────────────────────────────────────────

    displayCustomer(customer) {
        this.displayInfo.querySelector('.name-field').textContent = customer.name;
        this.displayInfo.querySelector('.phone-field').textContent = customer.phone;
        this.displayInfo.querySelector('.address-field').textContent = customer.address;

        this.inputGroup.classList.add('d-none');
        this.displayInfo.classList.remove('d-none');
        this.#hideSuggestion();
    }
    selectCustomer(customer) {
        this.#selectedCustomer = customer instanceof Customer ? customer : new Customer(customer);

        this.displayCustomer(this.#selectedCustomer);

        this.input.value = '';
        this.#dispatch('select', { customer: this.#selectedCustomer });
    }

    removeCustomer() {
        this.#selectedCustomer = null;
        this.inputGroup.classList.remove('d-none');
        this.displayInfo.classList.add('d-none');
        this.#dispatch('remove');
    }

    // ─── Tiện ích ────────────────────────────────────────────────────────────────

    #dispatch(eventName, detail = {}) {
        this.target.dispatchEvent(new CustomEvent(eventName, { bubbles: true, detail }));
    }

    get value() {
        return this.#selectedCustomer;
    }

    // ─── Template HTML ───────────────────────────────────────────────────────────
    #template() {
        return `
        <div class="input-group-container">
            <input type="text" class="form-control customerSearch" id="customerSearch"
                placeholder="Nhập tên, số điện thoại..." autocomplete="off"
                aria-label="Tìm kiếm khách hàng" aria-autocomplete="list" />
        </div>
        <div class="list-group list-group-flush position-absolute w-100 shadow-lg mt-1 customerSuggestion" style="z-index: 1050; display: none; max-height: 300px; overflow-y: auto;" role="listbox"></div>
        <div class="alert alert-primary d-none border-0 rounded-3 d-flex align-items-center mb-0 selectedCustomerInfo" role="status">
            <div class="flex-grow-1 overflow-hidden">
                <div class="fw-bold text-truncate name-field"></div>
                <div class="small text-muted phone-field"></div>
                <div class="small text-muted text-truncate address-field"></div>
            </div>
            <button type="button" class="btn-close ms-2 clearCustomer" aria-label="Xóa khách hàng"></button>
        </div>`;
    }
}
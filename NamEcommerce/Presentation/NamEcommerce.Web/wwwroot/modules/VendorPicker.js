export class Vendor {
    constructor({ id, name, phone }) {
        this.id = id;
        this.name = name ?? '';
        this.phone = phone ?? '';
    }
}

export default class VendorPicker {
    #vendors = [];
    #selectedVendor = null;
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
        this.input = q('.vendorSearch');
        this.inputGroup = q('.input-group-container');
        this.suggestion = q('.vendorSuggestion');
        this.displayInfo = q('.selectedVendorInfo');
        this.clearBtn = q('.clearVendor');
        this.loadingSpinner = q('.searchSpinner');
        this.searchIcon = q('.searchIcon');
    }

    // ─── Sự kiện ────────────────────────────────────────────────────────────────

    #bindEvents() {
        this.input.addEventListener('input', (e) => this.#onInput(e));
        this.input.addEventListener('focus', (e) => this.#onInput(e));
        this.input.addEventListener('keydown', (e) => this.#onKeydown(e));
        this.clearBtn.addEventListener('click', () => this.removeVendor());

        // Đóng suggestion khi click ra ngoài
        document.addEventListener('click', (e) => {
            if (!this.target.contains(e.target)) this.#hideSuggestion();
        });
    }

    #onInput(e) {
        const query = e.target.value.trim();
        this.#cancelPendingRequest();
        clearTimeout(this.#debounceTimer);

        if (query.length < VendorPicker.#MIN_QUERY_LENGTH) {
            this.#hideSuggestion();
            return;
        }

        this.#debounceTimer = setTimeout(() => this.#search(query), VendorPicker.#DEBOUNCE_DELAY);
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

        const data = await this.#fetchVendors(query);

        this.#setLoading(false);

        if (data === null) return; // Bị abort — bỏ qua

        this.#vendors = data;
        this.#renderSuggestion(query);
    }

    async #fetchVendors(query) {
        this.#abortController = new AbortController();

        try {
            const url = `/Vendor/Search?q=${encodeURIComponent(query)}`;
            const res = await fetch(url, { signal: this.#abortController.signal });

            if (!res.ok) throw new Error(`HTTP ${res.status}`);

            return await res.json();
        } catch (err) {
            if (err.name === 'AbortError') return null;

            console.error('[VendorPicker] Fetch error:', err);
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

        if (!this.#vendors.length) {
            this.suggestion.innerHTML = `
                <div class="list-group-item p-3 text-center text-muted small">
                    <i class="bi bi-inbox me-1"></i> Không tìm thấy kết quả
                </div>`;
            this.suggestion.style.display = 'block';
            return;
        }

        const fragment = document.createDocumentFragment();

        this.#vendors.forEach((data, index) => {
            const vendor = new Vendor(data);
            const btn = document.createElement('button');
            btn.type = 'button';
            btn.className = 'list-group-item list-group-item-action border-0 py-2';
            btn.dataset.index = index;
            btn.innerHTML = `
                <div class="fw-bold">${this.#highlight(vendor.name, query)}</div>
                ${vendor.phone ? `<small class="text-muted d-block">${vendor.phone}</small>` : ''}
                ${vendor.address ? `<div><i class="bi bi-geo-alt me-1"></i>${vendor.address}</div>` : ''}
            `;

            btn.addEventListener('click', (e) => {
                e.preventDefault();
                this.selectVendor(vendor);
            });

            btn.addEventListener('keydown', (e) => {
                if (e.key === 'Enter') {
                    e.preventDefault();
                    this.selectVendor(vendor);
                }
            });

            fragment.appendChild(btn);
        });

        this.suggestion.appendChild(fragment);
        this.suggestion.style.display = 'block';
    }

    #highlight(text, query) {
        if (!query) return text;

        const regex = new RegExp(`(${this.#escapeRegex(query)})`, 'gi');
        return text.replace(regex, '<mark>$1</mark>');
    }

    #escapeRegex(str) {
        return str.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
    }

    #setLoading(loading) {
        this.loadingSpinner.style.display = loading ? 'block' : 'none';
        this.searchIcon.style.display = loading ? 'none' : 'block';
    }

    #showError(message) {
        this.suggestion.innerHTML = `
            <div class="list-group-item p-3 text-center text-danger small">
                <i class="bi bi-exclamation-triangle-fill me-1"></i> ${message}
            </div>`;
        this.suggestion.style.display = 'block';
    }

    #hideSuggestion() {
        this.suggestion.style.display = 'none';
    }

    // ─── Chọn nhà cung cấp ───────────────────────────────────────────────────────

    displayVendor(vendor) {
        this.displayInfo.querySelector('.name-field').textContent = vendor.name;
        this.displayInfo.querySelector('.phone-field').textContent = vendor.phone;
        this.displayInfo.querySelector('.address-field').textContent = vendor.address;

        this.inputGroup.classList.add('d-none');
        this.displayInfo.classList.remove('d-none');
        this.#hideSuggestion();
    }
    selectVendor(vendor) {
        this.#selectedVendor = vendor instanceof Vendor ? vendor : new Vendor(vendor);

        this.displayVendor(this.#selectedVendor);

        this.input.value = '';
        this.#dispatch('select', { vendor: this.#selectedVendor });
    }

    removeVendor() {
        this.#selectedVendor = null;
        this.inputGroup.classList.remove('d-none');
        this.displayInfo.classList.add('d-none');
        this.#dispatch('remove');
    }

    #dispatch(eventName, detail = {}) {
        this.target.dispatchEvent(new CustomEvent(eventName, { bubbles: true, detail }));
    }

    // ─── Template ────────────────────────────────────────────────────────────────

    #template() {
        return `
        <div class="input-group-container">
            <div class="input-group">
                <span class="input-group-text bg-white border-end-0">
                    <span class="spinner-border spinner-border-sm text-secondary d-none searchSpinner" role="status"></span>
                    <i class="bi bi-search text-muted searchSpinner d-none"></i>
                    <i class="bi bi-search text-muted searchIcon"></i>
                </span>
                <input type="text" class="form-control border-start-0 ps-0 vendorSearch" id="vendorSearch"
                    placeholder="Nhập tên, số điện thoại..." autocomplete="off"
                    aria-label="Tìm kiếm nhà cung cấp" aria-autocomplete="list" />
            </div>
        </div>

        <div class="list-group position-absolute w-100 shadow-lg mt-1 vendorSuggestion"
            style="z-index: 1050; display: none; max-height: 300px; overflow-y: auto;"
            role="listbox"></div>

        <div class="alert alert-primary d-none border-0 rounded-3 d-flex align-items-center mb-0 selectedVendorInfo" role="status">
            <i class="bi bi-building-fill fs-4 me-3 flex-shrink-0"></i>
            <div class="flex-grow-1 overflow-hidden">
                <div class="fw-bold text-truncate name-field"></div>
                <div class="small text-muted phone-field"></div>
                <div class="small text-muted text-truncate address-field"></div>
            </div>
            <button type="button" class="btn-close ms-2 clearVendor" aria-label="Xóa nhà cung cấp"></button>
        </div>
        `;
    }
}

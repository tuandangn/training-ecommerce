// ─── Model ───────────────────────────────────────────────────────────────────

class Product {
    constructor({ id, name, availableQty, picture, unitPrice, vendorCount, firstVendorId, availableVendors }) {
        this.id = id;
        this.name = name ?? '';
        this.availableQty = availableQty ?? 0;
        this.picture = picture ?? '';
        this.unitPrice = unitPrice ?? 0;
        this.vendorCount = vendorCount ?? 0;
        this.firstVendorId = firstVendorId ?? null;
        this.availableVendors = availableVendors ?? [];
    }
}

// ─── API Layer ────────────────────────────────────────────────────────────────

class ProductApi {
    #abortController = null;

    async search(query, vendorId = null) {
        this.cancel();
        this.#abortController = new AbortController();

        let url = `/Product/Search?q=${encodeURIComponent(query)}`;
        if (vendorId) {
            url += `&vid=${encodeURIComponent(vendorId)}`;
        }

        const res = await fetch(url, {
            signal: this.#abortController.signal,
        });

        if (!res.ok) throw new Error(`HTTP ${res.status}`);
        return await res.json();
    }

    cancel() {
        this.#abortController?.abort();
        this.#abortController = null;
    }
}

// ─── View (chỉ lo render & emit DOM events) ──────────────────────────────────

class ProductPickerView {
    constructor(target) {
        this.target = target;
        target.innerHTML = ProductPickerView.#template();
        target.classList.add('position-relative');

        this.root = target;
        this.input = target.querySelector('.productSearch');
        this.inputGroup = target.querySelector('.input-group-container');
        this.suggestion = target.querySelector('.productSuggestion');
        this.displayInfo = target.querySelector('.selectedProductInfo');
        this.clearBtn = target.querySelector('.clearProduct');
        this.spinner = target.querySelector('.searchSpinner');
        this.searchIcon = target.querySelector('.searchIcon');
    }

    renderSuggestion(products, query = '') {
        this.suggestion.innerHTML = '';

        if (!products.length) {
            this.suggestion.innerHTML = `
                <div class="list-group-item p-3 text-center text-muted small">
                    <i class="bi bi-inbox me-1"></i> Không tìm thấy kết quả
                </div>`;
        } else {
            const fragment = document.createDocumentFragment();
            products.forEach(p => fragment.appendChild(this.#buildItem(p, query)));
            this.suggestion.appendChild(fragment);
        }

        this.suggestion.style.display = 'block';
    }

    renderError(message) {
        this.suggestion.innerHTML = `
            <div class="list-group-item p-3 text-center text-danger small">
                <i class="bi bi-exclamation-circle me-1"></i>${message}
            </div>`;
        this.suggestion.style.display = 'block';
    }

    showProduct(product) {
        const nameField = this.displayInfo.querySelector('.name-field');
        const stockField = this.displayInfo.querySelector('.stock-field');
        const pictureField = this.displayInfo.querySelector('.picture-field');
        const pictureFieldIcon = this.displayInfo.querySelector('.picture-field-icon');

        nameField.textContent = product.name;
        stockField.textContent = DecimalFields.formatQuantity(product.availableQty);

        const hasPicture = Boolean(product.picture);
        pictureField.alt = product.name;
        pictureField.src = hasPicture ? product.picture : '';
        pictureField.classList.toggle('d-none', !hasPicture);
        pictureFieldIcon.classList.toggle('d-none', hasPicture);

        this.inputGroup.classList.add('d-none');
        this.inputGroup.classList.add('d-none');
        this.target.querySelector(`label[for="${this.input.id}"]`)?.classList.add('d-none');
        this.displayInfo.classList.remove('d-none');
        this.hideSuggestion();
    }

    clearProduct() {
        this.inputGroup.classList.remove('d-none');
        this.displayInfo.classList.add('d-none');
        this.target.querySelector(`label[for="${this.input.id}"]`)?.classList.remove('d-none');
    }

    hideSuggestion() {
        this.suggestion.style.display = 'none';
        this.suggestion.innerHTML = '';
    }

    setLoading(isLoading) {
    //    this.spinner.classList.toggle('d-none', !isLoading);
    //    this.searchIcon.classList.toggle('d-none', isLoading);
    }

    #buildItem(product, query) {
        const btn = document.createElement('button');
        btn.type = 'button';
        btn.className = 'list-group-item list-group-item-action border-0 py-2';
        btn.innerHTML = `
            <div class="d-flex align-items-center gap-3">
                <div class="flex-shrink-0 text-center" style="width:70px;">
                    ${product.picture
                ? `<img class="img-fluid img-thumbnail"
                                src="${product.picture}" alt="${product.name}" />`
                : `<i class="bi bi-box-seam text-muted fs-5"></i>`
            }
                </div>
                <div class="flex-grow-1 overflow-hidden">
                    <div class="fw-bold text-truncate">${highlight(product.name, query)}</div>
                    ${product.availableQty > 0 ? `<div class="small text-muted">
                            Tồn kho: <span>${DecimalFields.formatQuantity(product.availableQty)}</span>
                        </div>`: ''}
                </div>
            </div>`;

        // View chỉ emit event — không tự xử lý logic
        btn.addEventListener('click', () =>
            this.root.dispatchEvent(new CustomEvent('picker:pick', {
                bubbles: true,
                detail: { product },
            }))
        );

        // Điều hướng bằng bàn phím trong danh sách
        btn.addEventListener('keydown', (e) => {
            if (e.key === 'ArrowDown') { e.preventDefault(); btn.nextElementSibling?.focus(); }
            if (e.key === 'ArrowUp') { e.preventDefault(); btn.previousElementSibling?.focus() ?? this.input.focus(); }
            if (e.key === 'Escape') { this.hideSuggestion(); this.input.focus(); }
        });

        return btn;
    }

    static #template() {
        return `
        <label class="form-label small fw-bold text-muted" for="productSearch">Tìm kiếm hàng hóa</label>
        <div class="input-group-container">
            <div class="input-group">
                <span class="input-group-text bg-white border-end-0">
                    <span class="spinner-border spinner-border-sm text-secondary d-none searchSpinner" role="status"></span>
                    <i class="bi bi-search text-muted searchIcon"></i>
                </span>
                <input type="text" class="form-control border-start-0 ps-0 productSearch" id="productSearch"
                    placeholder="Nhập tên hàng hóa..." autocomplete="off"
                    aria-label="Tìm kiếm hàng hóa" aria-autocomplete="list" />
            </div>
        </div>

        <div class="list-group position-absolute w-100 shadow-lg mt-1 productSuggestion"
            style="z-index: 1050; display: none; max-height: 300px; overflow-y: auto;"
            role="listbox"></div>

        <div class="alert alert-light border-0 rounded-3 mb-3 d-flex align-items-center gap-3 d-none selectedProductInfo"
            role="status">
            <img class="img-fluid img-thumbnail d-none picture-field" style="width:70px;" alt="" />
            <i class="bi bi-box-seam-fill text-primary fs-5 picture-field-icon"></i>
            <div class="flex-grow-1 overflow-hidden">
                <div class="fw-bold text-truncate name-field"></div>
                <div class="mb-2 small">
                    Tồn kho: <span class="text-muted stock-field"></span>
                </div>
                <button type="button" class="btn btn-light btn-sm clearProduct" aria-label="Thay đổi hàng hóa">
                    <i class="bi bi-arrow-clockwise"></i>
                    Thay đổi
                </button>
            </div>
        </div>`;
    }
}

// ─── Controller (điều phối View + API + state) ────────────────────────────────

export default class ProductPicker {
    #selected = null;
    #debounceTimer = null;
    vendorId = null;

    static #DEBOUNCE_MS = 500;
    static #MIN_QUERY_LEN = 0;

    constructor(target) {
        if (!(target instanceof HTMLElement))
            throw new TypeError('Target phải là HTMLElement hợp lệ');

        this.api = new ProductApi();
        this.view = new ProductPickerView(target);

        this.#bindEvents();
    }

    #bindEvents() {
        const { view } = this;

        // Input / Focus → debounce → search
        const onInput = (e) => {
            clearTimeout(this.#debounceTimer);
            const query = e.target.value.trim();

            if (query.length < ProductPicker.#MIN_QUERY_LEN) {
                this.api.cancel();
                view.hideSuggestion();
                return;
            }

            this.#debounceTimer = setTimeout(() => this.#search(query), ProductPicker.#DEBOUNCE_MS);
        };

        view.input.addEventListener('input', onInput);
        view.input.addEventListener('focus', onInput);

        view.input.addEventListener('keydown', (e) => {
            if (e.key === 'Escape') { view.hideSuggestion(); view.input.blur(); }
            if (e.key === 'ArrowDown') {
                e.preventDefault();
                view.suggestion.querySelector('.list-group-item-action')?.focus();
            }
        });

        // Chọn item từ suggestion
        view.root.addEventListener('picker:pick', (e) => {
            this.#select(new Product(e.detail.product));
        });

        // Nút xóa
        view.clearBtn.addEventListener('click', () => {
            this.clear()
        });

        // Click ngoài → đóng
        document.addEventListener('click', (e) => {
            if (!view.root.contains(e.target)) view.hideSuggestion();
        });
    }

    async #search(query) {
        this.view.setLoading(true);

        try {
            const data = await this.api.search(query, this.vendorId);
            const products = data.map(d => new Product(d));
            this.view.renderSuggestion(products, query);
        } catch (err) {
            if (err.name !== 'AbortError')
                this.view.renderError('Không thể tải dữ liệu. Vui lòng thử lại.');
        } finally {
            this.view.setLoading(false);
        }
    }

    #select(product) {
        this.#selected = product;
        this.view.showProduct(product);
        this.#dispatch('select', { product });
    }

    clear() {
        this.#selected = null;
        this.view.clearProduct();
        this.#dispatch('remove');
    }

    #dispatch(name, detail = {}) {
        this.view.root.dispatchEvent(new CustomEvent(name, { bubbles: true, detail }));
    }

    get value() {
        return this.#selected;
    }
}

// ─── Helpers ──────────────────────────────────────────────────────────────────

function highlight(text, query) {
    if (!query) return text;
    const safe = query.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
    return text.replace(new RegExp(`(${safe})`, 'gi'), '<mark class="bg-warning px-0">$1</mark>');
}
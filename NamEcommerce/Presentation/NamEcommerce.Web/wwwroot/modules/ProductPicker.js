import { toast } from "/modules/modals.js";
class Product {
    constructor({ id, name, availableQty, picture,
        unitPrice, vendorCount, firstVendorId,
        availableVendors, unitMeasurement, availableWarehouses,
        categoryName
    }) {
        this.id = id;
        this.name = name ?? '';
        this.availableQty = availableQty ?? 0;
        this.picture = picture ?? '';
        this.unitPrice = unitPrice ?? 0;
        this.unitMeasurement = unitMeasurement ?? '';
        this.vendorCount = vendorCount ?? 0;
        this.firstVendorId = firstVendorId ?? null;
        this.availableVendors = availableVendors ?? [];
        this.availableWarehouses = availableWarehouses ?? [];
        this.categoryName = categoryName ?? '';
    }
}

class ProductApi {
    #abortController = null;

    vendorId = null;

    updateVendor(vendorId) {
        this.vendorId = vendorId;
    }

    async search(query) {
        this.cancel();
        this.#abortController = new AbortController();

        let url = `/Product/Search?q=${encodeURIComponent(query)}`;
        if (this.vendorId) {
            url += `&vid=${encodeURIComponent(this.vendorId)}`;
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

class ProductPickerView {
    #options;

    constructor(target, options) {
        this.target = target;
        this.#options = Object.assign({ purchase: false }, options);

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

    isValidProduct(product) {
        if (this.#options.purchase) {
            return product.vendorCount > 0;
        }
        return product.availableQty > 0 || product.vendorCount > 0;
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

    updateVendor(vendorName) {
        this.root.querySelector('.vendor-info')?.classList.toggle('d-none', !vendorName);
        const vendorNameEl = this.root.querySelector('.vendor-name');
        if (vendorNameEl) vendorNameEl.textContent = vendorName;
    }

    showProduct(product) {
        const nameField = this.displayInfo.querySelector('.name-field');
        const stockContainer = this.displayInfo.querySelector('.stock-container');
        const stockField = this.displayInfo.querySelector('.stock-field');
        const pictureField = this.displayInfo.querySelector('.picture-field');
        const pictureFieldIcon = this.displayInfo.querySelector('.picture-field-icon');
        const vendorsContainer = this.displayInfo.querySelector('.vendors-container');
        const vendorsField = this.displayInfo.querySelector('.vendors-field');

        let productNameHtml = `${product.name}`;
        if (product.categoryName) {
            productNameHtml += ` <span class="small text-muted fw-normal bg-light p-1">${product.categoryName}</span>`;
        }
        nameField.innerHTML = productNameHtml;

        if (product.availableQty > 0) {
            stockField.textContent = 'Tồn kho: ' + DecimalFields.formatQuantity(product.availableQty) + (product.unitMeasurement ? ' ' + product.unitMeasurement.toLowerCase() : '');
        } else {
            stockField.innerHTML = this.#options.purchase ? '<span class="text-muted">Hết hàng</span>' : '<span class="text-danger">Hết hàng</span>';
        }

        if (this.#options.purchase && product.vendorCount > 0) {
            vendorsField.textContent = product.availableVendors.map(v => v.value).join(' | ');
        }
        vendorsContainer.classList.toggle('d-none', !this.#options.purchase || product.vendorCount == 0);

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
        const isDisabled = !this.isValidProduct(product);

        const pictureHtml = product.picture
            ? `<img class="img-fluid img-thumbnail" src="${product.picture}" alt="${product.name}" />`
            : `<i class="bi bi-box-seam text-muted fs-5"></i>`;

        let vendorInfoHtml = '';
        if (this.#options.purchase) {
            if (product.vendorCount == 0) {
                vendorInfoHtml = `<div class="small text-danger text-decoration-underline">Không có nhà cung cấp</div>`;
            } else {
                vendorInfoHtml = `<div class="small text-muted"><i class="bi bi-truck me-1"></i>${product.availableVendors.map(v => v.value).join(' | ')}</div>`;
            }
        }

        let stockInfoHtml = '';
        if (product.availableQty > 0) {
            stockInfoHtml = `<div class="small text-muted">
                            <span>Tồn kho: ${DecimalFields.formatQuantity(product.availableQty)}${product.unitMeasurement ? ' ' + product.unitMeasurement.toLowerCase() : ''}</span>
                        </div>`;
        } else {
            if (this.#options.purchase) {
                stockInfoHtml = `<div class="small text-muted">Hết hàng</div>`;
            } else {
                stockInfoHtml = `<div class="small text-danger text-decoration-underline"><i class="bi bi-exclamation-circle"></i> Hết hàng</div>`;
            }
        }

        let categoryHtml = '';
        if (product.categoryName) {
            categoryHtml = `<span class="small text-muted fw-normal">${product.categoryName}</span>`;
        }

        const btn = document.createElement('button');
        btn.type = 'button';
        btn.className = 'list-group-item list-group-item-action border-0 py-2' + (isDisabled ? ' bg-light disabled' : '');
        btn.innerHTML = `
            <div class="d-flex align-items-center gap-3">
                <div class="flex-shrink-0 text-center" style="width:70px;">
                    ${pictureHtml}
                </div>
                <div class="flex-grow-1 overflow-hidden">
                    <div class="d-flex align-items-center justify-content-between flex-wrap">
                        <div class="flex-grow-1 ${isDisabled ? 'text-muted fw-medium' : 'fw-bold'} text-truncate">
                            ${highlight(product.name, query)}
                        </div>
                        ${categoryHtml}
                    </div>
                    ${stockInfoHtml}
                    ${vendorInfoHtml}
                </div>
            </div>`;

        // View chỉ emit event — không tự xử lý logic
        btn.addEventListener('click', () => {
            if (isDisabled) {
                toast('Thông báo', `Mặt hàng "${product.name}" không thể được thêm do không thỏa điều kiện.`, 'error')
                return;
            }
            this.root.dispatchEvent(new CustomEvent('picker:pick', {
                bubbles: true,
                detail: { product },
            }))
        });

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
        <label class="form-label small text-muted" for="productSearch">Tìm kiếm hàng hóa</label>
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
            <div class="form-text vendor-info d-none">Nhà cung cấp: <strong class="vendor-name"></strong></div>
        </div>
        <div class="list-group position-absolute w-100 shadow-lg mt-1 productSuggestion"
            style="z-index: 1050; display: none; max-height: 300px; overflow-y: auto;"
            role="listbox">
        </div>
        <div class="alert alert-light border-0 rounded-3 mb-3 d-flex align-items-center gap-3 d-none selectedProductInfo" role="status">
            <img class="img-fluid img-thumbnail d-none picture-field" style="width:70px;" alt="" />
            <i class="bi bi-box-seam-fill text-primary fs-5 picture-field-icon"></i>
            <div class="flex-grow-1 overflow-hidden">
                <div class="fw-bold text-truncate name-field"></div>
                <div class="small stock-container">
                    <span class="text-muted stock-field"></span>
                </div>
                <div class="small text-muted d-none vendors-container"><i class="bi bi-truck me-1"></i><span class="vendors-field"></span></div>
                <div class="mt-2">
                    <button type="button" class="btn btn-light btn-sm clearProduct" aria-label="Thay đổi hàng hóa">
                        <i class="bi bi-arrow-clockwise"></i>
                        Thay đổi
                    </button>
                <div>
            </div>
        </div>`;
    }
}

export default class ProductPicker {
    #selected = null;
    #debounceTimer = null;

    static #DEBOUNCE_MS = 500;
    static #MIN_QUERY_LEN = 0;

    constructor(target, options) {
        if (!(target instanceof HTMLElement))
            throw new TypeError('Target phải là HTMLElement hợp lệ');

        var opts = Object.assign({
            purchase: false
        }, options)

        this.api = new ProductApi();
        this.view = new ProductPickerView(target, opts);

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

        view.root.addEventListener('picker:pick', (e) => {
            this.#select(new Product(e.detail.product));
        });

        view.clearBtn.addEventListener('click', () => {
            this.clear()
        });

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

    #dispatch(name, detail = {}) {
        this.view.root.dispatchEvent(new CustomEvent(name, { bubbles: true, detail }));
    }

    setVendor(vendorId, vendorName) {
        this.api.updateVendor(vendorId);
        this.view.updateVendor(vendorName);
        this.clear();
    }

    clear() {
        this.#selected = null;
        this.view.clearProduct();
        this.#dispatch('remove');
    }

    get value() {
        return this.#selected;
    }
}

function highlight(text, query) {
    if (!query) return text;
    const safe = query.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
    return text.replace(new RegExp(`(${safe})`, 'gi'), '<mark class="bg-warning px-0">$1</mark>');
}
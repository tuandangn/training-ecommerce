export default class ProductBrowser {
    #container;
    #onAdd;
    #options;

    #pendingChange = false;

    #abortController = null;

    #state = {
        q: '',
        cid: undefined,
        vid: null
    };
    #categories = [];

    static #defaults = {
        colClass: 'col-12 col-sm-6 col-md-4 col-lg-6 col-xl-4',
        categoryUrl: '/Category/Options',
        productSearchUrl: '/Product/Search',
        purchase: false,
        initialShow: false,
        allowCreateNew: false,
        checkProduct: null
    };

    constructor(containerEl, onAdd, options = {}) {
        if (!(containerEl instanceof HTMLElement))
            throw new TypeError('ProductBrowser: containerEl must be an HTMLElement');
        if (typeof onAdd !== 'function')
            throw new TypeError('ProductBrowser: onAdd must be a function');

        this.#container = containerEl;
        this.#onAdd = onAdd;
        this.#options = { ...ProductBrowser.#defaults, ...options };

        const initialData = Object.assign({}, containerEl.dataset);
        if (initialData.q)
            this.#state.q = initialData.q;
        if (initialData.cid)
            this.#state.cid = initialData.cid;
        if (initialData.vid)
            this.#state.vid = initialData.vid;

        if (this.#options.initialShow)
            this.#state.cid = null;
    }

    async init() {
        this.#controlTemplate();
        this.#bindEvents();
        this.#categories = await this.#loadCategories();
        await this.#render();
    }

    setVendor(vid) {
        this.#setState({ vid });
    }
    reload() {
        this.#setState({});
    }

    #bindEvents() {
        const input = this.#container.querySelector('.pb-search-input');

        const onChanged = debounce((e) => {
            this.#setState({ q: e.target.value.trim() });
        }, 400);
        input.addEventListener('input', onChanged);

        input.addEventListener('focus', () => {
            if (!this.#productSuggestionIsShown()) {
                this.#showProductSuggestions();
            }
        });

        const collapse = this.#container.querySelector('.collapse');
        collapse.addEventListener('shown.bs.collapse', event => {
            if (this.#pendingChange) {
                if (this.#state.cid === undefined)
                    this.#setState({cid: null});
                else
                    this.#setState({});
            }
        });
    }

    async #setState(patch) {
        this.#state = Object.assign({}, this.#state, patch);
        await this.#render();
    }

    async #render() {
        this.#renderCategories();
        if (!this.#productSuggestionIsShown()) {
            this.#pendingChange = true;
            return;
        }
        this.#pendingChange = false;
        await this.#loadProducts();
    }

    #controlTemplate() {
        this.#container.innerHTML = `
            <div class="accordion accordion-flush" id="accordionProductBrowser">
                <div class="accordion-item">
                    <div class="accordion-header position-relative">
                        <button class="accordion-button text-dark bg-white w-auto p-1 shadow-none position-absolute ${this.#options.initialShow ? '' : 'collapsed'}" style="top:-10px;right:-10px;" type="button"
                            data-bs-toggle="collapse" data-bs-target="#collapseProductBrowser" aria-expanded="${this.#options.initialShow}" aria-controls="collapseProductBrowser">
                            <span class="visually-hidden">Mở thêm hàng hóa</span>
                        </button>
                        <div class="pb-search">
                            <label class="form-label small fw-bold text-muted text-uppercase d-block" for="pbSearchKeywords">Thêm hàng hóa</label>
                            <div class="input-group">
                                <span class="input-group-text bg-white border-end-0">
                                    <i class="bi bi-search text-muted pb-search-icon"></i>
                                    <span class="spinner-border spinner-border-sm text-secondary d-none pb-spinner" role="status"></span>
                                </span>
                                <input type="text" id="pbSearchKeywords" class="form-control border-start-0 ps-0 pb-search-input" value="${this.#state.q}" placeholder="Tìm hàng hóa..." autocomplete="off" />
                                ${this.#options.allowCreateNew ? `<button class="btn btn-outline-secondary" type="button" data-open-quick-product data-bs-toggle="tooltip" title="Thêm hàng hóa mới">
                    <i class="bi bi-plus"></i>
                    <span class="visually-hidden">Thêm mới</span>
                </button>` : ''}
                            </div>
                        <div>
                    </div>
                </div>
            </div>
            <div class="pb-categories mt-3 d-flex flex-wrap gap-1">
                <span class="text-muted small">Đang tải danh mục...</span>
            </div>
            <div id="collapseProductBrowser" class="accordion-collapse collapse ${this.#options.initialShow ? 'show' : ''}" aria-labelledby="headingProductBrowser" data-bs-parent="#accordionProductBrowser">
                <div class="accordion-body p-0 mt-3">
                    <div class="pb-grid" style="max-height:300px; overflow-y: auto;overflow-x:hidden;">Đang tải hàng hóa...</div>
                </div>
            </div>
        `;
    }

    #showProductSuggestions() {
        return new Promise(resolve => {
            const collapse = this.#container.querySelector('.collapse');
            const bsCollapse = new bootstrap.Collapse(collapse, {
                toggle: false
            });
            bsCollapse.show();
            collapse.addEventListener('shown.bs.collapse', function onShow() {
                collapse.removeEventListener('shown.bs.collapse', onShow);
                resolve();
            });
        });
    }
    #productSuggestionIsShown() {
        const collapse = this.#container.querySelector('.collapse');
        return collapse.classList.contains('show');
    }

    async #loadCategories() {
        try {
            const res = await fetch(this.#options.categoryUrl);
            return res.ok ? await res.json() : [];
        } catch {
            return [];
        }
    }

    #renderCategories() {
        const el = this.#container.querySelector('.pb-categories');
        if (!el) return;
        el.innerHTML = '';
        el.appendChild(this.#buildCategoryBtn(null, 'Tất cả'));
        this.#categories.forEach(c => el.appendChild(this.#buildCategoryBtn(c.id, c.name)));
    }

    #buildCategoryBtn(id, name) {
        const btn = document.createElement('button');
        btn.type = 'button';
        btn.dataset.catId = id ?? '';
        btn.className = 'btn btn-sm ' + (this.#state.cid === id ? 'btn-primary' : 'btn-outline-secondary');
        btn.textContent = name;

        btn.addEventListener('click', () => {
            if (!this.#productSuggestionIsShown()) {
                this.#showProductSuggestions().then(() => this.#setState({ cid: id }));
            } else {
                if (this.#state.cid == id)
                    this.#setState({ cid: null });
                else
                    this.#setState({ cid: id });
            }
        });
        return btn;
    }

    async #loadProducts() {
        this.#abortController?.abort();
        this.#abortController = new AbortController();
        this.#setLoading(true);

        try {
            let url = `${this.#options.productSearchUrl}?q=${encodeURIComponent(this.#state.q)}`;
            if (this.#state.cid) url += `&cid=${encodeURIComponent(this.#state.cid)}`;
            if (this.#state.vid) url += `&vid=${encodeURIComponent(this.#state.vid)}`;
            const res = await fetch(url, { signal: this.#abortController.signal });
            if (!res.ok) throw new Error();
            const products = await res.json();
            this.#renderGrid(products);
        } catch (err) {
            if (err.name !== 'AbortError') {
                const grid = this.#container.querySelector('.pb-grid');
                if (grid) grid.innerHTML = '<div class="text-center text-danger small py-3">Không thể tải sản phẩm.</div>';
            }
        } finally {
            this.#setLoading(false);
        }
    }

    #renderGrid(products) {
        const grid = this.#container.querySelector('.pb-grid');
        if (!grid) return;

        if (!products.length) {
            grid.innerHTML = '<div class="text-center text-muted small py-3"><i class="bi bi-inbox me-1"></i>Không có sản phẩm.</div>';
            return;
        }

        grid.innerHTML = '';
        const row = document.createElement('div');
        row.className = 'row g-2 pb-1';

        products.forEach(p => {
            const isDisabled = !this.#isValidProduct(p);
            const col = document.createElement('div');
            col.className = this.#options.colClass;

            const picHtml = p.picture
                ? `<img src="${_esc(p.picture)}" class="pb-product-img" alt="${_esc(p.name)}" />`
                : `<div class="pb-product-img-placeholder"><i class="bi bi-box-seam text-muted fs-4"></i></div>`;

            let vendorInfoHtml = '';
            if (this.#options.purchase) {
                if (p.vendorCount == 0) {
                    vendorInfoHtml = `<div class="small text-danger text-decoration-underline">Không có NCC</div>`;
                } else {
                    vendorInfoHtml = `<div class="small text-muted"><i class="bi bi-truck me-1"></i>${p.vendorCount} NCC</div>`;
                }
            }

            let qtyHtml = p.availableQty > 0
                ? `<span class="text-success fw-medium">${DecimalFields.formatQuantity ? DecimalFields.formatQuantity(p.availableQty) : p.availableQty}</span>`
                : `<span class="text-muted">0</span>`;
            qtyHtml = '<div class="pb-product-stock small"><i class="bi bi-boxes me-1 text-muted"></i>Tồn: ' + qtyHtml + (p.unitMeasurement ? ' ' + p.unitMeasurement : '') + '</div>';

            const catHtml = p.categoryName
                ? `<div class="pb-product-category text-truncate">${_esc(p.categoryName)}</div>`
                : '';

            col.innerHTML = `
                <div class="pb-product-card ${isDisabled ? 'bg-light' : ''} position-relative">
                    <div class="pb-product-thumb">${picHtml}</div>
                    <div class="pb-product-info">
                        <div class="pb-product-name fw-medium text-truncate ${isDisabled ? 'text-muted' : ''}" title="${_esc(p.name)}">${_esc(p.name)}</div>
                        ${catHtml}
                        ${this.#options.purchase ? vendorInfoHtml : qtyHtml}
                    </div>
                    <button type="button" class="stretched-link pb-add-btn btn btn-sm btn-outline-primary ${isDisabled ? 'd-none' : ''}" data-bs-toggle="tooltip" title="Thêm vào phiếu">
                        <i class="bi bi-plus-lg"></i>
                    </button>
                </div>`;

            col.querySelector('.pb-add-btn')?.addEventListener('click', () => this.#onAdd(p));
            row.appendChild(col);
        });

        grid.appendChild(row);
    }

    #isValidProduct(product) {
        if (typeof this.#options.checkProduct === 'function') {
            return this.#options.checkProduct(product);
        }

        if (this.#options.purchase) {
            return product.vendorCount > 0;
        }
        return product.availableQty > 0 || product.vendorCount > 0;
    }

    #setLoading(on) {
        this.#container.querySelector('.pb-spinner')?.classList.toggle('d-none', !on);
        this.#container.querySelector('.pb-search-icon')?.classList.toggle('d-none', on);
    }
}

function _esc(str) {
    return String(str ?? '')
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;');
}

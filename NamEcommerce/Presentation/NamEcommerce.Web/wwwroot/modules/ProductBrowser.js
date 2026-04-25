/**
 * ProductBrowser — reusable product-browsing widget.
 *
 * Usage:
 *   import ProductBrowser from '/modules/ProductBrowser.js';
 *
 *   const browser = new ProductBrowser(
 *       document.getElementById('productBrowser'),
 *       (product) => console.log('add', product),
 *       {
 *           colClass: 'col-12 col-sm-6',        // optional
 *           categoryUrl: '/Category/Options',   // optional
 *           productSearchUrl: '/Product/Search' // optional
 *       }
 *   );
 *   await browser.init();
 *
 * The `product` object passed to `onAdd`:
 *   { id, name, picture, availableQty, categoryName }
 */
export default class ProductBrowser {
    // ── state ─────────────────────────────────────────────────────────────────
    #container;
    #onAdd;
    #options;

    #categories = [];
    #activeCategory = null;
    #searchQuery = '';
    #abortController = null;

    // ── defaults ──────────────────────────────────────────────────────────────
    static #defaults = {
        colClass: 'col-12 col-sm-6 col-md-4 col-lg-6 col-xl-4',
        categoryUrl: '/Category/Options',
        productSearchUrl: '/Product/Search',
    };

    /**
     * @param {HTMLElement} containerEl  – the element to render into
     * @param {Function}    onAdd        – called with the product when "+" is clicked
     * @param {object}      [options]    – optional overrides (colClass, categoryUrl, productSearchUrl)
     */
    constructor(containerEl, onAdd, options = {}) {
        if (!(containerEl instanceof HTMLElement))
            throw new TypeError('ProductBrowser: containerEl must be an HTMLElement');
        if (typeof onAdd !== 'function')
            throw new TypeError('ProductBrowser: onAdd must be a function');

        this.#container = containerEl;
        this.#onAdd = onAdd;
        this.#options = { ...ProductBrowser.#defaults, ...options };
    }

    // ── public ────────────────────────────────────────────────────────────────

    async init() {
        this.#render();
        await this.#loadCategories();
        await this.#loadProducts();
    }

    // ── render ────────────────────────────────────────────────────────────────

    #render() {
        this.#container.innerHTML = `
            <div class="pb-search mb-2">
                <div class="input-group input-group-sm">
                    <span class="input-group-text bg-white border-end-0">
                        <i class="bi bi-search text-muted pb-search-icon"></i>
                        <span class="spinner-border spinner-border-sm text-secondary d-none pb-spinner" role="status"></span>
                    </span>
                    <input type="text" class="form-control border-start-0 ps-0 pb-search-input"
                           placeholder="Tìm hàng hóa..." autocomplete="off" />
                </div>
            </div>
            <div class="pb-categories mb-3 d-flex flex-wrap gap-1">
                <span class="text-muted small">Đang tải danh mục...</span>
            </div>
            <div class="pb-grid"></div>`;

        this.#container.querySelector('.pb-search-input')
            ?.addEventListener('input', debounce((e) => {
                this.#searchQuery = e.target.value.trim();
                this.#loadProducts();
            }, 400));
    }

    // ── categories ────────────────────────────────────────────────────────────

    async #loadCategories() {
        try {
            const res = await fetch(this.#options.categoryUrl);
            this.#categories = res.ok ? await res.json() : [];
        } catch {
            this.#categories = [];
        }
        this.#renderCategories();
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
        btn.className = 'btn btn-sm ' + (this.#activeCategory === id ? 'btn-primary' : 'btn-outline-secondary');
        btn.textContent = name;
        btn.addEventListener('click', () => {
            this.#activeCategory = id;
            this.#container.querySelectorAll('.pb-categories button').forEach(b => {
                b.className = 'btn btn-sm ' + (b.dataset.catId === (id ?? '') ? 'btn-primary' : 'btn-outline-secondary');
            });
            this.#loadProducts();
        });
        return btn;
    }

    // ── products ──────────────────────────────────────────────────────────────

    async #loadProducts() {
        this.#abortController?.abort();
        this.#abortController = new AbortController();
        this.#setLoading(true);

        try {
            let url = `${this.#options.productSearchUrl}?q=${encodeURIComponent(this.#searchQuery)}`;
            if (this.#activeCategory) url += `&categoryId=${encodeURIComponent(this.#activeCategory)}`;
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
        row.className = 'row g-2';

        products.forEach(p => {
            const col = document.createElement('div');
            col.className = this.#options.colClass;

            const picHtml = p.picture
                ? `<img src="${_esc(p.picture)}" class="pb-product-img" alt="${_esc(p.name)}" />`
                : `<div class="pb-product-img-placeholder"><i class="bi bi-box-seam text-muted fs-4"></i></div>`;

            const qtyHtml = p.availableQty > 0
                ? `<span class="text-success fw-medium">${DecimalFields.formatQuantity ? DecimalFields.formatQuantity(p.availableQty) : p.availableQty}</span>`
                : `<span class="text-muted">0</span>`;

            const catHtml = p.categoryName
                ? `<div class="pb-product-category text-truncate">${_esc(p.categoryName)}</div>`
                : '';

            col.innerHTML = `
                <div class="pb-product-card">
                    <div class="pb-product-thumb">${picHtml}</div>
                    <div class="pb-product-info">
                        <div class="pb-product-name fw-medium text-truncate" title="${_esc(p.name)}">${_esc(p.name)}</div>
                        ${catHtml}
                        <div class="pb-product-stock small"><i class="bi bi-boxes me-1 text-muted"></i>Tồn: ${qtyHtml}</div>
                    </div>
                    <button type="button" class="pb-add-btn btn btn-sm btn-outline-primary" title="Thêm vào phiếu">
                        <i class="bi bi-plus-lg"></i>
                    </button>
                </div>`;

            col.querySelector('.pb-add-btn')?.addEventListener('click', () => this.#onAdd(p));
            row.appendChild(col);
        });

        grid.appendChild(row);
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    #setLoading(on) {
        this.#container.querySelector('.pb-spinner')?.classList.toggle('d-none', !on);
        this.#container.querySelector('.pb-search-icon')?.classList.toggle('d-none', on);
    }
}

// private module-level helper (not exported)
function _esc(str) {
    return String(str ?? '')
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;');
}

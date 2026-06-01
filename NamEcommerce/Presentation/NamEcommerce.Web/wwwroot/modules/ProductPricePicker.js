import { apiGet } from "/modules/ajax-helper.js";
import { toast } from "/modules/modals.js";

export class ProductPriceController {
    #url;
    #productPricesMap;

    constructor(url) {
        this.#productPricesMap = new Map();
        this.#url = url;
    }

    async requestPrices(productId) {
        if (!productId) {
            throw new Error('productId is required');
        }

        let recentPrices = this.#productPricesMap.get(productId);
        if (Array.isArray(recentPrices))
            return recentPrices;

        try {
            const result = await apiGet(`${this.#url}${productId}`);
            const payload = result.data ?? result;
            recentPrices = Array.isArray(payload) ? payload : (payload.items ?? []);
            this.#productPricesMap.set(productId, recentPrices);
        } catch {
            toast('Lỗi', 'Lỗi khi lấy dữ liệu giá của sản phẩm', 'error');
            recentPrices = [];
        }

        return recentPrices;
    }

    async getProductCostOfVendor(productId, vendorId) {
        if (!productId) {
            throw new Error('productId is required');
        }
        if (!vendorId) {
            throw new Error('vendorId is required');
        }

        const recentPrices = await this.requestPrices(productId);
        if (recentPrices.length == 0)
            return;

        const match = recentPrices.find(p => p.vendorId === vendorId);
        if (match) {
            return match.unitCost;
        }
    }

    async getProductPriceForCustomer(productId, customerId) {
        if (!productId) {
            throw new Error('productId is required');
        }

        const params = new URLSearchParams({ ProductId: productId });
        if (customerId)
            params.set('CustomerId', customerId);

        const result = await apiGet(`/Product/SalePriceReference?${params.toString()}`);
        const payload = result.data ?? result;
        return payload.suggestedPrice ?? 0;
    }
}

export class ProductPricePicker {
    #options;
    #controller;
    #target;

    #currentProduct;

    constructor(target, options) {
        if (!(target instanceof HTMLElement))
            throw new TypeError('ProductPricePicker: target must be an HTMLElement');
        this.#target = target;

        this.#options = Object({
            url: '/Product/PurchasePriceReference?ProductId='
        }, options)

        this.#controller = new ProductPriceController(this.#options.url);

        this.#init();
    }

    setProduct(productId) {
        this.#currentProduct = productId;
    }

    #init() {
        this.#target.classList.add('d-none');
        this.#target.innerHTML = this.#template();
    }

    #template() {
        return `
            <div id="recentPricesAutoFillInfo" class="d-none small text-success mb-1">
                <i class="bi bi-check-circle-fill me-1"></i>
                <span id="recentPricesAutoFillText"></span>
            </div>
            <div class="border rounded-3 bg-light p-2 mt-1">
                <div class="small fw-semibold text-muted mb-1">
                    <i class="bi bi-clock-history me-1"></i> Giá nhập gần nhất theo nhà cung cấp
                </div>
                <div id="recentPricesTableWrapper">
                    <table class="table table-sm table-borderless mb-0 small" id="recentPricesTable">
                        <thead class="text-muted">
                            <tr>
                                <th class="ps-0 py-1">Nhà cung cấp</th>
                                <th class="text-end py-1">Giá nhập</th>
                                <th class="text-end py-1 text-nowrap">Ngày</th>
                            </tr>
                        </thead>
                        <tbody id="recentPricesTbody"></tbody>
                    </table>
                </div>
                <div id="recentPricesEmpty" class="text-muted fst-italic small d-none py-1">
                    Chưa có lịch sử nhập hàng cho sản phẩm này.
                </div>
            </div>
        `;
    }
}

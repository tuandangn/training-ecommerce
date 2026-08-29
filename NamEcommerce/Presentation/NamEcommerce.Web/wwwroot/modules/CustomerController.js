import { apiGet, apiPost } from '/modules/ajax-helper.js';

export default class CustomerQuickCreateController {
    #QUICK_CREATE_URL = '/Customer/QuickCreate';

    #root;
    #form;
    #modal;

    #waitingPromise;
    #resolveFn;
    #rejectFn;

    constructor() {
        this.#waitingPromise = new Promise((resolve, reject) => {
            this.#resolveFn = resolve;
            this.#rejectFn = reject;
        }).then((result) => {
            this.#modal?.hide();
            return result;
        });

        (async () => {
            const modalHtml = await this.#loadQuickCreateModal();
            if (!modalHtml) {
                this.#rejectFn();
                return;
            }
            appendToPage(modalHtml);

            this.#root = document.getElementById('quickCustomerModal');
            this.#root.addEventListener('hidden.bs.modal', () => {
                this.#root.remove();
                this.#rejectFn();
            }, { once: true });

            this.#form = this.#root.querySelector('form');
            reparseForm(this.#form);

            this.#modal = bootstrap.Modal.getOrCreateInstance(this.#root);
            this.#modal.show();

            this.#bindEvents();
        })();
    }
    #bindEvents() {
        this.#form.addEventListener('submit', async (event) => {
            event.preventDefault();

            if (!isFormValid(this.#form)) {
                return;
            }

            const submitButton = this.#form.querySelector('button[type="submit"]');
            submitButton.disabled = true;

            try {
                const result = await apiPost(this.#form.action, new FormData(this.#form));
                if (!result.success) {
                    if (result.message) this.#showAlert('error', result.message);
                    return;
                }
                this.#showAlert('success', 'Thêm khách hàng mới thành công');
                this.#resolveFn(result.customer);
            } catch {
                this.#rejectFn('error', 'Có lỗi xảy ra khi tạo khách hàng.');
            } finally {
                submitButton.disabled = false;
            }
        });
    }

    async #loadQuickCreateModal() {
        const response = await apiGet(this.#QUICK_CREATE_URL);
        if (!response || (typeof response == 'object' && !response.success))
            return;
        return response;
    }
    #showAlert(type, message) {
        window.NotificationCenter[type](message, 'warning');
    }

    getCreatedCustomer() {
        return Promise.resolve(this.#waitingPromise);
    }
}

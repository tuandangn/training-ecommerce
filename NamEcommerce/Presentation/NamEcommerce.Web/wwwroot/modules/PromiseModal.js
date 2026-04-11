export default class PromiseModal {
    constructor(modalElement) {
        this._element =
            typeof modalElement === 'string'
                ? document.querySelector(modalElement)
                : modalElement;

        if (!this._element) throw new Error('Modal element not found');

        this._instance = bootstrap.Modal.getOrCreateInstance(this._element);
    }

    /**
     * Hiển thị modal và trả về Promise khi hoàn tất hiệu ứng mở.
     * @returns {Promise<void>}
     */
    show() {
        return new Promise((resolve) => {
            if (this._isShown()) return resolve();

            const onShown = () => {
                this._element.removeEventListener('shown.bs.modal', onShown);
                resolve();
            };

            this._element.addEventListener('shown.bs.modal', onShown);
            this._instance.show();
        });
    }

    /**
     * Đóng modal và trả về Promise khi hoàn tất hiệu ứng đóng.
     * @returns {Promise<void>}
     */
    hide() {
        return new Promise((resolve) => {
            if (!this._isShown()) return resolve();

            const onHidden = () => {
                this._element.removeEventListener('hidden.bs.modal', onHidden);
                resolve();
            };

            this._element.addEventListener('hidden.bs.modal', onHidden);
            this._instance.hide();
        });
    }

    /**
     * Toggle hiển thị/ẩn modal.
     * @returns {Promise<void>}
     */
    toggle() {
        return this._isShown() ? this.hide() : this.show();
    }

    /**
     * Dọn dẹp instance Bootstrap Modal.
     */
    dispose() {
        this._instance.dispose();
    }

    // --- Private ---

    _isShown() {
        return this._element.classList.contains('show');
    }
}
import ItemEditOffcanvas from './ItemEditOffcanvas.js';
import ItemEditModal from './ItemEditModal.js';

export default class ItemEditor {
    #offcanvas;
    #modal;

    // opts: { showPrice?: boolean, qtyLabel?: string }
    constructor(offcanvasEl, modalEl, opts = {}) {
        this.#offcanvas = offcanvasEl ? new ItemEditOffcanvas(offcanvasEl, opts) : null;
        this.#modal = modalEl ? new ItemEditModal(modalEl, opts) : null;
    }

    // item: { name, picture?, quantity, unitPrice, quantityDecimalPlaces, priceLabel? }
    // callbacks: { onApply(qty, price), onDelete() }
    open(item, callbacks = {}) {
        const isDesktop = window.matchMedia('(min-width: 992px)').matches;
        (isDesktop ? this.#modal : this.#offcanvas)?.open(item, callbacks);
    }
}

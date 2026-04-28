import ProductPicker from "/modules/ProductPicker.js";
import ProductBrowser from "/modules/ProductBrowser.js";
import { getWarehouseSettings } from "/modules/Settings.js";

// ─── Controller ───────────────────────────────────────────────────────────────

export default class GoodsReceiptCreateController {
    #state = {};

    #productPicker = null;
    #browser = null;
    #picturePicker = null;

    #warehouseSettings;
    #warehouseOptions;

    constructor() {
        this.#warehouseSettings = getWarehouseSettings();

        this.#bindModal();
        const warehouseSelect = document.getElementById('WarehouseId');
        warehouseSelect.addEventListener('change', (e) => {
            this.#setState({ warehouse: e.target.value });
        });
        const warehouse = warehouseSelect.value;

        this.#bindTableEvents();

        const browserEl = document.getElementById('productBrowser');
        if (browserEl) {
            this.#browser = new ProductBrowser(
                browserEl,
                (product) => this.#addOrIncrementItem(product)
            );
            this.#browser.init();
        }

        this.#picturePicker = new PicturePicker({
            listEl: document.getElementById('pictureList'),
            pickerEl: document.getElementById('picturePicker'),
            hiddenContainer: document.getElementById('pictureHiddenContainer'),
            defaultSlots: 2,
            maxFiles: 2,
        });
        this.#picturePicker.init();

        this.#setState({ warehouse });

        this.#warehouseOptions = Array.from(warehouseSelect.querySelectorAll('option')).filter(option => option.value)
            .map(option => ({ value: option.value, label: option.label }));
    }

    #setState(patch) {
        this.#state = Object.assign({}, this.#state, patch);
    }

    #bindModal() {
        const pickerEl = document.getElementById('productPicker');
        if (!pickerEl) return;

        this.#productPicker = new ProductPicker(pickerEl);

        pickerEl.addEventListener('select', (e) => {
            const product = e.detail?.product;
            if (product) {
                document.getElementById('modalProductId').value = product.id;
                pickerEl.dataset.selectedName = product.name;
                pickerEl.dataset.selectedPicture = product.picture ?? '';
                document.getElementById('modalProductDetails')?.classList.remove('d-none');
                document.getElementById('btnAddItemConfirm')?.classList.remove('d-none');
                document.getElementById('productPickerError')?.classList.add('d-none');
            }
        });

        pickerEl.addEventListener('remove', () => {
            document.getElementById('modalProductId').value = '';
            document.getElementById('modalProductDetails')?.classList.add('d-none');
            document.getElementById('btnAddItemConfirm')?.classList.add('d-none');
        });
        document.getElementById('modalWarehouseId')?.addEventListener('change', (e) => {
            if (!this.#warehouseSettings.AllowNonWarehouse && !e.target.value) {
                document.getElementById('modalWarehouseError')?.classList.remove('d-none');
                return;
            }
            document.getElementById('modalWarehouseError')?.classList.add('d-none');
        });
        document.getElementById('btnAddItemConfirm')?.addEventListener('click', () => {
            const productId = document.getElementById('modalProductId')?.value;
            const productName = pickerEl.dataset.selectedName ?? '—';
            const productPicture = pickerEl.dataset.selectedPicture ?? '';
            const warehouseId = document.getElementById('modalWarehouseId')?.value ?? '';
            const qtyRaw = document.getElementById('modalQty')?.value ?? '0';
            const costRaw = document.getElementById('modalUnitCost')?.value ?? '';

            if (!productId) {
                document.getElementById('productPickerError')?.classList.remove('d-none');
                return;
            }

            const qty = parseFloat(DecimalFields.stripFormatting(qtyRaw, 2));
            if (!qty || qty <= 0) {
                document.getElementById('modalQtyError')?.classList.remove('d-none');
                return;
            }

            if (!this.#warehouseSettings.AllowNonWarehouse && !warehouseId) {
                document.getElementById('modalWarehouseError')?.classList.remove('d-none');
                return;
            }

            const cost = costRaw ? (parseFloat(DecimalFields.stripFormatting(costRaw)) || null) : null;
            if (cost < 0) {
                document.getElementById('modalUnitCostError')?.classList.remove('d-none');
                return;
            }


            this.#addItemToTable(
                { id: productId, name: productName, picture: productPicture },
                qty, cost, warehouseId
            );

            bootstrap.Modal.getOrCreateInstance(document.getElementById('addItemModal')).hide();
            this.#resetModal();
        });

        document.getElementById('addItemModal')?.addEventListener('show.bs.modal', () => {
            if (this.#state.warehouse) {
                document.getElementById('modalWarehouseId').disabled = true;
                document.getElementById('modalWarehouseId').value = this.#state.warehouse;
            }
        });
        document.getElementById('addItemModal')?.addEventListener('hidden.bs.modal', () => this.#resetModal());
    }

    #bindTableEvents() {
        document.getElementById('itemsTableBody')?.addEventListener('click', (e) => {
            const btn = e.target.closest('.btn-remove-item');
            if (!btn) return;
            btn.closest('tr').remove();
            this.#updateNoItemsVisibility();
            this.#reindexTableRows();
        });
    }
    #updateNoItemsVisibility() {
        const tbody = document.getElementById('itemsTableBody');
        const msg = document.getElementById('noItemsMessage');
        if (!tbody || !msg) return;
        msg.style.display = tbody.querySelectorAll('tr').length > 0 ? 'none' : 'block';
    }
    #reindexTableRows() {
        const tbody = document.getElementById('itemsTableBody');
        if (!tbody) return;

        // Lấy tất cả các thẻ <tr> đang có trong bảng
        const rows = tbody.querySelectorAll('tr');

        // Lặp qua từng dòng và đánh lại index từ 0, 1, 2...
        rows.forEach((row, newIndex) => {
            // 1. Cập nhật lại ID của dòng (tuỳ chọn nhưng nên làm cho đồng bộ)
            row.id = `item-row-${newIndex}`;

            // 2. Cập nhật name cho tất cả thẻ input, select, textarea
            const inputs = row.querySelectorAll('input, select, textarea');
            inputs.forEach(input => {
                if (input.name) {
                    // Tìm mẫu "Items[số_bất_kỳ]" và thay bằng "Items[newIndex]"
                    input.name = input.name.replace(/Items\[\d+\]/, `Items[${newIndex}]`);
                }
            });

            // 3. Cập nhật data-valmsg-for cho các thẻ span hiển thị lỗi validation của ASP.NET
            const validationSpans = row.querySelectorAll('.field-validation-valid, .field-validation-error');
            validationSpans.forEach(span => {
                const valmsgFor = span.getAttribute('data-valmsg-for');
                if (valmsgFor) {
                    span.setAttribute(
                        'data-valmsg-for',
                        valmsgFor.replace(/Items\[\d+\]/, `Items[${newIndex}]`)
                    );
                }
            });
        });
        const form = document.getElementById('#createGoodsReceiptForm');
        $(form).removeData('validator').removeData('unobtrusiveValidation');
        $.validator.unobtrusive.parse(form);
    }

    #addOrIncrementItem(product) {
        const tbody = document.getElementById('itemsTableBody');
        if (!tbody) return;

        const existingRow = Array.from(tbody.querySelectorAll('tr')).find(row => {
            const idInput = row.querySelector('.item-product-id');
            return idInput && idInput.value === product.id;
        });

        if (existingRow) {
            const qtyInput = existingRow.querySelector('.item-qty');
            if (qtyInput) {
                const current = parseFloat(DecimalFields.stripFormatting(qtyInput.value, 2)) || 0;
                const newQty = current + 1;
                qtyInput.value = DecimalFields.formatQuantity
                    ? DecimalFields.formatQuantity(newQty) : newQty;
                qtyInput.dispatchEvent(new Event('change', { bubbles: true }));
            }
            existingRow.classList.add('table-success');
            setTimeout(() => existingRow.classList.remove('table-success'), 700);
            return;
        }

        this.#addItemToTable(product, 1, null, this.#state.warehouse);
        const newRow = tbody.lastElementChild;
        if (newRow) {
            newRow.classList.add('table-success');
            setTimeout(() => newRow.classList.remove('table-success'), 700);
        }
    }

    #addItemToTable(product, quantity, unitCost, warehouseId = '') {
        const tbody = document.getElementById('itemsTableBody');
        if (!tbody) return;

        const existingInputs = tbody.querySelectorAll('input[name^="Items["]');
        let maxIndex = -1;
        existingInputs.forEach(inp => {
            const m = inp.name.match(/Items\[(\d+)\]/);
            if (m) maxIndex = Math.max(maxIndex, parseInt(m[1]));
        });
        const i = maxIndex + 1;

        const qtyFormatted = DecimalFields.formatQuantity ? DecimalFields.formatQuantity(quantity) : quantity;
        const costFormatted = unitCost != null
            ? (DecimalFields.formatCurrency ? DecimalFields.formatCurrency(unitCost) : unitCost) : '';

        const pictureHtml = product.picture
            ? `<img src="${product.picture}" class="rounded me-2 product-picture"
                    style="width:36px;height:36px;object-fit:cover;flex-shrink:0;" alt="${escapeHtml(product.name)}" />`
            : `<div class="d-flex align-items-center justify-content-center rounded bg-light me-2"
                    style="width:36px;height:36px;flex-shrink:0;">
                    <i class="bi bi-image text-muted small"></i>
               </div>`;

        const row = document.createElement('tr');
        row.id = `item-row-${i}`;
        row.innerHTML = `
            <td class="ps-3">
                <div class="d-flex align-items-center">
                    ${pictureHtml}
                    <div>
                        <div class="fw-medium item-product-name">${escapeHtml(product.name)}</div>
                    </div>
                </div>
                <input type="hidden" name="Items[${i}].ProductId" value="${escapeHtml(product.id)}" class="item-product-id" />
            </td>
            <td class="text-center">
                <input name="Items[${i}].Quantity" value="${qtyFormatted}"
                       class="form-control form-control-sm text-end item-qty no-additional-element"
                       data-decimal="quantity" min="0.001" placeholder="0"
                       data-val="true"
                       data-val-required="Vui lòng nhập số lượng."
                       data-val-range="Số lượng phải lớn hơn 0."
                       data-val-range-min="0.001"
                       data-val-number="Số lượng không đúng." />
                <span class="small text-danger field-validation-valid"
                      data-valmsg-for="Items[${i}].Quantity" data-valmsg-replace="true"></span>
            </td>
            <td>
                <select id="Items[${i}]_WarehouseId" name="Items[${i}].WarehouseId" class="form-select item-warehouse" ${this.#state.warehouse ? 'disabled' : ''} data-val="${!this.#warehouseSettings.AllowNonWarehouse}" data-required="Vui lòng chọn kho hàng">
                    ${this.#warehouseSettings.AllowNonWarehouse ? `<option value="" ${warehouseId ? '' : 'selected'}>(Không chọn)</option>` : ''}
                    ${this.#warehouseOptions.map(option => `<option value="${option.value}" ${option.value == warehouseId ? 'selected' : ''}>${option.label}</option>`).join('')}
                </select>
                <span class="small text-danger field-validation-valid"
                      data-valmsg-for="Items[${i}].WarehouseId" data-valmsg-replace="true"></span>
            </td>
            <td class="text-end">
                <input name="Items[${i}].UnitCost" value="${costFormatted}"
                       class="form-control form-control-sm text-end item-unit-cost no-additional-element no-hint"
                       data-decimal="currency" placeholder="(Không rõ)" min="0"
                       data-val-range="Đơn giá không được âm."
                       data-val-range-min="0"
                       data-val-number="Đơn giá không đúng." />
                <span class="small text-danger field-validation-valid"
                      data-valmsg-for="Items[${i}].UnitCost" data-valmsg-replace="true"></span>
            </td>
            <td class="text-end pe-3">
                <button type="button" class="btn-table-action danger border-0 bg-transparent shadow-none btn-remove-item">
                    <i class="bi bi-trash"></i>
                </button>
            </td>`;

        tbody.appendChild(row);
        DecimalFields.autoWrap?.(row);
        this.#updateNoItemsVisibility();

        const form = document.getElementById('#createGoodsReceiptForm');
        $(form).removeData('validator').removeData('unobtrusiveValidation');
        $.validator.unobtrusive.parse(form);
    }

    #resetModal() {
        document.getElementById('modalProductId').value = '';
        if (this.#warehouseSettings.AllowNonWarehouse) {
            document.getElementById('modalWarehouseId').value = '';
        }
        document.getElementById('modalQty').value = '1';
        if (document.getElementById('modalUnitCost'))
            document.getElementById('modalUnitCost').value = '';
        document.getElementById('productPickerError')?.classList.add('d-none');
        document.querySelector('#modalProductDetails .currency-hint')?.classList.remove('visible');
        document.getElementById('modalQtyError')?.classList.add('d-none');
        document.getElementById('modalUnitCostError')?.classList.add('d-none');
        document.getElementById('modalWarehouseError')?.classList.add('d-none');
        document.getElementById('modalProductDetails')?.classList.add('d-none');
        document.getElementById('btnAddItemConfirm')?.classList.add('d-none');
        this.#productPicker?.clear();
    }
}

// ─── PicturePicker (ảnh chứng từ) ────────────────────────────────────────────

class PicturePicker {
    #listEl;
    #pickerEl;
    #hiddenContainer;
    #defaultSlots;
    #maxFiles;
    #uploadedCount = 0;

    constructor({ listEl, pickerEl, hiddenContainer, defaultSlots = 2, maxFiles = 2 }) {
        this.#listEl = listEl;
        this.#pickerEl = pickerEl;
        this.#hiddenContainer = hiddenContainer;
        this.#defaultSlots = defaultSlots;
        this.#maxFiles = maxFiles;
    }

    init() {
        if (!this.#pickerEl) return;

        // Render existing pictures (khi POST thất bại, pictureIds đã được submit)
        const existingInputs = this.#hiddenContainer?.querySelectorAll('input[name="PictureIds"]') ?? [];
        const existingPictureIds = Array.from(existingInputs).map(inp => inp.value);

        // Xóa hidden inputs cũ (sẽ được tạo lại bởi component)
        if (this.#hiddenContainer) this.#hiddenContainer.innerHTML = '';

        existingPictureIds.forEach(pictureId => {
            if (pictureId) this.#addExistingPicture(pictureId);
        });

        this.#renderSlots();
    }

    #renderSlots() {
        if (!this.#pickerEl) return;
        this.#pickerEl.innerHTML = '';

        const remaining = this.#maxFiles - this.#uploadedCount;
        const slotsToShow = Math.min(this.#defaultSlots, remaining);

        for (let i = 0; i < slotsToShow; i++) {
            this.#pickerEl.appendChild(this.#buildSlot());
        }

        if (slotsToShow === 0) {
            //this.#pickerEl.innerHTML = `<span class="text-muted small">Đã đạt tối đa ${this.#maxFiles} ảnh.</span>`;
        }
    }

    #buildSlot() {
        const slot = document.createElement('div');
        slot.className = 'picture-upload-slot';
        slot.innerHTML = `
            <label class="picture-upload-label">
                <input type="file" class="d-none picture-upload-input" accept="image/*" />
                <div class="picture-upload-placeholder">
                    <i class="bi bi-camera text-muted fs-5"></i>
                    <span class="visually-hidden">Tải ảnh lên</div>
                </div>
            </label>`;

        const input = slot.querySelector('.picture-upload-input');
        input?.addEventListener('change', async (e) => {
            const file = e.target.files?.[0];
            if (!file) return;
            await this.#uploadFile(file, slot);
        });

        // Drag & drop
        slot.addEventListener('dragover', (e) => {
            e.preventDefault();
            e.stopPropagation();
            slot.classList.add('drag-over');
        });
        slot.addEventListener('dragleave', () => slot.classList.remove('drag-over'));
        slot.addEventListener('drop', async (e) => {
            e.preventDefault();
            e.stopPropagation();
            slot.classList.remove('drag-over');
            const file = e.dataTransfer?.files?.[0];
            if (file) await this.#uploadFile(file, slot);
        });

        return slot;
    }

    async #uploadFile(file, slot) {
        const placeholder = slot.querySelector('.picture-upload-placeholder');
        if (placeholder) {
            placeholder.innerHTML = `
                <div class="spinner-border spinner-border-sm text-primary" role="status"></div>
                <div class="small text-muted mt-1">Đang tải...</div>`;
        }

        try {
            const formData = new FormData();
            formData.append('file', file);
            const res = await fetch('/Picture/Upload', {
                method: 'POST',
                headers: { 'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? '' },
                body: formData
            });
            const data = await res.json();

            if (!data.success) {
                if (placeholder) {
                    placeholder.innerHTML = `<i class="bi bi-x-circle text-danger"></i><div class="small text-danger mt-1">${escapeHtml(data.message ?? 'Lỗi')}</div>`;
                }
                return;
            }

            // Thành công: chuyển slot thành thumbnail
            this.#convertSlotToThumbnail(slot, data.id, data.dataUrl);
            this.#uploadedCount++;

            // Thêm vào listEl
            this.#addThumbnailToList(data.id, data.dataUrl);

            // Thêm hidden input
            this.#addHiddenInput(data.id);

            // Render thêm slot mới
            this.#renderSlots();

        } catch {
            if (placeholder) {
                placeholder.innerHTML = `<i class="bi bi-x-circle text-danger"></i><div class="small text-danger mt-1">Lỗi kết nối</div>`;
            }
        }
    }

    #convertSlotToThumbnail(slot, id, dataUrl) {
        // Ẩn slot sau khi upload thành công
        slot.remove();
    }

    #addExistingPicture(id) {
        this.#addThumbnailToList(id, `/Picture/${id}`);
        this.#addHiddenInput(id);
        this.#uploadedCount++;
    }

    #addThumbnailToList(id, src) {
        if (!this.#listEl) return;

        const item = document.createElement('div');
        item.className = 'picture-thumbnail-item';
        item.dataset.pictureId = id;
        item.innerHTML = `
            <img src="${escapeHtml(src)}" alt="Chứng từ"
                 style="width:100px;height:100px;object-fit:cover;border-radius:8px;border:1px solid var(--bs-border-color);" />
            <button type="button" class="btn-remove-picture"
                    title="Xóa ảnh" aria-label="Xóa ảnh">
                <i class="bi bi-x-lg"></i>
            </button>`;

        item.querySelector('.btn-remove-picture')?.addEventListener('click', () => {
            this.#removePicture(id, item);
        });

        this.#listEl.appendChild(item);
    }

    #addHiddenInput(id) {
        if (!this.#hiddenContainer) return;
        const input = document.createElement('input');
        input.type = 'hidden';
        input.name = 'PictureIds';
        input.value = id;
        this.#hiddenContainer.appendChild(input);
    }

    #removePicture(id, thumbnailItem) {
        thumbnailItem.remove();
        this.#hiddenContainer?.querySelector(`input[value="${id}"]`)?.remove();
        this.#uploadedCount = Math.max(0, this.#uploadedCount - 1);
        this.#renderSlots();
    }
}
// ─── Helpers ──────────────────────────────────────────────────────────────────

function escapeHtml(str) {
    return String(str ?? '')
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;');
}

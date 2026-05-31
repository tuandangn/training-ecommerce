// image-uploader.js — Shared pre-upload image control.
// Upload files to /Picture/Upload immediately on select/drop, then submit Guid[] IDs with form.
// Init via: new ImageUploader({ container, fieldName, maxFiles, required, existingIds }).init()
// Or zero-config auto-init: <div data-image-uploader data-field-name="PictureIds" ...>

const UPLOAD_URL = '/Picture/Upload';

export class ImageUploader {
    #container;
    #fieldName;
    #maxFiles;
    #required;
    #existingIds;
    #uploadedCount = 0;
    #abortController = null;

    constructor({ container, fieldName = 'PictureIds', maxFiles = 1, required = false, existingIds = [] } = {}) {
        this.#container = container;
        this.#fieldName = fieldName;
        this.#maxFiles = maxFiles;
        this.#required = required;
        this.#existingIds = existingIds;
    }

    init() {
        if (!this.#container) return;
        this.#container.innerHTML = '';
        this.#uploadedCount = 0;

        const thumbnails = document.createElement('div');
        thumbnails.className = 'img-uploader__thumbnails';

        const slots = document.createElement('div');
        slots.className = 'img-uploader__slots';

        this.#container.appendChild(thumbnails);
        this.#container.appendChild(slots);

        this.#existingIds.filter(Boolean).forEach(id => this.#addExisting(id, thumbnails, slots));
        this.#renderSlots(slots);
        this.#updateSentinel();
    }

    destroy() {
        this.#abortController?.abort();
        if (this.#container) this.#container.innerHTML = '';
    }

    getCount() { return this.#uploadedCount; }

    #renderSlots(slotsEl) {
        if (!slotsEl) return;
        slotsEl.innerHTML = '';
        const remaining = this.#maxFiles - this.#uploadedCount;
        for (let i = 0; i < remaining; i++) {
            slotsEl.appendChild(this.#buildSlot(slotsEl));
        }
    }

    #buildSlot(slotsEl) {
        const slot = document.createElement('div');
        slot.className = 'img-uploader__slot';
        slot.innerHTML = `
            <label class="img-uploader__label" title="Nhấn hoặc kéo ảnh vào đây">
                <input type="file" class="d-none" accept="image/*" />
                <div class="img-uploader__placeholder">
                    <i class="bi bi-camera fs-4 text-muted"></i>
                    <span class="img-uploader__hint">Chọn ảnh</span>
                </div>
            </label>`;

        const input = slot.querySelector('input[type=file]');
        input.addEventListener('change', async e => {
            const file = e.target.files?.[0];
            if (file) await this.#upload(file, slot, slotsEl);
        });

        const label = slot.querySelector('.img-uploader__label');
        label.addEventListener('dragover', e => { e.preventDefault(); label.classList.add('drag-over'); });
        label.addEventListener('dragleave', () => label.classList.remove('drag-over'));
        label.addEventListener('drop', async e => {
            e.preventDefault();
            label.classList.remove('drag-over');
            const file = e.dataTransfer?.files?.[0];
            if (file) await this.#upload(file, slot, slotsEl);
        });

        return slot;
    }

    async #upload(file, slot, slotsEl) {
        const placeholder = slot.querySelector('.img-uploader__placeholder');
        if (placeholder) placeholder.innerHTML = `<div class="spinner-border spinner-border-sm text-primary"></div><div class="small text-muted mt-1">Đang tải...</div>`;

        try {
            const fd = new FormData();
            fd.append('file', file);
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? '';
            const res = await fetch(UPLOAD_URL, { method: 'POST', headers: { RequestVerificationToken: token }, body: fd });
            const data = await res.json();

            if (!data.success) {
                if (placeholder) placeholder.innerHTML = `<i class="bi bi-x-circle text-danger"></i><div class="small text-danger mt-1">${_esc(data.message ?? 'Lỗi')}</div>`;
                return;
            }

            slot.remove();
            this.#addThumbnail(data.id, data.dataUrl, slotsEl.previousElementSibling, slotsEl);
            this.#uploadedCount++;
            this.#renderSlots(slotsEl);
            this.#updateSentinel();
        } catch {
            if (placeholder) placeholder.innerHTML = `<i class="bi bi-x-circle text-danger"></i><div class="small text-danger mt-1">Lỗi kết nối</div>`;
        }
    }

    #addExisting(id, thumbnailsEl, slotsEl) {
        this.#addThumbnail(id, `/Picture/${id}`, thumbnailsEl, slotsEl);
        this.#addHidden(id);
        this.#uploadedCount++;
    }

    #addThumbnail(id, src, thumbnailsEl, slotsEl) {
        const item = document.createElement('div');
        item.className = 'img-uploader__thumb';
        item.dataset.pictureId = id;
        item.innerHTML = `
            <img src="${_esc(src)}" alt="Ảnh" class="img-uploader__thumb-img" />
            <button type="button" class="img-uploader__remove" title="Xóa" aria-label="Xóa ảnh">
                <i class="bi bi-x-lg"></i>
            </button>`;

        item.querySelector('.img-uploader__remove').addEventListener('click', () => {
            item.remove();
            this.#container.querySelector(`input[name="${this.#fieldName}"][value="${id}"]`)?.remove();
            this.#uploadedCount = Math.max(0, this.#uploadedCount - 1);
            this.#renderSlots(slotsEl);
            this.#updateSentinel();
        });

        thumbnailsEl.appendChild(item);
        this.#addHidden(id);
    }

    #addHidden(id) {
        const input = document.createElement('input');
        input.type = 'hidden';
        input.name = this.#fieldName;
        input.value = id;
        this.#container.appendChild(input);
    }

    #updateSentinel() {
        const sentinel = this.#container.closest('[data-image-uploader]')
            ?.parentElement?.querySelector(`input[data-img-uploader-sentinel="${this.#fieldName}"]`)
            ?? this.#container.parentElement?.querySelector(`input[data-img-uploader-sentinel="${this.#fieldName}"]`);
        if (sentinel) {
            sentinel.value = this.#uploadedCount > 0 ? '1' : '';
            // Trigger jQuery validation recheck
            if (typeof $ !== 'undefined') $(sentinel).valid?.();
        }
    }
}

// ── Auto-init ─────────────────────────────────────────────────────────────────

export function initImageUploaders(root = document) {
    root.querySelectorAll('[data-image-uploader]').forEach(el => {
        if (el._imageUploader) return;
        const uploader = new ImageUploader({
            container: el,
            fieldName: el.dataset.fieldName ?? 'PictureIds',
            maxFiles: parseInt(el.dataset.maxFiles ?? '1', 10),
            required: el.dataset.required === 'true',
            existingIds: (el.dataset.existingIds ?? '').split(',').filter(Boolean)
        });
        uploader.init();
        el._imageUploader = uploader;
    });
}

function _esc(str) {
    return String(str).replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
}

// Auto-init on DOMContentLoaded if used as plain script tag
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => initImageUploaders());
} else {
    initImageUploaders();
}

import DecimalFields from '/modules/DecimalFields.js';

const deviceIdKey = 'nam-delivery-device-id';
const dbName = 'nam-delivery-mobile';
const dbVersion = 1;
const pendingStoreName = 'pending-completions';

function newClientId() {
    return crypto.randomUUID ? crypto.randomUUID() : `${Date.now()}-${Math.random()}`;
}

function getDeviceId() {
    const existing = localStorage.getItem(deviceIdKey);
    if (existing) {
        return existing;
    }

    const next = newClientId();
    localStorage.setItem(deviceIdKey, next);
    return next;
}

function getDeliveryNoteSyncKey(noteId) {
    const key = `delivery-note-sync-key:${noteId}`;
    const existing = localStorage.getItem(key);
    if (existing) {
        return existing;
    }

    const next = newClientId();
    localStorage.setItem(key, next);
    return next;
}

function updateCacheStatus(text, className) {
    const status = document.getElementById('delivery-cache-status');
    if (!status) {
        return;
    }

    status.textContent = text;
    if (!status.classList.contains('visually-hidden')) {
        status.className = `fw-bold ${className}`;
    }
}

export function installDeliveryIndexCache() {
    const payloadElement = document.getElementById('delivery-index-payload');
    if (!payloadElement?.textContent) {
        return;
    }

    localStorage.setItem('delivery-runs:index', payloadElement.textContent);
    localStorage.setItem('delivery-runs:index:cached-on', new Date().toISOString());
}

function getCurrentPosition() {
    if (!navigator.geolocation) {
        return Promise.resolve(null);
    }

    return new Promise(resolve => {
        navigator.geolocation.getCurrentPosition(
            position => resolve(position),
            () => resolve(null),
            { enableHighAccuracy: true, timeout: 6000, maximumAge: 300000 });
    });
}

function parseQuantity(value) {
    const parsed = Number(String(value ?? '').replace(',', '.'));
    return Number.isFinite(parsed) ? parsed : 0;
}

function formatQuantity(value) {
    return Number(value || 0).toLocaleString('vi-VN', { maximumFractionDigits: 2 });
}

function updateAcceptedQuantity(line) {
    const quantity = parseQuantity(line.dataset.quantity);
    const input = line.querySelector('.returned-quantity-input');
    const accepted = line.querySelector('.accepted-quantity-text');
    if (!input || !accepted) {
        return;
    }

    let returned = DecimalFields.getValue(input);
    if (returned < 0) returned = 0;
    if (returned > quantity) returned = quantity;
    DecimalFields.setValue(input, returned);
    const decimalPlaces = parseInt(input.dataset.decimals || '2', 10);
    accepted.textContent = DecimalFields.formatQuantity(
        Math.max(0, quantity - returned),
        Number.isNaN(decimalPlaces) ? 2 : decimalPlaces);
}

function installReturnedQuantityControls(form) {
    form.querySelectorAll('.delivery-return-line').forEach(line => {
        updateAcceptedQuantity(line);
        line.querySelector('.returned-quantity-input')?.addEventListener('input', () => updateAcceptedQuantity(line));
    });
}

function prepareAcceptancePayload(form, status) {
    const lines = Array.from(form.querySelectorAll('.delivery-return-line'));
    const rejectReason = String(form.querySelector('input[name="rejectReason"]')?.value || '').trim();
    let hasReturnedQuantity = false;
    const items = lines.map(line => {
        const quantity = parseQuantity(line.dataset.quantity);
        const input = line.querySelector('.returned-quantity-input');
        let returned = input ? DecimalFields.getValue(input) : 0;
        if (returned < 0) returned = 0;
        if (returned > quantity) returned = quantity;
        if (returned > 0) hasReturnedQuantity = true;
        if (input) DecimalFields.setValue(input, returned);
        updateAcceptedQuantity(line);
        return {
            deliveryNoteItemId: line.dataset.itemId,
            returnedQuantity: returned,
            rejectReason: returned > 0 ? rejectReason : null
        };
    });

    if (hasReturnedQuantity && !rejectReason) {
        if (status) status.textContent = 'Vui lòng nhập lý do trả hàng.';
        return false;
    }

    const payload = form.querySelector('input[name="acceptanceItemsJson"]');
    if (payload) {
        payload.value = JSON.stringify(items);
    }
    return true;
}

function openDeliveryDb() {
    return new Promise((resolve, reject) => {
        const request = indexedDB.open(dbName, dbVersion);
        request.onupgradeneeded = () => {
            const db = request.result;
            if (!db.objectStoreNames.contains(pendingStoreName)) {
                db.createObjectStore(pendingStoreName, { keyPath: 'noteId' });
            }
        };
        request.onsuccess = () => resolve(request.result);
        request.onerror = () => reject(request.error);
    });
}

async function savePendingCompletion(form) {
    const noteId = form.dataset.noteId;
    const proofFile = form.querySelector('input[name="proofFile"]')?.files?.[0];
    if (!proofFile) {
        throw new Error('Proof file is required');
    }

    const fields = {};
    const formData = DecimalFields.getFormData(form);
    formData.forEach((value, key) => {
        if (key !== 'proofFile') {
            fields[key] = value;
        }
    });

    const db = await openDeliveryDb();
    await new Promise((resolve, reject) => {
        const tx = db.transaction(pendingStoreName, 'readwrite');
        tx.objectStore(pendingStoreName).put({
            noteId,
            action: form.action,
            fields,
            proofFile,
            proofFileName: proofFile.name,
            createdOn: new Date().toISOString()
        });
        tx.oncomplete = resolve;
        tx.onerror = () => reject(tx.error);
    });
    db.close();
}

async function getPendingCompletions() {
    const db = await openDeliveryDb();
    const items = await new Promise((resolve, reject) => {
        const tx = db.transaction(pendingStoreName, 'readonly');
        const request = tx.objectStore(pendingStoreName).getAll();
        request.onsuccess = () => resolve(request.result ?? []);
        request.onerror = () => reject(request.error);
    });
    db.close();
    return items;
}

async function deletePendingCompletion(noteId) {
    const db = await openDeliveryDb();
    await new Promise((resolve, reject) => {
        const tx = db.transaction(pendingStoreName, 'readwrite');
        tx.objectStore(pendingStoreName).delete(noteId);
        tx.oncomplete = resolve;
        tx.onerror = () => reject(tx.error);
    });
    db.close();
}

async function queueCompletion(form, status, message) {
    const submitButton = form.querySelector('button[type="submit"]');
    let queued = false;
    try {
        await savePendingCompletion(form);
        queued = true;
        localStorage.setItem(`delivery-note-sync-state:${form.dataset.noteId}`, 'pending');
        markCompleteFormPending(form, message);
        await updatePendingCount();
        if (status) {
            status.textContent = message;
            status.className = 'delivery-complete-status mobile-meta mt-2 text-warning fw-bold';
        }
    } catch {
        if (status) status.textContent = 'Không thể lưu trên máy';
    } finally {
        if (!queued && submitButton) submitButton.disabled = false;
    }
}

export async function installDeliveryRunCache({ runId, cacheUrl }) {
    const payloadElement = document.getElementById('delivery-run-payload');
    if (payloadElement?.textContent) {
        localStorage.setItem(`delivery-run:${runId}`, payloadElement.textContent);
    }

    if (!navigator.onLine) {
        updateCacheStatus('Đã lưu trên máy', 'text-success');
        return;
    }

    try {
        const body = new URLSearchParams();
        body.set('id', runId);
        body.set('deviceId', getDeviceId());

        const response = await fetch(cacheUrl, {
            method: 'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
            body
        });
        const result = await response.json();
        if (result?.success) {
            updateCacheStatus('Đã cache', 'text-success');
            return;
        }

        updateCacheStatus('Chưa xác nhận cache', 'text-warning');
    } catch {
        updateCacheStatus('Đã lưu trên máy', 'text-success');
    }
}

export function installDeliveryCompletionForms() {
    document.querySelectorAll('.delivery-complete-form').forEach(form => {
        installProofPreview(form);
        installReturnedQuantityControls(form);

        const noteId = form.dataset.noteId;
        const stateKey = `delivery-note-sync-state:${noteId}`;
        if (localStorage.getItem(stateKey) === 'done') {
            markCompleteFormDone(form, 'Đã gửi');
        } else if (localStorage.getItem(stateKey) === 'pending') {
            markCompleteFormPending(form, 'Đã lưu trên máy, chờ gửi');
        }

        form.addEventListener('submit', async event => {
            event.preventDefault();

            const status = form.querySelector('.delivery-complete-status');
            const submitButton = form.querySelector('button[type="submit"]');
            submitButton.disabled = true;
            if (status) status.textContent = 'Đang gửi...';

            const idempotencyInput = form.querySelector('input[name="idempotencyKey"]');
            if (idempotencyInput) {
                idempotencyInput.value = getDeliveryNoteSyncKey(noteId);
            }

            const position = await getCurrentPosition();
            if (position) {
                form.querySelector('input[name="latitude"]').value = position.coords.latitude;
                form.querySelector('input[name="longitude"]').value = position.coords.longitude;
            }

            if (!prepareAcceptancePayload(form, status)) {
                submitButton.disabled = false;
                return;
            }

            if (!navigator.onLine) {
                await queueCompletion(form, status, 'Đã lưu trên máy, sẽ tự gửi khi có mạng');
                return;
            }

            try {
                const response = await fetch(form.action, {
                    method: 'POST',
                    body: DecimalFields.getFormData(form)
                });
                const result = await response.json();
                if (result?.success) {
                    localStorage.setItem(stateKey, 'done');
                    markCompleteFormDone(form, 'Đã gửi');
                    await updatePendingCount();
                    return;
                }

                submitButton.disabled = false;
                if (status) status.textContent = result?.message ?? 'Không gửi được';
            } catch {
                await queueCompletion(form, status, 'Đã lưu trên máy, sẽ tự gửi khi có mạng');
            }
        });
    });
}

function installProofPreview(form) {
    const input = form.querySelector('input[name="proofFile"]');
    const preview = form.querySelector('.mobile-proof-preview');
    const image = preview?.querySelector('img');
    if (!input || !preview || !image) {
        return;
    }

    let objectUrl = null;
    input.addEventListener('change', () => {
        if (objectUrl) {
            URL.revokeObjectURL(objectUrl);
            objectUrl = null;
        }

        const file = input.files?.[0];
        if (!file) {
            image.removeAttribute('src');
            preview.hidden = true;
            return;
        }

        objectUrl = URL.createObjectURL(file);
        image.src = objectUrl;
        preview.hidden = false;
    });
}

export function installDeliveryManualSync() {
    const button = document.getElementById('delivery-sync-pending');
    const status = document.getElementById('delivery-sync-status');

    updatePendingCount();
    syncPendingCompletions().then(result => {
        updatePendingCount();
        updateSyncStatus(result);
    });

    if (button) {
        button.addEventListener('click', async () => {
            button.disabled = true;
            if (status) status.textContent = 'Đang gửi...';

            const result = await syncPendingCompletions();
            await updatePendingCount();
            button.disabled = false;
            updateSyncStatus(result);
        });
    }

    window.addEventListener('online', () => {
        syncPendingCompletions().then(result => {
            updatePendingCount();
            updateSyncStatus(result);
        });
    });
}

async function syncPendingCompletions() {
    if (!navigator.onLine) {
        return { synced: 0, failed: 0 };
    }

    const items = await getPendingCompletions();
    let synced = 0;
    let failed = 0;

    for (const item of items) {
        const formData = new FormData();
        Object.entries(item.fields).forEach(([key, value]) => formData.set(key, value));
        formData.set('proofFile', item.proofFile, item.proofFileName);

        try {
            const response = await fetch(item.action, {
                method: 'POST',
                body: formData
            });
            const result = await response.json();
            if (!result?.success) {
                failed++;
                continue;
            }

            await deletePendingCompletion(item.noteId);
            localStorage.setItem(`delivery-note-sync-state:${item.noteId}`, 'done');
            const form = document.querySelector(`.delivery-complete-form[data-note-id="${item.noteId}"]`);
            if (form) {
                markCompleteFormDone(form, 'Đã gửi');
            }
            synced++;
        } catch {
            failed++;
        }
    }

    return { synced, failed };
}

async function updatePendingCount() {
    const element = document.getElementById('delivery-pending-count');
    if (!element) {
        return;
    }

    try {
        const items = await getPendingCompletions();
        element.textContent = items.length > 0 ? `${items.length} phiếu đang chờ gửi` : '';
        toggleSyncStrip(items.length > 0);
    } catch {
        element.textContent = 'Không đọc được dữ liệu chờ gửi';
        toggleSyncStrip(true);
    }
}

function updateSyncStatus(result) {
    const status = document.getElementById('delivery-sync-status');
    if (!status || !result) {
        return;
    }

    if (result.failed > 0) {
        status.textContent = `Có ${result.failed} phiếu chưa gửi được`;
        toggleSyncStrip(true);
        return;
    }

    status.textContent = '';
}

function toggleSyncStrip(visible) {
    const strip = document.querySelector('.mobile-sticky-actions');
    if (!strip) {
        return;
    }

    strip.hidden = !visible;
}

function markCompleteFormPending(form, message) {
    form.querySelectorAll('input, textarea, button').forEach(element => {
        element.disabled = true;
    });

    const status = form.querySelector('.delivery-complete-status');
    if (status) {
        status.textContent = message;
        status.className = 'delivery-complete-status mobile-meta mt-2 text-warning fw-bold';
    }
}

function markCompleteFormDone(form, message) {
    form.querySelectorAll('input, textarea, button').forEach(element => {
        element.disabled = true;
    });

    const status = form.querySelector('.delivery-complete-status');
    if (status) {
        status.textContent = message;
        status.className = 'delivery-complete-status mobile-meta mt-2 text-success fw-bold';
    }
}

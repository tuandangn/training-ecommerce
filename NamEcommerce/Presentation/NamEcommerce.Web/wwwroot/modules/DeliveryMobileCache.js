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
    status.className = `fw-bold ${className}`;
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
    new FormData(form).forEach((value, key) => {
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
    try {
        await savePendingCompletion(form);
        if (status) {
            status.textContent = message;
            status.className = 'delivery-complete-status mobile-meta mt-2 text-warning fw-bold';
        }
    } catch {
        if (status) status.textContent = 'Khong the luu offline';
    } finally {
        if (submitButton) submitButton.disabled = false;
    }
}

export async function installDeliveryRunCache({ runId, cacheUrl }) {
    const payloadElement = document.getElementById('delivery-run-payload');
    if (payloadElement?.textContent) {
        localStorage.setItem(`delivery-run:${runId}`, payloadElement.textContent);
    }

    if (!navigator.onLine) {
        updateCacheStatus('Da luu tren may', 'text-success');
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
            updateCacheStatus('Da cache', 'text-success');
            return;
        }

        updateCacheStatus('Chua xac nhan cache', 'text-warning');
    } catch {
        updateCacheStatus('Da luu tren may', 'text-success');
    }
}

export function installDeliveryCompletionForms() {
    document.querySelectorAll('.delivery-complete-form').forEach(form => {
        const noteId = form.dataset.noteId;
        const stateKey = `delivery-note-sync-state:${noteId}`;
        if (localStorage.getItem(stateKey) === 'done') {
            markCompleteFormDone(form, 'Da sync');
        }

        form.addEventListener('submit', async event => {
            event.preventDefault();

            const status = form.querySelector('.delivery-complete-status');
            const submitButton = form.querySelector('button[type="submit"]');
            submitButton.disabled = true;
            if (status) status.textContent = 'Dang gui...';

            const idempotencyInput = form.querySelector('input[name="idempotencyKey"]');
            if (idempotencyInput) {
                idempotencyInput.value = getDeliveryNoteSyncKey(noteId);
            }

            const position = await getCurrentPosition();
            if (position) {
                form.querySelector('input[name="latitude"]').value = position.coords.latitude;
                form.querySelector('input[name="longitude"]').value = position.coords.longitude;
            }

            if (!navigator.onLine) {
                await queueCompletion(form, status, 'Da luu offline, se sync khi co mang');
                return;
            }

            try {
                const response = await fetch(form.action, {
                    method: 'POST',
                    body: new FormData(form)
                });
                const result = await response.json();
                if (result?.success) {
                    localStorage.setItem(stateKey, 'done');
                    markCompleteFormDone(form, 'Da sync');
                    return;
                }

                submitButton.disabled = false;
                if (status) status.textContent = result?.message ?? 'Khong the sync';
            } catch {
                await queueCompletion(form, status, 'Da luu offline, se sync khi co mang');
            }
        });
    });
}

export function installDeliveryManualSync() {
    const button = document.getElementById('delivery-sync-pending');
    const status = document.getElementById('delivery-sync-status');
    if (!button) {
        return;
    }

    button.addEventListener('click', async () => {
        button.disabled = true;
        if (status) status.textContent = 'Dang dong bo...';

        const result = await syncPendingCompletions();
        button.disabled = false;
        if (status) {
            status.textContent = result.failed > 0
                ? `Da sync ${result.synced}, con loi ${result.failed}`
                : `Da sync ${result.synced}`;
        }
    });

    window.addEventListener('online', () => {
        syncPendingCompletions();
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
                markCompleteFormDone(form, 'Da sync');
            }
            synced++;
        } catch {
            failed++;
        }
    }

    return { synced, failed };
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

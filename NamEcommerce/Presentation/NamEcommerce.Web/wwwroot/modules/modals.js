const confirmModalId = 'confirmModal';
const btnConfirmSelector = '.btnConfirm';

function createModal(id, title, body) {
    const modalElement = document.getElementById(id);
    const modal = bootstrap.Modal.getOrCreateInstance(modalElement);

    if (title) {
        const titleElement = modalElement.querySelector('.modal-header .modal-title');
        if (titleElement) titleElement.innerHTML = title;
    }

    if (body) {
        const bodyElement = modalElement.querySelector('.modal-body');
        if (bodyElement) bodyElement.innerHTML = body;
    }

    return [modal, modalElement];
}

function confirm(title, body) {
    const [confirmModal, modalElement] = createModal(confirmModalId, title, body);
    const btnConfirm = modalElement.querySelector(btnConfirmSelector);

    var promise = new Promise(resolve => {
        let isConfirmed = false;

        modalElement.addEventListener('show.bs.modal', function onShow(e) {
            modalElement.removeEventListener('show.bs.modal', onShow);
            btnConfirm.addEventListener('click', onConfirm);
        });
        function onConfirm(e) {
            e.preventDefault();
            isConfirmed = true;
            confirmModal.hide();
        }

        modalElement.addEventListener('hidden.bs.modal', function onHidden(e) {
            modalElement.removeEventListener('hidden.bs.modal', onHidden);
            btnConfirm.removeEventListener('click', onConfirm);

            resolve(isConfirmed);
        });
    }).catch(function () {
        confirmModal.hide();
        return Promise.resolve(false);
    });

    confirmModal.show();

    return promise;
}

/**
 * Backward-compatible toast() helper.
 *
 * Routes to NotificationCenter (Notyf) so all UI notifications use the same
 * unified system. Falls back to SweetAlert2 if NotificationCenter is not
 * loaded for some reason.
 *
 * Supported shapes:
 *   toast({ icon: 'success'|'error'|'warning'|'info', title, text, timer })
 *   toast(title, text, type)
 *   toast(text)
 */
const toast = function (...args) {
    if (typeof window !== 'undefined' && window.NotificationCenter) {
        const payload = normalizeToastArgs(args);
        if (payload) {
            window.NotificationCenter[payload.type](payload.message, {
                title: payload.title,
                durationMs: payload.durationMs
            });
            return Promise.resolve(payload);
        }
    }
    // fallback when NotificationCenter not yet loaded
    return Swal.fire(...args);
}

function normalizeToastArgs(args) {
    if (!args || args.length === 0) return null;

    const first = args[0];

    // Object form (SweetAlert2-style)
    if (first && typeof first === 'object' && !Array.isArray(first)) {
        const icon = (first.icon || first.type || 'info').toLowerCase();
        const message = first.text || first.html || first.title || '';
        return {
            type: ['success', 'error', 'warning', 'info'].indexOf(icon) >= 0 ? icon : 'info',
            title: first.text || first.html ? first.title : null,
            message: message,
            durationMs: typeof first.timer === 'number' ? first.timer : undefined
        };
    }

    // Positional form: (title, text, type) or (text)
    if (args.length >= 3) {
        const type = (args[2] || 'info').toString().toLowerCase();
        return {
            type: ['success', 'error', 'warning', 'info'].indexOf(type) >= 0 ? type : 'info',
            title: args[0],
            message: args[1] || ''
        };
    }
    if (args.length === 2) {
        return { type: 'info', title: args[0], message: args[1] || '' };
    }
    return { type: 'info', title: null, message: String(first) };
}

export { confirm, toast, createModal }

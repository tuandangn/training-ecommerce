/**
 * ajax-helper — thin fetch wrapper that:
 *  1. Sets sane defaults (JSON, anti-forgery token, credentials)
 *  2. Reads back the standard JsonNotificationResult envelope
 *  3. Auto-renders the embedded notification via NotificationCenter
 *
 * Usage:
 *   import { apiPost, apiGet } from '/modules/ajax-helper.js';
 *
 *   const result = await apiPost('/Order/Confirm', { id: orderId });
 *   if (result.success) { ... }
 */

const DEFAULT_HEADERS = {
    'Accept': 'application/json',
    'X-Requested-With': 'XMLHttpRequest'
};

function getAntiForgeryToken() {
    const el = document.querySelector('input[name="__RequestVerificationToken"]');
    return el ? el.value : null;
}

async function request(method, url, body, options) {
    const opts = options || {};
    const headers = Object.assign({}, DEFAULT_HEADERS, opts.headers || {});

    let payload = body;
    if (body !== undefined && body !== null && !(body instanceof FormData)) {
        headers['Content-Type'] = 'application/json';
        payload = JSON.stringify(body);
    }

    if (method !== 'GET' && method !== 'HEAD') {
        const token = getAntiForgeryToken();
        if (token) headers['RequestVerificationToken'] = token;
    }

    const response = await fetch(url, {
        method: method,
        headers: headers,
        body: method === 'GET' || method === 'HEAD' ? undefined : payload,
        credentials: 'same-origin'
    });

    const contentType = response.headers.get('Content-Type') || '';
    const isJson = contentType.indexOf('application/json') >= 0;

    let parsed = null;
    if (isJson) {
        try { parsed = await response.json(); } catch (_) { parsed = null; }
    }

    // Auto-render embedded notification (server-side JsonNotificationResult shape)
    if (parsed && parsed.notification && window.NotificationCenter) {
        window.NotificationCenter.show(parsed.notification);
    }

    if (!response.ok && !parsed) {
        const errorMessage = 'Lỗi kết nối: ' + response.status;
        if (window.NotificationCenter) window.NotificationCenter.error(errorMessage);
        return { success: false, error: errorMessage, status: response.status };
    }

    if (parsed && typeof parsed.success === 'boolean') {
        return parsed;  // already in JsonNotificationResult shape
    }

    return { success: response.ok, data: parsed, status: response.status };
}

export const apiGet    = (url, options)        => request('GET',    url, null, options);
export const apiPost   = (url, body, options)  => request('POST',   url, body, options);
export const apiPut    = (url, body, options)  => request('PUT',    url, body, options);
export const apiDelete = (url, body, options)  => request('DELETE', url, body, options);

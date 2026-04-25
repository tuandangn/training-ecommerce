/**
 * NotificationCenter — wrapper around Notyf providing the 4 standard
 * notification types (success / error / warning / info) used across
 * the NamEcommerce UI.
 *
 * Usage:
 *   NotificationCenter.success('Đã lưu');
 *   NotificationCenter.error('Có lỗi xảy ra', { title: 'Lỗi', duration: 5000 });
 *   NotificationCenter.show({ type: 'warning', message: '...' });
 *
 * The wrapper exposes itself as window.NotificationCenter so plain
 * <script> blocks or non-module JS files can use it without imports.
 */
(function (global) {
    'use strict';

    if (typeof Notyf === 'undefined') {
        console.warn('[NotificationCenter] Notyf is not loaded. Notifications will be skipped.');
        global.NotificationCenter = createNoopCenter();
        return;
    }

    var notyfInstance = new Notyf({
        position: { x: 'right', y: 'top' },
        duration: 4000,
        dismissible: true,
        ripple: true,
        types: [
            {
                type: 'success',
                background: '#28a745',
                icon: { className: 'bi bi-check-circle-fill', tagName: 'i', text: '' }
            },
            {
                type: 'error',
                background: '#dc3545',
                duration: 5000,
                icon: { className: 'bi bi-exclamation-triangle-fill', tagName: 'i', text: '' }
            },
            {
                type: 'warning',
                background: '#ffc107',
                icon: { className: 'bi bi-exclamation-circle-fill', tagName: 'i', text: '' }
            },
            {
                type: 'info',
                background: '#0dcaf0',
                icon: { className: 'bi bi-info-circle-fill', tagName: 'i', text: '' }
            }
        ]
    });

    // Maps server-side enum value (0..3) → notification type name
    var TYPE_BY_NUMBER = ['success', 'error', 'warning', 'info'];

    function normalizeType(type) {
        if (typeof type === 'number') return TYPE_BY_NUMBER[type] || 'info';
        if (typeof type === 'string') {
            var lower = type.toLowerCase();
            return TYPE_BY_NUMBER.indexOf(lower) >= 0 ? lower : 'info';
        }
        return 'info';
    }

    function open(type, message, options) {
        if (!message) return;

        var resolvedType = normalizeType(type);
        var opts = options || {};
        var payload = {
            type: resolvedType,
            message: opts.title
                ? '<strong>' + escapeHtml(opts.title) + '</strong><br/>' + escapeHtml(message)
                : escapeHtml(message)
        };

        if (typeof opts.duration === 'number') payload.duration = opts.duration;
        else if (typeof opts.durationMs === 'number') payload.duration = opts.durationMs;

        return notyfInstance.open(payload);
    }

    function escapeHtml(value) {
        if (value === null || value === undefined) return '';
        return String(value)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#39;');
    }

    var api = {
        success: function (message, options) { return open('success', message, options); },
        error:   function (message, options) { return open('error',   message, options); },
        warning: function (message, options) { return open('warning', message, options); },
        info:    function (message, options) { return open('info',    message, options); },

        /**
         * Show a notification from a server-side payload (NotificationModel-shaped).
         * Accepts: { type, message, title?, durationMs? }
         */
        show: function (notification) {
            if (!notification) return;
            return open(notification.type, notification.message, {
                title: notification.title,
                durationMs: notification.durationMs
            });
        },

        /**
         * Show many notifications in order.
         */
        showAll: function (notifications) {
            if (!Array.isArray(notifications)) return;
            for (var i = 0; i < notifications.length; i++) api.show(notifications[i]);
        },

        dismissAll: function () { notyfInstance.dismissAll(); }
    };

    global.NotificationCenter = api;

    function createNoopCenter() {
        var noop = function () {};
        return { success: noop, error: noop, warning: noop, info: noop, show: noop, showAll: noop, dismissAll: noop };
    }
})(window);

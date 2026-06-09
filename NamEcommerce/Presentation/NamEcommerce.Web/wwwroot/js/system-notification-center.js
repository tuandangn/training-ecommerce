(function () {
    'use strict';

    var root = document.querySelector('[data-system-notification-center]');
    if (!root) return;

    var toggle = root.querySelector('[data-system-notification-toggle]');
    var panel = root.querySelector('[data-system-notification-panel]');
    var list = root.querySelector('[data-system-notification-list]');
    var countBadge = root.querySelector('[data-system-notification-count]');
    var latestItems = [];

    function html(value) {
        return String(value || '')
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#39;');
    }

    function resolve(value, pascalName, camelName) {
        if (!value) return null;
        return value[camelName] !== undefined ? value[camelName] : value[pascalName];
    }

    function openUrl(item) {
        var id = resolve(item, 'Id', 'id');
        var actionUrl = resolve(item, 'ActionUrl', 'actionUrl') || '';
        var provided = resolve(item, 'OpenUrl', 'openUrl');
        if (provided) return provided;
        return '/SystemNotification/Open?id=' + encodeURIComponent(id) + '&actionUrl=' + encodeURIComponent(actionUrl);
    }

    function setCount(count) {
        var value = Number(count || 0);
        countBadge.hidden = value <= 0;
        countBadge.textContent = value > 99 ? '99+' : String(value);
    }

    function render(items) {
        latestItems = items || [];
        if (!latestItems.length) {
            list.innerHTML = '<div class="system-notification-empty">Không có thông báo mới</div>';
            return;
        }

        list.innerHTML = latestItems.map(function (item) {
            var title = resolve(item, 'Title', 'title');
            var message = resolve(item, 'Message', 'message');
            var createdOn = resolve(item, 'CreatedOn', 'createdOn') || '';
            var isRead = Boolean(resolve(item, 'IsRead', 'isRead'));
            return '<a class="system-notification-item ' + (isRead ? '' : 'is-unread') + '" href="' + html(openUrl(item)) + '">' +
                '<div class="system-notification-item-title">' + html(title) + '</div>' +
                (message ? '<div class="system-notification-item-message">' + html(message) + '</div>' : '') +
                '<div class="system-notification-item-meta">' + html(createdOn) + '</div>' +
                '</a>';
        }).join('');
    }

    async function loadCount() {
        var response = await fetch('/SystemNotification/UnreadCount', { headers: { 'Accept': 'application/json' } });
        if (!response.ok) return;
        var payload = await response.json();
        setCount(payload.count || payload.Count || 0);
    }

    async function loadLatest() {
        var response = await fetch('/SystemNotification/Latest', { headers: { 'Accept': 'application/json' } });
        if (!response.ok) return;
        var payload = await response.json();
        render(payload.items || payload.Items || []);
    }

    function prependNotification(notification) {
        latestItems = [notification].concat(latestItems).slice(0, 5);
        render(latestItems);
        var current = Number(countBadge.hidden ? 0 : countBadge.textContent.replace('+', '')) || 0;
        setCount(current + 1);
        if (window.NotificationCenter) {
            window.NotificationCenter.info(resolve(notification, 'Title', 'title'));
        }
    }

    toggle.addEventListener('click', function () {
        var willOpen = panel.hidden;
        panel.hidden = !willOpen;
        toggle.setAttribute('aria-expanded', String(willOpen));
        if (willOpen) loadLatest().catch(function () {});
    });

    document.addEventListener('click', function (event) {
        if (!root.contains(event.target)) {
            panel.hidden = true;
            toggle.setAttribute('aria-expanded', 'false');
        }
    });

    document.addEventListener('keydown', function (event) {
        if (event.key === 'Escape') {
            panel.hidden = true;
            toggle.setAttribute('aria-expanded', 'false');
        }
    });

    loadCount().catch(function () {});
    loadLatest().catch(function () {});

    if (window.signalR) {
        var connection = new window.signalR.HubConnectionBuilder()
            .withUrl('/hubs/system-notifications')
            .withAutomaticReconnect()
            .build();

        connection.on('systemNotificationCreated', prependNotification);
        connection.start().catch(function () {
            window.setInterval(loadCount, 60000);
        });
    } else {
        window.setInterval(loadCount, 60000);
    }
})();

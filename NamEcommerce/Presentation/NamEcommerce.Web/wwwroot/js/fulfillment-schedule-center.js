(function () {
    'use strict';

    var root = document.querySelector('[data-fulfillment-schedule-center]');
    if (!root) return;

    var toggle = root.querySelector('[data-fulfillment-schedule-toggle]');
    var panel = root.querySelector('[data-fulfillment-schedule-panel]');

    async function loadPanel() {
        if (panel.dataset.loaded === 'true') return;

        panel.innerHTML = '<div class="fulfillment-schedule-panel-loading">Đang tải lịch...</div>';
        var response = await fetch('/OrderFulfillment/TopbarSchedule', { headers: { 'Accept': 'text/html' } });
        if (!response.ok) {
            panel.innerHTML = '<div class="fulfillment-schedule-panel-loading">Không thể tải lịch.</div>';
            return;
        }

        panel.innerHTML = await response.text();
        panel.dataset.loaded = 'true';
    }

    toggle.addEventListener('click', function () {
        var willOpen = panel.hidden;
        panel.hidden = !willOpen;
        toggle.setAttribute('aria-expanded', String(willOpen));
        if (willOpen) loadPanel().catch(function () {
            panel.innerHTML = '<div class="fulfillment-schedule-panel-loading">Không thể tải lịch.</div>';
        });
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
})();

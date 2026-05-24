'use strict';

(function () {
    const mobileQuery = window.matchMedia('(max-width: 767.98px)');

    document.addEventListener('DOMContentLoaded', function () {
        document.querySelectorAll('[data-responsive-list]').forEach(initResponsiveList);
    });

    function initResponsiveList(root) {
        const key = root.dataset.listKey || location.pathname;
        const panels = root.querySelectorAll('[data-list-view-panel]');
        const buttons = root.querySelectorAll('[data-list-view]');
        const getViewportKey = () => mobileQuery.matches ? 'mobile' : 'desktop';
        function getStorageKey() {
            return `nam:list-view:${key}:${getViewportKey()}`;
        }

        let activeStorageKey = getStorageKey();
        let hasSavedPreference = localStorage.getItem(activeStorageKey) !== null;

        const getDefaultMode = () => mobileQuery.matches ? 'grid' : 'list';
        const getMode = () => localStorage.getItem(activeStorageKey) || getDefaultMode();

        const applyMode = mode => {
            const activeMode = mode === 'grid' ? 'grid' : 'list';
            root.dataset.activeView = activeMode;
            root.classList.toggle('is-grid-view', activeMode === 'grid');
            root.classList.toggle('is-list-view', activeMode === 'list');

            panels.forEach(panel => {
                panel.hidden = panel.dataset.listViewPanel !== activeMode;
            });

            buttons.forEach(button => {
                const isActive = button.dataset.listView === activeMode;
                button.classList.toggle('active', isActive);
                button.setAttribute('aria-pressed', String(isActive));
            });
        };

        buttons.forEach(button => {
            button.addEventListener('click', function () {
                const nextMode = this.dataset.listView === 'grid' ? 'grid' : 'list';
                activeStorageKey = getStorageKey();
                localStorage.setItem(activeStorageKey, nextMode);
                hasSavedPreference = true;
                applyMode(nextMode);
            });
        });

        mobileQuery.addEventListener('change', function () {
            activeStorageKey = getStorageKey();
            hasSavedPreference = localStorage.getItem(activeStorageKey) !== null;

            if (!hasSavedPreference) {
                applyMode(getDefaultMode());
            }
            else {
                applyMode(getMode());
            }
        });

        applyMode(getMode());
    }
})();

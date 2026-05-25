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

        const getDefaultMode = () => mobileQuery.matches ? 'grid' : 'list';
        const getMode = () => {
            const storedMode = localStorage.getItem(activeStorageKey);
            return storedMode === 'grid' || storedMode === 'list' ? storedMode : getDefaultMode();
        };

        const applyMode = (mode, source = 'init') => {
            const activeMode = mode === 'grid' ? 'grid' : 'list';
            root.dataset.activeView = activeMode;
            root.dataset.viewport = getViewportKey();
            root.classList.toggle('is-grid-view', activeMode === 'grid');
            root.classList.toggle('is-list-view', activeMode === 'list');

            panels.forEach(panel => {
                const isActive = panel.dataset.listViewPanel === activeMode;
                panel.hidden = !isActive;
                panel.setAttribute('aria-hidden', String(!isActive));
            });

            buttons.forEach(button => {
                const isActive = button.dataset.listView === activeMode;
                button.type = 'button';
                button.classList.toggle('active', isActive);
                button.setAttribute('aria-pressed', String(isActive));
                button.tabIndex = isActive ? 0 : -1;
            });

            root.dispatchEvent(new CustomEvent('responsive-list:change', {
                bubbles: true,
                detail: {
                    mode: activeMode,
                    source: source,
                    viewport: getViewportKey(),
                    storageKey: activeStorageKey
                }
            }));
        };

        buttons.forEach((button, index) => {
            button.addEventListener('click', function () {
                const nextMode = this.dataset.listView === 'grid' ? 'grid' : 'list';
                activeStorageKey = getStorageKey();
                localStorage.setItem(activeStorageKey, nextMode);
                applyMode(nextMode, 'user');
            });

            button.addEventListener('keydown', function (event) {
                const nextKeys = ['ArrowRight', 'ArrowDown'];
                const prevKeys = ['ArrowLeft', 'ArrowUp'];
                let nextIndex = null;

                if (nextKeys.includes(event.key)) {
                    nextIndex = (index + 1) % buttons.length;
                }
                else if (prevKeys.includes(event.key)) {
                    nextIndex = (index - 1 + buttons.length) % buttons.length;
                }
                else if (event.key === 'Home') {
                    nextIndex = 0;
                }
                else if (event.key === 'End') {
                    nextIndex = buttons.length - 1;
                }

                if (nextIndex === null) return;
                event.preventDefault();
                buttons[nextIndex].focus();
                buttons[nextIndex].click();
            });
        });

        mobileQuery.addEventListener('change', function () {
            activeStorageKey = getStorageKey();
            applyMode(getMode(), 'viewport');
        });

        applyMode(getMode());
    }
})();

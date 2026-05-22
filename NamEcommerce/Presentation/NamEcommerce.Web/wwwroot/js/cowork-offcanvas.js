(function (window) {
    'use strict';

    const OFFCANVAS_ID = 'coworkOffcanvas';
    const TITLE_ID = 'coworkOffcanvasLabel';
    const BODY_ID = 'coworkOffcanvasBody';
    const SIZE_CLASSES = ['cowork-offcanvas-sm', 'cowork-offcanvas-md', 'cowork-offcanvas-lg', 'cowork-offcanvas-xl'];
    const LOADING_HTML = '<div class="cowork-offcanvas-loading text-center py-4 text-muted">'
        + '<div class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></div>'
        + 'Đang tải...'
        + '</div>';

    function ensureBootstrap() {
        if (typeof bootstrap === 'undefined' || !bootstrap.Offcanvas) {
            throw new Error('Bootstrap Offcanvas chưa được load.');
        }
    }

    function applySize(element, size) {
        SIZE_CLASSES.forEach(function (cls) { element.classList.remove(cls); });
        if (size) {
            const cls = 'cowork-offcanvas-' + size;
            element.classList.add(cls);
        }
    }

    function getOrCreateInstance() {
        ensureBootstrap();
        const element = document.getElementById(OFFCANVAS_ID);
        if (!element) {
            throw new Error('Offcanvas element không tồn tại. Đảm bảo _GlobalOffcanvas đã include trong _Layout.');
        }
        return { element: element, instance: bootstrap.Offcanvas.getOrCreateInstance(element) };
    }

    function setContent(html) {
        const body = document.getElementById(BODY_ID);
        if (body) body.innerHTML = html;
    }

    function setTitle(title) {
        const titleEl = document.getElementById(TITLE_ID);
        if (titleEl) titleEl.textContent = title || 'Chi tiết';
    }

    async function fetchHtml(url) {
        const response = await fetch(url, {
            headers: { 'X-Requested-With': 'XMLHttpRequest', 'Accept': 'text/html' },
            credentials: 'same-origin'
        });
        if (!response.ok) {
            throw new Error('HTTP ' + response.status);
        }
        return await response.text();
    }

    /**
     * Mở offcanvas chung.
     *
     * @param {Object} opts
     * @param {string} opts.title       Tiêu đề offcanvas.
     * @param {string} opts.url         URL trả về HTML partial render vào body.
     * @param {string} [opts.size]      'sm' | 'md' | 'lg' | 'xl' — tùy chọn class size.
     * @param {Function} [opts.onLoaded] Callback (bodyElement) sau khi load xong nội dung.
     */
    async function openCoworkOffcanvas(opts) {
        opts = opts || {};
        const { element, instance } = getOrCreateInstance();

        applySize(element, opts.size);
        setTitle(opts.title);
        setContent(LOADING_HTML);
        instance.show();

        if (!opts.url) return;

        try {
            const html = await fetchHtml(opts.url);
            setContent(html);
            if (typeof opts.onLoaded === 'function') {
                const body = document.getElementById(BODY_ID);
                opts.onLoaded(body);
            }
        } catch (err) {
            setContent('<div class="alert alert-danger m-3">Không thể tải nội dung. ' + (err.message || '') + '</div>');
        }
    }

    function closeCoworkOffcanvas() {
        const element = document.getElementById(OFFCANVAS_ID);
        if (!element) return;
        const instance = bootstrap.Offcanvas.getInstance(element);
        if (instance) instance.hide();
    }

    window.openCoworkOffcanvas = openCoworkOffcanvas;
    window.closeCoworkOffcanvas = closeCoworkOffcanvas;
})(window);

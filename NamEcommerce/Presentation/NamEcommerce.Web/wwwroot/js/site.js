'use strict';

$(function () {
    
    const disabledSubmitForms = [];

    $('form').on('submit', function (e) {
        if (e.isDefaultPrevented())
            return;

        var form = this;
        if (!isFormValid(form))
            return;

        enableSubmitButtons(form, false);
        disabledSubmitForms.push(form);

        showPageLoading();
    });

    $(document).ajaxComplete(function () {
        disabledSubmitForms.forEach(form => enableSubmitButtons(form, true));
    });

    $('.alert-dismissible').each(function () {
        let time = 3000;
        if (this.classList.contains('long-time-waiting') || this.classList.contains('validation-summary-errors'))
            time = 20000;
        setTimeout(() => $(this).slideUp(), time);
    });

    var tooltipTriggerList = Array.from(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl)
    });

    document.querySelectorAll('[data-progress-width]').forEach(function (el) {
        const width = el.dataset.progressWidth;
        if (!width)
            return;
        el.style.width = width.endsWith('%') ? width : `${width}%`;
    });
})

function showPageLoading() {
    const mask = document.getElementById('loading-mask');
    mask.style.display = 'flex';
}
function hidePageLoading() {
    const mask = document.getElementById('loading-mask');
    mask.style.display = 'none';
}

function reparseForm(formSelector) {
    var $form = $(formSelector);

    // Remove the cached validator data
    $form.removeData('validator')
        .removeData('unobtrusiveValidation');

    // Re-parse the form rules
    $.validator.unobtrusive.parse($form);
}
function isFormValid(form) {
    try {
        return $(form).valid();
    } catch {
        return false;
    }
}
function validateElement(element) {
    if (!element || !element.form)
        return;
    $(element.form).data('validator')?.element(element);
}

function enableSubmitButtons(form, enabled) {
    $(form).find('[type=submit]').each((_, btn) => {
        if ($(btn).hasClass('noDisabled'))
            return;
        $(btn).toggleClass('disabled', !enabled).prop('disabled', !enabled);
    });
}

$.fn.focusEnd = function () {
    return this.each(function () {
        focusEnd(this);
    });
};
function focusEnd(el) {
    const len = el.value.length;
    el.focus();
    if (el.setSelectionRange) {
        el.setSelectionRange(len, len);
    } else {
        const value = el.value;
        el.value = '';
        el.value = value;
    }
};

function parseNumber(value, defaultVal = 0) {
    const n = parseFloat(value);
    return Number.isFinite(n) ? n : defaultVal;
}

function debounce(fn, ms, checkFn, beforeFn) {
    let timer;

    const debounced = (...args) => {
        clearTimeout(timer);
        if (beforeFn) beforeFn();
        if (checkFn && !checkFn())
            return;
        timer = setTimeout(() => fn(...args), ms);
    };

    debounced.cancel = () => clearTimeout(timer);
    debounced.flush = (...args) => {
        clearTimeout(timer);
        if (checkFn && !checkFn())
            return;
        fn(...args);
    };

    return debounced;
}

function getEl(id) {
    const el = document.getElementById(id);
    if (!el) throw new Error(`Element #${id} không tồn tại`);
    return el;
}

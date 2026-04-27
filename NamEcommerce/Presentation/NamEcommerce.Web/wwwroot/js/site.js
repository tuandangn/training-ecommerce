'use strict';

$(function () {
    const disabledSubmitForms = [];

    $('form').on('submit', function (e) {
        var form = this;
        if (!isFormValid(form))
            return;
        enableSubmitButtons(form, false);
        disabledSubmitForms.push(form);
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
})

function isFormValid(form) {
    try {
        return $(form).valid();
    } catch {
        return false;
    }
}

function enableSubmitButtons(form, enabled) {
    $(form).find('[type=submit]').each(btn => {
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

function debounce(fn, ms, checkFn) {
    let timer;

    const debounced = (...args) => {
        if (checkFn && !checkFn())
            return;
        clearTimeout(timer);
        timer = setTimeout(() => fn(...args), ms);
    };

    debounced.cancel = () => clearTimeout(timer);
    debounced.flush = (...args) => {
        if (checkFn && !checkFn())
            return;
        clearTimeout(timer);
        fn(...args);
    };

    return debounced;
}

function getEl(id) {
    const el = document.getElementById(id);
    if (!el) throw new Error(`Element #${id} không tồn tại`);
    return el;
}

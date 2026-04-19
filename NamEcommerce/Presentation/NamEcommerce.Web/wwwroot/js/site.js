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
        setTimeout(() => $(this).slideUp(), 3000);
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
        var el = this;
        var len = el.value.length;

        // Focus trước
        el.focus();

        // Nếu trình duyệt hỗ trợ setSelectionRange
        if (el.setSelectionRange) {
            el.setSelectionRange(len, len);
        } else {
            // Support cho các trình duyệt cũ hoặc các trường hợp đặc biệt
            $(el).val($(el).val());
        }
    });
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

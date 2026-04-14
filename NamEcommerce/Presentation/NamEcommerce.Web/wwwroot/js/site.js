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

    $(document).on('focus', 'form input', function (e) {
        if (this.type != 'number')
            return;
        if (this.value.match('^\\s*0+\\s*$'))
            this.value = '';
        $(this).on('blur', function onBlur() {
            if (this.value == '')
                this.value = '0';
            $(this).off('blur', onBlur);
        });
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

function debounce(fn, ms) {
    let timer;
    return (...args) => {
        clearTimeout(timer);
        timer = setTimeout(() => fn(...args), ms);
    };
}

function getEl(id) {
    const el = document.getElementById(id);
    if (!el) throw new Error(`Element #${id} không tồn tại`);
    return el;
}

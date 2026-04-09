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
        var oldValue = this.value;
        if (oldValue.match('^0+$'))
            oldValue = '0';
        if (this.value.match('^\\s*0+\\s*$'))
            this.value = '';
        $(this).on('blur', function onBlur() {
            if (this.value == '')
                this.value = oldValue;
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
        $(btn).prop('disabled', !enabled);
    });
}


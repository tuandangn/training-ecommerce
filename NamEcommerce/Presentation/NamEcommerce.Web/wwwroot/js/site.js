'use strict';

$(function() {
    const disabledSubmitForms = [];

    $('form').on('submit', function(e) {
        var form = this;
        if (!isFormValid(form))
            return;
        enableSubmitButtons(form, false);
        disabledSubmitForms.push(form);
    });

    $(document).ajaxComplete(function() { 
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
    $(form).find('[type=submit]').prop('disabled', !enabled);
}


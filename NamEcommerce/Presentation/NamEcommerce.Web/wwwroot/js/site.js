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
})

/* form */
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


/**
 * decimal-validation-patch.js
 *
 * Patch jQuery Unobtrusive Validation de hoat dong dung voi
 * currency/quantity input da duoc format boi decimal-fields.js.
 *
 * Thu tu load:
 *   1. jquery.js
 *   2. jquery.validate.js
 *   3. jquery.validate.unobtrusive.js
 *   4. decimal-fields.js
 *   5. decimal-validation-patch.js   <-- file nay, load cuoi cung
 */
(function ($) {
    'use strict';

    if (!$ || !$.validator) {
        console.warn('[decimal-validation-patch] jQuery Validate chua duoc load.');
        return;
    }

    // ----------------------------------------------------------------
    // 1. Override $.validator.methods.number
    //    Cho phep gia tri da format: "5.000.000", "1.234,56"
    // ----------------------------------------------------------------
    var _origNumber = $.validator.methods.number;

    $.validator.methods.number = function (value, element) {
        if (isDecimalInput(element)) {
            value = stripDecimal(value, element);
        }
        return _origNumber.call(this, value, element);
    };

    // ----------------------------------------------------------------
    // 2. Override $.validator.methods.required
    //    Truong hop input bi boc trong .decimal-field wrapper:
    //    jQuery Validate doc .value cua element goc nen van dung,
    //    nhung strip format truoc khi check de tranh "  " -> truthy
    // ----------------------------------------------------------------
    var _origRequired = $.validator.methods.required;

    $.validator.methods.required = function (value, element, param) {
        if (isDecimalInput(element)) {
            value = stripDecimal(value, element).trim();
        }
        return _origRequired.call(this, value, element, param);
    };

    // ----------------------------------------------------------------
    // 3. Override $.validator.methods.range
    //    [Range(min, max)] so sanh gia tri so, phai strip truoc
    // ----------------------------------------------------------------
    var _origRange = $.validator.methods.range;

    $.validator.methods.range = function (value, element, param) {
        if (isDecimalInput(element)) {
            value = stripDecimal(value, element);
        }
        return _origRange.call(this, value, element, param);
    };

    // ----------------------------------------------------------------
    // 4. Override $.validator.methods.min / max (neu dung [Range] phan ra)
    // ----------------------------------------------------------------
    var _origMin = $.validator.methods.min;
    $.validator.methods.min = function (value, element, param) {
        if (isDecimalInput(element)) value = stripDecimal(value, element);
        return _origMin.call(this, value, element, param);
    };

    var _origMax = $.validator.methods.max;
    $.validator.methods.max = function (value, element, param) {
        if (isDecimalInput(element)) value = stripDecimal(value, element);
        return _origMax.call(this, value, element, param);
    };

    // ----------------------------------------------------------------
    // 5. Hook vao $.validator normalizeRules / clean
    //    Dam bao gia tri submit len server luon la so thuan
    //    (backup cho truong hop capture-phase submit chua kip chay)
    // ----------------------------------------------------------------
    $(document).on('submit', 'form', function () {
        $(this).find('input.decimal-input').each(function () {
            var decimals = parseInt(this.dataset.decimals || '0', 10);
            var raw = stripFormatRaw(this.value, decimals);
            if (raw !== '') this.value = raw;
        });
    });

    // ----------------------------------------------------------------
    // Helpers
    // ----------------------------------------------------------------

    function isDecimalInput(element) {
        return element &&
            element.classList &&
            element.classList.contains('decimal-input');
    }

    /**
     * Strip format, tra ve chuoi so dang InvariantCulture.
     * Doc decimals tu data-decimals cua element.
     */
    function stripDecimal(value, element) {
        var decimals = parseInt((element && element.dataset.decimals) || '0', 10);
        return stripFormatRaw(value, decimals);
    }

    function stripFormatRaw(str, decimals) {
        if (!str) return '';
        str = str.trim();
        if (!decimals) return str.replace(/[^\d]/g, '');
        var lastDot = str.lastIndexOf('.');
        var lastComma = str.lastIndexOf(',');
        if (lastDot === -1 && lastComma === -1) return str.replace(/[^\d]/g, '');
        if (lastComma > lastDot) return str.replace(/\./g, '').replace(',', '.');
        return str.replace(/,/g, '');
    }

}(window.jQuery));
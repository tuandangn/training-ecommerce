/**
 * decimal-fields.js (ES Module Version)
 * Currency : 3.400.000  |  Quantity : 1.234,56
 */

'use strict';

const numberSettings = {
    groupSeparator: '.',
    decimalSeparator: ','
};

// --- Các hàm bổ trợ (Helper Functions) ---

// Fallback cho hàm focusEnd nếu chưa được định nghĩa ở nơi khác
function focusEnd(input) {
    const len = input.value.length;
    input.setSelectionRange(len, len);
}

// Fallback cho hàm parseNumber nếu chưa được định nghĩa ở nơi khác
function parseNumber(str) {
    return parseFloat(str) || 0;
}

export function stripFormatting(str, decimals) {
    if (!str) return '';
    str = str.trim();
    if (!decimals) return str.replace(/[^\d]/g, '');

    const lastDot = str.lastIndexOf('.');
    const lastComma = str.lastIndexOf(',');
    if (lastDot === -1 && lastComma === -1) return str.replace(/[^\d]/g, '');
    if (lastComma > lastDot) return str.replace(/\./g, '').replace(',', '.');
    if (lastDot != -1 && lastComma == -1) {
        const dotCount = Array.from(str.matchAll(/\./g)).length;
        if (dotCount > 1) return str.replace(/\./g, '');
        if (str.length - (lastDot + 1) != 3) return str;
        return str.replace(/\./g, '');
    }
    return str.replace(/,/g, '');
}

export function stripInputFormatting(input) {
    if (!input || !(input instanceof HTMLInputElement)) throw new Error('Invalid input element');
    var value = input.value;
    if (!value || value == '0') return stripFormatting(value, 0);
    var decimals = parseInt(input.dataset.decimals, 10) || 0;
    return stripFormatting(value, decimals);
}

// Dùng riêng trong blur: lúc này user đã gõ, "." luôn là dấu thập phân
export function parseTypedDecimal(str) {
    if (!str) return '';
    return str.trim().replace(/[^\d.]/g, '');
}

export function formatCurrency(raw, endSymbol, decimals) {
    const n = parseFloat(raw);
    if (isNaN(n)) return raw;

    decimals = (decimals === undefined) ? 0 : parseInt(decimals, 10);

    const fixedNumber = n.toFixed(decimals);
    const parts = fixedNumber.split(numberSettings.groupSeparator);

    let currencyText = parts[0].replace(/\B(?=(\d{3})+(?!\d))/g, numberSettings.groupSeparator);

    if (parts[1] && parseInt(parts[1], 10) > 0) {
        currencyText += numberSettings.decimalSeparator + parts[1];
    }

    if (endSymbol) {
        currencyText += ' ' + endSymbol;
    }

    return currencyText;
}

export function formatCurrencyWithSymbol(raw) {
    return formatCurrency(raw, '\u20ab');
}

export function formatQuantity(raw, decimals) {
    decimals = (decimals === undefined) ? 2 : parseInt(decimals, 10);

    const n = parseFloat(raw);
    if (isNaN(n)) return raw;

    const fixedNumber = n.toFixed(decimals);
    const parts = fixedNumber.split(numberSettings.groupSeparator);

    const formattedInteger = parts[0].replace(/\B(?=(\d{3})+(?!\d))/g, numberSettings.groupSeparator);

    if (!parts[1] || parseInt(parts[1], 10) === 0) {
        return formattedInteger;
    }

    return formattedInteger + numberSettings.decimalSeparator + parts[1];
}

export function formatInput(input) {
    if (!input || !(input instanceof HTMLInputElement)) throw new Error('Invalid input element');
    var value = stripInputFormatting(input);
    if (!value || value === '0') return formatQuantity(value);
    var type = input.dataset.decimal || "quantity";
    var decimals = parseInt(input.dataset.decimals, 10) || 0;
    return type === 'currency' ? formatCurrency(value) : formatQuantity(value, decimals);
}

export function setValue(input, value) {
    if (!input || !(input instanceof HTMLInputElement)) throw new Error('Invalid input element');
    var type = input.dataset.decimal || "quantity";
    var decimals = parseInt(input.dataset.decimals, 10) || 0;
    input.value = type === 'currency' ? formatCurrency(value) : formatQuantity(value, decimals);
    return input.value;
}

export function getValue(input) {
    var stripped = stripInputFormatting(input);
    return parseFloat(stripped) || 0;
}

const SUFFIX_SVG =
    '<svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24"' +
    ' fill="none" stroke="currentColor" stroke-width="2"' +
    ' stroke-linecap="round" stroke-linejoin="round">' +
    '<rect x="2" y="7" width="20" height="14" rx="2"/>' +
    '<path d="M16 7V5a2 2 0 0 0-4 0v2"/>' +
    '<line x1="12" y1="12" x2="12" y2="16"/>' +
    '<line x1="10" y1="14" x2="14" y2="14"/></svg>';

// ---- Validate truoc khi format ------------------------------------

export function isValidDecimal(input, rawValue) {
    if (typeof jQuery === 'undefined' || !jQuery.validator) return true;

    var $form = jQuery(input).closest('form');
    if (!$form.length) return true;

    var validator = $form.data('validator');
    if (!validator) return true;

    var prevValue = input.value;
    input.value = rawValue;

    var valid = validator.element(input);

    input.value = prevValue;

    return valid !== false;
}

// ---- Gan events cho mot input ---------------------------------------

export function bindInput(input) {
    if (input.dataset.decimalBound === '1') return;
    input.dataset.decimalBound = '1';

    var type = input.dataset.type;

    if (input._focusHandler)
        input.removeEventListener('focus', input._focusHandler);
    input.addEventListener('focus', function onFocus() {
        this._focusHandler = onFocus;
        this.value = stripInputFormatting(this);
        if (this.value == '0')
            this.select();
        else
            focusEnd(this);
    });

    if (input._blurHandler)
        input.removeEventListener('blur', input._blurHandler);
    input.addEventListener('blur', function onBlur(e) {
        this._blurHandler = onBlur;
        var raw = parseTypedDecimal(this.value);

        if (raw !== '' && !isValidDecimal(this, raw)) {
            return;
        }

        if (raw === '') {
            this.value = '';
        } else {
            var type = this.dataset.type || 'quantity';
            var decimals = parseInt(this.dataset.decimals, 10) || 0;
            this.value = type === 'currency' ? formatCurrency(raw) : formatQuantity(raw, decimals);
        }
    });

    if (input._keyPressHandler)
        input.removeEventListener('keypress', input._keyPressHandler);
    input.addEventListener('keypress', function onKeyPress(e) {
        this._keyPressHandler = onKeyPress;
        if (e.key == 'Enter' || e.code == 'Enter' || e.keyCode == 13)
            return;
        var decimals = parseInt(this.dataset.decimals || '0', 10);
        var char = String.fromCharCode(e.which);
        if (!/\d/.test(char) && !(decimals > 0 && char === '.')) e.preventDefault();
        if (char === '.' && this.value.includes('.')) e.preventDefault();
    });

    if (input._pasteHandler)
        input.removeEventListener('paste', input._pasteHandler);
    input.addEventListener('paste', function onPaste(e) {
        this._pasteHandler = onPaste;
        e.preventDefault();
        var decimals = parseInt(this.dataset.decimals || '0', 10);
        var pasted = (e.clipboardData || window.clipboardData).getData('text');
        document.execCommand('insertText', false, stripFormatting(pasted, decimals));
    });
}

// ---- Hint doc bang chu ----------------------------------------------

function attachHint(wrapper, input) {
    if (!window.SoBangChu) return null;

    var hint = document.createElement('div');
    hint.className = 'currency-hint text-end';

    var rawNum = parseNumber(stripFormatting(input.value, 0));
    if (rawNum) {
        hint.textContent = window.SoBangChu.docSoTien(rawNum);
        hint.classList.add('visible');
    }

    wrapper.insertAdjacentElement('afterend', hint);

    input.addEventListener('input', function () {
        var r = parseNumber(stripFormatting(this.value, 0));
        hint.textContent = r ? window.SoBangChu.docSoTien(r) : '';
        hint.classList.toggle('visible', !!r);
    });

    input.addEventListener('blur', function () {
        var self = this;
        setTimeout(function () {
            var r = parseNumber(stripFormatting(self.value, 0));
            hint.textContent = r ? window.SoBangChu.docSoTien(r) : '';
            hint.classList.toggle('visible', !!r);
        }, 0);
    });

    return hint;
}

// ---- Factory: tao moi -----------------------------------------------

export function createCurrencyInput(options) {
    var opts = Object.assign({
        name: '', value: null,
        id: null, cssClass: '',
        placeholder: '0',
        showHint: false
    }, options);

    var wrapper = document.createElement('div');
    wrapper.className = 'decimal-field currency-field';

    var prefix = document.createElement('span');
    prefix.className = 'field-prefix';
    prefix.textContent = '\u20ab';

    var input = document.createElement('input');
    input.type = 'text';
    input.name = opts.name;
    input.className = ('form-control decimal-input currency-input ' + opts.cssClass).trim();
    input.placeholder = opts.placeholder;
    input.inputMode = 'numeric';
    input.autocomplete = 'off';
    input.dataset.decimals = '0';
    input.dataset.type = 'currency';
    if (opts.id) input.id = opts.id;
    if (opts.value != null) input.value = formatCurrency(String(opts.value));

    if (opts.includeSuffix || input.classList.contains('include-suffix'))
        wrapper.appendChild(prefix);
    wrapper.appendChild(input);
    bindInput(input);

    var hint = (opts.showHint || input.classList.contains('include-hint')) ? attachHint(wrapper, input) : null;
    return { wrapper: wrapper, input: input, hint: hint };
}

export function createQuantityInput(options) {
    var opts = Object.assign({
        name: '', value: null,
        id: null, cssClass: '',
        placeholder: null,
        decimals: 2
    }, options);

    var wrapper = document.createElement('div');
    wrapper.className = 'decimal-field quantity-field';

    var suffix = document.createElement('span');
    suffix.className = 'field-suffix';
    suffix.innerHTML = SUFFIX_SVG;

    var input = document.createElement('input');
    input.type = 'text';
    input.name = opts.name;
    input.className = ('form-control decimal-input quantity-input ' + opts.cssClass).trim();
    var decimals = parseInt(opts.decimals, 10);
    input.inputMode = decimals > 0 ? 'decimal' : 'numeric';
    input.autocomplete = 'off';
    input.dataset.decimals = String(decimals);
    input.dataset.type = 'quantity';
    if (opts.id) input.id = opts.id;
    if (opts.value != null) input.value = formatQuantity(String(opts.value), decimals);

    if (opts.includeSuffix || input.classList.contains('include-suffix'))
        wrapper.appendChild(suffix);
    wrapper.appendChild(input);
    bindInput(input);

    return { wrapper: wrapper, input: input };
}

export function wrapExistingInput(input, type, options) {
    if (typeof input === 'string') input = document.querySelector(input);
    if (!input) throw new Error('DecimalFields.wrapExistingInput: khong tim thay input');
    if (input.dataset.decimalBound === '1') return { input: input };

    var isCurr = (type === 'currency');
    var decimals = isCurr ? 0 : parseInt(input.dataset.decimals || '2', 10);

    var opts = Object.assign({
        showHint: false,
        includeSuffix: false
    }, options);
    if (input.classList.contains('include-hint'))
        opts.showHint = true;
    if (input.classList.contains('include-suffix'))
        opts.includeSuffix = true;

    input.dataset.type = type;
    input.dataset.decimals = String(decimals);

    input.classList.add('form-control', 'decimal-input',
        isCurr ? 'currency-input' : 'quantity-input');

    var wrapper;
    if (input.closest('.input-group')) {
        const inputGroup = input.closest('.input-group');
        wrapper = inputGroup;
        input.style.width = '';
    } else {
        wrapper = document.createElement('div');
        wrapper.className = 'decimal-field ' + (isCurr ? 'currency-field' : 'quantity-field');
        input.parentNode.insertBefore(wrapper, input);
        wrapper.appendChild(input);
    }

    if (opts.includeSuffix) {
        if (isCurr) {
            var prefix = document.createElement('span');
            prefix.className = 'field-prefix';
            prefix.textContent = '\u20ab';
            wrapper.insertBefore(prefix, input);
        } else {
            var suffix = document.createElement('span');
            suffix.className = 'field-suffix';
            suffix.innerHTML = SUFFIX_SVG;
            wrapper.appendChild(suffix);
        }
    } else {
        input.style.paddingRight = '0.5rem';
    }

    var raw = stripFormatting(input.value, decimals);
    if (raw) input.value = isCurr ? formatCurrency(raw, null, decimals) : formatQuantity(raw, decimals);

    bindInput(input);
    var hint = (isCurr && (opts.showHint)) ? attachHint(wrapper, input) : null;

    return { wrapper: wrapper, input: input, hint: hint };
}

export function autoWrap(root) {
    root = root || document;
    root.querySelectorAll('input[data-decimal]:not([data-decimal-bound])')
        .forEach(function (input) {
            var type = input.dataset.decimal;
            if (type !== 'currency' && type !== 'quantity') return;
            wrapExistingInput(input, type);
        });
}

function initDecimalFields() {
    autoWrap();
    document.querySelectorAll('.decimal-input').forEach(bindInput);
    document.querySelectorAll('form').forEach(function (form) {
        form.addEventListener('submit', function () {
            form.querySelectorAll('.decimal-input').forEach(function (inp) {
                var raw = stripFormatting(inp.value, parseInt(inp.dataset.decimals || '0', 10));
                if (raw !== '') inp.value = raw;
            });
        }, true);
    });
}

export function getFormData(form) {
    if (!form || !(form instanceof HTMLFormElement))
        throw new Error('Form is required');
    const formData = new FormData(form);
    const decimalFields = form.querySelectorAll('.decimal-input');
    for (const decimalField of decimalFields) {
        formData.set(decimalField.name, stripInputFormatting(decimalField));
    }
    return formData;
}

// Tự động khởi chạy khi file module được load
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initDecimalFields);
} else {
    initDecimalFields();
}

// --- Xuất (Export) Object API mặc định giống như Window API cũ ---
const DecimalFields = {
    createCurrencyInput,
    createQuantityInput,
    wrapExistingInput,
    autoWrap,
    bindInput,
    isValidDecimal,
    formatCurrency,
    formatCurrencyWithSymbol,
    formatQuantity,
    stripFormatting,
    stripInputFormatting,
    getFormData,
    getValue,
    setValue
};

export default DecimalFields;
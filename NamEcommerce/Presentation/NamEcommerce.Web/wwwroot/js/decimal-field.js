/**
 * decimal-fields.js
 * Currency : 3.400.000  |  Quantity : 1.234,56
 *
 * Public API (window.DecimalFields):
 *   createCurrencyInput(options)          -- tao moi tu dau
 *   createQuantityInput(options)          -- tao moi tu dau
 *   wrapExistingInput(input, type, opts)  -- wrap <input> co san
 *   bindInput(inputEl)                    -- chi gan events
 *   formatCurrency / formatQuantity / stripFormatting
 */
(function (global) {
    'use strict';

    // ---- Helpers -------------------------------------------------------

    function stripFormatting(str, decimals) {
        if (!str) return '';
        str = str.trim();
        if (!decimals) return str.replace(/[^\d]/g, '');
        var lastDot = str.lastIndexOf('.');
        var lastComma = str.lastIndexOf(',');
        if (lastDot === -1 && lastComma === -1) return str.replace(/[^\d]/g, '');
        if (lastComma > lastDot) return str.replace(/\./g, '').replace(',', '.');
        return str.replace(/,/g, '');
    }

    function formatCurrency(raw) {
        var n = parseInt(raw, 10);
        if (isNaN(n)) return raw;
        return n.toString().replace(/\B(?=(\d{3})+(?!\d))/g, '.');
    }

    function formatQuantity(raw) {
        var n = parseFloat(raw);
        if (isNaN(n)) return raw;
        var parts = n.toFixed(2).split('.');
        return parts[0].replace(/\B(?=(\d{3})+(?!\d))/g, '.') + ',' + parts[1];
    }

    var SUFFIX_SVG =
        '<svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24"' +
        ' fill="none" stroke="currentColor" stroke-width="2"' +
        ' stroke-linecap="round" stroke-linejoin="round">' +
        '<rect x="2" y="7" width="20" height="14" rx="2"/>' +
        '<path d="M16 7V5a2 2 0 0 0-4 0v2"/>' +
        '<line x1="12" y1="12" x2="12" y2="16"/>' +
        '<line x1="10" y1="14" x2="14" y2="14"/></svg>';

    // ---- Validate truoc khi format ------------------------------------

    /**
     * Kiem tra mot decimal input co hop le khong bang cach chay
     * jQuery Validate tren dung field do (neu co), tra ve true/false.
     *
     * - Neu jQuery Validate chua load: bo qua validate, tra ve true (cho format).
     * - Neu input khong nam trong form co validator: tra ve true.
     * - Chi kiem tra cac rule lien quan den gia tri so: required, range, min, max.
     *   (Khong kich hoat toan bo validate de tranh side effect.)
     */
    function isValidDecimal(input, rawValue) {
        if (typeof jQuery === 'undefined' || !jQuery.validator) return true;

        var $form = jQuery(input).closest('form');
        if (!$form.length) return true;

        var validator = $form.data('validator');
        if (!validator) return true;

        // Tam thay the value bang raw de validator doc dung gia tri so
        var prevValue = input.value;
        input.value = rawValue;

        // element() chay validate cho dung 1 field, cap nhat luon UI error
        var valid = validator.element(input);

        // Khoi phuc value goc de blur handler xu ly tiep
        input.value = prevValue;

        // valid co the la undefined (chua tung validate) -> coi la hop le
        return valid !== false;
    }

    // ---- Gan events cho mot input ---------------------------------------

    function bindInput(input) {
        if (input.dataset.decimalBound === '1') return;
        input.dataset.decimalBound = '1';

        var type = input.dataset.type;
        var decimals = parseInt(input.dataset.decimals || '0', 10);
        // Flag ngan event 'input' / 'change' fire khi chinh code minh gan .value
        var _formatting = false;

        // Wrap de ben ngoai co the check: input._decimalFormatting
        Object.defineProperty(input, '_decimalFormatting', {
            get: function () { return _formatting; }
        });

        input.addEventListener('input', function (e) {
            // Bo qua neu do chinh blur handler gan gia tri
            if (_formatting) {
                e.stopImmediatePropagation();
                return;
            }
        });

        input.addEventListener('change', function (e) {
            if (_formatting) {
                e.stopImmediatePropagation();
                return;
            }
        });

        input.addEventListener('focus', function () {
            _formatting = true;
            this.value = stripFormatting(this.value, decimals);
            this.select();
            _formatting = false;
        });

        input.addEventListener('blur', function () {
            var raw = stripFormatting(this.value, decimals);

            // Validate truoc khi format
            // jQuery Validate unobtrusive: kiem tra field nay co hop le khong
            if (raw !== '' && !isValidDecimal(this, raw)) {
                // Gia tri sai: khong format, giu nguyen de nguoi dung biet
                return;
            }

            _formatting = true;
            if (raw === '') {
                this.value = '';
            } else {
                this.value = type === 'currency' ? formatCurrency(raw) : formatQuantity(raw);
            }
            _formatting = false;
        });

        input.addEventListener('keypress', function (e) {
            var char = String.fromCharCode(e.which);
            if (!/\d/.test(char) && !(decimals > 0 && char === '.')) e.preventDefault();
            if (char === '.' && this.value.includes('.')) e.preventDefault();
        });

        input.addEventListener('paste', function (e) {
            e.preventDefault();
            var pasted = (e.clipboardData || window.clipboardData).getData('text');
            document.execCommand('insertText', false, stripFormatting(pasted, decimals));
        });
    }

    // ---- Hint doc bang chu ----------------------------------------------

    function attachHint(wrapper, input) {
        if (!global.SoBangChu) return null;

        var hint = document.createElement('div');
        hint.className = 'currency-hint text-end';

        var rawNum = parseNumber(stripFormatting(input.value, 0));
        if (rawNum) {
            hint.textContent = global.SoBangChu.docSoTien(rawNum);
            hint.classList.add('visible');
        }

        wrapper.insertAdjacentElement('afterend', hint);

        input.addEventListener('input', function () {
            var r = parseNumber(stripFormatting(this.value, 0));
            hint.textContent = r ? global.SoBangChu.docSoTien(r) : '';
            hint.classList.toggle('visible', !!r);
        });

        input.addEventListener('blur', function () {
            var self = this;
            setTimeout(function () {
                var r = parseNumber(stripFormatting(self.value, 0));
                hint.textContent = r ? global.SoBangChu.docSoTien(r) : '';
                hint.classList.toggle('visible', !!r);
            }, 0);
        });

        return hint;
    }

    // ---- Factory: tao moi -----------------------------------------------

    function createCurrencyInput(options) {
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
        prefix.textContent = '\u20ab'; // dong

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

    function createQuantityInput(options) {
        var opts = Object.assign({
            name: '', value: null,
            id: null, cssClass: '',
            placeholder: '0,00'
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
        input.placeholder = opts.placeholder;
        input.inputMode = 'decimal';
        input.autocomplete = 'off';
        input.dataset.decimals = '2';
        input.dataset.type = 'quantity';
        if (opts.id) input.id = opts.id;
        if (opts.value != null) input.value = formatQuantity(String(opts.value));

        if (opts.includeSuffix || input.classList.contains('include-suffix'))
            wrapper.appendChild(suffix);
        wrapper.appendChild(input);
        bindInput(input);

        return { wrapper: wrapper, input: input };
    }

    // ---- wrapExistingInput: nhan <input> co san trong DOM ---------------

    /**
     * wrapExistingInput(input, type, options)
     *
     * Nhan mot <input> da co trong DOM (co the la Razor-generated),
     * boc no vao wrapper + prefix/suffix, gan data-attributes, format
     * gia tri hien tai va bind events.
     *
     * @param {HTMLInputElement|string} input  - element hoac CSS selector
     * @param {'currency'|'quantity'}   type
     * @param {object}                  options
     *   showHint {boolean}  - hien doc so bang chu (chi currency, mac dinh true)
     *
     * @returns {{ wrapper, input, hint }}
     *
     * Vi du:
     *   DecimalFields.wrapExistingInput('#UnitPrice', 'currency');
     *   DecimalFields.wrapExistingInput(document.getElementById('Qty'), 'quantity');
     */
    function wrapExistingInput(input, type, options) {
        if (typeof input === 'string') input = document.querySelector(input);
        if (!input) throw new Error('DecimalFields.wrapExistingInput: khong tim thay input');
        if (input.dataset.decimalBound === '1') return { input: input };

        var opts = Object.assign({ showHint: false }, options);
        var isCurr = (type === 'currency');
        var decimals = isCurr ? 0 : 2;

        // 1. Gan data-attributes
        input.dataset.type = type;
        input.dataset.decimals = String(decimals);

        // 2. Them CSS classes
        input.classList.add('form-control', 'decimal-input',
            isCurr ? 'currency-input' : 'quantity-input');

        // 3. Tao wrapper, chen vao DOM tai vi tri hien tai cua input
        var wrapper = document.createElement('div');
        wrapper.className = 'decimal-field ' + (isCurr ? 'currency-field' : 'quantity-field');
        input.parentNode.insertBefore(wrapper, input);
        wrapper.appendChild(input);

        // 4. Them prefix hoac suffix
        if (opts.includeSuffix || input.classList.contains('include-suffix')) {
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

        // 5. Format gia tri hien tai
        var raw = stripFormatting(input.value, decimals);
        if (raw) input.value = isCurr ? formatCurrency(raw) : formatQuantity(raw);

        // 6. Bind events
        bindInput(input);
        // 7. Hint doc bang chu
        var hint = (isCurr && (opts.showHint || input.classList.contains('include-hint'))) ? attachHint(wrapper, input) : null;

        return { wrapper: wrapper, input: input, hint: hint };
    }

    // ---- Init DOM co san ------------------------------------------------

    function patchJqueryValidator() {
        // Viec patch da duoc chuyen sang decimal-validation-patch.js
        // Load file do SAU jquery.validate.unobtrusive.js
    }

    // ---- Auto-wrap theo data-decimal attribute -------------------------

    /**
     * Quet DOM tim cac <input data-decimal="currency|quantity"> chua duoc wrap
     * va tu dong goi wrapExistingInput.
     *
     * Dung trong:
     *   - Razor View  : <input asp-for="UnitPrice" data-decimal="currency" />
     *   - EditorTemplate: them data_decimal = "currency" vao Html.TextBox(...)
     *   - Input tao dong: sau khi inject vao DOM goi lai autoWrap(container)
     *
     * @param {HTMLElement} [root=document] - quet trong pham vi nay (tuy chon)
     */
    function autoWrap(root) {
        root = root || document;
        root.querySelectorAll('input[data-decimal]:not([data-decimal-bound])')
            .forEach(function (input) {
                var type = input.dataset.decimal; // "currency" | "quantity"
                if (type !== 'currency' && type !== 'quantity') return;
                wrapExistingInput(input, type);
            });
    }

    function initDecimalFields() {
        patchJqueryValidator();
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

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initDecimalFields);
    } else {
        initDecimalFields();
    }

    // ---- Public API -----------------------------------------------------
    global.DecimalFields = {
        createCurrencyInput: createCurrencyInput,
        createQuantityInput: createQuantityInput,
        wrapExistingInput: wrapExistingInput,
        autoWrap: autoWrap,
        bindInput: bindInput,
        isValidDecimal: isValidDecimal,
        formatCurrency: formatCurrency,
        formatQuantity: formatQuantity,
        stripFormatting: stripFormatting
    };

})(window);
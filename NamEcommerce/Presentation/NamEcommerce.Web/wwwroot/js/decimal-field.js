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

    // ---- Gan events cho mot input ---------------------------------------

    function bindInput(input) {
        if (input.dataset.decimalBound === '1') return;
        input.dataset.decimalBound = '1';

        var type = input.dataset.type;
        var decimals = parseInt(input.dataset.decimals || '0', 10);

        input.addEventListener('focus', function () {
            this.value = stripFormatting(this.value, decimals);
            this.select();
        });

        input.addEventListener('blur', function () {
            var raw = stripFormatting(this.value, decimals);
            if (raw === '') { this.value = ''; return; }
            this.value = type === 'currency' ? formatCurrency(raw) : formatQuantity(raw);
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
        hint.className = 'currency-hint';

        var rawNum = input.value.replace(/[^\d]/g, '');
        if (rawNum) {
            hint.textContent = global.SoBangChu.docSoTien(rawNum);
            hint.classList.add('visible');
        }

        wrapper.insertAdjacentElement('afterend', hint);

        input.addEventListener('input', function () {
            var r = this.value.replace(/[^\d]/g, '');
            hint.textContent = r ? global.SoBangChu.docSoTien(r) : '';
            hint.classList.toggle('visible', !!r);
        });

        input.addEventListener('blur', function () {
            var self = this;
            setTimeout(function () {
                var r = stripFormatting(self.value, 0);
                hint.textContent = r ? global.SoBangChu.docSoTien(r) : '';
                hint.classList.toggle('visible', !!r);
            }, 0);
        });

        return hint;
    }

    // ---- Factory: tao moi -----------------------------------------------

    function createCurrencyInput(options) {
        var opts = Object.assign({
            name: '', value: null, id: null,
            placeholder: '0', cssClass: '', showHint: true,
            noPrefix: false
        }, options);

        var wrapper = document.createElement('div');
        wrapper.className = 'decimal-field currency-field';

        var prefix = document.createElement('span');
        prefix.className = 'field-prefix';
        if (!opts.noPrefix)
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
        if (opts.noPrefix)
            input.style.paddingRight = '0.5em';
        if (opts.id) input.id = opts.id;
        if (opts.value != null) input.value = formatCurrency(String(opts.value));

        wrapper.appendChild(prefix);
        wrapper.appendChild(input);
        bindInput(input);

        var hint = opts.showHint ? attachHint(wrapper, input) : null;
        return { wrapper: wrapper, input: input, hint: hint };
    }

    function createQuantityInput(options) {
        var opts = Object.assign({
            name: '', value: null, id: null,
            placeholder: '0,00', cssClass: ''
        }, options);

        var wrapper = document.createElement('div');
        wrapper.className = 'decimal-field quantity-field';

        var suffix = document.createElement('span');
        suffix.className = 'field-suffix';
        if (!opts.noSuffix)
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
        if (opts.noSuffix)
            input.style.paddingRight = '0.5em';
        if (opts.id) input.id = opts.id;
        if (opts.value != null) input.value = formatQuantity(String(opts.value));

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

        var opts = Object.assign({ showHint: true }, options);
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
        if (opts.noAdditionalElement) {
            input.style.paddingRight = '0.5em';
        }else {
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
        }

        // 5. Format gia tri hien tai
        var raw = stripFormatting(input.value, decimals);
        if (raw) input.value = isCurr ? formatCurrency(raw) : formatQuantity(raw);

        // 6. Bind events
        bindInput(input);

        // 7. Hint doc bang chu
        var hint = (isCurr && opts.showHint) ? attachHint(wrapper, input) : null;

        return { wrapper: wrapper, input: input, hint: hint };
    }

    // ---- Init DOM co san ------------------------------------------------

    function patchJqueryValidator() {
        if (typeof jQuery === 'undefined' || !jQuery.validator) return;
        var _number = jQuery.validator.methods.number;
        jQuery.validator.methods.number = function (value, element) {
            if (element.classList && element.classList.contains('decimal-input')) {
                value = stripFormatting(value, parseInt(element.dataset.decimals || '0', 10));
            }
            return _number.call(this, value, element);
        };
    }

    function initDecimalFields() {
        patchJqueryValidator();
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
        bindInput: bindInput,
        formatCurrency: formatCurrency,
        formatQuantity: formatQuantity,
        stripFormatting: stripFormatting
    };

})(window);
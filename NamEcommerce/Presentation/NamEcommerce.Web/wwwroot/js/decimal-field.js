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

    const numberSettings = {
        groupSeparator: '.',
        decimalSeparator: ','
    };

    // ---- Helpers -------------------------------------------------------

    function stripFormatting(str, decimals) {
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

    function stripInputFormatting(input) {
        if (!input || !(input instanceof HTMLInputElement)) throw new Error('Invalid input element');
        var value = input.value;
        if (!value || value == '0') return stripFormatting(value, 0);
        var decimals = parseInt(input.dataset.decimals, 10) || 0;
        return stripFormatting(value, decimals);
    }

    // Dùng riêng trong blur: lúc này user đã gõ, "." luôn là dấu thập phân
    // (keypress handler đảm bảo chỉ có tối đa một ".")
    function parseTypedDecimal(str) {
        if (!str) return '';
        return str.trim().replace(/[^\d.]/g, '');
    }

    function formatCurrency(raw, endSymbol, decimals) {
        // 1. Chuyển đổi thành số thực (Float) thay vì số nguyên (Int)
        const n = parseFloat(raw);
        if (isNaN(n)) return raw;

        // 2. Xác định số lượng chữ số thập phân (mặc định tiền tệ thường là 0)
        decimals = (decimals === undefined) ? 0 : parseInt(decimals, 10);

        // 3. Làm tròn số theo decimals được truyền vào
        const fixedNumber = n.toFixed(decimals);
        const parts = fixedNumber.split(numberSettings.groupSeparator);

        // Định dạng dấu chấm phân cách hàng nghìn cho phần số nguyên
        let currencyText = parts[0].replace(/\B(?=(\d{3})+(?!\d))/g, numberSettings.groupSeparator);

        // 4. Nếu có phần thập phân (và decimals > 0), ghép nó vào bằng dấu phẩy
        if (parts[1] && parseInt(parts[1], 10) > 0) {
            currencyText += numberSettings.decimalSeparator + parts[1];
        }

        // 5. Thêm ký hiệu tiền tệ ở cuối nếu có
        if (endSymbol) {
            currencyText += ' ' + endSymbol;
        }

        return currencyText;
    }

    function formatCurrencyWithSymbol(raw) {
        return formatCurrency(raw, '\u20ab');
    }

    function formatQuantity(raw, decimals) {
        // 1. Xác định số lượng chữ số thập phân (mặc định là 2 nếu undefined)
        decimals = (decimals === undefined) ? 2 : parseInt(decimals, 10);

        const n = parseFloat(raw);
        if (isNaN(n)) return raw;

        // 2. Dùng toFixed(decimals) để làm tròn chuẩn theo tham số truyền vào
        const fixedNumber = n.toFixed(decimals);
        const parts = fixedNumber.split(numberSettings.groupSeparator);

        // Định dạng dấu chấm phân cách hàng nghìn cho phần số nguyên
        const formattedInteger = parts[0].replace(/\B(?=(\d{3})+(?!\d))/g, numberSettings.groupSeparator);

        // 3. Nếu không có phần thập phân hoặc decimals = 0 thì chỉ trả về phần nguyên
        if (!parts[1] || parseInt(parts[1], 10) === 0) {
            return formattedInteger;
        }

        // 4. Trả về kết quả kết hợp với phần thập phân sau dấu phẩy
        return formattedInteger + numberSettings.decimalSeparator + parts[1];
    }

    function formatInput(input) {
        if (!input || !(input instanceof HTMLInputElement)) throw new Error('Invalid input element');
        var value = stripInputFormatting(input);
        if (!value || value === '0') return formatQuantity(value);
        var type = input.dataset.decimal || "quantity";
        var decimals = parseInt(input.dataset.decimals, 10) || 0;
        return type === 'currency' ? formatCurrency(value) : formatQuantity(value, decimals);
    }

    function getValue(input) {
        var stripped = stripInputFormatting(input);
        return parseFloat(stripped) || 0;
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

        // const newInput = input.cloneNode(true);
        // input.parentNode.replaceChild(newInput, input);
        // input = newInput;

        var type = input.dataset.type;

        input.addEventListener('focus', function () {
            this.value = stripInputFormatting(this);
            focusEnd(this);
        });

        input.addEventListener('blur', function (e) {
            // parseTypedDecimal vì tại đây value do user gõ, "." là thập phân
            // stripInputFormatting dùng heuristic "3 chữ số sau dấu chấm = hàng nghìn"
            // sẽ sai khi user gõ "1.004" với ý nghĩa một phẩy lẻ bốn
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

        input.addEventListener('keypress', function (e) {
            if (e.key == 'Enter' || e.code == 'Enter' || e.keyCode == 13)
                return;
            var decimals = parseInt(this.dataset.decimals || '0', 10);
            var char = String.fromCharCode(e.which);
            if (!/\d/.test(char) && !(decimals > 0 && char === '.')) e.preventDefault();
            if (char === '.' && this.value.includes('.')) e.preventDefault();
        });

        input.addEventListener('paste', function (e) {
            e.preventDefault();
            var decimals = parseInt(this.dataset.decimals || '0', 10);
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
        //input.placeholder = opts.placeholder !== null ? opts.placeholder : (decimals > 0 ? '0,00' : '0');
        if (opts.id) input.id = opts.id;
        if (opts.value != null) input.value = formatQuantity(String(opts.value), decimals);

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

    function getFormData(form) {
        if (!form || !(form instanceof HTMLFormElement))
            throw new Error('Form is required');
        const formData = new FormData(form);
        const decimalFields = form.querySelectorAll('.decimal-input');
        for (const decimalField of decimalFields) {
            const decimals = Number(decimalField.dataset.decimals) ?? 0;
            formData.set(decimalField.name, DecimalFields.stripFormatting(decimalField.value, decimals))
        }
        return formData;
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
        formatCurrencyWithSymbol: formatCurrencyWithSymbol,
        formatQuantity: formatQuantity,
        stripFormatting: stripFormatting,
        stripInputFormatting: stripInputFormatting,
        getFormData: getFormData,
        getValue: getValue
    };

})(window);
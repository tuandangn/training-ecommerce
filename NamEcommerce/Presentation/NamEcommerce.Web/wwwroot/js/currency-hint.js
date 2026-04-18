/**
 * so-bang-chu.js
 * Đọc số tiền Việt Nam bằng chữ
 * Hỗ trợ: 0 → hàng tỷ tỷ
 */
(function (global) {
    'use strict';

    const CH_DON_VI = ['', 'một', 'hai', 'ba', 'bốn', 'năm', 'sáu', 'bảy', 'tám', 'chín'];
    const CH_HANG   = ['', 'nghìn', 'triệu', 'tỷ'];

    /** Đọc số có 3 chữ số (0–999) */
    function docNhom(n, isFirst) {
        if (n === 0) return '';

        const tram  = Math.floor(n / 100);
        const chuc  = Math.floor((n % 100) / 10);
        const donvi = n % 10;
        let result  = '';

        if (tram > 0) {
            result += CH_DON_VI[tram] + ' trăm';
        } else if (!isFirst) {
            result += 'không trăm';
        }

        if (chuc === 0) {
            if (donvi > 0) result += ' ' + CH_DON_VI[donvi];
        } else if (chuc === 1) {
            result += ' mười';
            if (donvi === 5) result += ' lăm';
            else if (donvi > 0) result += ' ' + CH_DON_VI[donvi];
        } else {
            result += ' ' + CH_DON_VI[chuc] + ' mươi';
            if (donvi === 1) result += ' mốt';
            else if (donvi === 5) result += ' lăm';
            else if (donvi > 0) result += ' ' + CH_DON_VI[donvi];
        }

        return result.trim();
    }

    /** Đọc số nguyên bất kỳ */
    function docSo(n) {
        if (n === 0) return 'không';
        if (n < 0)   return 'âm ' + docSo(-n);

        // Tách thành nhóm 3 chữ số từ phải sang trái
        const nhoms = [];
        let tmp = n;
        while (tmp > 0) {
            nhoms.unshift(tmp % 1000);
            tmp = Math.floor(tmp / 1000);
        }

        // Xử lý trường hợp > tỷ: nhóm tỷ tỷ
        // Ghép nhóm tỷ nếu quá lớn
        let parts = [];
        const offset = nhoms.length - 1;

        nhoms.forEach(function (nhom, idx) {
            const hang  = offset - idx;           // vị trí hàng (tỷ=3, triệu=2, nghìn=1, đơn vị=0)
            const hangIdx = hang % 4;             // 0-3 map vào CH_HANG
            const isTyTy  = hang >= 4;            // tỷ tỷ, triệu tỷ ...

            if (nhom === 0) return;

            const chu = docNhom(nhom, idx === 0);
            let suffix = CH_HANG[hangIdx] || '';

            // Thêm "tỷ" lần nữa nếu là tỷ tỷ
            if (isTyTy && hangIdx === 0) suffix = 'tỷ';

            parts.push(chu + (suffix ? ' ' + suffix : ''));
        });

        return parts.join(' ');
    }

    /**
     * Chuyển số tiền → chuỗi đọc bằng chữ tiếng Việt
     * @param {number|string} soTien
     * @param {string} [donViTien='đồng']
     * @returns {string}
     */
    function docSoTien(soTien, donViTien) {
        donViTien = donViTien ?? 'đồng';

        const n = typeof soTien === 'string'
            ? parseInt(soTien.replace(/[^\d]/g, ''), 10)
            : Math.floor(soTien);

        if (isNaN(n) || soTien === '' || soTien == null) return '';

        const chu = docSo(n);
        // Viết hoa chữ cái đầu
        const chuHoa = chu.charAt(0).toUpperCase() + chu.slice(1);
        return chuHoa + ' ' + donViTien;
    }

    // Export
    global.SoBangChu = { docSoTien };

})(window);


/* ─────────────────────────────────────────────
   Tích hợp vào Currency input
   ───────────────────────────────────────────── */
(function () {
    'use strict';

    function getOrCreateHint(input) {
        const wrapper = input.closest('.currency-field');
        const fieldGroup = wrapper.closest('.form-group, .mb-3, .col') ?? wrapper.parentElement;
        if (!wrapper) return null;

        let hint = fieldGroup.querySelector('.currency-hint');
        if (!hint) {
            hint = document.createElement('div');
            hint.className = 'currency-hint';
            fieldGroup.insertAdjacentElement('beforeend', hint);
        }
        return hint;
    }

    function updateHint(input) {
        const hint = getOrCreateHint(input);
        if (!hint) return;

        const raw = input.value.replace(/[^\d]/g, '');
        if (!raw || raw === '0') {
            hint.textContent = '';
            hint.classList.remove('visible');
            return;
        }

        hint.textContent = window.SoBangChu.docSoTien(raw);
        hint.classList.add('visible');
    }

    function init() {
        document.querySelectorAll('.currency-input').forEach(function (input) {
            // Lần đầu load (nếu có giá trị sẵn)
            updateHint(input);

            // Khi đang gõ
            input.addEventListener('input', function () { updateHint(this); });

            // Sau khi blur (đã format lại) → cập nhật lại hint
            input.addEventListener('blur',  function () {
                setTimeout(() => updateHint(this), 0);
            });
        });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
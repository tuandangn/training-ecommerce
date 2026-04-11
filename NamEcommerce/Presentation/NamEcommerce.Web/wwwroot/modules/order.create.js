import { toast } from "/modules/modals.js";

(function ($) {
    let itemIndex = 0;
    const itemsTableBody = document.getElementById('itemsTableBody');
    const noItemsMessage = document.getElementById('noItemsMessage');
    const tableFooter = document.getElementById('tableFooter');
    const grandTotalLabel = document.getElementById('grandTotal');
    const addProductModal = new bootstrap.Modal('#addProductModal');
    const addProductForm = document.querySelector('#addProductModal form');

    // --- Customer Picker ---
    const customerSearch = document.getElementById('customerSearch');
    const customerHidden = document.getElementById('CustomerId');
    const customerSuggestions = document.getElementById('customerSuggestions');
    const customerDisplay = document.getElementById('selectedCustomerDisplay');
    const customerValidator = document.querySelector('[data-valmsg-for="CustomerId"]');
    let customerTimer;

    customerSearch.addEventListener('input', customerSearchHandler);
    customerSearch.addEventListener('focus', customerSearchHandler);
    function customerSearchHandler() {
        const q = this.value;
        if (customerTimer) clearTimeout(customerTimer);
        // if (!q) { customerSuggestions.style.display = 'none'; return; }

        customerTimer = setTimeout(async () => {
            const res = await fetch(`/Customer/Search?q=${encodeURIComponent(q)}`);
            const data = await res.json();
            renderCustomerSuggestions(data);
        }, 300);
    }

    function renderCustomerSuggestions(items) {
        customerSuggestions.innerHTML = '';
        if (!items || items.length === 0) {
            customerSuggestions.style.display = 'none';
            return;
        }
        items.forEach(it => {
            const btn = document.createElement('button');
            btn.type = 'button';
            btn.className = 'list-group-item list-group-item-action border-0 py-3';
            btn.innerHTML = `
                <div class="d-flex">
                    <div><i class="bi bi-person fs-4 me-3"></i></div>
                    <div>
                        <div class="fw-bold">${it.name}</div>
                        <div class="small text-muted">${it.phone}</div>
                        <div class="small text-muted">${it.address}</div>
                    </div>
                </div>
            `;
            btn.onclick = () => {
                customerHidden.value = it.id;
                document.getElementById('displayName').innerText = it.name;
                document.getElementById('displayPhone').innerText = it.phone;
                document.getElementById('displayAddress').innerText = it.address;
                customerDisplay.classList.remove('d-none');
                customerSearch.parentElement.parentElement.classList.add('d-none');
                customerSuggestions.style.display = 'none';
                customerValidator.innerText = '';
            };
            customerSuggestions.appendChild(btn);
        });
        customerSuggestions.style.display = 'block';
    }

    document.getElementById('resetCustomer').onclick = () => {
        customerHidden.value = '';
        customerDisplay.classList.add('d-none');
        customerSearch.parentElement.parentElement.classList.remove('d-none');
        customerSearch.value = '';
    };

    // --- Product Picker (Modal) ---
    const productSearch = document.getElementById('productSearch');
    const productSuggestions = document.getElementById('productSuggestions');
    const modalProductInfo = document.getElementById('modalProductInfo');
    const addItemButton = document.getElementById('addItemToTable');
    let productTimer;
    let currentProduct = null;

    productSearch.addEventListener('input', productSearchHandler);
    productSearch.addEventListener('focus', productSearchHandler);
    function productSearchHandler() {
        const q = this.value;
        if (productTimer) clearTimeout(productTimer);
        //if (!q) { productSuggestions.style.display = 'none'; return; }

        productTimer = setTimeout(async () => {
            const res = await fetch(`/Product/Search?q=${encodeURIComponent(q)}`);
            const data = await res.json();
            renderProductSuggestions(data);
        }, 300);
    }

    function renderProductSuggestions(items) {
        productSuggestions.innerHTML = '';
        if (!items || items.length === 0) { productSuggestions.style.display = 'none'; return; }
        items.forEach(it => {
            const btn = document.createElement('button');
            btn.type = 'button';
            btn.className = 'list-group-item list-group-item-action border-0 py-3';
            btn.innerHTML = `
                <div class="d-flex align-items-start gap-3">
                    <div class="text-center" style="min-width:45px;">
                        ${it.picture ? `<img src="${it.picture}" class="img-fluid img-thumbnail" style="width:45px;"/>` : '<i class="bi bi-image fs-4"></i>'}
                    </div>
                    <div>
                        <div class="fw-bold">${it.name}</div>
                        <div class="small text-muted">Tồn: ${it.availableQty}</div>
                    </div>
                </div>
            `;
            btn.onclick = () => {
                currentProduct = it;
                document.getElementById('itemProductName').innerText = it.name;
                document.getElementById('itemProductStock').innerText = it.availableQty;
                document.getElementById('itemUnitPrice').value = '';
                if (it.picture) {
                    document.getElementById('itemProductHasPicture').src = it.picture;
                    document.getElementById('itemProductHasPicture').classList.remove('d-none');
                    document.getElementById('itemProductNoPicture').classList.add('d-none');
                } else {
                    document.getElementById('itemProductHasPicture').src = '';
                    document.getElementById('itemProductHasPicture').classList.add('d-none');
                    document.getElementById('itemProductNoPicture').classList.remove('d-none');
                }
                productSuggestions.style.display = 'none';
                modalProductInfo.classList.remove('d-none');
                addItemButton.classList.remove('d-none');
                productSearch.value = it.name;
            };
            productSuggestions.appendChild(btn);
        });
        productSuggestions.style.display = 'block';
    }

    addItemButton.onclick = () => {
        const qty = parseFloat(document.getElementById('itemQuantity').value);
        const price = parseFloat(document.getElementById('itemUnitPrice').value);
        const stockQty = document.getElementById('itemProductStock').innerText;
        const picture = $('#itemProductHasPicture').attr('src');

        if (!$(addProductForm).valid())
            return;

        if (qty <= 0 || isNaN(qty)) {
            toast('Lỗi', 'Số lượng không hợp lệ', 'error');
            return;
        }

        addItemRow(currentProduct.id, currentProduct.name, qty, price, stockQty, picture);

        // Clear modal
        productSearch.value = '';
        modalProductInfo.classList.add('d-none');
        addItemButton.classList.add('d-none');
        currentProduct = null;
        addProductModal.hide();
    };

    function addItemRow(pid, name, qty, price, stockQty, picture) {
        const row = document.createElement('tr');
        row.id = `row-${itemIndex}`;
        row.className = 'align-top';
        row.innerHTML = `
            <td class="ps-4">
                <div class="d-flex gap-3">
                    <div class="text-center d-none d-lg-block" style="min-width:45px;">
                        ${picture ? `<img src="${picture}" class="img-fluid img-thumbnail" style="width:45px;"/>` : '<i class="bi bi-image fs-4 text-muted"></i>'}
                    </div>
                    <div>
                        <div class="fw-bold text-dark text-nowrap">${name}</div>
                        <div class="small text-muted">Tồn kho: ${stockQty}</div>
                        <span class="small text-danger field-validation-valid" data-valmsg-for="Items[${itemIndex}].ProductId" data-valmsg-replace="true"></span>
                    </div>
                </div>
                <input type="text" class="visually-hidden" name="Items[${itemIndex}].ProductId" value="${pid}"
                                            data-val="true" data-val-required="Vui lòng chọn hàng hóa."/>
            </td>
            <td class="text-center">
                <input type="number" name="Items[${itemIndex}].Quantity" value="${qty}" class="form-control form-control-sm text-center row-qty"
                    step="any" min="0.00000001" data-val-required="Vui lòng nhập số lượng."
                    data-val="true" data-val-range="Số lượng phải lớn hơn 0." data-val-range-min="0.000000000000000001" data-val-number="Số lượng không đúng." />
                <span class="small text-danger field-validation-valid" data-valmsg-for="Items[${itemIndex}].Quantity" data-valmsg-replace="true"></span>
            </td>
            <td class="text-end">
                <input type="number" name="Items[${itemIndex}].UnitPrice" value="${price}" style="min-width:80px;"
                    class="form-control form-control-sm text-end row-price" min="0" data-val-required="Vui lòng nhập đơn giá."
                    data-val="true" data-val-range="Đơn giá phải lớn hơn hoặc bằng 0." data-val-range-min="0" data-val-number="Đơn giá không đúng." />
                <span class="small text-danger field-validation-valid" data-valmsg-for="Items[${itemIndex}].UnitPrice" data-valmsg-replace="true"></span>
            </td>
            <td class="text-end fw-bold text-primary px-3 row-total text-nowrap d-none d-lg-table-cell">
                ${((qty * price) ?? 0).toLocaleString()} đ
            </td>
            <td class="text-end pe-4 w-auto">
                <button type="button" class="btn btn-link link-danger p-0 border-0" onclick="this.closest('tr').remove(); window.updateGrandTotal();">
                    <i class="bi bi-trash"></i>
                </button>
            </td>`;
        itemsTableBody.appendChild(row);
        itemIndex++;
        updateGrandTotal();

        // Add event listeners for recalculation
        row.querySelector('.row-qty').addEventListener('input', updateGrandTotal);
        row.querySelector('.row-price').addEventListener('input', updateGrandTotal);

        var $form = $("#createOrderForm");
        $form.removeData("validator").removeData("unobtrusiveValidation");
        $.validator.unobtrusive.parse($form);
    }

    const orderDiscountInput = document.getElementById('OrderDiscount');
    const subTotalLabel = document.getElementById('subTotal');
    const discountDisplay = document.getElementById('discountDisplay');

    orderDiscountInput.addEventListener('input', () => updateGrandTotal());

    window.updateGrandTotal = function () {
        let subTotal = 0;
        const rows = itemsTableBody.querySelectorAll('tr');
        rows.forEach(r => {
            const qInput = r.querySelector('.row-qty');
            const pInput = r.querySelector('.row-price');
            if (!qInput || !pInput) return;

            const q = parseFloat(qInput.value) || 0;
            const p = parseFloat(pInput.value) || 0;
            const lineTotal = q * p;
            r.querySelector('.row-total').innerText = lineTotal.toLocaleString() + ' đ';
            subTotal += lineTotal;
        });

        const discount = parseFloat(orderDiscountInput.value) || 0;
        const grandTotal = Math.max(0, subTotal - discount);

        subTotalLabel.innerText = subTotal.toLocaleString() + ' đ';
        discountDisplay.innerText = '- ' + discount.toLocaleString() + ' đ';
        grandTotalLabel.innerText = grandTotal.toLocaleString() + ' đ';
        if (rows.length > 0) {
            noItemsMessage.style.display = 'none';
            tableFooter.classList.remove('d-none');
        } else {
            noItemsMessage.style.display = 'block';
            tableFooter.classList.add('d-none');
        }
    };

    document.querySelector('#addProductModal').addEventListener('show.bs.modal', () => {
        $(addProductForm).valid();
    });
    addProductForm.addEventListener('submit', (e) => e.preventDefault());

    document.addEventListener('click', (e) => {
        if (!customerSuggestions.contains(e.target) && e.target !== customerSearch) {
            customerSuggestions.style.display = 'none';
        }
        if (!productSuggestions.contains(e.target) && e.target !== productSearch) {
            productSuggestions.style.display = 'none';
        }
    });
})($);

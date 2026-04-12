import { toast } from "/modules/modals.js";

(function ($) {
    let createOrderState = {
        customer: null,
        items: [],
        discount: 0,
        subTotal() {
            return this.items.reduce((subTotal, item) => subTotal += item.qty * item.unitPrice, 0);
        },
        total() {
            return Math.max(0, this.subTotal() - this.discount);
        }
    };
    function setOrderState(orderState) {
        createOrderState = orderState;
        renderOrderState();
    }
    function renderOrderState() {
        const subTotalLabel = document.getElementById('subTotal');
        const discountDisplay = document.getElementById('discountDisplay');
        const grandTotalLabel = document.getElementById('grandTotal');
        const noItemsMessage = document.getElementById('noItemsMessage');
        const tableFooter = document.getElementById('tableFooter');
        subTotalLabel.innerText = createOrderState.subTotal().toLocaleString() + ' đ';
        discountDisplay.innerText = '- ' + createOrderState.discount.toLocaleString() + ' đ';
        grandTotalLabel.innerText = createOrderState.total().toLocaleString() + ' đ';
        if (createOrderState.items.length > 0) {
            noItemsMessage.style.display = 'none';
            tableFooter.classList.remove('d-none');
        } else {
            noItemsMessage.style.display = 'block';
            tableFooter.classList.add('d-none');
        }

        const customerHidden = document.getElementById('CustomerId');
        const customerDisplay = document.getElementById('displayName');
        const customerSearch = document.getElementById('customerSearch');
        const selectedCustomerDisplay = document.getElementById('selectedCustomerDisplay');
        customerHidden.value = createOrderState.customer?.id ?? '';
        customerDisplay.innerText = createOrderState.customer?.name ?? '';
        document.getElementById('displayPhone').innerText = createOrderState.customer?.phone ?? '';
        document.getElementById('displayAddress').innerText = createOrderState.customer?.address ?? '';
        if (createOrderState.customer) {
            selectedCustomerDisplay.classList.remove('d-none');
            customerSearch.parentElement.parentElement.classList.add('d-none');
            document.getElementById('customerSuggestions').style.display = 'none';
            document.querySelector('[data-valmsg-for="CustomerId"]').innerText = '';
        } else {
            customerHidden.value = '';
            selectedCustomerDisplay.classList.add('d-none');
            customerSearch.parentElement.parentElement.classList.remove('d-none');
            customerSearch.value = '';
        }

        const itemsTableBody = document.getElementById('itemsTableBody');
        itemsTableBody.innerHTML = '';
        for (let i = 0; i < createOrderState.items.length; i++) {
            const item = createOrderState.items[i];
            const info = item.info;
            const row = renderOrderItem(i, info.id, item.qty, item.unitPrice, info.name, info.picture, info.availableQty);

            // Add event listeners for recalculation
            row.querySelector('.orderItemRemove').addEventListener('click', function () {
                const items = createOrderState.items.filter((_, index) => index !== i);
                const state = Object.assign(createOrderState, { items });
                setOrderState(state);
            });
            row.querySelector('.row-qty').addEventListener('input', function() {
                item.qty = parseFloat(this.value);
                setOrderState(createOrderState);
            });
            row.querySelector('.row-price').addEventListener('input', function () {
                item.unitPrice = parseFloat(this.value);
                setOrderState(createOrderState);
            });

            itemsTableBody.appendChild(row);
        }

        var $form = $("#createOrderForm");
        $form.removeData("validator").removeData("unobtrusiveValidation");
        $.validator.unobtrusive.parse($form);

        function renderOrderItem(itemIndex, pid, qty, price, name, picture, stockQty) {
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
                <button type="button" class="btn btn-link link-danger p-0 border-0 orderItemRemove">
                    <i class="bi bi-trash"></i>
                </button>
            </td>`;

            return row;
        }
    }

    const orderDiscountInput = document.getElementById('OrderDiscount');
    orderDiscountInput.addEventListener('input', () => {
        const orderState = Object.assign(createOrderState, { discount: parseFloat(orderDiscountInput.value) });
        setOrderState(orderState);
    });

    // --- Customer Picker ---
    let customerPickerState = {
        items: []
    };
    function setCustomerPickerState(state) {
        customerPickerState = state;
        renderCustomerPickerState();
    }
    function renderCustomerPickerState() {
        const customerSuggestions = document.getElementById('customerSuggestions');
        customerSuggestions.innerHTML = '';
        if (!customerPickerState.items || customerPickerState.items.length === 0) {
            customerSuggestions.style.display = 'none';
            return;
        }
        customerPickerState.items.forEach(it => {
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
                </div>`;
            customerSuggestions.appendChild(btn);
            btn.onclick = () => {
                const orderState = Object.assign(createOrderState, {
                    customer: {
                        id: it.id,
                        name: it.name,
                        phone: it.phone,
                        address: it.address
                    }
                });
                setOrderState(orderState);
            };
        });
        customerSuggestions.style.display = 'block';
    }

    customerSearch.addEventListener('input', customerSearchHandler);
    customerSearch.addEventListener('focus', customerSearchHandler);
    let customerTimer;
    function customerSearchHandler() {
        const q = this.value;
        if (customerTimer) clearTimeout(customerTimer);
        customerTimer = setTimeout(async () => {
            const res = await fetch(`/Customer/Search?q=${encodeURIComponent(q)}`);
            const data = await res.json();

            var customerState = Object.assign(customerPickerState, { items: data});
            setCustomerPickerState(customerState);
        }, 300);
    }
    document.getElementById('resetCustomer').onclick = () => {
        var orderState = Object.assign(createOrderState, { customer: null });
        setOrderState(orderState);
    };

    let addOrderItemState = {
        productInfo: null,
        quantity: 1,
        unitPrice: 0
    };
    function setAddOrderItemState(state) {
        addOrderItemState = state;
        renderAddOrderItemState();
    }
    function renderAddOrderItemState() {
        const productInfo = addOrderItemState.productInfo;
        document.getElementById('itemQuantity').value = addOrderItemState.quantity;
        document.getElementById('itemUnitPrice').value = addOrderItemState.unitPrice;
        if (productInfo) {
            document.getElementById('itemProductName').innerText = productInfo.name;
            document.getElementById('itemProductStock').innerText = productInfo.availableQty;
            const itemProductHasPicture = document.getElementById('itemProductHasPicture');
            const itemProductNoPicture = document.getElementById('itemProductNoPicture');
            if (productInfo.picture) {
                itemProductHasPicture.src = productInfo.picture;
                itemProductHasPicture.classList.remove('d-none');
                itemProductNoPicture.classList.add('d-none');
            } else {
                itemProductHasPicture.src = '';
                itemProductHasPicture.classList.add('d-none');
                itemProductNoPicture.classList.remove('d-none');
            }
            document.getElementById('productSuggestions').style.display = 'none';
            document.getElementById('modalProductInfo').classList.remove('d-none');
            document.getElementById('addItemToTable').classList.remove('d-none');
            document.getElementById('productSearch').value = productInfo.name;
        } else {
            document.getElementById('productSearch').value = '';
            document.getElementById('modalProductInfo').classList.add('d-none');
            document.getElementById('addItemToTable').classList.add('d-none');
        }
    }
    document.getElementById('itemQuantity').addEventListener('input', function () {
        const quantity = parseFloat(this.value);
        if (Number.isNaN(quantity)) return;
        addOrderItemState.quantity = quantity;
    });
    document.getElementById('itemUnitPrice').addEventListener('input', function () {
        const unitPrice = parseFloat(this.value);
        if (Number.isNaN(unitPrice)) return;
        addOrderItemState.unitPrice = unitPrice;
    });
    $('#addProductForm').on('submit', function(e) {
        e.preventDefault();

        if (!$(this).valid())
            return;

        const qty = addOrderItemState.quantity;
        if (qty <= 0 || isNaN(qty)) {
            toast('Lỗi', 'Số lượng không hợp lệ', 'error');
            return;
        }

        const items = Array.from(createOrderState.items);
        items.push({
            info: addOrderItemState.productInfo,
            qty: addOrderItemState.quantity,
            unitPrice: addOrderItemState.unitPrice
        });
        const state = Object.assign(createOrderState, {
            items
        });
        setOrderState(state);

        const addProductModalElement = document.getElementById('addProductModal');
        const addProductModal = bootstrap.Modal.getOrCreateInstance(addProductModalElement);
        addProductModal.hide();

        setAddOrderItemState({
            productInfo: null,
            quantity: 1,
            unitPrice: 0
        });
    });

    // --- Product Picker (Modal) ---
    let productPickerState = {
        items: [],
        selectedProduct: null
    };
    function setProductPickerState(state) {
        productPickerState = state;
        renderProductPickerState();
    }
    function renderProductPickerState() {
        const items = productPickerState.items;

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
                const productInfo = {
                    id: it.id,
                    name: it.name,
                    availableQty: it.availableQty,
                    picture: it.picture
                };
                const addItemState = Object.assign(addOrderItemState, { productInfo });
                setAddOrderItemState(addItemState);

            };
            productSuggestions.appendChild(btn);
        });
        productSuggestions.style.display = 'block';
    }

    const productSearch = document.getElementById('productSearch');
    productSearch.addEventListener('input', productSearchHandler);
    productSearch.addEventListener('focus', productSearchHandler);
    let productTimer;
    function productSearchHandler() {
        const q = this.value;
        if (productTimer) clearTimeout(productTimer);

        productTimer = setTimeout(async () => {
            const res = await fetch(`/Product/Search?q=${encodeURIComponent(q)}`);
            const data = await res.json();
            var state = Object.assign(productPickerState, { items: data });
            setProductPickerState(state);
        }, 300);
    }

    document.addEventListener('click', (e) => {
        const productSuggestions = document.getElementById('productSuggestions');
        const customerSuggestions = document.getElementById('customerSuggestions');
        if (!customerSuggestions.contains(e.target) && e.target !== customerSearch) {
            customerSuggestions.style.display = 'none';
        }
        if (!productSuggestions.contains(e.target) && e.target !== productSearch) {
            productSuggestions.style.display = 'none';
        }
    });
})($);

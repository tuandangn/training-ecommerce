import { toast } from "/modules/modals.js";

class OrderState {
    items;
    discount;
    customer;
    expectedDate;

    constructor() {
        this.items = [];
        this.discount = 0;
        this.customer = null;
    }

    subTotal() {
        return this.items.reduce((subTotal, item) => subTotal += item.quantity * item.unitPrice, 0)
    }
    total() {
        return Math.max(0, this.subTotal() - this.discount);
    }

}
class Customer {
    constructor(id, phone, name, address) {
        this.id = id;
        this.phone = phone;
        this.name = name;
        this.address = address;
    }
}
class OrderItem {
    constructor(productInfo, quantity, unitPrice) {
        this.productInfo = productInfo;
        this.quantity = quantity;
        this.unitPrice = unitPrice;
    }
}
class ProductInfo {
    constructor(id, name, availableQty, picture) {
        this.id = id;
        this.name = name;
        this.availableQty = availableQty;
        this.picture = picture;
    }
}

(function ($) {
    let createOrderState = new OrderState();
    function setOrderState(orderState) {
        createOrderState = Object.assign(new OrderState(), createOrderState, orderState);
        renderOrderState();
    }
    function onItemRemoved(index) {
        const items = createOrderState.items.filter((_, i) => index !== i);
        setOrderState({ items });
    }
    function onItemQuantityChanged(index, quantity) {
        const items = Array.from(createOrderState.items);
        const changedItem = Object.assign(new OrderItem(), items[index], { quantity });
        items[index] = changedItem;
        setOrderState({ items });
    }
    function onItemUnitPriceChanged(index, unitPrice) {
        const items = Array.from(createOrderState.items);
        const changedItem = Object.assign(new OrderItem(), items[index], { unitPrice });
        items[index] = changedItem;
        setOrderState({ items });
    }
    function renderOrderState() {
        //summary
        const orderSubTotal = document.getElementById('subTotal');
        const orderDiscount = document.getElementById('discountDisplay');
        const orderTotal = document.getElementById('grandTotal');
        const noItemsMessage = document.getElementById('noItemsMessage');
        const orderSummary = document.getElementById('tableFooter');

        orderSubTotal.innerText = createOrderState.subTotal().toLocaleString() + ' đ';
        orderDiscount.innerText = '- ' + createOrderState.discount.toLocaleString() + ' đ';
        orderTotal.innerText = createOrderState.total().toLocaleString() + ' đ';

        if (createOrderState.items.length > 0) {
            noItemsMessage.style.display = 'none';
            orderSummary.classList.remove('d-none');
        } else {
            noItemsMessage.style.display = 'block';
            orderSummary.classList.add('d-none');
        }

        //customer
        const customerId = document.getElementById('CustomerId');
        const customerDisplay = document.getElementById('displayName');
        const customerSearch = document.getElementById('customerSearch');
        const customerPhone = document.getElementById('displayPhone');
        const customerAddress = document.getElementById('displayAddress');
        const shippingAddress = document.getElementById('ShippingAddress');
        const selectedCustomerContainer = document.getElementById('selectedCustomerDisplay');
        const customerSuggestions = document.getElementById('customerSuggestions');
        const customerValidator = document.querySelector('[data-valmsg-for="CustomerId"]');

        const customer = createOrderState.customer;

        customerId.value = customer?.id ?? '';
        customerDisplay.innerText = customer?.name ?? '';
        customerPhone.innerText = customer?.phone ?? '';
        customerAddress.innerText = customer?.address ?? '';
        if (shippingAddress.hasAttribute('readonly') || !shippingAddress.value)
            shippingAddress.value = customer?.address ?? '';
        if (customer) {
            selectedCustomerContainer.classList.remove('d-none');
            customerSearch.parentElement.parentElement.classList.add('d-none');

            customerSuggestions.style.display = 'none';
            customerValidator.innerText = '';
        } else {
            selectedCustomerContainer.classList.add('d-none');
            customerSearch.parentElement.parentElement.classList.remove('d-none');
            customerValidator.innerText = 'Vui lòng chọn khách hàng.';
        }

        //order items
        const orderItemContainer = document.getElementById('itemsTableBody');
        orderItemContainer.innerHTML = '';
        for (let i = 0; i < createOrderState.items.length; i++) {
            const orderItem = createOrderState.items[i];
            const productInfo = orderItem.productInfo;
            const row = renderOrderItem(i,
                productInfo.id, orderItem.quantity, orderItem.unitPrice,
                productInfo.name, productInfo.picture, productInfo.availableQty);

            // Add event listeners for recalculation
            $('.orderItemRemove', row).click(() => onItemRemoved(i));

            let _itemQuantityTimer;
            $('.row-qty', row).on('input', function () {
                clearTimeout(_itemQuantityTimer);
                _itemQuantityTimer = setTimeout(() => {
                    let quantity = parseFloat(this.value);
                    if (Number.isNaN(quantity))
                        quantity = 0;
                    onItemQuantityChanged(i, quantity);
                    this.focus();
                }, 500);
            });
            let _itemUnitPriceTimer;
            row.querySelector('.row-price').addEventListener('input', function () {
                clearTimeout(_itemUnitPriceTimer);
                _itemUnitPriceTimer = setTimeout(() => {
                    let unitPrice = parseFloat(this.value);
                    if (Number.isNaN(unitPrice))
                        unitPrice = 0;
                    onItemUnitPriceChanged(i, unitPrice);
                }, 700);
            });

            orderItemContainer.appendChild(row);
        }
        if (createOrderState.items.length) {
            $('#OrderDiscount')
                .attr('disabled', false)
                .attr('data-val-range', `Giảm giá tối đa không quá ${createOrderState.subTotal().toLocaleString()} đ.`)
                .attr('data-val-range-max', createOrderState.subTotal());
        } else {
            $('#OrderDiscount')
                .attr('disabled', true)
                .attr('data-val-range', false)
                .attr('data-val-range-max', false);
        }

        //validate
        var $form = $("#createOrderForm");
        $form.removeData("validator").removeData("unobtrusiveValidation");
        $.validator.unobtrusive.parse($form);

        let validExpectedDate = true;
        if (createOrderState.expectedDate) {
            const today = new Date();
            today.setHours(0, 0, 0, 0);
            if (createOrderState.expectedDate.getTime() < today.getTime()) {
                document.querySelector('[data-valmsg-for="ExpectedShippingDate"]').innerText = 'Ngày giao dự kiến phải lớn hơn hiện tại.';
                validExpectedDate = false;
            }
        }
        if (validExpectedDate)
            document.querySelector('[data-valmsg-for="ExpectedShippingDate"]').innerText = '';

        //submit
        $('#createOrderForm :submit').attr('disabled', !createOrderState.customer || !createOrderState.items.length || !validExpectedDate);

        //helpers
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

    // discount
    let _discountTimer;
    $('#OrderDiscount').on('input blur', function () {
        clearTimeout(_discountTimer);
        _discountTimer = setTimeout(() => {
            let discount = parseFloat(this.value);

            if (Number.isNaN(discount) || discount < 0)
                discount = 0;

            setOrderState({ discount: Math.min(createOrderState.subTotal(), discount) });
        }, 300);
    });

    $('#ExpectedShippingDate').on('change', function () {
        setOrderState({ expectedDate: this.value ? new Date(this.value) : null });
    });

    // customer
    let customerPickerState = {
        customers: []
    };
    function onCustomerSelect(customer) {
        setOrderState({
            customer: customer ? new Customer(customer.id, customer.name, customer.phone, customer.address) : null
        });
    }
    function setCustomerPickerState(state) {
        customerPickerState = Object.assign({}, customerPickerState, state);
        renderCustomerPickerState();
    }
    function renderCustomerPickerState() {
        const customers = customerPickerState.customers;

        const customerSuggestions = document.getElementById('customerSuggestions');
        customerSuggestions.style.display = 'block';

        if (!customers || customers.length === 0) {
            customerSuggestions.innerHTML = '<div class="text-muted text-center list-group-item p-4 small">Không tìm thấy khách hàng nào.</div>';
            onCustomerSelect(null);
            return;
        }

        customerSuggestions.innerHTML = '';
        customers.forEach(customer => {
            const btn = document.createElement('button');
            btn.type = 'button';
            btn.className = 'list-group-item list-group-item-action border-0 py-3';
            btn.innerHTML = `
                <div class="d-flex">
                    <div><i class="bi bi-person fs-4 me-3"></i></div>
                    <div>
                        <div class="fw-bold">${customer.name}</div>
                        <div class="small text-muted">${customer.phone}</div>
                        <div class="small text-muted">${customer.address}</div>
                    </div>
                </div>`;
            customerSuggestions.appendChild(btn);
            btn.onclick = () => {
                onCustomerSelect(customer);
            };
        });
    }

    let _customerTimer;
    $('#customerSearch').on('input focus', function () {
        const q = this.value;
        if (_customerTimer) clearTimeout(_customerTimer);
        _customerTimer = setTimeout(async () => {
            const res = await fetch(`/Customer/Search?q=${encodeURIComponent(q)}`);
            const data = await res.json();

            setCustomerPickerState({ customers: data });
        }, 300);
    });

    document.getElementById('resetCustomer').onclick = () => {
        setOrderState({ customer: null });
    };

    // item
    let addOrderItemState = {
        productInfo: null,
        quantity: 1,
        unitPrice: 0
    };
    function setAddOrderItemState(state) {
        addOrderItemState = Object.assign({}, addOrderItemState, state);
        renderAddOrderItemState();
    }
    function renderAddOrderItemState() {
        document.getElementById('itemQuantity').value = addOrderItemState.quantity;
        document.getElementById('itemUnitPrice').value = addOrderItemState.unitPrice;

        const productInfo = addOrderItemState.productInfo;
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
        } else {
            document.getElementById('modalProductInfo').classList.add('d-none');
            document.getElementById('addItemToTable').classList.add('d-none');
        }
    }
    document.getElementById('itemQuantity').addEventListener('input', function () {
        let quantity = parseFloat(this.value);

        if (Number.isNaN(quantity) || quantity < 0)
            quantity = 0;

        addOrderItemState.quantity = quantity;
    });
    document.getElementById('itemUnitPrice').addEventListener('input', function () {
        let unitPrice = parseFloat(this.value);

        if (Number.isNaN(unitPrice) || unitPrice < 0)
            unitPrice = 0;

        addOrderItemState.unitPrice = unitPrice;
    });
    $('#addProductForm').on('submit', function (e) {
        e.preventDefault();

        if (!$(this).valid())
            return;

        const qty = addOrderItemState.quantity;
        if (qty <= 0 || isNaN(qty)) {
            toast('Lỗi', 'Số lượng không hợp lệ', 'error');
            return;
        }

        const orderItems = Array.from(createOrderState.items);
        orderItems.push(new OrderItem(addOrderItemState.productInfo, addOrderItemState.quantity, addOrderItemState.unitPrice));
        setOrderState({
            items: orderItems
        });

        const addProductModal = bootstrap.Modal.getOrCreateInstance(document.getElementById('addProductModal'));
        addProductModal.hide();

        setAddOrderItemState({
            productInfo: null,
            quantity: 1,
            unitPrice: 0
        });
    });

    // product picker
    let productPickerState = {
        products: []
    };
    function onProductSelect(product) {
        const productInfo = product ? new ProductInfo(product.id, product.name, product.availableQty, product.picture) : null;
        setAddOrderItemState({ productInfo });
    }
    function setProductPickerState(state) {
        productPickerState = Object.assign({}, productPickerState, state);
        renderProductPickerState();
    }
    function renderProductPickerState() {
        const products = productPickerState.products;

        const productSuggestions = document.getElementById('productSuggestions');
        productSuggestions.style.display = 'block';

        if (!products || products.length === 0) {
            productSuggestions.innerHTML = '<div class="text-muted text-center list-group-item p-4 small">Không tìm thấy hàng hóa nào.</div>';
            onProductSelect(null);
            return;
        }

        productSuggestions.innerHTML = '';
        products.forEach(product => {
            const btn = document.createElement('button');
            btn.type = 'button';
            btn.className = 'list-group-item list-group-item-action border-0 py-3';
            btn.innerHTML = `
                <div class="d-flex align-items-start gap-3">
                    <div class="text-center" style="min-width:45px;">
                        ${product.picture ? `<img src="${product.picture}" class="img-fluid img-thumbnail" style="width:45px;"/>` : '<i class="bi bi-image fs-4"></i>'}
                    </div>
                    <div>
                        <div class="fw-bold">${product.name}</div>
                        <div class="small text-muted">Tồn: ${product.availableQty}</div>
                    </div>
                </div>
            `;
            btn.onclick = () => onProductSelect(product);
            productSuggestions.appendChild(btn);
        });
    }

    let _productTimer;
    $('#productSearch').on('input focus', function () {
        const q = this.value;
        if (_productTimer) clearTimeout(_productTimer);

        _productTimer = setTimeout(async () => {
            const res = await fetch(`/Product/Search?q=${encodeURIComponent(q)}`);
            const data = await res.json();

            setProductPickerState({ products: data });
        }, 300);
    })

    // misc
    $(document).on('click', function (e) {
        const customerSuggestions = document.getElementById('customerSuggestions');
        const customerSearch = document.getElementById('customerSearch');
        if (!customerSuggestions.contains(e.target) && e.target !== customerSearch) {
            customerSuggestions.style.display = 'none';
        }

        const productSuggestions = document.getElementById('productSuggestions');
        const productSearch = document.getElementById('productSearch');
        if (!productSuggestions.contains(e.target) && e.target !== productSearch) {
            productSuggestions.style.display = 'none';
        }
    });

    $('#btnEditShippingAddress').on('click', function () {
        const shippingAddress = document.getElementById('ShippingAddress');

        $(shippingAddress).attr('readonly', false)
            .attr('placeholder', 'Vui lòng nhập địa chỉ giao hàng.')
            .removeClass('border-end-0')
            .focus();

        $(this).remove();
    });
})($);

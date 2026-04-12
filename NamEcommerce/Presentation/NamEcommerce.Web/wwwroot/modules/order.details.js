import { confirm, toast } from "/modules/modals.js";
import PromiseModal from "/modules/PromiseModal.js";

(function () {
    // Add item
    const txtSearchProduct = document.getElementById('productPickerDisplay');
    const txtProductId = document.getElementById('productId');
    const productSuggestions = document.getElementById('productSuggestions');

    const addItemForm = document.getElementById('addItemForm');
    const addItemQty = document.getElementById('quantityInput');
    const addItemUnitPrice = document.getElementById('unitPriceInput');
    const btnAddItemSubmit = document.getElementById('btnAddItemSubmit');
    let timer;

    if (txtProductId) {
        txtSearchProduct.addEventListener('input', searchProductHandler);
        txtSearchProduct.addEventListener('focus', searchProductHandler);
        function searchProductHandler() {
            const q = this.value;
            txtProductId.value = '';
            if (timer) clearTimeout(timer);
            if (!q) {
                txtProductId.value = '';
                addItemUnitPrice.value = '';
                btnAddItemSubmit.disabled = true;
                addItemUnitPrice.disabled = true;
                addItemQty.disabled = true;
            }

            timer = setTimeout(async () => {
                try {
                    this.readonly = true;
                    const res = await fetch(`/Product/Search?q=${encodeURIComponent(q)}`);
                    if (res.ok) {
                        const data = await res.json();
                        renderSuggestions(data);
                    }
                    this.readonly = false;
                } catch (err) { console.error(err); }
            }, 300);
        }
        function renderSuggestions(items) {
            productSuggestions.innerHTML = '';
            if (!items || items.length === 0) { productSuggestions.style.display = 'none'; return; }

            items.forEach(it => {
                const btn = document.createElement('button');
                btn.type = 'button';
                btn.className = 'list-group-item list-group-item-action border-0 py-3 d-flex align-items-center gap-3';
                btn.innerHTML = `<div class="d-flex align-items-start gap-3">
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
                    txtSearchProduct.value = it.name;
                    txtProductId.value = it.id;
                    addItemUnitPrice.value = it.price;
                    productSuggestions.style.display = 'none';
                    btnAddItemSubmit.disabled = false;
                    addItemUnitPrice.disabled = false;
                    addItemQty.disabled = false;
                    addItemQty.focus();
                };
                productSuggestions.appendChild(btn);
            });
            productSuggestions.style.display = 'block';
        }

        document.addEventListener('click', (e) => {
            if (!productSuggestions.contains(e.target) && e.target !== txtSearchProduct) {
                productSuggestions.style.display = 'none';
            }
        });

        $('#addItemForm').on('submit', async function (e) {
            e.preventDefault();
            if (!$(this).valid())
                return;

            const data = $(this).serialize();
            try {
                const res = await fetch(this.action, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                    body: data
                });
                const result = await res.json();

                if (result.success) {
                    await toast('Thành công', 'Đã thêm hàng hóa vào đơn hàng.', 'success');
                    location.reload();
                } else {
                    toast('Lỗi', result.message || 'Không thể thêm hàng hóa vào đơn hàng.', 'error');
                }
            } catch (err) {
                toast('Lỗi', 'Có lỗi xảy ra khi gửi yêu cầu.', 'error');
            }
        });
    }

    // Edit item
    const editItemModal = document.getElementById('editItemModal');
    if (editItemModal) {
        const editModal = new PromiseModal('#editItemModal');

        $('.btnEditItem').on('click', function () {
            const data = $(this).data();
            $('#editItemId').val(data.id);
            $('#editProductName').text(data.product);
            $('#editQuantity').val(data.qty);
            $('#editUnitPrice').val(data.price);
            $('#editProductStock').text(data.availableqty);

            var $img = $(this).closest('tr').find('.item-product-img');
            var pictureUrl = $img.attr('src');
            if (pictureUrl) {
                $('#editProductHasPicture').attr('src', pictureUrl).removeClass('d-none');
                $('#editProductNoPicture').addClass('d-none')
            } else {
                $('#editProductHasPicture').attr('src', '').addClass('d-none');
                $('#editProductNoPicture').removeClass('d-none')
            }

            editModal.show();
        });

        $('#editItemForm').on('submit', async function (e) {
            e.preventDefault();

            if (!$(this).valid())
                return;

            await editModal.hide();

            const data = $(this).serialize();
            try {
                const res = await fetch(this.action, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                    body: data
                });
                const result = await res.json();

                if (result.success) {
                    await toast('Thành công', 'Đã cập nhật hàng hóa của đơn hàng.', 'success');
                    location.reload();
                } else {
                    toast('Lỗi', result.message || 'Không thể cập nhật hàng hóa của đơn hàng.', 'error');
                }
            } catch (err) {
                toast('Lỗi', 'Có lỗi xảy ra khi gửi yêu cầu.', 'error');
            }
        });
    }

    // Remove item
    $('.btnRemoveItem').on('click', async function () {
        const id = $(this).data('id');
        const name = $(this).data('name');
        const orderId = $(this).data('order');

        const confirmed = await confirm('Xác nhận xóa', `Bạn có chắc chắn muốn xóa <strong>${name}</strong> khỏi đơn hàng không?`);
        if (confirmed) {
            try {
                const res = await fetch('/Order/RemoveOrderItem', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                    body: `OrderId=${orderId}&ItemId=${id}`
                });
                const result = await res.json();
                if (result.success) {
                    location.reload();
                } else {
                    toast('Lỗi', result.message || 'Không thể xóa hàng hóa khỏi đơn hàng.', 'error');
                }
            } catch (err) {
                toast('Lỗi', 'Có lỗi xảy ra khi gửi yêu cầu.', 'error');
            }
        }
    });

    // Lock
    $('#lockForm').on('submit', async function (e) {
        e.preventDefault();

        if (!$(this).valid())
            return;

        const lockModal = new PromiseModal('#lockModal');
        await lockModal.hide();

        const data = $(this).serialize();
        try {
            const res = await fetch(this.action, {
                method: 'POST',
                headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                body: data
            });
            const result = await res.json();

            if (result.success) {
                await toast('Thành công', 'Đã khóa đơn hàng này.', 'success');
                location.reload();
            } else {
                toast('Lỗi', result.message || 'Không thể khóa đơn hàng.', 'error');
            }
        } catch (err) {
            toast('Lỗi', 'Có lỗi xảy ra khi gửi yêu cầu.', 'error');
        }
    });

    // Discount
    const editDiscountModalElement = document.getElementById('editDiscountModal');
    if (editDiscountModalElement) {
        $('#editDiscountForm').on('submit', async function (e) {
            e.preventDefault();

            if (!$(this).valid())
                return;

            const editDiscountModal = new PromiseModal('#editDiscountModal');
            await editDiscountModal.hide();

            const data = $(this).serialize();
            try {
                const res = await fetch(this.action, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                    body: data
                });
                const result = await res.json();

                if (result.success) {
                    await toast('Thành công', 'Đã cập nhật giảm giá cho đơn hàng.', 'success');
                    location.reload();
                } else {
                    toast('Lỗi', result.message || 'Không thể cập nhật giảm giá cho đơn hàng.', 'error');
                }
            } catch (err) {
                toast('Lỗi', 'Có lỗi xảy ra khi gửi yêu cầu.', 'error');
            }
        });
    }

    // Edit note
    const changeNoteModalElement = document.getElementById('changeNoteModal');
    if (changeNoteModalElement) {
        changeNoteModalElement.addEventListener('shown.bs.modal', () => $('#orderNote').focusEnd());

        $('#changeNoteForm').on('submit', async function (e) {
            e.preventDefault();

            if (!$(this).valid())
                return;

            const changeNoteModal = new PromiseModal('#changeNoteModal');
            await changeNoteModal.hide();

            var note = $(this).find('[name="Note"]');

            const data = $(this).serialize();
            try {
                const res = await fetch(this.action, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                    body: data
                });
                const result = await res.json();

                if (result.success) {
                    if (note)
                        await toast('Thành công', 'Đã cập nhật ghi chú đơn hàng.', 'success');
                    else
                        await toast('Thành công', 'Đã xóa ghi chú đơn hàng.', 'success');
                    location.reload();
                } else {
                    toast('Lỗi', result.message || 'Không thể cập nhật ghi chú đơn hàng.', 'error');
                }
            } catch (err) {
                toast('Lỗi', 'Có lỗi xảy ra khi gửi yêu cầu.', 'error');
            }
        });
    }

    // Shipping
    const shippingModalElement = document.getElementById('shippingModal');
    if (shippingModalElement) {
        $('#shippingForm').on('submit', async function (e) {
            e.preventDefault();

            if (!$(this).valid())
                return;

            const shippingModal = new PromiseModal('#shippingModal');
            await shippingModal.hide();

            const data = $(this).serialize();
            try {
                const res = await fetch(this.action, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                    body: data
                });
                const result = await res.json();

                if (result.success) {
                    await toast('Thành công', 'Đã cập nhật thông tin giao hàng.', 'success');
                    location.reload();
                } else {
                    toast('Lỗi', result.message || 'Không thể cập nhật thông tin giao hàng.', 'error');
                }
            } catch (err) {
                toast('Lỗi', 'Có lỗi xảy ra khi gửi yêu cầu.', 'error');
            }
        });
    }
})();

import { confirm, toast } from "/modules/modals.js";

(function () {
    // Product Picker
    const display = document.getElementById('productPickerDisplay');
    const hiddenId = document.getElementById('productId');
    const suggestions = document.getElementById('productSuggestions');
    const addItemForm = document.getElementById('addItemForm');
    const qtyInput = document.getElementById('quantityInput');
    const priceInput = document.getElementById('unitPriceInput');
    let timer;

    function renderSuggestions(items) {
        suggestions.innerHTML = '';
        if (!items || items.length === 0) { suggestions.style.display = 'none'; return; }

        items.forEach(it => {
            const btn = document.createElement('button');
            btn.type = 'button';
            btn.className = 'list-group-item list-group-item-action border-0 py-3 d-flex align-items-center gap-3';
            btn.innerHTML = `
                        <div class="rounded-2 bg-light p-2"><i class="bi bi-box text-primary"></i></div>
                        <div>
                            <div class="fw-bold text-dark">${it.name}</div>
                            <div class="small text-muted">${it.price.toLocaleString()} đ | Kho: ${it.inventoryQuantity}</div>
                        </div>
                    `;
            btn.onclick = () => {
                display.value = it.name;
                hiddenId.value = it.id;
                priceInput.value = it.price;
                suggestions.style.display = 'none';
                qtyInput.focus();
            };
            suggestions.appendChild(btn);
        });
        suggestions.style.display = 'block';
    }

    display.addEventListener('input', function () {
        const q = this.value;
        hiddenId.value = '';
        if (timer) clearTimeout(timer);
        if (!q) { suggestions.style.display = 'none'; return; }

        timer = setTimeout(async () => {
            try {
                const res = await fetch(`/Product/Search?q=${encodeURIComponent(q)}`);
                if (res.ok) {
                    const data = await res.json();
                    renderSuggestions(data);
                }
            } catch (err) { console.error(err); }
        }, 300);
    });

    document.addEventListener('click', (e) => {
        if (!suggestions.contains(e.target) && e.target !== display) {
            suggestions.style.display = 'none';
        }
    });

    // Edit Item
    const editModal = new bootstrap.Modal('#editItemModal');

    $('.btnEditItem').on('click', function () {
        const data = $(this).data();
        $('#editItemId').val(data.id);
        $('#editProductName').text(data.product);
        $('#editQuantity').val(data.qty);
        $('#editUnitPrice').val(data.price);
        $('#editProductStock').text(data.availableQty);

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

    $('#submitEditItem').on('click', async function () {
        const data = $('#editItemForm').serialize();
        try {
            const res = await fetch('/Order/UpdateItem', {
                method: 'POST',
                headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                body: data
            });
            const result = await res.json();
            if (result.success) {
                toast('Thành công', 'Đã cập nhật sản phẩm.', 'success');
                location.reload();
            } else {
                toast('Lỗi', result.message || 'Không thể cập nhật sản phẩm.', 'error');
            }
        } catch (err) {
            toast('Lỗi', 'Có lỗi xảy ra khi gửi yêu cầu.', 'error');
        }
    });

    // Update Discount
    $('#submitDiscount').on('click', async function () {
        const data = $('#editDiscountForm').serialize();
        try {
            const res = await fetch('/Order/UpdateOrder', {
                method: 'POST',
                headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                body: data
            });
            const result = await res.json();
            if (result.success) {
                toast('Thành công', 'Đã cập nhật giảm giá.', 'success');
                location.reload();
            } else {
                toast('Lỗi', result.message || 'Lỗi cập nhật.', 'error');
            }
        } catch (err) {
            toast('Lỗi', 'Lỗi hệ thống.', 'error');
        }
    });

    window.editNote = async () => {
        const newNote = prompt("Nhập ghi chú mới:", "@Model.Note");
        if (newNote !== null) {
            const res = await fetch('/Order/UpdateOrder', {
                method: 'POST',
                headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                body: `Id=@Model.Id&Note=${encodeURIComponent(newNote)}`
            });
            if ((await res.json()).success) location.reload();
        }
    };

    // Remove Item
    $('.btnRemoveItem').on('click', async function () {
        const id = $(this).data('id');
        const name = $(this).data('name');
        const orderId = '@Model.Id';

        const confirmed = await confirm('Xác nhận xóa', `Bạn có chắc chắn muốn xóa <strong>${name}</strong> khỏi đơn hàng không?`);
        if (confirmed) {
            try {
                const res = await fetch('/Order/RemoveItem', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                    body: `OrderId=${orderId}&ItemId=${id}`
                });
                const result = await res.json();
                if (result.success) {
                    toast('Thành công', 'Đã xóa sản phẩm khỏi đơn hàng.', 'success');
                    location.reload();
                } else {
                    toast('Lỗi', result.message || 'Không thể xóa sản phẩm.', 'error');
                }
            } catch (err) {
                toast('Lỗi', 'Có lỗi xảy ra khi gửi yêu cầu.', 'error');
            }
        }
    });
})();

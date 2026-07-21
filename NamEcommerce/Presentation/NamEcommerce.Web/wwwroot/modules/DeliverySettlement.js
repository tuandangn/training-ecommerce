import DecimalFields from '/modules/DecimalFields.js';

function parseNum(value) {
    const parsed = Number(String(value ?? '').replace(/[^\d.-]/g, ''));
    return Number.isFinite(parsed) ? parsed : 0;
}

function formatMoney(value) {
    return `${Number(value || 0).toLocaleString('vi-VN', { maximumFractionDigits: 0 })}đ`;
}

function getReturnedValue(form) {
    let value = 0;
    form.querySelectorAll('.delivery-return-line').forEach(line => {
        const unitPrice = parseNum(line.dataset.unitPrice);
        const input = line.querySelector('.returned-quantity-input');
        let returned = input ? DecimalFields.getValue(input) : 0;
        if (returned < 0) returned = 0;
        value += returned * unitPrice;
    });
    return value;
}

function getCash(form) {
    const cashInput = form.querySelector('input[name="cashCollectedAmount"]');
    if (!cashInput) return 0;
    return cashInput.type === 'hidden' ? parseNum(cashInput.value) : DecimalFields.getValue(cashInput);
}

function computeProposed(form) {
    const cod = parseNum(form.dataset.amountToCollect);
    const returnedValue = getReturnedValue(form);
    const cash = getCash(form);
    const dueAfterReturn = Math.max(0, cod - returnedValue);
    const shortfall = returnedValue > 0 || cash < dueAfterReturn;
    const proposed = Math.min(dueAfterReturn, cash > 0 ? cash : dueAfterReturn);
    return { shortfall, proposed, dueAfterReturn };
}

export function installSettlementForms() {
    document.querySelectorAll('.delivery-complete-form[data-settlement="1"]').forEach(form => {
        const fullButton = form.querySelector('.btn-complete-full');
        const requestButton = form.querySelector('.btn-request-settlement');
        const hint = form.querySelector('.settlement-hint');
        const proposedLabel = form.querySelector('.settlement-proposed');
        if (!requestButton) return;

        const refresh = () => {
            const { shortfall, proposed } = computeProposed(form);
            if (fullButton) fullButton.classList.toggle('d-none', shortfall);
            requestButton.classList.toggle('d-none', !shortfall);
            if (hint) hint.classList.toggle('d-none', !shortfall);
            if (proposedLabel) proposedLabel.textContent = formatMoney(proposed);
        };

        form.addEventListener('input', refresh);
        refresh();

        requestButton.addEventListener('click', async () => {
            const status = form.querySelector('.delivery-complete-status');
            const rejectReason = String(form.querySelector('input[name="rejectReason"]')?.value || '').trim();

            const proof = form.querySelector('input[name="proofFile"]')?.files?.[0];
            if (!proof) {
                if (status) status.textContent = 'Vui lòng chụp ảnh hàng hóa.';
                return;
            }

            requestButton.disabled = true;
            if (status) status.textContent = 'Đang gửi admin duyệt...';

            const { proposed } = computeProposed(form);
            let hasReturnedQuantity = false;
            const items = Array.from(form.querySelectorAll('.delivery-return-line')).map(line => {
                const input = line.querySelector('.returned-quantity-input');
                let returned = input ? DecimalFields.getValue(input) : 0;
                if (returned < 0) returned = 0;
                if (returned > 0) hasReturnedQuantity = true;
                return {
                    productId: line.dataset.productId,
                    returnedQuantity: returned,
                    rejectReason: returned > 0 ? rejectReason : null
                };
            });
            if (hasReturnedQuantity && !rejectReason) {
                if (status) status.textContent = 'Vui lòng nhập lý do trả hàng.';
                requestButton.disabled = false;
                return;
            }

            const formData = new FormData();
            formData.set('deliveryRunId', form.dataset.runId);
            formData.set('deliveryNoteId', form.dataset.noteId);
            formData.set('receiverName', form.querySelector('input[name="receiverName"]')?.value || '');
            formData.set('proposedAmountToCollect', proposed);
            formData.set('acceptanceItemsJson', JSON.stringify(items));
            formData.set('proofFile', proof, proof.name);

            try {
                const response = await fetch('/DeliveryMobile/RequestSettlement', { method: 'POST', body: formData });
                const result = await response.json();
                if (result?.success) {
                    location.reload();
                    return;
                }

                requestButton.disabled = false;
                if (status) status.textContent = result?.message || 'Không gửi được';
            } catch {
                requestButton.disabled = false;
                if (status) status.textContent = 'Lỗi mạng, thử lại';
            }
        });
    });

    document.querySelectorAll('.btn-confirm-approved').forEach(button => {
        button.addEventListener('click', async () => {
            button.disabled = true;

            const formData = new FormData();
            formData.set('deliveryRunId', button.dataset.runId);
            formData.set('deliveryNoteId', button.dataset.noteId);

            try {
                const response = await fetch('/DeliveryMobile/ConfirmApprovedDelivery', { method: 'POST', body: formData });
                const result = await response.json();
                if (result?.success) {
                    location.reload();
                    return;
                }

                button.disabled = false;
                alert(result?.message || 'Không xác nhận được');
            } catch {
                button.disabled = false;
                alert('Lỗi mạng, thử lại');
            }
        });
    });
}

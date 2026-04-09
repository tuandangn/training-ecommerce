const confirmModalId = 'confirmModal';
const btnConfirmSelector = '.btnConfirm';

function createModal(id, title, body) {
    const modalElement = document.getElementById(id);
    const modal = bootstrap.Modal.getOrCreateInstance(modalElement);

    const titleElement = modalElement.querySelector('.modal-header .modal-title');
    if (titleElement) titleElement.innerHTML = title;

    const bodyElement = modalElement.querySelector('.modal-body');
    if (bodyElement) bodyElement.innerHTML = body;

    return [modal, modalElement];
}

function confirm(title, body) {
    const [confirmModal, modalElement] = createModal(confirmModalId, title, body);
    const btnConfirm = modalElement.querySelector(btnConfirmSelector);

    var promise = new Promise(resolve => {
        let isConfirmed = false;

        modalElement.addEventListener('show.bs.modal', function onShow(e) {
            modalElement.removeEventListener('show.bs.modal', onShow);
            btnConfirm.addEventListener('click', onConfirm);
        });
        function onConfirm(e) {
            e.preventDefault();
            isConfirmed = true;
            confirmModal.hide();
        }

        modalElement.addEventListener('hidden.bs.modal', function onHidden(e) {
            modalElement.removeEventListener('hidden.bs.modal', onHidden);
            btnConfirm.removeEventListener('click', onConfirm);

            resolve(isConfirmed);
        });
    }).catch(function () {
        confirmModal.hide();
        return Promise.resolve(false);
    });

    confirmModal.show();

    return promise;
}

function toast() { }

export { confirm, toast };

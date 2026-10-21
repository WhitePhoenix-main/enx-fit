(() => {
    'use strict';
    // Catalog previews and workspace dialogs share the native dialog contract.
    document.addEventListener('click', event => {
        const trigger = event.target.closest('[data-dialog]');
        if (trigger) {
            const dialog = document.getElementById(trigger.dataset.dialog);
            if (!(dialog instanceof HTMLDialogElement)) return;
            event.preventDefault();
            trigger.closest('details')?.removeAttribute('open');
            if (!dialog.open) dialog.showModal();
        }
        event.target.closest('[data-close]')?.closest('dialog')?.close();
        if (event.target instanceof HTMLDialogElement) {
            const box = event.target.getBoundingClientRect();
            if (event.clientX < box.left || event.clientX > box.right || event.clientY < box.top || event.clientY > box.bottom) event.target.close();
        }
    });
})();

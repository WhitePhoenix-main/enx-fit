(() => {
    'use strict';
    const dialog = document.getElementById('enix-navigation-dialog');
    const trigger = document.querySelector('[data-app-menu-open]');
    if (!(dialog instanceof HTMLDialogElement) || !trigger) return;
    trigger.addEventListener('click', () => {
        if (!dialog.open) dialog.showModal();
        trigger.setAttribute('aria-expanded', 'true');
    });
    dialog.addEventListener('click', event => {
        if (!(event.target instanceof Element)) return;
        if (event.target.closest('[data-app-menu-close], a')) dialog.close();
        if (event.target === dialog) {
            const box = dialog.getBoundingClientRect();
            if (event.clientX < box.left || event.clientX > box.right || event.clientY < box.top || event.clientY > box.bottom) dialog.close();
        }
    });
    dialog.addEventListener('close', () => trigger.setAttribute('aria-expanded', 'false'));
    matchMedia('(min-width:1024px)').addEventListener('change', event => {
        if (event.matches && dialog.open) dialog.close();
    });
})();

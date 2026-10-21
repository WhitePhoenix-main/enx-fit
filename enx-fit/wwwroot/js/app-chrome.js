(() => {
    'use strict';
    const dialog = document.getElementById('enix-navigation-dialog');
    const triggers = [...document.querySelectorAll('[data-app-menu-open]')];
    if (!(dialog instanceof HTMLDialogElement) || !triggers.length) return;
    let opener;
    triggers.forEach(trigger => trigger.addEventListener('click', () => {
        opener = trigger;
        if (!dialog.open) dialog.showModal();
        triggers.forEach(item => item.setAttribute('aria-expanded', 'true'));
    }));
    dialog.addEventListener('click', event => {
        if (!(event.target instanceof Element)) return;
        if (event.target.closest('[data-app-menu-close], a')) dialog.close();
        if (event.target === dialog) {
            const box = dialog.getBoundingClientRect();
            if (event.clientX < box.left || event.clientX > box.right || event.clientY < box.top || event.clientY > box.bottom) dialog.close();
        }
    });
    dialog.addEventListener('close', () => {
        triggers.forEach(item => item.setAttribute('aria-expanded', 'false'));
        if (opener?.getClientRects().length) opener.focus();
    });
})();

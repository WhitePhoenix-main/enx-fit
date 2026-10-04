(() => {
  'use strict';
  document.addEventListener('click', event => {
    const trigger = event.target.closest('[data-mobile-dialog]');
    if (trigger) {
      const dialog = document.getElementById(trigger.dataset.mobileDialog);
      if (dialog instanceof HTMLDialogElement) {
        document.querySelectorAll('.em-mobile-dialog[open]').forEach(item => item.close());
        dialog.showModal();
      }
    }
    const close = event.target.closest('[data-mobile-close], .em-mobile-menu-links a');
    if (close) close.closest('dialog')?.close();
    if (event.target instanceof HTMLDialogElement && event.target.matches('.em-mobile-dialog')) {
      const box = event.target.getBoundingClientRect();
      if (event.clientX < box.left || event.clientX > box.right || event.clientY < box.top || event.clientY > box.bottom) event.target.close();
    }
  });
  window.matchMedia('(min-width:1024px)').addEventListener('change', event => {
    if (event.matches) document.querySelectorAll('.em-mobile-dialog[open]').forEach(dialog => dialog.close());
  });
})();

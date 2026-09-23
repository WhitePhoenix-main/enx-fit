(() => {
  'use strict';
  const openDialog = id => {
    const dialog = document.getElementById(id);
    if (!(dialog instanceof HTMLDialogElement)) return;
    document.querySelectorAll('dialog[open]').forEach(item => item.close());
    dialog.showModal();
  };
  document.addEventListener('click', event => {
    const trigger = event.target.closest('[data-dialog]');
    if (trigger) openDialog(trigger.dataset.dialog);
    const close = event.target.closest('[data-close]');
    if (close) close.closest('dialog').close();
    if (event.target instanceof HTMLDialogElement) {
      const box = event.target.getBoundingClientRect();
      if (event.clientX < box.left || event.clientX > box.right || event.clientY < box.top || event.clientY > box.bottom) event.target.close();
    }
    if (event.target.closest('.search-results a')) event.target.closest('dialog').close();
    const menu = document.querySelector('.account-menu');
    if (menu && !menu.contains(event.target)) menu.open = false;
  });
  document.addEventListener('keydown', event => {
    if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 'k') {
      event.preventDefault();
      openDialog('search-dialog');
    }
  });
  document.querySelector('[data-section-search]')?.addEventListener('input', event => {
    const search = event.target.value.trim().toLocaleLowerCase('ru');
    const links = [...document.querySelectorAll('#search-dialog .search-results a')];
    links.forEach(link => { link.hidden = !link.textContent.toLocaleLowerCase('ru').includes(search); });
    document.querySelector('[data-search-empty]').hidden = links.some(link => !link.hidden);
  });
  document.querySelectorAll('[data-submit-change]').forEach(input => input.addEventListener('change', () => input.form.requestSubmit()));
  document.querySelector('.period-form')?.addEventListener('submit', event => {
    // Avoid duplicate Days values when a period button is the submitter.
    const fallback = event.target.querySelector('[data-default-period]');
    if (fallback) fallback.disabled = event.submitter?.name === 'Days';
  });
  document.querySelectorAll('.dashboard-form').forEach(form => form.addEventListener('submit', () => {
    const button = form.querySelector('button[type="submit"]');
    button.disabled = true;
    button.setAttribute('aria-busy', 'true');
    button.textContent = 'Сохраняем…';
  }));
  window.addEventListener('pageshow', () => document.querySelectorAll('[aria-busy="true"]').forEach(button => {
    button.disabled = false;
    button.removeAttribute('aria-busy');
    button.textContent = 'Сохранить';
  }));
})();

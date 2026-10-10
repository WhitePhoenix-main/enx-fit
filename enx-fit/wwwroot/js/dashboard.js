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
    if (trigger) { if (trigger.tagName === 'A') event.preventDefault(); openDialog(trigger.dataset.dialog); }
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
    if (event.key === 'Escape') {
      const dialog = document.querySelector('dialog[open]');
      if (dialog) { event.preventDefault(); dialog.close(); }
      document.querySelectorAll('.account-menu[open], .ef-chart-period[open], .ef-why[open]')
        .forEach(menu => { menu.open = false; });
    }
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
  document.querySelectorAll('.dashboard-form').forEach(form => {
    const button = form.querySelector('button[type="submit"]');
    if (!button) return;
    const feedback = document.createElement('p');
    feedback.className = 'app-status';
    feedback.setAttribute('role', 'status');
    feedback.setAttribute('aria-live', 'polite');
    feedback.dataset.dashboardFeedback = '';
    feedback.hidden = true;
    form.append(feedback);
    let busy = false;
    const notice = (state, message) => {
      window.AppComponents?.setNoticeState(feedback, state);
      feedback.textContent = message;
      feedback.hidden = false;
    };
    form.addEventListener('submit', event => {
      if (busy) { event.preventDefault(); return; }
      if (!form.checkValidity()) { event.preventDefault(); form.reportValidity(); return; }
      if (!navigator.onLine) {
        event.preventDefault();
        notice('error', 'Сейчас нет сети. Данные остались в форме; повторите сохранение после подключения.');
      }
    });
    // Run after form/document validators. Disabling is deferred so the native submitter remains in the POST.
    window.addEventListener('submit', event => {
      if (event.target !== form || event.defaultPrevented || !form.checkValidity() || busy) return;
      busy = true;
      form.setAttribute('aria-busy', 'true');
      button.setAttribute('aria-busy', 'true');
      notice('pending', 'Ожидаем подтверждения сохранения…');
      setTimeout(() => { if (busy) button.disabled = true; }, 0);
    });
    window.addEventListener('online', () => {
      if (feedback.dataset.appState === 'error') notice('pending', 'Подключение восстановлено. Проверьте данные и повторите сохранение.');
    });
    window.addEventListener('pageshow', () => {
      const wasBusy = busy;
      busy = false;
      form.removeAttribute('aria-busy');
      button.removeAttribute('aria-busy');
      button.disabled = false;
      if (wasBusy) notice('pending', 'Проверьте данные перед повторным сохранением.');
    });
  });
})();
